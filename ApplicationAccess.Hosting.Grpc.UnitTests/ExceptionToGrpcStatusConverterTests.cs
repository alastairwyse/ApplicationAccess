/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Linq;
using System.Net;
using System.Text;
using Google.Protobuf.Collections;
using Google.Rpc;
using ApplicationAccess.Hosting.Grpc;
using ApplicationAccess.Hosting.Grpc.Models;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Serialization;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Hosting.Grpc.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Grpc.ExceptionToGrpcStatusConverter class.
    /// </summary>
    public class ExceptionToGrpcStatusConverterTests
    {
        private ExceptionToGrpcStatusConverter testExceptionToGrpcStatusConverter;

        [SetUp]
        protected void SetUp()
        {
            testExceptionToGrpcStatusConverter = new ExceptionToGrpcStatusConverter();
        }

        [Test]
        public void Constructor_InnerExceptionDepthLimitParameterLessThan0()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testExceptionToGrpcStatusConverter = new ExceptionToGrpcStatusConverter(-1);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'innerExceptionDepthLimit' with value -1 cannot be less than 0."));
            Assert.AreEqual("innerExceptionDepthLimit", e.ParamName);


            e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testExceptionToGrpcStatusConverter = new ExceptionToGrpcStatusConverter(new List<Tuple<Type, Func<Exception, Status>>>(), -1);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'innerExceptionDepthLimit' with value -1 cannot be less than 0."));
            Assert.AreEqual("innerExceptionDepthLimit", e.ParamName);
        }

        [Test]
        public void AddConversionFunction_ExceptionTypeParameterNotException()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testExceptionToGrpcStatusConverter.AddConversionFunction(typeof(StringBuilder), (Exception exception) => 
                {   
                    return new Status
                    {
                        Code = (Int32)Code.Internal,
                        Message = exception.Message
                    };
                });
            });

            Assert.That(e.Message, Does.StartWith("Type 'System.Text.StringBuilder' specified in parameter 'exceptionType' is not assignable to 'System.Exception'."));
            Assert.AreEqual("exceptionType", e.ParamName);
        }


        [Test]
        public void AddConversionFunction_ExceptionAndCodeOverloadAndExceptionTypeParameterNotException()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testExceptionToGrpcStatusConverter.AddConversionFunction(typeof(StringBuilder), Code.Internal);
            });

            Assert.That(e.Message, Does.StartWith("Type 'System.Text.StringBuilder' specified in parameter 'exceptionType' is not assignable to 'System.Exception'."));
            Assert.AreEqual("exceptionType", e.ParamName);
        }

        [Test]
        public void AddConversionFunction()
        {
            Func<Exception, Status> exceptionHandler = (Exception exception) =>
            {
                return new Status
                {
                    Code = 1234,
                    Message = "CustomeMessage"
                };
            };

            testExceptionToGrpcStatusConverter.AddConversionFunction(typeof(Exception), exceptionHandler);

            Status result = testExceptionToGrpcStatusConverter.Convert(new Exception("Fake Exception"));
            Assert.AreEqual(1234, result.Code);
            Assert.AreEqual("CustomeMessage", result.Message);
            Assert.AreEqual(0, result.Details.Count);
        }

        [Test]
        public void AddConversionFunction_ExceptionAndCodeOverload()
        {
            testExceptionToGrpcStatusConverter.AddConversionFunction(typeof(DeserializationException), Code.InvalidArgument);
            testExceptionToGrpcStatusConverter.AddConversionFunction(typeof(ServiceUnavailableException), Code.Unavailable);

            Status result = testExceptionToGrpcStatusConverter.Convert(new DeserializationException("Deserialization Failed"));

            Assert.AreEqual((Int32)Code.InvalidArgument, result.Code);
            Assert.AreEqual("Deserialization Failed", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("DeserializationException", unpackedDetail.Code);
            Assert.AreEqual("Deserialization Failed", unpackedDetail.Message);
            Assert.AreEqual("", unpackedDetail.Target);
            Assert.AreEqual(0, unpackedDetail.Attributes.Count);
            Assert.IsNull(unpackedDetail.InnerError);


            result = testExceptionToGrpcStatusConverter.Convert(new ServiceUnavailableException("Service Unavailable"));

            Assert.AreEqual((Int32)Code.Unavailable, result.Code);
            Assert.AreEqual("Service Unavailable", result.Message);
            unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("ServiceUnavailableException", unpackedDetail.Code);
            Assert.AreEqual("Service Unavailable", unpackedDetail.Message);
            Assert.AreEqual("", unpackedDetail.Target);
            Assert.AreEqual(0, unpackedDetail.Attributes.Count);
            Assert.IsNull(unpackedDetail.InnerError);
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

            Status result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.Internal, result.Code);
            Assert.AreEqual("Test exception message.", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("Exception", unpackedDetail.Code);
            Assert.AreEqual("Test exception message.", unpackedDetail.Message);
            Assert.AreEqual("Convert_Exception", unpackedDetail.Target);
            Assert.AreEqual(0, unpackedDetail.Attributes.Count);
            Assert.IsNull(unpackedDetail.InnerError);
        }

        [Test]
        public void Convert_UnthrownException()
        {
            // Unlikely to encounter this case in the real world, but tests that the 'TargetSite' property of the Exception is not included when null (i.e. when the Exception is just 'newed' rather th than being thrown).
            Status result = testExceptionToGrpcStatusConverter.Convert(new Exception("Test exception message."));

            Assert.AreEqual((Int32)Code.Internal, result.Code);
            Assert.AreEqual("Test exception message.", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("Exception", unpackedDetail.Code);
            Assert.AreEqual("Test exception message.", unpackedDetail.Message);
            Assert.AreEqual("", unpackedDetail.Target);
            Assert.AreEqual(0, unpackedDetail.Attributes.Count);
            Assert.IsNull(unpackedDetail.InnerError);
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

            Status result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.InvalidArgument, result.Code);
            Assert.AreEqual("Test argument exception message. (Parameter 'TestArgumentName')", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("ArgumentException", unpackedDetail.Code);
            Assert.AreEqual("Test argument exception message. (Parameter 'TestArgumentName')", unpackedDetail.Message);
            Assert.AreEqual("Convert_ArgumentException", unpackedDetail.Target);
            Assert.AreEqual(1, unpackedDetail.Attributes.Count);
            Assert.AreEqual("TestArgumentName", unpackedDetail.Attributes["ParameterName"]);
            Assert.IsNull(unpackedDetail.InnerError);
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

            Status result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.InvalidArgument, result.Code);
            Assert.AreEqual("Test argument out of range exception message. (Parameter 'TestArgumentName')", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("ArgumentOutOfRangeException", unpackedDetail.Code);
            Assert.AreEqual("Test argument out of range exception message. (Parameter 'TestArgumentName')", unpackedDetail.Message);
            Assert.AreEqual("Convert_ArgumentOutOfRangeException", unpackedDetail.Target);
            Assert.AreEqual(1, unpackedDetail.Attributes.Count);
            Assert.AreEqual("TestArgumentName", unpackedDetail.Attributes["ParameterName"]);
            Assert.IsNull(unpackedDetail.InnerError);
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

            Status result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.InvalidArgument, result.Code);
            Assert.AreEqual("Test argument null exception message. (Parameter 'TestArgumentName')", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("ArgumentNullException", unpackedDetail.Code);
            Assert.AreEqual("Test argument null exception message. (Parameter 'TestArgumentName')", unpackedDetail.Message);
            Assert.AreEqual("Convert_ArgumentNullException", unpackedDetail.Target);
            Assert.AreEqual(1, unpackedDetail.Attributes.Count);
            Assert.AreEqual("TestArgumentName", unpackedDetail.Attributes["ParameterName"]);
            Assert.IsNull(unpackedDetail.InnerError);
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

            Status result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.Internal, result.Code);
            Assert.AreEqual("Test index out of range exception message.", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("IndexOutOfRangeException", unpackedDetail.Code);
            Assert.AreEqual("Test index out of range exception message.", unpackedDetail.Message);
            Assert.AreEqual("Convert_IndexOutOfRangeException", unpackedDetail.Target);
            Assert.AreEqual(0, unpackedDetail.Attributes.Count);
            Assert.IsNull(unpackedDetail.InnerError);
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
                        throw new ArgumentException("Test argument exception message.", "testParameterName", e);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Outermost exception.", e);
                }
            }
            catch (Exception e)
            {
                testException = e;
            }

            Status result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.Internal, result.Code);
            Assert.AreEqual("Outermost exception.", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("Exception", unpackedDetail.Code);
            Assert.AreEqual("Outermost exception.", unpackedDetail.Message);
            Assert.AreEqual("Convert_InnerExceptionStack", unpackedDetail.Target);
            Assert.AreEqual(0, unpackedDetail.Attributes.Count);
            Assert.IsNotNull(unpackedDetail.InnerError);
            Assert.AreEqual("ArgumentException", unpackedDetail.InnerError.Code);
            Assert.AreEqual("Test argument exception message. (Parameter 'testParameterName')", unpackedDetail.InnerError.Message);
            Assert.AreEqual("Convert_InnerExceptionStack", unpackedDetail.InnerError.Target);
            Assert.AreEqual(1, unpackedDetail.InnerError.Attributes.Count());
            Assert.AreEqual("testParameterName", unpackedDetail.InnerError.Attributes["ParameterName"]);
            Assert.IsNotNull(unpackedDetail.InnerError.InnerError);
            Assert.AreEqual("IndexOutOfRangeException", unpackedDetail.InnerError.InnerError.Code);
            Assert.AreEqual("Test index out of range exception message.", unpackedDetail.InnerError.InnerError.Message);
            Assert.AreEqual("Convert_InnerExceptionStack", unpackedDetail.InnerError.InnerError.Target);
            Assert.AreEqual(0, unpackedDetail.InnerError.InnerError.Attributes.Count());
            Assert.IsNull(unpackedDetail.InnerError.InnerError.InnerError);
        }

        [Test]
        public void Convert_GenericException()
        {
            Func<Exception, Status> genericExceptionConversionFunction = (Exception exception) =>
            {
                var genericException = (GenericException<Int32>)exception;
                var attributes = new MapField<String, String>();
                attributes.Add("Int32 Value", genericException.GenericParameter.ToString());
                var grpcError = new GrpcError
                {
                    Code = exception.GetType().Name,
                    Message = exception.Message,
                    Attributes = { attributes }
                };
                if (exception.TargetSite != null)
                {
                    grpcError.Target = exception.TargetSite.Name;
                }
                return new Status
                {
                    Code = (Int32)Code.Unimplemented,
                    Message = exception.Message,
                    Details = { Google.Protobuf.WellKnownTypes.Any.Pack(grpcError) }
                };
            };
            testExceptionToGrpcStatusConverter.AddConversionFunction(typeof(GenericException<Int32>), genericExceptionConversionFunction);

            Exception testException = null;
            try
            {
                throw new GenericException<Int32>("Test generic string type exception message.", 123);
            }
            catch (Exception e)
            {
                testException = e;
            }

            Status result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.Unimplemented, result.Code);
            Assert.AreEqual("Test generic string type exception message.", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("GenericException`1", unpackedDetail.Code);
            Assert.AreEqual("Test generic string type exception message.", unpackedDetail.Message);
            Assert.AreEqual("Convert_GenericException", unpackedDetail.Target);
            Assert.AreEqual(1, unpackedDetail.Attributes.Count);
            Assert.AreEqual("123", unpackedDetail.Attributes["Int32 Value"]);
            Assert.IsNull(unpackedDetail.InnerError);


            // Test that the conversion function for plain Exceptions is used if the generic type doesn't match
            try
            {
                throw new GenericException<Char>("Test generic char type exception message.", 'A');
            }
            catch (Exception e)
            {
                testException = e;
            }

            result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.Internal, result.Code);
            Assert.AreEqual("Test generic char type exception message.", result.Message);
            unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("GenericException`1", unpackedDetail.Code);
            Assert.AreEqual("Test generic char type exception message.", unpackedDetail.Message);
            Assert.AreEqual("Convert_GenericException", unpackedDetail.Target);
            Assert.AreEqual(0, unpackedDetail.Attributes.Count);
            Assert.IsNull(unpackedDetail.InnerError);
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

            Status result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.Internal, result.Code);
            Assert.AreEqual("Test derived exception message.", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("DerivedException", unpackedDetail.Code);
            Assert.AreEqual("Test derived exception message.", unpackedDetail.Message);
            Assert.AreEqual("Convert_NoConversionFunctionDefined", unpackedDetail.Target);
            Assert.AreEqual(0, unpackedDetail.Attributes.Count);
            Assert.IsNull(unpackedDetail.InnerError);
        }

        [Test]
        public void Convert_ConversionFunctionDefinedForBaseClass()
        {
            Func<Exception, Status> derivedExceptionConversionFunction = (Exception exception) =>
            {
                var derivedException = (DerivedException)exception;
                var attributes = new MapField<String, String>();
                attributes.Add(nameof(derivedException.NumericProperty), derivedException.NumericProperty.ToString());
                var grpcError = new GrpcError
                {
                    Code = exception.GetType().Name,
                    Message = exception.Message,
                    Attributes = { attributes }
                };
                if (exception.TargetSite != null)
                {
                    grpcError.Target = exception.TargetSite.Name;
                }
                return new Status
                {
                    Code = (Int32)Code.ResourceExhausted,
                    Message = exception.Message,
                    Details = { Google.Protobuf.WellKnownTypes.Any.Pack(grpcError) }
                };
            };
            testExceptionToGrpcStatusConverter.AddConversionFunction(typeof(DerivedException), derivedExceptionConversionFunction);

            Exception testException = null;
            try
            {
                throw new SecondLevelDerivedException("Second level derived exception message.", 456, 'B');
            }
            catch (Exception e)
            {
                testException = e;
            }

            Status result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.ResourceExhausted, result.Code);
            Assert.AreEqual("Second level derived exception message.", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("SecondLevelDerivedException", unpackedDetail.Code);
            Assert.AreEqual("Second level derived exception message.", unpackedDetail.Message);
            Assert.AreEqual("Convert_ConversionFunctionDefinedForBaseClass", unpackedDetail.Target);
            Assert.AreEqual(1, unpackedDetail.Attributes.Count);
            Assert.AreEqual("456", unpackedDetail.Attributes["NumericProperty"]);
            Assert.IsNull(unpackedDetail.InnerError);
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

            Status result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.Internal, result.Code);
            Assert.AreEqual(testException.Message, result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("AggregateException", unpackedDetail.Code);
            Assert.AreEqual(testException.Message, unpackedDetail.Message);
            Assert.AreEqual("Convert_AggregateException", unpackedDetail.Target);
            Assert.AreEqual(6, unpackedDetail.Attributes.Count);
            Assert.AreEqual("Exception", unpackedDetail.Attributes["InnerException1Code"]);
            Assert.AreEqual("Plain exception inner exception.", unpackedDetail.Attributes["InnerException1Message"]);
            Assert.AreEqual("ArgumentException", unpackedDetail.Attributes["InnerException2Code"]);
            Assert.AreEqual("Argument exception inner exception. (Parameter 'ArgumentExceptionParameterName')", unpackedDetail.Attributes["InnerException2Message"]);
            Assert.AreEqual("ArgumentNullException", unpackedDetail.Attributes["InnerException3Code"]);
            Assert.AreEqual("Argument null exception inner exception. (Parameter 'ArgumentNullExceptionParameterName')", unpackedDetail.Attributes["InnerException3Message"]);
            Assert.IsNotNull(unpackedDetail.InnerError);
            Assert.AreEqual("Exception", unpackedDetail.InnerError.Code);
            Assert.AreEqual("Plain exception inner exception.", unpackedDetail.InnerError.Message);
            Assert.AreEqual("", unpackedDetail.InnerError.Target);
            Assert.AreEqual(0, unpackedDetail.InnerError.Attributes.Count());
            Assert.IsNull(unpackedDetail.InnerError.InnerError);
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

            Status result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.NotFound, result.Code);
            Assert.AreEqual("Test not found exception message.", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("NotFoundException", unpackedDetail.Code);
            Assert.AreEqual("Test not found exception message.", unpackedDetail.Message);
            Assert.AreEqual("Convert_NotFoundException", unpackedDetail.Target);
            Assert.AreEqual(1, unpackedDetail.Attributes.Count);
            Assert.AreEqual("ABC", unpackedDetail.Attributes["ResourceId"]);
            Assert.IsNull(unpackedDetail.InnerError);
        }

        [Test]
        public void Convert_InnerExceptionDepthLimitSet()
        {
            testExceptionToGrpcStatusConverter = new ExceptionToGrpcStatusConverter(1);
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
                        throw new ArgumentException("Test argument exception message.", "testParameterName", e);
                    }
                }
                catch (Exception e)
                {
                    throw new Exception("Outermost exception.", e);
                }
            }
            catch (Exception e)
            {
                testException = e;
            }

            Status result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.Internal, result.Code);
            Assert.AreEqual("Outermost exception.", result.Message);
            GrpcError unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("Exception", unpackedDetail.Code);
            Assert.AreEqual("Outermost exception.", unpackedDetail.Message);
            Assert.AreEqual("Convert_InnerExceptionDepthLimitSet", unpackedDetail.Target);
            Assert.AreEqual(0, unpackedDetail.Attributes.Count);
            Assert.IsNotNull(unpackedDetail.InnerError);
            Assert.AreEqual("ArgumentException", unpackedDetail.InnerError.Code);
            Assert.AreEqual("Test argument exception message. (Parameter 'testParameterName')", unpackedDetail.InnerError.Message);
            Assert.AreEqual("Convert_InnerExceptionDepthLimitSet", unpackedDetail.InnerError.Target);
            Assert.AreEqual(1, unpackedDetail.InnerError.Attributes.Count());
            Assert.AreEqual("testParameterName", unpackedDetail.InnerError.Attributes["ParameterName"]);
            Assert.IsNull(unpackedDetail.InnerError.InnerError);


            testExceptionToGrpcStatusConverter = new ExceptionToGrpcStatusConverter(0);

            result = testExceptionToGrpcStatusConverter.Convert(testException);

            Assert.AreEqual((Int32)Code.Internal, result.Code);
            Assert.AreEqual("Outermost exception.", result.Message);
            unpackedDetail = result.GetDetail<GrpcError>();
            Assert.AreEqual("Exception", unpackedDetail.Code);
            Assert.AreEqual("Outermost exception.", unpackedDetail.Message);
            Assert.AreEqual("Convert_InnerExceptionDepthLimitSet", unpackedDetail.Target);
            Assert.AreEqual(0, unpackedDetail.Attributes.Count);
            Assert.IsNull(unpackedDetail.InnerError);
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
                : base()
            {

            }
        }

        #endregion
    }
}
