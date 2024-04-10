﻿/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Hosting.Rest.DistributedAsyncClient.IntegrationTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.DistributedAsyncClient.AccessManagerRestClientConfiguration class.
    /// </summary>
    public class AccessManagerRestClientConfigurationTests
    {
        private AccessManagerRestClientConfiguration testAccessManagerRestClientConfiguration;

        [SetUp]
        protected void SetUp()
        {
            testAccessManagerRestClientConfiguration = new AccessManagerRestClientConfiguration(new Uri("http://127.0.0.1:5001/"));
        }

        [Test]
        public void Description()
        {
            String result = testAccessManagerRestClientConfiguration.Description;

            Assert.AreEqual("AccessManagerRestClientConfiguration { BaseUrl = http://127.0.0.1:5001/ }", result);
        }
    }
}
