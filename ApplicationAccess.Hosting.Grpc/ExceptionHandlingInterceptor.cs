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
using System.Threading.Tasks;
using Google.Rpc;
using Grpc.Core;
using Grpc.Core.Interceptors;
using ApplicationAccess.Hosting.Grpc.Models;
using ApplicationAccess.Hosting.Models.Options;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Utilities;
using ApplicationLogging;

namespace ApplicationAccess.Hosting.Grpc
{
    /// <summary>
    /// gRPC <see cref="Interceptor"/> implementation which catches any thrown exceptions and converts them to and returns <see cref="Google.Rpc.Status"/> objects.
    /// </summary>
    public class ExceptionHandlingInterceptor : Interceptor
    {
        /// <summary>A set of application error handling options.</summary>
        protected ErrorHandlingOptions errorHandlingOptions;
        /// <summary>Used to convert exceptions to <see cref="Google.Rpc.Status"/> objects.</summary>
        protected ExceptionToGrpcStatusConverter exceptionToGrpcStatusConverter;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.ExceptionHandlingInterceptor class.
        /// </summary>
        /// <param name="errorHandlingOptions">A set of application error handling options.</param>
        /// <param name="exceptionToGrpcStatusConverter">Used to convert exceptions to <see cref="Google.Rpc.Status"/> objects.</param>
        public ExceptionHandlingInterceptor
        (
            ErrorHandlingOptions errorHandlingOptions,
            ExceptionToGrpcStatusConverter exceptionToGrpcStatusConverter
        ) 
        {
            this.errorHandlingOptions = errorHandlingOptions;
            this.exceptionToGrpcStatusConverter = exceptionToGrpcStatusConverter;
            logger = new NullLogger(); 
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.ExceptionHandlingInterceptor class.
        /// </summary>
        /// <param name="errorHandlingOptions">A set of application error handling options.</param>
        /// <param name="exceptionToGrpcStatusConverter">Used to convert exceptions to <see cref="Google.Rpc.Status"/> objects.</param>
        /// <param name="logger">The logger for general logging.</param>
        public ExceptionHandlingInterceptor
        (
            ErrorHandlingOptions errorHandlingOptions,
            ExceptionToGrpcStatusConverter exceptionToGrpcStatusConverter,
            IApplicationLogger logger
        )
            : this(errorHandlingOptions, exceptionToGrpcStatusConverter)
        {
            this.logger = logger;
        }

        /// <inheritdoc/>
        public override async Task<TResponse> UnaryServerHandler<TRequest, TResponse>
        (
            TRequest request,
            ServerCallContext context,
            UnaryServerMethod<TRequest, TResponse> continuation
        )
        {
            try
            {
                return await continuation(request, context);
            }
            catch (Exception e)
            {
                logger.Log(LogLevel.Warning, e.Message, e);
                Google.Rpc.Status grpcStatus = exceptionToGrpcStatusConverter.Convert(e);
                if (grpcStatus.Code == (Int32)Code.Internal && errorHandlingOptions.OverrideInternalServerErrors.Value == true)
                {
                    var overrideGrpcError = new GrpcError
                    {
                        Code = e.GetType().Name,
                        Message = errorHandlingOptions.InternalServerErrorMessageOverride
                    };
                    var overrideStatus = new Google.Rpc.Status
                    {
                        Code = (Int32)Code.Internal,
                        Message = errorHandlingOptions.InternalServerErrorMessageOverride,
                        Details = { Google.Protobuf.WellKnownTypes.Any.Pack(overrideGrpcError) }
                    };

                    throw overrideStatus.ToRpcException();
                }
                else
                {
                    throw grpcStatus.ToRpcException();
                }
            }
        }
    }
}
