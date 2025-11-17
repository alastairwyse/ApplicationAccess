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
    /// Unit tests for the ApplicationAccess.Hosting.Models.Options.EventCacheConnectionOptions class.
    /// </summary>
    public class EventCacheConnectionOptionsTests
    {
        protected EventCacheConnectionOptions testEventCacheConnectionOptions;

        [SetUp]
        protected void SetUp()
        {
            testEventCacheConnectionOptions = new EventCacheConnectionOptions
            {
                Host = "http://127.0.0.1:5003", 
                RetryCount = 10, 
                RetryInterval = 5
            };
        }

        [Test]
        public void Validate_HostNull()
        {
            testEventCacheConnectionOptions.Host = null;
            var validationContext = new ValidationContext(testEventCacheConnectionOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testEventCacheConnectionOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating EventCacheConnection options.  Configuration for 'Host' is required."));
        }

        [Test]
        public void Validate_RetryCount0()
        {
            testEventCacheConnectionOptions.RetryCount = 0;
            var validationContext = new ValidationContext(testEventCacheConnectionOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testEventCacheConnectionOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating EventCacheConnection options.  Value for 'RetryCount' must be between 1 and 2147483647."));
        }

        [Test]
        public void Validate_RetryInterval0()
        {
            testEventCacheConnectionOptions.RetryInterval = 0;
            var validationContext = new ValidationContext(testEventCacheConnectionOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testEventCacheConnectionOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating EventCacheConnection options.  Value for 'RetryInterval' must be between 1 and 2147483647."));
        }
    }
}
