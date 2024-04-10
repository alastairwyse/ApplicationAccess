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
using ApplicationMetrics;

namespace ApplicationAccess.Hosting.Rest.AsyncClient
{
    /// <summary>
    /// Count metric which records sending of a HTTP request resulting in a transient error and being retried.
    /// </summary>
    public class HttpRequestRetried : CountMetric
    {
        #pragma warning disable 1591

        protected static String staticName = "HttpRequestRetried";
        protected static String staticDescription = "Sending of a HTTP request resulting in a transient error and being retried.";

        public HttpRequestRetried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }

        #pragma warning restore 1591
    }
}
