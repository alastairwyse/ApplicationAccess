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
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;
using ApplicationMetrics;
using ApplicationAccess.UnitTests;

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
