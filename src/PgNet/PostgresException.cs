using System;

namespace PgNet
{
    /// <summary>
    /// Base PostgreSQL exception
    /// </summary>
    public class PostgresException : Exception
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresException" /> class.
        /// </summary>
        public PostgresException()
        {
            
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public PostgresException(string message) : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresException" /> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public PostgresException(string message, Exception innerException) : base(message, innerException)
        {
            
        }
    }

    /// <summary>
    /// Unexpected backend message
    /// </summary>
    public class UnexpectedBackendMessageException : PostgresException
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresException" /> class.
        /// </summary>
        public UnexpectedBackendMessageException()
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="UnexpectedBackendMessageException" /> class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public UnexpectedBackendMessageException(string message) : base(message)
        {

        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PostgresException" /> class.
        /// </summary>
        /// <param name="message">The error message that explains the reason for the exception.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
        public UnexpectedBackendMessageException(string message, Exception innerException) : base(message, innerException)
        {

        }
    }
}
