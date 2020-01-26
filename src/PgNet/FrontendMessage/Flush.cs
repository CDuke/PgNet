using System;

namespace PgNet.FrontendMessage
{
    internal readonly struct Flush : IKnownFrontendMessage
    {
        private static readonly Memory<byte> s_message = new Memory<byte>(new[]
        {
            FrontendMessageCode.Flush, (byte)0, (byte)0, (byte)0, (byte)4
        });

        public ReadOnlyMemory<byte> GetMessage()
        {
            return s_message;
        }
    }
}
