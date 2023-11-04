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

namespace ApplicationAccess.Distribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Distribution.AccessManagerRestClientConfiguration class.
    /// </summary>
    public class AccessManagerRestClientConfigurationTests
    {
        [Test]
        public void Constructor_BaseUrlParameterWhitespace()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                var testAccessManagerRestClientConfiguration = new AccessManagerRestClientConfiguration(" ");
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'baseUrl' must contain a value."));
            Assert.AreEqual("baseUrl", e.ParamName);
        }
    }
}
