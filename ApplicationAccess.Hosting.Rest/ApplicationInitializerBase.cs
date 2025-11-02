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
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using ApplicationAccess.Hosting.Rest.Models;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// A base for classes which provide common Initialization routines for hosted ApplicationAccess components.
    /// </summary>
    /// <typeparam name="TParameters">The type of parameters used to initialize the component.</typeparam>
    public abstract class ApplicationInitializerBase<TParameters> where TParameters: ApplicationInitializerParametersBase
    {
        /// <summary>The ASP.NET core environment variable to use for integration testing of the component.</summary>
        public static readonly String IntegrationTestingEnvironmentName = "IntegrationTesting";

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.ApplicationInitializerBase class.
        /// </summary>
        public ApplicationInitializerBase()
        {
        }

        /// <summary>
        /// Initializes the component.
        /// </summary>
        /// <typeparam name="THostedService">The type of the hosted service which underlies the component, and should be registered using the <see cref="ServiceCollectionHostedServiceExtensions.AddHostedService{THostedService}(IServiceCollection)"/> method.</typeparam>
        /// <param name="parameters">A collection of parameters used to initialize the component.</param>
        /// <returns>A <see cref="WebApplication"/> initialized and ready to host the component.</returns>
        public abstract WebApplication Initialize<THostedService>(TParameters parameters)
            where THostedService : class, IHostedService;
    }
}
