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
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

namespace ApplicationAccess.Hosting.LaunchPreparer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.LaunchPreparer.ArgumentValidatorConverter class.
    /// </summary>
    public class ArgumentValidatorConverterTests
    {
        private ArgumentValidatorConverter testArgumentValidatorConverter;

        [SetUp]
        protected void SetUp()
        {
            testArgumentValidatorConverter = new ArgumentValidatorConverter();
        }

        [Test]
        public void Convert_ArgumentNameParameterInvalid()
        {
            var e = Assert.Throws<Exception>(delegate
            {
                testArgumentValidatorConverter.Convert<Int32>("listenPor", "5001");
            });

            Assert.That(e.Message, Does.StartWith("Encountered unknown argument name 'listenPor'."));
        }

        [Test]
        public void Convert_TypeParameterInvalid()
        {
            var e = Assert.Throws<Exception>(delegate
            {
                testArgumentValidatorConverter.Convert<String>("listenPort", "5001");
            });

            Assert.That(e.Message, Does.StartWith("Generic parameter type 'String' is invalid for argument 'listenPort'."));
        }

        [Test]
        public void Convert_ModeArgumentInvalidValue()
        {
            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Convert<LaunchPreparerMode>("mode", "InvalidMode");
            });

            Assert.That(e.Message, Does.StartWith("Value 'InvalidMode' is invalid for parameter 'mode'.  Valid values are 'Launch', 'EncodeConfiguration'"));
            Assert.AreEqual("mode", e.ArgumentName);
        }

        [Test]
        public void Convert_ModeArgument()
        {
            var result = testArgumentValidatorConverter.Convert<LaunchPreparerMode>("mode", "EncodeConfiguration");

            Assert.AreEqual(LaunchPreparerMode.EncodeConfiguration, result);
        }

        [Test]
        public void Convert_ComponentArgumentInvalidValue()
        {
            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Convert<AccessManagerComponent>("component", "InvalidComponent");
            });

            Assert.That(e.Message, Does.StartWith("Value 'InvalidComponent' is invalid for parameter 'component'.  Valid values are 'EventCacheNode', 'GrpcEventCacheNode', 'ReaderNode', 'ReaderWriterNode', 'ReaderWriterLiteNode', 'WriterNode', 'DependencyFreeReaderWriterNode', 'DistributedReaderNode', 'DistributedWriterNode', 'DistributedOperationCoordinatorNode'"));
            Assert.AreEqual("component", e.ArgumentName);
        }

        [Test]
        public void Convert_ComponentArgument()
        {
            var result = testArgumentValidatorConverter.Convert<AccessManagerComponent>("component", "ReaderWriterNode");

            Assert.AreEqual(AccessManagerComponent.ReaderWriterNode, result);
        }

        [Test]
        public void Convert_ListenPortArgumentInvalidPortNumber()
        {
            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Convert<UInt16>("listenPort", "65546");
            });

            Assert.That(e.Message, Does.StartWith("Value '65546' is invalid for parameter 'listenPort'.  Valid values are 0-65535"));
            Assert.AreEqual("listenPort", e.ArgumentName);
        }

        [Test]
        public void Convert_ListenPortArgument()
        {
            var result = testArgumentValidatorConverter.Convert<UInt16>("listenPort", "5000");

            Assert.AreEqual(5000, result);
        }

        [Test]
        public void Convert_MinimumLogLevelArgumentInvalidValue()
        {
            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Convert<LogLevel>("minimumLogLevel", "InvalidLogLevel");
            });

            Assert.That(e.Message, Does.StartWith("Value 'InvalidLogLevel' is invalid for parameter 'minimumLogLevel'.  Valid values are 'Information', 'Warning', 'Critical'"));
            Assert.AreEqual("minimumLogLevel", e.ArgumentName);
        }

        [Test]
        public void Convert_MinimumLogLevelArgument()
        {
            var result = testArgumentValidatorConverter.Convert<LogLevel>("minimumLogLevel", "Information");

            Assert.AreEqual(LogLevel.Information, result);
        }

        [Test]
        public void Convert_EncodedJsonConfigurationInvalidValue()
        {
            // Test with a string which is not Base64
            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Convert<JObject>("encodedJsonConfiguration", "InvalidEncodedJson");
            });

            Assert.That(e.Message, Does.StartWith("Value for parameter 'encodedJsonConfiguration' could not be decoded"));
            Assert.AreEqual("encodedJsonConfiguration", e.ArgumentName);


            // Test with a string which is Base64, but not JSON
            e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Convert<JObject>("encodedJsonConfiguration", "H4sIAAAAAAACCvLLL1HwCvb3UwguKcrMSwcAAAD//w==");
            });

            Assert.That(e.Message, Does.StartWith("Value for parameter 'encodedJsonConfiguration' could not be decoded"));
            Assert.AreEqual("encodedJsonConfiguration", e.ArgumentName);
        }

        [Test]
        public void Convert_EncodedJsonConfiguration()
        {
            string testEncodedJson = "H4sIAAAAAAACCqpWUApLzClNVbJSUPIsVvAK9vdTUqgFAAAA//8=";
            JObject expectedJson = JObject.Parse(@"{ ""Value"": ""Is JSON"" }");

            var result = testArgumentValidatorConverter.Convert<JObject>("encodedJsonConfiguration", testEncodedJson);

            Assert.AreEqual(expectedJson, result);
        }

        [Test]
        public void Convert_ConfigurationFilePathArgumentInvalidValue()
        {
            String invalidFolderPath = @"X:\NonExistentFile.json";

            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Convert<String>("configurationFilePath", invalidFolderPath);
            });

            Assert.That(e.Message, Does.StartWith($"File '{invalidFolderPath}' could not be found"));
            Assert.AreEqual("configurationFilePath", e.ArgumentName);


            String invalidFilePath = @"C:\NonExistentFile.json";

            e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Convert<String>("configurationFilePath", invalidFilePath);
            });

            Assert.That(e.Message, Does.StartWith($"File '{invalidFilePath}' could not be found"));
            Assert.AreEqual("configurationFilePath", e.ArgumentName);
        }

        [Test]
        public void Validate_ArgumentsContainsParameterWithInvalidName()
        {
            var testArguments = new Dictionary<String, String>()
            {
                { "mode", "Launch" },
                { "listenPor", "5001" },
                { "component", "ReaderWriterNode" }
            };

            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Validate(testArguments);
            });

            Assert.That(e.Message, Does.StartWith($"Encountered unknown parameter 'listenPor'"));
            Assert.AreEqual("listenPor", e.ArgumentName);
        }

        [Test]
        public void Validate_ArgumentsContainsParameterWithInvalidValue()
        {
            var testArguments = new Dictionary<String, String>()
            {
                { "mode", "Launch" },
                { "listenPort", "abc" },
                { "component", "ReaderWriterNode" }
            };

            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Validate(testArguments);
            });

            Assert.That(e.Message, Does.StartWith($"Value 'abc' is invalid for parameter 'listenPort'.  Valid values are 0-65535"));
            Assert.AreEqual("listenPort", e.ArgumentName);
        }

        [Test]
        public void Validate_ArgumentsDoesntContainModeParameter()
        {
            var testArguments = new Dictionary<String, String>()
            {
                { "listenPort", "5001" },
                { "component", "ReaderWriterNode" }
            };

            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Validate(testArguments);
            });

            Assert.That(e.Message, Does.StartWith($"Missing required parameter 'mode"));
            Assert.AreEqual("mode", e.ArgumentName);
        }

        [Test]
        public void Validate_RequiredArgumentMissingForMode()
        {
            var testArguments = new Dictionary<String, String>()
            {
                { "mode", "Launch" },
                { "listenPort", "5001" },
                { "component", "ReaderWriterNode" }
            };

            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Validate(testArguments);
            });

            Assert.That(e.Message, Does.StartWith($"Missing required parameter for mode 'Launch' - 'minimumLogLevel'"));
            Assert.AreEqual("minimumLogLevel", e.ArgumentName);


            testArguments = new Dictionary<String, String>()
            {
                { "mode", "EncodeConfiguration" }
            };

            e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentValidatorConverter.Validate(testArguments);
            });

            Assert.That(e.Message, Does.StartWith($"Missing required parameter for mode 'EncodeConfiguration' - 'configurationFilePath'"));
            Assert.AreEqual("configurationFilePath", e.ArgumentName);
        }

        [Test]
        public void Validate()
        {
            var testArguments = new Dictionary<String, String>()
            {
                { "mode", "Launch" },
                { "listenPort", "5001" },
                { "component", "ReaderWriterNode" },
                { "minimumLogLevel", "Critical" },
                { "encodedJsonConfiguration", "H4sIAAAAAAACCqpWUApLzClNVbJSUPIsVvAK9vdTUqgFAAAA//8=" }
            };

            Assert.DoesNotThrow(delegate
            {
                testArgumentValidatorConverter.Validate(testArguments);
            });
        }
    }
}
