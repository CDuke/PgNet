using System;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;

namespace PgNet.BackendMessage
{
    internal interface IReceiver
    {
        ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken);
    }

    internal readonly struct SocketReceiver : IReceiver
    {
        private readonly Socket _socket;

        public SocketReceiver(Socket socket)
        {
            _socket = socket;
        }

        public ValueTask<int> ReceiveAsync(Memory<byte> buffer, CancellationToken cancellationToken)
        {
            return _socket.ReceiveAsync(buffer, SocketFlags.None, cancellationToken);
        }
    }

    internal struct BackendMessageReader<T> where T: IReceiver
    {
        private T m_receiver;
        private Memory<byte> m_receiveBuffer;
        private Memory<byte> m_content;
        private readonly int m_lastReceiveCount;
        private int m_totalReceived;

        private bool m_readyForQuery;

        public byte MessageType { get; private set; }
        public int MessageSize { get; private set; }

        public BackendMessageReader(T receiver, Memory<byte> receiveBuffer)
        {
            m_receiver = receiver;
            m_receiveBuffer = receiveBuffer;
            m_lastReceiveCount = 0;
            m_totalReceived = 0;
            m_content = Memory<byte>.Empty;
            MessageType = 0;
            MessageSize = 0;
            m_readyForQuery = false;
        }

        public async ValueTask<bool> MoveNext(CancellationToken cancellationToken)
        {
            if (m_readyForQuery)
                return false;
            if (m_content.IsEmpty)
            {
                var receiveTask = m_receiver.ReceiveAsync(m_receiveBuffer, cancellationToken);
                var receiveCount = !receiveTask.IsCompletedSuccessfully
                    ? await receiveTask.ConfigureAwait(false)
                    : receiveTask.Result;

                if (receiveCount == 0)
                    return false;
                m_totalReceived += receiveCount;
                if (m_totalReceived <= sizeof(byte) + sizeof(int))
                    ThrowHelper.ThrowNotImplementedException();
                m_content = m_receiveBuffer;
            }
            else
            {
                m_content = m_content.Slice(MessageSize + 1);
            }

            MessageType = m_content.Span[0];
            MessageSize = m_content.Span[1];
            m_readyForQuery = MessageType == BackendMessageCode.ReadyForQuery;

            return true;
        }

        public CommandComplete ReadCommandComplete()
        {
            var completedResponse = new CommandComplete(m_content);
            return completedResponse;
        }

        public ReadyForQuery ReadReadyForQuery()
        {
            var readyForQuery = new ReadyForQuery(m_content);
            return readyForQuery;
        }
    }
}
