using System;
using System.Buffers;
using System.Text;

namespace PgNet.BackendMessage
{
    internal readonly struct CommandComplete
    {
        public readonly byte MessageType;
        public readonly int Length;
        public readonly string Tag;

        public CommandComplete(ReadOnlyMemory<byte> bytes)
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
            reader.TryRead(out MessageType);
            reader.TryReadBigEndian(out Length);
            var encoding = Encoding.UTF8;
            Tag = reader.ReadNullTerminateString(encoding);
        }
    }
}
