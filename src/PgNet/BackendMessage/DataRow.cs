using System;
using System.Buffers;

namespace PgNet.BackendMessage
{
    internal readonly struct DataRow
    {
        public readonly byte MessageType;
        public readonly int Length;
        public readonly short ColumnsCount;
        public readonly ReadOnlyMemory<byte>[] Cells;

        public DataRow(ReadOnlyMemory<byte> bytes)
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
            _ = reader.TryRead(out MessageType);
            _ = reader.TryReadBigEndian(out Length);
            _ = reader.TryReadBigEndian(out ColumnsCount);

            if (ColumnsCount > 0)
            {
                Cells = new ReadOnlyMemory<byte>[ColumnsCount];
                for (var i = 0; i < Cells.Length; i++)
                {
                    ReadCell(ref reader, out var start, out var length);
                    Cells[i] = bytes.Slice(start, length);
                }
            }
            else
            {
                Cells = Array.Empty<ReadOnlyMemory<byte>>();
            }
        }

        private static void ReadCell(ref SequenceReader<byte> reader, out int start, out int length)
        {
            _ = reader.TryReadBigEndian(out length);
            start = (int)reader.Consumed;
        }

    }

}
