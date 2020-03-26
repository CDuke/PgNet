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

        public ValueTask<MessageRef> MoveNextAsync(MessageRef previousMessage, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new ValueTask<MessageRef>(Task.FromCanceled<MessageRef>(cancellationToken));
            }

            var messageLength = previousMessage.MessageLength;
            var contentLength = previousMessage.ContentLength - messageLength;
            var messageStartIndex = previousMessage.StartIndex;

            if (contentLength > 0)
            {
                return new ValueTask<MessageRef>(CreateMessageRef(messageStartIndex, messageLength, contentLength, m_receiveBuffer));
            }

            if (previousMessage.IsReadyForQuery)
            {
                return new ValueTask<MessageRef>(MessageRef.Empty);
            }

            var receiveTask = m_receiver.ReceiveAsync(m_receiveBuffer, cancellationToken);
            if (receiveTask.IsCompletedSuccessfully)
            {
                contentLength = receiveTask.Result;
                if (contentLength == 0)
                {
                    return new ValueTask<MessageRef>(MessageRef.Empty);
                }
                messageStartIndex = -messageLength;

                return new ValueTask<MessageRef>(CreateMessageRef(messageStartIndex, messageLength, contentLength, m_receiveBuffer));

            }

            return Awaited(receiveTask, messageLength, m_receiveBuffer);

            static async ValueTask<MessageRef> Awaited(ValueTask<int> task, int mLength, Memory<byte> buffer)
            {
                var cLength = await task.ConfigureAwait(false);
                if (cLength == 0)
                {
                    return MessageRef.Empty;
                }
                var startIndex = -mLength;

                return CreateMessageRef(startIndex, mLength, cLength, buffer);
            }
        }

        private static MessageRef CreateMessageRef(int messageStartIndex, int messageLength, int contentLength, Memory<byte> receiveBuffer)
        {
            if (contentLength <= sizeof(byte) + sizeof(int))
            {
                ThrowHelper.ThrowNotImplementedException();
            }

            messageStartIndex += messageLength;

            var messageType = receiveBuffer.Span[messageStartIndex];
            messageLength = 1 + BinaryPrimitives.ReadInt32BigEndian(receiveBuffer.Span.Slice(messageStartIndex + 1));

            return new MessageRef(messageType, messageStartIndex, messageLength, contentLength);
        }

        public CommandComplete ReadCommandComplete(MessageRef messageRef) => new CommandComplete(SliceMessage(messageRef));

        public ReadyForQuery ReadReadyForQuery(MessageRef messageRef) => new ReadyForQuery(SliceMessage(messageRef));

        public RowDescription ReadRowDescription(MessageRef messageRef) => new RowDescription(SliceMessage(messageRef));

        public DataRow ReadDataRow(MessageRef messageRef) => new DataRow(SliceMessage(messageRef));

        public ErrorResponse ReadErrorResponse(MessageRef messageRef) => new ErrorResponse(SliceMessage(messageRef), ArrayPool<ErrorOrNoticeResponseField>.Shared);

        public NoticeResponse ReadNoticeResponse(MessageRef messageRef) => new NoticeResponse(SliceMessage(messageRef), ArrayPool<ErrorOrNoticeResponseField>.Shared);

        public Authentication ReadAuthentication(MessageRef messageRef) => new Authentication(SliceMessage(messageRef));

        public bool IsAuthenticationOK(MessageRef messageRef) => Authentication.IsOk(SliceMessage(messageRef));

        public BackendKeyData ReadBackendKeyData(MessageRef messageRef) => new BackendKeyData(SliceMessage(messageRef));

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
