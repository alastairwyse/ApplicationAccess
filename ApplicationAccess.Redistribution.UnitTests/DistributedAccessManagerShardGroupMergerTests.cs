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
using ApplicationAccess.Distribution.Persistence;
using ApplicationAccess.Distribution;
using ApplicationAccess.Persistence;
using ApplicationAccess.Utilities;
using ApplicationLogging;
using ApplicationMetrics;
using NUnit.Framework;
using NSubstitute;
using ApplicationAccess.Persistence.Models;
using NUnit.Framework.Internal.Execution;
using System.Diagnostics.Tracing;

namespace ApplicationAccess.Redistribution.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Redistribution.DistributedAccessManagerShardGroupMerger class.
    /// </summary>
    public class DistributedAccessManagerShardGroupMergerTests
    {
        private IAccessManagerTemporalEventBatchReader mockSourceShardGroup1EventReader;
        private IAccessManagerTemporalEventBatchReader mockSourceShardGroup2EventReader;
        private IAccessManagerIdempotentTemporalEventBulkPersister<String, String, String, String> mockTargetShardGroupEventPersister;
        private IApplicationLogger mockApplicationLogger;
        private IMetricLogger mockMetricLogger;
        private DistributedAccessManagerShardGroupMergerMergerWithProtectedMembers testShardGroupMerger;


        [SetUp]
        protected void SetUp()
        {
            mockSourceShardGroup1EventReader = Substitute.For<IAccessManagerTemporalEventBatchReader>();
            mockSourceShardGroup2EventReader = Substitute.For<IAccessManagerTemporalEventBatchReader>();
            mockTargetShardGroupEventPersister = Substitute.For<IAccessManagerIdempotentTemporalEventBulkPersister<String, String, String, String>>();
            mockApplicationLogger = Substitute.For<IApplicationLogger>();
            mockMetricLogger = Substitute.For<IMetricLogger>();
            testShardGroupMerger = new DistributedAccessManagerShardGroupMergerMergerWithProtectedMembers(mockApplicationLogger, mockMetricLogger);
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup_DestinationEventQueueParameterNotEmpty()
        {
            var initialEventId = Guid.NewGuid();
            var destinationEventQueue = new Queue<TemporalEventBufferItemBase>();
            destinationEventQueue.Enqueue
            (
                new UserEventBufferItem<String>(Guid.NewGuid(), EventAction.Add, "testUser", DateTime.UtcNow, 0)
            );

            var e = Assert.Throws<ArgumentException>(delegate
            {
                testShardGroupMerger.ReadSourceShardGroupEventsIntoQueue
                (
                    initialEventId,
                    mockSourceShardGroup1EventReader,
                    destinationEventQueue,
                    100,
                    true
                );
            });

            Assert.That(e.Message, Does.StartWith($"The queue in parameter 'destinationEventQueue' is not empty."));
            Assert.AreEqual("destinationEventQueue", e.ParamName);
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup_ExceptionReadingEvents()
        {
            var mockException = new Exception("Mock exception");
            var initialEventId = Guid.NewGuid();
            var destinationEventQueue = new Queue<TemporalEventBufferItemBase>();
            Int32 eventCount = 100;
            mockSourceShardGroup1EventReader.When(reader => reader.GetEvents(initialEventId, Int32.MinValue, Int32.MaxValue, false, eventCount)).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testShardGroupMerger.ReadSourceShardGroupEventsIntoQueue
                (
                    initialEventId,
                    mockSourceShardGroup1EventReader,
                    destinationEventQueue,
                    eventCount,
                    true
                );
            });

            mockSourceShardGroup1EventReader.Received(1).GetEvents(initialEventId, Int32.MinValue, Int32.MaxValue, false, eventCount);
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve event batch from first source shard group beginning with event with id '{initialEventId}'."));
            Assert.AreSame(mockException, e.InnerException);


            mockSourceShardGroup1EventReader.ClearReceivedCalls();

            e = Assert.Throws<Exception>(delegate
            {
                testShardGroupMerger.ReadSourceShardGroupEventsIntoQueue
                (
                    initialEventId,
                    mockSourceShardGroup1EventReader,
                    destinationEventQueue,
                    eventCount,
                    false
                );
            });

            mockSourceShardGroup1EventReader.Received(1).GetEvents(initialEventId, Int32.MinValue, Int32.MaxValue, false, eventCount);
            Assert.That(e.Message, Does.StartWith($"Failed to retrieve event batch from second source shard group beginning with event with id '{initialEventId}'."));
            Assert.AreSame(mockException, e.InnerException);
        }

        [Test]
        public void MergeEventBatchesToTargetShardGroup()
        {
            throw new NotImplementedException();
        }

        #region Nested Classes

        private class DistributedAccessManagerShardGroupMergerMergerWithProtectedMembers : DistributedAccessManagerShardGroupMerger
        {
            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Redistribution.UnitTests.DistributedAccessManagerShardGroupMergerTests+DistributedAccessManagerShardGroupMergerMergerWithProtectedMembers class.
            /// </summary>
            /// <param name="logger">The logger for general logging.</param>
            /// <param name="metricLogger">The logger for metrics.</param>
            public DistributedAccessManagerShardGroupMergerMergerWithProtectedMembers(IApplicationLogger logger, IMetricLogger metricLogger)
                : base(logger, metricLogger)
            {
            }


            public new Tuple<Nullable<Guid>, Nullable<Guid>> MergeEventBatchesToTargetShardGroup<TUser, TGroup, TComponent, TAccess>
            (
                IAccessManagerTemporalEventBatchReader sourceShardGroup1EventReader,
                IAccessManagerTemporalEventBatchReader sourceShardGroup2EventReader,
                IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> targetShardGroupEventPersister,
                ref Int32 currentBatchNumber,
                Guid sourceShardGroup1FirstEventId,
                Guid sourceShardGroup2FirstEventId,
                Int32 eventBatchSize
            )
            {
                return base.MergeEventBatchesToTargetShardGroup
                (
                    sourceShardGroup1EventReader,
                    sourceShardGroup2EventReader,
                    targetShardGroupEventPersister,
                    ref currentBatchNumber,
                    sourceShardGroup1FirstEventId,
                    sourceShardGroup2FirstEventId,
                    eventBatchSize
                );
            }

            public new Nullable<Guid> ReadSourceShardGroupEventsIntoQueue
            (
                Guid initialEventId,
                IAccessManagerTemporalEventBatchReader sourceShardGroupEventReader,
                Queue<TemporalEventBufferItemBase> destinationEventQueue,
                Int32 eventCount,
                Boolean readerIsFirstEventReader
            )
            {
                return base.ReadSourceShardGroupEventsIntoQueue
                (
                    initialEventId,
                    sourceShardGroupEventReader,
                    destinationEventQueue,
                    eventCount,
                    readerIsFirstEventReader
                );
            }
        }

        #endregion
    }
}
