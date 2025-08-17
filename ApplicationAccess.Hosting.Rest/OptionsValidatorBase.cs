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
using Microsoft.Extensions.Options;
using ApplicationAccess.Hosting.Models.Options;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Base for classes which validate objects which follow the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options">Options pattern</see>.
    /// </summary>
    public abstract class OptionsValidatorBase
    {
        /// <summary>Contains utility method for Options classes.</summary>
        protected OptionsUtilities optionsUtilities;

        /// <summary>The name of the options within the application configuration.</summary>
        protected abstract String OptionsName { get; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.OptionsValidatorBase class.
        /// </summary>
        public OptionsValidatorBase()
        {
            optionsUtilities = new OptionsUtilities();
        }

        #region Private/Protected Methods

        #pragma warning disable 1591

        protected String GenerateExceptionMessagePrefix()
        {
            return optionsUtilities.GenerateValidationExceptionMessagePrefix(OptionsName);
        }

        #pragma warning restore 1591

        #endregion
    }
}
