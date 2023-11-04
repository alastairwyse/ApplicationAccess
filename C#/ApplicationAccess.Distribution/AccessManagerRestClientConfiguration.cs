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
using System.Collections.Generic;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Model/container class holding configuration required to instantiate an AccessManager REST client.
    /// </summary>
    public class AccessManagerRestClientConfiguration : IDistributedAccessManagerAsyncClientConfiguration, IEquatable<AccessManagerRestClientConfiguration>
    {
        /// <summary>The base URL for the hosted AccessManager.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</summary>
        public String BaseUrl { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.AccessManagerRestClientConfiguration class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted AccessManager.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        public AccessManagerRestClientConfiguration(String baseUrl)
        {
            if (String.IsNullOrWhiteSpace(baseUrl) == true)
                throw new ArgumentException($"Parameter '{nameof(baseUrl)}' must contain a value.", nameof(baseUrl));

            BaseUrl = baseUrl;
        }

        /// <inheritdoc/>
        public override Boolean Equals(Object obj)
        {
            if (!(obj is AccessManagerRestClientConfiguration))
            {
                return false;
            }
            return Equals((AccessManagerRestClientConfiguration)obj);
        }

        /// <inheritdoc/>
        public Boolean Equals(AccessManagerRestClientConfiguration other)
        {
            return BaseUrl == other.BaseUrl;
        }

        /// <inheritdoc/>
        public override Int32 GetHashCode()
        {
            return BaseUrl.GetHashCode();
        }
    }
}
