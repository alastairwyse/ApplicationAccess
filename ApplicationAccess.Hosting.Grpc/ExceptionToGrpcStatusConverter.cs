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
using System.Xml.Linq;
using Google.Rpc;
using ApplicationAccess.Hosting.Rest.Utilities;

namespace ApplicationAccess.Hosting.Grpc
{
    /// <summary>
    /// Converts an Exception (or object derived from Exception) to a <see cref="Status"/> object.
    /// </summary>
    public class ExceptionToGrpcStatusConverter
    {
        /// <summary>Maps a type (assignable to Exception) to a conversion function which converts that type to a <see cref="Status"/>.</summary>
        protected Dictionary<Type, Func<Exception, Status>> typeToConversionFunctionMap;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.ExceptionToGrpcStatusConverter class.
        /// </summary>
        public ExceptionToGrpcStatusConverter()
        {
            typeToConversionFunctionMap = new();
            InitialiseTypeToConversionFunctionMap();
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
                    return new Google.Rpc.Status
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
                    return new Google.Rpc.Status
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
                    return new Google.Rpc.Status
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
                    return new Google.Rpc.Status
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
                    return new Google.Rpc.Status
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
                    return new Google.Rpc.Status
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
                    return new Google.Rpc.Status
                    {
                        Code = (Int32)Code.InvalidArgument,
                        Message = exception.Message,
                        Details = { Google.Protobuf.WellKnownTypes.Any.Pack(grpcError) }
                    };
                }
            );
        }

        #endregion
    }
}
