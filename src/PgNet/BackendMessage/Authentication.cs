using System;
using System.Buffers;

namespace PgNet.BackendMessage
{
    internal readonly struct Authentication
    {
        private static readonly ReadOnlyMemory<byte> s_okResponse
            = new [] {(byte)82, (byte)0, (byte)0, (byte)0, (byte)8,
                (byte)0, (byte)0, (byte)0, (byte)0 };

        public const int OkResponseLength = 9; //s_okResponse.Length

        public readonly byte MessageType;
        public readonly int Length;
        public readonly AuthenticationRequestType AuthenticationRequestType;
        public readonly ReadOnlyMemory<byte> AdditionalInfo;

        public Authentication(ReadOnlyMemory<byte> bytes)
        {
            var reader = new SequenceReader<byte>(new ReadOnlySequence<byte>(bytes));
            reader.TryRead(out MessageType);
            reader.TryReadBigEndian(out Length);
            reader.TryReadBigEndian(out int authenticationRequestType);
            AuthenticationRequestType = (AuthenticationRequestType)authenticationRequestType;
            AdditionalInfo = bytes.Slice(9);
        }

        public static bool IsOk(ReadOnlyMemory<byte> bytes)
        {
            return bytes.Length >= OkResponseLength
                   && bytes.Slice(0, OkResponseLength).Span.SequenceEqual(s_okResponse.Span);
        }
    }
}
