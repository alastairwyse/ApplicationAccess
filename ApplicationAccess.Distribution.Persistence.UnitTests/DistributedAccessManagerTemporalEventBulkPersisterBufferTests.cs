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
using System.Globalization;
using System.Threading;
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;
using ApplicationAccess.Persistence.UnitTests;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Utilities;
using ApplicationAccess.Validation;
using ApplicationMetrics;
using NSubstitute;
using NUnit.Framework;

namespace ApplicationAccess.Distribution.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Distribution.Persistence.DistributedAccessManagerTemporalEventBulkPersisterBuffer class.
    /// </summary>
    public class DistributedAccessManagerTemporalEventBulkPersisterBufferTests
    {
        protected IMethodCallInterceptor dateTimeProviderMethodCallInterceptor;
        protected IMethodCallInterceptor eventValidatorMethodCallInterceptor;
        protected IAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel> mockEventValidator;
        protected IAccessManagerEventBufferFlushStrategy mockBufferFlushStrategy;
        protected IHashCodeGenerator<String> mockUserHashCodeGenerator;
        protected IHashCodeGenerator<String> mockGroupHashCodeGenerator;
        protected IHashCodeGenerator<String> mockEntityTypeHashCodeGenerator;
        protected IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel> mockEventPersister;
        protected IMetricLogger mockMetricLogger;
        protected IGuidProvider mockGuidProvider;
        protected IDateTimeProvider mockDateTimeProvider;
        protected DistributedAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testDistributedAccessManagerTemporalEventBulkPersisterBuffer;

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
            mockEventPersister = Substitute.For<IAccessManagerTemporalEventBulkPersister<String, String, ApplicationScreen, AccessLevel>>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            mockGuidProvider = Substitute.For<IGuidProvider>();
            mockDateTimeProvider = Substitute.For<IDateTimeProvider>();
            var methodInterceptingDateTimeProvider = new MethodInterceptingDateTimeProvider(dateTimeProviderMethodCallInterceptor, mockDateTimeProvider);
            var methodInterceptingValidator = new MethodInterceptingAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>(eventValidatorMethodCallInterceptor, new NullAccessManagerEventValidator<String, String, ApplicationScreen, AccessLevel>());
            testDistributedAccessManagerTemporalEventBulkPersisterBuffer = new DistributedAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>
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
        public void RemoveEntityType_CorrectLocksAreSet()
        {
            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            DateTime eventOccurredTime = CreateDataTimeFromString("2021-06-09 00:17:08");
            Int32 hashCode = 6;
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockEntityTypeHashCodeGenerator.GetHashCode(entityType).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDistributedAccessManagerTemporalEventBulkPersisterBuffer.UserToEntityMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDistributedAccessManagerTemporalEventBulkPersisterBuffer.GroupToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDistributedAccessManagerTemporalEventBulkPersisterBuffer.RemoveEntityType(entityType);

            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveEntity()
        {
            // Standard test for the RemoveEntity() method where no event buffer locks are previously set

            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2025-07-10 21:48:56");
            Int32 hashCode = -11;
            Boolean dateTimeProviderAssertionsWereChecked = false;
            Boolean validatorAssertionsWereChecked = false;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockEntityTypeHashCodeGenerator.GetHashCode(entityType).Returns<Int32>(hashCode);
            dateTimeProviderMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManagerTemporalEventBulkPersisterBuffer.EventSequenceNumberLock));
                dateTimeProviderAssertionsWereChecked = true;
            });
            eventValidatorMethodCallInterceptor.When(interceptor => interceptor.Intercept()).Do(callInfo =>
            {
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManagerTemporalEventBulkPersisterBuffer.EntityTypeEventBufferLock));
                Assert.IsTrue(Monitor.IsEntered(testDistributedAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDistributedAccessManagerTemporalEventBulkPersisterBuffer.UserToEntityMappingEventBufferLock));
                Assert.IsFalse(Monitor.IsEntered(testDistributedAccessManagerTemporalEventBulkPersisterBuffer.GroupToEntityMappingEventBufferLock));
                validatorAssertionsWereChecked = true;
            });

            testDistributedAccessManagerTemporalEventBulkPersisterBuffer.RemoveEntity(entityType, entity);

            mockBufferFlushStrategy.Received(1).EntityEventBufferItemCount = 1;
            Assert.AreEqual(1, testDistributedAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBuffer.Count);
            EntityEventBufferItem bufferedEvent = testDistributedAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testDistributedAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBuffer.First.Value.Item2);
            Assert.IsTrue(dateTimeProviderAssertionsWereChecked);
            Assert.IsTrue(validatorAssertionsWereChecked);
        }

        [Test]
        public void RemoveEntity_LocksAlreadySet()
        {
            // Test for the RemoveEntity() method where event buffer locks have already been set (i.e. simulating calling the method as a result of a prepended add user event)

            Guid eventId = Guid.NewGuid();
            const String entityType = "ClientAccount";
            const String entity = "CompanyA";
            DateTime eventOccurredTime = CreateDataTimeFromString("2025-07-10 21:48:57");
            Int32 hashCode = -109;
            mockGuidProvider.NewGuid().Returns(eventId);
            mockDateTimeProvider.UtcNow().Returns<DateTime>(eventOccurredTime);
            mockEntityTypeHashCodeGenerator.GetHashCode(entityType).Returns<Int32>(hashCode);

            lock (testDistributedAccessManagerTemporalEventBulkPersisterBuffer.UserEventBufferLock)
            {
                testDistributedAccessManagerTemporalEventBulkPersisterBuffer.RemoveEntity(entityType, entity);
            }

            mockBufferFlushStrategy.Received(1).EntityEventBufferItemCount = 1;
            Assert.AreEqual(1, testDistributedAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBuffer.Count);
            EntityEventBufferItem bufferedEvent = testDistributedAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBuffer.First.Value.Item1;
            Assert.AreEqual(eventId, bufferedEvent.EventId);
            Assert.AreEqual(EventAction.Remove, bufferedEvent.EventAction);
            Assert.AreEqual(entityType, bufferedEvent.EntityType);
            Assert.AreEqual(entity, bufferedEvent.Entity);
            Assert.AreEqual(eventOccurredTime, bufferedEvent.OccurredTime);
            Assert.AreEqual(hashCode, bufferedEvent.HashCode);
            Assert.AreEqual(0, testDistributedAccessManagerTemporalEventBulkPersisterBuffer.EntityEventBuffer.First.Value.Item2);
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
        /// Version of the DistributedAccessManagerTemporalEventBulkPersisterBuffer class where private and protected methods are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        protected class DistributedAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : DistributedAccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess>
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
            /// Initialises a new instance of the ApplicationAccess.Distribution.Persistence.UnitTests.DistributedAccessManagerTemporalEventBulkPersisterBufferTests+DistributedAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers class.
            /// </summary>
            /// <param name="eventValidator">The validator to use to validate events.</param>
            /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
            /// <param name="eventPersister">The bulk persister to use to write flushed events to permanent storage.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            /// <param name="guidProvider">The provider to use for random Guids.</param>
            /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
            public DistributedAccessManagerTemporalEventBulkPersisterBufferWithProtectedMembers
            (
                IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
                IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
                IHashCodeGenerator<TUser> userHashCodeGenerator,
                IHashCodeGenerator<TGroup> groupHashCodeGenerator,
                IHashCodeGenerator<String> entityTypeHashCodeGenerator,
                IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
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

