/*
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
using ApplicationAccess.Distribution.Serialization;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using NUnit.Framework;

namespace ApplicationAccess.Distribution.Serialization.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Distribution.Serialization.AccessManagerRestClientConfigurationJsonSerializer class.
    /// </summary>
    public class AccessManagerRestClientConfigurationJsonSerializerTests
    {
        private AccessManagerRestClientConfigurationJsonSerializer testJsonSerializer;

        [SetUp]
        protected void SetUp()
        {
            testJsonSerializer = new AccessManagerRestClientConfigurationJsonSerializer();
        }

        [Test]
        public void SerializeDeserialize()
        {
            var testClientBaseUrl = new Uri("https://127.0.0.1:5170/");
            var testClientConfiguration = new AccessManagerRestClientConfiguration(testClientBaseUrl);

            String serializedClientConfiguration = testJsonSerializer.Serialize(testClientConfiguration);
            AccessManagerRestClientConfiguration resultClientConfiguration = testJsonSerializer.Deserialize(serializedClientConfiguration);

            Assert.AreEqual(testClientBaseUrl, resultClientConfiguration.BaseUrl);
        }

        [Test]
        public void Deserialize_SerializedClientConfigurationParameterNotJson()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testJsonSerializer.Deserialize("Not JSON");
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'serializedClientConfiguration' could not be parsed as JSON."));
            Assert.AreEqual("serializedClientConfiguration", e.ParamName);
        }

        [Test]
        public void Deserialize_SerializedClientConfigurationParameterDoesntContainBaseUrlProperty()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testJsonSerializer.Deserialize(@"{ ""EntityType"" : ""ClientAccount"" }");
            });

            Assert.That(e.Message, Does.StartWith("JSON document in parameter 'serializedClientConfiguration' does not contain a 'baseUrl' property."));
            Assert.AreEqual("serializedClientConfiguration", e.ParamName);
        }
    }
}
