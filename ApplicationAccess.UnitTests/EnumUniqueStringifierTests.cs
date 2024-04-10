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
    /// Unit tests for the ApplicationAccess.EnumUniqueStringifier class.
    /// </summary>
    public class EnumUniqueStringifierTests
    {
        private EnumUniqueStringifier<AccessLevel> testEnumUniqueStringifier;

        [SetUp]
        protected void SetUp()
        {
            testEnumUniqueStringifier = new EnumUniqueStringifier<AccessLevel>();
        }

        // Added '_Success' suffix to prevent hiding base class ToString() method.
        [Test]
        public void ToString_Success()
        {
            string result = testEnumUniqueStringifier.ToString(AccessLevel.Modify);

            Assert.AreEqual("Modify", result);
        }

        [Test]
        public void FromString_InvalidInputString()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testEnumUniqueStringifier.FromString("InvalidString");
            });

            Assert.That(e.Message, Does.StartWith("Failed to convert string 'InvalidString' into an enum of type 'ApplicationAccess.UnitTests.EnumUniqueStringifierTests+AccessLevel'."));
            Assert.AreEqual("stringifiedObject", e.ParamName);
        }

        [Test]
        public void FromString()
        {
            AccessLevel result = testEnumUniqueStringifier.FromString("Create");

            Assert.AreEqual(AccessLevel.Create, result);
        }

        #region Nested Classes

        protected enum AccessLevel
        {
            View,
            Create,
            Modify,
            Delete
        }

        #endregion
    }
}

