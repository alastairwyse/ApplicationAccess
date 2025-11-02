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
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace ApplicationAccess.Hosting.Rest.Models
{
    /// <summary>
    /// Base for container classes holding parameters passed to subclasses of the <see cref="ApplicationInitializerBase{TParameters}"/> class.
    /// </summary>
    public abstract class ApplicationInitializerParametersBase
    {
        /// <summary>Startup arguments/parameters passed to the hosted component.</summary>
        public String[] Args { get; set; }

        /// <summary>An action which configures adding, binding, and validation of <see cref="IOptions{TOptions}"/> instances on the specified web application builder.</summary>
        public Action<WebApplicationBuilder> ConfigureOptionsAction { get; set; }

        /// <summary>A collection of types of 'processor holder' classes (e.g. <see cref="UserEventProcessorHolder"/>) to be registered in dependency injection.</summary>
        public IEnumerable<Type> ProcessorHolderTypes { get; set; }

        /// <summary>An action which allows custom service configuration and registration.</summary>
        public Action<IServiceCollection> ConfigureServicesAction { get; set; }

        /// <summary>The exception to throw on receiving any requests after the switch has been tripped, when <see cref="TripSwitchMiddleware"/> is enabled.  <see cref="TripSwitchMiddleware"/> will not be used if not set.</summary>
        public Exception TripSwitchTrippedException { get; set; }

        /// <summary>An action which allows configuring the specified <see cref="IApplicationBuilder"/>.</summary>
        public Action<IApplicationBuilder> ConfigureApplicationBuilderAction { get; set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Models.ApplicationInitializerParametersBase class.
        /// </summary>
        public ApplicationInitializerParametersBase()
        {
            ConfigureOptionsAction = (WebApplicationBuilder builder) => { };
            ProcessorHolderTypes = Enumerable.Empty<Type>();
            ConfigureServicesAction = (IServiceCollection serviceCollection) => { };
            TripSwitchTrippedException = null;
            ConfigureApplicationBuilderAction = (IApplicationBuilder applicationBuilder) => { };
        }
    }
}
