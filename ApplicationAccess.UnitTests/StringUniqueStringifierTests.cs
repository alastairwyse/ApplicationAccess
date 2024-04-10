/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Text;
using ApplicationAccess;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.StringUniqueStringifier class.
    /// </summary>
    public class StringUniqueStringifierTests
    {
        private StringUniqueStringifier testStringUniqueStringifier;

        [SetUp]
        protected void SetUp()
        {
            testStringUniqueStringifier = new StringUniqueStringifier();
        }

        // Added '_Success' suffix to prevent hiding base class ToString() method.
        [Test]
        public void ToString_Success()
        {
            string result = testStringUniqueStringifier.ToString("ABC");

            Assert.AreEqual("ABC", result);
        }

        [Test]
        public void FromString()
        {
            string result = testStringUniqueStringifier.FromString("XYZ");

            Assert.AreEqual("XYZ", result);
        }
    }
}
