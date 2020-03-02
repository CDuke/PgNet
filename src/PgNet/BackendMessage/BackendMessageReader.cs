using System;
using System.Buffers;
using System.Buffers.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace PgNet.BackendMessage
{
    internal readonly struct BackendMessageReader<T> where T : IReceiver
    {
        private readonly T m_receiver;
        private readonly Memory<byte> m_receiveBuffer;

        public BackendMessageReader(T receiver, Memory<byte> receiveBuffer)
        {
            m_receiver = receiver;
            m_receiveBuffer = receiveBuffer;
        }

        public async ValueTask<MessageRef> MoveNext(MessageRef previousMessage, CancellationToken cancellationToken)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var contentLength = previousMessage.ContentLength;
            var messageStartIndex = previousMessage.StartIndex;
            var messageLength = previousMessage.MessageLength;
            contentLength -= messageLength;
            if (contentLength == 0)
            {
                if (previousMessage.IsReadyForQuery)
                {
                    return MessageRef.Empty;
                }

                var receiveTask = m_receiver.ReceiveAsync(m_receiveBuffer, cancellationToken);
                contentLength = !receiveTask.IsCompletedSuccessfully
                    ? await receiveTask.ConfigureAwait(false)
                    : receiveTask.Result;

                if (contentLength == 0)
                {
                    return MessageRef.Empty;
                }

                messageStartIndex = -messageLength;
            }

            if (contentLength <= sizeof(byte) + sizeof(int))
            {
                ThrowHelper.ThrowNotImplementedException();
            }

            messageStartIndex += messageLength;

            var messageType = m_receiveBuffer.Span[messageStartIndex];
            messageLength = 1 + BinaryPrimitives.ReadInt32BigEndian(m_receiveBuffer.Span.Slice(messageStartIndex + 1));

            return new MessageRef(messageType, messageStartIndex, messageLength, contentLength);
        }

        public CommandComplete ReadCommandComplete(MessageRef messageRef) => new CommandComplete(SliceMessage(messageRef));

        public ReadyForQuery ReadReadyForQuery(MessageRef messageRef) => new ReadyForQuery(SliceMessage(messageRef));

        public RowDescription ReadRowDescription(MessageRef messageRef) => new RowDescription(SliceMessage(messageRef));

        public DataRow ReadDataRow(MessageRef messageRef) => new DataRow(SliceMessage(messageRef));

        public ErrorResponse ReadErrorResponse(MessageRef messageRef) => new ErrorResponse(SliceMessage(messageRef), ArrayPool<ErrorOrNoticeResponseField>.Shared);

        public NoticeResponse ReadNoticeResponse(MessageRef messageRef) => new NoticeResponse(SliceMessage(messageRef), ArrayPool<ErrorOrNoticeResponseField>.Shared);

        public Authentication ReadAuthentication(MessageRef messageRef) => new Authentication(SliceMessage(messageRef));

        private ReadOnlyMemory<byte> SliceMessage(MessageRef messageRef) =>
            m_receiveBuffer.Slice(messageRef.StartIndex, messageRef.MessageLength);
    }

    internal readonly struct MessageRef
    {
        internal static MessageRef Empty = new MessageRef();

        public readonly byte MessageType;

        public readonly int StartIndex;

        public readonly int MessageLength;

        public readonly int ContentLength;

        public MessageRef(byte messageType, int startIndex, int messageLength, int contentLength)
        {
            MessageType = messageType;
            StartIndex = startIndex;
            MessageLength = messageLength;
            ContentLength = contentLength;
        }

        public bool IsReadyForQuery => MessageType == BackendMessageCode.ReadyForQuery;

        public bool HasData => MessageLength > 0;
    }
}
