using System;

namespace PgNet.FrontendMessage
{
    internal interface IFrontendMessageWriter
    {
        int CalculateLength();

        void Write(Memory<byte> buffer);
    }
}
