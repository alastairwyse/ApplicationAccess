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
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using ApplicationAccess.Hosting.Rest;

namespace ApplicationAccess.Hosting.Grpc
{
    /// <summary>
    /// Implements <see cref="IHealthCheck"/> by checking the state of a <see cref="TripSwitchActuator"/>.
    /// </summary>
    public class TripSwitchHealthCheck : IHealthCheck
    {
        /// <summary>The actuator for the trip switch.</summary>
        protected readonly TripSwitchActuator actuator;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.TripSwitchHealthCheck class.
        /// </summary>
        /// <param name="actuator">The actuator for the trip switch.</param>
        public TripSwitchHealthCheck(TripSwitchActuator actuator)
        {
            this.actuator = actuator;
        }

        #pragma warning disable 1998

        /// <inheritdoc/>
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            if (actuator.IsActuated == false)
            {
                return new HealthCheckResult(HealthStatus.Healthy);
            }
            else
            {
                return new HealthCheckResult(HealthStatus.Unhealthy);
            }
        }
        
        #pragma warning restore 1998
    }
}
