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
using System.Linq;
using System.Reflection;
using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ApplicationAccess.Hosting.Models;

namespace ApplicationAccess.Hosting.Rest.Models
{
    /// <summary>
    /// Container class holding parameters passed to the <see cref="ApplicationInitializer"/> class.
    /// </summary>
    public class ApplicationInitializerParameters : ApplicationInitializerParametersBase
    {
        /// <summary>An action which configures custom JSON serialization on the specified <see cref="JsonOptions"/> object.</summary>
        public Action<JsonOptions> ConfigureJsonOptionsAction { get; set; }

        /// <summary>The version to use in the Swagger definition.</summary>
        public String SwaggerVersionString { get; set; }

        /// <summary>The name of the application to use in the Swagger definition.</summary>
        public String SwaggerApplicationName { get; set; }

        /// <summary>The description of the application to use in the Swagger definition.</summary>
        public String SwaggerApplicationDescription { get; set; }

        /// <summary>A collection of assemblies outside of the assembly for the hosted application, for which to include swagger generation for (typeically used to add swagger generation for classes in the shared ApplicationAccess.Hosting.Rest.Controllers namespace).</summary>
        public IEnumerable<Assembly> SwaggerGenerationAdditionalAssemblies { get; set; }

        /// <summary>A collection of mappings between a type (derived from <see cref="Exception"/>) and a <see cref="System.Net.HttpStatusCode"/> that should be returned when an exception of that type is thrown from a controller method.</summary>
        public IEnumerable<Tuple<Type, HttpStatusCode>> ExceptionToHttpStatusCodeMappings { get; set; }

        /// <summary>A collection of types (derived from <see cref="Exception"/>) which should be mapped to <see cref="HttpErrorResponse">HttpErrorResponses</see> via the standard conversion function.</summary>
        public IEnumerable<Type> ExceptionTypesMappedToStandardHttpErrorResponse { get; set; }

        /// <summary>A collection of mappings between a type (derived from <see cref="Exception"/>) and a custom function which converts that type into an <see cref="HttpErrorResponse"/>.  Each of the functions accepts an <see cref="Exception"/> (although typed as the base <see cref="Exception"/> it's safe to cast it to derived type of the first item in the Tuple), and returns an <see cref="HttpErrorResponse"/> representing the exception.</summary>
        public IEnumerable<Tuple<Type, Func<Exception, HttpErrorResponse>>> ExceptionToCustomHttpErrorResponseGeneratorFunctionMappings { get; set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Rest.Models.ApplicationInitializerParameters class.
        /// </summary>
        public ApplicationInitializerParameters()
            : base()
        {
            ConfigureJsonOptionsAction = null;
            SwaggerVersionString = "";
            SwaggerApplicationName = "";
            SwaggerApplicationDescription = "";
            SwaggerGenerationAdditionalAssemblies = Enumerable.Empty<Assembly>();
            ExceptionToHttpStatusCodeMappings = Enumerable.Empty<Tuple<Type, HttpStatusCode>>();
            ExceptionTypesMappedToStandardHttpErrorResponse = Enumerable.Empty<Type>();
            ExceptionToCustomHttpErrorResponseGeneratorFunctionMappings = Enumerable.Empty<Tuple<Type, Func<Exception, HttpErrorResponse>>>();
        }
    }
}
