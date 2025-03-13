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
using ApplicationMetrics.MetricLoggers;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Models.Options.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Models.Options.ErrorHandlingOptions class.
    /// </summary>
    public class ErrorHandlingOptionsTests
    {
        protected ErrorHandlingOptions testErrorHandlingOptions;

        [SetUp]
        protected void SetUp()
        {
            testErrorHandlingOptions = new ErrorHandlingOptions
            {
                IncludeInnerExceptions = true,
                OverrideInternalServerErrors = true,
                InternalServerErrorMessageOverride = "An internal server error occurred"
            };
        }

        [Test]
        public void Validate_IncludeInnerExceptionsNull()
        {
            testErrorHandlingOptions.IncludeInnerExceptions = null;
            var validationContext = new ValidationContext(testErrorHandlingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testErrorHandlingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ErrorHandling options.  Configuration for 'IncludeInnerExceptions' is required."));
        }

        [Test]
        public void Validate_OverrideInternalServerErrorsNull()
        {
            testErrorHandlingOptions.OverrideInternalServerErrors = null;
            var validationContext = new ValidationContext(testErrorHandlingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testErrorHandlingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ErrorHandling options.  Configuration for 'OverrideInternalServerErrors' is required."));
        }

        [Test]
        public void Validate_InternalServerErrorMessageOverrideNull()
        {
            testErrorHandlingOptions.InternalServerErrorMessageOverride = null;
            var validationContext = new ValidationContext(testErrorHandlingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testErrorHandlingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ErrorHandling options.  Configuration for 'InternalServerErrorMessageOverride' is required."));
        }
    }
}
