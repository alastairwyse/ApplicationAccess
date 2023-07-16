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
using NUnit.Framework;

namespace ApplicationAccess.Hosting.LaunchPreparer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.LaunchPreparer.Base64StringEncoder class.
    /// </summary>
    public class Base64StringEncoderTests
    {
        private Base64StringEncoder testBase64StringEncoder;

        [SetUp]
        protected void SetUp()
        {
            testBase64StringEncoder = new Base64StringEncoder();
        }

        [Test]
        public void EncodeDecode() 
        {
            String testInputString = @"
            {
              ""AllowedHosts"": ""*"",
              ""AccessManager"": {
                ""StoreBidirectionalMappings"": true
              },
              ""AccessManagerSqlServerConnection"": {
                ""DataSource"": ""118.182.2.139"",
                ""InitialCatalog"": ""ApplicationAccess"",
                ""UserId"": ""sa"",
                ""Password"": ""mypassword"",
                ""RetryCount"": 10,
                ""RetryInterval"": 20,
                ""OperationTimeout"": 0
              },
              ""EventBufferFlushing"": {
                ""BufferSizeLimit"": 50,
                ""FlushLoopInterval"": 30000
              },
              ""MetricLogging"": {
                ""MetricLoggingEnabled"": false,
                ""MetricCategorySuffix"": """",
                ""MetricBufferProcessing"": {
                  ""BufferProcessingStrategy"": ""SizeLimitedLoopingWorkerThreadHybridBufferProcessor"",
                  ""BufferSizeLimit"": 500,
                  ""DequeueOperationLoopInterval"": 30000
                },
                ""MetricsSqlServerConnection"": {
                  ""DataSource"": ""118.182.2.139"",
                  ""InitialCatalog"": ""ApplicationMetrics"",
                  ""UserId"": ""sa"",
                  ""Password"": ""mypassword"",
                  ""RetryCount"": 10,
                  ""RetryInterval"": 20,
                  ""OperationTimeout"": 0
                }
              },
              ""ErrorHandling"": {
                ""IncludeInnerExceptions"": false,
                ""OverrideInternalServerErrors"": true,
                ""InternalServerErrorMessageOverride"": ""An internal server error occurred""
              }
            }
            ";

            String encodeResult = testBase64StringEncoder.Encode(testInputString);
            String decodeResult = testBase64StringEncoder.Decode(encodeResult);

            Assert.AreEqual(testInputString, decodeResult);
        }
    }
}
