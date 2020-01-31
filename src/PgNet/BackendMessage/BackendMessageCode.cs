namespace PgNet.BackendMessage
{
    internal static class BackendMessageCode
    {
        internal const byte AuthenticationRequest = (byte)'R';
        internal const byte BackendKeyData = (byte)'K';
        internal const byte BindComplete = (byte)'2';
        internal const byte CloseComplete = (byte)'3';
        internal const byte CompletedResponse = (byte)'C';
        internal const byte CopyData = (byte)'d';
        internal const byte CopyDone = (byte)'c';
        internal const byte CopyBothResponse = (byte)'W';
        internal const byte CopyInResponse = (byte)'G';
        internal const byte CopyOutResponse = (byte)'H';
        internal const byte DataRow = (byte)'D';
        internal const byte EmptyQueryResponse = (byte)'I';
        internal const byte ErrorResponse = (byte)'E';
        internal const byte FunctionCall = (byte)'F';
        internal const byte FunctionCallResponse = (byte)'V';
        internal const byte NoData = (byte)'n';
        internal const byte NoticeResponse = (byte)'N';
        internal const byte NotificationResponse = (byte)'A';
        internal const byte ParameterDescription = (byte)'t';
        internal const byte ParameterStatus = (byte)'S';
        internal const byte ParseComplete = (byte)'1';
        internal const byte PasswordPacket = (byte)' ';
        internal const byte PortalSuspended = (byte)'s';
        internal const byte ReadyForQuery = (byte)'Z';
        internal const byte RowDescription = (byte)'T';
    }
}
