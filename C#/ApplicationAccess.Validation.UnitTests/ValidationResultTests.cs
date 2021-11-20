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
using NUnit.Framework;

namespace ApplicationAccess.Validation.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Validation.ValidationResult class.
    /// </summary>
    public class ValidationResultTests
    {
        [Test]
        public void Constructor_SingleSuccessfulParameterSetFalse()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                var testValidationResult = new ValidationResult(false);
            });

            Assert.That(e.Message, Does.StartWith("If parameter 'successful' is set false, a 'message' parameter must be specified."));
            Assert.AreEqual("successful", e.ParamName);
        }

        [Test]
        public void Constructor_TwoParametersSuccessfulSetTrue()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                var testValidationResult = new ValidationResult(true, "Validation message.");
            });

            Assert.That(e.Message, Does.StartWith("If parameter 'successful' is set true, a message parameter cannot specified."));
            Assert.AreEqual("successful", e.ParamName);
        }

        [Test]
        public void Constructor_TwoParametersMessageNull()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                var testValidationResult = new ValidationResult(false, null);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'message' must contain a value."));
            Assert.AreEqual("message", e.ParamName);
        }

        [Test]
        public void Constructor_TwoParametersMessageWhitespace()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                var testValidationResult = new ValidationResult(false, " ");
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'message' must contain a value."));
            Assert.AreEqual("message", e.ParamName);
        }

        [Test]
        public void Constructor_ThreeParametersSuccessfulSetTrue()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                var testValidationResult = new ValidationResult(true, "Validation message.", new Exception("Validation Exception"));
            });

            Assert.That(e.Message, Does.StartWith("If parameter 'successful' is set true, a message parameter cannot specified."));
            Assert.AreEqual("successful", e.ParamName);
        }

        [Test]
        public void Constructor_ThreeParametersMessageNull()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                var testValidationResult = new ValidationResult(false, null, new Exception("Validation Exception"));
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'message' must contain a value."));
            Assert.AreEqual("message", e.ParamName);
        }

        [Test]
        public void Constructor_ThreeParametersMessageWhitespace()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                var testValidationResult = new ValidationResult(false, " ", new Exception("Validation Exception"));
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'message' must contain a value."));
            Assert.AreEqual("message", e.ParamName);
        }

        [Test]
        public void Constructor_ThreeParametersValidationExceptionNull()
        {
            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                var testValidationResult = new ValidationResult(false, "Validation message.", null);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'validationException' cannot be null."));
            Assert.AreEqual("validationException", e.ParamName);
        }
    }
}
