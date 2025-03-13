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
    /// Unit tests for the ApplicationAccess.Hosting.Models.Options.EventCacheRefreshOptions class.
    /// </summary>
    public class EventCacheRefreshOptionsTests
    {
        protected EventCacheRefreshOptions testEventCacheRefreshOptions;

        [SetUp]
        protected void SetUp()
        {
            testEventCacheRefreshOptions = new EventCacheRefreshOptions
            {
                RefreshInterval = 10000
            };
        }

        [Test]
        public void Validate_RefreshIntervalNull()
        {
            testEventCacheRefreshOptions.RefreshInterval = null;
            var validationContext = new ValidationContext(testEventCacheRefreshOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testEventCacheRefreshOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating EventCacheRefresh options.  Configuration for 'RefreshInterval' is required."));
        }

        [Test]
        public void Validate_RefreshInterval0()
        {
            testEventCacheRefreshOptions.RefreshInterval = 0;
            var validationContext = new ValidationContext(testEventCacheRefreshOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testEventCacheRefreshOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating EventCacheRefresh options.  Value for 'RefreshInterval' must be between 1 and 2147483647."));
        }
    }
}
