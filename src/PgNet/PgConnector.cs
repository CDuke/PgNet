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
            public string Host;
            public string Database;
            public string UserName;
            public string Password;
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
            var socketTask = ConnectAsync(host, cancellationToken);
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
            var sendRequestTask = WriteAndSendMessage(w, cancellationToken);
            if (!sendRequestTask.IsCompletedSuccessfully)
            {
                await sendRequestTask.ConfigureAwait(false);
            }

            var receiveBufferArray = m_arrayPool.Rent(512);
            var receiveBuffer = new Memory<byte>(receiveBufferArray);
            var receiveTask = m_socket.ReceiveAsync(receiveBuffer, SocketFlags.None, cancellationToken);
            var receiveCount = !receiveTask.IsCompletedSuccessfully
                ? await receiveTask.ConfigureAwait(false)
                : receiveTask.Result;

            var temp = receiveBuffer.Slice(0, receiveCount);
            while (temp.Length > 0)
            {
                var responseCode = temp.Span[0];
                switch (responseCode)
                {
                    case BackendMessageCode.CompletedResponse:
                        var completedResponse = new CommandComplete(temp);
                        temp = temp.Slice(completedResponse.Length + 1);
                        break;
                    case BackendMessageCode.CopyInResponse:
                        ThrowHelper.ThrowNotImplementedException();
                        break;
                    case BackendMessageCode.CopyOutResponse:
                        ThrowHelper.ThrowNotImplementedException();
                        break;
                    case BackendMessageCode.RowDescription:
                        var rowDescription = new RowDescription(temp);
                        temp = temp.Slice(rowDescription.Length + 1);
                        break;
                    case BackendMessageCode.DataRow:
                        var dataRow = new DataRow(temp);
                        temp = temp.Slice(dataRow.Length + 1);
                        break;
                    case BackendMessageCode.EmptyQueryResponse:
                        ThrowHelper.ThrowNotImplementedException();
                        break;
                    case BackendMessageCode.ErrorResponse:
                        ThrowHelper.ThrowNotImplementedException();
                        break;
                    case BackendMessageCode.ReadyForQuery:
                        var readyForQuery = new ReadyForQuery(temp);
                        temp = temp.Slice(readyForQuery.Length + 1);
                        break;
                    case BackendMessageCode.NoticeResponse:
                        ThrowHelper.ThrowNotImplementedException();
                        break;
                    default:
                        ThrowHelper.ThrowNotImplementedException();
                        break;
                }
            }

            //ThrowHelper.ThrowNotImplementedException();
            return 0;
        }

        public async ValueTask Cancel(CancellationToken cancellationToken)
        {
            if (m_processId == 0)
                throw new PostgresException("Cancellation not supported on this database (no BackendKeyData was received during connection)");

            var connectionInfo = m_connectionInfo;
            await using var connector = new PgConnector(m_arrayPool);
            var openTask = connector.OpenAsync(connectionInfo.Host, connectionInfo.Database, connectionInfo.UserName,
                connectionInfo.Password, cancellationToken);
            if (!openTask.IsCompletedSuccessfully)
                await openTask;
            var cancelRequest = new CancelRequest(m_processId, m_secretKey);
            var sendTask = connector.WriteAndSendMessage(cancelRequest, cancellationToken);
            if (sendTask.IsCompletedSuccessfully)
                await sendTask;

            // Now wait for the server to close the connection, better chance of the cancellation
            // actually being delivered before we continue with the user's logic.
            await connector.WaitForDisconnect(cancellationToken);

        }

        private async ValueTask<bool> WaitForDisconnect(CancellationToken cancellationToken)
        {
            byte[]? sendBufferBytes = null;
            try
            {
                sendBufferBytes = m_arrayPool.Rent(1);
                var sendBuffer = new Memory<byte>(sendBufferBytes);
                var receiveCount = await m_socket.ReceiveAsync(sendBuffer, SocketFlags.None, cancellationToken);
                if (receiveCount > 0)
                {
                    return false;
                }

                return true;
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
                if (sendBufferBytes != null)
                    m_arrayPool.Return(sendBufferBytes);
            }
        }

        private static async ValueTask<Socket> ConnectAsync(string host, CancellationToken cancellationToken)
        {
            var endpointsTask = ParseHost(host, cancellationToken);
            var endpoints = endpointsTask.IsCompletedSuccessfully
                ? endpointsTask.Result
                : await endpointsTask.ConfigureAwait(false);

            foreach (var ipAddress in endpoints)
            {
                var protocolType = ipAddress.AddressFamily == AddressFamily.InterNetwork
                    ? ProtocolType.Tcp
                    : ProtocolType.IP;
                var socket = new Socket(ipAddress.AddressFamily, SocketType.Stream, protocolType);

                SetSocketOptions(socket);

                var connectTask = socket.ConnectAsync(ipAddress, PostgreSQLDefaultPort);
                if (!connectTask.IsCompletedSuccessfully)
                {
                    await connectTask.ConfigureAwait(false);
                }

                return socket;
            }

            //TODO:
            ThrowHelper.ThrowNotImplementedException();
            return null;
        }

        private static ValueTask<IPAddress[]> ParseHost(string host, CancellationToken cancellationToken)
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
            var receiveAsyncTask = socket.ReceiveAsync(receiveBuffer, SocketFlags.None, cancellationToken);
            var receiveAsync = receiveAsyncTask.IsCompletedSuccessfully
                ? receiveAsyncTask.Result
                : await receiveAsyncTask.ConfigureAwait(false);


            if (receiveAsync > 0)
            {
                var responseCode = receiveBuffer.Span[0];
                switch (responseCode)
                {
                    case BackendMessageCode.AuthenticationRequest:
                        var authResponse = receiveBuffer.Slice(0, receiveAsync);
                        await ProcessAuthentication(authResponse, userName, password, socket, sendBuffer, receiveBuffer, cancellationToken)
                            .ConfigureAwait(false);
                        break;
                    case BackendMessageCode.ErrorResponse: break;
                    default:
                        throw new UnexpectedBackendMessageException(
                            $"Unexpected backend message '{(char)responseCode}'");
                }
            }
            else
            {
                // TODO:
                ThrowHelper.ThrowNotImplementedException();
            }
        }

        private async ValueTask ProcessAuthentication(ReadOnlyMemory<byte> authenticationResponse, string userName, string password,
            Socket socket, Memory<byte> sendBuffer, Memory<byte> receiveBuffer, CancellationToken cancellationToken)
        {
            var authResponse = new Authentication(authenticationResponse);
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
                    throw new ArgumentOutOfRangeException(nameof(authResponse.AuthenticationRequestType));
            }

            var result = await socket.ReceiveAsync(receiveBuffer, SocketFlags.None, cancellationToken)
                .ConfigureAwait(false);

            var buffer = receiveBuffer;
            if (result > 0)
            {
                buffer = buffer.Slice(0, result);

                byte responseCode;
                if (!Authentication.IsOk(buffer))
                {
                    responseCode = buffer.Span[0];

                    switch (responseCode)
                    {
                        case BackendMessageCode.AuthenticationRequest:
                            //TODO:
                            ThrowHelper.ThrowNotImplementedException();

                            break;
                        case BackendMessageCode.ErrorResponse:
                            var error = new ErrorResponse(buffer, ArrayPool<ErrorOrNoticeResponseField>.Shared);
                            break;
                        default:
                            throw new UnexpectedBackendMessageException(
                                $"Unexpected backend message '{(char)responseCode}'");
                    }
                }
                buffer = buffer.Slice(Authentication.OkResponseLength);

                responseCode = buffer.Span[0];
                while (responseCode == BackendMessageCode.ParameterStatus)
                {
                    var parameterStatus = new ParameterStatus(buffer);
                    buffer = buffer.Slice(parameterStatus.Length + 1);
                    responseCode = buffer.Span[0];
                }

                if (responseCode == BackendMessageCode.BackendKeyData)
                {
                    var backendKeyData = new BackendKeyData(buffer);
                    m_processId = backendKeyData.ProcessId;
                    m_secretKey = backendKeyData.SecretKey;
                    buffer = buffer.Slice(backendKeyData.Length + 1);
                    responseCode = buffer.Span[0];
                }

                if (responseCode == BackendMessageCode.ReadyForQuery)
                {
                    var readyForQuery = new ReadyForQuery(buffer);
                    m_transactionStatus = readyForQuery.TransactionStatus;
                    buffer = buffer.Slice(readyForQuery.Length + 1);
                }
            }
            else
            {
                // TODO:
                ThrowHelper.ThrowNotImplementedException();
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

            var message = sendBuffer.Slice(0, messageLength);
            writer.Write(message);
            return socket.SendAsync(message, SocketFlags.None, cancellationToken);
        }

        private async ValueTask<int> WriteAndSendMessage<T>(T writer, 
            CancellationToken cancellationToken) where T : struct, IFrontendMessageWriter
        {
            byte[]? sendBufferBytes = null;
            try
            {
                var messageLength = writer.CalculateLength();
                sendBufferBytes = m_arrayPool.Rent(messageLength);
                var sendBuffer = new Memory<byte>(sendBufferBytes);

                var message = sendBuffer.Slice(0, messageLength);
                writer.Write(message);
                var sendAsyncTask = m_socket.SendAsync(message, SocketFlags.None, cancellationToken);

                return !sendAsyncTask.IsCompletedSuccessfully
                    ? await sendAsyncTask.ConfigureAwait(false)
                    : sendAsyncTask.Result;
            }
            finally
            {
                if (sendBufferBytes != null)
                    m_arrayPool.Return(sendBufferBytes);
            }
        }

        private ValueTask<int> SendSimpleMessage<T>(T sender, CancellationToken cancellationToken) where T : struct, IFrontendMessageSender
        {
            return m_socket != null
                ? sender.Send(m_socket, cancellationToken)
                : new ValueTask<int>(0);
        }

        private static void SetSocketOptions(Socket socket)
        {
            socket.NoDelay = true;
        }

        public async ValueTask CloseAsync(int socketCloseTimeoutSeconds, CancellationToken cancellationToken)
        {
            await SendSimpleMessage(new Terminate(), cancellationToken);
            m_socket?.Shutdown(SocketShutdown.Both);
            m_socket?.Close(socketCloseTimeoutSeconds);
        }

        public async ValueTask DisposeAsync()
        {
            await CloseAsync(SocketCloseTimeoutDefault, CancellationToken.None);
            m_socket?.Dispose();
            m_socket = null;
        }
    }
}
