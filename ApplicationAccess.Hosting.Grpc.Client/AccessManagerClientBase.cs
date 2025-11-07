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
using System.Text;
using Google.Rpc;
using Grpc.Core;
using Grpc.Net.Client;
using ApplicationAccess.Hosting.Grpc.Models;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Utilities;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Hosting.Grpc.Client
{
    /// <summary>
    /// Base for client classes which interface to <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> instances hosted as gRPC services.
    /// </summary>
    public abstract class AccessManagerClientBase : IDisposable
    {
        /// <summary>The gRPC channel to use to connect.</summary>
        protected GrpcChannel channel;
        /// <summary>Maps a gRPC error code to an action which throws a matching Exception to the code.  The action accepts 1 parameter: the <see cref="GrpcError"/> representing the exception.</summary>
        protected Dictionary<Code, Action<GrpcError>> grpcErrorCodeToExceptionThrowingActionMap;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.Client.AccessManagerClientBase class.
        /// </summary>
        /// <param name="url">The URL for the hosted gRPC API.</param>
        public AccessManagerClientBase(Uri url)
        {
            channel = GrpcChannel.ForAddress(url);
            InitializeGrpcErrorCodeToExceptionThrowingActionMap();
            logger = new NullLogger();
            metricLogger = new NullMetricLogger();
            disposed = false;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.Client.AccessManagerClientBase class.
        /// </summary>
        /// <param name="url">The URL for the hosted gRPC API.</param>
        /// <param name="channelOptions">The options for configuring the gRPC channel.</param>
        public AccessManagerClientBase(Uri url, GrpcChannelOptions channelOptions) 
            : this(url)
        {
            channel.Dispose();
            channel = GrpcChannel.ForAddress(url, channelOptions);
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.Client.AccessManagerClientBase class.
        /// </summary>
        /// <param name="url">The URL for the hosted gRPC API.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerClientBase(Uri url, IApplicationLogger logger, IMetricLogger metricLogger)
            : this(url)
        {
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.Client.AccessManagerClientBase class.
        /// </summary>
        /// <param name="url">The URL for the hosted gRPC API.</param>
        /// <param name="channelOptions">The options for configuring the gRPC channel.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerClientBase(Uri url,  GrpcChannelOptions channelOptions, IApplicationLogger logger, IMetricLogger metricLogger)
            : this(url, channelOptions)
        {
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the 'grpcErrorCodeToExceptionThrowingActionMap' field.
        /// </summary>
        protected void InitializeGrpcErrorCodeToExceptionThrowingActionMap()
        {
            grpcErrorCodeToExceptionThrowingActionMap = new Dictionary<Code, Action<GrpcError>>()
            {
                {
                    Code.Internal,
                    (GrpcError grpcError) =>
                    {
                        if (grpcError.Code == typeof(IndexOutOfRangeException).Name)
                        {
                            throw new IndexOutOfRangeException(grpcError.Message);
                        }
                        else if (grpcError.Code == typeof(AggregateException).Name)
                        {
                            List<Tuple<String, String>> innerExceptionTypesAndMessages = new();
                            for (Int32 innerExceptionNumber = 1; innerExceptionNumber <= (grpcError.Attributes.Count / 2); innerExceptionNumber++)
                            {
                                String exceptionType = GetGrpcErrorAttributeValue(grpcError, $"InnerException{innerExceptionNumber}Code");
                                String message = GetGrpcErrorAttributeValue(grpcError, $"InnerException{innerExceptionNumber}Message");
                                innerExceptionTypesAndMessages.Add(Tuple.Create(exceptionType, message));
                            }
                            if (innerExceptionTypesAndMessages.Count > 0)
                            {
                                StringBuilder innerExceptionMessgeBuilder = new($"The {nameof(AggregateException)} contained the following exception types and messages as inner exceptions: ");
                                foreach (Tuple<String, String> currentInnerExceptionTypesAndMessage in innerExceptionTypesAndMessages)
                                {
                                    innerExceptionMessgeBuilder.Append($"{currentInnerExceptionTypesAndMessage.Item1}, '{currentInnerExceptionTypesAndMessage.Item2}'; ");
                                }
                                var innerException = new Exception(innerExceptionMessgeBuilder.ToString());
                                throw new AggregateException(grpcError.Message, innerException);
                            }
                            else
                            {
                                throw new AggregateException(grpcError.Message);
                            }
                        }
                        else
                        {
                            throw new Exception(grpcError.Message);
                        }
                    }
                },
                {
                    Code.InvalidArgument,
                    (GrpcError grpcError) =>
                    {
                        if (grpcError.Code == typeof(ArgumentException).Name)
                        {
                            String parameterName = GetGrpcErrorAttributeValue(grpcError, "ParameterName");
                            if (parameterName == "")
                            {
                                throw new ArgumentException(grpcError.Message);
                            }
                            else
                            {
                                throw new ArgumentException(grpcError.Message, parameterName);
                            }
                        }
                        else if (grpcError.Code == typeof(ArgumentOutOfRangeException).Name)
                        {
                            String parameterName = GetGrpcErrorAttributeValue(grpcError, "ParameterName");
                            if (parameterName == "")
                            {
                                throw new ArgumentOutOfRangeException(grpcError.Message);
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException(parameterName, grpcError.Message);
                            }
                        }
                        else if (grpcError.Code == typeof(ArgumentNullException).Name)
                        {
                            String parameterName = GetGrpcErrorAttributeValue(grpcError, "ParameterName");
                            if (parameterName == "")
                            {
                                throw new ArgumentNullException(grpcError.Message);
                            }
                            else
                            {
                                throw new ArgumentNullException(parameterName, grpcError.Message);
                            }
                        }
                    }
                },
                {
                    Code.NotFound,
                    (GrpcError grpcError) =>
                    {
                        if (grpcError.Code == "UserNotFoundException")
                        {
                            String parameterName = GetGrpcErrorAttributeValue(grpcError, "ParameterName");
                            String user = GetGrpcErrorAttributeValue(grpcError, "User");
                            throw new UserNotFoundException<String>(grpcError.Message, parameterName, user);
                        }
                        else if (grpcError.Code == "GroupNotFoundException")
                        {
                            String parameterName = GetGrpcErrorAttributeValue(grpcError, "ParameterName");
                            String group = GetGrpcErrorAttributeValue(grpcError, "Group");
                            throw new GroupNotFoundException<String>(grpcError.Message, parameterName, group);
                        }
                        else if (grpcError.Code == typeof(EntityTypeNotFoundException).Name)
                        {
                            String parameterName = GetGrpcErrorAttributeValue(grpcError, "ParameterName");
                            String entityType = GetGrpcErrorAttributeValue(grpcError, "EntityType");
                            throw new EntityTypeNotFoundException(grpcError.Message, parameterName, entityType);
                        }
                        else if (grpcError.Code == typeof(EntityNotFoundException).Name)
                        {
                            String parameterName = GetGrpcErrorAttributeValue(grpcError, "ParameterName");
                            String entityType = GetGrpcErrorAttributeValue(grpcError, "EntityType");
                            String entity = GetGrpcErrorAttributeValue(grpcError, "Entity");
                            throw new EntityNotFoundException(grpcError.Message, parameterName, entityType, entity);
                        }
                        else
                        {
                            String resourceId = GetGrpcErrorAttributeValue(grpcError, nameof(NotFoundException.ResourceId));
                            throw new NotFoundException(grpcError.Message, resourceId);
                        }
                    }
                },
                {
                    Code.Unavailable,
                    (GrpcError grpcError) =>
                    {
                        throw new ServiceUnavailableException(grpcError.Message);
                    }
                }
            };
        }

        /// <summary>
        /// Gets the value of the specified <see cref="GrpcError"/> attribute.
        /// </summary>
        /// <param name="grpcError">The <see cref="GrpcError"/> to retrieve the attribute from.</param>
        /// <param name="attributeKey">The key of the attribute to retrieve.</param>
        /// <returns>The value of the attribute, or a blank string if no attribute with that key exists.</returns>
        protected String GetGrpcErrorAttributeValue(GrpcError grpcError, String attributeKey)
        {
            if (grpcError.Attributes.ContainsKey(attributeKey) == true)
            {
                return grpcError.Attributes[attributeKey];
            }
            else
            {
                return "";
            }
        }

        /// <summary>
        /// Handles an exception caught when making and RPC request, by attempting to convert the caught exception to an <see cref="RpcException"/>, extracting the details from it (contained in an embedded <see cref="GrpcError"/>), and rethrowing the details in a standard Exception.
        /// </summary>
        /// <param name="rpcName">The name of the RPC method being called.</param>
        /// <param name="exception">The exception that was caught.</param>
        /// <remarks>If the <paramref name="exception"/> parameter is not an instance (or subclass) of <see cref="RpcException"/>, the method will return without throwing an exception.  In these cases the calling routine should rethrow the exception.</remarks>
        protected void HandleRpcException(String rpcName, Exception exception)
        {
            if (exception.GetType().IsAssignableTo(typeof(RpcException)) == true)
            {
                RpcException rpcException = (RpcException)exception;
                Google.Rpc.Status rpcStatus = rpcException.GetRpcStatus();
                if (rpcStatus != null)
                {
                    GrpcError grpcError = rpcStatus.GetDetail<GrpcError>();
                    if (grpcError != null)
                    {
                        if (grpcErrorCodeToExceptionThrowingActionMap.ContainsKey((Code)rpcStatus.Code) == true)
                        {
                            grpcErrorCodeToExceptionThrowingActionMap[(Code)rpcStatus.Code].Invoke(grpcError);
                        }
                        else
                        {
                            throw new Exception($"Failed to call RPC method '{rpcName}'.  Received non-success RPC status '{(Code)rpcStatus.Code}',  error code '{grpcError.Code}', and error message '{grpcError.Message}'.");
                        }
                    }
                    else
                    {
                        throw new Exception($"Caught ${nameof(RpcException)} with no details of type '{nameof(GrpcError)}' when calling RPC method '{rpcName}'.", rpcException);
                    }
                }
            }
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the AccessManagerClientBase.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~AccessManagerClientBase()
        {
            Dispose(false);
        }

        #pragma warning restore 1591

        /// <summary>
        /// Provides a method to free unmanaged resources used by this class.
        /// </summary>
        /// <param name="disposing">Whether the method is being called as part of an explicit Dispose routine, and hence whether managed resources should also be freed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    channel.Dispose();
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
