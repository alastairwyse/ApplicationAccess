using System;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// The exception that is thrown when an event which changes the structure of an access manager is requested from a cache but not found.
    /// </summary>
    public class EventNotCachedException : Exception
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.EventNotCachedException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        public EventNotCachedException(String message)
            : base(message)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.EventNotCachedException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="innerException">The exception that is the cause of the current exception, or a null reference (Nothing in Visual Basic) if no inner exception is specified.</param>
        public EventNotCachedException(String message, Exception innerException)
            : base(message, innerException)
        {
        }
    }
}
