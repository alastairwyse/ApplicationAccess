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
    /// Unit tests for the ApplicationAccess.Hosting.Models.Options.MetricBufferProcessingOptions class.
    /// </summary>
    public class MetricBufferProcessingOptionsTests
    {
        protected MetricBufferProcessingOptions testMetricBufferProcessingOptions;

        [SetUp]
        protected void SetUp()
        {
            testMetricBufferProcessingOptions = new MetricBufferProcessingOptions
            {
                BufferProcessingStrategy = MetricBufferProcessingStrategyImplementation.SizeLimitedBufferProcessor,
                BufferSizeLimit = 500,
                DequeueOperationLoopInterval = 30_000, 
            };
        }

        [Test]
        public void Validate_BufferProcessingStrategyNull()
        {
            testMetricBufferProcessingOptions.BufferProcessingStrategy = null;
            var validationContext = new ValidationContext(testMetricBufferProcessingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testMetricBufferProcessingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating MetricBufferProcessing options.  Configuration for 'BufferProcessingStrategy' is required."));
        }

        [Test]
        public void Validate_BufferSizeLimitNull()
        {
            testMetricBufferProcessingOptions.BufferSizeLimit = null;
            var validationContext = new ValidationContext(testMetricBufferProcessingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testMetricBufferProcessingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating MetricBufferProcessing options.  Configuration for 'BufferSizeLimit' is required."));
        }

        [Test]
        public void Validate_BufferSizeLimit0()
        {
            testMetricBufferProcessingOptions.BufferSizeLimit = 0;
            var validationContext = new ValidationContext(testMetricBufferProcessingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testMetricBufferProcessingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating MetricBufferProcessing options.  Value for 'BufferSizeLimit' must be between 1 and 2147483647."));
        }

        [Test]
        public void Validate_DequeueOperationLoopIntervalNull()
        {
            testMetricBufferProcessingOptions.DequeueOperationLoopInterval = null;
            var validationContext = new ValidationContext(testMetricBufferProcessingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testMetricBufferProcessingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating MetricBufferProcessing options.  Configuration for 'DequeueOperationLoopInterval' is required."));
        }

        [Test]
        public void Validate_DequeueOperationLoopInterval0()
        {
            testMetricBufferProcessingOptions.DequeueOperationLoopInterval = 0;
            var validationContext = new ValidationContext(testMetricBufferProcessingOptions);

            var e = Assert.Throws<ValidationException>(delegate
            {
                Validator.ValidateObject(testMetricBufferProcessingOptions, validationContext, true);
            });

            Assert.That(e.Message, Does.StartWith($"Error validating MetricBufferProcessing options.  Value for 'DequeueOperationLoopInterval' must be between 1 and 2147483647."));
        }
    }
}
