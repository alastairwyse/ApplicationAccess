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
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Hosting.Internal;
using ApplicationAccess.Utilities;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// A middleware which can pause/hold any incoming requests until a signal is receieved to unpause/release the requests.
    /// </summary>
    public class RequestPauserMiddleware
    {
        /// <summary>The next middleware in the application middleware pipeline.</summary>
        protected readonly RequestDelegate next;
        /// <summary>The <see cref="IThreadPauser"/> instance which implements the pause functionality.</summary>
        protected readonly IThreadPauser threadPauser;
        /// <summary>A collection of HTTP request paths which should be excluded from pausing.</summary>
        protected readonly HashSet<String> excludedRequestPaths;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.RequestPauserMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="threadPauser">The <see cref="IThreadPauser"/> instance which implements the pause functionality.</param>
        /// <param name="excludedRequestPaths">A collection of HTTP request paths which should be excluded from pausing.</param>
        /// <remarks>Parameter <paramref name="excludedRequestPaths"/> can be used to exclude request paths from being subject to pausing, e.g. paths used for healthcheck/heartbeat, and/or paths which are used to unpause.</remarks>
        public RequestPauserMiddleware(RequestDelegate next, IThreadPauser threadPauser, IEnumerable<String> excludedRequestPaths)
        {
            this.next = next;
            this.threadPauser = threadPauser;
            this.excludedRequestPaths = new HashSet<String>(excludedRequestPaths);
        }

        /// <summary>
        /// Invokes the middleware implementing the request pausing.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> of the current request.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            if (context.Request.Path.HasValue == true)
            {
                if (excludedRequestPaths.Contains(context.Request.Path.Value) == false)
                {
                    threadPauser.TestPaused();
                }
            }
            else
            {
                threadPauser.TestPaused();
            }

            await next(context);
        }
    }

    /// <summary>
    /// Extension methods to add request pausing capabilities to an HTTP application pipeline.
    /// </summary>
    public static class RequestPauserMiddlewareExtensions
    {
        /// <summary>
        /// Adds the <see cref="RequestPauserMiddleware"/> to the <see cref="IApplicationBuilder">IApplicationBuilder's</see> request pipeline.
        /// </summary>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to add the request pausing middleware to.</param>
        /// <param name="threadPauser">The <see cref="IThreadPauser"/> instance which implements the pause functionality.</param>
        /// <param name="excludedRequestPaths">A collection of HTTP request paths which should be excluded from pausing.</param>
        /// <returns>A reference to <paramref name="applicationBuilder"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseRequestPauser(this IApplicationBuilder applicationBuilder, IThreadPauser threadPauser, IEnumerable<String> excludedRequestPaths)
        {
            return applicationBuilder.UseMiddleware<RequestPauserMiddleware>(threadPauser, excludedRequestPaths);
        }
    }
}
