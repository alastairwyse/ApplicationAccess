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
using Microsoft.AspNetCore.Builder;
using ApplicationAccess.Hosting.Grpc.Models;
using ApplicationAccess.Hosting.Rest;

namespace ApplicationAccess.Hosting.Grpc
{
    /// <summary>
    /// Provides common Initialization routines for ApplicationAccess components hosted as gRPC services.
    /// </summary>
    public class ApplicationInitializer : ApplicationInitializerBase<ApplicationInitializerParameters>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Grpc.ApplicationInitializer class.
        /// </summary>
        public ApplicationInitializer()
            : base()
        {
        }

        /// <inheritdoc/>
        public override WebApplication Initialize<THostedService>(ApplicationInitializerParameters parameters)
        {
            // TODO: Copy from REST version
            //   Will need placeholders for things not yet done like Tripswitch
            //   Then implement in EventCache Program.cs
            //   Move everything from Program.cs to here that's common in all gRPC service (e.g. call to UseGrrpc() method)
            //   Then return to InterceptorTests and complete them
            //   Then add Tripswicth, kube liveliness probe etc
        }
    }
}
