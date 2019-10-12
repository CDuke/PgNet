using System;
using System.Buffers;

namespace PgNet
{
    internal readonly struct ReadyForQuery
    {
        public readonly byte MessageType;
        public readonly byte TransactionStatus;
        public readonly int Length;
        
        public ReadyForQuery(ReadOnlyMemory<byte> bytes)
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
            reader.TryRead(out MessageType);
            reader.TryReadBigEndian(out Length);
            reader.TryRead(out TransactionStatus);
        }
    }
}
