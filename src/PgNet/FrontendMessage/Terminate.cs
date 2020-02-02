using System;

namespace PgNet.FrontendMessage
{
    internal readonly struct Terminate : IKnownFrontendMessage
    {
        private static readonly ReadOnlyMemory<byte> s_message = new[]
        {
            FrontendMessageCode.Terminate, (byte)0, (byte)0, (byte)0, (byte)4
        };

        public ReadOnlyMemory<byte> GetMessage() => s_message;
    }
}
