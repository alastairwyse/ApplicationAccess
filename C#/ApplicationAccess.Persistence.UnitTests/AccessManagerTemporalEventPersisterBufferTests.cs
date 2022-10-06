/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Globalization;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Validation;
using ApplicationAccess.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBuffer class.
    /// </summary>
    public class AccessManagerTemporalEventPersisterBufferTests
    {
        private AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testAccessManagerTemporalEventPersisterBuffer;
        private IMethodCallInterceptor methodCallInterceptor;
        private IAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel> mockEventValidator;
        private IAccessManagerEventBufferFlushStrategy mockBufferFlushStrategy;
        private IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel> mockEventPersister;
        private IGuidProvider mockGuidProvider;
        private IDateTimeProvider mockDateTimeProvider;

        [SetUp]
        protected void SetUp()
        {
            mockBufferFlushStrategy = Substitute.For<IAccessManagerEventBufferFlushStrategy>();
            methodCallInterceptor = Substitute.For<IMethodCallInterceptor>();
            mockEventValidator = Substitute.For<IAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>>();
            mockEventPersister = Substitute.For<IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockGuidProvider = Substitute.For<IGuidProvider>();
            mockDateTimeProvider = Substitute.For<IDateTimeProvider>();
            var methodInterceptingValidator = new MethodInterceptingAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>(methodCallInterceptor, new NullAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>());
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(methodInterceptingValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
        }

        [Test]
        public void AddUser()
        {
            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:55");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddUser(user);

            mockBufferFlushStrategy.Received(1).UserEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserEventBuffer.Count);
            UserEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void AddUser_ValidationFails()
        {
            const String user = "user1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateAddUser(user, Arg.Any<Action<String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.AddUser(user);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void RemoveUser()
        {
            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:56");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveUser(user);

            mockBufferFlushStrategy.Received(1).UserEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserEventBuffer.Count);
            UserEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void RemoveUser_ValidationFails()
        {
            const String user = "user1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateRemoveUser(user, Arg.Any<Action<String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.RemoveUser(user);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void AddGroup()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:57");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddGroup(group);

            mockBufferFlushStrategy.Received(1).GroupEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupEventBuffer.Count);
            GroupEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void AddGroup_ValidationFails()
        {
            const String group = "group1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateAddGroup(group, Arg.Any<Action<String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.AddGroup(group);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void RemoveGroup()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:58");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroup(group);

            mockBufferFlushStrategy.Received(1).GroupEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupEventBuffer.Count);
            GroupEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void RemoveGroup_ValidationFails()
        {
            const String group = "group1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateRemoveGroup(group, Arg.Any<Action<String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.RemoveGroup(group);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void AddUserToGroupMapping()
        {
            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:59");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddUserToGroupMapping(user, group);

            mockBufferFlushStrategy.Received(1).UserToGroupMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBuffer.Count);
            UserToGroupMappingEventBufferItem<String, String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void AddUserToGroupMapping_ValidationFails()
        {
            const String user = "user1";
            const String group = "group1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateAddUserToGroupMapping(user, group, Arg.Any<Action<String, String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.AddUserToGroupMapping(user, group);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void RemoveUserToGroupMapping()
        {
            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:00");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToGroupMapping(user, group);

            mockBufferFlushStrategy.Received(1).UserToGroupMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBuffer.Count);
            UserToGroupMappingEventBufferItem<String, String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void RemoveUserToGroupMapping_ValidationFails()
        {
            const String user = "user1";
            const String group = "group1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateRemoveUserToGroupMapping(user, group, Arg.Any<Action<String, String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.RemoveUserToGroupMapping(user, group);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void AddGroupToGroupMapping()
        {
            Guid eventId = Guid.NewGuid();
            const String fromGroup = "group1";
            const String toGroup = "group2";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:01");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddGroupToGroupMapping(fromGroup, toGroup);

            mockBufferFlushStrategy.Received(1).GroupToGroupMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBuffer.Count);
            GroupToGroupMappingEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(fromGroup, bufferedEvent.FromGroup);
            Assert.AreEqual(toGroup, bufferedEvent.ToGroup);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void AddGroupToGroupMapping_ValidationFails()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateAddGroupToGroupMapping(fromGroup, toGroup, Arg.Any<Action<String, String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.AddGroupToGroupMapping(fromGroup, toGroup);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void RemoveGroupToGroupMapping()
        {
            Guid eventId = Guid.NewGuid();
            const String fromGroup = "group1";
            const String toGroup = "group2";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:02");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToGroupMapping(fromGroup, toGroup);

            mockBufferFlushStrategy.Received(1).GroupToGroupMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBuffer.Count);
            GroupToGroupMappingEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(fromGroup, bufferedEvent.FromGroup);
            Assert.AreEqual(toGroup, bufferedEvent.ToGroup);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void RemoveGroupToGroupMapping_ValidationFails()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateRemoveGroupToGroupMapping(fromGroup, toGroup, Arg.Any<Action<String, String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToGroupMapping(fromGroup, toGroup);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping()
        {
            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:03");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Summary, AccessLevel.View);

            mockBufferFlushStrategy.Received(1).UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(ApplicationScreen.Summary, bufferedEvent.ApplicationComponent);
            Assert.AreEqual(AccessLevel.View, bufferedEvent.AccessLevel);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_ValidationFails()
        {
            const String user = "user1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateAddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Summary, AccessLevel.View, Arg.Any<Action<String, ApplicationScreen, AccessLevel>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Summary, AccessLevel.View);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping()
        {
            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:04");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Summary, AccessLevel.View);

            mockBufferFlushStrategy.Received(1).UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(ApplicationScreen.Summary, bufferedEvent.ApplicationComponent);
            Assert.AreEqual(AccessLevel.View, bufferedEvent.AccessLevel);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_ValidationFails()
        {
            const String user = "user1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Summary, AccessLevel.View, Arg.Any<Action<String, ApplicationScreen, AccessLevel>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.RemoveUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Summary, AccessLevel.View);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:05");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Create);

            mockBufferFlushStrategy.Received(1).GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(ApplicationScreen.Order, bufferedEvent.ApplicationComponent);
            Assert.AreEqual(AccessLevel.Create, bufferedEvent.AccessLevel);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_ValidationFails()
        {
            const String group = "group1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateAddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Create, Arg.Any<Action<String, ApplicationScreen, AccessLevel>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:06");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Create);

            mockBufferFlushStrategy.Received(1).GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(ApplicationScreen.Order, bufferedEvent.ApplicationComponent);
            Assert.AreEqual(AccessLevel.Create, bufferedEvent.AccessLevel);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_ValidationFails()
        {
            const String group = "group1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Create, Arg.Any<Action<String, ApplicationScreen, AccessLevel>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Create);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void AddEntityType()
        {
            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:07");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddEntityType(entityType);

            mockBufferFlushStrategy.Received(1).EntityTypeEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBuffer.Count);
            EntityTypeEventBufferItem bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void AddEntityType_ValidationFails()
        {
            const String entityType = "ClientAccount";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateAddEntityType(entityType, Arg.Any<Action<String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.AddEntityType(entityType);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void RemoveEntityType()
        {
            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:08");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveEntityType(entityType);

            mockBufferFlushStrategy.Received(1).EntityTypeEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBuffer.Count);
            EntityTypeEventBufferItem bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void RemoveEntityType_ValidationFails()
        {
            const String entityType = "ClientAccount";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateRemoveEntityType(entityType, Arg.Any<Action<String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.RemoveEntityType(entityType);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void AddEntity()
        {
            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:09");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EntityEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddEntity(entityType, entity);

            mockBufferFlushStrategy.Received(1).EntityEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.EntityEventBuffer.Count);
            EntityEventBufferItem bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.EntityEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.EntityEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void AddEntity_ValidationFails()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateAddEntity(entityType, entity, Arg.Any<Action<String, String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.AddEntity(entityType, entity);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void RemoveEntity()
        {
            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:10");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EntityEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveEntity(entityType, entity);

            mockBufferFlushStrategy.Received(1).EntityEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.EntityEventBuffer.Count);
            EntityEventBufferItem bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.EntityEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.EntityEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void RemoveEntity_ValidationFails()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateRemoveEntity(entityType, entity, Arg.Any<Action<String, String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.RemoveEntity(entityType, entity);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void AddUserToEntityMapping()
        {
            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:11");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddUserToEntityMapping(user, entityType, entity);

            mockBufferFlushStrategy.Received(1).UserToEntityMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBuffer.Count);
            UserToEntityMappingEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void AddUserToEntityMapping_ValidationFails()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateAddUserToEntityMapping(user, entityType, entity, Arg.Any<Action<String, String, String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.AddUserToEntityMapping(user, entityType, entity);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void RemoveUserToEntityMapping()
        {
            Guid eventId = Guid.NewGuid();
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:12");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToEntityMapping(user, entityType, entity);

            mockBufferFlushStrategy.Received(1).UserToEntityMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBuffer.Count);
            UserToEntityMappingEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void RemoveUserToEntityMapping_ValidationFails()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateRemoveUserToEntityMapping(user, entityType, entity, Arg.Any<Action<String, String, String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.RemoveUserToEntityMapping(user, entityType, entity);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void AddGroupToEntityMapping()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:13");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddGroupToEntityMapping(group, entityType, entity);

            mockBufferFlushStrategy.Received(1).GroupToEntityMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBuffer.Count);
            GroupToEntityMappingEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void AddGroupToEntityMapping_ValidationFails()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateAddGroupToEntityMapping(group, entityType, entity, Arg.Any<Action<String, String, String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.AddGroupToEntityMapping(group, entityType, entity);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void RemoveGroupToEntityMapping()
        {
            Guid eventId = Guid.NewGuid();
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:14");
            Boolean assertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            methodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBufferLock));
                assertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToEntityMapping(group, entityType, entity);

            mockBufferFlushStrategy.Received(1).GroupToEntityMappingEventBufferItemCount = 1;
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBuffer.Count);
            GroupToEntityMappingEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void RemoveGroupToEntityMapping_ValidationFails()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>(mockEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            var mockException = new Exception(mockExceptionMessage);
            var validationResult = new ValidationResult(false, mockException.Message, mockException);
            mockEventValidator.ValidateRemoveGroupToEntityMapping(group, entityType, entity, Arg.Any<Action<String, String, String>>()).Returns(validationResult);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToEntityMapping(group, entityType, entity);
            });

            Assert.That(e.Message, Does.StartWith(mockExceptionMessage));
        }

        [Test]
        public void Flush_CallToPersisterAddUserFails()
        {
            const String user = "user1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000001");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:55");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.AddUser(user, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddUser(user);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add user' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveUserFails()
        {
            const String user = "user1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000002");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 11:39:55");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveUser(user, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveUser(user);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove user' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddGroupFails()
        {
            const String group = "group1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000003");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-10 22:50:15");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.AddGroup(group, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddGroup(group);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add group' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveGroupFails()
        {
            const String group = "group1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000004");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 22:51:55");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveGroup(group, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroup(group);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove group' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddUserToGroupMappingFails()
        {
            const String user = "user1";
            const String group = "group1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000005");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-11 22:56:15");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.AddUserToGroupMapping(user, group, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddUserToGroupMapping(user, group);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add user to group mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveUserToGroupMappingFails()
        {
            const String user = "user1";
            const String group = "group1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000006");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 22:59:07");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveUserToGroupMapping(user, group, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToGroupMapping(user, group);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove user to group mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddGroupToGroupMappingFails()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000007");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-11 23:02:15");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.AddGroupToGroupMapping(fromGroup, toGroup, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddGroupToGroupMapping(fromGroup, toGroup);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add group to group mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveGroupToGroupMappingFails()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000008");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 23:02:16");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveGroupToGroupMapping(fromGroup, toGroup, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToGroupMapping(fromGroup, toGroup);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove group to group mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddUserToApplicationComponentAndAccessLevelMappingFails()
        {
            const String user = "user1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000009");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-11 23:05:16");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Order, AccessLevel.Delete, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Order, AccessLevel.Delete);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add user to application component and access level mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveUserToApplicationComponentAndAccessLevelMappingFails()
        {
            const String user = "user1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-00000000000a");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 23:07:17");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Settings, AccessLevel.Modify, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Settings, AccessLevel.Modify);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove user to application component and access level mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddGroupToApplicationComponentAndAccessLevelMappingFails()
        {
            const String group = "group1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-00000000000b");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-11 23:10:18");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Delete, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Delete);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add group to application component and access level mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveGroupToApplicationComponentAndAccessLevelMappingFails()
        {
            const String group = "group1";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-00000000000c");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 23:10:19");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Settings, AccessLevel.Modify, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Settings, AccessLevel.Modify);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove group to application component and access level mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddEntityTypeFails()
        {
            const String entityType = "Products";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-00000000000d");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 12:07:20");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.AddEntityType(entityType, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddEntityType(entityType);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add entity type' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveEntityTypeFails()
        {
            const String entityType = "Products";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-00000000000e");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 12:08:21");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveEntityType(entityType, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveEntityType(entityType);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove entity type' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddEntityFails()
        {
            const String entityType = "Clients";
            const String entity = "CompanyA";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-00000000000f");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 12:09:22");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.AddEntity(entityType, entity, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddEntity(entityType, entity);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add entity' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveEntityFails()
        {
            const String entityType = "Clients";
            const String entity = "CompanyA";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000010");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 12:10:23");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveEntity(entityType, entity, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveEntity(entityType, entity);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove entity' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddUserToEntityMappingFails()
        {
            const String user = "user1";
            const String entityType = "Clients";
            const String entity = "CompanyA";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000011");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 12:18:24");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.AddUserToEntityMapping(user, entityType, entity, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddUserToEntityMapping(user, entityType, entity);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add user to entity mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveUserToEntityMappingFails()
        {
            const String user = "user1";
            const String entityType = "Clients";
            const String entity = "CompanyA";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000012");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 12:19:24");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveUserToEntityMapping(user, entityType, entity, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToEntityMapping(user, entityType, entity);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove user to entity mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterAddGroupToEntityMappingFails()
        {
            const String group = "group1";
            const String entityType = "Clients";
            const String entity = "CompanyB";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000013");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 13:16:27");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.AddGroupToEntityMapping(group, entityType, entity, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.AddGroupToEntityMapping(group, entityType, entity);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'add group to entity mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush_CallToPersisterRemoveGroupToEntityMappingFails()
        {
            const String group = "group1";
            const String entityType = "Clients";
            const String entity = "CompanyB";
            Guid eventId = Guid.Parse("00000000-0000-0000-0000-000000000014");
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-12 13:17:28");
            var mockException = new Exception("Failed to persist event.");

            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGuidProvider.NewGuid().Returns<Guid>(eventId);
            mockEventPersister.When((eventPersister) => eventPersister.RemoveGroupToEntityMapping(group, entityType, entity, eventId, eventOccurredTime)).Do((callInfo) => throw mockException);

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToEntityMapping(group, entityType, entity);

            var e = Assert.Throws<Exception>(delegate
            {
                testAccessManagerTemporalEventPersisterBuffer.Flush();
            });

            Assert.That(e.Message, Does.StartWith("Failed to persist 'remove group to entity mapping' event."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Flush()
        {
            var guid0 = Guid.Parse("00000000-0000-0000-0000-000000000000");
            var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var guid3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var guid4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var guid5 = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var guid6 = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var guid7 = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var guid8 = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var guid9 = Guid.Parse("00000000-0000-0000-0000-000000000009");
            var guid10 = Guid.Parse("00000000-0000-0000-0000-00000000000a");
            var guid11 = Guid.Parse("00000000-0000-0000-0000-00000000000b");
            var guid12 = Guid.Parse("00000000-0000-0000-0000-00000000000c");
            var guid13 = Guid.Parse("00000000-0000-0000-0000-00000000000d");
            var guid14 = Guid.Parse("00000000-0000-0000-0000-00000000000e");
            var guid15 = Guid.Parse("00000000-0000-0000-0000-00000000000f");
            var guid16 = Guid.Parse("00000000-0000-0000-0000-000000000010");
            var guid17 = Guid.Parse("00000000-0000-0000-0000-000000000011");
            var guid18 = Guid.Parse("00000000-0000-0000-0000-000000000012");
            var guid19 = Guid.Parse("00000000-0000-0000-0000-000000000013");
            var guid20 = Guid.Parse("00000000-0000-0000-0000-000000000014");
            var guid21 = Guid.Parse("00000000-0000-0000-0000-000000000015");
            var guid22 = Guid.Parse("00000000-0000-0000-0000-000000000016");
            var guid23 = Guid.Parse("00000000-0000-0000-0000-000000000017");
            var guid24 = Guid.Parse("00000000-0000-0000-0000-000000000018");
            var guid25 = Guid.Parse("00000000-0000-0000-0000-000000000019");
            var guid26 = Guid.Parse("00000000-0000-0000-0000-00000000001a");
            var guid27 = Guid.Parse("00000000-0000-0000-0000-00000000001b");
            mockBufferFlushStrategy.ClearReceivedCalls();
            mockGuidProvider.NewGuid().Returns<Guid>
            (
                guid0,
                guid1,
                guid2,
                guid3,
                guid4,
                guid5,
                guid6,
                guid7,
                guid8,
                guid9,
                guid10,
                guid11,
                guid12,
                guid13,
                guid14,
                guid15,
                guid16,
                guid17,
                guid18,
                guid19,
                guid20,
                guid21,
                guid22,
                guid23,
                guid24, 
                guid25,
                guid26,
                guid27
            );
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                CreateDataTimeFromString("2021-06-12 13:43:00"),
                CreateDataTimeFromString("2021-06-12 13:43:01"),
                CreateDataTimeFromString("2021-06-12 13:43:02"),
                CreateDataTimeFromString("2021-06-12 13:43:03"),
                CreateDataTimeFromString("2021-06-12 13:43:04"),
                CreateDataTimeFromString("2021-06-12 13:43:05"),
                CreateDataTimeFromString("2021-06-12 13:43:06"),
                CreateDataTimeFromString("2021-06-12 13:43:07"),
                CreateDataTimeFromString("2021-06-12 13:43:08"),
                CreateDataTimeFromString("2021-06-12 13:43:09"),
                CreateDataTimeFromString("2021-06-12 13:43:10"),
                CreateDataTimeFromString("2021-06-12 13:43:11"),
                CreateDataTimeFromString("2021-06-12 13:43:12"),
                CreateDataTimeFromString("2021-06-12 13:43:13"),
                CreateDataTimeFromString("2021-06-12 13:43:14"),
                CreateDataTimeFromString("2021-06-12 13:43:15"),
                CreateDataTimeFromString("2021-06-12 13:43:16"),
                CreateDataTimeFromString("2021-06-12 13:43:17"),
                CreateDataTimeFromString("2021-06-12 13:43:18"),
                CreateDataTimeFromString("2021-06-12 13:43:19"),
                CreateDataTimeFromString("2021-06-12 13:43:20"),
                CreateDataTimeFromString("2021-06-12 13:43:21"),
                CreateDataTimeFromString("2021-06-12 13:43:22"),
                CreateDataTimeFromString("2021-06-12 13:43:23"),
                CreateDataTimeFromString("2021-06-12 13:43:24"),
                CreateDataTimeFromString("2021-06-12 13:43:25"),
                CreateDataTimeFromString("2021-06-12 13:43:26"),
                CreateDataTimeFromString("2021-06-12 13:43:27")
            );

            testAccessManagerTemporalEventPersisterBuffer.AddEntityType("Clients");
            testAccessManagerTemporalEventPersisterBuffer.AddEntity("Clients", "CompanyA");
            testAccessManagerTemporalEventPersisterBuffer.AddGroup("group1");
            testAccessManagerTemporalEventPersisterBuffer.AddUser("user1");
            testAccessManagerTemporalEventPersisterBuffer.AddUser("user2");
            testAccessManagerTemporalEventPersisterBuffer.AddGroup("group2");
            testAccessManagerTemporalEventPersisterBuffer.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.View);
            testAccessManagerTemporalEventPersisterBuffer.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify);
            testAccessManagerTemporalEventPersisterBuffer.AddGroupToGroupMapping("group1", "group2");
            testAccessManagerTemporalEventPersisterBuffer.AddUserToGroupMapping("user1", "group2");
            testAccessManagerTemporalEventPersisterBuffer.AddGroupToEntityMapping("group2", "Clients", "CompanyA");
            testAccessManagerTemporalEventPersisterBuffer.AddUserToGroupMapping("user2", "group1");
            testAccessManagerTemporalEventPersisterBuffer.AddUserToEntityMapping("user1", "Clients", "CompanyA");
            testAccessManagerTemporalEventPersisterBuffer.AddEntity("Clients", "CompanyB");
            testAccessManagerTemporalEventPersisterBuffer.RemoveEntity("Clients", "CompanyB");
            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToEntityMapping("user1", "Clients", "CompanyA");
            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToGroupMapping("user2", "group1");
            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToEntityMapping("group2", "Clients", "CompanyA");
            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToGroupMapping("user1", "group2");
            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToGroupMapping("group1", "group2");
            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify);
            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.View);
            testAccessManagerTemporalEventPersisterBuffer.RemoveGroup("group2");
            testAccessManagerTemporalEventPersisterBuffer.RemoveUser("user2");
            testAccessManagerTemporalEventPersisterBuffer.RemoveUser("user1");
            testAccessManagerTemporalEventPersisterBuffer.RemoveGroup("group1");
            testAccessManagerTemporalEventPersisterBuffer.RemoveEntity("Clients", "CompanyA");
            testAccessManagerTemporalEventPersisterBuffer.RemoveEntityType("Clients");

            testAccessManagerTemporalEventPersisterBuffer.Flush();

            Received.InOrder(() => {
                // These get called as the initial events are buffered
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 1;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 1;
                mockBufferFlushStrategy.UserEventBufferItemCount = 1;
                mockBufferFlushStrategy.UserEventBufferItemCount = 2;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 2;
                mockBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 2;
                mockBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 2;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 3;
                mockBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 2;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 3;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 2;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 4;
                mockBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 2;
                mockBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 2;
                mockBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 2;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 3;
                mockBufferFlushStrategy.UserEventBufferItemCount = 3;
                mockBufferFlushStrategy.UserEventBufferItemCount = 4;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 4;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 4;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 2;
                // These are called as the buffers are cleared
                mockBufferFlushStrategy.UserEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 0;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 0;
                // These are called as the buffered items are processed
                mockEventPersister.AddEntityType("Clients", guid0, CreateDataTimeFromString("2021-06-12 13:43:00"));
                mockEventPersister.AddEntity("Clients", "CompanyA", guid1, CreateDataTimeFromString("2021-06-12 13:43:01"));
                mockEventPersister.AddGroup("group1", guid2, CreateDataTimeFromString("2021-06-12 13:43:02"));
                mockEventPersister.AddUser("user1", guid3, CreateDataTimeFromString("2021-06-12 13:43:03"));
                mockEventPersister.AddUser("user2", guid4, CreateDataTimeFromString("2021-06-12 13:43:04"));
                mockEventPersister.AddGroup("group2", guid5, CreateDataTimeFromString("2021-06-12 13:43:05"));
                mockEventPersister.AddGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.View, guid6, CreateDataTimeFromString("2021-06-12 13:43:06"));
                mockEventPersister.AddUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify, guid7, CreateDataTimeFromString("2021-06-12 13:43:07"));
                mockEventPersister.AddGroupToGroupMapping("group1", "group2", guid8, CreateDataTimeFromString("2021-06-12 13:43:08"));
                mockEventPersister.AddUserToGroupMapping("user1", "group2", guid9, CreateDataTimeFromString("2021-06-12 13:43:09"));
                mockEventPersister.AddGroupToEntityMapping("group2", "Clients", "CompanyA", guid10, CreateDataTimeFromString("2021-06-12 13:43:10"));
                mockEventPersister.AddUserToGroupMapping("user2", "group1", guid11, CreateDataTimeFromString("2021-06-12 13:43:11"));
                mockEventPersister.AddUserToEntityMapping("user1", "Clients", "CompanyA", guid12, CreateDataTimeFromString("2021-06-12 13:43:12"));
                mockEventPersister.AddEntity("Clients", "CompanyB", guid13, CreateDataTimeFromString("2021-06-12 13:43:13"));
                mockEventPersister.RemoveEntity("Clients", "CompanyB", guid14, CreateDataTimeFromString("2021-06-12 13:43:14"));
                mockEventPersister.RemoveUserToEntityMapping("user1", "Clients", "CompanyA", guid15, CreateDataTimeFromString("2021-06-12 13:43:15"));
                mockEventPersister.RemoveUserToGroupMapping("user2", "group1", guid16, CreateDataTimeFromString("2021-06-12 13:43:16"));
                mockEventPersister.RemoveGroupToEntityMapping("group2", "Clients", "CompanyA", guid17, CreateDataTimeFromString("2021-06-12 13:43:17"));
                mockEventPersister.RemoveUserToGroupMapping("user1", "group2", guid18, CreateDataTimeFromString("2021-06-12 13:43:18"));
                mockEventPersister.RemoveGroupToGroupMapping("group1", "group2", guid19, CreateDataTimeFromString("2021-06-12 13:43:19"));
                mockEventPersister.RemoveUserToApplicationComponentAndAccessLevelMapping("user2", ApplicationScreen.Order, AccessLevel.Modify, guid20, CreateDataTimeFromString("2021-06-12 13:43:20"));
                mockEventPersister.RemoveGroupToApplicationComponentAndAccessLevelMapping("group1", ApplicationScreen.Order, AccessLevel.View, guid21, CreateDataTimeFromString("2021-06-12 13:43:21"));
                mockEventPersister.RemoveGroup("group2", guid22, CreateDataTimeFromString("2021-06-12 13:43:22"));
                mockEventPersister.RemoveUser("user2", guid23, CreateDataTimeFromString("2021-06-12 13:43:23"));
                mockEventPersister.RemoveUser("user1", guid24, CreateDataTimeFromString("2021-06-12 13:43:24"));
                mockEventPersister.RemoveGroup("group1", guid25, CreateDataTimeFromString("2021-06-12 13:43:25"));
                mockEventPersister.RemoveEntity("Clients", "CompanyA", guid26, CreateDataTimeFromString("2021-06-12 13:43:26"));
                mockEventPersister.RemoveEntityType("Clients", guid27, CreateDataTimeFromString("2021-06-12 13:43:27"));
            });
            Assert.AreEqual(38, mockBufferFlushStrategy.ReceivedCalls().Count());
            Assert.AreEqual(28, mockEventPersister.ReceivedCalls().Count());
        }

        /// <summary>
        /// Tests that any events which are buffered after the call to Flush() are not processed as part of that Flush() method call.
        /// </summary>
        [Test]
        public void Flush_EventsCreatedAfterCallToFlushAreNotProcessed()
        {
            // This tests that calls to the main public methods of AccessManagerTemporalEventPersisterBuffer like AddUser(), AddGroup() etc... will NOT be included in Flush() processing, if they're called after the start of processing of a current Flush() call
            // This situation would arise in multi-thread environments where a separate thread is processing the flush strategy
            // AccessManagerTemporalEventPersisterBuffer uses field 'lastEventSequenceNumber' to record which events should be processed as part of each Flush()

            const String user1 = "user1";
            const String user2 = "user2";
            const String user3 = "user3";
            const String group1 = "group1";
            const String group2 = "group2";
            const String group3 = "group3";
            const String group4 = "group4";

            var guid0 = Guid.Parse("00000000-0000-0000-0000-000000000000");
            var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var guid3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var guid4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var guid5 = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var guid6 = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var guid7 = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var guid8 = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var guid9 = Guid.Parse("00000000-0000-0000-0000-000000000009");
            var guid10 = Guid.Parse("00000000-0000-0000-0000-00000000000a");
            var guid11 = Guid.Parse("00000000-0000-0000-0000-00000000000b");
            var guid12 = Guid.Parse("00000000-0000-0000-0000-00000000000c");
            var guid13 = Guid.Parse("00000000-0000-0000-0000-00000000000d");
            mockBufferFlushStrategy.ClearReceivedCalls();
            mockGuidProvider.NewGuid().Returns<Guid>
            (
                guid0,
                guid1,
                guid2,
                guid3,
                guid4,
                guid5,
                guid6,
                guid7,
                guid8,
                guid9,
                guid10,
                guid11,
                guid12,
                guid13
            );
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                CreateDataTimeFromString("2021-06-13 09:58:00"),
                CreateDataTimeFromString("2021-06-13 09:58:01"),
                CreateDataTimeFromString("2021-06-13 09:58:02"),
                CreateDataTimeFromString("2021-06-13 09:58:03"),
                CreateDataTimeFromString("2021-06-13 09:58:04"),
                CreateDataTimeFromString("2021-06-13 09:58:05"),
                CreateDataTimeFromString("2021-06-13 09:58:06"),
                CreateDataTimeFromString("2021-06-13 09:58:07"),
                CreateDataTimeFromString("2021-06-13 09:58:08"),
                CreateDataTimeFromString("2021-06-13 09:58:09"),
                CreateDataTimeFromString("2021-06-13 09:58:10"),
                CreateDataTimeFromString("2021-06-13 09:58:11"),
                CreateDataTimeFromString("2021-06-13 09:58:12"),
                CreateDataTimeFromString("2021-06-13 09:58:13")
            );

            mockEventPersister.When((eventPersister) => eventPersister.AddUser(user3, guid2, CreateDataTimeFromString("2021-06-13 09:58:02"))).Do((callInfo) =>
            {
                testAccessManagerTemporalEventPersisterBuffer.AddGroup(group3);
                testAccessManagerTemporalEventPersisterBuffer.AddGroup(group4);
                testAccessManagerTemporalEventPersisterBuffer.AddGroupToGroupMapping(group1, group2);
                testAccessManagerTemporalEventPersisterBuffer.AddEntityType("Clients");
                testAccessManagerTemporalEventPersisterBuffer.AddEntity("Clients", "CompanyA");
                testAccessManagerTemporalEventPersisterBuffer.AddGroupToEntityMapping(group3, "Clients", "CompanyA");
                testAccessManagerTemporalEventPersisterBuffer.AddGroupToEntityMapping(group4, "Clients", "CompanyA");
            });

            testAccessManagerTemporalEventPersisterBuffer.AddUser(user1);
            testAccessManagerTemporalEventPersisterBuffer.AddUser(user2);
            testAccessManagerTemporalEventPersisterBuffer.AddUser(user3);
            testAccessManagerTemporalEventPersisterBuffer.AddGroup(group1);
            testAccessManagerTemporalEventPersisterBuffer.AddGroup(group2);
            testAccessManagerTemporalEventPersisterBuffer.AddUserToGroupMapping(user1, group1);
            testAccessManagerTemporalEventPersisterBuffer.AddUserToGroupMapping(user2, group1);
            testAccessManagerTemporalEventPersisterBuffer.Flush();

            Received.InOrder(() => {
                // These are the calls to the flush strategy that occur within the main public methods of the AccessManagerTemporalEventPersisterBuffer class
                mockBufferFlushStrategy.UserEventBufferItemCount = 1;
                mockBufferFlushStrategy.UserEventBufferItemCount = 2;
                mockBufferFlushStrategy.UserEventBufferItemCount = 3;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 2;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 2;
                // These will get called as the Flush() method starts...
                //    ...these as buffered items are moved to temporary queues
                mockBufferFlushStrategy.UserEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 0;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 0;
                //    ...and these as the buffered items are processed
                mockEventPersister.AddUser(user1, guid0, CreateDataTimeFromString("2021-06-13 09:58:00"));
                mockEventPersister.AddUser(user2, guid1, CreateDataTimeFromString("2021-06-13 09:58:01"));
                mockEventPersister.AddUser(user3, guid2, CreateDataTimeFromString("2021-06-13 09:58:02"));
                // When AddUser(user3) is called, we simulate calling the public methods again, and hence expect further calls to the flush strategy...
                mockBufferFlushStrategy.GroupEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 2;
                mockBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 1;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 2;
                // ...and once these are complete, the processing of the call to Flush() continues
                mockEventPersister.AddGroup(group1, guid3, CreateDataTimeFromString("2021-06-13 09:58:03"));
                mockEventPersister.AddGroup(group2, guid4, CreateDataTimeFromString("2021-06-13 09:58:04"));
                mockEventPersister.AddUserToGroupMapping(user1, group1, guid5, CreateDataTimeFromString("2021-06-13 09:58:05"));
                mockEventPersister.AddUserToGroupMapping(user2, group1, guid6, CreateDataTimeFromString("2021-06-13 09:58:06"));
            });
            Assert.AreEqual(24, mockBufferFlushStrategy.ReceivedCalls().Count());
            Assert.AreEqual(7, mockEventPersister.ReceivedCalls().Count());
            // These method calls would occur as part of a subsequent Flush() call, and hence should not be received
            mockEventPersister.DidNotReceive().AddGroup(group3, Arg.Any<Guid>(), Arg.Any<DateTime>());
            mockEventPersister.DidNotReceive().AddGroup(group4, Arg.Any<Guid>(), Arg.Any<DateTime>());
            mockEventPersister.DidNotReceive().AddGroupToGroupMapping(group1, group2, Arg.Any<Guid>(), Arg.Any<DateTime>());
            mockEventPersister.DidNotReceive().AddEntityType("Clients", Arg.Any<Guid>(), Arg.Any<DateTime>());
            mockEventPersister.DidNotReceive().AddEntity("Clients", "CompanyA", Arg.Any<Guid>(), Arg.Any<DateTime>());
            mockEventPersister.DidNotReceive().AddGroupToEntityMapping(group3, "Clients", "CompanyA", Arg.Any<Guid>(), Arg.Any<DateTime>());
            mockEventPersister.DidNotReceive().AddGroupToEntityMapping(group4, "Clients", "CompanyA", Arg.Any<Guid>(), Arg.Any<DateTime>());


            // Test that a subsequent call to Flush() processes the remaining buffered events correctly
            mockBufferFlushStrategy.ClearReceivedCalls();
            mockEventPersister.ClearReceivedCalls();

            testAccessManagerTemporalEventPersisterBuffer.Flush();

            Received.InOrder(() =>
            {                
                // These will get called as the Flush() method starts...
                //    ...these as buffered items are moved to temporary queues
                mockBufferFlushStrategy.UserEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 0;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 0;
                //    ...and these as the buffered items are processed
                mockEventPersister.AddGroup(group3, guid7, CreateDataTimeFromString("2021-06-13 09:58:07"));
                mockEventPersister.AddGroup(group4, guid8, CreateDataTimeFromString("2021-06-13 09:58:08"));
                mockEventPersister.AddGroupToGroupMapping(group1, group2, guid9, CreateDataTimeFromString("2021-06-13 09:58:09"));
                mockEventPersister.AddEntityType("Clients", guid10, CreateDataTimeFromString("2021-06-13 09:58:10"));
                mockEventPersister.AddEntity("Clients", "CompanyA", guid11, CreateDataTimeFromString("2021-06-13 09:58:11"));
                mockEventPersister.AddGroupToEntityMapping(group3, "Clients", "CompanyA", guid12, CreateDataTimeFromString("2021-06-13 09:58:12"));
                mockEventPersister.AddGroupToEntityMapping(group4, "Clients", "CompanyA", guid13, CreateDataTimeFromString("2021-06-13 09:58:13"));
            });
            Assert.AreEqual(10, mockBufferFlushStrategy.ReceivedCalls().Count());
            Assert.AreEqual(7, mockEventPersister.ReceivedCalls().Count());
        }

        /// <summary>
        /// Tests that any events which are buffered whilst the events to be flushed are moved to temporary queues, are not processed as part of that Flush() method call.
        /// </summary>
        [Test]
        public void Flush_NewEventsCreatedWhilstMovingEventsToTemporaryQueuesNotProcessed()
        {
            var guid0 = Guid.Parse("00000000-0000-0000-0000-000000000000");
            var guid1 = Guid.Parse("00000000-0000-0000-0000-000000000001");
            var guid2 = Guid.Parse("00000000-0000-0000-0000-000000000002");
            var guid3 = Guid.Parse("00000000-0000-0000-0000-000000000003");
            var guid4 = Guid.Parse("00000000-0000-0000-0000-000000000004");
            var guid5 = Guid.Parse("00000000-0000-0000-0000-000000000005");
            var guid6 = Guid.Parse("00000000-0000-0000-0000-000000000006");
            var guid7 = Guid.Parse("00000000-0000-0000-0000-000000000007");
            var guid8 = Guid.Parse("00000000-0000-0000-0000-000000000008");
            var guid9 = Guid.Parse("00000000-0000-0000-0000-000000000009");
            var guid10 = Guid.Parse("00000000-0000-0000-0000-00000000000a");
            var guid11 = Guid.Parse("00000000-0000-0000-0000-00000000000b");
            var guid12 = Guid.Parse("00000000-0000-0000-0000-00000000000c");
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                CreateDataTimeFromString("2021-06-14 20:09:00"),
                CreateDataTimeFromString("2021-06-14 20:09:01"),
                CreateDataTimeFromString("2021-06-14 20:09:02"),
                CreateDataTimeFromString("2021-06-14 20:09:03"),
                CreateDataTimeFromString("2021-06-14 20:09:04"),
                CreateDataTimeFromString("2021-06-14 20:09:05"),
                CreateDataTimeFromString("2021-06-14 20:09:06"),
                CreateDataTimeFromString("2021-06-14 20:09:07"),
                CreateDataTimeFromString("2021-06-14 20:09:08"),
                CreateDataTimeFromString("2021-06-14 20:09:09"),
                CreateDataTimeFromString("2021-06-14 20:09:10"),
                CreateDataTimeFromString("2021-06-14 20:09:11"),
                CreateDataTimeFromString("2021-06-14 20:09:12")
            ); 
            mockGuidProvider.NewGuid().Returns<Guid>
            (
                guid0,
                guid1,
                guid2,
                guid3,
                guid4,
                guid5,
                guid6,
                guid7,
                guid8,
                guid9,
                guid10,
                guid11,
                guid12
            );

            var nullAccessManagerEventValidator = new NullAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>();
            var testAccessManagerTemporalEventPersisterBuffer2 = new AccessManagerTemporalEventPersisterBufferForFlushTesting<String, String, ApplicationScreen, AccessLevel>(nullAccessManagerEventValidator, mockBufferFlushStrategy, mockEventPersister, mockGuidProvider, mockDateTimeProvider);
            mockBufferFlushStrategy.ClearReceivedCalls();
            var beforeMoveEventsToTemporaryQueuesAction = new Action(() =>
            {
                testAccessManagerTemporalEventPersisterBuffer2.AddEntityType("Accounts");
                testAccessManagerTemporalEventPersisterBuffer2.RemoveEntityType("Accounts");
                testAccessManagerTemporalEventPersisterBuffer2.AddEntity("Clients", "CompanyA");
                testAccessManagerTemporalEventPersisterBuffer2.AddEntity("Clients", "CompanyB");
                testAccessManagerTemporalEventPersisterBuffer2.AddEntity("Clients", "CompanyC");
                testAccessManagerTemporalEventPersisterBuffer2.RemoveEntity("Clients", "CompanyA");
            });
            testAccessManagerTemporalEventPersisterBuffer2.BeforeMoveEventsToTemporaryQueueAction = beforeMoveEventsToTemporaryQueuesAction;
            testAccessManagerTemporalEventPersisterBuffer2.AddUser("user1");
            testAccessManagerTemporalEventPersisterBuffer2.AddGroup("group1");
            testAccessManagerTemporalEventPersisterBuffer2.AddGroup("group2");
            testAccessManagerTemporalEventPersisterBuffer2.AddGroup("group3");
            testAccessManagerTemporalEventPersisterBuffer2.AddGroup("group4");
            testAccessManagerTemporalEventPersisterBuffer2.AddEntityType("Clients");
            testAccessManagerTemporalEventPersisterBuffer2.AddEntityType("Products");

            testAccessManagerTemporalEventPersisterBuffer2.Flush();

            Received.InOrder(() => {
                // These get called as the initial events are buffered
                mockBufferFlushStrategy.UserEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 2;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 3;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 4;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 1;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 2;
                // These are called at the start of the Flush() method as part of action 'beforeMoveEventsToTemporaryQueuesAction'
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 3;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 4;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 1;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 2;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 3;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 4;
                // These are called as the Flush() method starts.  Note that entity and entity type events generated as part of action 'beforeMoveEventsToTemporaryQueuesAction' occur after Flush() starts and hence are returned to the relevant event queues (counts of 2 and 4 for entity type and entity events respectively).
                mockBufferFlushStrategy.UserEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 2;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 4;
                mockBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 0;
                // These are called as the buffered items are processed
                mockEventPersister.AddUser("user1", guid0, CreateDataTimeFromString("2021-06-14 20:09:00"));
                mockEventPersister.AddGroup("group1", guid1, CreateDataTimeFromString("2021-06-14 20:09:01"));
                mockEventPersister.AddGroup("group2", guid2, CreateDataTimeFromString("2021-06-14 20:09:02"));
                mockEventPersister.AddGroup("group3", guid3, CreateDataTimeFromString("2021-06-14 20:09:03"));
                mockEventPersister.AddGroup("group4", guid4, CreateDataTimeFromString("2021-06-14 20:09:04"));
                mockEventPersister.AddEntityType("Clients", guid5, CreateDataTimeFromString("2021-06-14 20:09:05"));
                mockEventPersister.AddEntityType("Products", guid6, CreateDataTimeFromString("2021-06-14 20:09:06"));
            });
            Assert.AreEqual(23, mockBufferFlushStrategy.ReceivedCalls().Count());
            Assert.AreEqual(7, mockEventPersister.ReceivedCalls().Count());


            // Test that a subsequent call to Flush() processes the remaining buffered events correctly
            mockBufferFlushStrategy.ClearReceivedCalls();
            mockEventPersister.ClearReceivedCalls();

            testAccessManagerTemporalEventPersisterBuffer2.Flush();

            Received.InOrder(() =>
            {
                // These are called as the buffers are cleared
                mockBufferFlushStrategy.UserEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 0;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 0;
                mockBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 0;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 0;
                // These are called as the buffered items are processed
                mockEventPersister.AddEntityType("Accounts", guid7, CreateDataTimeFromString("2021-06-14 20:09:07"));
                mockEventPersister.RemoveEntityType("Accounts", guid8, CreateDataTimeFromString("2021-06-14 20:09:08"));
                mockEventPersister.AddEntity("Clients", "CompanyA", guid9, CreateDataTimeFromString("2021-06-14 20:09:09"));
                mockEventPersister.AddEntity("Clients", "CompanyB", guid10, CreateDataTimeFromString("2021-06-14 20:09:10"));
                mockEventPersister.AddEntity("Clients", "CompanyC", guid11, CreateDataTimeFromString("2021-06-14 20:09:11"));
                mockEventPersister.RemoveEntity("Clients", "CompanyA", guid12, CreateDataTimeFromString("2021-06-14 20:09:12"));
            });
            Assert.AreEqual(10, mockBufferFlushStrategy.ReceivedCalls().Count());
            Assert.AreEqual(6, mockEventPersister.ReceivedCalls().Count());
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
        /// Version of the AccessManagerTemporalEventPersisterBuffer class allowing additional 'hooks' into protected methods, to facilitate unit testing of the Flush() method.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class AccessManagerTemporalEventPersisterBufferForFlushTesting<TUser, TGroup, TComponent, TAccess> : AccessManagerTemporalEventPersisterBuffer<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>An action to perform during the MoveEventsToTemporaryQueues() method, after capturing variable 'maxSequenceNumber', but before moving events to temporary queues.</summary>
            protected Action beforeMoveEventsToTemporaryQueueAction;
            /// <summary> Whether the action in 'beforeMoveEventsToTemporaryQueuesAction' has been invoked.</summary>
            protected Boolean beforeMoveEventsToTemporaryQueueActionInvoked;

            /// <summary>
            /// An action to perform during the MoveEventsToTemporaryQueues() method, after capturing variable 'maxSequenceNumber', but before moving events to temporary queues.
            /// </summary>
            public Action BeforeMoveEventsToTemporaryQueueAction
            {
                set
                {
                    beforeMoveEventsToTemporaryQueueAction = value;
                }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Persistence.UnitTests.AccessManagerTemporalEventPersisterBuffer+AccessManagerTemporalEventPersisterBufferForFlushTesting class.
            /// </summary>
            /// <param name="eventValidator">The validator to use to validate events.</param>
            /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
            /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
            /// <param name="guidProvider">The provider to use for random Guids.</param>
            /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
            public AccessManagerTemporalEventPersisterBufferForFlushTesting
            (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
                IAccessManagerEventBufferFlushStrategy bufferFlushStrategy, 
                IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
                IGuidProvider guidProvider,
                IDateTimeProvider dateTimeProvider
            ) : base(eventValidator, bufferFlushStrategy, eventPersister, guidProvider, dateTimeProvider)
            {
                this.beforeMoveEventsToTemporaryQueueAction = new Action(() => { });
                beforeMoveEventsToTemporaryQueueActionInvoked = false;
            }

            /// <summary>
            /// Moves all events with sequence number below or equal to that specified, from an event buffer to a temporary event buffer.
            /// </summary>
            /// <typeparam name="TEventBuffer">The type of the event buffer.</typeparam>
            /// <typeparam name="TEventBufferItemType">The type of items in the event buffer.</typeparam>
            /// <param name="eventBuffer">The event buffer to move events from.</param>
            /// <param name="temporaryEventBuffer">The temporary event buffer to move events to.</param>
            /// <param name="eventBufferLockObject">Lock object used to serialize access to the event buffer parameter.</param>
            /// <param name="maxSequenceNumber">The maximum (inclusive) sequence number of events to move.  Only events with a sequence number below or equal to this maximum will be moved.</param>
            /// <param name="bufferFlushStrategyEventCountSetAction">An action which sets the relevant 'EventBufferItemCount' property on the 'bufferFlushStrategy' member.</param>
            protected override void MoveEventsToTemporaryQueue<TEventBuffer, TEventBufferItemType>
            (
                ref TEventBuffer eventBuffer, 
                out TEventBuffer temporaryEventBuffer, 
                Object eventBufferLockObject, 
                Int64 maxSequenceNumber,
                Action<Int32> bufferFlushStrategyEventCountSetAction
            )
            {
                if (beforeMoveEventsToTemporaryQueueActionInvoked == false && eventBuffer is LinkedList<Tuple<UserEventBufferItem<TUser>, Int64>>)
                {
                    beforeMoveEventsToTemporaryQueueAction.Invoke();
                    beforeMoveEventsToTemporaryQueueActionInvoked = true;
                }
                base.MoveEventsToTemporaryQueue<TEventBuffer, TEventBufferItemType>(ref eventBuffer, out temporaryEventBuffer, eventBufferLockObject, maxSequenceNumber, bufferFlushStrategyEventCountSetAction);
            }
        }

        /// <summary>
        /// Version of the AccessManagerTemporalEventPersisterBuffer class where private and protected methods are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class AccessManagerTemporalEventPersisterBufferWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : AccessManagerTemporalEventPersisterBuffer<TUser, TGroup, TComponent, TAccess>
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

            /// <summary>
            ///  Initialises a new instance of the ApplicationAccess.Persistence.UnitTests.AccessManagerTemporalEventPersisterBuffer+AccessManagerTemporalEventPersisterBufferWithProtectedMembers class.
            /// </summary>
            /// <param name="eventValidator">The validator to use to validate events.</param>
            /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
            /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
            /// <param name="guidProvider">The provider to use for random Guids.</param>
            /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
            public AccessManagerTemporalEventPersisterBufferWithProtectedMembers
            (
                IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
                IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
                IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
                IGuidProvider guidProvider,
                IDateTimeProvider dateTimeProvider
            )
                : base(eventValidator, bufferFlushStrategy, eventPersister, guidProvider, dateTimeProvider)
            {
            }
        }

        /// <summary>
        /// An implementation of IAccessManagerEventValidator which allows interception of method calls via a call to IMethodCallInterceptor.Intercept(), and subsequently calls the equivalent method in an instance of NullAccessManagerEventValidator.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the AccessManager implementation.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the AccessManager implementation.</typeparam>
        /// <typeparam name="TComponent">The type of components in the AccessManager implementation.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        private class MethodInterceptingAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>A mock of IMethodCallInterceptor (for intercepting method calls).</summary>
            protected IMethodCallInterceptor interceptor;
            /// <summary>An instance of NullAccessManagerEventValidator to perform the IAccessManagerEventValidator functionality.</summary>
            protected NullAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> nullAccessManagerEventValidator;

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Persistence.UnitTests.AccessManagerTemporalEventPersisterBuffer+MethodInterceptingAccessManagerEventValidator class.
            /// </summary>
            /// <param name="interceptor">A mock of IMethodCallInterceptor (for intercepting method calls).</param>
            /// <param name="nullAccessManagerEventValidator">An instance of NullAccessManagerEventValidator to perform the IAccessManagerEventValidator functionality.</param>
            public MethodInterceptingAccessManagerEventValidator(IMethodCallInterceptor interceptor, NullAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> nullAccessManagerEventValidator)
            {
                this.interceptor = interceptor;
                this.nullAccessManagerEventValidator = nullAccessManagerEventValidator;
            }

            public ValidationResult ValidateAddUser(TUser user, Action<TUser> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateAddUser(user, postValidationAction);
            }

            public ValidationResult ValidateRemoveUser(TUser user, Action<TUser> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateRemoveUser(user, postValidationAction);
            }

            public ValidationResult ValidateAddGroup(TGroup group, Action<TGroup> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateAddGroup(group, postValidationAction);
            }

            public ValidationResult ValidateRemoveGroup(TGroup group, Action<TGroup> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateRemoveGroup(group, postValidationAction);
            }

            public ValidationResult ValidateAddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateAddUserToGroupMapping(user, group, postValidationAction);
            }

            public ValidationResult ValidateRemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateRemoveUserToGroupMapping(user, group, postValidationAction);
            }

            public ValidationResult ValidateAddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateAddGroupToGroupMapping(fromGroup, toGroup, postValidationAction);
            }

            public ValidationResult ValidateRemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateRemoveGroupToGroupMapping(fromGroup, toGroup, postValidationAction);
            }

            public ValidationResult ValidateAddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateAddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, postValidationAction);
            }

            public ValidationResult ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateRemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, postValidationAction);
            }

            public ValidationResult ValidateAddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateAddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, postValidationAction);
            }

            public ValidationResult ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateRemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, postValidationAction);
            }

            public ValidationResult ValidateAddEntityType(string entityType, Action<string> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateAddEntityType(entityType, postValidationAction);
            }

            public ValidationResult ValidateRemoveEntityType(string entityType, Action<string> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateRemoveEntityType(entityType, postValidationAction);
            }

            public ValidationResult ValidateAddEntity(string entityType, string entity, Action<string, string> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateAddEntity(entityType, entity, postValidationAction);
            }

            public ValidationResult ValidateRemoveEntity(string entityType, string entity, Action<string, string> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateRemoveEntity(entityType, entity, postValidationAction);
            }

            public ValidationResult ValidateAddUserToEntityMapping(TUser user, string entityType, string entity, Action<TUser, string, string> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateAddUserToEntityMapping(user, entityType, entity, postValidationAction);
            }

            public ValidationResult ValidateRemoveUserToEntityMapping(TUser user, string entityType, string entity, Action<TUser, string, string> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateRemoveUserToEntityMapping(user, entityType, entity, postValidationAction);
            }

            public ValidationResult ValidateAddGroupToEntityMapping(TGroup group, string entityType, string entity, Action<TGroup, string, string> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateAddGroupToEntityMapping(group, entityType, entity, postValidationAction);
            }

            public ValidationResult ValidateRemoveGroupToEntityMapping(TGroup group, string entityType, string entity, Action<TGroup, string, string> postValidationAction)
            {
                interceptor.Intercept();
                return nullAccessManagerEventValidator.ValidateRemoveGroupToEntityMapping(group, entityType, entity, postValidationAction);
            }
        }
    }

    #endregion
}

