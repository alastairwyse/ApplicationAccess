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
using System.Text;
using System.Linq;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Hosting.Rest.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Rest.ExceptionToHttpErrorResponseConverter class.
    /// </summary>
    public class ExceptionToHttpErrorResponseConverterTests
    {
        private ExceptionToHttpErrorResponseConverter testExceptionToHttpErrorResponseConverter;

        [SetUp]
        protected void SetUp()
        {
            testExceptionToHttpErrorResponseConverter = new ExceptionToHttpErrorResponseConverter();
        }

        [Test]
        public void Constructor_InnerExceptionDepthLimitParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testExceptionToHttpErrorResponseConverter = new ExceptionToHttpErrorResponseConverter(-1);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'innerExceptionDepthLimit' with value -1 cannot be less than 0."));
            Assert.AreEqual("innerExceptionDepthLimit", e.ParamName);


            e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testExceptionToHttpErrorResponseConverter = new ExceptionToHttpErrorResponseConverter(new List<Tuple<Type, Func<Exception, HttpErrorResponse>>>(), - 1);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'innerExceptionDepthLimit' with value -1 cannot be less than 0."));
            Assert.AreEqual("innerExceptionDepthLimit", e.ParamName);
        }

        [Test]
        public void AddConversionFunction_ExceptionTypeParameterNotException()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testExceptionToHttpErrorResponseConverter.AddConversionFunction(typeof(StringBuilder), (Exception exception) => { return new HttpErrorResponse("code", "message"); });
            });

            Assert.That(e.Message, Does.StartWith("Type 'System.Text.StringBuilder' specified in parameter 'exceptionType' is not assignable to 'System.Exception'."));
            Assert.AreEqual("exceptionType", e.ParamName);
        }

        [Test]
        public void AddConversionFunction()
        {
            Func<Exception, HttpErrorResponse> exceptionHandler = (Exception exception) =>
            {
                return new HttpErrorResponse("CustomCode", "CustomeMessage");
            };

            testExceptionToHttpErrorResponseConverter.AddConversionFunction(typeof(Exception), exceptionHandler);

            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(new Exception("Fake Exception"));
            Assert.AreEqual("CustomCode", result.Code);
            Assert.AreEqual("CustomeMessage", result.Message);
            Assert.IsNull(result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNull(result.InnerError);
        }

        [Test]
        public void Convert_Exception()
        {
            // Need to actually throw the test exception otherwise the 'TargetSite' property is not set.
            Exception testException = null;
            try
            {
                throw new Exception("Test exception message.");
            }
            catch (Exception e)
            {
                testException = e;
            }

            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("Exception", result.Code);
            Assert.AreEqual("Test exception message.", result.Message);
            Assert.AreEqual("Convert_Exception", result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNull(result.InnerError);
        }

        [Test]
        public void Convert_UnthrownException()
        {
            // Unlikely to encounter this case in the real world, but tests that the 'TargetSite' property of the Exception is not included when null (i.e. when the Exception is just 'newed' rather th than being thrown).
            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(new Exception("Test exception message."));

            Assert.AreEqual("Exception", result.Code);
            Assert.AreEqual("Test exception message.", result.Message);
            Assert.IsNull(result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNull(result.InnerError);
        }

        [Test]
        public void Convert_ArgumentException()
        {
            Exception testException = null;
            try
            {
                throw new ArgumentException("Test argument exception message.", "TestArgumentName");
            }
            catch (Exception e)
            {
                testException = e;
            }

            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("ArgumentException", result.Code);
            Assert.AreEqual("Test argument exception message. (Parameter 'TestArgumentName')", result.Message);
            Assert.AreEqual("Convert_ArgumentException", result.Target);
            Assert.AreEqual(1, result.Attributes.Count());
            var attributes = new List<Tuple<String, String>>(result.Attributes);
            Assert.AreEqual("ParameterName", attributes[0].Item1);
            Assert.AreEqual("TestArgumentName", attributes[0].Item2);
            Assert.IsNull(result.InnerError);
        }

        [Test]
        public void Convert_ArgumentOutOfRangeException()
        {
            Exception testException = null;
            try
            {
                throw new ArgumentOutOfRangeException("TestArgumentName", "Test argument out of range exception message.");
            }
            catch (Exception e)
            {
                testException = e;
            }

            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("ArgumentOutOfRangeException", result.Code);
            Assert.AreEqual("Test argument out of range exception message. (Parameter 'TestArgumentName')", result.Message);
            Assert.AreEqual("Convert_ArgumentOutOfRangeException", result.Target);
            Assert.AreEqual(1, result.Attributes.Count());
            var attributes = new List<Tuple<String, String>>(result.Attributes);
            Assert.AreEqual("ParameterName", attributes[0].Item1);
            Assert.AreEqual("TestArgumentName", attributes[0].Item2);
            Assert.IsNull(result.InnerError);
        }

        [Test]
        public void Convert_ArgumentNullException()
        {
            Exception testException = null;
            try
            {
                throw new ArgumentNullException("TestArgumentName", "Test argument null exception message.");
            }
            catch (Exception e)
            {
                testException = e;
            }

            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("ArgumentNullException", result.Code);
            Assert.AreEqual("Test argument null exception message. (Parameter 'TestArgumentName')", result.Message);
            Assert.AreEqual("Convert_ArgumentNullException", result.Target);
            Assert.AreEqual(1, result.Attributes.Count());
            var attributes = new List<Tuple<String, String>>(result.Attributes);
            Assert.AreEqual("ParameterName", attributes[0].Item1);
            Assert.AreEqual("TestArgumentName", attributes[0].Item2);
            Assert.IsNull(result.InnerError);
        }

        [Test]
        public void Convert_IndexOutOfRangeException()
        {
            Exception testException = null;
            try
            {
                throw new IndexOutOfRangeException("Test index out of range exception message.");
            }
            catch (Exception e)
            {
                testException = e;
            }

            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("IndexOutOfRangeException", result.Code);
            Assert.AreEqual("Test index out of range exception message.", result.Message);
            Assert.AreEqual("Convert_IndexOutOfRangeException", result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNull(result.InnerError);
        }

        [Test]
        public void Convert_InnerExceptionStack()
        {
            Exception testException = null;
            try
            {
                try
                {
                    try
                    {
                        throw new IndexOutOfRangeException("Test index out of range exception message.");
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException("Test argument exception message.", "testParameterName", e); ;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Outermost exception.", e); ;
                }
            }
            catch (Exception e)
            {
                testException = e;
            }

            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("Exception", result.Code);
            Assert.AreEqual("Outermost exception.", result.Message);
            Assert.AreEqual("Convert_InnerExceptionStack", result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNotNull(result.InnerError);
            Assert.AreEqual("ArgumentException", result.InnerError.Code);
            Assert.AreEqual("Test argument exception message. (Parameter 'testParameterName')", result.InnerError.Message);
            Assert.AreEqual("Convert_InnerExceptionStack", result.InnerError.Target);
            Assert.AreEqual(1, result.InnerError.Attributes.Count());
            var attributes = new List<Tuple<String, String>>(result.InnerError.Attributes);
            Assert.AreEqual("ParameterName", attributes[0].Item1);
            Assert.AreEqual("testParameterName", attributes[0].Item2);
            Assert.IsNotNull(result.InnerError.InnerError);
            Assert.AreEqual("IndexOutOfRangeException", result.InnerError.InnerError.Code);
            Assert.AreEqual("Test index out of range exception message.", result.InnerError.InnerError.Message);
            Assert.AreEqual("Convert_InnerExceptionStack", result.InnerError.InnerError.Target);
            Assert.AreEqual(0, result.InnerError.InnerError.Attributes.Count());
            Assert.IsNull(result.InnerError.InnerError.InnerError);
        }

        [Test]
        public void Convert_GenericException()
        {
            Func<Exception, HttpErrorResponse> genericExceptionConversionFunction = (Exception exception) =>
            {
                var genericException = (GenericException<Int32>)exception;
                if (exception.TargetSite == null)
                {
                    return new HttpErrorResponse
                    (
                        exception.GetType().Name,
                        exception.Message,
                        new List<Tuple<String, String>>()
                        {
                        new Tuple<String, String>("Int32 Value", genericException.GenericParameter.ToString())
                        }
                    );
                }
                else
                {
                    return new HttpErrorResponse
                    (
                        exception.GetType().Name,
                        exception.Message,
                        exception.TargetSite.Name,
                        new List<Tuple<String, String>>()
                        {
                        new Tuple<String, String>("Int32 Value", genericException.GenericParameter.ToString())
                        }
                    );
                }
            };
            testExceptionToHttpErrorResponseConverter.AddConversionFunction(typeof(GenericException<Int32>), genericExceptionConversionFunction);

            Exception testException = null;
            try
            {
                throw new GenericException<Int32>("Test generic string type exception message.", 123);
            }
            catch (Exception e)
            {
                testException = e;
            }

            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("GenericException`1", result.Code);
            Assert.AreEqual("Test generic string type exception message.", result.Message);
            Assert.AreEqual("Convert_GenericException", result.Target);
            Assert.AreEqual(1, result.Attributes.Count());
            var attributes = new List<Tuple<String, String>>(result.Attributes);
            Assert.AreEqual("Int32 Value", attributes[0].Item1);
            Assert.AreEqual("123", attributes[0].Item2);
            Assert.IsNull(result.InnerError);


            // Test that the conversion function for plain Exceptions is used if the generic type doesn't match
            try
            {
                throw new GenericException<Char>("Test generic char type exception message.", 'A');
            }
            catch (Exception e)
            {
                testException = e;
            }

            result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("GenericException`1", result.Code);
            Assert.AreEqual("Test generic char type exception message.", result.Message);
            Assert.AreEqual("Convert_GenericException", result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNull(result.InnerError);
        }

        [Test]
        public void Convert_NoConversionFunctionDefined()
        {
            Exception testException = null;
            try
            {
                throw new DerivedException("Test derived exception message.", 123);
            }
            catch (Exception e)
            {
                testException = e;
            }

            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("DerivedException", result.Code);
            Assert.AreEqual("Test derived exception message.", result.Message);
            Assert.AreEqual("Convert_NoConversionFunctionDefined", result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNull(result.InnerError);
        }

        [Test]
        public void Convert_ConversionFunctionDefinedForBaseClass()
        {
            Func<Exception, HttpErrorResponse> derivedExceptionConversionFunction = (Exception exception) =>
            {
                var derivedException = (DerivedException)exception;
                if (exception.TargetSite == null)
                {
                    return new HttpErrorResponse
                    (
                        exception.GetType().Name,
                        exception.Message,
                        new List<Tuple<String, String>>()
                        {
                        new Tuple<String, String>(nameof(derivedException.NumericProperty), derivedException.NumericProperty.ToString())
                        }
                    );
                }
                else
                {
                    return new HttpErrorResponse
                    (
                        exception.GetType().Name,
                        exception.Message,
                        exception.TargetSite.Name,
                        new List<Tuple<String, String>>()
                        {
                        new Tuple<String, String>(nameof(derivedException.NumericProperty), derivedException.NumericProperty.ToString())
                        }
                    );
                }
            };
            testExceptionToHttpErrorResponseConverter.AddConversionFunction(typeof(DerivedException), derivedExceptionConversionFunction);

            Exception testException = null;
            try
            {
                throw new SecondLevelDerivedException("Second level derived exception message.", 456, 'B');
            }
            catch (Exception e)
            {
                testException = e;
            }

            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("SecondLevelDerivedException", result.Code);
            Assert.AreEqual("Second level derived exception message.", result.Message);
            Assert.AreEqual("Convert_ConversionFunctionDefinedForBaseClass", result.Target);
            Assert.AreEqual(1, result.Attributes.Count());
            var attributes = new List<Tuple<String, String>>(result.Attributes);
            Assert.AreEqual("NumericProperty", attributes[0].Item1);
            Assert.AreEqual("456", attributes[0].Item2);
            Assert.IsNull(result.InnerError);
        }

        [Test]
        public void Convert_AggregateException()
        {
            var innerException1 = new Exception("Plain exception inner exception.");
            var innerException2 = new ArgumentException("Argument exception inner exception.", "ArgumentExceptionParameterName");
            var innerException3 = new ArgumentNullException("ArgumentNullExceptionParameterName", "Argument null exception inner exception.");
            Exception testException = null;
            try
            {
                throw new AggregateException
                (
                    "Test aggreate exception message.",
                    new List<Exception>()
                    { innerException1, innerException2, innerException3 }
                );
            }
            catch (Exception e)
            {
                testException = e;
            }

            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("AggregateException", result.Code);
            Assert.AreEqual(testException.Message, result.Message);
            Assert.AreEqual("Convert_AggregateException", result.Target);
            Assert.AreEqual(6, result.Attributes.Count()); 
            var attributes = new List<Tuple<String, String>>(result.Attributes);
            Assert.AreEqual("InnerException1Code", attributes[0].Item1);
            Assert.AreEqual("Exception", attributes[0].Item2);
            Assert.AreEqual("InnerException1Message", attributes[1].Item1);
            Assert.AreEqual("Plain exception inner exception.", attributes[1].Item2);
            Assert.AreEqual("InnerException2Code", attributes[2].Item1);
            Assert.AreEqual("ArgumentException", attributes[2].Item2);
            Assert.AreEqual("InnerException2Message", attributes[3].Item1);
            Assert.AreEqual("Argument exception inner exception. (Parameter 'ArgumentExceptionParameterName')", attributes[3].Item2);
            Assert.AreEqual("InnerException3Code", attributes[4].Item1);
            Assert.AreEqual("ArgumentNullException", attributes[4].Item2);
            Assert.AreEqual("InnerException3Message", attributes[5].Item1);
            Assert.AreEqual("Argument null exception inner exception. (Parameter 'ArgumentNullExceptionParameterName')", attributes[5].Item2);
            Assert.IsNotNull(result.InnerError);
            Assert.AreEqual("Exception", result.InnerError.Code);
            Assert.AreEqual("Plain exception inner exception.", result.InnerError.Message);
            Assert.AreEqual(0, result.InnerError.Attributes.Count());
            Assert.IsNull(result.InnerError.InnerError);
        }

        [Test]
        public void Convert_NotFoundException()
        {
            Exception testException = null;
            try
            {
                throw new NotFoundException("Test not found exception message.", "ABC");
            }
            catch (Exception e)
            {
                testException = e;
            }
            
            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("NotFoundException", result.Code);
            Assert.AreEqual("Test not found exception message.", result.Message);
            Assert.AreEqual("Convert_NotFoundException", result.Target);
            Assert.AreEqual(1, result.Attributes.Count()); 
            var attributes = new List<Tuple<String, String>>(result.Attributes);
            Assert.AreEqual("ResourceId", attributes[0].Item1);
            Assert.AreEqual("ABC", attributes[0].Item2);
            Assert.IsNull(result.InnerError);
        }

        [Test]
        public void Convert_InnerExceptionDepthLimitSet()
        {
            testExceptionToHttpErrorResponseConverter = new ExceptionToHttpErrorResponseConverter(1);
            Exception testException = null;
            try
            {
                try
                {
                    try
                    {
                        throw new IndexOutOfRangeException("Test index out of range exception message.");
                    }
                    catch (Exception e)
                    {
                        throw new ArgumentException("Test argument exception message.", "testParameterName", e); ;
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Outermost exception.", e); ;
                }
            }
            catch (Exception e)
            {
                testException = e;
            }

            HttpErrorResponse result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("Exception", result.Code);
            Assert.AreEqual("Outermost exception.", result.Message);
            Assert.AreEqual("Convert_InnerExceptionDepthLimitSet", result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNotNull(result.InnerError);
            Assert.AreEqual("ArgumentException", result.InnerError.Code);
            Assert.AreEqual("Test argument exception message. (Parameter 'testParameterName')", result.InnerError.Message);
            Assert.AreEqual("Convert_InnerExceptionDepthLimitSet", result.InnerError.Target);
            Assert.AreEqual(1, result.InnerError.Attributes.Count());
            var attributes = new List<Tuple<String, String>>(result.InnerError.Attributes);
            Assert.AreEqual("ParameterName", attributes[0].Item1);
            Assert.AreEqual("testParameterName", attributes[0].Item2);
            Assert.IsNull(result.InnerError.InnerError);


            testExceptionToHttpErrorResponseConverter = new ExceptionToHttpErrorResponseConverter(0);

            result = testExceptionToHttpErrorResponseConverter.Convert(testException);

            Assert.AreEqual("Exception", result.Code);
            Assert.AreEqual("Outermost exception.", result.Message);
            Assert.AreEqual("Convert_InnerExceptionDepthLimitSet", result.Target);
            Assert.AreEqual(0, result.Attributes.Count());
            Assert.IsNull(result.InnerError);
        }

        #region Nested Classes

        private class DerivedException : Exception
        {
            protected Int32 numericProperty;

            public Int32 NumericProperty
            {
                get { return numericProperty; }
            }

            public DerivedException(String message, Int32 numericProperty)
                : base(message)
            {
                this.numericProperty = numericProperty;
            }
        }

        private class SecondLevelDerivedException : DerivedException
        {
            protected Char charProperty;

            public Char CharProperty
            {
                get { return charProperty; }
            }

            public SecondLevelDerivedException(String message, Int32 numericProperty, Char charProperty)
                : base(message, numericProperty)
            {
                this.charProperty = charProperty;
            }
        }

        private class GenericException<T> : Exception
        {
            protected T genericParameter;

            public T GenericParameter
            {
                get { return genericParameter; }
            }

            public GenericException(T genericParameter)
                : base()
            {
                this.genericParameter = genericParameter;
            }

            public GenericException(String message, T genericParameter)
                : base(message)
            {
                this.genericParameter = genericParameter;
            }

            public GenericException()
                :base()
            {

            }
        }

        #endregion
    }
}
