using System;
using System.Buffers;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using PgNet.BackendMessage;
using PgNet.FrontendMessage;

namespace PgNet
{
    public sealed class PgConnector : IAsyncDisposable
    {
        private class ConnectionInfo
        {
            public string Host = string.Empty;
            public string Database = string.Empty;
            public string UserName = string.Empty;
            public string Password = string.Empty;
        }

        private const int SocketCloseTimeoutDefault = 5; // 5 seconds

        private readonly ArrayPool<byte> m_arrayPool;
        private const int PostgreSQLDefaultPort = 5432;
        private Socket? m_socket;
        private ConnectorState m_connectorState;

        private int m_processId;
        private int m_secretKey;

        private byte m_transactionStatus;

        private ConnectionInfo m_connectionInfo;

        private static readonly ValueTask<IPAddress[]> s_loopback = new ValueTask<IPAddress[]>(new[] {IPAddress.Loopback});

        public PgConnector(ArrayPool<byte> arrayPool)
        {
            m_arrayPool = arrayPool;
            m_connectorState = ConnectorState.Disconnected;
        }

        public async ValueTask OpenAsync(string host, string database, string userName, string password,
            CancellationToken cancellationToken)
        {
            m_connectorState = ConnectorState.Connecting;
            var socketTask = ConnectAsync(host);
            var socket = socketTask.IsCompletedSuccessfully
                ? socketTask.Result
                : await socketTask.ConfigureAwait(false);

            var sendBufferBytes = m_arrayPool.Rent(512);
            var receiveBufferBytes = m_arrayPool.Rent(512);
            try
            {
                var sendBuffer = new Memory<byte>(sendBufferBytes);
                var receiveBuffer = new Memory<byte>(sendBufferBytes);

                var sendStartupMessageTask =
                    SendStartupMessage(socket, sendBuffer, database, userName, cancellationToken);
                if (!sendStartupMessageTask.IsCompletedSuccessfully)
                {
                    await sendStartupMessageTask.ConfigureAwait(false);
                }

                var processStartupMessageResponseTask = ProcessStartupMessageResponse(socket, sendBuffer, receiveBuffer,
                    userName, password, cancellationToken);
                if (!processStartupMessageResponseTask.IsCompletedSuccessfully)
                {
                    await processStartupMessageResponseTask.ConfigureAwait(false);
                }
                
                m_connectorState = ConnectorState.ReadyForQuery;

                m_connectionInfo = new ConnectionInfo();
                m_connectionInfo.Host = host;
                m_connectionInfo.Database = database;
                m_connectionInfo.UserName = userName;
                m_connectionInfo.Password = password;

                m_socket = socket;
            }
            finally
            {
                m_arrayPool.Return(sendBufferBytes);
                m_arrayPool.Return(receiveBufferBytes);
            }
        }

        public ValueTask<int> SendQueryAsync(string query, CancellationToken cancellationToken)
        {
            return SendQueryAsync(query.AsMemory(), cancellationToken);
        }

        public async ValueTask<int> SendQueryAsync(ReadOnlyMemory<char> query, CancellationToken cancellationToken)
        {
            var w = new Query(query);
            var socket = m_socket!;
            var sendRequestTask = WriteAndSendMessage(socket, w, cancellationToken);
            if (!sendRequestTask.IsCompletedSuccessfully)
            {
                await sendRequestTask.ConfigureAwait(false);
            }

            var receiveBufferArray = m_arrayPool.Rent(512);
            var receiveBuffer = new Memory<byte>(receiveBufferArray);
            try
            {
                var reader = new BackendMessageReader<SocketReceiver>(new SocketReceiver(socket), receiveBuffer);
                var messageRef = MessageRef.Empty;
                while (true)
                {
                    var moveNextTask = reader.MoveNext(messageRef, cancellationToken);
                    messageRef = !moveNextTask.IsCompletedSuccessfully
                        ? await moveNextTask
                        : moveNextTask.Result;
                    if (!messageRef.HasData)
                    {
                        break;
                    }

                    switch (messageRef.MessageType)
                    {
                        case BackendMessageCode.CompletedResponse:
                            reader.ReadCommandComplete(messageRef);
                            break;
                        case BackendMessageCode.CopyInResponse:
                            ThrowHelper.ThrowNotImplementedException();
                            break;
                        case BackendMessageCode.CopyOutResponse:
                            ThrowHelper.ThrowNotImplementedException();
                            break;
                        case BackendMessageCode.RowDescription:
                            reader.ReadRowDescription(messageRef);
                            break;
                        case BackendMessageCode.DataRow:
                            reader.ReadDataRow(messageRef);
                            break;
                        case BackendMessageCode.EmptyQueryResponse:
                            ThrowHelper.ThrowNotImplementedException();
                            break;
                        case BackendMessageCode.ErrorResponse:
                            reader.ReadErrorResponse(messageRef);
                            ThrowHelper.ThrowNotImplementedException();
                            break;
                        case BackendMessageCode.ReadyForQuery:
                            reader.ReadReadyForQuery(messageRef);
                            break;
                        case BackendMessageCode.NoticeResponse:
                            reader.ReadNoticeResponse(messageRef);
                            break;
                        default:
                            ThrowHelper.ThrowUnexpectedBackendMessageException(messageRef.MessageType);
                            break;
                    }
                }
            }
            finally
            {
                m_arrayPool.Return(receiveBufferArray);
            }

            //ThrowHelper.ThrowNotImplementedException();
            return 0;
        }

        public async ValueTask Cancel(CancellationToken cancellationToken)
        {
            if (m_processId == 0)
                ThrowHelper.ThrowPostgresException("Cancellation not supported on this database (no BackendKeyData was received during connection)");

            var connectionInfo = m_connectionInfo;
            await using var connector = new PgConnector(m_arrayPool);
            var openTask = connector.OpenAsync(connectionInfo.Host, connectionInfo.Database, connectionInfo.UserName,
                connectionInfo.Password, cancellationToken);
            if (!openTask.IsCompletedSuccessfully)
                await openTask.ConfigureAwait(false);
            var cancelRequest = new CancelRequest(m_processId, m_secretKey);
            var socket = connector.m_socket!;
            var sendTask = connector.WriteAndSendMessage(socket, cancelRequest, cancellationToken);
            if (sendTask.IsCompletedSuccessfully)
                await sendTask.ConfigureAwait(false);

            // Now wait for the server to close the connection, better chance of the cancellation
            // actually being delivered before we continue with the user's logic.
            var waitTask = connector.WaitForDisconnect(socket, cancellationToken);
            if (waitTask.IsCompletedSuccessfully)
                await waitTask.ConfigureAwait(false);

        }

        private async ValueTask<bool> WaitForDisconnect(Socket socket, CancellationToken cancellationToken)
        {
            var receiveBufferBytes = m_arrayPool.Rent(1);
            try
            {
                var socketReceiver = new SocketReceiver(socket);
                var receiveCount = await socketReceiver.ReceiveAsync(receiveBufferBytes, cancellationToken).ConfigureAwait(false);
                return receiveCount <= 0;
            }
            catch (SocketException socketException) when (socketException.SocketErrorCode == SocketError.ConnectionReset)
            {
                return true;
            }
            catch (SocketException)
            {
                return false;
            }
            finally
            {
                m_arrayPool.Return(receiveBufferBytes);
            }
        }

        private static async ValueTask<Socket> ConnectAsync(string host)
        {
            // Note that there aren't any timeoutable or cancellable DNS methods
            var endpointsTask = ParseHost(host);
            var endpoints = endpointsTask.IsCompletedSuccessfully
                ? endpointsTask.Result
                : await endpointsTask.ConfigureAwait(false);

            Socket? socket = default;
            Exception? exception = default;
            foreach (var ipAddress in endpoints)
            {
                if (ipAddress == null
                    || (ipAddress.AddressFamily == AddressFamily.InterNetwork && !Socket.OSSupportsIPv4)
                    || (ipAddress.AddressFamily == AddressFamily.InterNetworkV6 && !Socket.OSSupportsIPv6))
                {
                    continue;
                }

                var protocolType = (ipAddress.AddressFamily == AddressFamily.InterNetwork || ipAddress.AddressFamily == AddressFamily.InterNetworkV6)
                    ? ProtocolType.Tcp
                    : ProtocolType.IP;

                socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, protocolType);

                try
                {
                    var connectTask = socket.ConnectAsync(ipAddress, PostgreSQLDefaultPort);
                    if (!connectTask.IsCompletedSuccessfully)
                    {
                        await connectTask.ConfigureAwait(false);
                    }

                    exception = null;
                    break;
                }
#pragma warning disable CA1031 // Do not catch general exception types
                catch (Exception e)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    exception = e;
                    socket.SafeDispose();
                    socket = default;
                }
            }

            if (socket == null)
            {
                throw exception ?? new SocketException((int)SocketError.AddressNotAvailable);
            }

            SetSocketOptions(socket);

            return socket;
        }

        private static ValueTask<IPAddress[]> ParseHost(string host)
        {
            if (host == "localhost" || host == "127.0.0.1")
                return s_loopback;

            if (IPAddress.TryParse(host, out var ipAddress))
            {
                return new ValueTask<IPAddress[]>(new [] {ipAddress});
            }

            return new ValueTask<IPAddress[]>(Dns.GetHostAddressesAsync(host));
        }

        private static ValueTask<int> SendStartupMessage(Socket socket, Memory<byte> sendBuffer, string database, string userName,
            CancellationToken cancellationToken)
        {
            var startupMessage = new StartupMessage();
            startupMessage.SetDatabase(database);
            startupMessage.SetUser(userName);
            startupMessage.SetApplicationName("MyApp");
            return WriteAndSendMessage(startupMessage, socket, sendBuffer, cancellationToken);
        }

        private async ValueTask ProcessStartupMessageResponse(Socket socket, Memory<byte> sendBuffer, Memory<byte> receiveBuffer,
            string userName, string password, CancellationToken cancellationToken)
        {
            var receiver = new SocketReceiver(socket);
            var messageReader = new BackendMessageReader<SocketReceiver>(receiver, receiveBuffer);
            var messageRef = MessageRef.Empty;
            var receiveAsyncTask = messageReader.MoveNext(messageRef, cancellationToken);
            messageRef = receiveAsyncTask.IsCompletedSuccessfully
                ? receiveAsyncTask.Result
                : await receiveAsyncTask.ConfigureAwait(false);


            if (messageRef.HasData)
            {
                switch (messageRef.MessageType)
                {
                    case BackendMessageCode.AuthenticationRequest:
                        var authResponse = messageReader.ReadAuthentication(messageRef);
                        await ProcessAuthentication(authResponse, userName, password, socket, sendBuffer, messageReader, cancellationToken)
                            .ConfigureAwait(false);
                        break;
                    case BackendMessageCode.ErrorResponse:
                        // TODO: process error response
                        messageReader.ReadErrorResponse(messageRef);
                        break;
                    default:
                        ThrowHelper.ThrowUnexpectedBackendMessageException(messageRef.MessageType);
                        break;
                }
            }
            else
            {
                // TODO:
                ThrowHelper.ThrowNotImplementedException();
            }
        }

        private async ValueTask ProcessAuthentication<TReceiver>(Authentication authResponse, string userName, string password,
            Socket socket, Memory<byte> sendBuffer, BackendMessageReader<TReceiver> messageReader, CancellationToken cancellationToken) where TReceiver : IReceiver
        {
            switch (authResponse.AuthenticationRequestType)
            {
                case AuthenticationRequestType.Ok:
                    ThrowHelper.ThrowNotImplementedException();
                    break;
                case AuthenticationRequestType.KerberosV5:
                    ThrowHelper.ThrowNotImplementedException();
                    break;
                case AuthenticationRequestType.CleartextPassword:
                    await AuthenticateClearText(password, socket, sendBuffer, cancellationToken).ConfigureAwait(false);
                    break;
                case AuthenticationRequestType.MD5Password:
                    await AuthenticateMD5(authResponse.AdditionalInfo, userName, password, socket, sendBuffer, cancellationToken).ConfigureAwait(false);
                    break;
                case AuthenticationRequestType.SCMCredential:
                    ThrowHelper.ThrowNotImplementedException();
                    break;
                case AuthenticationRequestType.GSS:
                    ThrowHelper.ThrowNotImplementedException();
                    break;
                case AuthenticationRequestType.GSSContinue:
                    ThrowHelper.ThrowNotImplementedException();
                    break;
                case AuthenticationRequestType.SSPI:
                    ThrowHelper.ThrowNotImplementedException();
                    break;
                case AuthenticationRequestType.SASL:
                    ThrowHelper.ThrowNotImplementedException();
                    break;
                case AuthenticationRequestType.SASLContinue:
                    ThrowHelper.ThrowNotImplementedException();
                    break;
                case AuthenticationRequestType.SASLFinal:
                    ThrowHelper.ThrowNotImplementedException();
                    break;
                default:
                    ThrowHelper.ThrowUnexpectedAuthenticationRequestType(authResponse.AuthenticationRequestType);
                    break;
            }

            var message = MessageRef.Empty;

            while (true)
            {
                var moveNextTask = messageReader.MoveNext(message, cancellationToken);
                message = !moveNextTask.IsCompletedSuccessfully
                    ? await moveNextTask
                    : moveNextTask.Result;

                if (!message.HasData)
                {
                    break;
                }
                switch (message.MessageType)
                {
                    case BackendMessageCode.AuthenticationRequest:
                        if (!messageReader.IsAuthenticationOK(message))
                        {
                            ThrowHelper.ThrowNotImplementedException();
                        }
                        break;
                    case BackendMessageCode.ParameterStatus:
                        // TODO:
                        break;
                    case BackendMessageCode.BackendKeyData:
                        var backendKeyData = messageReader.ReadBackendKeyData(message);
                        m_processId = backendKeyData.ProcessId;
                        m_secretKey = backendKeyData.SecretKey;
                        break;
                    case BackendMessageCode.ReadyForQuery:
                        var readyForQuery = messageReader.ReadReadyForQuery(message);
                        m_transactionStatus = readyForQuery.TransactionStatus;
                        break;
                    case BackendMessageCode.ErrorResponse:
                        //TODO:
                        var error = messageReader.ReadErrorResponse(message);
                        break;
                    default:
                        ThrowHelper.ThrowUnexpectedBackendMessageException(message.MessageType);
                        break;
                }
            }
        }

        private static ValueTask<int> AuthenticateClearText(string password, Socket socket,
            Memory<byte> sendBuffer, CancellationToken cancellationToken)
        {
            var w = new PasswordCleartext(password);
            return WriteAndSendMessage(w, socket, sendBuffer, cancellationToken);
        }

        private static ValueTask<int> AuthenticateMD5(ReadOnlyMemory<byte> salt, string user, string password,
            Socket socket, Memory<byte> sendBuffer, CancellationToken cancellationToken)
        {
            var w = new PasswordMD5Message(user, password, salt);
            return WriteAndSendMessage(w, socket, sendBuffer, cancellationToken);
        }

        /*private async ValueTask<int> WriteAndSendMessage<T>(T writer, Socket socket, CancellationToken cancellationToken) where T : struct, IFrontendMessageWriter
        {
            var messageLength = writer.CalculateLength();
            var messageBytes = m_arrayPool.Rent(messageLength);
            var message = new Memory<byte>(messageBytes).Slice(0, messageLength);

            int send;
            try
            {
                writer.Write(message);
                send = await socket.SendAsync(message, SocketFlags.None, cancellationToken);
            }
            finally
            {
                m_arrayPool.Return(messageBytes);
            }

            return send;
        }*/

        private static ValueTask<int> WriteAndSendMessage<T>(T writer, Socket socket, Memory<byte> sendBuffer,
            CancellationToken cancellationToken) where T : struct, IFrontendMessageWriter
        {
            var messageLength = writer.CalculateLength();
            return WriteAndSendMessage(writer, socket, sendBuffer, messageLength, cancellationToken);
        }

        private async ValueTask<int> WriteAndSendMessage<T>(Socket socket, T writer,
            CancellationToken cancellationToken) where T : struct, IFrontendMessageWriter
        {
            var messageLength = writer.CalculateLength();
            var sendBufferBytes = m_arrayPool.Rent(messageLength);
            try
            {
                var sendBuffer = new Memory<byte>(sendBufferBytes);

                var sendAsyncTask = WriteAndSendMessage(writer, socket, sendBuffer, messageLength, cancellationToken);

                return !sendAsyncTask.IsCompletedSuccessfully
                    ? await sendAsyncTask.ConfigureAwait(false)
                    : sendAsyncTask.Result;
            }
            finally
            {
                m_arrayPool.Return(sendBufferBytes);
            }
        }

        private static ValueTask<int> WriteAndSendMessage<T>(T writer, Socket socket, Memory<byte> sendBuffer, int messageLength,
            CancellationToken cancellationToken) where T : struct, IFrontendMessageWriter
        {
            var message = sendBuffer.Slice(0, messageLength);
            writer.Write(message);
            return socket.SendAsync(message, SocketFlags.None, cancellationToken);
        }

        /*private ValueTask<int> SendSimpleMessage<T>(T sender, CancellationToken cancellationToken) where T : struct, IFrontendMessageSender
        {
            return m_socket != null
                ? sender.Send(m_socket, cancellationToken)
                : new ValueTask<int>(0);
        }*/

        private static ValueTask<int> SendSimpleMessage<T>(Socket socket, T knownMessage, CancellationToken cancellationToken) where T : struct, IKnownFrontendMessage
        {
            var m = knownMessage.GetMessage();
            return socket?.SendAsync(m, SocketFlags.None, cancellationToken) ?? new ValueTask<int>(0);
        }

        private static void SetSocketOptions(Socket socket)
        {
            socket.NoDelay = true;
        }

        public async ValueTask CloseAsync(int socketCloseTimeoutSeconds, CancellationToken cancellationToken)
        {
            if (m_socket != null)
            {
                var socket = m_socket;
                var sentSimpleMessageTask = SendSimpleMessage(socket, new Terminate(), cancellationToken);
                if (!sentSimpleMessageTask.IsCompletedSuccessfully)
                    await sentSimpleMessageTask.ConfigureAwait(false);
                socket.Shutdown(SocketShutdown.Both);
                socket.Close(socketCloseTimeoutSeconds);
            }
        }

        public async ValueTask DisposeAsync()
        {
            var closeTask = CloseAsync(SocketCloseTimeoutDefault, CancellationToken.None);
            if (!closeTask.IsCompletedSuccessfully)
                await closeTask.ConfigureAwait(false);
            m_socket?.SafeDispose();
            m_socket = null;
        }
    }
}
