/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Validation
{
    /// <summary>
    /// Container class which represents the result of a validation operation.
    /// </summary>
    public class ValidationResult
    {
        /// <summary>Whether or not the validation operation resulted in success.</summary>
        protected Boolean successful;
        /// <summary>Describes the reason for failure in the case the validation operation was not successful.</summary>
        protected String message;
        /// <summary>An optional exception, providing further detail for the reason for failure in the case the validation operation was not successful.  Null if not set.</summary>
        protected Exception validationException;

        /// <summary>
        /// Whether or not the validation operation resulted in success.
        /// </summary>
        public Boolean Successful
        {
            get { return successful; }
        }

        /// <summary>
        /// Describes the reason for failure in the case the validation operation was not successful.
        /// </summary>
        public String Message
        {
            get { return message; }
        }

        /// <summary>
        /// An optional exception, providing further detail for the reason for failure in the case the validation operation was not successful.
        /// </summary>
        public Exception ValidationException
        {
            get { return validationException; }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Validation.ValidationResult class.
        /// </summary>
        /// <param name="successful">Whether or not the validation operation resulted in success.  Should be set true for this constructor overload.</param>
        public ValidationResult(Boolean successful)
        {
            if (successful == false)
                throw new ArgumentException($"If parameter '{nameof(successful)}' is set false, a 'message' parameter must be specified.", nameof(successful));

            this.successful = successful;
            message = "";
            validationException = null;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Validation.ValidationResult class.
        /// </summary>
        /// <param name="successful">Whether or not the validation operation resulted in success.  Should be set false for this constructor overload.</param>
        /// <param name="message">Describes the reason for failure of the validation operation.</param>
        public ValidationResult(Boolean successful, String message)
        {
            if (successful == true)
                throw new ArgumentException($"If parameter '{nameof(successful)}' is set true, a message parameter cannot specified.", nameof(successful));
            ValidateMessageParameter(message, nameof(message));

            this.successful = successful;
            this.message = message;
            validationException = null;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Validation.ValidationResult class.
        /// </summary>
        /// <param name="successful">Whether or not the validation operation resulted in success.  Should be set false for this constructor overload.</param>
        /// <param name="message">Describes the reason for failure of the validation operation.</param>
        /// <param name="validationException">An exception providing further detail for the reason for the failure of the validation operation.</param>
        public ValidationResult(Boolean successful, String message, Exception validationException)
        {
            if (successful == true)
                throw new ArgumentException($"If parameter '{nameof(successful)}' is set true, a message parameter cannot specified.", nameof(successful));
            ValidateMessageParameter(message, nameof(message));
            if (validationException == null)
                throw new ArgumentNullException(nameof(validationException), $"Parameter '{nameof(validationException)}' cannot be null.");

            this.successful = successful;
            this.message = message;
            this.validationException = validationException;
        }

        #pragma warning disable 1591

        protected void ValidateMessageParameter(String message, String messageParameterName)
        {
            if (String.IsNullOrWhiteSpace(message) == true)
                throw new ArgumentException($"Parameter '{messageParameterName}' must contain a value.", messageParameterName);
        }

        #pragma warning restore 1591
    }
}
