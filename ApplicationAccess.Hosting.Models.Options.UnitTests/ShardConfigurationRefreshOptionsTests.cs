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
    /// Unit tests for the ApplicationAccess.Hosting.Models.Options.ShardConfigurationRefreshOptions class.
    /// </summary>
    public class ShardConfigurationRefreshOptionsTests
    {
        protected ShardConfigurationRefreshOptions testShardConfigurationRefreshOptions;

        [SetUp]
        protected void SetUp()
        {
            testShardConfigurationRefreshOptions = new ShardConfigurationRefreshOptions
            {
                RefreshInterval = 10_000
            };
        }

        [Test]
        public void Validate_RefreshIntervalNull()
        {
            testShardConfigurationRefreshOptions.RefreshInterval = null;
            var validationContext = new ValidationContext(testShardConfigurationRefreshOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardConfigurationRefreshOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardConfigurationRefresh options.  Configuration for 'RefreshInterval' is required."));
        }

        [Test]
        public void Validate_RefreshInterval0()
        {
            testShardConfigurationRefreshOptions.RefreshInterval = 0;
            var validationContext = new ValidationContext(testShardConfigurationRefreshOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardConfigurationRefreshOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardConfigurationRefresh options.  Value for 'RefreshInterval' must be between 1 and 2147483647."));
        }
    }
}
