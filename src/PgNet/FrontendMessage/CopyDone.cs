using System;

namespace PgNet.FrontendMessage
{
    internal readonly struct CopyDone : IKnownFrontendMessage
    {
        private static readonly ReadOnlyMemory<byte> s_message = new ReadOnlyMemory<byte>(new[]
        {
            FrontendMessageCode.CopyDone, (byte)0, (byte)0, (byte)0, (byte)4
        });

        public ReadOnlyMemory<byte> GetMessage()
        {
            return s_message;
        }
    }
}
