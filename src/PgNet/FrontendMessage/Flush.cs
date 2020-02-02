using System;

namespace PgNet.FrontendMessage
{
    internal readonly struct Flush : IKnownFrontendMessage
    {
        private static readonly ReadOnlyMemory<byte> s_message = new[]
        {
            FrontendMessageCode.Flush, (byte)0, (byte)0, (byte)0, (byte)4
        };

        public ReadOnlyMemory<byte> GetMessage() => s_message;
    }
}
