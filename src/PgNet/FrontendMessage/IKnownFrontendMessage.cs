using System;

namespace PgNet.FrontendMessage
{
    internal interface IKnownFrontendMessage
    {
        ReadOnlyMemory<byte> GetMessage();
    }
}
