/*
 * Copyright 2022 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using ApplicationAccess.Validation;
using ApplicationMetrics;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.AccessManagerTemporalEventPersisterBufferBase class.
    /// </summary>
    /// <remarks>Since <see cref="AccessManagerTemporalEventPersisterBufferBase{TUser, TGroup, TComponent, TAccess}"/> is abstract, tests are performed via the <see cref="AccessManagerTemporalEventPersisterBuffer{TUser, TGroup, TComponent, TAccess}"/> class.</remarks>
    public class AccessManagerTemporalEventPersisterBufferBaseTests
    {
        protected IMethodCallInterceptor dateTimeProviderMethodCallInterceptor;
        protected IMethodCallInterceptor eventValidatorMethodCallInterceptor;
        protected IAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel> mockEventValidator;
        protected IAccessManagerEventBufferFlushStrategy mockBufferFlushStrategy;
        protected IHashCodeGenerator<String> mockUserHashCodeGenerator;
        protected IHashCodeGenerator<String> mockGroupHashCodeGenerator;
        protected IHashCodeGenerator<String> mockEntityTypeHashCodeGenerator;
        protected IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel> mockEventPersister;
        protected IMetricLogger mockMetricLogger;
        protected IGuidProvider mockGuidProvider;
        protected IDateTimeProvider mockDateTimeProvider;
        protected AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testAccessManagerTemporalEventPersisterBuffer;

        [SetUp]
        protected void SetUp()
        {
            mockBufferFlushStrategy = Substitute.For<IAccessManagerEventBufferFlushStrategy>();
            mockUserHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockGroupHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            mockEntityTypeHashCodeGenerator = Substitute.For<IHashCodeGenerator<String>>();
            dateTimeProviderMethodCallInterceptor = Substitute.For<IMethodCallInterceptor>();
            eventValidatorMethodCallInterceptor = Substitute.For<IMethodCallInterceptor>();
            mockEventValidator = Substitute.For<IAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>>();
            mockEventPersister = Substitute.For<IAccessManagerTemporalEventPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockGuidProvider = Substitute.For<IGuidProvider>();
            mockDateTimeProvider = Substitute.For<IDateTimeProvider>();
            var methodInterceptingDateTimeProvider = new MethodInterceptingDateTimeProvider(dateTimeProviderMethodCallInterceptor, mockDateTimeProvider);
            var methodInterceptingValidator = new MethodInterceptingAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>(eventValidatorMethodCallInterceptor, new NullAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>());
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                methodInterceptingValidator, 
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator, 
                mockEventPersister, 
                mockMetricLogger, 
                mockGuidProvider,
                methodInterceptingDateTimeProvider
            );
        }

        [Test]
        public void AddUser()
        {
            Guid eventId = Guid.NewGuid();
            Int32 hashCode = -20;
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:55");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddUser(user);

            mockBufferFlushStrategy.Received(1).UserEventBufferItemCount = 1;
            mockUserHashCodeGenerator.Received(1).GetHashCode(user);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserEventBuffer.Count);
            UserEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddUser_ValidationFails()
        {
            const String user = "user1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator, 
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister, 
                mockMetricLogger, 
                mockGuidProvider, 
                mockDateTimeProvider
            );
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
            Int32 hashCode = -19;
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:56");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveUser(user);

            mockBufferFlushStrategy.Received(1).UserEventBufferItemCount = 1;
            mockUserHashCodeGenerator.Received(1).GetHashCode(user);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserEventBuffer.Count);
            UserEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveUser_ValidationFails()
        {
            const String user = "user1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -18;
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:57");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGroupHashCodeGenerator.GetHashCode(group).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddGroup(group);

            mockBufferFlushStrategy.Received(1).GroupEventBufferItemCount = 1;
            mockGroupHashCodeGenerator.Received(1).GetHashCode(group);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupEventBuffer.Count);
            GroupEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddGroup_ValidationFails()
        {
            const String group = "group1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -17;
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:58");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGroupHashCodeGenerator.GetHashCode(group).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroup(group);

            mockBufferFlushStrategy.Received(1).GroupEventBufferItemCount = 1;
            mockGroupHashCodeGenerator.Received(1).GetHashCode(group);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupEventBuffer.Count);
            GroupEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveGroup_ValidationFails()
        {
            const String group = "group1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -16;
            const String user = "user1";
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:16:59");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddUserToGroupMapping(user, group);

            mockBufferFlushStrategy.Received(1).UserToGroupMappingEventBufferItemCount = 1;
            mockUserHashCodeGenerator.Received(1).GetHashCode(user);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBuffer.Count);
            UserToGroupMappingEventBufferItem<String, String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddUserToGroupMapping_ValidationFails()
        {
            const String user = "user1";
            const String group = "group1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -15;
            const String user = "user1";
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:00");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToGroupMapping(user, group);

            mockBufferFlushStrategy.Received(1).UserToGroupMappingEventBufferItemCount = 1;
            mockUserHashCodeGenerator.Received(1).GetHashCode(user);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBuffer.Count);
            UserToGroupMappingEventBufferItem<String, String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserToGroupMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveUserToGroupMapping_ValidationFails()
        {
            const String user = "user1";
            const String group = "group1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -14;
            const String fromGroup = "group1";
            const String toGroup = "group2";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:01");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGroupHashCodeGenerator.GetHashCode(fromGroup).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddGroupToGroupMapping(fromGroup, toGroup);

            mockBufferFlushStrategy.Received(1).GroupToGroupMappingEventBufferItemCount = 1;
            mockGroupHashCodeGenerator.Received(1).GetHashCode(fromGroup);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBuffer.Count);
            GroupToGroupMappingEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(fromGroup, bufferedEvent.FromGroup);
            Assert.AreEqual(toGroup, bufferedEvent.ToGroup);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddGroupToGroupMapping_ValidationFails()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -13;
            const String fromGroup = "group1";
            const String toGroup = "group2";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:02");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGroupHashCodeGenerator.GetHashCode(fromGroup).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToGroupMapping(fromGroup, toGroup);

            mockBufferFlushStrategy.Received(1).GroupToGroupMappingEventBufferItemCount = 1;
            mockGroupHashCodeGenerator.Received(1).GetHashCode(fromGroup);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBuffer.Count);
            GroupToGroupMappingEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(fromGroup, bufferedEvent.FromGroup);
            Assert.AreEqual(toGroup, bufferedEvent.ToGroup);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupToGroupMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveGroupToGroupMapping_ValidationFails()
        {
            const String fromGroup = "group1";
            const String toGroup = "group2";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -13;
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:03");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Summary, AccessLevel.View);

            mockBufferFlushStrategy.Received(1).UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
            mockUserHashCodeGenerator.Received(1).GetHashCode(user);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(ApplicationScreen.Summary, bufferedEvent.ApplicationComponent);
            Assert.AreEqual(AccessLevel.View, bufferedEvent.AccessLevel);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_ValidationFails()
        {
            const String user = "user1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -12;
            const String user = "user1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:04");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToApplicationComponentAndAccessLevelMapping(user, ApplicationScreen.Summary, AccessLevel.View);

            mockBufferFlushStrategy.Received(1).UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
            mockUserHashCodeGenerator.Received(1).GetHashCode(user);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            UserToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(ApplicationScreen.Summary, bufferedEvent.ApplicationComponent);
            Assert.AreEqual(AccessLevel.View, bufferedEvent.AccessLevel);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_ValidationFails()
        {
            const String user = "user1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -11;
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:05");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGroupHashCodeGenerator.GetHashCode(group).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Create);

            mockBufferFlushStrategy.Received(1).GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
            mockGroupHashCodeGenerator.Received(1).GetHashCode(group);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(ApplicationScreen.Order, bufferedEvent.ApplicationComponent);
            Assert.AreEqual(AccessLevel.Create, bufferedEvent.AccessLevel);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_ValidationFails()
        {
            const String group = "group1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -10;
            const String group = "group1";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:06");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGroupHashCodeGenerator.GetHashCode(group).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, ApplicationScreen.Order, AccessLevel.Create);

            mockBufferFlushStrategy.Received(1).GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
            mockGroupHashCodeGenerator.Received(1).GetHashCode(group);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.Count);
            GroupToApplicationComponentAndAccessLevelMappingEventBufferItem<String, ApplicationScreen, AccessLevel> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(ApplicationScreen.Order, bufferedEvent.ApplicationComponent);
            Assert.AreEqual(AccessLevel.Create, bufferedEvent.AccessLevel);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupToApplicationComponentAndAccessLevelMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_ValidationFails()
        {
            const String group = "group1";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -9;
            const String entityType = "ClientAccount";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:07");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockEntityTypeHashCodeGenerator.GetHashCode(entityType).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddEntityType(entityType);

            mockBufferFlushStrategy.Received(1).EntityTypeEventBufferItemCount = 1;
            mockEntityTypeHashCodeGenerator.Received(1).GetHashCode(entityType);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBuffer.Count);
            EntityTypeEventBufferItem bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddEntityType_ValidationFails()
        {
            const String entityType = "ClientAccount";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -8;
            const String entityType = "ClientAccount";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:08");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockEntityTypeHashCodeGenerator.GetHashCode(entityType).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveEntityType(entityType);

            mockBufferFlushStrategy.Received(1).EntityTypeEventBufferItemCount = 1;
            mockEntityTypeHashCodeGenerator.Received(1).GetHashCode(entityType);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBuffer.Count);
            EntityTypeEventBufferItem bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.EntityTypeEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveEntityType_ValidationFails()
        {
            const String entityType = "ClientAccount";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -7;
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:09");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockEntityTypeHashCodeGenerator.GetHashCode(entityType).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EntityEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddEntity(entityType, entity);

            mockBufferFlushStrategy.Received(1).EntityEventBufferItemCount = 1;
            mockEntityTypeHashCodeGenerator.Received(1).GetHashCode(entityType);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.EntityEventBuffer.Count);
            EntityEventBufferItem bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.EntityEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.EntityEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddEntity_ValidationFails()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -6;
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:10");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockEntityTypeHashCodeGenerator.GetHashCode(entityType).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EntityEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveEntity(entityType, entity);

            mockBufferFlushStrategy.Received(1).EntityEventBufferItemCount = 1;
            mockEntityTypeHashCodeGenerator.Received(1).GetHashCode(entityType);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.EntityEventBuffer.Count);
            EntityEventBufferItem bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.EntityEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.EntityEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveEntity_ValidationFails()
        {
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -5;
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:11");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddUserToEntityMapping(user, entityType, entity);

            mockBufferFlushStrategy.Received(1).UserToEntityMappingEventBufferItemCount = 1;
            mockUserHashCodeGenerator.Received(1).GetHashCode(user);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBuffer.Count);
            UserToEntityMappingEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddUserToEntityMapping_ValidationFails()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -4;
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:12");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockUserHashCodeGenerator.GetHashCode(user).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveUserToEntityMapping(user, entityType, entity);

            mockBufferFlushStrategy.Received(1).UserToEntityMappingEventBufferItemCount = 1;
            mockUserHashCodeGenerator.Received(1).GetHashCode(user);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBuffer.Count);
            UserToEntityMappingEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(user, bufferedEvent.User);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.UserToEntityMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveUserToEntityMapping_ValidationFails()
        {
            const String user = "user1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -3;
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:13");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGroupHashCodeGenerator.GetHashCode(group).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.AddGroupToEntityMapping(group, entityType, entity);

            mockBufferFlushStrategy.Received(1).GroupToEntityMappingEventBufferItemCount = 1;
            mockGroupHashCodeGenerator.Received(1).GetHashCode(group);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBuffer.Count);
            GroupToEntityMappingEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Add, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void AddGroupToEntityMapping_ValidationFails()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
            Int32 hashCode = -2;
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:14");
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockGroupHashCodeGenerator.GetHashCode(group).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });

            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testAccessManagerTemporalEventPersisterBuffer.RemoveGroupToEntityMapping(group, entityType, entity);

            mockBufferFlushStrategy.Received(1).GroupToEntityMappingEventBufferItemCount = 1;
            mockGroupHashCodeGenerator.Received(1).GetHashCode(group);
            Assert.AreEqual(1, testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBuffer.Count);
            GroupToEntityMappingEventBufferItem<String> bufferedEvent = testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(group, bufferedEvent.Group);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testAccessManagerTemporalEventPersisterBuffer.GroupToEntityMappingEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveGroupToEntityMapping_ValidationFails()
        {
            const String group = "group1";
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            const String mockExceptionMessage = "Mock Exception.";
            testAccessManagerTemporalEventPersisterBuffer = new AccessManagerTemporalEventPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
            (
                mockEventValidator,
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister,
                mockMetricLogger,
                mockGuidProvider,
                mockDateTimeProvider
            );
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
        public virtual void Flush()
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
            });
            Assert.AreEqual(38, mockBufferFlushStrategy.ReceivedCalls().Count());
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

            Int32 user1HashCode = 1;
            Int32 user2HashCode = 2;
            Int32 user3HashCode = 3;
            Int32 group1HashCode = 11;
            Int32 group2HashCode = 12;
            Int32 group3HashCode = 13;
            Int32 group4HashCode = 14;
            Int32 clientsHashCode = 21;
            mockUserHashCodeGenerator.GetHashCode(user1).Returns(user1HashCode);
            mockUserHashCodeGenerator.GetHashCode(user2).Returns(user2HashCode);
            mockUserHashCodeGenerator.GetHashCode(user3).Returns(user3HashCode);
            mockGroupHashCodeGenerator.GetHashCode(group1).Returns(group1HashCode);
            mockGroupHashCodeGenerator.GetHashCode(group2).Returns(group2HashCode);
            mockGroupHashCodeGenerator.GetHashCode(group3).Returns(group3HashCode);
            mockGroupHashCodeGenerator.GetHashCode(group4).Returns(group4HashCode);
            mockEntityTypeHashCodeGenerator.GetHashCode("Clients").Returns(clientsHashCode);

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

            mockEventPersister.When((eventPersister) => eventPersister.AddUser(user3, guid2, CreateDataTimeFromString("2021-06-13 09:58:02"), user3HashCode)).Do((callInfo) =>
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
                mockEventPersister.AddUser(user1, guid0, CreateDataTimeFromString("2021-06-13 09:58:00"), user1HashCode);
                mockEventPersister.AddUser(user2, guid1, CreateDataTimeFromString("2021-06-13 09:58:01"), user2HashCode);
                mockEventPersister.AddUser(user3, guid2, CreateDataTimeFromString("2021-06-13 09:58:02"), user3HashCode);
                // When AddUser(user3) is called, we simulate calling the public methods again, and hence expect further calls to the flush strategy...
                mockBufferFlushStrategy.GroupEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupEventBufferItemCount = 2;
                mockBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.EntityTypeEventBufferItemCount = 1;
                mockBufferFlushStrategy.EntityEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 1;
                mockBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 2;
                // ...and once these are complete, the processing of the call to Flush() continues
                mockEventPersister.AddGroup(group1, guid3, CreateDataTimeFromString("2021-06-13 09:58:03"), group1HashCode);
                mockEventPersister.AddGroup(group2, guid4, CreateDataTimeFromString("2021-06-13 09:58:04"), group2HashCode);
                mockEventPersister.AddUserToGroupMapping(user1, group1, guid5, CreateDataTimeFromString("2021-06-13 09:58:05"), user1HashCode);
                mockEventPersister.AddUserToGroupMapping(user2, group1, guid6, CreateDataTimeFromString("2021-06-13 09:58:06"), user2HashCode);
            });
            Assert.AreEqual(24, mockBufferFlushStrategy.ReceivedCalls().Count());
            Assert.AreEqual(7, mockEventPersister.ReceivedCalls().Count());
            // These method calls would occur as part of a subsequent Flush() call, and hence should not be received
            mockEventPersister.DidNotReceive().AddGroup(group3, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<Int32>());
            mockEventPersister.DidNotReceive().AddGroup(group4, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<Int32>());
            mockEventPersister.DidNotReceive().AddGroupToGroupMapping(group1, group2, Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<Int32>());
            mockEventPersister.DidNotReceive().AddEntityType("Clients", Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<Int32>());
            mockEventPersister.DidNotReceive().AddEntity("Clients", "CompanyA", Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<Int32>());
            mockEventPersister.DidNotReceive().AddGroupToEntityMapping(group3, "Clients", "CompanyA", Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<Int32>());
            mockEventPersister.DidNotReceive().AddGroupToEntityMapping(group4, "Clients", "CompanyA", Arg.Any<Guid>(), Arg.Any<DateTime>(), Arg.Any<Int32>());


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
                mockEventPersister.AddGroup(group3, guid7, CreateDataTimeFromString("2021-06-13 09:58:07"), group3HashCode);
                mockEventPersister.AddGroup(group4, guid8, CreateDataTimeFromString("2021-06-13 09:58:08"), group4HashCode);
                mockEventPersister.AddGroupToGroupMapping(group1, group2, guid9, CreateDataTimeFromString("2021-06-13 09:58:09"), group1HashCode);
                mockEventPersister.AddEntityType("Clients", guid10, CreateDataTimeFromString("2021-06-13 09:58:10"), clientsHashCode);
                mockEventPersister.AddEntity("Clients", "CompanyA", guid11, CreateDataTimeFromString("2021-06-13 09:58:11"), 21);
                mockEventPersister.AddGroupToEntityMapping(group3, "Clients", "CompanyA", guid12, CreateDataTimeFromString("2021-06-13 09:58:12"), group3HashCode);
                mockEventPersister.AddGroupToEntityMapping(group4, "Clients", "CompanyA", guid13, CreateDataTimeFromString("2021-06-13 09:58:13"), group4HashCode);
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

            Int32 user1HashCode = 1;
            Int32 group1HashCode = 11;
            Int32 group2HashCode = 12;
            Int32 group3HashCode = 13;
            Int32 group4HashCode = 14;
            Int32 clientsHashCode = 21;
            Int32 productssHashCode = 22;
            Int32 accountssHashCode = 23;
            mockUserHashCodeGenerator.GetHashCode("user1").Returns(user1HashCode);
            mockGroupHashCodeGenerator.GetHashCode("group1").Returns(group1HashCode);
            mockGroupHashCodeGenerator.GetHashCode("group2").Returns(group2HashCode);
            mockGroupHashCodeGenerator.GetHashCode("group3").Returns(group3HashCode);
            mockGroupHashCodeGenerator.GetHashCode("group4").Returns(group4HashCode);
            mockEntityTypeHashCodeGenerator.GetHashCode("Clients").Returns(clientsHashCode);
            mockEntityTypeHashCodeGenerator.GetHashCode("Products").Returns(productssHashCode);
            mockEntityTypeHashCodeGenerator.GetHashCode("Accounts").Returns(accountssHashCode);

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
            var testAccessManagerTemporalEventPersisterBuffer2 = new AccessManagerTemporalEventPersisterBufferForFlushTesting<String, String, ApplicationScreen, AccessLevel>
            (
                nullAccessManagerEventValidator, 
                mockBufferFlushStrategy,
                mockUserHashCodeGenerator,
                mockGroupHashCodeGenerator,
                mockEntityTypeHashCodeGenerator,
                mockEventPersister, 
                mockMetricLogger, 
                mockGuidProvider, 
                mockDateTimeProvider
            );
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
                mockEventPersister.AddUser("user1", guid0, CreateDataTimeFromString("2021-06-14 20:09:00"), user1HashCode);
                mockEventPersister.AddGroup("group1", guid1, CreateDataTimeFromString("2021-06-14 20:09:01"), group1HashCode);
                mockEventPersister.AddGroup("group2", guid2, CreateDataTimeFromString("2021-06-14 20:09:02"), group2HashCode);
                mockEventPersister.AddGroup("group3", guid3, CreateDataTimeFromString("2021-06-14 20:09:03"), group3HashCode);
                mockEventPersister.AddGroup("group4", guid4, CreateDataTimeFromString("2021-06-14 20:09:04"), group4HashCode);
                mockEventPersister.AddEntityType("Clients", guid5, CreateDataTimeFromString("2021-06-14 20:09:05"), clientsHashCode);
                mockEventPersister.AddEntityType("Products", guid6, CreateDataTimeFromString("2021-06-14 20:09:06"), productssHashCode);
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
                mockEventPersister.AddEntityType("Accounts", guid7, CreateDataTimeFromString("2021-06-14 20:09:07"), accountssHashCode);
                mockEventPersister.RemoveEntityType("Accounts", guid8, CreateDataTimeFromString("2021-06-14 20:09:08"), accountssHashCode);
                mockEventPersister.AddEntity("Clients", "CompanyA", guid9, CreateDataTimeFromString("2021-06-14 20:09:09"), clientsHashCode);
                mockEventPersister.AddEntity("Clients", "CompanyB", guid10, CreateDataTimeFromString("2021-06-14 20:09:10"), clientsHashCode);
                mockEventPersister.AddEntity("Clients", "CompanyC", guid11, CreateDataTimeFromString("2021-06-14 20:09:11"), clientsHashCode);
                mockEventPersister.RemoveEntity("Clients", "CompanyA", guid12, CreateDataTimeFromString("2021-06-14 20:09:12"), clientsHashCode);
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
        protected class AccessManagerTemporalEventPersisterBufferForFlushTesting<TUser, TGroup, TComponent, TAccess> : AccessManagerTemporalEventPersisterBuffer<TUser, TGroup, TComponent, TAccess>
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
            /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
            /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
            /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
            /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            /// <param name="guidProvider">The provider to use for random Guids.</param>
            /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
            public AccessManagerTemporalEventPersisterBufferForFlushTesting
            (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
                IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
                IHashCodeGenerator<TUser> userHashCodeGenerator,
                IHashCodeGenerator<TGroup> groupHashCodeGenerator,
                IHashCodeGenerator<String> entityTypeHashCodeGenerator,
                IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
                IMetricLogger metricLogger,
                IGuidProvider guidProvider,
                IDateTimeProvider dateTimeProvider
            ) : base(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator, eventPersister, metricLogger, guidProvider, dateTimeProvider)
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
        protected class AccessManagerTemporalEventPersisterBufferWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : AccessManagerTemporalEventPersisterBuffer<TUser, TGroup, TComponent, TAccess>
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
            ///  Initialises a new instance of the ApplicationAccess.Persistence.UnitTests.AccessManagerTemporalEventPersisterBuffer+AccessManagerTemporalEventPersisterBufferWithProtectedMembers class.
            /// </summary>
            /// <param name="eventValidator">The validator to use to validate events.</param>
            /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
            /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
            /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
            /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
            /// <param name="eventPersister">The persister to use to write flushed events to permanent storage.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            /// <param name="guidProvider">The provider to use for random Guids.</param>
            /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
            public AccessManagerTemporalEventPersisterBufferWithProtectedMembers
            (
                IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
                IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
                IHashCodeGenerator<TUser> userHashCodeGenerator,
                IHashCodeGenerator<TGroup> groupHashCodeGenerator,
                IHashCodeGenerator<String> entityTypeHashCodeGenerator,
                IAccessManagerTemporalEventPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
                IMetricLogger metricLogger,
                IGuidProvider guidProvider,
                IDateTimeProvider dateTimeProvider
            )
                : base(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator, eventPersister, metricLogger, guidProvider, dateTimeProvider)
            {
            }
        }
    }

    #endregion
}

