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
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Grpc.EventCache.IntegrationTests
{
    /// <summary>
    /// Tests custom gRPC interceptors (e.g. custom error handling).
    /// </summary>
    public class InterceptorTests : IntegrationTestsBase
    {
        [Test]
        public void Todo()
        {
            // TODO: 
            //   Follow the test pattern in ApplicationAccess.Hosting.Rest.ReaderWriter.IntegrationTests.MiddlewareTests
            //     Tests like ArgumentExceptionMappedToHttpErrorResponse()
            //   Will likely want to move this to ApplicationAccess.Hosting.Grpc.DistributedWriter.IntegrationTests (or maybe another project with more core functionality)
            //   Also need to test when 'errorHandlingOptions.OverrideInternalServerErrors' is set to true

            throw new NotImplementedException();
        }
    }
}
