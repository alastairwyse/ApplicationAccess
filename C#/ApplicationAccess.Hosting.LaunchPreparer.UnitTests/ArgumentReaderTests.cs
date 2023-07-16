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
using System.Collections.Generic;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.LaunchPreparer.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.LaunchPreparer.ArgumentReader class.
    /// </summary>
    public class ArgumentReaderTests
    {
        private ArgumentReader testArgumentReader;

        [SetUp]
        protected void SetUp()
        {
            testArgumentReader = new ArgumentReader();
        }

        [Test]
        public void Read_ParameterNameNotPrefixedWithDash()
        {
            var testArguments = new String[]
            {
                "-mode", "launch", "component", "ReaderWriter", "-listenPort", "5001"
            };

            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentReader.Read(testArguments);
            });

            Assert.That(e.Message, Does.StartWith("Encountered unknown parameter name 'component'"));
            Assert.AreEqual("component", e.ArgumentName);
        }

        [Test]
        public void Read_ParameterNameNotRecognized()
        {
            var testArguments = new String[]
            {
                "-mode", "launch", "-xomponent", "ReaderWriter", "-listenPort", "5001"
            };

            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentReader.Read(testArguments);
            });

            Assert.That(e.Message, Does.StartWith("Encountered unknown parameter name 'xomponent'"));
            Assert.AreEqual("xomponent", e.ArgumentName);
        }

        [Test]
        public void Read_ParameterMissingValue()
        {
            var testArguments = new String[]
            {
                "-mode", "launch", "-component", "-port", "5001"
            };

            var e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentReader.Read(testArguments);
            });

            Assert.That(e.Message, Does.StartWith("Missing value for parameter 'component'"));
            Assert.AreEqual("component", e.ArgumentName);


            testArguments = new String[]
            {
                "-mode", "launch", "-component", "ReaderWriter", "-listenPort"
            };

            e = Assert.Throws<CommandLineArgumentInvalidException>(delegate
            {
                testArgumentReader.Read(testArguments);
            });

            Assert.That(e.Message, Does.StartWith("Missing value for parameter 'listenPort'"));
            Assert.AreEqual("listenPort", e.ArgumentName);
        }


        [Test]
        public void Read()
        {
            var testArguments = new String[]
            {
                "-mode", "launch", "-component", "ReaderWriter", "-listenPort", "5001", "-minimumLogLevel", "Warning", "-encodedJsonConfiguration", "eyJMb2dnaW5nIjp7IkxvZ0xldmVsIjp7IkRlZmF1bHQiOiJJbmZvcm1hdGlvbiJ9fX0="
            };

            Dictionary<String, String> result = testArgumentReader.Read(testArguments);

            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.ContainsKey("mode"));
            Assert.IsTrue(result.ContainsKey("component"));
            Assert.IsTrue(result.ContainsKey("listenPort"));
            Assert.IsTrue(result.ContainsKey("minimumLogLevel"));
            Assert.IsTrue(result.ContainsKey("encodedJsonConfiguration"));
            Assert.AreEqual("launch", result["mode"]);
            Assert.AreEqual("ReaderWriter", result["component"]);
            Assert.AreEqual("5001", result["listenPort"]);
            Assert.AreEqual("Warning", result["minimumLogLevel"]);
            Assert.AreEqual("eyJMb2dnaW5nIjp7IkxvZ0xldmVsIjp7IkRlZmF1bHQiOiJJbmZvcm1hdGlvbiJ9fX0=", result["encodedJsonConfiguration"]);
        }
    }
}