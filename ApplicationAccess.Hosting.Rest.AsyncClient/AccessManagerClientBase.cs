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
using System.IO;
using System.Net;
using System.Net.Http;
using ApplicationAccess.Utilities;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Serialization;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json;
using Polly;
using System.Linq.Expressions;

namespace ApplicationAccess.Hosting.Rest.AsyncClient
{
    /// <summary>
    /// Base for client classes which interface to <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> instances hosted as REST web APIs.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public abstract class AccessManagerClientBase<TUser, TGroup, TComponent, TAccess> : IDisposable
    {
        /// <summary>The character encoding used when interpreting/reading HTTP responses.</summary>
        protected Encoding defaultEncoding = Encoding.UTF8;

        /// <summary>The client to use to connect.</summary>
        protected HttpClient httpClient;
        /// <summary>>The base URL for the hosted Web API.</summary>
        protected Uri baseUrl;
        /// <summary>Deserializer for HttpErrorResponse objects.</summary>
        protected HttpErrorResponseJsonSerializer errorResponseDeserializer;
        /// <summary>Maps an HTTP status code to an action which throws a matching Exception to the status code.  The action accepts 1 parameter: the <see cref="HttpErrorResponse"/> representing the exception.</summary>
        protected Dictionary<HttpStatusCode, Action<HttpErrorResponse>> statusCodeToExceptionThrowingActionMap;
        /// <summary>A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</summary>
        protected IUniqueStringifier<TUser> userStringifier;
        /// <summary>A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</summary>
        protected IUniqueStringifier<TGroup> groupStringifier;
        /// <summary>A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</summary>
        protected IUniqueStringifier<TComponent> applicationComponentStringifier;
        /// <summary>A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</summary>
        protected IUniqueStringifier<TAccess> accessLevelStringifier;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Whether the HttpClient member was instantiated within the class constructor.</summary>
        protected Boolean httpClientInstantiatedInConstructor;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public AccessManagerClientBase
        (
            Uri baseUrl,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Int32 retryCount,
            Int32 retryInterval
        )
        {
            SetBaseConstructorParameters
            (
                baseUrl,
                userStringifier,
                groupStringifier,
                applicationComponentStringifier,
                accessLevelStringifier,
                retryCount,
                retryInterval
            );
            httpClient = new HttpClient();
            SetHttpClientAcceptHeader(httpClient);
            httpClientInstantiatedInConstructor = true;
            logger = new NullLogger();
            metricLogger = new NullMetricLogger();

        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerClientBase
        (
            Uri baseUrl,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Int32 retryCount,
            Int32 retryInterval,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
            : this(baseUrl, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval)
        {
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        public AccessManagerClientBase
        (
            Uri baseUrl,
            HttpClient httpClient,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Int32 retryCount,
            Int32 retryInterval
        )
        {
            SetBaseConstructorParameters
            (
                baseUrl,
                userStringifier,
                groupStringifier,
                applicationComponentStringifier,
                accessLevelStringifier,
                retryCount,
                retryInterval
            );
            this.httpClient = httpClient;
            SetHttpClientAcceptHeader(this.httpClient);
            httpClientInstantiatedInConstructor = false;
            logger = new NullLogger();
            metricLogger = new NullMetricLogger();
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.AsyncClient.AccessManagerClientBase class.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.  This should contain the scheme, host, and port subcomponents of the Web API URL, but not include the path 'api' prefix and version number.  For example 'https://127.0.0.1:5170/'.</param>
        /// <param name="httpClient">The client to use to connect.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerClientBase
        (
            Uri baseUrl,
            HttpClient httpClient,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Int32 retryCount,
            Int32 retryInterval,
            IApplicationLogger logger,
            IMetricLogger metricLogger
        )
            : this(baseUrl, httpClient, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, retryCount, retryInterval)
        {
            this.logger = logger;
            this.metricLogger = metricLogger;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Performs setup for a minimal/common set of constructor parameters.
        /// </summary>
        /// <param name="baseUrl">The base URL for the hosted Web API.</param>
        /// <param name="userStringifier">A string converter for users.  Used to convert strings sent to and received from the web API from/to TUser instances.</param>
        /// <param name="groupStringifier">A string converter for groups.  Used to convert strings sent to and received from the web API from/to TGroup instances.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.  Used to convert strings sent to and received from the web API from/to TComponent instances.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.  Used to convert strings sent to and received from the web API from/to TAccess instances.</param>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        protected void SetBaseConstructorParameters
        (
            Uri baseUrl,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            Int32 retryCount,
            Int32 retryInterval
        )
        {
            Action<Exception, TimeSpan, Int32, Context> pollyRetryAction = ValidateRetryConstructorParametersAndCreateRetryAction(retryCount, retryInterval);
            SetupExceptionHanderPolicies(retryCount, retryInterval, pollyRetryAction);
            InitializeBaseUrl(baseUrl);
            errorResponseDeserializer = new HttpErrorResponseJsonSerializer();
            InitializeStatusCodeToExceptionThrowingActionMap();
            this.userStringifier = userStringifier;
            this.groupStringifier = groupStringifier;
            this.applicationComponentStringifier = applicationComponentStringifier;
            this.accessLevelStringifier = accessLevelStringifier;
            disposed = false;
        }

        /// <summary>
        /// Adds an appropriate path suffix to the specified 'baseUrl' constructor parameter.
        /// </summary>
        /// <param name="baseUrl">The base URL to initialize.</param>
        protected void InitializeBaseUrl(Uri baseUrl)
        {
            this.baseUrl = new Uri(baseUrl, "api/v1/");
        }

        /// <summary>
        /// Sets appropriate 'Accept' headers on the specified HTTP client.
        /// </summary>
        /// <param name="httpClient">The HTTP client to set the header(s) on.</param>
        protected void SetHttpClientAcceptHeader(HttpClient httpClient)
        {
            const String acceptHeaderName = "Accept";
            const String acceptHeaderValue = "application/json";
            if (httpClient.DefaultRequestHeaders.Contains(acceptHeaderName) == true)
            {
                httpClient.DefaultRequestHeaders.Remove(acceptHeaderName);
            }
            httpClient.DefaultRequestHeaders.Add(acceptHeaderName, acceptHeaderValue);
        }

        /// <summary>
        /// Initializes the 'statusCodeToExceptionThrowingActionMap' member.
        /// </summary>
        protected void InitializeStatusCodeToExceptionThrowingActionMap()
        {
            statusCodeToExceptionThrowingActionMap = new Dictionary<HttpStatusCode, Action<HttpErrorResponse>>()
            {
                {
                    HttpStatusCode.InternalServerError,
                    (HttpErrorResponse httpErrorResponse) => { throw new Exception(httpErrorResponse.Message); }
                },
                {
                    HttpStatusCode.BadRequest,
                    (HttpErrorResponse httpErrorResponse) => 
                    {
                        if (httpErrorResponse.Code == typeof(ArgumentException).Name)
                        {
                            String parameterName = GetHttpErrorResponseAttributeValue(httpErrorResponse, "ParameterName");
                            if (parameterName == "")
                            {
                                throw new ArgumentException(httpErrorResponse.Message);
                            }
                            else
                            {
                                throw new ArgumentException(httpErrorResponse.Message, parameterName);
                            }
                        }
                        else if (httpErrorResponse.Code == typeof(ArgumentOutOfRangeException).Name)
                        {
                            String parameterName = GetHttpErrorResponseAttributeValue(httpErrorResponse, "ParameterName");
                            if (parameterName == "")
                            {
                                throw new ArgumentOutOfRangeException(httpErrorResponse.Message);
                            }
                            else
                            {
                                throw new ArgumentOutOfRangeException(parameterName, httpErrorResponse.Message);
                            }
                        }
                        else if (httpErrorResponse.Code == typeof(ArgumentNullException).Name)
                        {
                            String parameterName = GetHttpErrorResponseAttributeValue(httpErrorResponse, "ParameterName");
                            if (parameterName == "")
                            {
                                throw new ArgumentNullException(httpErrorResponse.Message);
                            }
                            else
                            {
                                throw new ArgumentNullException(parameterName, httpErrorResponse.Message);
                            }
                        }
                        else
                        {
                            throw new ArgumentException(httpErrorResponse.Message);
                        }
                    }
                },
                {
                    HttpStatusCode.NotFound,
                    (HttpErrorResponse httpErrorResponse) =>
                    {
                        if (httpErrorResponse.Code == "UserNotFoundException")
                        {
                            String parameterName = GetHttpErrorResponseAttributeValue(httpErrorResponse, "ParameterName");
                            String user = GetHttpErrorResponseAttributeValue(httpErrorResponse, "User");
                            throw new UserNotFoundException<String>(httpErrorResponse.Message, parameterName, user);
                        }
                        else if (httpErrorResponse.Code == "GroupNotFoundException")
                        {
                            String parameterName = GetHttpErrorResponseAttributeValue(httpErrorResponse, "ParameterName");
                            String group = GetHttpErrorResponseAttributeValue(httpErrorResponse, "Group");
                            throw new GroupNotFoundException<String>(httpErrorResponse.Message, parameterName, group);
                        }
                        else if (httpErrorResponse.Code == typeof(EntityTypeNotFoundException).Name)
                        {
                            String parameterName = GetHttpErrorResponseAttributeValue(httpErrorResponse, "ParameterName");
                            String entityType = GetHttpErrorResponseAttributeValue(httpErrorResponse, "EntityType");
                            throw new EntityTypeNotFoundException(httpErrorResponse.Message, parameterName, entityType);
                        }
                        else if (httpErrorResponse.Code == typeof(EntityNotFoundException).Name)
                        {
                            String parameterName = GetHttpErrorResponseAttributeValue(httpErrorResponse, "ParameterName");
                            String entityType = GetHttpErrorResponseAttributeValue(httpErrorResponse, "EntityType");
                            String entity = GetHttpErrorResponseAttributeValue(httpErrorResponse, "Entity");
                            throw new EntityNotFoundException(httpErrorResponse.Message, parameterName, entityType, entity);
                        }
                        else
                        {
                            String resourceId = GetHttpErrorResponseAttributeValue(httpErrorResponse, "ResourceId");
                            throw new NotFoundException(httpErrorResponse.Message, resourceId);
                        }
                    }
                }
            };
        }

        /// <summary>
        /// Validates the 'retryCount' and 'retryInterval' constructor parameters, and uses them to create a Polly retry action.
        /// </summary>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        /// <returns>An action which can be set as the retry action on a Polly <see cref="Policy"/>  or <see cref="AsyncPolicy"/> object.</returns>
        protected Action<Exception, TimeSpan, Int32, Context> ValidateRetryConstructorParametersAndCreateRetryAction(Int32 retryCount, Int32 retryInterval)
        {
            if (retryCount < 0)
                throw new ArgumentOutOfRangeException(nameof(retryCount), $"Parameter '{nameof(retryCount)}' with value {retryCount} cannot be less than 0.");
            if (retryCount > 59)
                throw new ArgumentOutOfRangeException(nameof(retryCount), $"Parameter '{nameof(retryCount)}' with value {retryCount} cannot be greater than 59.");
            if (retryInterval < 0)
                throw new ArgumentOutOfRangeException(nameof(retryInterval), $"Parameter '{nameof(retryInterval)}' with value {retryInterval} cannot be less than 0.");
            if (retryInterval > 120)
                throw new ArgumentOutOfRangeException(nameof(retryInterval), $"Parameter '{nameof(retryInterval)}' with value {retryInterval} cannot be greater than 120.");

            return (Exception exception, TimeSpan actionRetryInterval, Int32 currentRetryCount, Context context) =>
            {
                logger.Log(this, LogLevel.Warning, $"Exception occurred when sending HTTP request.  Retrying in {retryInterval} seconds (retry {currentRetryCount} of {retryCount}).", exception);
                metricLogger.Increment(new HttpRequestRetried());
            };
        }

        /// <summary>
        /// Sets up the Polly exception handling policy.
        /// </summary>
        /// <param name="retryCount">The number of times an operation should be retried in the case of a transient error (e.g. network error).</param>
        /// <param name="retryInterval">The time in seconds between retries.</param>
        /// <param name="onRetryAction">An action which can be set as the retry action on a Polly <see cref="Policy"/>  or <see cref="AsyncPolicy"/> object.</param>
        protected abstract void SetupExceptionHanderPolicies(Int32 retryCount, Int32 retryInterval, Action<Exception, TimeSpan, Int32, Context> onRetryAction);

        /// <summary>
        /// Handles receipt of a non-success HTTP response status, by converting the status and response body to an appropriate Exception and throwing that Exception.
        /// </summary>
        /// <param name="method">The HTTP method used in the request which generated the response.</param>
        /// <param name="requestUrl">The URL of the request which generated the response.</param>
        /// <param name="responseStatus">The received HTTP response status.</param>
        /// <param name="responseBody">The received response body.</param>
        protected void HandleNonSuccessResponseStatus(HttpMethod method, Uri requestUrl, HttpStatusCode responseStatus, Stream responseBody)
        {
            String baseExceptionMessage = $"Failed to call URL '{requestUrl.ToString()}' with '{method.ToString()}' method.  Received non-succces HTTP response status '{(Int32)responseStatus}'";

            // Attempt to deserialize a HttpErrorResponse from the body
            responseBody.Position = 0;
            HttpErrorResponse httpErrorResponse = DeserializeResponseBodyToHttpErrorResponse(responseBody);
            if (httpErrorResponse != null)
            {
                if (statusCodeToExceptionThrowingActionMap.ContainsKey(responseStatus) == true)
                {
                    statusCodeToExceptionThrowingActionMap[responseStatus].Invoke(httpErrorResponse);
                }
                else
                {
                    String exceptionMessagePostfix = $", error code '{httpErrorResponse.Code}', and error message '{httpErrorResponse.Message}'.";
                    throw new Exception(baseExceptionMessage + exceptionMessagePostfix);
                }
            }
            else
            {
                // Read the response body as a string
                String responseBodyAsString = "";
                responseBody.Position = 0;
                using (var streamReader = new StreamReader(responseBody, defaultEncoding))
                {
                    responseBodyAsString = streamReader.ReadToEnd();
                }
                if (String.IsNullOrEmpty(responseBodyAsString.Trim()) == false)
                {
                    throw new Exception(baseExceptionMessage + $" and response body '{responseBodyAsString}'.");
                }
                else
                {
                    throw new Exception(baseExceptionMessage + ".");
                }
            }
        }

        /// <summary>
        /// Attempts to deserialize the body of a HTTP response received as a <see cref="Stream"/> to an <see cref="HttpErrorResponse"/> instance.
        /// </summary>
        /// <param name="responseBody">The response body to deserialize.</param>
        /// <returns>The deserialized response body, or null if the reponse could not be deserialized (e.g. was empty, or did not contain JSON).</returns>
        protected HttpErrorResponse DeserializeResponseBodyToHttpErrorResponse(Stream responseBody)
        {
            using (var streamReader = new StreamReader(responseBody, defaultEncoding, false, 1024, true))
            using (var jsonReader = new JsonTextReader(streamReader))
            {
                JObject bodyAsJson = null;
                try
                {
                    bodyAsJson = (JObject)JToken.ReadFrom(jsonReader);

                }
                catch (Exception)
                {
                    return null;
                }
                try
                {
                    return errorResponseDeserializer.Deserialize(bodyAsJson);
                }
                catch (DeserializationException)
                {
                    return null;
                }
            }
        }

        /// <summary>
        /// Gets the value of the specified <see cref="HttpErrorResponse"/> attribute.
        /// </summary>
        /// <param name="httpErrorResponse">The <see cref="HttpErrorResponse"/> to retrieve the attribute from.</param>
        /// <param name="attributeKey">The key of the attribute to retrieve.</param>
        /// <returns>The value of the attribute, or a blank string if no attribute with that key exists.</returns>
        protected String GetHttpErrorResponseAttributeValue(HttpErrorResponse httpErrorResponse, String attributeKey)
        {
            foreach (Tuple<String, String> currentAttribute in httpErrorResponse.Attributes)
            {
                if (currentAttribute.Item1 == attributeKey)
                {
                    return currentAttribute.Item2;
                }
            }

            return "";
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
                    if (httpClientInstantiatedInConstructor == true)
                    {
                        httpClient.Dispose();
                    }
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
