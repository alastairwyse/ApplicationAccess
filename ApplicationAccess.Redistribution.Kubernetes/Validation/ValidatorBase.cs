/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Redistribution.Kubernetes.Validation
{
    /// <summary>
    /// Base for validator classes.
    /// </summary>
    public abstract class ValidatorBase
    {
        #pragma warning disable 1591

        protected void ThrowExceptionIfPropertyNull(String className, Object propertyValue, String propertyName)
        {
            if (propertyValue == null)
                throw new ArgumentNullException(propertyName, $"{className} property '{propertyName}' cannot be null.");
        }

        #pragma warning restore 1591
    }
}
