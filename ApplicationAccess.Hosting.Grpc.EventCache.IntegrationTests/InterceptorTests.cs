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
using ApplicationAccess.Hosting.Rest.Utilities;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using NSubstitute;
using NSubstitute.ClearExtensions;
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
            //   Also need to test when 'errorHandlingOptions.OverrideInternalServerErrors' is set to true
            //   Ensure that by default I've covered all the same exceptions as the REST version does
            //   Need to test AppAccess specific testing being mapped aswell...
            //     Rest middelware tests have UserNotFoundExceptionMappedToHttpErrorResponse(), and others...
            //       FYI, these handlers are added in the REST ApplicationInitializer... SO NEED TO DO SAME IN GRPC VERSION
            //     Will need this to, but not in generic namespace... should put these tests in the Writer RpcTests
            //     Will also need tripswitch tests in the same test project as above.

            throw new NotImplementedException();
        }

        #region Exception Mapping Tests

        // These tests check that the ExceptionHandlingInterceptor class converts exceptions to gRPC Status objects, and that those objects are converted back to exceptions within
        //   the AccessManagerClientBase class, and re-thrown.

        [Test]
        public void ExceptionMappedToGrpcError()
        {
            var priorEventdId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");
            var exceptionMessage = "Test exception message.";
            var mockException = new Exception(exceptionMessage);
            mockTemporalEventQueryProcessor.ClearSubstitute(ClearOptions.All);
            mockTemporalEventQueryProcessor.When((processor) => processor.GetAllEventsSince(priorEventdId)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                grpcClient.GetAllEventsSince(priorEventdId);
            });

            Assert.That(e.Message, Does.StartWith(exceptionMessage));
        }

        [Test]
        public void ArgumentExceptionMappedToGrpcError()
        {
            var priorEventdId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");
            var exceptionMessage = "Test exception message.";
            var parameterName = "testParamName";
            var mockException = new ArgumentException(exceptionMessage, parameterName);
            mockTemporalEventQueryProcessor.ClearSubstitute(ClearOptions.All);
            mockTemporalEventQueryProcessor.When((processor) => processor.GetAllEventsSince(priorEventdId)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                grpcClient.GetAllEventsSince(priorEventdId);
            });

            Assert.That(e.Message, Does.StartWith(exceptionMessage));
            Assert.AreEqual(parameterName, e.ParamName);


            mockException = new ArgumentException(exceptionMessage);

            e = Assert.Throws<ArgumentException>(delegate
            {
                grpcClient.GetAllEventsSince(priorEventdId);
            });

            Assert.That(e.Message, Does.StartWith(exceptionMessage));
            Assert.IsNull(e.ParamName);
        }

        [Test]
        public void ArgumentOutOfRangeExceptionMappedToGrpcError()
        {
            var priorEventdId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");
            var exceptionMessage = "Test exception message.";
            var parameterName = "testParamName";
            var mockException = new ArgumentOutOfRangeException(parameterName, exceptionMessage);
            mockTemporalEventQueryProcessor.ClearSubstitute(ClearOptions.All);
            mockTemporalEventQueryProcessor.When((processor) => processor.GetAllEventsSince(priorEventdId)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                grpcClient.GetAllEventsSince(priorEventdId);
            });

            Assert.That(e.Message, Does.StartWith(exceptionMessage));
            Assert.AreEqual(parameterName, e.ParamName);


            mockException = new ArgumentOutOfRangeException(parameterName);

            e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                grpcClient.GetAllEventsSince(priorEventdId);
            });

            Assert.AreEqual(parameterName, e.ParamName);
        }

        [Test]
        public void ArgumentNullExceptionMappedToGrpcError()
        {
            var priorEventdId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");
            var exceptionMessage = "Test exception message.";
            var parameterName = "testParamName";
            var mockException = new ArgumentNullException(parameterName, exceptionMessage);
            mockTemporalEventQueryProcessor.ClearSubstitute(ClearOptions.All);
            mockTemporalEventQueryProcessor.When((processor) => processor.GetAllEventsSince(priorEventdId)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<ArgumentNullException>(delegate
            {
                grpcClient.GetAllEventsSince(priorEventdId);
            });

            Assert.That(e.Message, Does.StartWith(exceptionMessage));
            Assert.AreEqual(parameterName, e.ParamName);


            mockException = new ArgumentNullException(parameterName);

            e = Assert.Throws<ArgumentNullException>(delegate
            {
                grpcClient.GetAllEventsSince(priorEventdId);
            });

            Assert.AreEqual(parameterName, e.ParamName);
        }

        [Test]
        public void IndexOutOfRangeExceptionMappedToGrpcError()
        {
            var priorEventdId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");
            var exceptionMessage = "Test exception message.";
            var mockException = new IndexOutOfRangeException(exceptionMessage);
            mockTemporalEventQueryProcessor.ClearSubstitute(ClearOptions.All);
            mockTemporalEventQueryProcessor.When((processor) => processor.GetAllEventsSince(priorEventdId)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<IndexOutOfRangeException>(delegate
            {
                grpcClient.GetAllEventsSince(priorEventdId);
            });

            Assert.That(e.Message, Does.StartWith(exceptionMessage));
        }

        [Test]
        public void AggregateExceptionMappedToGrpcError()
        {
            var priorEventdId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");
            var innerException1 = new ArgumentException("Argument inner exception", "innerExceptionParamName");
            var innerException2 = new Exception("Plain inner exception");
            var exceptionMessage = "Test exception message.";
            var mockException = new AggregateException(exceptionMessage, innerException1, innerException2);
            mockTemporalEventQueryProcessor.ClearSubstitute(ClearOptions.All);
            mockTemporalEventQueryProcessor.When((processor) => processor.GetAllEventsSince(priorEventdId)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<AggregateException>(delegate
            {
                grpcClient.GetAllEventsSince(priorEventdId);
            });

            Assert.That(e.Message, Does.StartWith(exceptionMessage));
            Assert.NotNull(e.InnerException);
            Assert.IsInstanceOf<Exception>(e.InnerException);
            Exception resultInnerException = (Exception)e.InnerException;
            Assert.That(resultInnerException.Message, Does.StartWith("The AggregateException contained the following exception types and messages as inner exceptions: ArgumentException, 'Argument inner exception (Parameter 'innerExceptionParamName')'; Exception, 'Plain inner exception'; "));
        }

        [Test]
        public void AggregateExceptionMappedToGrpcError_NoInnerExceptions()
        {
            var priorEventdId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");
            var exceptionMessage = "Test exception message.";
            var mockException = new AggregateException(exceptionMessage);
            mockTemporalEventQueryProcessor.ClearSubstitute(ClearOptions.All);
            mockTemporalEventQueryProcessor.When((processor) => processor.GetAllEventsSince(priorEventdId)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<AggregateException>(delegate
            {
                grpcClient.GetAllEventsSince(priorEventdId);
            });

            Assert.That(e.Message, Does.StartWith(exceptionMessage));
            Assert.IsNull(e.InnerException);
        }

        [Test]
        public void NotFoundExceptionMappedToGrpcError()
        {
            var priorEventdId = Guid.Parse("a13ec1a2-e0ef-473c-96be-1e5f33ec5d45");
            var exceptionMessage = "Test exception message.";
            var resourceId = "user1";
            var mockException = new NotFoundException(exceptionMessage, resourceId);
            mockTemporalEventQueryProcessor.ClearSubstitute(ClearOptions.All);
            mockTemporalEventQueryProcessor.When((processor) => processor.GetAllEventsSince(priorEventdId)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<NotFoundException>(delegate
            {
                grpcClient.GetAllEventsSince(priorEventdId);
            });

            Assert.That(e.Message, Does.StartWith(exceptionMessage));
            Assert.AreEqual(resourceId, e.ResourceId);
        }

        #endregion
    }
}
