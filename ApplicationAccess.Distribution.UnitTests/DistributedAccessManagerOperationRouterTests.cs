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
using System.Linq.Expressions;
using System.Threading.Tasks;
using ApplicationAccess.Distribution.Metrics;
using ApplicationAccess.Distribution.Models;
using ApplicationAccess.Hosting.Rest.DistributedAsyncClient;
using ApplicationAccess.Metrics;
using ApplicationAccess.Utilities;
using ApplicationAccess.UnitTests;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;
using NSubstitute.ClearExtensions;

namespace ApplicationAccess.Distribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Distribution.DistributedAccessManagerOperationRouter class.
    /// </summary>
    public class DistributedAccessManagerOperationRouterTests
    {
        // Many methods in DistributedAccessManagerOperationRouter are implemented using overloads of common method ImplementRoutingAsync().  All paths of
        //   ImplementRoutingAsync() overloads are tested explicitly in this test class.  For methods that call these overloads (like ContainsUserAsync())
        //   since most/all of the functionality is handled within ImplementRoutingAsync(), all permutations of state and parameters (routing on/off, differing
        //   hash code values, etc...) are NOT explicitly tested.  Methods with bespoke implementations (e.g. GetUsersAsync()) do have multiple tests covering
        //   all paths in the bespoke code.

        private TestUtilities testUtilities;
        private Int32 sourceShardHashRangeStart;
        private Int32 sourceShardHashRangeEnd;
        private Int32 targetShardHashRangeStart;
        private Int32 targetShardHashRangeEnd;
        private IDistributedAccessManagerAsyncClient<String, String, String, String> mockSourceQueryShardClient;
        private IDistributedAccessManagerAsyncClient<String, String, String, String> mockSourceEventShardClient;
        private IDistributedAccessManagerAsyncClient<String, String, String, String> mockTargetQueryShardClient;
        private IDistributedAccessManagerAsyncClient<String, String, String, String> mockTargetEventShardClient;
        private IShardClientManager<AccessManagerRestClientConfiguration> mockShardClientManager;
        private IHashCodeGenerator<String> mockUserHashCodeGenerator;
        private IHashCodeGenerator<String> mockGroupHashCodeGenerator;
        private IThreadPauser mockThreadPauser;
        private IMetricLogger mockMetricLogger;
        private DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration> testUserOperationRouter;
        private DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration> testGroupOperationRouter;

        [SetUp]
        protected void SetUp()
        {
            testUtilities = new TestUtilities();
            sourceShardHashRangeStart = -100;
            sourceShardHashRangeEnd = -5;
            targetShardHashRangeStart = -4;
            targetShardHashRangeEnd = 96;
            mockSourceQueryShardClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            mockSourceEventShardClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            mockTargetQueryShardClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            mockTargetEventShardClient = Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>();
            mockShardClientManager = Substitute.For<IShardClientManager<AccessManagerRestClientConfiguration>>();
            mockUserHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockGroupHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockThreadPauser = Substitute.For<IThreadPauser>();
            mockMetricLogger = Substitute.For<IMetricLogger>();

            // Mock constructor method calls which setup shard client fields within the router
            var sourceQueryShardClientAndDescription = new DistributedClientAndShardDescription
            (
                mockSourceQueryShardClient,
                "SourceQueryShardClientAndDescription"
            );
            var sourceEventShardClientAndDescription = new DistributedClientAndShardDescription
            (
                mockSourceEventShardClient,
                "SourceEventShardClientAndDescription"
            );
            var targetQueryShardClientAndDescription = new DistributedClientAndShardDescription
            (
                mockTargetQueryShardClient,
                "TargetQueryShardClientAndDescription"
            );
            var TargetEventShardClientAndDescription = new DistributedClientAndShardDescription
            (
                mockTargetEventShardClient,
                "TargetEventShardClientAndDescription"
            );
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, sourceShardHashRangeStart).Returns(sourceQueryShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, sourceShardHashRangeStart).Returns(sourceEventShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, targetShardHashRangeStart).Returns(targetQueryShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, targetShardHashRangeStart).Returns(TargetEventShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, sourceShardHashRangeStart).Returns(sourceQueryShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, sourceShardHashRangeStart).Returns(sourceEventShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, targetShardHashRangeStart).Returns(targetQueryShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, targetShardHashRangeStart).Returns(TargetEventShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, sourceShardHashRangeEnd).Returns(sourceQueryShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, sourceShardHashRangeEnd).Returns(sourceEventShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, targetShardHashRangeEnd).Returns(targetQueryShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, targetShardHashRangeEnd).Returns(TargetEventShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, sourceShardHashRangeEnd).Returns(sourceQueryShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, sourceShardHashRangeEnd).Returns(sourceEventShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Query, targetShardHashRangeEnd).Returns(targetQueryShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.Group, Operation.Event, targetShardHashRangeEnd).Returns(TargetEventShardClientAndDescription);

            testUserOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
            (
                sourceShardHashRangeStart,
                sourceShardHashRangeEnd,
                targetShardHashRangeStart,
                targetShardHashRangeEnd, 
                DataElement.User,
                mockShardClientManager,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockThreadPauser, 
                true,
                mockMetricLogger
            );
            testGroupOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
            (
                sourceShardHashRangeStart,
                sourceShardHashRangeEnd,
                targetShardHashRangeStart,
                targetShardHashRangeEnd,
                DataElement.Group,
                mockShardClientManager,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockThreadPauser,
                true,
                mockMetricLogger
            );
            mockShardClientManager.ClearReceivedCalls();
        }

        [Test] 
        public void Constructor_SourceShardHashRangeEndParameterLessThanSourceShardHashRangeStart()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testUserOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
                (
                    sourceShardHashRangeStart,
                    sourceShardHashRangeEnd,
                    4,
                    3,
                    DataElement.User,
                    mockShardClientManager,
                    mockUserHashCodeGenerator,
                    mockGroupHashCodeGenerator,
                    mockThreadPauser,
                    true,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'targetShardHashRangeEnd' with value 3 must be greater than or equal to the value 4 of parameter 'targetShardHashRangeStart'."));
            Assert.AreEqual("targetShardHashRangeEnd", e.ParamName);
        }

        [Test]
        public void Constructor_ShardDataElementParameterInvalid()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                testUserOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
                (
                    sourceShardHashRangeStart,
                    sourceShardHashRangeEnd,
                    targetShardHashRangeStart,
                    targetShardHashRangeEnd,
                    DataElement.GroupToGroupMapping,
                    mockShardClientManager,
                    mockUserHashCodeGenerator,
                    mockGroupHashCodeGenerator,
                    mockThreadPauser,
                    true,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Value 'GroupToGroupMapping' in parameter 'shardDataElement' is not valid."));
            Assert.AreEqual("shardDataElement", e.ParamName);
        }

        [Test]
        public void Constructor_ShardHashRangesNotContiguous()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testUserOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
                (
                    0,
                    100,
                    102,
                    200,
                    DataElement.User,
                    mockShardClientManager,
                    mockUserHashCodeGenerator,
                    mockGroupHashCodeGenerator,
                    mockThreadPauser,
                    true,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'targetShardHashRangeStart' with value 102 must be contiguous with parameter 'sourceShardHashRangeEnd' with value 100."));
            Assert.AreEqual("targetShardHashRangeStart", e.ParamName);
        }

        [Test]
        public void Constructor_TargetShardHashRangeEndParameterLessThanTargetShardHashRangeStart()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testUserOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
                (
                    2,
                    1,
                    targetShardHashRangeStart,
                    targetShardHashRangeEnd,
                    DataElement.User,
                    mockShardClientManager,
                    mockUserHashCodeGenerator,
                    mockGroupHashCodeGenerator,
                    mockThreadPauser,
                    true,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'sourceShardHashRangeEnd' with value 1 must be greater than or equal to the value 2 of parameter 'sourceShardHashRangeStart'."));
            Assert.AreEqual("sourceShardHashRangeEnd", e.ParamName);
        }

        [Test]
        public void Constructor_ParameterShardClientManagerDoesntContainRequiredClients()
        {
            var mockException = new Exception("Mock exception");
            var sourceQueryShardClientAndDescription = new DistributedClientAndShardDescription
            (
                mockSourceQueryShardClient,
                "SourceQueryShardClientAndDescription"
            );
            var sourceEventShardClientAndDescription = new DistributedClientAndShardDescription
            (
                mockSourceEventShardClient,
                "SourceEventShardClientAndDescription"
            );
            var targetQueryShardClientAndDescription = new DistributedClientAndShardDescription
            (
                mockTargetQueryShardClient,
                "RargetQueryShardClientAndDescription"
            );
            var TargetEventShardClientAndDescription = new DistributedClientAndShardDescription
            (
                mockTargetEventShardClient,
                "TargetEventShardClientAndDescription"
            );
            mockShardClientManager.When((shardClientManager) => shardClientManager.GetClient(DataElement.User, Operation.Query, sourceShardHashRangeStart)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testUserOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
                (
                    sourceShardHashRangeStart,
                    sourceShardHashRangeEnd,
                    targetShardHashRangeStart,
                    targetShardHashRangeEnd,
                    DataElement.User,
                    mockShardClientManager,
                    mockUserHashCodeGenerator,
                    mockGroupHashCodeGenerator,
                    mockThreadPauser,
                    true,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to retrieve shard client for DataElement 'User', Operation 'Query', and hash code {sourceShardHashRangeStart} from parameter 'shardClientManager'."));
            Assert.AreEqual("shardClientManager", e.ParamName);
            Assert.AreSame(mockException, e.InnerException);


            mockShardClientManager.ClearSubstitute();
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, sourceShardHashRangeStart).Returns(sourceQueryShardClientAndDescription);
            mockShardClientManager.When((shardClientManager) => shardClientManager.GetClient(DataElement.User, Operation.Event, sourceShardHashRangeStart)).Do((callInfo) => throw mockException);

            e = Assert.Throws<ArgumentException>(delegate
            {
                testUserOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
                (
                    sourceShardHashRangeStart,
                    sourceShardHashRangeEnd,
                    targetShardHashRangeStart,
                    targetShardHashRangeEnd,
                    DataElement.User,
                    mockShardClientManager,
                    mockUserHashCodeGenerator,
                    mockGroupHashCodeGenerator,
                    mockThreadPauser,
                    true,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to retrieve shard client for DataElement 'User', Operation 'Event', and hash code {sourceShardHashRangeStart} from parameter 'shardClientManager'."));
            Assert.AreEqual("shardClientManager", e.ParamName);
            Assert.AreSame(mockException, e.InnerException);


            mockShardClientManager.ClearSubstitute();
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, sourceShardHashRangeStart).Returns(sourceQueryShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, sourceShardHashRangeStart).Returns(sourceEventShardClientAndDescription);
            mockShardClientManager.When((shardClientManager) => shardClientManager.GetClient(DataElement.User, Operation.Query, targetShardHashRangeStart)).Do((callInfo) => throw mockException);

            e = Assert.Throws<ArgumentException>(delegate
            {
                testUserOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
                (
                    sourceShardHashRangeStart,
                    sourceShardHashRangeEnd,
                    targetShardHashRangeStart,
                    targetShardHashRangeEnd,
                    DataElement.User,
                    mockShardClientManager,
                    mockUserHashCodeGenerator,
                    mockGroupHashCodeGenerator,
                    mockThreadPauser,
                    true,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to retrieve shard client for DataElement 'User', Operation 'Query', and hash code {targetShardHashRangeStart} from parameter 'shardClientManager'."));
            Assert.AreEqual("shardClientManager", e.ParamName);
            Assert.AreSame(mockException, e.InnerException);


            mockShardClientManager.ClearSubstitute();
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, sourceShardHashRangeStart).Returns(sourceQueryShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, sourceShardHashRangeStart).Returns(sourceEventShardClientAndDescription);
            mockShardClientManager.GetClient(DataElement.User, Operation.Event, targetShardHashRangeStart).Returns(TargetEventShardClientAndDescription);
            mockShardClientManager.When((shardClientManager) => shardClientManager.GetClient(DataElement.User, Operation.Event, targetShardHashRangeStart)).Do((callInfo) => throw mockException);

            e = Assert.Throws<ArgumentException>(delegate
            {
                testUserOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
                (
                    sourceShardHashRangeStart,
                    sourceShardHashRangeEnd,
                    targetShardHashRangeStart,
                    targetShardHashRangeEnd,
                    DataElement.User,
                    mockShardClientManager,
                    mockUserHashCodeGenerator,
                    mockGroupHashCodeGenerator,
                    mockThreadPauser,
                    true,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to retrieve shard client for DataElement 'User', Operation 'Event', and hash code {targetShardHashRangeStart} from parameter 'shardClientManager'."));
            Assert.AreEqual("shardClientManager", e.ParamName);
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void Constructor_ShardClientNotFoundForShardHashRangeEndParameter()
        {
            var mockException = new Exception("Mock exception");
            mockShardClientManager.When((shardClientManager) => shardClientManager.GetClient(DataElement.User, Operation.Query, sourceShardHashRangeEnd)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testUserOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
                (
                    sourceShardHashRangeStart,
                    sourceShardHashRangeEnd,
                    targetShardHashRangeStart,
                    targetShardHashRangeEnd,
                    DataElement.User,
                    mockShardClientManager,
                    mockUserHashCodeGenerator,
                    mockGroupHashCodeGenerator,
                    mockThreadPauser,
                    true,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Failed to retrieve shard client for DataElement 'User', Operation 'Query', and hash code {sourceShardHashRangeEnd} from parameter 'sourceShardHashRangeEnd'."));
            Assert.AreEqual("sourceShardHashRangeEnd", e.ParamName);
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void Constructor_ShardClientsForHashRangeStartAndEndDiffer()
        {
            var hashRangeEndShardClientAndDescription = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "HashRangeEndShardClientAndDescription"
            );
            mockShardClientManager.GetClient(DataElement.User, Operation.Query, sourceShardHashRangeEnd).Returns(hashRangeEndShardClientAndDescription);

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testUserOperationRouter = new DistributedAccessManagerOperationRouter<AccessManagerRestClientConfiguration>
                (
                    sourceShardHashRangeStart,
                    sourceShardHashRangeEnd,
                    targetShardHashRangeStart,
                    targetShardHashRangeEnd,
                    DataElement.User,
                    mockShardClientManager,
                    mockUserHashCodeGenerator,
                    mockGroupHashCodeGenerator,
                    mockThreadPauser,
                    true,
                    mockMetricLogger
                );
            });

            Assert.That(e.Message, Does.StartWith($"Parameter 'sourceShardHashRangeEnd' with value {sourceShardHashRangeEnd} returns shard client with description 'HashRangeEndShardClientAndDescription' for DataElement 'User' and Operation 'Query', but shard client returned for the equivalent hash range start in parameter 'sourceShardHashRangeStart' has differing description 'SourceQueryShardClientAndDescription'."));
            Assert.AreEqual("sourceShardHashRangeEnd", e.ParamName);
        }

        [Test]
        public void RoutingOnProperty()
        {
            testUserOperationRouter.RoutingOn = true;

            Assert.IsTrue(testUserOperationRouter.RoutingOn);
            mockMetricLogger.Received(1).Increment(Arg.Any<RoutingSwitchedOn>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, mockThreadPauser.ReceivedCalls().Count());


            mockMetricLogger.ClearReceivedCalls();

            testUserOperationRouter.RoutingOn = false;

            Assert.IsFalse(testUserOperationRouter.RoutingOn);
            mockMetricLogger.Received(1).Increment(Arg.Any<RoutingSwitchedOff>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(0, mockThreadPauser.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetUsersAsync_RoutingOff()
        {
            var returnUsers = new List<String>()
            {
                "user1",
                "user2",
                "user3"
            };
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.GetUsersAsync().Returns(returnUsers);

            List<String> result = await testUserOperationRouter.GetUsersAsync();

            Assert.AreSame(returnUsers, result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).GetUsersAsync();
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetUsersAsync_RoutingOn()
        {
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            var userClient3ReturnUsers = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(userClient3ReturnUsers));

            List<String> result = await testUserOperationRouter.GetUsersAsync();

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetUsersAsync();
            await userShardClientAndDescription2.Client.Received(1).GetUsersAsync();
            await userShardClientAndDescription3.Client.Received(1).GetUsersAsync();
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetUsersAsync_RoutingOnExceptionWhenReadingUserShard()
        {
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetUsersAsync().Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetUsersAsync().Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.GetUsersAsync();
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).GetUsersAsync();
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve users from shard with configuration 'UserShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetGroupsAsync_RoutingOff()
        {
            var returnGroups = new List<String>()
            {
                "group1",
                "group2",
                "group3"
            };
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.GetGroupsAsync().Returns(returnGroups);

            List<String> result = await testUserOperationRouter.GetGroupsAsync();

            Assert.AreSame(returnGroups, result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).GetGroupsAsync();
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetGroupsAsync_RoutingOnReadingUserShards()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var userClient2ReturnGroups = new List<String>()
            {
                "group2",
                "group3"
            };
            var userClient3ReturnGroups = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(userClient1ReturnGroups));
            userShardClientAndDescription2.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(userClient2ReturnGroups));
            userShardClientAndDescription3.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(userClient3ReturnGroups));

            List<String> result = await testUserOperationRouter.GetGroupsAsync();

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetGroupsAsync();
            await userShardClientAndDescription2.Client.Received(1).GetGroupsAsync();
            await userShardClientAndDescription3.Client.Received(1).GetGroupsAsync();
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetGroupsAsync_RoutingOnReadingGroupShards()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            var groupClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var groupClient2ReturnGroups = new List<String>()
            {
                "group2",
                "group3"
            };
            var groupClient3ReturnGroups = new List<String>();
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(userClients);
            groupShardClientAndDescription1.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(groupClient1ReturnGroups));
            groupShardClientAndDescription2.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(groupClient2ReturnGroups));
            groupShardClientAndDescription3.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(groupClient3ReturnGroups));

            List<String> result = await testGroupOperationRouter.GetGroupsAsync();

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).GetGroupsAsync();
            await groupShardClientAndDescription2.Client.Received(1).GetGroupsAsync();
            await groupShardClientAndDescription3.Client.Received(1).GetGroupsAsync();
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetGroupsAsync_RoutingOnExceptionWhenReadingUserShard()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2
            };
            var userClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetGroupsAsync().Returns(Task.FromResult<List<String>>(userClient1ReturnGroups));
            userShardClientAndDescription2.Client.GetGroupsAsync().Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.GetGroupsAsync();
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).GetGroupsAsync();
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve groups from shard with configuration 'UserShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntityTypesAsync_RoutingOff()
        {
            var returnEntityTypes = new List<String>()
            {
                "ClientAccount",
                "BusinessUnit"
            };
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.GetEntityTypesAsync().Returns(returnEntityTypes);

            List<String> result = await testUserOperationRouter.GetEntityTypesAsync();

            Assert.AreSame(returnEntityTypes, result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).GetEntityTypesAsync();
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntityTypessAsync_RoutingOnReadingUserShards()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnEntityTypes = new List<String>()
            {
                "ClientAccount", 
                "BusinessUnit"
            };
            var userClient2ReturnEntityTypes = new List<String>()
            {
                "BusinessUnit"
            };
            var userClient3ReturnEntityTypes = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(userClient1ReturnEntityTypes));
            userShardClientAndDescription2.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(userClient2ReturnEntityTypes));
            userShardClientAndDescription3.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(userClient3ReturnEntityTypes));

            List<String> result = await testUserOperationRouter.GetEntityTypesAsync();

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("ClientAccount"));
            Assert.IsTrue(result.Contains("BusinessUnit"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetEntityTypesAsync();
            await userShardClientAndDescription2.Client.Received(1).GetEntityTypesAsync();
            await userShardClientAndDescription3.Client.Received(1).GetEntityTypesAsync();
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntityTypessAsync_RoutingOnReadingGroupShards()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            var groupClient1ReturnEntityTypes = new List<String>()
            {
                "ClientAccount"
            };
            var groupClient2ReturnEntityTypes = new List<String>()
            {
                "BusinessUnit"
            };
            var groupClient3ReturnEntityTypes = new List<String>();
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(groupClient1ReturnEntityTypes));
            groupShardClientAndDescription2.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(groupClient2ReturnEntityTypes));
            groupShardClientAndDescription3.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(groupClient3ReturnEntityTypes));

            List<String> result = await testGroupOperationRouter.GetEntityTypesAsync();

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("ClientAccount"));
            Assert.IsTrue(result.Contains("BusinessUnit"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).GetEntityTypesAsync();
            await groupShardClientAndDescription2.Client.Received(1).GetEntityTypesAsync();
            await groupShardClientAndDescription3.Client.Received(1).GetEntityTypesAsync();
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntityTypesAsync_RoutingOnExceptionWhenReadingUserShard()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2
            };
            var userClient1ReturnEntityTypes = new List<String>()
            {
                "ClientAccount",
                "BusinessUnit"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetEntityTypesAsync().Returns(Task.FromResult<List<String>>(userClient1ReturnEntityTypes));
            userShardClientAndDescription2.Client.GetEntityTypesAsync().Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.GetEntityTypesAsync();
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).GetEntityTypesAsync();
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity types from shard with configuration 'UserShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ContainsGroupAsync_RoutingOff()
        {
            String testGroup = "group1";
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.ContainsGroupAsync(testGroup).Returns(true);

            Boolean result = await testUserOperationRouter.ContainsGroupAsync(testGroup);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).ContainsGroupAsync(testGroup);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsGroupAsync_RoutingOnReadingUserShardsResultTrue()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testGroup = "group1";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));
            userShardClientAndDescription3.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testUserOperationRouter.ContainsGroupAsync(testGroup);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).ContainsGroupAsync(testGroup);
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsGroupAsync_RoutingOnReadingUserShardsResultFalse()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testGroup = "group1";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription3.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testUserOperationRouter.ContainsGroupAsync(testGroup);

            Assert.IsFalse(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).ContainsGroupAsync(testGroup);
            await userShardClientAndDescription2.Client.Received(1).ContainsGroupAsync(testGroup);
            await userShardClientAndDescription3.Client.Received(1).ContainsGroupAsync(testGroup);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsGroupAsync_RoutingOnReadingGroupShardsResultTrue()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            String testGroup = "group1";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));
            groupShardClientAndDescription3.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testGroupOperationRouter.ContainsGroupAsync(testGroup);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsGroupAsync_RoutingOnExceptionWhenChecking()
        {
            var mockException = new Exception("Mock exception");
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testGroup = "group1";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsGroupAsync(testGroup).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription3.Client.ContainsGroupAsync(testGroup).Returns(Task.FromException<Boolean>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.ContainsGroupAsync(testGroup);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).ContainsGroupAsync(testGroup);
            await userShardClientAndDescription2.Client.Received(1).ContainsGroupAsync(testGroup);
            await userShardClientAndDescription3.Client.Received(1).ContainsGroupAsync(testGroup);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check for group '{testGroup}' in shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveGroupAsync_RoutingOff()
        {
            String testGroup = "group1";
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();

            await testUserOperationRouter.RemoveGroupAsync(testGroup);

            mockThreadPauser.Received(1).TestPaused();
            await mockSourceEventShardClient.Received(1).RemoveGroupAsync(testGroup);
            Assert.AreEqual(0, mockSourceQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveGroupAsync_RoutingOnExecutingAgainstUserShards()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testGroup = "group1";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Event)).Do((callInfo) => throw groupShardGetClientsException);

            await testUserOperationRouter.RemoveGroupAsync(testGroup);

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.DidNotReceive().GetAllClients(DataElement.Group, Operation.Event);
            await userShardClientAndDescription1.Client.Received(1).RemoveGroupAsync(testGroup);
            await userShardClientAndDescription2.Client.Received(1).RemoveGroupAsync(testGroup);
            await userShardClientAndDescription3.Client.Received(1).RemoveGroupAsync(testGroup);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveGroupAsync_RoutingOnExecutingAgainstGroupShards()
        {
            String testGroup = "group1";
            mockGroupHashCodeGenerator.GetHashCode(testGroup).Returns<Int32>(sourceShardHashRangeStart);

            await testGroupOperationRouter.RemoveGroupAsync(testGroup);

            mockThreadPauser.Received(1).TestPaused();
            mockGroupHashCodeGenerator.Received(1).GetHashCode(testGroup);
            await mockSourceEventShardClient.Received(1).RemoveGroupAsync(testGroup);
            await mockTargetEventShardClient.DidNotReceive().RemoveGroupAsync(testGroup);
            Assert.AreEqual(1, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            

            mockThreadPauser.ClearReceivedCalls();
            mockGroupHashCodeGenerator.ClearReceivedCalls();
            mockShardClientManager.ClearReceivedCalls();
            mockSourceEventShardClient.ClearReceivedCalls();
            mockTargetEventShardClient.ClearReceivedCalls();
            mockShardClientManager.ClearReceivedCalls();
            mockGroupHashCodeGenerator.GetHashCode(testGroup).Returns<Int32>(targetShardHashRangeStart);

            await testGroupOperationRouter.RemoveGroupAsync(testGroup);

            mockThreadPauser.Received(1).TestPaused();
            mockGroupHashCodeGenerator.Received(1).GetHashCode(testGroup);
            await mockSourceEventShardClient.DidNotReceive().RemoveGroupAsync(testGroup);
            await mockTargetEventShardClient.Received(1).RemoveGroupAsync(testGroup);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(1, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveGroupAsync_RoutingOnExceptionWhenExecuting()
        {
            var mockException = new Exception("Mock exception");
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testGroup = "group1";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Event)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription3.Client.RemoveGroupAsync(testGroup).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.RemoveGroupAsync(testGroup);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.DidNotReceive().GetAllClients(DataElement.Group, Operation.Event);
            await userShardClientAndDescription3.Client.Received(1).RemoveGroupAsync(testGroup);
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove group '{testGroup}' from shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddUserToGroupMappingAsync()
        {
            String testUser = "user1";
            String testGroup = "group1";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.AddUserToGroupMappingAsync(testUser, testGroup);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceEventShardClient.Received(1).AddUserToGroupMappingAsync(testUser, testGroup);
        }

        [Test]
        public async Task GetUserToGroupMappingsAsync()
        {
            String testUser = "user1";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.GetUserToGroupMappingsAsync(testUser, false);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceQueryShardClient.Received(1).GetUserToGroupMappingsAsync(testUser, false);
        }

        [Test]
        public void GetUserToGroupMappingsAsync_RoutingOnIncludeIndirectMappingsTrue()
        {
            String testUser = "user1";

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testUserOperationRouter.GetUserToGroupMappingsAsync(testUser, true);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'includeIndirectMappings' with a value of 'True' is not supported."));
            Assert.AreEqual("includeIndirectMappings", e.ParamName);
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync_RoutingOff()
        {
            var testGroups = new List<String> { "group1" };
            var returnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(returnUsers);

            List<String> result = await testUserOperationRouter.GetGroupToUserMappingsAsync(testGroups[0], false);

            Assert.AreSame(returnUsers, result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync_RoutingOn()
        {
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            var testGroups = new List<String> { "group1" };
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            var userClient3ReturnUsers = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient3ReturnUsers));

            List<String> result = await testUserOperationRouter.GetGroupToUserMappingsAsync(testGroups[0], false);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await userShardClientAndDescription2.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await userShardClientAndDescription3.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToUserMappingsAsync_RoutingOnIncludeIndirectMappingsTrue()
        {
            String testGroup = "group1";

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testUserOperationRouter.GetGroupToUserMappingsAsync(testGroup, true);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'includeIndirectMappings' with a value of 'True' is not supported."));
            Assert.AreEqual("includeIndirectMappings", e.ParamName);
        }

        [Test]
        public async Task GetGroupToUserMappingsAsync_RoutingOnExceptionWhenReadingUserShard()
        {
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            var testGroups = new List<String> { "group1" };
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.GetGroupToUserMappingsAsync(testGroups[0], false);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription3.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to user mappings from shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }


        [Test]
        public async Task GetGroupToUserMappingsAsyncGroupsOverload_RoutingOff()
        {
            var testGroups = new List<String>()
            {
                "group1",
                "group2",
                "group3"
            };
            var returnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.GetGroupToUserMappingsAsync(testGroups).Returns(returnUsers);

            List<String> result = await testUserOperationRouter.GetGroupToUserMappingsAsync(testGroups);

            Assert.AreSame(returnUsers, result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).GetGroupToUserMappingsAsync(testGroups);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetGroupToUserMappingsAsyncGroupsOverload_RoutingOn()
        {
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            var userClient3ReturnUsers = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient3ReturnUsers));

            List<String> result = await testUserOperationRouter.GetGroupToUserMappingsAsync(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await userShardClientAndDescription2.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await userShardClientAndDescription3.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetGroupToUserMappingsAsyncGroupsOverload_RoutingOnExceptionWhenReadingUserShard()
        {
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.GetGroupToUserMappingsAsync(testGroups);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription3.Client.Received(1).GetGroupToUserMappingsAsync(Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve group to user mappings for multiple groups from shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveUserToGroupMappingAsync()
        {
            String testUser = "user1";
            String testGroup = "group1";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.RemoveUserToGroupMappingAsync(testUser, testGroup);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceEventShardClient.Received(1).RemoveUserToGroupMappingAsync(testUser, testGroup);
        }

        [Test]
        public async Task AddUserToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceEventShardClient.Received(1).AddUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);
        }

        [Test]
        public async Task GetUserToApplicationComponentAndAccessLevelMappingsAsync()
        {
            String testUser = "user1";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceQueryShardClient.Received(1).GetUserToApplicationComponentAndAccessLevelMappingsAsync(testUser);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync_RoutingOff()
        {
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var returnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(returnUsers);

            List<String> result = await testUserOperationRouter.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);

            Assert.AreSame(returnUsers, result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync_RoutingOn()
        {
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            var userClient3ReturnUsers = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(userClient3ReturnUsers));

            List<String> result = await testUserOperationRouter.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShardClientAndDescription2.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await userShardClientAndDescription3.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToUserMappingsAsync_RoutingOnIncludeIndirectMappingsTrue()
        {
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testUserOperationRouter.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, true);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'includeIndirectMappings' with a value of 'True' is not supported."));
            Assert.AreEqual("includeIndirectMappings", e.ParamName);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToUserMappingsAsync_RoutingOnExceptionWhenReadingUserShard()
        {
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription3.Client.Received(1).GetApplicationComponentAndAccessLevelToUserMappingsAsync(testApplicationComponent, testAccessLevel, false);
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level to user mappings from shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveUserToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceEventShardClient.Received(1).RemoveUserToApplicationComponentAndAccessLevelMappingAsync(testUser, testApplicationComponent, testAccessLevel);
        }

        [Test]
        public async Task AddGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testGroup = "group1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockGroupHashCodeGenerator.GetHashCode(testGroup).Returns<Int32>(sourceShardHashRangeStart);

            await testGroupOperationRouter.AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);

            mockThreadPauser.Received(1).TestPaused();
            mockGroupHashCodeGenerator.Received(1).GetHashCode(testGroup);
            await mockSourceEventShardClient.Received(1).AddGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);
        }

        [Test]
        public async Task GetGroupToApplicationComponentAndAccessLevelMappingsAsync()
        {
            String testGroup = "group1";
            mockGroupHashCodeGenerator.GetHashCode(testGroup).Returns<Int32>(sourceShardHashRangeStart);

            await testGroupOperationRouter.GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup);

            mockThreadPauser.Received(1).TestPaused();
            mockGroupHashCodeGenerator.Received(1).GetHashCode(testGroup);
            await mockSourceQueryShardClient.Received(1).GetGroupToApplicationComponentAndAccessLevelMappingsAsync(testGroup);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync_RoutingOff()
        {
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var returnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            testGroupOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(returnGroups);

            List<String> result = await testGroupOperationRouter.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);

            Assert.AreSame(returnGroups, result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync_RoutingOn()
        {
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            var groupClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var groupClient2ReturnGroups = new List<String>()
            {
                "group2",
                "group3"
            };
            var groupClient3ReturnGroups = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(groupClient1ReturnGroups));
            groupShardClientAndDescription2.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(groupClient2ReturnGroups));
            groupShardClientAndDescription3.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(groupClient3ReturnGroups));

            List<String> result = await testGroupOperationRouter.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShardClientAndDescription2.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            await groupShardClientAndDescription3.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentAndAccessLevelToGroupMappingsAsync_RoutingOnIncludeIndirectMappingsTrue()
        {
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testGroupOperationRouter.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, true);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'includeIndirectMappings' with a value of 'True' is not supported."));
            Assert.AreEqual("includeIndirectMappings", e.ParamName);
        }

        [Test]
        public async Task GetApplicationComponentAndAccessLevelToGroupMappingsAsync_RoutingOnExceptionWhenReadingGroupShard()
        {
            var mockException = new Exception("Mock exception");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            var groupClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var groupClient2ReturnGroups = new List<String>()
            {
                "group2",
                "group3"
            };
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(groupClient1ReturnGroups));
            groupShardClientAndDescription2.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromResult<List<String>>(groupClient2ReturnGroups));
            groupShardClientAndDescription3.Client.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testGroupOperationRouter.GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription3.Client.Received(1).GetApplicationComponentAndAccessLevelToGroupMappingsAsync(testApplicationComponent, testAccessLevel, false);
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level to group mappings from shard with configuration 'GroupShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveGroupToApplicationComponentAndAccessLevelMappingAsync()
        {
            String testGroup = "group1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockGroupHashCodeGenerator.GetHashCode(testGroup).Returns<Int32>(sourceShardHashRangeStart);

            await testGroupOperationRouter.RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);

            mockThreadPauser.Received(1).TestPaused();
            mockGroupHashCodeGenerator.Received(1).GetHashCode(testGroup);
            await mockSourceEventShardClient.Received(1).RemoveGroupToApplicationComponentAndAccessLevelMappingAsync(testGroup, testApplicationComponent, testAccessLevel);
        }

        [Test]
        public async Task ContainsEntityTypeAsync_RoutingOff()
        {
            String testEntityType = "ClientAccount";
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.ContainsEntityTypeAsync(testEntityType).Returns(true);

            Boolean result = await testUserOperationRouter.ContainsEntityTypeAsync(testEntityType);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).ContainsEntityTypeAsync(testEntityType);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityTypeAsync_RoutingOnReadingUserShardsResultTrue()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testEntityType = "ClientAccount";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(true));
            userShardClientAndDescription3.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testUserOperationRouter.ContainsEntityTypeAsync(testEntityType);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityTypeAsync_RoutingOnReadingUserShardsResultFalse()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testEntityType = "ClientAccount";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription3.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testUserOperationRouter.ContainsEntityTypeAsync(testEntityType);

            Assert.IsFalse(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await userShardClientAndDescription2.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await userShardClientAndDescription3.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityTypeAsync_RoutingOnReadingGroupShardsResultTrue()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            String testEntityType = "ClientAccount";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(true));
            groupShardClientAndDescription3.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testGroupOperationRouter.ContainsEntityTypeAsync(testEntityType);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityTypeAsync_RoutingOnExceptionWhenChecking()
        {
            var mockException = new Exception("Mock exception");
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testEntityType = "ClientAccount";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription3.Client.ContainsEntityTypeAsync(testEntityType).Returns(Task.FromException<Boolean>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.ContainsEntityTypeAsync(testEntityType);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await userShardClientAndDescription2.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            await userShardClientAndDescription3.Client.Received(1).ContainsEntityTypeAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check for entity type '{testEntityType}' in shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveEntityTypeAsync_RoutingOff()
        {
            String testEntityType = "ClientAccount";
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();

            await testUserOperationRouter.RemoveEntityTypeAsync(testEntityType);

            mockThreadPauser.Received(1).TestPaused();
            await mockSourceEventShardClient.Received(1).RemoveEntityTypeAsync(testEntityType);
            Assert.AreEqual(0, mockSourceQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveEntityTypeAsync_RoutingOnExecutingAgainstUserShards()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testEntityType = "ClientAccount";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Event)).Do((callInfo) => throw groupShardGetClientsException);

            await testUserOperationRouter.RemoveEntityTypeAsync(testEntityType);

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await userShardClientAndDescription1.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await userShardClientAndDescription2.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await userShardClientAndDescription3.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveEntityTypeAsync_RoutingOnExecutingAgainstGroupShards()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            String testEntityType = "ClientAccount";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Event)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Event).Returns(groupClients);

            await testGroupOperationRouter.RemoveEntityTypeAsync(testEntityType);

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await groupShardClientAndDescription1.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await groupShardClientAndDescription2.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            await groupShardClientAndDescription3.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveEntityTypeAsync_RoutingOnExceptionWhenExecuting()
        {
            var mockException = new Exception("Mock exception");
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testEntityType = "ClientAccount";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Event)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription3.Client.RemoveEntityTypeAsync(testEntityType).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.RemoveEntityTypeAsync(testEntityType);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await userShardClientAndDescription3.Client.Received(1).RemoveEntityTypeAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove entity type '{testEntityType}' from shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAsync_RoutingOff()
        {
            String testEntityType = "Clients";
            var returnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.GetEntitiesAsync(testEntityType).Returns(returnEntities);

            List<String> result = await testUserOperationRouter.GetEntitiesAsync(testEntityType);

            Assert.AreSame(returnEntities, result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).GetEntitiesAsync(testEntityType);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAsync_RoutingOnReadingUserShards()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testEntityType = "Clients";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            var userClient2ReturnEntities = new List<String>()
            {
                "CompanyB",
                "CompanyC"
            };
            var userClient3ReturnEntities = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(userClient1ReturnEntities));
            userShardClientAndDescription2.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(userClient2ReturnEntities));
            userShardClientAndDescription3.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(userClient3ReturnEntities));

            List<String> result = await testUserOperationRouter.GetEntitiesAsync(testEntityType);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetEntitiesAsync(testEntityType);
            await userShardClientAndDescription2.Client.Received(1).GetEntitiesAsync(testEntityType);
            await userShardClientAndDescription3.Client.Received(1).GetEntitiesAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAsync_RoutingOnReadingGroupShards()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            String testEntityType = "Clients";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            var groupClient1ReturnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            var groupClient2ReturnEntities = new List<String>()
            {
                "CompanyB",
                "CompanyC"
            };
            var groupClient3ReturnEntities = new List<String>();
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(groupClient1ReturnEntities));
            groupShardClientAndDescription2.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(groupClient2ReturnEntities));
            groupShardClientAndDescription3.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(groupClient3ReturnEntities));

            List<String> result = await testGroupOperationRouter.GetEntitiesAsync(testEntityType);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAsync(testEntityType);
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAsync(testEntityType);
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAsync(testEntityType);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAsync_RoutingOnReadingUserShardsEntityTypeDoesntExist()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            String testEntityType = "Clients";
            var mockException = new EntityTypeNotFoundException($"Entity type '{testEntityType}' does not exist.", "entityType", testEntityType);
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            var userClient3ReturnEntities = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(userClient1ReturnEntities));
            userShardClientAndDescription2.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromException<List<String>>(mockException));
            userShardClientAndDescription3.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(userClient3ReturnEntities));

            List<String> result = await testUserOperationRouter.GetEntitiesAsync(testEntityType);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetEntitiesAsync(testEntityType);
            await userShardClientAndDescription2.Client.Received(1).GetEntitiesAsync(testEntityType);
            await userShardClientAndDescription3.Client.Received(1).GetEntitiesAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAsync_RoutingOnExceptionWhenReadingUserShard()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            String testEntityType = "Clients";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2
            };
            var userClient1ReturnEntities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromResult<List<String>>(userClient1ReturnEntities));
            userShardClientAndDescription2.Client.GetEntitiesAsync(testEntityType).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.GetEntitiesAsync(testEntityType);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).GetEntitiesAsync(testEntityType);
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entities of type '{testEntityType}' from shard with configuration 'UserShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task ContainsEntityAsync_RoutingOff()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.ContainsEntityAsync(testEntityType, testEntity).Returns(true);

            Boolean result = await testUserOperationRouter.ContainsEntityAsync(testEntityType, testEntity);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityAsync_RoutingOnReadingUserShardsResultTrue()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));
            userShardClientAndDescription3.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testUserOperationRouter.ContainsEntityAsync(testEntityType, testEntity);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityAsync_RoutingOnReadingUserShardsResultFalse()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription3.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testUserOperationRouter.ContainsEntityAsync(testEntityType, testEntity);

            Assert.IsFalse(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await userShardClientAndDescription2.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await userShardClientAndDescription3.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityAsync_RoutingOnReadingGroupShardsResultTrue()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Query)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));
            groupShardClientAndDescription3.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(true));

            Boolean result = await testGroupOperationRouter.ContainsEntityAsync(testEntityType, testEntity);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task ContainsEntityAsync_RoutingOnExceptionWhenChecking()
        {
            var mockException = new Exception("Mock exception");
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Query)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription1.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription2.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            userShardClientAndDescription3.Client.ContainsEntityAsync(testEntityType, testEntity).Returns(Task.FromException<Boolean>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.ContainsEntityAsync(testEntityType, testEntity);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await userShardClientAndDescription2.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            await userShardClientAndDescription3.Client.Received(1).ContainsEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check for entity '{testEntity}' with type '{testEntityType}' in shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveEntityAsync_RoutingOff()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();

            await testUserOperationRouter.RemoveEntityAsync(testEntityType, testEntity);

            mockThreadPauser.Received(1).TestPaused();
            await mockSourceEventShardClient.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(0, mockSourceQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveEntityAsync_RoutingOnExecutingAgainstUserShards()
        {
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Event)).Do((callInfo) => throw groupShardGetClientsException);

            await testUserOperationRouter.RemoveEntityAsync(testEntityType, testEntity);

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await userShardClientAndDescription1.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await userShardClientAndDescription2.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await userShardClientAndDescription3.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveEntityAsync_RoutingOnExecutingAgainstGroupShards()
        {
            var userShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.User}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.User, Operation.Event)).Do((callInfo) => throw userShardGetClientsException);
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Event).Returns(groupClients);

            await testUserOperationRouter.RemoveEntityAsync(testEntityType, testEntity);

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await groupShardClientAndDescription1.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await groupShardClientAndDescription2.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            await groupShardClientAndDescription3.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task RemoveEntityAsync_RoutingOnExceptionWhenExecuting()
        {
            var mockException = new Exception("Mock exception");
            var groupShardGetClientsException = new ArgumentException($"No shard configuration exists for {typeof(DataElement).Name} '{DataElement.Group}' and {typeof(Operation).Name} '{Operation.Query}'.");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Event).Returns(userClients);
            mockShardClientManager.When((clientManager) => clientManager.GetAllClients(DataElement.Group, Operation.Event)).Do((callInfo) => throw groupShardGetClientsException);
            userShardClientAndDescription3.Client.RemoveEntityAsync(testEntityType, testEntity).Returns(Task.FromException(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.RemoveEntityAsync(testEntityType, testEntity);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Event);
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Event);
            await userShardClientAndDescription3.Client.Received(1).RemoveEntityAsync(testEntityType, testEntity);
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(2, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to remove entity '{testEntity}' with type '{testEntityType}' from shard with configuration 'UserShardDescription3"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task AddUserToEntityMappingAsync()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.AddUserToEntityMappingAsync(testUser, testEntityType, testEntity);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceEventShardClient.Received(1).AddUserToEntityMappingAsync(testUser, testEntityType, testEntity);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsync()
        {
            String testUser = "user1";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.GetUserToEntityMappingsAsync(testUser);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceQueryShardClient.Received(1).GetUserToEntityMappingsAsync(testUser);
        }

        [Test]
        public async Task GetUserToEntityMappingsAsyncUserAndEntityTypeOverload()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.GetUserToEntityMappingsAsync(testUser, testEntityType);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceQueryShardClient.Received(1).GetUserToEntityMappingsAsync(testUser, testEntityType);
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_RoutingOff()
        {
            String testEntityType = "Clients";
            String testEntity = "CompanyA";
            var returnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(returnUsers);

            List<String> result = await testUserOperationRouter.GetEntityToUserMappingsAsync(testEntityType, testEntity, false);

            Assert.AreSame(returnUsers, result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_RoutingOn()
        {
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            var userShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription3"
            );
            String testEntityType = "Clients";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2,
                userShardClientAndDescription3
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            var userClient2ReturnUsers = new List<String>()
            {
                "user2",
                "user3"
            };
            var userClient3ReturnUsers = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(userClient2ReturnUsers));
            userShardClientAndDescription3.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(userClient3ReturnUsers));

            List<String> result = await testUserOperationRouter.GetEntityToUserMappingsAsync(testEntityType, testEntity, false);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("user1"));
            Assert.IsTrue(result.Contains("user2"));
            Assert.IsTrue(result.Contains("user3"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription1.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShardClientAndDescription2.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            await userShardClientAndDescription3.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            Assert.AreEqual(1, userShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, userShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntityToUserMappingsAsync_RoutingOnIncludeIndirectMappingsTrue()
        {
            String testEntityType = "Clients";
            String testEntity = "CompanyA";

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testUserOperationRouter.GetEntityToUserMappingsAsync(testEntityType, testEntity, true);
            });

            mockThreadPauser.Received(1).TestPaused();
            Assert.That(e.Message, Does.StartWith("Parameter 'includeIndirectMappings' with a value of 'True' is not supported."));
            Assert.AreEqual("includeIndirectMappings", e.ParamName);
        }

        [Test]
        public async Task GetEntityToUserMappingsAsync_RoutingOnExceptionWhenReadingUserShard()
        {
            var mockException = new Exception("Mock exception");
            var userShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription1"
            );
            var userShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "UserShardDescription2"
            );
            String testEntityType = "Clients";
            String testEntity = "CompanyA";
            var userClients = new List<DistributedClientAndShardDescription>()
            {
                userShardClientAndDescription1,
                userShardClientAndDescription2
            };
            var userClient1ReturnUsers = new List<String>()
            {
                "user1",
                "user2"
            };
            mockShardClientManager.GetAllClients(DataElement.User, Operation.Query).Returns(userClients);
            userShardClientAndDescription1.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(userClient1ReturnUsers));
            userShardClientAndDescription2.Client.GetEntityToUserMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testUserOperationRouter.GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.User, Operation.Query);
            await userShardClientAndDescription2.Client.Received(1).GetEntityToUserMappingsAsync(testEntityType, testEntity, false);
            Assert.AreEqual(1, userShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity to user mappings from shard with configuration 'UserShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveUserToEntityMappingAsync()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceEventShardClient.Received(1).RemoveUserToEntityMappingAsync(testUser, testEntityType, testEntity);
        }

        [Test]
        public async Task AddGroupToEntityMappingAsync()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockGroupHashCodeGenerator.GetHashCode(testGroup).Returns<Int32>(sourceShardHashRangeStart);

            await testGroupOperationRouter.AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);

            mockThreadPauser.Received(1).TestPaused();
            mockGroupHashCodeGenerator.Received(1).GetHashCode(testGroup);
            await mockSourceEventShardClient.Received(1).AddGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsync()
        {
            String testGroup = "group1";
            mockGroupHashCodeGenerator.GetHashCode(testGroup).Returns<Int32>(sourceShardHashRangeStart);

            await testGroupOperationRouter.GetGroupToEntityMappingsAsync(testGroup);

            mockThreadPauser.Received(1).TestPaused();
            mockGroupHashCodeGenerator.Received(1).GetHashCode(testGroup);
            await mockSourceQueryShardClient.Received(1).GetGroupToEntityMappingsAsync(testGroup);
        }

        [Test]
        public async Task GetGroupToEntityMappingsAsyncGroupAndEntityTypeOverload()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            mockGroupHashCodeGenerator.GetHashCode(testGroup).Returns<Int32>(sourceShardHashRangeStart);

            await testGroupOperationRouter.GetGroupToEntityMappingsAsync(testGroup, testEntityType);

            mockThreadPauser.Received(1).TestPaused();
            mockGroupHashCodeGenerator.Received(1).GetHashCode(testGroup);
            await mockSourceQueryShardClient.Received(1).GetGroupToEntityMappingsAsync(testGroup, testEntityType);
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_RoutingOff()
        {
            String testEntityType = "Clients";
            String testEntity = "CompanyA";
            var returnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            testGroupOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(returnGroups);

            List<String> result = await testGroupOperationRouter.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);

            Assert.AreSame(returnGroups, result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_RoutingOn()
        {
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            String testEntityType = "Clients";
            String testEntity = "CompanyA";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2,
                groupShardClientAndDescription3
            };
            var groupClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            var groupClient2ReturnGroups = new List<String>()
            {
                "group2",
                "group3"
            };
            var groupClient3ReturnGroups = new List<String>();
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(groupClient1ReturnGroups));
            groupShardClientAndDescription2.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(groupClient2ReturnGroups));
            groupShardClientAndDescription3.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(groupClient3ReturnGroups));

            List<String> result = await testGroupOperationRouter.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("group1"));
            Assert.IsTrue(result.Contains("group2"));
            Assert.IsTrue(result.Contains("group3"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription1.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShardClientAndDescription2.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            await groupShardClientAndDescription3.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntityToGroupMappingsAsync_RoutingOnIncludeIndirectMappingsTrue()
        {
            String testEntityType = "Clients";
            String testEntity = "CompanyA";

            var e = Assert.ThrowsAsync<ArgumentException>(async delegate
            {
                await testGroupOperationRouter.GetEntityToGroupMappingsAsync(testEntityType, testEntity, true);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'includeIndirectMappings' with a value of 'True' is not supported."));
            Assert.AreEqual("includeIndirectMappings", e.ParamName);
        }

        [Test]
        public async Task GetEntityToGroupMappingsAsync_RoutingOnExceptionWhenReadingGroupShard()
        {
            var mockException = new Exception("Mock exception");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            String testEntityType = "Clients";
            String testEntity = "CompanyA";
            var groupClients = new List<DistributedClientAndShardDescription>()
            {
                groupShardClientAndDescription1,
                groupShardClientAndDescription2
            };
            var groupClient1ReturnGroups = new List<String>()
            {
                "group1",
                "group2"
            };
            mockShardClientManager.GetAllClients(DataElement.Group, Operation.Query).Returns(groupClients);
            groupShardClientAndDescription1.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromResult<List<String>>(groupClient1ReturnGroups));
            groupShardClientAndDescription2.Client.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false).Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testGroupOperationRouter.GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetAllClients(DataElement.Group, Operation.Query);
            await groupShardClientAndDescription2.Client.Received(1).GetEntityToGroupMappingsAsync(testEntityType, testEntity, false);
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity to group mappings from shard with configuration 'GroupShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task RemoveGroupToEntityMappingAsync()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockGroupHashCodeGenerator.GetHashCode(testGroup).Returns<Int32>(sourceShardHashRangeStart);

            await testGroupOperationRouter.RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);

            mockThreadPauser.Received(1).TestPaused();
            mockGroupHashCodeGenerator.Received(1).GetHashCode(testGroup);
            await mockSourceEventShardClient.Received(1).RemoveGroupToEntityMappingAsync(testGroup, testEntityType, testEntity);
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsync()
        {
            String testUser = "user1";
            String testApplicationComponent = "Summary";
            String testAccessLevel = "View";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceQueryShardClient.Received(1).HasAccessToApplicationComponentAsync(testUser, testApplicationComponent, testAccessLevel);
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsyncGroupsOverload_RoutingOff()
        {
            var testGroups = new List<String>()
            {
                "group1",
                "group2",
                "group3"
            }; 
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel).Returns(true);

            Boolean result = await testUserOperationRouter.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        public async Task HasAccessToApplicationComponentAsyncGroupsOverload_RoutingOnResultTrue(Boolean groupShard1Result, Boolean groupShard2Result)
        {
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3", "group4" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                )
            };
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(groupShard1Result));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(groupShard2Result));

            Boolean result = await testGroupOperationRouter.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsyncGroupsOverload_RoutingOnResultFalse()
        {
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3", "group4" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                )
            };
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testGroupOperationRouter.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);

            Assert.IsFalse(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel);
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToApplicationComponentAsyncGroupsOverload_RoutingOnExceptionWhenReadingGroupShard()
        {
            var mockException = new Exception("Mock exception");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            String testApplicationComponent = "Order";
            String testAccessLevel = "Create";
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3", "group4" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                )
            };
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToApplicationComponentAsync(groupClient1Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel).Returns(Task.FromException<Boolean> (mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testGroupOperationRouter.HasAccessToApplicationComponentAsync(testGroups, testApplicationComponent, testAccessLevel);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToApplicationComponentAsync(groupClient2Groups, testApplicationComponent, testAccessLevel);
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check access to application component 'Order' at access level 'Create' for multiple groups in shard with configuration 'GroupShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task HasAccessToEntityAsync()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.HasAccessToEntityAsync(testUser, testEntityType, testEntity);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceQueryShardClient.Received(1).HasAccessToEntityAsync(testUser, testEntityType, testEntity);
        }

        [TestCase(true, false)]
        [TestCase(false, true)]
        [TestCase(true, true)]
        [Test]
        public async Task HasAccessToEntityAsyncGroupsOverload_RoutingOnResultTrue(Boolean groupShard1Result, Boolean groupShard2Result)
        {
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3", "group4" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                )
            };
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(groupShard1Result));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(groupShard2Result));

            Boolean result = await testGroupOperationRouter.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsyncGroupsOverload_RoutingOff()
        {
            var testGroups = new List<String>()
            {
                "group1",
                "group2",
                "group3"
            };
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testUserOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.HasAccessToEntityAsync(testGroups, testEntityType, testEntity).Returns(true);

            Boolean result = await testUserOperationRouter.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);

            Assert.IsTrue(result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).HasAccessToEntityAsync(testGroups, testEntityType, testEntity);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsyncGroupsOverload_RoutingOnResultFalse()
        {
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3", "group4" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                )
            };
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));

            Boolean result = await testGroupOperationRouter.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);

            Assert.IsFalse(result);
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity);
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task HasAccessToEntityAsyncGroupsOverload_RoutingOnExceptionWhenReadingGroupShard()
        {
            var mockException = new Exception("Mock exception");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3", "group4" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                )
            };
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.HasAccessToEntityAsync(groupClient1Groups, testEntityType, testEntity).Returns(Task.FromResult<Boolean>(false));
            groupShardClientAndDescription2.Client.HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity).Returns(Task.FromException<Boolean>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testGroupOperationRouter.HasAccessToEntityAsync(testGroups, testEntityType, testEntity);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription2.Client.Received(1).HasAccessToEntityAsync(groupClient2Groups, testEntityType, testEntity);
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to check access to entity 'CompanyA' with type 'ClientAccount' for multiple groups in shard with configuration 'GroupShardDescription2"));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByUserAsync()
        {
            String testUser = "user1";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.GetApplicationComponentsAccessibleByUserAsync(testUser);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceQueryShardClient.Received(1).GetApplicationComponentsAccessibleByUserAsync(testUser);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupAsync()
        {
            String testGroup = "group1";
            mockGroupHashCodeGenerator.GetHashCode(testGroup).Returns<Int32>(sourceShardHashRangeStart);

            await testGroupOperationRouter.GetApplicationComponentsAccessibleByGroupAsync(testGroup);

            mockThreadPauser.Received(1).TestPaused();
            mockGroupHashCodeGenerator.Received(1).GetHashCode(testGroup);
            await mockSourceQueryShardClient.Received(1).GetApplicationComponentsAccessibleByGroupAsync(testGroup);
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupsAsync_RoutingOff()
        {
            var testGroups = new List<String>()
            {
                "group1",
                "group2",
                "group3"
            };
            var returnApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Order", "Create")
            };
            testGroupOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.GetApplicationComponentsAccessibleByGroupsAsync(testGroups).Returns(returnApplicationComponents);

            List<Tuple<String, String>> result = await testGroupOperationRouter.GetApplicationComponentsAccessibleByGroupsAsync(testGroups);

            Assert.AreSame(returnApplicationComponents, result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(testGroups);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupsAsync_RoutingOn()
        {
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription3,
                    groupClient3Groups
                )
            };
            var groupClient1ApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Order", "Create")
            };
            var groupClient2ApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "Create"),
                Tuple.Create("Summary", "View")
            };
            var groupClient3ApplicationComponents = new List<Tuple<String, String>>();
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1ApplicationComponents));
            groupShardClientAndDescription2.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2ApplicationComponents));
            groupShardClientAndDescription3.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient3ApplicationComponents));

            List<Tuple<String, String>> result = await testGroupOperationRouter.GetApplicationComponentsAccessibleByGroupsAsync(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "View")));
            Assert.IsTrue(result.Contains(Tuple.Create("Order", "Create")));
            Assert.IsTrue(result.Contains(Tuple.Create("Summary", "View")));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups);
            await groupShardClientAndDescription2.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups);
            await groupShardClientAndDescription3.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetApplicationComponentsAccessibleByGroupsAsync_RoutingOnExceptionWhenReadingGroupShard()
        {
            var mockException = new Exception("Mock exception");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription3,
                    groupClient3Groups
                )
            };
            var groupClient1ApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "View"),
                Tuple.Create("Order", "Create")
            };
            var groupClient2ApplicationComponents = new List<Tuple<String, String>>()
            {
                Tuple.Create("Order", "Create"),
                Tuple.Create("Summary", "View")
            };
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1ApplicationComponents));
            groupShardClientAndDescription2.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2ApplicationComponents));
            groupShardClientAndDescription3.Client.GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testGroupOperationRouter.GetApplicationComponentsAccessibleByGroupsAsync(testGroups);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription3.Client.Received(1).GetApplicationComponentsAccessibleByGroupsAsync(groupClient3Groups);
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve application component and access level mappings for multiple groups from shard with configuration 'GroupShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsync()
        {
            String testUser = "user1";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.GetEntitiesAccessibleByUserAsync(testUser);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceQueryShardClient.Received(1).GetEntitiesAccessibleByUserAsync(testUser);
        }

        [Test]
        public async Task GetEntitiesAccessibleByUserAsyncUserAndEntityTypeOverload()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);

            await testUserOperationRouter.GetEntitiesAccessibleByUserAsync(testUser, testEntityType);

            mockThreadPauser.Received(1).TestPaused();
            mockUserHashCodeGenerator.Received(1).GetHashCode(testUser);
            await mockSourceQueryShardClient.Received(1).GetEntitiesAccessibleByUserAsync(testUser, testEntityType);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsync()
        {
            String testGroup = "group1";
            mockGroupHashCodeGenerator.GetHashCode(testGroup).Returns<Int32>(sourceShardHashRangeStart);

            await testGroupOperationRouter.GetEntitiesAccessibleByGroupAsync(testGroup);

            mockThreadPauser.Received(1).TestPaused();
            mockGroupHashCodeGenerator.Received(1).GetHashCode(testGroup);
            await mockSourceQueryShardClient.Received(1).GetEntitiesAccessibleByGroupAsync(testGroup);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupAsyncGroupAndEntityTypeOverload()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            mockGroupHashCodeGenerator.GetHashCode(testGroup).Returns<Int32>(sourceShardHashRangeStart);

            await testGroupOperationRouter.GetEntitiesAccessibleByGroupAsync(testGroup, testEntityType);

            mockThreadPauser.Received(1).TestPaused();
            mockGroupHashCodeGenerator.Received(1).GetHashCode(testGroup);
            await mockSourceQueryShardClient.Received(1).GetEntitiesAccessibleByGroupAsync(testGroup, testEntityType);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsync_RoutingOff()
        {
            var testGroups = new List<String>()
            {
                "group1",
                "group2",
                "group3"
            };
            var returnEntities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("ClientAccount", "CompanyB")
            };
            testGroupOperationRouter.RoutingOn = false;
            mockMetricLogger.ClearReceivedCalls();
            mockSourceQueryShardClient.GetEntitiesAccessibleByGroupsAsync(testGroups).Returns(returnEntities);

            List<Tuple<String, String>> result = await testGroupOperationRouter.GetEntitiesAccessibleByGroupsAsync(testGroups);

            Assert.AreSame(returnEntities, result);
            mockThreadPauser.Received(1).TestPaused();
            await mockSourceQueryShardClient.Received(1).GetEntitiesAccessibleByGroupsAsync(testGroups);
            Assert.AreEqual(0, mockSourceEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetQueryShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockTargetEventShardClient.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsync_RoutingOn()
        {
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription3,
                    groupClient3Groups
                )
            };
            var groupClient1Entities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("ClientAccount", "CompanyB")
            };
            var groupClient2Entities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyB"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            var groupClient3Entities = new List<Tuple<String, String>>();
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient3Entities));

            List<Tuple<String, String>> result = await testGroupOperationRouter.GetEntitiesAccessibleByGroupsAsync(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(Tuple.Create("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(Tuple.Create("ClientAccount", "CompanyB")));
            Assert.IsTrue(result.Contains(Tuple.Create("BusinessUnit", "Sales")));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups);
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient2Groups);
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups);
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsync_RoutingOnExceptionWhenReadingGroupShard()
        {
            var mockException = new Exception("Mock exception");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription3,
                    groupClient3Groups
                )
            };
            var groupClient1Entities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyA"),
                Tuple.Create("ClientAccount", "CompanyB")
            };
            var groupClient2Entities = new List<Tuple<String, String>>()
            {
                Tuple.Create("ClientAccount", "CompanyB"),
                Tuple.Create("BusinessUnit", "Sales")
            };
            var groupClient3Entities = new List<Tuple<String, String>>();
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups).Returns(Task.FromResult<List<Tuple<String, String>>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups).Returns(Task.FromException<List<Tuple<String, String>>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testGroupOperationRouter.GetEntitiesAccessibleByGroupsAsync(testGroups);
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups);
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity mappings for multiple groups from shard with configuration 'GroupShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsyncEntityTypeOverload_RoutingOn()
        {
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription3,
                    groupClient3Groups
                )
            };
            var groupClient1Entities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            var groupClient2Entities = new List<String>()
            {
                "CompanyB",
                "CompanyC"
            };
            var groupClient3Entities = new List<String>();
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient3Entities));

            List<String> result = await testGroupOperationRouter.GetEntitiesAccessibleByGroupsAsync(testGroups, "ClientAccount");

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription1.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, "ClientAccount");
            await groupShardClientAndDescription2.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, "ClientAccount");
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, "ClientAccount");
            Assert.AreEqual(1, groupShardClientAndDescription1.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription2.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public async Task GetEntitiesAccessibleByGroupsAsyncEntityTypeOverload_RoutingOnExceptionWhenReadingGroupShard()
        {
            var mockException = new Exception("Mock exception");
            var groupShardClientAndDescription1 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription1"
            );
            var groupShardClientAndDescription2 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription2"
            );
            var groupShardClientAndDescription3 = new DistributedClientAndShardDescription
            (
                Substitute.For<IDistributedAccessManagerAsyncClient<String, String, String, String>>(),
                "GroupShardDescription3"
            );
            var testGroups = new List<String> { "group1", "group2", "group3", "group4", "group5", "group6", };
            var groupClient1Groups = new List<String>() { "group1", "group2", "group3" };
            var groupClient2Groups = new List<String>() { "group4", "group5" };
            var groupClient3Groups = new List<String>() { "group6" };
            var groupsAndGroupClients = new List<Tuple<DistributedClientAndShardDescription, IEnumerable<String>>>()
            {
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription1,
                    groupClient1Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription2,
                    groupClient2Groups
                ),
                new Tuple<DistributedClientAndShardDescription, IEnumerable<String>>
                (
                    groupShardClientAndDescription3,
                    groupClient3Groups
                )
            };
            var groupClient1Entities = new List<String>()
            {
                "CompanyA",
                "CompanyB"
            };
            var groupClient2Entities = new List<String>()
            {
                "CompanyB",
                "CompanyC"
            };
            var groupClient3Entities = new List<String>();
            // Mock the calls the group nodes to get the mappings
            mockShardClientManager.GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups))).Returns(groupsAndGroupClients);
            groupShardClientAndDescription1.Client.GetEntitiesAccessibleByGroupsAsync(groupClient1Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient1Entities));
            groupShardClientAndDescription2.Client.GetEntitiesAccessibleByGroupsAsync(groupClient2Groups, "ClientAccount").Returns(Task.FromResult<List<String>>(groupClient2Entities));
            groupShardClientAndDescription3.Client.GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, "ClientAccount").Returns(Task.FromException<List<String>>(mockException));

            var e = Assert.ThrowsAsync<Exception>(async delegate
            {
                await testGroupOperationRouter.GetEntitiesAccessibleByGroupsAsync(testGroups, "ClientAccount");
            });

            mockThreadPauser.Received(1).TestPaused();
            mockShardClientManager.Received(1).GetClients(DataElement.Group, Operation.Query, Arg.Is<IEnumerable<String>>(EqualIgnoringOrder(testGroups)));
            await groupShardClientAndDescription3.Client.Received(1).GetEntitiesAccessibleByGroupsAsync(groupClient3Groups, "ClientAccount");
            Assert.AreEqual(1, groupShardClientAndDescription3.Client.ReceivedCalls().Count());
            Assert.AreEqual(1, mockShardClientManager.ReceivedCalls().Count());
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve entity mappings for multiple groups and entity type 'ClientAccount' from shard with configuration 'GroupShardDescription3'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void PauseOperations()
        {
            testGroupOperationRouter.PauseOperations();

            mockMetricLogger.Received(1).Increment(Arg.Any<RouterPaused>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
            mockThreadPauser.DidNotReceive().TestPaused();
            mockThreadPauser.Received(1).Pause();
        }

        [Test]
        public void ResumeOperations()
        {
            testGroupOperationRouter.ResumeOperations();

            mockMetricLogger.Received(1).Increment(Arg.Any<RouterResumed>());
            Assert.AreEqual(1, mockMetricLogger.ReceivedCalls().Count());
            mockThreadPauser.DidNotReceive().TestPaused();
            mockThreadPauser.Received(1).Resume();
        }

        // The following tests for method ImplementRoutingAsync() are tested via public method RemoveUserAsync(), since ImplementRoutingAsync() is protected
        //   Not testing with parameter shardOperationType = Operation.Query, since no query methods have a void return type.
        [Test]
        public async Task ImplementRoutingAsync()
        {
            String testUser = "user1";
            testUserOperationRouter.RoutingOn = true;
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(targetShardHashRangeStart);
            mockSourceEventShardClient.ClearReceivedCalls();
            mockTargetEventShardClient.ClearReceivedCalls();

            await testUserOperationRouter.RemoveUserAsync(testUser);

            await mockTargetEventShardClient.Received(1).RemoveUserAsync(testUser);


            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);
            mockSourceEventShardClient.ClearReceivedCalls();
            mockTargetEventShardClient.ClearReceivedCalls();

            await testUserOperationRouter.RemoveUserAsync(testUser);

            await mockSourceEventShardClient.Received(1).RemoveUserAsync(testUser);


            testUserOperationRouter.RoutingOn = false;
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(targetShardHashRangeStart);
            mockSourceEventShardClient.ClearReceivedCalls();
            mockTargetEventShardClient.ClearReceivedCalls();

            await testUserOperationRouter.RemoveUserAsync(testUser);

            await mockSourceEventShardClient.Received(1).RemoveUserAsync(testUser);


            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);
            mockSourceEventShardClient.ClearReceivedCalls();
            mockTargetEventShardClient.ClearReceivedCalls();

            await testUserOperationRouter.RemoveUserAsync(testUser);

            await mockSourceEventShardClient.Received(1).RemoveUserAsync(testUser);
        }

        // The following tests for method ImplementRoutingAsync<T>() are tested via public method ContainsUserAsync(), since ImplementRoutingAsync<T>() is protected
        //   Not testing with parameter shardOperationType = Operation.Event, since no event methods have a non-void return type.
        [Test]
        public async Task ImplementRoutingAsyncReturnTypeParameterOverload()
        {
            String testUser = "user1";
            testUserOperationRouter.RoutingOn = true;
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(targetShardHashRangeStart);
            mockSourceQueryShardClient.ClearReceivedCalls();
            mockTargetQueryShardClient.ClearReceivedCalls();
            mockTargetQueryShardClient.ContainsUserAsync(testUser).Returns<Boolean>(true);

            Boolean result = await testUserOperationRouter.ContainsUserAsync(testUser);

            Assert.IsTrue(result);
            await mockTargetQueryShardClient.Received(1).ContainsUserAsync(testUser);


            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);
            mockSourceQueryShardClient.ClearReceivedCalls();
            mockTargetQueryShardClient.ClearReceivedCalls();
            mockSourceQueryShardClient.ContainsUserAsync(testUser).Returns<Boolean>(true);

            result = await testUserOperationRouter.ContainsUserAsync(testUser);

            Assert.IsTrue(result);
            await mockSourceQueryShardClient.Received(1).ContainsUserAsync(testUser);


            testUserOperationRouter.RoutingOn = false;
            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(targetShardHashRangeStart);
            mockSourceQueryShardClient.ClearReceivedCalls();
            mockTargetQueryShardClient.ClearReceivedCalls();
            mockSourceQueryShardClient.ContainsUserAsync(testUser).Returns<Boolean>(true);

            result = await testUserOperationRouter.ContainsUserAsync(testUser);

            Assert.IsTrue(result);
            await mockSourceQueryShardClient.Received(1).ContainsUserAsync(testUser);


            mockUserHashCodeGenerator.GetHashCode(testUser).Returns<Int32>(sourceShardHashRangeStart);
            mockSourceQueryShardClient.ClearReceivedCalls();
            mockTargetQueryShardClient.ClearReceivedCalls();
            mockSourceQueryShardClient.ContainsUserAsync(testUser).Returns<Boolean>(true);

            result = await testUserOperationRouter.ContainsUserAsync(testUser);

            Assert.IsTrue(result);
            await mockSourceQueryShardClient.Received(1).ContainsUserAsync(testUser);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Returns an <see cref="Expression"/> which evaluates a <see cref="Predicate{T}"/> which checks whether a collection of strings matches the collection in parameter <paramref name="expected"/> irrespective of their enumeration order.
        /// </summary>
        /// <param name="expected">The collection of strings the predicate compares to.</param>
        /// <returns>The <see cref="Expression"/> which evaluates a <see cref="Predicate{T}"/>.</returns>
        /// <remarks>Designed to be passed to the 'predicate' parameter of the <see cref="Arg.Any{T}"/> argument matcher.</remarks>
        protected Expression<Predicate<IEnumerable<String>>> EqualIgnoringOrder(IEnumerable<String> expected)
        {
            return testUtilities.EqualIgnoringOrder(expected);
        }

        #endregion
    }
}
