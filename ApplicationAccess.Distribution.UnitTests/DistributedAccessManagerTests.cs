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
using System.Threading;
using ApplicationAccess.Metrics;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using NSubstitute;
using ApplicationMetrics;
using MoreComplexDataStructures;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Distribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Metrics.DistributedAccessManager class.
    /// </summary>
    public class DistributedAccessManagerTests
    {
        private const String beginMetricsLoggedText = "beginMetricsLogged";
        private const String postProcessingActionInvokedText = "postProcessingActionInvoked";
        private const String endMetricsLoggedText = "endMetricsLogged";

        private IAccessManagerEventProcessor<String, String, ApplicationScreen, AccessLevel> mockEventProcessor;
        private IMetricLogger mockMetricLogger;
        private ConcurrentAccessManagerMetricLoggerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testMetricLoggingWrapper;
        private DistributedAccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testDistributedAccessManager;

        [SetUp]
        protected void SetUp()
        {
            mockEventProcessor = Substitute.For<IAccessManagerEventProcessor<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testMetricLoggingWrapper = new ConcurrentAccessManagerMetricLoggerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger); 
            testDistributedAccessManager = new DistributedAccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);
            testDistributedAccessManager.EventProcessor = mockEventProcessor;
            // Test version of class exposes a setter, so that the test version of the decorator can be set after construction
            testDistributedAccessManager.MetricLoggingWrapper = testMetricLoggingWrapper;
        }

        [Test]
        public void Constructor_MappingMetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            DistributedAccessManager<String, String, ApplicationScreen, AccessLevel> testDistributedAccessManager;
            var fieldNamePath = new List<String>() { "mappingMetricLogger" };
            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testDistributedAccessManager);
        }

        [Test]
        public void Constructor_UserToGroupMapAcquireLocksParameterSetCorrectlyOnComposedFields()
        {
            DistributedAccessManager<String, String, ApplicationScreen, AccessLevel> testDistributedAccessManager;
            var fieldNamePath = new List<String>() { "userToGroupMap", "acquireLocks" };
            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDistributedAccessManager);


            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDistributedAccessManager);
        }

        [Test]
        public void Constructor_UserToGroupMapMetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            DistributedAccessManager<String, String, ApplicationScreen, AccessLevel> testDistributedAccessManager;
            var fieldNamePath = new List<String>() { "userToGroupMap", "metricLogger" };
            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testDistributedAccessManager);
        }

        [Test]
        public void GetGroupToUserMappingsGroupsOverload()
        {
            CreateGroupGraph(testDistributedAccessManager);
            testDistributedAccessManager.AddUserToGroupMapping("Usr7", "Grp7");
            testDistributedAccessManager.AddUserToGroupMapping("Usr8", "Grp8");
            testDistributedAccessManager.AddUserToGroupMapping("Usr9", "Grp9");
            testDistributedAccessManager.AddUserToGroupMapping("Usr1", "Grp1");
            testDistributedAccessManager.AddUserToGroupMapping("Usr2", "Grp2");
            testDistributedAccessManager.AddUserToGroupMapping("Usr3", "Grp3");
            var testGroups = new List<String>() { "Grp7" };

            HashSet<String> result = testDistributedAccessManager.GetGroupToUserMappings(testGroups);

            Assert.AreEqual(1, result.Count);
            Assert.IsTrue(result.Contains("Usr7"));


            testGroups = new List<String>() { "Grp7", "Grp1" };

            result = testDistributedAccessManager.GetGroupToUserMappings(testGroups);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Usr7"));
            Assert.IsTrue(result.Contains("Usr1"));


            testGroups = new List<String>() { "Grp7", "Grp1", "Grp9" };

            result = testDistributedAccessManager.GetGroupToUserMappings(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Usr7"));
            Assert.IsTrue(result.Contains("Usr1"));
            Assert.IsTrue(result.Contains("Usr9"));


            testGroups = new List<String>() { "Grp7", "Grp1", "Grp9", "Grp3" };

            result = testDistributedAccessManager.GetGroupToUserMappings(testGroups);

            Assert.AreEqual(4, result.Count);
            Assert.IsTrue(result.Contains("Usr7"));
            Assert.IsTrue(result.Contains("Usr1"));
            Assert.IsTrue(result.Contains("Usr9"));
            Assert.IsTrue(result.Contains("Usr3"));


            testGroups = new List<String>() { "Grp7", "Grp1", "Grp9", "Grp3", "Grp2" };

            result = testDistributedAccessManager.GetGroupToUserMappings(testGroups);

            Assert.AreEqual(5, result.Count);
            Assert.IsTrue(result.Contains("Usr7"));
            Assert.IsTrue(result.Contains("Usr1"));
            Assert.IsTrue(result.Contains("Usr9"));
            Assert.IsTrue(result.Contains("Usr3"));
            Assert.IsTrue(result.Contains("Usr2"));


            testGroups = new List<String>() { "Grp7", "Grp1", "Grp9", "Grp3", "Grp2", "Grp8", "Grp11", "Grp5" };

            result = testDistributedAccessManager.GetGroupToUserMappings(testGroups);

            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.Contains("Usr7"));
            Assert.IsTrue(result.Contains("Usr1"));
            Assert.IsTrue(result.Contains("Usr9"));
            Assert.IsTrue(result.Contains("Usr3"));
            Assert.IsTrue(result.Contains("Usr2"));
            Assert.IsTrue(result.Contains("Usr8"));
        }

        [Test]
        public void GetGroupToUserMappingsGroupsOverload_GroupsParameterContainsInvalidGroup()
        {
            CreateGroupGraph(testDistributedAccessManager);
            testDistributedAccessManager.AddUserToGroupMapping("Usr7", "Grp7");
            testDistributedAccessManager.AddUserToGroupMapping("Usr8", "Grp8");
            testDistributedAccessManager.AddUserToGroupMapping("Usr9", "Grp9");
            testDistributedAccessManager.AddUserToGroupMapping("Usr1", "Grp1");
            testDistributedAccessManager.AddUserToGroupMapping("Usr2", "Grp2");
            testDistributedAccessManager.AddUserToGroupMapping("Usr3", "Grp3");
            var testGroups = new List<String>() { "Grp7", "Grp1", "Grp9", "Grp13", "Grp3", "Grp2", "Grp8" };

            HashSet<String> result = testDistributedAccessManager.GetGroupToUserMappings(testGroups);

            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.Contains("Usr7"));
            Assert.IsTrue(result.Contains("Usr1"));
            Assert.IsTrue(result.Contains("Usr9"));
            Assert.IsTrue(result.Contains("Usr3"));
            Assert.IsTrue(result.Contains("Usr2"));
            Assert.IsTrue(result.Contains("Usr8"));
        }

        [Test]
        public void GetGroupToUserMappingsGroupsOverload_Metrics()
        {
            CreateGroupGraph(testDistributedAccessManager);
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testDistributedAccessManager.AddUserToGroupMapping("Usr7", "Grp7");
            testDistributedAccessManager.AddUserToGroupMapping("Usr8", "Grp8");
            var testGroups = new List<String>() { "Grp7", "Grp8", "Grp2" };
            mockMetricLogger.Begin(Arg.Any<GetGroupToUserMappingsForGroupsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testDistributedAccessManager.GetGroupToUserMappings(testGroups);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Usr7"));
            Assert.IsTrue(result.Contains("Usr8"));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToUserMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToUserMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToUserMappingsForGroupsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToUserMappingsGroupsOverload_MetricLoggingDisabled()
        {
            testDistributedAccessManager.MetricLoggingEnabled = false;
            testDistributedAccessManager.AddUserToGroupMapping("Usr7", "Grp7");
            testDistributedAccessManager.AddUserToGroupMapping("Usr8", "Grp8");
            var testGroups = new List<String>() { "Grp7", "Grp8", "Grp2" };

            HashSet<String> result = testDistributedAccessManager.GetGroupToUserMappings(testGroups);

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("Usr7"));
            Assert.IsTrue(result.Contains("Usr8"));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToGroupMappingsGroupsOverload()
        {
            CreateGroupGraph(testDistributedAccessManager);
            var testGroups = new List<String>() { "Grp1", "Grp5", "Grp8", "Grp11" };

            HashSet<String> result = testDistributedAccessManager.GetGroupToGroupMappings(testGroups);

            Assert.AreEqual(7, result.Count);
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp7"));
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp11"));
            Assert.IsTrue(result.Contains("Grp12"));


            testGroups = new List<String>() { "Grp1", "Grp8", "Grp11", "Grp5" };

            result = testDistributedAccessManager.GetGroupToGroupMappings(testGroups);

            Assert.AreEqual(7, result.Count);
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp7"));
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp11"));
            Assert.IsTrue(result.Contains("Grp12"));


            testGroups = new List<String>() { "Grp5", "Grp11", "Grp1", "Grp8" };

            result = testDistributedAccessManager.GetGroupToGroupMappings(testGroups);

            Assert.AreEqual(7, result.Count);
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp7"));
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp11"));
            Assert.IsTrue(result.Contains("Grp12"));


            testGroups = new List<String>() { "Grp11", "Grp5", "Grp8", "Grp1" };

            result = testDistributedAccessManager.GetGroupToGroupMappings(testGroups);

            Assert.AreEqual(7, result.Count);
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp7"));
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp11"));
            Assert.IsTrue(result.Contains("Grp12"));


            testGroups = new List<String>() { "Grp8", "Grp1", "Grp5", "Grp11" };

            result = testDistributedAccessManager.GetGroupToGroupMappings(testGroups);

            Assert.AreEqual(7, result.Count);
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp7"));
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp11"));
            Assert.IsTrue(result.Contains("Grp12"));


            testGroups = new List<String>() { "Grp8", "Grp5", "Grp11", "Grp1" };

            result = testDistributedAccessManager.GetGroupToGroupMappings(testGroups);

            Assert.AreEqual(7, result.Count);
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp7"));
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp11"));
            Assert.IsTrue(result.Contains("Grp12"));
        }


        [Test]
        public void GetGroupToGroupMappingsGroupsOverload_GroupsParameterContainsInvalidGroup()
        {
            CreateGroupGraph(testDistributedAccessManager);
            var testGroups = new List<String>() { "Grp1", "Grp5", "Grp13", "Grp8", "Grp11" };

            HashSet<String> result = testDistributedAccessManager.GetGroupToGroupMappings(testGroups);

            Assert.AreEqual(7, result.Count);
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp7"));
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp11"));
            Assert.IsTrue(result.Contains("Grp12"));
        }

        [Test]
        public void GetGroupToGroupMappingsGroupsOverload_Metrics()
        {
            var testGroups = new List<String>() { "Grp1", "Grp4" };
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testDistributedAccessManager.AddGroupToGroupMapping("Grp1", "Grp3");
            testDistributedAccessManager.AddGroupToGroupMapping("Grp1", "Grp4");
            testDistributedAccessManager.AddGroupToGroupMapping("Grp2", "Grp3");
            testDistributedAccessManager.AddGroupToGroupMapping("Grp2", "Grp4");
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupMappingsForGroupsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testDistributedAccessManager.GetGroupToGroupMappings(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp3"));
            Assert.IsTrue(result.Contains("Grp4"));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupMappingsForGroupsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToGroupMappingsGroupsOverload_MetricLoggingDisabled()
        {
            testDistributedAccessManager.MetricLoggingEnabled = false;
            var testGroups = new List<String>() { "Grp1", "Grp4" };
            testDistributedAccessManager.AddGroupToGroupMapping("Grp1", "Grp3");
            testDistributedAccessManager.AddGroupToGroupMapping("Grp1", "Grp4");
            testDistributedAccessManager.AddGroupToGroupMapping("Grp2", "Grp3");
            testDistributedAccessManager.AddGroupToGroupMapping("Grp2", "Grp4");

            HashSet<String> result = testDistributedAccessManager.GetGroupToGroupMappings(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp3"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToGroupReverseMappingsGroupsOverload()
        {
            CreateGroupGraph(testDistributedAccessManager);
            var testGroups = new List<String>() { "Grp7", "Grp6", "Grp2", "Grp12" };

            HashSet<String> result = testDistributedAccessManager.GetGroupToGroupReverseMappings(testGroups);

            Assert.AreEqual(9, result.Count);
            Assert.IsTrue(result.Contains("Grp7"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp3"));
            Assert.IsTrue(result.Contains("Grp10"));
            Assert.IsTrue(result.Contains("Grp2"));
            Assert.IsTrue(result.Contains("Grp12"));
            Assert.IsTrue(result.Contains("Grp11"));


            testGroups = new List<String>() { "Grp12", "Grp2", "Grp6", "Grp7" };

            result = testDistributedAccessManager.GetGroupToGroupReverseMappings(testGroups);

            Assert.AreEqual(9, result.Count);
            Assert.IsTrue(result.Contains("Grp7"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp3"));
            Assert.IsTrue(result.Contains("Grp10"));
            Assert.IsTrue(result.Contains("Grp2"));
            Assert.IsTrue(result.Contains("Grp12"));
            Assert.IsTrue(result.Contains("Grp11"));


            testGroups = new List<String>() { "Grp5", "Grp8", "Grp11" };

            result = testDistributedAccessManager.GetGroupToGroupReverseMappings(testGroups);

            Assert.AreEqual(9, result.Count);
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp2"));
            Assert.IsTrue(result.Contains("Grp3"));
            Assert.IsTrue(result.Contains("Grp10"));
            Assert.IsTrue(result.Contains("Grp11"));


            testGroups = new List<String>() { "Grp11", "Grp8", "Grp5"  };

            result = testDistributedAccessManager.GetGroupToGroupReverseMappings(testGroups);

            Assert.AreEqual(9, result.Count);
            Assert.IsTrue(result.Contains("Grp8"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.IsTrue(result.Contains("Grp5"));
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp2"));
            Assert.IsTrue(result.Contains("Grp3"));
            Assert.IsTrue(result.Contains("Grp10"));
            Assert.IsTrue(result.Contains("Grp11"));
        }

        [Test]
        public void GetGroupToGroupReverseMappingsGroupsOverload_GroupsParameterContainsInvalidGroup()
        {
            CreateGroupGraph(testDistributedAccessManager);
            var testGroups = new List<String>() { "Grp7", "Grp6", "Grp13", "Grp2", "Grp12" };

            HashSet<String> result = testDistributedAccessManager.GetGroupToGroupReverseMappings(testGroups);

            Assert.AreEqual(9, result.Count);
            Assert.IsTrue(result.Contains("Grp7"));
            Assert.IsTrue(result.Contains("Grp4"));
            Assert.IsTrue(result.Contains("Grp1"));
            Assert.IsTrue(result.Contains("Grp6"));
            Assert.IsTrue(result.Contains("Grp3"));
            Assert.IsTrue(result.Contains("Grp10"));
            Assert.IsTrue(result.Contains("Grp2"));
            Assert.IsTrue(result.Contains("Grp12"));
            Assert.IsTrue(result.Contains("Grp11"));
        }

        [Test]
        public void GetGroupToGroupReverseMappingsGroupsOverload_Metrics()
        {
            CreateGroupGraph(testDistributedAccessManager);
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            var testGroups = new List<String>() { "Grp3", "Grp11" };
            mockMetricLogger.Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testDistributedAccessManager.GetGroupToGroupReverseMappings(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Grp3"));
            Assert.IsTrue(result.Contains("Grp10"));
            Assert.IsTrue(result.Contains("Grp11"));
            mockMetricLogger.Received(1).Begin(Arg.Any<GetGroupToGroupReverseMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetGroupToGroupReverseMappingsForGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetGroupToGroupReverseMappingsForGroupsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetGroupToGroupReverseMappingsGroupsOverload_MetricLoggingDisabled()
        {
            testDistributedAccessManager.MetricLoggingEnabled = false;
            CreateGroupGraph(testDistributedAccessManager);
            var testGroups = new List<String>() { "Grp3", "Grp11" };

            HashSet<String> result = testDistributedAccessManager.GetGroupToGroupReverseMappings(testGroups);

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("Grp3"));
            Assert.IsTrue(result.Contains("Grp10"));
            Assert.IsTrue(result.Contains("Grp11"));
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveEntityType()
        {
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testBeginId2 = Guid.Parse("1c7e0715-6314-4396-9485-e8ca3219f5fa");
            Guid testBeginId3 = Guid.Parse("d9faf8d4-a463-4131-ab70-88128a45b9a1");
            CreateEntityElements(testDistributedAccessManager);
            mockMetricLogger.ClearReceivedCalls();
            mockEventProcessor.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<EntityRemoveTime>()).Returns(testBeginId2, testBeginId3);

            testDistributedAccessManager.RemoveEntityType("EntityType1");

            Assert.AreEqual(2, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).RemoveEntity("EntityType1", "Entity1");
            mockEventProcessor.Received(1).RemoveEntity("EntityType1", "Entity2");
            mockMetricLogger.Received(2).Begin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId3, Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(2).Increment(Arg.Any<EntityRemoved>());
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeRemoved>());
            // 2 of the below calls are generated by the prepended 'remove entity' events, and 1 by the 'remove entity type event'
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 4);
            mockMetricLogger.Received(2).Set(Arg.Any<EntitiesStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            // 2 of the below calls are generated by the prepended 'remove entity' events, and 1 by the 'remove entity type event'
            mockMetricLogger.Received(3).Set(Arg.Any<UserToEntityMappingsStored>(), 3);
            // 2 of the below calls are generated by the prepended 'remove entity' events, and 1 by the 'remove entity type event'
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 4);
            mockMetricLogger.Received(2).Set(Arg.Any<GroupToEntityMappingsStored>(), 2);
            Assert.AreEqual(19, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(3, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("User1"));
            Assert.AreEqual(2, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("User2"));
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("User3"));
            Assert.AreEqual(2, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("Group1"));
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("Group2"));
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("Group3"));
            Assert.IsFalse(testDistributedAccessManager.EntityTypes.Contains("EntityType1"));
        }

        [Test]
        public void RemoveEntityType_IdempotentCallDoesntLogMetrics()
        {
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testBeginId2 = Guid.Parse("1c7e0715-6314-4396-9485-e8ca3219f5fa");
            Guid testBeginId3 = Guid.Parse("d9faf8d4-a463-4131-ab70-88128a45b9a1");
            Guid testBeginId4 = Guid.Parse("bb4ccd17-9333-4387-83d0-3c591c8a932c");
            CreateEntityElements(testDistributedAccessManager);
            mockMetricLogger.ClearReceivedCalls();
            mockEventProcessor.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId1, testBeginId4);
            mockMetricLogger.Begin(Arg.Any<EntityRemoveTime>()).Returns(testBeginId2, testBeginId3);

            testDistributedAccessManager.RemoveEntityType("EntityType1");
            testDistributedAccessManager.RemoveEntityType("EntityType1");

            Assert.AreEqual(2, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).RemoveEntity("EntityType1", "Entity1");
            mockEventProcessor.Received(1).RemoveEntity("EntityType1", "Entity2");
            mockMetricLogger.Received(2).Begin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId3, Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(2).Increment(Arg.Any<EntityRemoved>());
            mockMetricLogger.Received(2).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeRemoved>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId4, Arg.Any<EntityTypeRemoveTime>());
            // 2 of the below calls are generated by the prepended 'remove entity' events, and 1 by the 'remove entity type event'
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 4);
            mockMetricLogger.Received(2).Set(Arg.Any<EntitiesStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            // 2 of the below calls are generated by the prepended 'remove entity' events, and 1 by the 'remove entity type event'
            mockMetricLogger.Received(3).Set(Arg.Any<UserToEntityMappingsStored>(), 3);
            // 2 of the below calls are generated by the prepended 'remove entity' events, and 1 by the 'remove entity type event'
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 4);
            mockMetricLogger.Received(2).Set(Arg.Any<GroupToEntityMappingsStored>(), 2);
            Assert.AreEqual(21, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void RemoveEntityType_WrappingActionMethodOverrideOrdering()
        {
            const String beginEntityMetricsLoggedText = "beginEntityMetricsLogged";
            const String endEntityMetricsLoggedText = "endEntityMetricsLogged";
            String testEntityType = "EntityType1";
            testDistributedAccessManager.AddEntity(testEntityType, "Entity1");
            testDistributedAccessManager.AddEntity(testEntityType, "Entity2");
            mockMetricLogger.ClearReceivedCalls();
            mockEventProcessor.ClearReceivedCalls();
            var wrappingActionOrder = new List<String>();
            // Lock and state checks for prepended 'remove entity' events
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<EntityRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManager.UserToEntityMap));
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManager.GroupToEntityMap));
                Assert.IsTrue(testDistributedAccessManager.EntityTypes.Contains(testEntityType));
                wrappingActionOrder.Add(beginEntityMetricsLoggedText);
            });
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<EntityRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManager.UserToEntityMap));
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManager.GroupToEntityMap));
                wrappingActionOrder.Add(endEntityMetricsLoggedText);
            });
            // Lock and state checks for the 'remove entity type' event
            mockMetricLogger.When((metricLogger) => metricLogger.Begin(Arg.Any<EntityTypeRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks have been set
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManager.UserToEntityMap));
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManager.GroupToEntityMap));
                Assert.IsTrue(testDistributedAccessManager.EntityTypes.Contains(testEntityType));
                Assert.IsFalse(testDistributedAccessManager.Entities[testEntityType].Contains("Entity1"));
                Assert.IsFalse(testDistributedAccessManager.Entities[testEntityType].Contains("Entity2"));
                wrappingActionOrder.Add(beginMetricsLoggedText);
            });
            Action<String> postProcessingAction = (entityType) =>
            {
                // Check that the user has been added
                Assert.IsFalse(testDistributedAccessManager.EntityTypes.Contains(testEntityType));
                wrappingActionOrder.Add(postProcessingActionInvokedText);
            };
            mockMetricLogger.When((metricLogger) => metricLogger.End(Arg.Any<Guid>(), Arg.Any<EntityTypeRemoveTime>())).Do((callInfo) =>
            {
                // Check that locks are still set
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManager.UserToEntityMap));
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManager.GroupToEntityMap));
                wrappingActionOrder.Add(endMetricsLoggedText);
            });

            testDistributedAccessManager.RemoveEntityType(testEntityType, postProcessingAction);

            Assert.AreEqual(2, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).RemoveEntity("EntityType1", "Entity1");
            mockEventProcessor.Received(1).RemoveEntity("EntityType1", "Entity2");
            Assert.AreEqual(7, wrappingActionOrder.Count);
            Assert.AreEqual(beginEntityMetricsLoggedText, wrappingActionOrder[0]);
            Assert.AreEqual(endEntityMetricsLoggedText, wrappingActionOrder[1]);
            Assert.AreEqual(beginEntityMetricsLoggedText, wrappingActionOrder[2]);
            Assert.AreEqual(endEntityMetricsLoggedText, wrappingActionOrder[3]);
            Assert.AreEqual(beginMetricsLoggedText, wrappingActionOrder[4]);
            Assert.AreEqual(postProcessingActionInvokedText, wrappingActionOrder[5]);
            Assert.AreEqual(endMetricsLoggedText, wrappingActionOrder[6]);
        }

        [Test]
        public void RemoveEntityType_ExceptionWhenRemoving()
        {
            var mockException = new Exception("Mock exception");
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testBeginId2 = Guid.Parse("1c7e0715-6314-4396-9485-e8ca3219f5fa");
            Guid testBeginId3 = Guid.Parse("d9faf8d4-a463-4131-ab70-88128a45b9a1");
            CreateEntityElements(testDistributedAccessManager);
            mockMetricLogger.ClearReceivedCalls();
            mockEventProcessor.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<EntityRemoveTime>()).Returns(testBeginId2, testBeginId3);
            Action<String> postProcessingAction = (entityType) =>
            {
                throw mockException;
            };

            var e = Assert.Throws<Exception>(delegate
            {
                testDistributedAccessManager.RemoveEntityType("EntityType1", postProcessingAction);
            });

            Assert.AreEqual(2, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).RemoveEntity("EntityType1", "Entity1");
            mockEventProcessor.Received(1).RemoveEntity("EntityType1", "Entity2");
            mockMetricLogger.Received(2).Begin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId3, Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(2).Increment(Arg.Any<EntityRemoved>());
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).CancelBegin(testBeginId1, Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 3);
            mockMetricLogger.Received(2).Set(Arg.Any<UserToEntityMappingsStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 2);
            Assert.AreEqual(14, mockMetricLogger.ReceivedCalls().Count());
            mockMetricLogger.DidNotReceive().Increment(Arg.Any<EntityTypeRemoved>());
            mockMetricLogger.DidNotReceive().Set(Arg.Any<EntityTypesStored>(), Arg.Any<Int64>());
            Assert.AreEqual(3, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("User1"));
            Assert.AreEqual(2, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("User2"));
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("User3"));
            Assert.AreEqual(2, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("Group1"));
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("Group2"));
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("Group3"));
            Assert.IsFalse(testDistributedAccessManager.EntityTypes.Contains("EntityType1"));
        }

        [Test]
        public void RemoveEntityTypePostProcessingActionOverload()
        {
            Guid testBeginId1 = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            Guid testBeginId2 = Guid.Parse("1c7e0715-6314-4396-9485-e8ca3219f5fa");
            Guid testBeginId3 = Guid.Parse("d9faf8d4-a463-4131-ab70-88128a45b9a1");
            CreateEntityElements(testDistributedAccessManager);
            mockMetricLogger.ClearReceivedCalls();
            mockEventProcessor.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityTypeRemoveTime>()).Returns(testBeginId1);
            mockMetricLogger.Begin(Arg.Any<EntityRemoveTime>()).Returns(testBeginId2, testBeginId3);
            Boolean postProcessingActionWasCalled = false;
            Action<String> postProcessingAction = (String removeEntityType) =>
            {
                postProcessingActionWasCalled = true;
            };

            testDistributedAccessManager.RemoveEntityType("EntityType1", postProcessingAction);

            Assert.AreEqual(2, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).RemoveEntity("EntityType1", "Entity1");
            mockEventProcessor.Received(1).RemoveEntity("EntityType1", "Entity2");
            mockMetricLogger.Received(2).Begin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId2, Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId3, Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(2).Increment(Arg.Any<EntityRemoved>());
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId1, Arg.Any<EntityTypeRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityTypeRemoved>());
            // 2 of the below calls are generated by the prepended 'remove entity' events, and 1 by the 'remove entity type event'
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 4);
            mockMetricLogger.Received(2).Set(Arg.Any<EntitiesStored>(), 3);
            mockMetricLogger.Received(1).Set(Arg.Any<EntityTypesStored>(), 1);
            // 2 of the below calls are generated by the prepended 'remove entity' events, and 1 by the 'remove entity type event'
            mockMetricLogger.Received(3).Set(Arg.Any<UserToEntityMappingsStored>(), 3);
            // 2 of the below calls are generated by the prepended 'remove entity' events, and 1 by the 'remove entity type event'
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 4);
            mockMetricLogger.Received(2).Set(Arg.Any<GroupToEntityMappingsStored>(), 2);
            Assert.AreEqual(19, mockMetricLogger.ReceivedCalls().Count());
            Assert.AreEqual(3, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(3, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("User1"));
            Assert.AreEqual(2, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("User2"));
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("User3"));
            Assert.AreEqual(2, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(0, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("Group1"));
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("Group2"));
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("Group3"));
            Assert.IsFalse(testDistributedAccessManager.EntityTypes.Contains("EntityType1"));
            Assert.IsTrue(postProcessingActionWasCalled);
        }

        [Test]
        public void RemoveEntity_GenerateEventParameterFalse()
        {
            CreateEntityElements(testDistributedAccessManager);

            var e = Assert.Throws<NotImplementedException>(delegate
            {
                testDistributedAccessManager.RemoveEntity("EntityType2", "Entity3", false);
            });
        }

        [Test]
        public void RemoveEntity_GenerateEventParameterTrue()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            CreateEntityElements(testDistributedAccessManager);
            mockMetricLogger.ClearReceivedCalls();
            mockEventProcessor.ClearReceivedCalls();
            mockMetricLogger.Begin(Arg.Any<EntityRemoveTime>()).Returns(testBeginId);

            testDistributedAccessManager.RemoveEntity("EntityType2", "Entity3", true);

            Assert.AreEqual(1, mockEventProcessor.ReceivedCalls().Count());
            mockEventProcessor.Received(1).RemoveEntity("EntityType2", "Entity3");
            mockMetricLogger.Received(1).Begin(Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<EntityRemoveTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<EntityRemoved>());
            mockMetricLogger.Received(1).Set(Arg.Any<EntitiesStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<UserToEntityMappingsStored>(), 4);
            mockMetricLogger.Received(1).Set(Arg.Any<GroupToEntityMappingsStored>(), 3);
            Assert.AreEqual(4, testMetricLoggingWrapper.EntityCount);
            Assert.AreEqual(4, testMetricLoggingWrapper.UserToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("User1"));
            Assert.AreEqual(3, testMetricLoggingWrapper.UserToEntityMappingCountPerUser.GetFrequency("User2"));
            Assert.AreEqual(3, testMetricLoggingWrapper.GroupToEntityMappingCount);
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("Group1"));
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("Group2"));
            Assert.AreEqual(1, testMetricLoggingWrapper.GroupToEntityMappingCountPerGroup.GetFrequency("Group3"));
            Assert.IsFalse(testDistributedAccessManager.GetEntities("EntityType2").Contains("Entity3"));
        }

        [Test]
        public void HasAccessToApplicationComponentGroupsOverload_GroupHasAccess()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Delete);

            Boolean result = testDistributedAccessManager.HasAccessToApplicationComponent(new List<String>() { "group1", "group2", "group3" }, ApplicationScreen.ManageProducts, AccessLevel.Delete);

            Assert.IsTrue(result);
        }

        [Test]
        public void HasAccessToApplicationComponentGroupsOverload_NoGroupsHaveAccess()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Delete);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.Create);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Summary, AccessLevel.View);

            Boolean result = testDistributedAccessManager.HasAccessToApplicationComponent(new List<String>() { "group1", "group2", "group3" }, ApplicationScreen.ManageProducts, AccessLevel.Modify);

            Assert.IsFalse(result);
        }

        [Test]
        public void HasAccessToApplicationComponentGroupsOverload_InvalidGroupIncluded()
        {

            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Delete);

            Boolean result = testDistributedAccessManager.HasAccessToApplicationComponent(new List<String>() { "invalid group", "group2", "group3" }, ApplicationScreen.ManageProducts, AccessLevel.Delete);

            Assert.IsTrue(result);
        }

        [Test]
        public void HasAccessToApplicationComponentGroupsOverload_MetricsExceptionWhenQuerying()
        {
            // TODO: Find a way to test this.  Currently I can't see a way to make the method throw an exception due to ignoring of invalid elements.
        }

        [Test]
        public void HasAccessToApplicationComponentGroupsOverload_Metrics()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Delete);
            mockMetricLogger.Begin(Arg.Any<HasAccessToApplicationComponentForGroupsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testDistributedAccessManager.HasAccessToApplicationComponent(new List<String>() { "group1", "group2", "group3" }, ApplicationScreen.ManageProducts, AccessLevel.Delete);

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToApplicationComponentForGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToApplicationComponentForGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToApplicationComponentForGroupsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void HasAccessToApplicationComponentGroupsOverload_MetricLoggingDisabled()
        {
            testDistributedAccessManager.MetricLoggingEnabled = false;
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Delete);

            Boolean result = testDistributedAccessManager.HasAccessToApplicationComponent(new List<String>() { "group1", "group2", "group3" }, ApplicationScreen.ManageProducts, AccessLevel.Delete);

            Assert.IsTrue(result);
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void HasAccessToEntityGroupsOverload_GroupHasAccess()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");

            Boolean result = testDistributedAccessManager.HasAccessToEntity(new List<String>() { "group1", "group2", "group3" }, "ClientAccount", "CompanyB");

            Assert.IsTrue(result);
        }

        [Test]
        public void HasAccessToEntityGroupsOverload_NoGroupsHaveAccess()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyC");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");

            Boolean result = testDistributedAccessManager.HasAccessToEntity(new List<String>() { "group1", "group2", "group3" }, "ClientAccount", "CompanyC");

            Assert.IsFalse(result);
        }

        [Test]
        public void HasAccessToEntityGroupsOverload_InvalidGroupIncluded()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");

            Boolean result = testDistributedAccessManager.HasAccessToEntity(new List<String>() { "invalid group", "group2", "group3" }, "ClientAccount", "CompanyB");

            Assert.IsTrue(result);
        }

        [Test]
        public void HasAccessToEntityGroupsOverload_InvalidEntityType()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");

            Boolean result = testDistributedAccessManager.HasAccessToEntity(new List<String>() { "group1", "group2", "group3" }, "InvalidEntityType", "CompanyB");

            Assert.IsFalse(result);
        }

        [Test]
        public void HasAccessToEntityGroupsOverload_InvalidEntity()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");

            Boolean result = testDistributedAccessManager.HasAccessToEntity(new List<String>() { "group1", "group2", "group3" }, "ClientAccount", "InvalidEntity");

            Assert.IsFalse(result);
        }

        [Test]
        public void HasAccessToEntityGroupsOverload_MetricsExceptionWhenQuerying()
        {
            // TODO: Find a way to test this.  Currently I can't see a way to make the method throw an exception due to ignoring of invalid elements.
        }

        [Test]
        public void HasAccessToEntityGroupsOverload_Metrics()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");
            mockMetricLogger.Begin(Arg.Any<HasAccessToEntityForGroupsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            Boolean result = testDistributedAccessManager.HasAccessToEntity(new List<String>() { "group1", "group2", "group3" }, "ClientAccount", "CompanyB");

            Assert.IsTrue(result);
            mockMetricLogger.Received(1).Begin(Arg.Any<HasAccessToEntityForGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<HasAccessToEntityForGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<HasAccessToEntityForGroupsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void HasAccessToEntityGroupsOverload_MetricLoggingDisabled()
        {
            testDistributedAccessManager.MetricLoggingEnabled = false;
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");

            Boolean result = testDistributedAccessManager.HasAccessToEntity(new List<String>() { "group1", "group2", "group3" }, "ClientAccount", "CompanyB");

            Assert.IsTrue(result);
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroups()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddGroup("group4");
            testDistributedAccessManager.AddGroup("group5");
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Delete);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Delete);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.Order, AccessLevel.Create);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.Create);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.Delete);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.Modify);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group4", ApplicationScreen.Order, AccessLevel.Modify);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group4", ApplicationScreen.Settings, AccessLevel.Create);

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testDistributedAccessManager.GetApplicationComponentsAccessibleByGroups(new List<String>() { "group1", "group2", "group3", "group5" });

            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Create)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Delete)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Modify)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Create)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Delete)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.Order, AccessLevel.Modify)));
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroups_GroupsParameterContainsInvalidGroup()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3"); 
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Delete);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.Create);

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testDistributedAccessManager.GetApplicationComponentsAccessibleByGroups(new List<String>() { "group1", "Invalid", "group2" });

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Create)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Delete)));
            Assert.IsTrue(result.Contains(new Tuple<ApplicationScreen, AccessLevel>(ApplicationScreen.ManageProducts, AccessLevel.Modify)));
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroups_MetricsExceptionWhenQuerying()
        {
            // TODO: Find a way to test this.  Currently I can't see a way to make the method throw an exception due to ignoring of invalid elements.
        }

        [Test]
        public void GetApplicationComponentsAccessibleByGroups_Metrics()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Delete);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.Create);
            mockMetricLogger.Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testDistributedAccessManager.GetApplicationComponentsAccessibleByGroups(new List<String>() { "group1", "group2" });

            Assert.AreEqual(3, result.Count);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetApplicationComponentsAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetApplicationComponentsAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetApplicationComponentsAccessibleByGroupsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }
        [Test]
        public void GetApplicationComponentsAccessibleByGroups_MetricLoggingDisabled()
        {
            testDistributedAccessManager.MetricLoggingEnabled = false;
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.ManageProducts, AccessLevel.Create);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Delete);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group2", ApplicationScreen.ManageProducts, AccessLevel.Modify);
            testDistributedAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping("group3", ApplicationScreen.Order, AccessLevel.Create);

            HashSet<Tuple<ApplicationScreen, AccessLevel>> result = testDistributedAccessManager.GetApplicationComponentsAccessibleByGroups(new List<String>() { "group1", "group2" });

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByGroupsGroupsOverload()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddGroup("group4");
            testDistributedAccessManager.AddGroup("group5");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyC");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyD");
            testDistributedAccessManager.AddEntityType("BusinessUnit");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Sales");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Accounting");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Marketing");
            testDistributedAccessManager.AddEntity("BusinessUnit", "GeneralAffairs");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "BusinessUnit", "Sales");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Sales");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Accounting");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Marketing");
            testDistributedAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "Marketing");
            testDistributedAccessManager.AddGroupToEntityMapping("group4", "ClientAccount", "CompanyD");

            HashSet<Tuple<String, String>> result = testDistributedAccessManager.GetEntitiesAccessibleByGroups(new List<String>() { "group1", "group2", "group3", "group5" });

            Assert.AreEqual(6, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyB")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyC")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Sales")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Accounting")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("BusinessUnit", "Marketing")));
        }

        [Test]
        public void GetEntitiesAccessibleByGroupsGroupsOverload_GroupsParameterContainsInvalidGroup()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyC");
            testDistributedAccessManager.AddEntityType("BusinessUnit");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Sales");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Sales");

            HashSet<Tuple<String, String>> result = testDistributedAccessManager.GetEntitiesAccessibleByGroups(new List<String>() { "group1", "Invalid", "group2" });

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyA")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyB")));
            Assert.IsTrue(result.Contains(new Tuple<String, String>("ClientAccount", "CompanyC")));
        }

        [Test]
        public void GetEntitiesAccessibleByGroupsGroupsOverload_MetricsExceptionWhenQuerying()
        {
            // TODO: Find a way to test this.  Currently I can't see a way to make the method throw an exception due to ignoring of invalid elements.
        }

        [Test]
        public void GetEntitiesAccessibleByGroupsGroupsOverload_Metrics()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyC");
            testDistributedAccessManager.AddEntityType("BusinessUnit");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Sales");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Sales");
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<Tuple<String, String>> result = testDistributedAccessManager.GetEntitiesAccessibleByGroups(new List<String>() { "group1", "group2" });

            Assert.AreEqual(3, result.Count);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByGroupsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByGroupsGroupsOverload_MetricLoggingDisabled()
        {
            testDistributedAccessManager.MetricLoggingEnabled = false;
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyC");
            testDistributedAccessManager.AddEntityType("BusinessUnit");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Sales");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Sales");

            HashSet<Tuple<String, String>> result = testDistributedAccessManager.GetEntitiesAccessibleByGroups(new List<String>() { "group1", "group2" });

            Assert.AreEqual(3, result.Count);
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByGroupsGroupsAndEntityTypeOverload()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddGroup("group4");
            testDistributedAccessManager.AddGroup("group5");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyC");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyD");
            testDistributedAccessManager.AddEntityType("BusinessUnit");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Sales");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Accounting");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Marketing");
            testDistributedAccessManager.AddEntity("BusinessUnit", "GeneralAffairs");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "BusinessUnit", "Sales");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Sales");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Accounting");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Marketing");
            testDistributedAccessManager.AddGroupToEntityMapping("group4", "BusinessUnit", "Marketing");
            testDistributedAccessManager.AddGroupToEntityMapping("group4", "ClientAccount", "CompanyD");

            HashSet<String> result = testDistributedAccessManager.GetEntitiesAccessibleByGroups(new List<String>() { "group1", "group2", "group3", "group5" }, "ClientAccount");

            Assert.AreEqual(3, result.Count);
            Assert.IsTrue(result.Contains("CompanyA"));
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
        }

        [Test]
        public void GetEntitiesAccessibleByGroupsGroupsAndEntityTypeOverload_GroupsParameterContainsInvalidGroup()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyC");
            testDistributedAccessManager.AddEntityType("BusinessUnit");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Sales");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Sales");

            HashSet<String> result = testDistributedAccessManager.GetEntitiesAccessibleByGroups(new List<String>() { "group2", "Invalid", "group3" }, "ClientAccount");

            Assert.AreEqual(2, result.Count);
            Assert.IsTrue(result.Contains("CompanyB"));
            Assert.IsTrue(result.Contains("CompanyC"));
        }

        [Test]
        public void GetEntitiesAccessibleByGroupsGroupsAndEntityTypeOverload_InvalidEntityType()
        {
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyC");
            testDistributedAccessManager.AddEntityType("BusinessUnit");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Sales");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Sales");

            HashSet<String> result = testDistributedAccessManager.GetEntitiesAccessibleByGroups(new List<String>() { "group2", "group3" }, "Invalid");

            Assert.AreEqual(0, result.Count);
        }

        [Test]
        public void GetEntitiesAccessibleByGroupsGroupsAndEntityTypeOverload_MetricsExceptionWhenQuerying()
        {
            // TODO: Find a way to test this.  Currently I can't see a way to make the method throw an exception due to ignoring of invalid elements.
        }

        [Test]
        public void GetEntitiesAccessibleByGroupsGroupsAndEntityTypeOverload_Metrics()
        {
            Guid testBeginId = Guid.Parse("5c8ab5fa-f438-4ab4-8da4-9e5728c0ed32");
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyC");
            testDistributedAccessManager.AddEntityType("BusinessUnit");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Sales");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Sales");
            mockMetricLogger.Begin(Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>()).Returns(testBeginId);
            mockMetricLogger.ClearReceivedCalls();

            HashSet<String> result = testDistributedAccessManager.GetEntitiesAccessibleByGroups(new List<String>() { "group2", "group3" }, "ClientAccount");

            Assert.AreEqual(2, result.Count);
            mockMetricLogger.Received(1).Begin(Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).End(testBeginId, Arg.Any<GetEntitiesAccessibleByGroupsQueryTime>());
            mockMetricLogger.Received(1).Increment(Arg.Any<GetEntitiesAccessibleByGroupsQuery>());
            Assert.AreEqual(3, mockMetricLogger.ReceivedCalls().Count());
        }

        [Test]
        public void GetEntitiesAccessibleByGroupsGroupsAndEntityTypeOverload_MetricLoggingDisabled()
        {
            testDistributedAccessManager.MetricLoggingEnabled = false;
            testDistributedAccessManager.AddGroup("group1");
            testDistributedAccessManager.AddGroup("group2");
            testDistributedAccessManager.AddGroup("group3");
            testDistributedAccessManager.AddEntityType("ClientAccount");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyA");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyB");
            testDistributedAccessManager.AddEntity("ClientAccount", "CompanyC");
            testDistributedAccessManager.AddEntityType("BusinessUnit");
            testDistributedAccessManager.AddEntity("BusinessUnit", "Sales");
            testDistributedAccessManager.AddGroupToEntityMapping("group1", "ClientAccount", "CompanyA");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyB");
            testDistributedAccessManager.AddGroupToEntityMapping("group2", "ClientAccount", "CompanyC");
            testDistributedAccessManager.AddGroupToEntityMapping("group3", "BusinessUnit", "Sales");

            HashSet<String> result = testDistributedAccessManager.GetEntitiesAccessibleByGroups(new List<String>() { "group2", "group3" }, "ClientAccount");

            Assert.AreEqual(2, result.Count);
            Assert.AreEqual(0, mockMetricLogger.ReceivedCalls().Count());
        }

        #region Private/Protected Methods

        // Creates the following graph of groups...
        //
        //   Grp7   Grp8   Grp9
        //    |   /  |   \  |
        //   Grp4   Grp5   Grp6       Grp12
        //    |   /  |   \  |          |
        //   Grp1   Grp2   Grp3       Grp11
        //                  |
        //                 Grp10
        //
        /// <summary>
        /// Creates a sample hierarchy of groups of users in the specified access manager.
        /// </summary>
        /// <param name="accessManager">The access manager to create the hierarchy in.</param>
        protected void CreateGroupGraph(DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel> accessManager)
        {
            accessManager.AddGroupToGroupMapping("Grp1", "Grp4");
            accessManager.AddGroupToGroupMapping("Grp1", "Grp5");
            accessManager.AddGroupToGroupMapping("Grp2", "Grp5");
            accessManager.AddGroupToGroupMapping("Grp3", "Grp5");
            accessManager.AddGroupToGroupMapping("Grp3", "Grp6");
            accessManager.AddGroupToGroupMapping("Grp4", "Grp7");
            accessManager.AddGroupToGroupMapping("Grp4", "Grp8");
            accessManager.AddGroupToGroupMapping("Grp5", "Grp8");
            accessManager.AddGroupToGroupMapping("Grp6", "Grp8");
            accessManager.AddGroupToGroupMapping("Grp6", "Grp9");
            accessManager.AddGroupToGroupMapping("Grp10", "Grp3");
            accessManager.AddGroupToGroupMapping("Grp11", "Grp12");
        }

        /// <summary>
        /// Creates a sample set of entities and related element mappings in the specified access manager.
        /// </summary>
        /// <param name="accessManager">The access manager to create the sample elements in.</param>
        protected void CreateEntityElements(DependencyFreeAccessManager<String, String, ApplicationScreen, AccessLevel> accessManager)
        {
            accessManager.AddEntity("EntityType1", "Entity1");
            accessManager.AddEntity("EntityType1", "Entity2");
            accessManager.AddEntity("EntityType2", "Entity3");
            accessManager.AddEntity("EntityType2", "Entity4");
            accessManager.AddEntity("EntityType2", "Entity5");
            accessManager.AddUser("User1");
            accessManager.AddUser("User2");
            accessManager.AddUser("User3");
            accessManager.AddUser("Group1");
            accessManager.AddUser("Group2");
            accessManager.AddUser("Group3");
            accessManager.AddUserToEntityMapping("User1", "EntityType1", "Entity1");
            accessManager.AddUserToEntityMapping("User2", "EntityType1", "Entity1");
            accessManager.AddUserToEntityMapping("User2", "EntityType2", "Entity4");
            accessManager.AddUserToEntityMapping("User2", "EntityType2", "Entity5");
            accessManager.AddUserToEntityMapping("User3", "EntityType2", "Entity3");
            accessManager.AddGroupToEntityMapping("Group1", "EntityType1", "Entity2");
            accessManager.AddGroupToEntityMapping("Group2", "EntityType1", "Entity2");
            accessManager.AddGroupToEntityMapping("Group2", "EntityType2", "Entity3");
            accessManager.AddGroupToEntityMapping("Group3", "EntityType2", "Entity5");
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Version of the ConcurrentAccessManagerMetricLogger class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application</typeparam>
        /// <typeparam name="TComponent">The type of components in the application to control access to.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class ConcurrentAccessManagerMetricLoggerWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : ConcurrentAccessManagerMetricLogger<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Metrics.UnitTests.MetricLoggingConcurrentAccessManagerTests+ConcurrentAccessManagerMetricLoggerWithProtectedMembers class.
            /// </summary>
            /// <param name="metricLogger">The logger for metrics.</param>
            public ConcurrentAccessManagerMetricLoggerWithProtectedMembers(IMetricLogger metricLogger)
                : base(metricLogger)
            {
            }

            /// <summary>
            /// The number of user to application component and access level mappings in the access manager.
            /// </summary>
            public Int32 UserToApplicationComponentAndAccessLevelMappingCount
            {
                get { return userToApplicationComponentAndAccessLevelMappingCount; }
            }

            /// <summary>
            /// The number of group to application component and access level mappings in the access manager.
            /// </summary>
            public Int32 GroupToApplicationComponentAndAccessLevelMappingCount
            {
                get { return groupToApplicationComponentAndAccessLevelMappingCount; }
            }

            /// <summary>
            /// The number of entities in the access manager.
            /// </summary>
            public Int32 EntityCount
            {
                get { return entityCount; }
            }

            /// <summary>
            /// The number of user to entity mappings stored.
            /// </summary>
            public Int32 UserToEntityMappingCount
            {
                get { return userToEntityMappingCount; }
            }

            /// <summary>
            /// The number of user to entity mappings stored for each user.
            /// </summary>
            public FrequencyTable<TUser> UserToEntityMappingCountPerUser
            {
                get { return userToEntityMappingCountPerUser; }
            }

            /// <summary>
            /// The number of group tp entity mappings stored.
            /// </summary>
            public Int32 GroupToEntityMappingCount
            {
                get { return groupToEntityMappingCount; }
            }

            /// <summary>
            /// The number of group to entity mappings stored for each group.
            /// </summary>
            public FrequencyTable<TGroup> GroupToEntityMappingCountPerGroup
            {
                get { return groupToEntityMappingCountPerGroup; }
            }
        }

        /// <summary>
        /// Version of the DistributedAccessManager class where protected members are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application</typeparam>
        /// <typeparam name="TComponent">The type of components in the application to control access to.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class DistributedAccessManagerWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : DistributedAccessManager<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>Lock object for the users collection.</summary>
            public Object UsersLock
            {
                get { return usersLock; }
            }

            /// <summary>Lock object for the groups collection.</summary>
            public Object GroupsLock
            {
                get { return groupsLock; }
            }

            /// <summary>Lock object for the user to group map.</summary>
            public Object UserToGroupMapLock
            {
                get { return userToGroupMapLock; }
            }

            /// <summary>Lock object for the group to group map.</summary>
            public Object GroupToGroupMapLock
            {
                get { return groupToGroupMapLock; }
            }

            /// <summary>The user to application component and access level map as an object (to check for locking).</summary>
            public IDictionary<TUser, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>> UserToComponentMap
            {
                get { return userToComponentMap; }
            }

            /// <summary>The group to application component and access level map as an object (to check for locking).</summary>
            public IDictionary<TGroup, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>> GroupToComponentMap
            {
                get { return groupToComponentMap; }
            }

            /// <summary>The entities colection as an object (to check for locking).</summary>
            public IDictionary<String, ISet<String>> Entities
            {
                get { return entities; }
            }

            /// <summary>The user to entity map as an object (to check for locking).</summary>
            public IDictionary<TUser, IDictionary<String, ISet<String>>> UserToEntityMap
            {
                get { return userToEntityMap; }
            }

            /// <summary>The group to entity map as an object (to check for locking).</summary>
            public IDictionary<TGroup, IDictionary<String, ISet<String>>> GroupToEntityMap
            {
                get { return groupToEntityMap; }
            }

            /// <summary>
            /// The logger for metrics.
            /// </summary>
            public ConcurrentAccessManagerMetricLogger<TUser, TGroup, TComponent, TAccess> MetricLoggingWrapper
            {
                set { metricLoggingWrapper = value; }
            }

            /// <summary>
            /// Idempotently removes an entity.
            /// </summary>
            /// <param name="entityType">The type of the entity.</param>
            /// <param name="entity">The entity to remove.</param>
            /// <param name="generateEvent">Whether to write an event to the 'eventProcessor' member.</param>
            public new void RemoveEntity(String entityType, String entity, Boolean generateEvent)
            {
                base.RemoveEntity(entityType, entity, generateEvent);
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Metrics.UnitTests.DistributedAccessManagerTests+DistributedAccessManagerWithProtectedMembers class.
            /// </summary>
            /// <param name="metricLogger">The logger for metrics.</param>
            public DistributedAccessManagerWithProtectedMembers(IMetricLogger metricLogger)
                : base(metricLogger)
            {
            }
        }

        #endregion
    }
}
