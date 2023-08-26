/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
 * 
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 * 
 *     http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System;

namespace ApplicationAccess
{
    /// <summary>
    /// Container class which holds an application component and a level of access of that component.
    /// </summary>
    /// <typeparam name="TComponent">The type of the application component.</typeparam>
    /// <typeparam name="TAccess">The type of the access level.</typeparam>
    public class ApplicationComponentAndAccessLevel<TComponent, TAccess> : IEquatable<ApplicationComponentAndAccessLevel<TComponent, TAccess>>
    {
        #pragma warning disable 1591

        protected static readonly Int32 prime1 = 7;
        protected static readonly Int32 prime2 = 11;

        protected TComponent applicationComponent;
        protected TAccess accessLevel;

        /// <summary>
        /// The application component.
        /// </summary>
        public TComponent ApplicationComponent
        {
            get { return applicationComponent; }
        }

        /// <summary>
        /// The level of access.
        /// </summary>
        public TAccess AccessLevel
        {
            get { return accessLevel; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.AccessManager+ApplicationComponentAndAccessLevel class.
        /// </summary>
        /// <param name="applicationComponent">The application component.</param>
        /// <param name="accessLevel">The level of access.</param>
        public ApplicationComponentAndAccessLevel(TComponent applicationComponent, TAccess accessLevel)
        {
            this.applicationComponent = applicationComponent;
            this.accessLevel = accessLevel;
        }

        /// <inheritdoc/>
        public Boolean Equals(ApplicationComponentAndAccessLevel<TComponent, TAccess> other)
        {
            return (this.applicationComponent.Equals(other.applicationComponent) && this.accessLevel.Equals(other.accessLevel));
        }

        /// <inheritdoc/>
        public override Int32 GetHashCode()
        {
            return (prime1 * applicationComponent.GetHashCode() + prime2 * accessLevel.GetHashCode());
        }

        #pragma warning restore 1591
    }
}
