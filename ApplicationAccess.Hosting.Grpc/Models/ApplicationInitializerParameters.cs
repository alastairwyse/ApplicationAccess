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
using Google.Rpc;
using ApplicationAccess.Hosting.Models;
using ApplicationAccess.Hosting.Rest.Models;

namespace ApplicationAccess.Hosting.Grpc.Models
{
    /// <summary>
    /// Container class holding parameters passed to the <see cref="ApplicationInitializer"/> class.
    /// </summary>
    public class ApplicationInitializerParameters : ApplicationInitializerParametersBase
    {
        /// <summary>A collection of types (derived from <see cref="Exception"/>) which should be mapped to <see cref="Status">Statuses</see> via the standard conversion function.</summary>
        public IEnumerable<Type> ExceptionTypesMappedToStandardGrpcStatuses { get; set; }

        /// <summary>A collection of mappings between a type (derived from <see cref="Exception"/>) and a custom function which converts that type into a <see cref="Status"/>.  Each of the functions accepts an <see cref="Exception"/> (although typed as the base <see cref="Exception"/> it's safe to cast it to derived type of the first item in the Tuple), and returns a <see cref="Status"/> representing the exception.</summary>
        public IEnumerable<Tuple<Type, Func<Exception, Status>>> ExceptionToCustomGrpcStatusGeneratorFunctionMappings { get; set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.Models.ApplicationInitializerParameters class.
        /// </summary>
        public ApplicationInitializerParameters()
            : base()
        {
            ExceptionTypesMappedToStandardGrpcStatuses = Enumerable.Empty<Type>();
            ExceptionToCustomGrpcStatusGeneratorFunctionMappings = Enumerable.Empty<Tuple<Type, Func<Exception, Status>>>();
        }
    }
}
