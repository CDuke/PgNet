namespace PgNet
{
    internal enum ConnectorState
    {
        Disconnected,

        Connecting,

        ReadyForQuery,

        Busy
    }
}
