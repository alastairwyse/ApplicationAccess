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
using k8s.Models;

namespace ApplicationAccess.Redistribution.Kubernetes
{
    /// <summary>
    /// The exception that is thrown when a timeout expired waiting for a predicate executed against a Kubernetes <see cref="V1Deployment"/> to return true.
    /// </summary>
    public class DeploymentPredicateWaitTimeoutExpiredException : Exception
    {
        /// <summary>
        /// The timeout value in milliseconds.
        /// </summary>
        public Int32 Timeout { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Redistribution.Kubernetes.DeploymentPredicateWaitTimeoutExpiredException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="timeout">The timeout value in milliseconds.</param>
        public DeploymentPredicateWaitTimeoutExpiredException(String message, Int32 timeout)
            : base(message)
        {
            Timeout = timeout;
        }
    }
}
