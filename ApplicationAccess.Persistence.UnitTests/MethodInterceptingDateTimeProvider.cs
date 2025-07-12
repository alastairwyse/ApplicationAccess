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
using ApplicationAccess.Utilities;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// An implementation of <see cref="IDateTimeProvider"/> which allows interception of method calls via a call to <see cref="IMethodCallInterceptor.Intercept">IMethodCallInterceptor.Intercept()</see>, and subsequently calls the equivalent method on another instance of <see cref="IDateTimeProvider"/>.
    /// </summary>
    public class MethodInterceptingDateTimeProvider : IDateTimeProvider
    {
        /// <summary>A mock of IMethodCallInterceptor (for intercepting method calls).</summary>
        protected IMethodCallInterceptor interceptor;
        /// <summary>An instance of IDateTimeProvider to perform the actual functionality.</summary>
        protected IDateTimeProvider dateTimeProvider;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.UnitTests.MethodInterceptingDateTimeProvider class.
        /// </summary>
        /// <param name="interceptor">A mock of IMethodCallInterceptor (for intercepting method calls).</param>
        /// <param name="dateTimeProvider">An instance of IDateTimeProvider to perform the actual functionality.</param>
        public MethodInterceptingDateTimeProvider(IMethodCallInterceptor interceptor, IDateTimeProvider dateTimeProvider)
        {
            this.interceptor = interceptor;
            this.dateTimeProvider = dateTimeProvider;
        }

        public DateTime UtcNow()
        {
            interceptor.Intercept();
            return dateTimeProvider.UtcNow();
        }
    }
}
