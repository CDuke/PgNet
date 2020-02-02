using System;
using System.Buffers;

namespace PgNet.BackendMessage
{
    internal readonly struct ParameterStatus
    {
        public readonly byte MessageType;
        public readonly int Length;
        public readonly string ParameterName;
        public readonly string ParameterValue;

        public ParameterStatus(ReadOnlyMemory<byte> bytes)
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
            reader.TryRead(out MessageType);
            reader.TryReadBigEndian(out Length);
            ParameterName = reader.ReadUtf8NullTerminateStringAsUtf16();
            ParameterValue = reader.ReadUtf8NullTerminateStringAsUtf16();
        }
    }
}
