using System;
using System.Buffers;

namespace PgNet.BackendMessage
{
    internal readonly struct RowDescription
    {
        public readonly byte MessageType;
        public readonly int Length;
        public readonly short FieldCount;
        public readonly FieldDescription[] Fields;

        public RowDescription(ReadOnlyMemory<byte> bytes)
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
            reader.TryRead(out MessageType);
            reader.TryReadBigEndian(out Length);
            reader.TryReadBigEndian(out FieldCount);

            if (FieldCount > 0)
            {
                Fields = new FieldDescription[FieldCount];
                for (var i = 0; i < Fields.Length; i++)
                {
                    Fields[i] = new FieldDescription(ref reader);
                }
            }
            else
            {
                Fields = Array.Empty<FieldDescription>();
            }
        }
    }

    internal readonly struct FieldDescription
    {
        public readonly string Name;
        public readonly int TableId;
        public readonly short NumberOfColumn;
        public readonly int FieldDataTypeId;
        public readonly short DataTypeSize;
        public readonly int DataTypeModifier;
        public readonly short FormatCode;

        public FieldDescription(ref SequenceReader<byte> reader)
        {
            Name = reader.ReadUtf8NullTerminateStringAsUtf16();
            reader.TryReadBigEndian(out TableId);
            reader.TryReadBigEndian(out NumberOfColumn);
            reader.TryReadBigEndian(out FieldDataTypeId);
            reader.TryReadBigEndian(out DataTypeSize);
            reader.TryReadBigEndian(out DataTypeModifier);
            reader.TryReadBigEndian(out FormatCode);
        }
    }
}
