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
using System.ComponentModel.DataAnnotations;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Models.Options;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Validates an <see cref="EventCacheConnectionOptions"/> instance.
    /// </summary>
    public class EventCacheConnectionOptionsValidator : OptionsValidatorBase
    {
        /// <inheritdoc/>
        protected override String OptionsName
        {
            get { return EventCacheConnectionOptions.EventCacheConnectionOptionsName; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.EventCacheConnectionOptionsValidator class.
        /// </summary>
        public EventCacheConnectionOptionsValidator()
            : base()
        {
        }

        /// <summary>
        /// Validates a <see cref="EventCacheConnectionOptions"/> instance.
        /// </summary>
        /// <param name="eventCacheConnectionOptions">The <see cref="EventCacheConnectionOptions"/> instance to validate.</param>
        public void Validate(EventCacheConnectionOptions eventCacheConnectionOptions)
        {
            var validationContext = new ValidationContext(eventCacheConnectionOptions);
            Validator.ValidateObject(eventCacheConnectionOptions, validationContext, true);

            if (Uri.TryCreate(eventCacheConnectionOptions.Host, new UriCreationOptions { DangerousDisablePathAndQueryCanonicalization = false }, out Uri parsedHost) == false)
                throw new ValidationException($"{GenerateExceptionMessagePrefix()}  Configuration for '{nameof(eventCacheConnectionOptions.Host)}' could not be parsed as a URI.");

            if (eventCacheConnectionOptions.Protocol == Protocol.Rest)
            {
                if (eventCacheConnectionOptions.RetryCount == null)
                    throw new ValidationException($"{GenerateExceptionMessagePrefix()}  Configuration for '{nameof(eventCacheConnectionOptions.RetryCount)}' is required when '{nameof(eventCacheConnectionOptions.Protocol)}' is set to '{Protocol.Rest}'.");

                if (eventCacheConnectionOptions.RetryInterval == null)
                    throw new ValidationException($"{GenerateExceptionMessagePrefix()}  Configuration for '{nameof(eventCacheConnectionOptions.RetryInterval)}' is required when '{nameof(eventCacheConnectionOptions.Protocol)}' is set to '{Protocol.Rest}'.");
            }
        }
    }
}
