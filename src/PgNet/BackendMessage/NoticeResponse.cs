using System;
using System.Buffers;

namespace PgNet.BackendMessage
{
    internal readonly struct NoticeResponse
    {
        public readonly byte MessageType;
        public readonly int Length;
        public readonly ErrorOrNoticeResponseField[] Fields;

        public NoticeResponse(ReadOnlyMemory<byte> bytes, ArrayPool<ErrorOrNoticeResponseField> arrayPool)
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
            reader.TryRead(out MessageType);
            reader.TryReadBigEndian(out Length);

            if (Length > sizeof(byte) + sizeof(int) + sizeof(byte))
            {
                using (var list = new ValueListBuilder<ErrorOrNoticeResponseField>(Span<ErrorOrNoticeResponseField>.Empty, arrayPool))
                {
                    while (true)
                    {
                        var field = new ErrorOrNoticeResponseField(ref reader);
                        if (field.Type == FieldCodes.Termination)
                            break;
                        list.Append(field);
                    }

                    Fields = list.AsSpan().ToArray();
                }
            }
            else
            {
                Fields = Array.Empty<ErrorOrNoticeResponseField>();
            }
        }
    }
}
