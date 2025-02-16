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
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using ApplicationAccess.Utilities;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// A middleware which maintains a count of the requests currently being processed by the ASP.NET instance.
    /// </summary>
    public class RequestCounterMiddleware
    {
        /// <summary>The next middleware in the application middleware pipeline.</summary>
        protected readonly RequestDelegate next;
        /// <summary>The counter which maintains the count of requests.</summary>
        protected readonly ThreadSafeCounter counter;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.RequestCounterMiddleware class.
        /// </summary>
        /// <param name="next">The next middleware in the application middleware pipeline.</param>
        /// <param name="counter">The counter which maintains the count of requests.</param>
        public RequestCounterMiddleware(RequestDelegate next, ThreadSafeCounter counter)
        {
            this.next = next;
            this.counter = counter;
        }

        /// <summary>
        /// Invokes the middleware implementing the request counting.
        /// </summary>
        /// <param name="context">The <see cref="HttpContext"/> of the current request.</param>
        public async Task InvokeAsync(HttpContext context)
        {
            counter.Increment();
            try
            {
                await next(context);
            }
            finally
            {
                counter.Decrement();
            }
        }
    }

    /// <summary>
    /// Extension methods to add request counting capabilities to an HTTP application pipeline.
    /// </summary>
    public static class RequestCounterMiddlewareExtensions
    {
        /// <summary>
        /// Adds <see cref="RequestCounterMiddleware"/> to the <see cref="IApplicationBuilder">IApplicationBuilder's</see> request pipeline.
        /// </summary>
        /// <param name="applicationBuilder">The <see cref="IApplicationBuilder"/> to add the request counting middleware to.</param>
        /// <param name="counter">The counter which maintains the count of requests.</param>
        /// <returns>A reference to <paramref name="applicationBuilder"/> after the operation has completed.</returns>
        public static IApplicationBuilder UseRequestCounter(this IApplicationBuilder applicationBuilder, ThreadSafeCounter counter)
        {
            return applicationBuilder.UseMiddleware<RequestCounterMiddleware>(counter);
        }
    }
}
