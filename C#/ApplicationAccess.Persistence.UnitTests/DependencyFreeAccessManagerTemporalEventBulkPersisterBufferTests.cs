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
using System.Globalization;
using System.Threading;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using ApplicationAccess.Validation;
using ApplicationMetrics;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer class.
    /// </summary>
    public class DependencyFreeAccessManagerTemporalEventBulkPersisterBufferTests
    {
        // N.b. First test of each 'type' of event (primary add, primary remove, secondary add, secondary remove) contains comments explaining purpose of each test

        protected IMethodCallInterceptor dateTimeProviderMethodCallInterceptor;
        protected IMethodCallInterceptor eventValidatorMethodCallInterceptor;
        protected IAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel> mockEventValidator;
        protected IAccessManagerEventBufferFlushStrategy mockBufferFlushStrategy;
        protected IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel> mockEventPersister;
        protected IMetricLogger mockMetricLogger;
        protected IGuidProvider mockGuidProvider;
        protected IDateTimeProvider mockDateTimeProvider;
        protected DependencyFreeAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer;

        [SetUp]
        protected void SetUp()
        {
            mockBufferFlushStrategy = Substitute.For<IAccessManagerEventBufferFlushStrategy>();
            dateTimeProviderMethodCallInterceptor = Substitute.For<IMethodCallInterceptor>();
            eventValidatorMethodCallInterceptor = Substitute.For<IMethodCallInterceptor>();
            mockEventValidator = Substitute.For<IAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>>();
            mockEventPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockGuidProvider = Substitute.For<IGuidProvider>();
            mockDateTimeProvider = Substitute.For<IDateTimeProvider>();
            var methodInterceptingDateTimeProvider = new MethodInterceptingDateTimeProvider(dateTimeProviderMethodCallInterceptor, mockDateTimeProvider);
            var methodInterceptingValidator = new MethodInterceptingAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>(eventValidatorMethodCallInterceptor, new NullAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>());
            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer = new DependencyFreeAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(methodInterceptingValidator, mockBufferFlushStrategy, mockEventPersister, mockMetricLogger, mockGuidProvider, methodInterceptingDateTimeProvider);
        }

        [Test]
        public void AddUser()
        {
            // Standard test for the AddUser() method where no event buffer locks are previously set

            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:55");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToGroupMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddUser(user);

            mockBufferFlushStrategy.Received(1).UserEventBufferItemCount = 1;
            Assert.AreEqual(1, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBuffer.Count);
            UserEventBufferItem<String> bufferedEvent = testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddUser_LocksAlreadySet()
        {
            // Test for the AddUser() method where event buffer locks have already been set (i.e. simulating calling the method as a result of a prepended add user event)

            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:55");
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);

            lock (testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBufferLock)
            {
                testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddUser(user);
            }

            mockBufferFlushStrategy.Received(1).UserEventBufferItemCount = 1;
            Assert.AreEqual(1, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBuffer.Count);
            UserEventBufferItem<String> bufferedEvent = testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBuffer.First.Value.Item2);
        }

        [Test]
        public void RemoveUser_CorrectLocksAreSet()
        {
            // Tests that the correct locks are set when the RemoveUser() method is called (checking that the lock manager is configured and called correctly)

            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:56");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToGroupMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.RemoveUser(user);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddGroup()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:57");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToGroupMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddGroup(group);

            mockBufferFlushStrategy.Received(1).GroupEventBufferItemCount = 1;
            Assert.AreEqual(1, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBuffer.Count);
            GroupEventBufferItem<String> bufferedEvent = testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddGroup_LocksAlreadySet()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:57");
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);

            lock (testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBufferLock)
            {
                testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddGroup(group);
            }

            mockBufferFlushStrategy.Received(1).GroupEventBufferItemCount = 1;
            Assert.AreEqual(1, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBuffer.Count);
            GroupEventBufferItem<String> bufferedEvent = testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBuffer.First.Value.Item2);
        }

        [Test]
        public void RemoveGroup_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:57");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToGroupMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddGroup(group);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddUserToGroupMapping_CorrectLocksAreSet()
        {
            // Tests that the correct locks are set when the AddUserToGroupMapping() method is called (checking that the lock manager is configured and called correctly)

            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:59");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToGroupMappingEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddUserToGroupMapping(user, group);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveUserToGroupMapping_CorrectLocksAreSet()
        {
            // Tests that the correct locks are set when the RemoveUserToGroupMapping() method is called (checking that the lock manager is configured and called correctly)

            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:00");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToGroupMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.RemoveUserToGroupMapping(user, group);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddGroupToGroupMapping_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String fromGroup = "group1";
            const String toGroup = "group2";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:01");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToGroupMappingEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddGroupToGroupMapping(fromGroup, toGroup);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveGroupToGroupMapping_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String fromGroup = "group1";
            const String toGroup = "group2";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:02");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToGroupMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.RemoveGroupToGroupMapping(fromGroup, toGroup);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:03");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Summary, AccessLevel.View);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:04");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.RemoveUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Summary, AccessLevel.View);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:05");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Create);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:06");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Create);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddEntityType()
        {
            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:07");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToEntityMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddEntityType(entityType);

            mockBufferFlushStrategy.Received(1).EntityTypeEventBufferItemCount = 1;
            Assert.AreEqual(1, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBuffer.Count);
            EntityTypeEventBufferItem bufferedEvent = testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddEntityType_LocksAlreadySet()
        {
            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:07");
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);

            lock (testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBufferLock)
            {
                testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddEntityType(entityType);
            }

            mockBufferFlushStrategy.Received(1).EntityTypeEventBufferItemCount = 1;
            Assert.AreEqual(1, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBuffer.Count);
            EntityTypeEventBufferItem bufferedEvent = testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBuffer.First.Value.Item2);
        }

        [Test]
        public void RemoveEntityType_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:08");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToEntityMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.RemoveEntityType(entityType);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddEntity()
        {
            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:09");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToEntityMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddEntity(entityType, entity);

            mockBufferFlushStrategy.Received(1).EntityEventBufferItemCount = 1;
            Assert.AreEqual(1, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBuffer.Count);
            EntityEventBufferItem bufferedEvent = testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddEntity_LocksAlreadySet()
        {
            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:09");
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);

            lock (testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBufferLock)
            {
                testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddEntity(entityType, entity);
            }

            mockBufferFlushStrategy.Received(1).EntityEventBufferItemCount = 1;
            Assert.AreEqual(1, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBuffer.Count);
            EntityEventBufferItem bufferedEvent = testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBuffer.First.Value.Item2);
        }

        [Test]
        public void RemoveEntity_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:10");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToEntityMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.RemoveEntity(entityType, entity);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddUserToEntityMapping_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:11");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToEntityMappingEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddUserToEntityMapping(user, entityType, entity);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveUserToEntityMapping_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:12");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserToEntityMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.UserEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.RemoveUserToEntityMapping(user, entityType, entity);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddGroupToEntityMapping_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:13");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToEntityMappingEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.AddGroupToEntityMapping(group, entityType, entity);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveGroupToEntityMapping_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:14");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });

            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupToEntityMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.GroupEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDependencyFreeAccessManagerTemporalEventBulkPersisterBuffer.RemoveGroupToEntityMapping(group, entityType, entity);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates a DateTime from the specified yyyy-MM-dd HH:mm:ss format string.
        /// </summary>
        /// <param name="stringifiedDateTime">The stringified date/time to convert.</param>
        /// <returns>A DateTime.</returns>
        protected DateTime CreateDataTimeFromString(String stringifiedDateTime)
        {
            DateTime returnDateTime = DateTime.ParseExact(stringifiedDateTime, "yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo);

            return DateTime.SpecifyKind(returnDateTime, DateTimeKind.Utc);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Version of the DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer class where private and protected methods are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        protected class DependencyFreeAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>The queue used to buffer user events.</summary>
            public LinkedList<Tuple<UserEventBufferItem<TUser>, Int64>> UserEventBuffer
            {
                get { return userEventBuffer; }
            }

            /// <summary>The queue used to buffer group events.</summary>
            public LinkedList<Tuple<GroupEventBufferItem<TGroup>, Int64>> GroupEventBuffer
            {
                get { return groupEventBuffer; }
            }

            /// <summary>The queue used to buffer user to group mapping events.</summary>
            public LinkedList<Tuple<UserToGroupMappingEventBufferItem<TUser, TGroup>, Int64>> UserToGroupMappingEventBuffer
            {
                get { return userToGroupMappingEventBuffer; }
            }

            /// <summary>The queue used to buffer group to group mapping events.</summary>
            public LinkedList<Tuple<GroupToGroupMappingEventBufferItem<TGroup>, Int64>> GroupToGroupMappingEventBuffer
            {
                get { return groupToGroupMappingEventBuffer; }
            }

            /// <summary>The queue used to buffer user to application component and access level mapping events.</summary>
            public LinkedList<Tuple<UserToApplicationComponentAndAccessLevelMappingEventBufferItem<TUser, TComponent, TAccess>, Int64>> UserToApplicationComponentAndAccessLevelMappingEventBuffer
            {
                get { return userToApplicationComponentAndAccessLevelMappingEventBuffer; }
            }

            /// <summary>The queue used to buffer group to application component and access level mapping events.</summary>
            public LinkedList<Tuple<GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<TGroup, TComponent, TAccess>, Int64>> GroupToApplicationComponentAndAccessLevelMappingEventBuffer
            {
                get { return groupToApplicationComponentAndAccessLevelMappingEventBuffer; }
            }

            /// <summary>The queue used to buffer entity type events.</summary>
            public LinkedList<Tuple<EntityTypeEventBufferItem, Int64>> EntityTypeEventBuffer
            {
                get { return entityTypeEventBuffer; }
            }

            /// <summary>The queue used to buffer entity events.</summary>
            public LinkedList<Tuple<EntityEventBufferItem, Int64>> EntityEventBuffer
            {
                get { return entityEventBuffer; }
            }

            /// <summary>The queue used to buffer user to entity mapping events.</summary>
            public LinkedList<Tuple<UserToEntityMappingEventBufferItem<TUser>, Int64>> UserToEntityMappingEventBuffer
            {
                get { return userToEntityMappingEventBuffer; }
            }

            /// <summary>The queue used to buffer group to entity mapping events.</summary>
            public LinkedList<Tuple<GroupToEntityMappingEventBufferItem<TGroup>, Int64>> GroupToEntityMappingEventBuffer
            {
                get { return groupToEntityMappingEventBuffer; }
            }

            /// <summary>Lock objects for the user event queue.</summary>
            public Object UserEventBufferLock
            {
                get { return userEventBufferLock; }
            }

            /// <summary>Lock objects for the group event queue.</summary>
            public Object GroupEventBufferLock
            {
                get { return groupEventBufferLock; }
            }

            /// <summary>Lock objects for the user to group mapping event queue.</summary>
            public Object UserToGroupMappingEventBufferLock
            {
                get { return userToGroupMappingEventBufferLock; }
            }

            /// <summary>Lock objects for the group to group mapping event queue.</summary>
            public Object GroupToGroupMappingEventBufferLock
            {
                get { return groupToGroupMappingEventBufferLock; }
            }

            /// <summary>Lock objects for the user to application component and access level mapping event queue.</summary>
            public Object UserToApplicationComponentAndAccessLevelMappingEventBufferLock
            {
                get { return userToApplicationComponentAndAccessLevelMappingEventBufferLock; }
            }

            /// <summary>Lock objects for the group to application component and access level mapping event queue.</summary>
            public Object GroupToApplicationComponentAndAccessLevelMappingEventBufferLock
            {
                get { return groupToApplicationComponentAndAccessLevelMappingEventBufferLock; }
            }

            /// <summary>Lock objects for the entity type event queue.</summary>
            public Object EntityTypeEventBufferLock
            {
                get { return entityTypeEventBufferLock; }
            }

            /// <summary>Lock objects for the entity event queue.</summary>
            public Object EntityEventBufferLock
            {
                get { return entityEventBufferLock; }
            }

            /// <summary>Lock objects for the user to entity mapping event queue.</summary>
            public Object UserToEntityMappingEventBufferLock
            {
                get { return userToEntityMappingEventBufferLock; }
            }

            /// <summary>Lock objects for the group to entity mapping event queue.</summary>
            public Object GroupToEntityMappingEventBufferLock
            {
                get { return groupToEntityMappingEventBufferLock; }
            }

            /// <summary>Lock object for the 'lastEventSequenceNumber' and 'dateTimeProvider' members, to ensure that their sequence orders are maintained between queuing of different events.</summary>
            public Object EventSequenceNumberLock
            {
                get { return eventSequenceNumberLock; }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Persistence.UnitTests.DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer+DependencyFreeAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers class.
            /// </summary>
            /// <param name="eventValidator">The validator to use to validate events.</param>
            /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
            /// <param name="eventPersister">The bulk persister to use to write flushed events to permanent storage.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            /// <param name="guidProvider">The provider to use for random Guids.</param>
            /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
            public DependencyFreeAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers
            (
                IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
                IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
                IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
                IMetricLogger metricLogger,
                IGuidProvider guidProvider,
                IDateTimeProvider dateTimeProvider
            )
                : base(eventValidator, bufferFlushStrategy, eventPersister, metricLogger, guidProvider, dateTimeProvider)
            {
            }
        }
    }

    #endregion
}

