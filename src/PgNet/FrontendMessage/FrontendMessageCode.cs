namespace PgNet.FrontendMessage
{
    internal static class FrontendMessageCode
    {
        internal const byte Describe = (byte)'D';
        internal const byte Sync = (byte)'S';
        internal const byte Execute = (byte)'E';
        internal const byte Parse = (byte)'P';
        internal const byte Bind = (byte)'B';
        internal const byte Close = (byte)'C';
        internal const byte Query = (byte)'Q';
        internal const byte CopyDone = (byte)'c';
        internal const byte CopyFail = (byte)'f';
        internal const byte Terminate = (byte)'X';
        internal const byte Password = (byte)'p';
        internal const byte Flush = (byte)'H';
    }
}
