using System;

namespace ApplicationAccess.Utilities
{
    /// <summary>
    /// Returns new random Guids using the static Guid struct.
    /// </summary>
    public class DefaultGuidProvider : IGuidProvider
    {
        /// <inheritdoc/>
        public Guid NewGuid()
        {
            return Guid.NewGuid();
        }
    }
}
