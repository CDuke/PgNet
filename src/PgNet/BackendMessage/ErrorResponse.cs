using System;
using System.Buffers;
using System.Text;

namespace PgNet
{
    internal readonly struct ErrorResponse
    {
        public readonly byte MessageType;
        public readonly int Length;
        public readonly ErrorOrNoticeResponseField[] Fields;

        public ErrorResponse(ReadOnlyMemory<byte> bytes, ArrayPool<ErrorOrNoticeResponseField> arrayPool)
        {
            var encoding = Encoding.UTF8;
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
            reader.TryRead(out MessageType);
            reader.TryReadBigEndian(out Length);

            if (Length > sizeof(byte) + sizeof(int) + sizeof(byte))
            {
                using (var list = new ValueListBuilder<ErrorOrNoticeResponseField>(Span<ErrorOrNoticeResponseField>.Empty, arrayPool))
                {
                    while (true)
                    {
                        var field = new ErrorOrNoticeResponseField(ref reader, encoding);
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

    internal readonly struct ErrorOrNoticeResponseField
    {
        public readonly byte Type;
        public readonly string Value;

        public ErrorOrNoticeResponseField(ref SequenceReader<byte> reader, Encoding encoding)
        {
            reader.TryRead(out Type);
            Value = string.Empty;
            if (Type != FieldCodes.Termination)
            {
                if (Type == FieldCodes.Message)
                {
                    var win1251 = Encoding.GetEncoding("Windows-1251");
                    Value = reader.ReadNullTerminateString(win1251);
                }
                else
                {
                    Value = reader.ReadNullTerminateString(encoding);
                }
            }
        }
    }
}
