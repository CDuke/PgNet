using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace PgNet.BackendMessage
{
    internal class BackendMessageReader<T> where T : IReceiver
    {
        private T m_receiver;
        private Memory<byte> m_receiveBuffer;
        private ReadOnlyMemory<byte> m_content;
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
            if (MessageSize > 0)
                m_content = m_content.Slice(MessageSize + 1);
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
                m_content = m_receiveBuffer.Slice(0,receiveCount);
            }

            MessageType = m_content.Span[0];
            MessageSize = BinaryPrimitives.ReadInt32BigEndian(m_content.Span.Slice(1));
            m_readyForQuery = MessageType == BackendMessageCode.ReadyForQuery;

            return true;
        }

        public CommandComplete ReadCommandComplete() => new CommandComplete(m_content);

        public ReadyForQuery ReadReadyForQuery() => new ReadyForQuery(m_content);

        public RowDescription ReadRowDescription() => new RowDescription(m_content);

        public DataRow ReadDataRow() => new DataRow(m_content);

        public ErrorResponse ReadErrorResponse() => new ErrorResponse(m_content, ArrayPool<ErrorOrNoticeResponseField>.Shared);

        public NoticeResponse ReadNoticeResponse() => new NoticeResponse(m_content, ArrayPool<ErrorOrNoticeResponseField>.Shared);
    }
}
