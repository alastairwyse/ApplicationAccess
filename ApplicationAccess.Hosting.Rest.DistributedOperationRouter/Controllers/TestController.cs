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
using System.Net.Http;
using System.Net.Mime;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Distribution;
using ApplicationAccess.Hosting.Rest.Controllers;
using ApplicationAccess.Hosting.Rest.DistributedOperationRouter;
using ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Models;
using ApplicationAccess.Utilities;

namespace ApplicationAccess.Hosting.Rest.DistributedOperationRouter.Controllers
{
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    public class TestController : ControllerBase
    {
        // TODO:

        //   If thread pausers or swicth need to be mocked, will need to wrap them in holder classes
        //   Don't bother with any GroupToGroup Methods
        //   Can we reuse distopcoord controllers OR put gjem into base classes if not already??
        //     We maybe could... it does a few other things
        //       Returning NotFoundExceptions
        //       Returning 201 statuses
        //       If statements
        //     Depends how easily that could be extracted
        //       tbh I think the abstraction would create more code than it would save by just copying the controller
        //   Put UnescapeParameterValues() methods in something common
        // TODO in HostedServiceWrapper
        //   Create a httpclient and insert into a HttpClientShim instance and put in holder
        //     Rehister the httpclient instance for disposal
        //   Create constructor dependencies for DistributedAccessManagerOperationRouter
        //     Including IShardClientManager<TClientConfiguration> preconfigured with the 2x shard details created from config
        //   Create instances to put in any other holder classes in the Hosting.Rest.DistributedOperationRouter namespace

        /// <summary>Configuration of the routings to the shards.</summary>
        protected RouteConfiguration routeConfiguration;
        /// <summary>Instance of <see cref="IAccessManagerAsyncQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> to process incoming requests.</summary>
        protected IAccessManagerAsyncQueryProcessor<String, String, String, String> queryProcessor;
        /// <summary>Instance of <see cref="IAccessManagerAsyncEventProcessor{TUser, TGroup, TComponent, TAccess}"/> to process incoming requests.</summary>
        protected IAccessManagerAsyncEventProcessor<String, String, String, String> eventProcessor;
        /// <summary>Instance of <see cref="IDistributedAccessManagerAsyncQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> to process incoming requests.</summary>
        protected IDistributedAccessManagerAsyncQueryProcessor<String, String, String, String> distributedQueryProcessor;
        /// <summary>A hash code generator for users.</summary>
        protected IHashCodeGenerator<String> userHashCodeGenerator;
        /// <summary>A hash code generator for groups.</summary>
        protected IHashCodeGenerator<String> groupHashCodeGenerator;
        /// <summary>Converts an <see cref="HttpRequest"/> to an <see cref="HttpRequestMessage"/> to implement routing.</summary>
        protected IHttpRequestResponseMessageConverter httpRequestResponseMessageConverter;
        /// <summary>Wraps and abstracts the Send*() methods on an <see cref="HttpClient"/> instance, so they can be mocked in unit tests.</summary>
        protected IHttpClientShim httpClientShim;
        /// <summary>Allows the routing functionality to be switched on and off.</summary>
        protected RoutingSwitch routingSwitch;
        /// <summary>Allows incoming requests to be paused/held and subsequently resumed.</summary>
        protected IThreadPauser threadPauser;
        /// <summary>Logger.</summary>
        protected ILogger<TestController> logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.DistributedOperationRouter.TestController class.
        /// </summary>
        public TestController
        (
            RouteConfigurationHolder routeConfigurationHolder,
            DistributedOperationCoordinatorHolder distributedOperationCoordinatorHolder,
            DistributedAsyncQueryProcessorHolder distributedAsyncQueryProcessorHolder,
            HashCodeGeneratorHolder hashCodeGeneratorHolder,
            HttpRequestResponseMessageConverterHolder httpRequestResponseMessageConverterHolder, 
            HttpClientShimHolder httpClientShimHolder, 
            RoutingSwitch routingSwitch,
            IThreadPauser threadPauser, 
            ILogger<TestController> logger
        )
        {
            routeConfiguration = routeConfigurationHolder.RouteConfiguration;
            queryProcessor = distributedOperationCoordinatorHolder.DistributedOperationCoordinator;
            eventProcessor = distributedOperationCoordinatorHolder.DistributedOperationCoordinator;
            distributedQueryProcessor = distributedAsyncQueryProcessorHolder.AsyncQueryProcessor;
            userHashCodeGenerator = hashCodeGeneratorHolder.UserHashCodeGenerator;
            groupHashCodeGenerator = hashCodeGeneratorHolder.GroupHashCodeGenerator;
            httpRequestResponseMessageConverter = httpRequestResponseMessageConverterHolder.HttpRequestResponseMessageConverter;
            httpClientShim = httpClientShimHolder.HttpClientShim;
            this.routingSwitch = routingSwitch;
            this.threadPauser = threadPauser;
            this.logger = logger;
        }

        [HttpGet]
        [Route("users")]
        [ApiExplorerSettings(GroupName = "UserQueryProcessor")]
        [Produces(MediaTypeNames.Application.Json)]
        public async Task<IEnumerable<String>> GetUsersAsync()
        {
            // TODO: This is wrong
            //   Only do below if routing is on
            //     Could send to protected version of method in AddUserToGroupMappingAsync()


            return await queryProcessor.GetUsersAsync();
        }

        [HttpPost]
        [Route("userToGroupMappings/user/{user}/group/{group}")]
        [ApiExplorerSettings(GroupName = "UserEventProcessor")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public async Task<StatusCodeResult> AddUserToGroupMappingAsync([FromRoute] String user, [FromRoute] String group)
        {
            UnescapeParameterValue(ref user);
            Int32 hashCode = userHashCodeGenerator.GetHashCode(user);
            ThrowExceptionIfHashCodeOutsideConfiguredRange(hashCode, routeConfiguration);
            String uriScheme = routeConfiguration.SourceShardConfiguration.UriScheme;
            String uriHost = routeConfiguration.SourceShardConfiguration.UriHost;
            UInt16 uriPort = routeConfiguration.SourceShardConfiguration.UriPort;
            if (routingSwitch.State == true && hashCode >= routeConfiguration.TargetShardConfiguration.HashRangeStart)
            {
                uriScheme = routeConfiguration.TargetShardConfiguration.UriScheme;
                uriHost = routeConfiguration.TargetShardConfiguration.UriHost;
                uriPort = routeConfiguration.TargetShardConfiguration.UriPort;
            }
            using (var targetReqeust = new HttpRequestMessage())
            {
                httpRequestResponseMessageConverter.ConvertRequest(base.HttpContext.Request, targetReqeust, uriScheme, uriHost, uriPort);
                using (var targetResponse = await httpClientShim.SendAsync(targetReqeust))
                {
                    await httpRequestResponseMessageConverter.ConvertResponseAsync(targetResponse, base.HttpContext.Response);
                }
            }

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        // TODO: Implement these as first step...
        // Task<Boolean> ContainsUserAsync(String user)
        // Task<List<String>> GetGroupToGroupReverseMappingsAsync(IEnumerable<String> groups)
        // Endpoint for routing switch
        // Endpoint for holding

        #region Private/Protected Methods

        /// <summary>
        /// Decodes escaped URL characters in the specified controller parameter value.
        /// </summary>
        /// <param name="parameterValue">The parameter value to decode the characters in.</param>
        protected void UnescapeParameterValue(ref String parameterValue)
        {
            parameterValue = Uri.UnescapeDataString(parameterValue);
        }

        protected void ThrowExceptionIfHashCodeOutsideConfiguredRange(Int32 hashCode, RouteConfiguration routeConfiguration)
        {
            if (hashCode < routeConfiguration.SourceShardConfiguration.HashRangeStart)
                throw new Exception($"Hashcode {hashCode} is less than the configured source node hash code range start value {routeConfiguration.SourceShardConfiguration.HashRangeStart}.");
            if (hashCode > routeConfiguration.TargetShardConfiguration.HashRangeEnd)
                throw new Exception($"Hashcode {hashCode} is greater than the configured target node hash code range end value {routeConfiguration.TargetShardConfiguration.HashRangeEnd}.");
        }

        #endregion
    }
}
