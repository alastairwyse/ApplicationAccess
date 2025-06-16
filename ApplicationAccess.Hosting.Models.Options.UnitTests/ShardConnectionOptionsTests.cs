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
    /// Unit tests for the ApplicationAccess.Hosting.Models.Options.ShardConnectionOptions class.
    /// </summary>
    public class ShardConnectionOptionsTests
    {
        protected ShardConnectionOptions testShardConnectionOptions;

        [SetUp]
        protected void SetUp()
        {
            testShardConnectionOptions = new ShardConnectionOptions
            {
                RetryCount = 10,
                RetryInterval = 7, 
                ConnectionTimeout = 300_000
            };
        }

        [Test]
        public void Validate_RetryCountNull()
        {
            testShardConnectionOptions.RetryCount = null;
            var validationContext = new ValidationContext(testShardConnectionOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardConnectionOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardConnection options.  Configuration for 'RetryCount' is required."));
        }

        [Test]
        public void Validate_RetryCount0()
        {
            testShardConnectionOptions.RetryCount = 0;
            var validationContext = new ValidationContext(testShardConnectionOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardConnectionOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardConnection options.  Value for 'RetryCount' must be between 1 and 2147483647."));
        }

        [Test]
        public void Validate_RetryIntervalNull()
        {
            testShardConnectionOptions.RetryInterval = null;
            var validationContext = new ValidationContext(testShardConnectionOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardConnectionOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardConnection options.  Configuration for 'RetryInterval' is required."));
        }

        [Test]
        public void Validate_RetryInterval0()
        {
            testShardConnectionOptions.RetryInterval = 0;
            var validationContext = new ValidationContext(testShardConnectionOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardConnectionOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardConnection options.  Value for 'RetryInterval' must be between 1 and 2147483647."));
        }

        [Test]
        public void Validate_ConnectionTimeoutNull()
        {
            testShardConnectionOptions.ConnectionTimeout = null;
            var validationContext = new ValidationContext(testShardConnectionOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardConnectionOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardConnection options.  Configuration for 'ConnectionTimeout' is required."));
        }

        [Test]
        public void Validate_ConnectionTimeout0()
        {
            testShardConnectionOptions.ConnectionTimeout = 0;
            var validationContext = new ValidationContext(testShardConnectionOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardConnectionOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardConnection options.  Value for 'ConnectionTimeout' must be between 1 and 2147483647."));
        }
    }
}
