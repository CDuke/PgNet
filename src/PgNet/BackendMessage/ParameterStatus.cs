using System;
using System.Buffers;
using System.Text;

namespace PgNet.BackendMessage
{
    internal readonly struct ParameterStatus
    {
        private static readonly Encoding s_utf8Encoding = Encoding.UTF8;

        public readonly byte MessageType;
        public readonly int Length;
        public readonly string ParameterName;
        public readonly string ParameterValue;

        public ParameterStatus(ReadOnlyMemory<byte> bytes)
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
            reader.TryRead(out MessageType);
            reader.TryReadBigEndian(out Length);
            ParameterName = reader.ReadNullTerminateString(s_utf8Encoding);
            ParameterValue = reader.ReadNullTerminateString(s_utf8Encoding);
        }
    }
}
