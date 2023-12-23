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
using ApplicationAccess.Metrics;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;
using ApplicationMetrics;

namespace ApplicationAccess.Distribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Metrics.DistributedAccessManager class.
    /// </summary>
    public class DistributedAccessManagerTests
    {
        private IMetricLogger mockMetricLogger;
        private DistributedAccessManager<String, String, ApplicationScreen, AccessLevel> testDistributedAccessManager;

        [SetUp]
        protected void SetUp()
        {
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(false, mockMetricLogger);
        }

        [Test]
        public void Constructor_StoreBidirectionalMappingsParameterSetCorrectlyOnComposedFields()
        {
            DistributedAccessManager<String, String, ApplicationScreen, AccessLevel> testDistributedAccessManager;
            var fieldNamePath = new List<String>() { "storeBidirectionalMappings" };
            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testDistributedAccessManager);


            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(false, mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDistributedAccessManager);


            fieldNamePath = new List<String>() { "userToGroupMap", "storeBidirectionalMappings" };
            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, true, testDistributedAccessManager);


            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(false, mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDistributedAccessManager);
        }

        [Test]
        public void Constructor_MappingMetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            DistributedAccessManager<String, String, ApplicationScreen, AccessLevel> testDistributedAccessManager;
            var fieldNamePath = new List<String>() { "mappingMetricLogger" };
            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(true, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testDistributedAccessManager);
        }

        [Test]
        public void Constructor_UserToGroupMapAcquireLocksParameterSetCorrectlyOnComposedFields()
        {
            DistributedAccessManager<String, String, ApplicationScreen, AccessLevel> testDistributedAccessManager;
            var fieldNamePath = new List<String>() { "userToGroupMap", "acquireLocks" };
            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(true, mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDistributedAccessManager);


            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(false, mockMetricLogger);

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testDistributedAccessManager);
        }

        [Test]
        public void Constructor_UserToGroupMapMetricLoggerParameterSetCorrectlyOnComposedFields()
        {
            DistributedAccessManager<String, String, ApplicationScreen, AccessLevel> testDistributedAccessManager;
            var fieldNamePath = new List<String>() { "userToGroupMap", "metricLogger" };
            testDistributedAccessManager = new DistributedAccessManager<String, String, ApplicationScreen, AccessLevel>(true, mockMetricLogger);

            NonPublicFieldAssert.IsOfType<MappingMetricLogger>(fieldNamePath, testDistributedAccessManager);
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

        #endregion
    }
}
