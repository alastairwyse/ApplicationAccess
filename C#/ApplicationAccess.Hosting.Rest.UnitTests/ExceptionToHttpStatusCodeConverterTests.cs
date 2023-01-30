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
using System.Text;
using System.Net;
using System.IO;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.ExceptionToHttpStatusCodeConverter class.
    /// </summary>
    public class ExceptionToHttpStatusCodeConverterTests
    {
        private ExceptionToHttpStatusCodeConverter testExceptionToHttpStatusCodeConverter;

        [SetUp]
        protected void SetUp()
        {
            testExceptionToHttpStatusCodeConverter = new ExceptionToHttpStatusCodeConverter();
        }

        [Test]
        public void AddMapping_ExceptionTypeParameterNotException()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testExceptionToHttpStatusCodeConverter.AddMapping(typeof(StringBuilder), HttpStatusCode.Unauthorized);
            });

            Assert.That(e.Message, Does.StartWith("Type 'System.Text.StringBuilder' specified in parameter 'exceptionType' is not assignable to 'System.Exception'."));
            Assert.AreEqual("exceptionType", e.ParamName);
        }

        [Test]
        public void AddMapping()
        {
            testExceptionToHttpStatusCodeConverter.AddMapping(typeof(UnauthorizedAccessException), HttpStatusCode.Unauthorized);
            testExceptionToHttpStatusCodeConverter.AddMapping(typeof(Exception), HttpStatusCode.NotFound);

            HttpStatusCode result = testExceptionToHttpStatusCodeConverter.Convert(new UnauthorizedAccessException());

            Assert.AreEqual(HttpStatusCode.Unauthorized, result);


            result = testExceptionToHttpStatusCodeConverter.Convert(new IOException());

            Assert.AreEqual(HttpStatusCode.NotFound, result);


            result = testExceptionToHttpStatusCodeConverter.Convert(new Exception());

            Assert.AreEqual(HttpStatusCode.NotFound, result);
        }

        [Test]
        public void Convert_ArgumentException()
        {
            HttpStatusCode result = testExceptionToHttpStatusCodeConverter.Convert(new ArgumentException());

            Assert.AreEqual(HttpStatusCode.BadRequest, result);
        }

        [Test]
        public void Convert_NotFoundException()
        {
            HttpStatusCode result = testExceptionToHttpStatusCodeConverter.Convert(new NotFoundException("Test message", "Test resource id"));

            Assert.AreEqual(HttpStatusCode.NotFound, result);
        }

        [Test]
        public void Convert_Exception()
        {
            HttpStatusCode result = testExceptionToHttpStatusCodeConverter.Convert(new Exception());

            Assert.AreEqual(HttpStatusCode.InternalServerError, result);
        }

        [Test]
        public void Convert_ExceptionTypeParameterIsDerivedClass()
        {
            HttpStatusCode result = testExceptionToHttpStatusCodeConverter.Convert(new ArgumentOutOfRangeException());

            Assert.AreEqual(HttpStatusCode.BadRequest, result);


            result = testExceptionToHttpStatusCodeConverter.Convert(new IOException());

            Assert.AreEqual(HttpStatusCode.InternalServerError, result);
        }
    }
}
