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
    /// Unit tests for the ApplicationAccess.Hosting.Models.Options.ShardRoutingOptions class.
    /// </summary>
    public class ShardRoutingOptionsTests
    {
        protected ShardRoutingOptions testShardRoutingOptions;

        [SetUp]
        protected void SetUp()
        {
            testShardRoutingOptions = new ShardRoutingOptions
            {
                DataElementType = Distribution.Models.DataElement.User,
                SourceQueryShardBaseUrl = "http://127.0.0.1:5003",
                SourceEventShardBaseUrl = "http://127.0.0.1:5004",
                SourceShardHashRangeStart = -2_147_483_648,
                SourceShardHashRangeEnd = 0,
                TargetQueryShardBaseUrl = "http://127.0.0.1:5005",
                TargetEventShardBaseUrl = "http://127.0.0.1:5006",
                TargetShardHashRangeStart = 1,
                TargetShardHashRangeEnd = 2_147_483_647,
                RoutingInitiallyOn = false
            };
        }

        [Test]
        public void Validate_DataElementTypeNull()
        {
            testShardRoutingOptions.DataElementType = null;
            var validationContext = new ValidationContext(testShardRoutingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardRoutingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardRouting options.  Configuration for 'DataElementType' is required."));
        }

        [Test]
        public void Validate_SourceQueryShardBaseUrlNull()
        {
            testShardRoutingOptions.SourceQueryShardBaseUrl = null;
            var validationContext = new ValidationContext(testShardRoutingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardRoutingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardRouting options.  Configuration for 'SourceQueryShardBaseUrl' is required."));
        }

        [Test]
        public void Validate_SourceEventShardBaseUrlNull()
        {
            testShardRoutingOptions.SourceEventShardBaseUrl = null;
            var validationContext = new ValidationContext(testShardRoutingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardRoutingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardRouting options.  Configuration for 'SourceEventShardBaseUrl' is required."));
        }

        [Test]
        public void Validate_SourceShardHashRangeStartNull()
        {
            testShardRoutingOptions.SourceShardHashRangeStart = null;
            var validationContext = new ValidationContext(testShardRoutingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardRoutingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardRouting options.  Configuration for 'SourceShardHashRangeStart' is required."));
        }

        [Test]
        public void Validate_SourceShardHashRangeEndNull()
        {
            testShardRoutingOptions.SourceShardHashRangeEnd = null;
            var validationContext = new ValidationContext(testShardRoutingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardRoutingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardRouting options.  Configuration for 'SourceShardHashRangeEnd' is required."));
        }

        [Test]
        public void Validate_TargetQueryShardBaseUrlNull()
        {
            testShardRoutingOptions.TargetQueryShardBaseUrl = null;
            var validationContext = new ValidationContext(testShardRoutingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardRoutingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardRouting options.  Configuration for 'TargetQueryShardBaseUrl' is required."));
        }

        [Test]
        public void Validate_TargetEventShardBaseUrlNull()
        {
            testShardRoutingOptions.TargetEventShardBaseUrl = null;
            var validationContext = new ValidationContext(testShardRoutingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardRoutingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardRouting options.  Configuration for 'TargetEventShardBaseUrl' is required."));
        }

        [Test]
        public void Validate_TargetShardHashRangeStartNull()
        {
            testShardRoutingOptions.TargetShardHashRangeStart = null;
            var validationContext = new ValidationContext(testShardRoutingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardRoutingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardRouting options.  Configuration for 'TargetShardHashRangeStart' is required."));
        }

        [Test]
        public void Validate_TargetShardHashRangeEndNull()
        {
            testShardRoutingOptions.TargetShardHashRangeEnd = null;
            var validationContext = new ValidationContext(testShardRoutingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardRoutingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardRouting options.  Configuration for 'TargetShardHashRangeEnd' is required."));
        }

        [Test]
        public void Validate_RoutingInitiallyOnNull()
        {
            testShardRoutingOptions.RoutingInitiallyOn = null;
            var validationContext = new ValidationContext(testShardRoutingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testShardRoutingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating ShardRouting options.  Configuration for 'RoutingInitiallyOn' is required."));
        }
    }
}
