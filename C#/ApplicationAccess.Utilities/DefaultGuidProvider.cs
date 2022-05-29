using System;

namespace ApplicationAccess.Utilities
{
    /// <summary>
    /// Returns new random Guids using the static Guid struct.
    /// </summary>
    public class DefaultGuidProvider : IGuidProvider
    {
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Utilities.IGuidProvider.NewGuid"]/*'/>
        public Guid NewGuid()
        {
            return Guid.NewGuid();
        }
    }
}
