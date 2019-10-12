namespace PgNet
{
    internal static class TransactionStatus
    {
        /// <summary>
        /// Idle (not in a transaction block).
        /// </summary>
        public const byte Idle = (byte)'I';

        /// <summary>
        /// In a transaction block.
        /// </summary>
        public const byte InTransactionBlock = (byte)'T';

        /// <summary>
        /// Failed transaction block (queries will be rejected until block is ended).
        /// </summary>
        public const byte Failed = (byte)'E';
    }
}
