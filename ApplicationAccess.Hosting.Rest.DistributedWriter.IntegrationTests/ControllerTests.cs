﻿/*
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
using NUnit.Framework;
using NSubstitute;
using Microsoft.Extensions.DependencyInjection;


namespace ApplicationAccess.Hosting.Rest.DistributedWriter.IntegrationTests
{
    /// <summary>
    /// Tests controller methods implemented by the DistributedWriter node.
    /// </summary>
    public class ControllerTests : DistributedWriterIntegrationTestsBase
    {
        #region DistributedWriterController Methods

        [Test]
        public void FlushEventBuffers()
        {
            administratorClient.FlushEventBuffers();

            mockManuallyFlushableBufferFlushStrategy.Received(1).FlushBuffers();
        }

        [Test]
        public void GetActiveRequestCount()
        {
            testDistributedWriter.Services.GetService<RequestCounter>().Increment();
            testDistributedWriter.Services.GetService<RequestCounter>().Increment();

            Int32 result = administratorClient.GetEventProcessingCount();

            Assert.AreEqual(2, result);
        }

        #endregion
    }
}
