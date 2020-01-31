using System;
using System.Buffers;

namespace PgNet.BackendMessage
{
    internal readonly struct BackendKeyData
    {
        public readonly byte MessageType;
        public readonly int Length;
        public readonly int ProcessId;
        public readonly int SecretKey;

        public BackendKeyData(ReadOnlyMemory<byte> bytes)
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
            reader.TryRead(out MessageType);
            reader.TryReadBigEndian(out Length);
            reader.TryReadBigEndian(out ProcessId);
            reader.TryReadBigEndian(out SecretKey);
        }
    }
}
