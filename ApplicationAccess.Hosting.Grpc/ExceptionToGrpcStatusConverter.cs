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
using Google.Rpc;
using ApplicationAccess.Hosting.Rest.Utilities;

namespace ApplicationAccess.Hosting.Grpc
{
    /// <summary>
    /// Converts an Exception (or object derived from Exception) to a <see cref="Status"/> object.
    /// </summary>
    public class ExceptionToGrpcStatusConverter
    {
        // TODO: This class has a lot of overlap with ApplicationAccess.Hosting.Rest.Utilities.ExceptionToHttpErrorResponseConverter
        //   Could derive both from an abstract base class similar to ExceptionToErrorConverter<T>, where the T is the type to convert to
        //   i.e. Google.Rpc.Status or HttpErrorResponse

        /// <summary>Maps a type (assignable to Exception) to a conversion function which converts that type to a <see cref="Status"/>.</summary>
        protected Dictionary<Type, Func<Exception, Status>> typeToConversionFunctionMap;
        /// <summary>The limit of the depth of inner exceptions to convert.</summary>
        protected Int32 innerExceptionDepthLimit;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.ExceptionToGrpcStatusConverter class.
        /// </summary>
        public ExceptionToGrpcStatusConverter()
        {
            typeToConversionFunctionMap = new();
            InitialiseTypeToConversionFunctionMap();
            innerExceptionDepthLimit = Int32.MaxValue;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.ExceptionToGrpcStatusConverter class.
        /// </summary>
        /// <param name="typesAndConversionFunctions">A collection of tuples containing 2 values: A type (assignable to Exception) that the conversion function in the second component converts, and the conversion function which accepts an Exception object and returns a Status.</param>
        /// <remarks>Note that the conversion functions should not handle the exception's 'InnerException' property, nor assign to the HttpErrorResponse's 'InnerError' property.</remarks>
        public ExceptionToGrpcStatusConverter(IEnumerable<Tuple<Type, Func<Exception, Status>>> typesAndConversionFunctions)
            : this()
        {
            foreach (Tuple<Type, Func<Exception, Status>> currentTypeAndConversionFunction in typesAndConversionFunctions)
            {
                AddConversionFunction(currentTypeAndConversionFunction.Item1, currentTypeAndConversionFunction.Item2);
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.ExceptionToGrpcStatusConverter class.
        /// </summary>
        /// <param name="typesAndConversionFunctions">A collection of tuples containing 2 values: A type (assignable to Exception) that the conversion function in the second component converts, and the conversion function which accepts an Exception object and returns a Status.</param>
        /// <param name="innerExceptionDepthLimit">The limit of the depth of inner exceptions to convert.  A value of 0 will convert only the top level exception and no inner exceptions.</param>
        /// <remarks>Note that the conversion functions should not handle the exception's 'InnerException' property, nor assign to the HttpErrorResponse's 'InnerError' property.</remarks>
        public ExceptionToGrpcStatusConverter(IEnumerable<Tuple<Type, Func<Exception, Status>>> typesAndConversionFunctions, Int32 innerExceptionDepthLimit)
            : this(typesAndConversionFunctions)
        {
            {
                ThrowExceptionIfInnerExceptionDepthLimitParameterOutOfRange(nameof(innerExceptionDepthLimit), innerExceptionDepthLimit);
                this.innerExceptionDepthLimit = innerExceptionDepthLimit;
            }
        }

        /// <summary>
        /// Initialises a new instance of the  ApplicationAccess.Hosting.Grpc.ExceptionToGrpcStatusConverter class.
        /// </summary>
        /// <param name="innerExceptionDepthLimit">The limit of the depth of inner exceptions to convert.  A value of 0 will convert only the top level exception and no inner exceptions.</param>
        public ExceptionToGrpcStatusConverter(Int32 innerExceptionDepthLimit)
            : this()
        {
            ThrowExceptionIfInnerExceptionDepthLimitParameterOutOfRange(nameof(innerExceptionDepthLimit), innerExceptionDepthLimit);
            this.innerExceptionDepthLimit = innerExceptionDepthLimit;
        }

        /// <summary>
        /// Adds a conversion function to the converter.
        /// </summary>
        /// <param name="exceptionType">The type (assignable to <see cref="Exception"/>) that the conversion function converts.</param>
        /// <param name="conversionFunction">The conversion function.  Accepts an Exception object and returns a <see cref="Status"/>.</param>
        /// <remarks>Note that the conversion function should not handle the exception's 'InnerException' property, nor assign to the HttpErrorResponse's 'InnerError' property.</remarks>
        public void AddConversionFunction(Type exceptionType, Func<Exception, Status> conversionFunction)
        {
            if (typeof(Exception).IsAssignableFrom(exceptionType) == false)
                throw new ArgumentException($"Type '{exceptionType.FullName}' specified in parameter '{nameof(exceptionType)}' is not assignable to '{typeof(Exception).FullName}'.", nameof(exceptionType));

            if (typeToConversionFunctionMap.ContainsKey(exceptionType) == true)
            {
                typeToConversionFunctionMap.Remove(exceptionType);
            }
            typeToConversionFunctionMap.Add(exceptionType, conversionFunction);
        }

        /// <summary>
        /// Adds handling for an exception to the converter, using a default conversion function (one which populates just the <see cref="GrpcError.Code"/>, <see cref="GrpcError.Message"/> and optionally <see cref="GrpcError.Target"/> properties).
        /// </summary>
        /// <param name="exceptionType">The type (assignable to <see cref="Exception"/>) to convert.</param>
        public void AddConversionFunction(Type exceptionType)
        {
            Func<Exception, Status> conversionFunction = (Exception exception) =>
            {
                var grpcError = new GrpcError
                {
                    Code = exception.GetType().Name,
                    Message = exception.Message
                };
                if (exception.TargetSite != null)
                {
                    grpcError.Target = exception.TargetSite.Name;
                }
                return new Status
                {
                    Code = (Int32)Code.Internal,
                    Message = exception.Message,
                    Details = { Google.Protobuf.WellKnownTypes.Any.Pack(grpcError) }
                };
            };
            AddConversionFunction(exceptionType, conversionFunction);
        }

        /// <summary>
        /// Converts the specified exception to a <see cref="Status">.
        /// </summary>
        /// <param name="exception">The exception to convert.</param>
        /// <returns>The exception converted to a <see cref="Status">.</returns>
        public Status Convert(Exception exception)
        {
            return ConvertExceptionRecurse(exception, 0);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initialises the 'typeToConversionFunctionMap' member.
        /// </summary>
        protected void InitialiseTypeToConversionFunctionMap()
        {
            typeToConversionFunctionMap.Add
            (
                typeof(Exception),
                (Exception exception) =>
                {
                    var grpcError = new GrpcError
                    {
                        Code = exception.GetType().Name,
                        Message = exception.Message
                    };
                    if (exception.TargetSite != null)
                    {
                        grpcError.Target = exception.TargetSite.Name;
                    }
                    return new Status
                    {
                        Code = (Int32)Code.Internal,
                        Message = exception.Message,
                        Details = { Google.Protobuf.WellKnownTypes.Any.Pack(grpcError) }
                    };
                }
            );
            typeToConversionFunctionMap.Add
            (
                typeof(ArgumentException),
                (Exception exception) =>
                {
                    var argumentException = (ArgumentException)exception;
                    var attributes = new KeyValuePair
                    {
                        Key = "ParameterName", 
                        Value = $"{argumentException.ParamName}"
                    };
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
                        Code = (Int32)Code.InvalidArgument,
                        Message = exception.Message,
                        Details = { Google.Protobuf.WellKnownTypes.Any.Pack(grpcError) }
                    };
                }
            );
            typeToConversionFunctionMap.Add
            (
                typeof(ArgumentOutOfRangeException),
                (Exception exception) =>
                {
                    var argumentOutOfRangeException = (ArgumentOutOfRangeException)exception;
                    var attributes = new KeyValuePair
                    {
                        Key = "ParameterName",
                        Value = $"{argumentOutOfRangeException.ParamName}"
                    };
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
                        Code = (Int32)Code.InvalidArgument,
                        Message = exception.Message,
                        Details = { Google.Protobuf.WellKnownTypes.Any.Pack(grpcError) }
                    };
                }
            );
            typeToConversionFunctionMap.Add
            (
                typeof(ArgumentNullException),
                (Exception exception) =>
                {
                    var argumentNullException = (ArgumentNullException)exception;
                    var attributes = new KeyValuePair
                    {
                        Key = "ParameterName",
                        Value = $"{argumentNullException.ParamName}"
                    };
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
                        Code = (Int32)Code.InvalidArgument,
                        Message = exception.Message,
                        Details = { Google.Protobuf.WellKnownTypes.Any.Pack(grpcError) }
                    };
                }
            );
            typeToConversionFunctionMap.Add
            (
                typeof(IndexOutOfRangeException),
                (Exception exception) =>
                {
                    var grpcError = new GrpcError
                    {
                        Code = exception.GetType().Name,
                        Message = exception.Message
                    };
                    if (exception.TargetSite != null)
                    {
                        grpcError.Target = exception.TargetSite.Name;
                    }
                    return new Status
                    {
                        Code = (Int32)Code.Internal,
                        Message = exception.Message,
                        Details = { Google.Protobuf.WellKnownTypes.Any.Pack(grpcError) }
                    };
                }
            );
            typeToConversionFunctionMap.Add
            (
                typeof(AggregateException),
                (Exception exception) =>
                {
                    var aggregateException = (AggregateException)exception;
                    var grpcError = new GrpcError
                    {
                        Code = exception.GetType().Name,
                        Message = exception.Message
                    };
                    // Convert the inner exceptions
                    Int32 innerExceptionNumber = 1;
                    foreach (Exception currentInnerException in aggregateException.InnerExceptions)
                    {
                        grpcError.Attributes.Add(new KeyValuePair { Key = $"InnerException{innerExceptionNumber}Code", Value = currentInnerException.GetType().Name });
                        grpcError.Attributes.Add(new KeyValuePair { Key = $"InnerException{innerExceptionNumber}Message", Value = currentInnerException.Message });
                        innerExceptionNumber++;
                    }
                    if (exception.TargetSite != null)
                    {
                        grpcError.Target = exception.TargetSite.Name;
                    }
                    return new Status
                    {
                        Code = (Int32)Code.Internal,
                        Message = exception.Message,
                        Details = { Google.Protobuf.WellKnownTypes.Any.Pack(grpcError) }
                    };
                }
            );
            typeToConversionFunctionMap.Add
            (
                typeof(NotFoundException),
                (Exception exception) =>
                {
                    var notFoundException = (NotFoundException)exception;
                    var attributes = new KeyValuePair
                    {
                        Key = nameof(notFoundException.ResourceId),
                        Value = $"{notFoundException.ResourceId}"
                    };
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
                        Code = (Int32)Code.NotFound,
                        Message = exception.Message,
                        Details = { Google.Protobuf.WellKnownTypes.Any.Pack(grpcError) }
                    };
                }
            );
        }

        /// <summary>
        /// Converts the specified exception to a <see cref="Status">, recursively converting any inner exceptions.
        /// </summary>
        /// <param name="exception">The exception to convert.</param>
        /// <param name="currentInnerExceptionDepth">The current depth of inner exceptions.</param>
        /// <returns>The converted exception.</returns>
        protected Status ConvertExceptionRecurse(Exception exception, Int32 currentInnerExceptionDepth)
        {
            // Recursively call for any inner exceptions
            Status innerError = null;
            if (currentInnerExceptionDepth < innerExceptionDepthLimit)
            {
                if (exception.InnerException != null)
                {
                    innerError = ConvertExceptionRecurse(exception.InnerException, currentInnerExceptionDepth + 1);
                }
            }

            // Convert the exception using a matching function from the type to conversion function map
            Status returnError = ConvertException(exception);
            if (innerError != null)
            {
                GrpcError unpackedDetail = returnError.GetDetail<GrpcError>();
                GrpcError unpackedInnerErrorDetail = innerError.GetDetail<GrpcError>();
                if (unpackedDetail != null && unpackedInnerErrorDetail != null)
                {
                    unpackedDetail.InnerError = unpackedInnerErrorDetail;
                }
                returnError = new Status
                {
                    Code = returnError.Code,
                    Message = returnError.Message,
                    Details = { Google.Protobuf.WellKnownTypes.Any.Pack(unpackedDetail) }
                };
            }

            return returnError;
        }

        /// <summary>
        /// Converts an individual exception (i.e. not its inner exception hierarchy) to a <see cref="Status"> using a function from the type to conversion function map.
        /// </summary>
        /// <param name="exception">The exception to convert.</param>
        /// <returns>The converted exception.</returns>
        protected Status ConvertException(Exception exception)
        {
            var currentType = exception.GetType();
            while (currentType != null)
            {
                if (typeToConversionFunctionMap.ContainsKey(currentType) == true)
                {
                    return typeToConversionFunctionMap[currentType].Invoke(exception);
                }
                else
                {
                    currentType = currentType.BaseType;
                }
            }
            throw new Exception($"No valid conversion function defined for exception type '{exception.GetType().FullName}'.");
        }

        protected void ThrowExceptionIfInnerExceptionDepthLimitParameterOutOfRange(String parameterName, Int32 parameterValue)
        {
            if (parameterValue < 0)
                throw new ArgumentOutOfRangeException(parameterName, $"Parameter '{parameterName}' with value {parameterValue} cannot be less than 0.");
        }

        #endregion
    }
}
