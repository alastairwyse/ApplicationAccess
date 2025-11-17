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
using ApplicationAccess.Hosting.Models.Options;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.EventCacheConnectionOptionsValidator class.
    /// </summary>
    public class EventCacheConnectionOptionsValidatorTests
    {
        private EventCacheConnectionOptionsValidator testEventCacheConnectionOptionsValidator;

        [SetUp]
        protected void SetUp()
        {
            testEventCacheConnectionOptionsValidator = new EventCacheConnectionOptionsValidator();
        }

        [Test]
        public void Validate_HostNull()
        {
            var testEventCacheConnectionOptions = new EventCacheConnectionOptions();

            var e = Assert.Throws<ValidationException>(delegate
            {
                testEventCacheConnectionOptionsValidator.Validate(testEventCacheConnectionOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating EventCacheConnection options.  Configuration for 'Host' is required."));
        }

        [Test]
        public void Validate_HostInvalid()
        {
            var testEventCacheConnectionOptions = new EventCacheConnectionOptions();
            testEventCacheConnectionOptions.Host = "invalid";

            var e = Assert.Throws<ValidationException>(delegate
            {
                testEventCacheConnectionOptionsValidator.Validate(testEventCacheConnectionOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating EventCacheConnection options.  Configuration for 'Host' could not be parsed as a URI."));
        }

        [Test]
        public void Validate_ProtocolSetToRestAndRetryCountNull()
        {
            var testEventCacheConnectionOptions = new EventCacheConnectionOptions();
            testEventCacheConnectionOptions.Host = "http://127.0.0.1:5000/";
            testEventCacheConnectionOptions.RetryCount = null;
            testEventCacheConnectionOptions.RetryInterval = null;

            var e = Assert.Throws<ValidationException>(delegate
            {
                testEventCacheConnectionOptionsValidator.Validate(testEventCacheConnectionOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating EventCacheConnection options.  Configuration for 'RetryCount' is required when 'Protocol' is set to 'Rest'."));
        }

        [Test]
        public void Validate_ProtocolSetToRestAndRetryIntervalNull()
        {
            var testEventCacheConnectionOptions = new EventCacheConnectionOptions();
            testEventCacheConnectionOptions.Host = "http://127.0.0.1:5000/";
            testEventCacheConnectionOptions.RetryCount = 5;
            testEventCacheConnectionOptions.RetryInterval = null;

            var e = Assert.Throws<ValidationException>(delegate
            {
                testEventCacheConnectionOptionsValidator.Validate(testEventCacheConnectionOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating EventCacheConnection options.  Configuration for 'RetryInterval' is required when 'Protocol' is set to 'Rest'."));
        }

        [Test]
        public void Validate_ProtocolSetToRestAndRetryCountInvalid()
        {
            var testEventCacheConnectionOptions = new EventCacheConnectionOptions();
            testEventCacheConnectionOptions.Host = "http://127.0.0.1:5000/";
            testEventCacheConnectionOptions.RetryCount = 0;
            testEventCacheConnectionOptions.RetryInterval = 1;

            var e = Assert.Throws<ValidationException>(delegate
            {
                testEventCacheConnectionOptionsValidator.Validate(testEventCacheConnectionOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating EventCacheConnection options.  Value for 'RetryCount' must be between 1 and 2147483647."));
        }

        [Test]
        public void Validate_ProtocolSetToRestAndRetryIntervalInvalid()
        {
            var testEventCacheConnectionOptions = new EventCacheConnectionOptions();
            testEventCacheConnectionOptions.Host = "http://127.0.0.1:5000/";
            testEventCacheConnectionOptions.RetryCount = 1;
            testEventCacheConnectionOptions.RetryInterval = 0;

            var e = Assert.Throws<ValidationException>(delegate
            {
                testEventCacheConnectionOptionsValidator.Validate(testEventCacheConnectionOptions);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating EventCacheConnection options.  Value for 'RetryInterval' must be between 1 and 2147483647."));
        }
    }
}
