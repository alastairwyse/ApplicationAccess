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
using System.Threading;
using ApplicationAccess.UnitTests;
using ApplicationMetrics.MetricLoggers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.SizeLimitedBufferFlushStrategy class.
    /// </summary>
    public class SizeLimitedBufferFlushStrategyTests
    {
        // Some of these tests use Thread.Sleep() statements to synchronise activity between the main thread and buffer processing worker thread, and hence results could be non-deterministic depending on system thread scheduling and performance.
        // Decided to do this, as making things fully deterministic would involve adding more test-only thread synchronising mechanisms (in addition to the existing WorkerThreadBufferFlushStrategyBase.workerThreadCompleteSignal property), which would mean more redundtant statements executing during normal runtime.
        // I think the current implementation strikes a balance between having fully deterministic tests, and not interfering too much with normal runtime code/operation.

        private ManualResetEvent workerThreadCompleteSignal;
        private EventHandler flushHandler;
        private SizeLimitedBufferFlushStrategy<String, String, ApplicationScreen, AccessLevel> testSizeLimitedBufferFlushStrategy;
        private Int32 flushEventsRaised;
        private Int32 millisecondsToWaitBeforeStop;

        [SetUp]
        protected void SetUp()
        {
            millisecondsToWaitBeforeStop = 250;
            flushEventsRaised = 0;
            workerThreadCompleteSignal = new ManualResetEvent(false); 
            testSizeLimitedBufferFlushStrategy = new SizeLimitedBufferFlushStrategy<String, String, ApplicationScreen, AccessLevel>(3, new NullMetricLogger(), workerThreadCompleteSignal);
            flushHandler = (Object sender, EventArgs e) =>
            {
                flushEventsRaised++;
                // The following property sets simulate resetting that occurs in the AccessManagerTemporalEventPersisterBuffer.Flush() method
                testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.GroupEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.EntityTypeEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.EntityEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 0;
            };
            testSizeLimitedBufferFlushStrategy.BufferFlushed += flushHandler;
            testSizeLimitedBufferFlushStrategy.Start();
        }

        [TearDown]
        protected void TearDown()
        {
            testSizeLimitedBufferFlushStrategy.BufferFlushed -= flushHandler;
            testSizeLimitedBufferFlushStrategy.Dispose();
            workerThreadCompleteSignal.Dispose();
        }

        [Test]
        public void Constructor_BufferSizeLimitParameterLessThan1()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSizeLimitedBufferFlushStrategy = new SizeLimitedBufferFlushStrategy<String, String, ApplicationScreen, AccessLevel>(0, new NullMetricLogger(), workerThreadCompleteSignal);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'bufferSizeLimit' with value 0 cannot be less than 1."));
            Assert.AreEqual(e.ParamName, "bufferSizeLimit");
        }

        [Test]
        public void UserEventBufferItemCount()
        {
            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 2;

            Assert.AreEqual(0, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 3;
            // Sleep to try to ensure the worker thread has enough time to process the above buffered events
            Thread.Sleep(millisecondsToWaitBeforeStop);

            Assert.AreEqual(1, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.Stop();
        }

        [Test]
        public void GroupEventBufferItemCount()
        {
            testSizeLimitedBufferFlushStrategy.GroupEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.GroupEventBufferItemCount = 2;

            Assert.AreEqual(0, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.GroupEventBufferItemCount = 3;
            // Sleep to try to ensure the worker thread has enough time to process the above buffered events
            Thread.Sleep(millisecondsToWaitBeforeStop);

            Assert.AreEqual(1, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.Stop();
        }

        [Test]
        public void UserToGroupMappingEventBufferItemCount()
        {
            testSizeLimitedBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 2;

            Assert.AreEqual(0, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 3;
            // Sleep to try to ensure the worker thread has enough time to process the above buffered events
            Thread.Sleep(millisecondsToWaitBeforeStop);

            Assert.AreEqual(1, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.Stop();
        }

        [Test]
        public void GroupToGroupMappingEventBufferItemCount()
        {
            testSizeLimitedBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 2;

            Assert.AreEqual(0, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 3;
            // Sleep to try to ensure the worker thread has enough time to process the above buffered events
            Thread.Sleep(millisecondsToWaitBeforeStop);

            Assert.AreEqual(1, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.Stop();
        }

        [Test]
        public void UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount()
        {
            testSizeLimitedBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 2;

            Assert.AreEqual(0, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 3;
            // Sleep to try to ensure the worker thread has enough time to process the above buffered events
            Thread.Sleep(millisecondsToWaitBeforeStop);

            Assert.AreEqual(1, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.Stop();
        }

        [Test]
        public void GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount()
        {
            testSizeLimitedBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 2;

            Assert.AreEqual(0, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 3;
            // Sleep to try to ensure the worker thread has enough time to process the above buffered events
            Thread.Sleep(millisecondsToWaitBeforeStop);

            Assert.AreEqual(1, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.Stop();
        }

        [Test]
        public void EntityTypeEventBufferItemCount()
        {
            testSizeLimitedBufferFlushStrategy.EntityTypeEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.EntityTypeEventBufferItemCount = 2;

            Assert.AreEqual(0, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.EntityTypeEventBufferItemCount = 3;
            // Sleep to try to ensure the worker thread has enough time to process the above buffered events
            Thread.Sleep(millisecondsToWaitBeforeStop);

            Assert.AreEqual(1, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.Stop();
        }

        [Test]
        public void EntityEventBufferItemCount()
        {
            testSizeLimitedBufferFlushStrategy.EntityEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.EntityEventBufferItemCount = 2;

            Assert.AreEqual(0, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.EntityEventBufferItemCount = 3;
            // Sleep to try to ensure the worker thread has enough time to process the above buffered events
            Thread.Sleep(millisecondsToWaitBeforeStop);

            Assert.AreEqual(1, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.Stop();
        }

        [Test]
        public void UserToEntityMappingEventBufferItemCount()
        {
            testSizeLimitedBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 2;

            Assert.AreEqual(0, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 3;
            // Sleep to try to ensure the worker thread has enough time to process the above buffered events
            Thread.Sleep(millisecondsToWaitBeforeStop);

            Assert.AreEqual(1, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.Stop();
        }

        [Test]
        public void GroupToEntityMappingEventBufferItemCount()
        {
            testSizeLimitedBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 2;

            Assert.AreEqual(0, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 3;
            // Sleep to try to ensure the worker thread has enough time to process the above buffered events
            Thread.Sleep(millisecondsToWaitBeforeStop);

            Assert.AreEqual(1, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.Stop();
        }

        [Test]
        public void Stop_ExceptionOccurredOnWorkerThread()
        {
            const string exceptionMessage = "Mock worker thread exception.";
            testSizeLimitedBufferFlushStrategy.BufferFlushed -= flushHandler;
            flushHandler = (Object sender, EventArgs e) => { throw new Exception(exceptionMessage); };
            testSizeLimitedBufferFlushStrategy.BufferFlushed += flushHandler;

            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.GroupEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.EntityTypeEventBufferItemCount = 1;
            // Sleep to try to ensure the worker thread has enough time to throw the exception
            Thread.Sleep(millisecondsToWaitBeforeStop);

            BufferFlushingException e = Assert.Throws<BufferFlushingException>(delegate
            {
                testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 1;
            });

            Assert.That(e.Message, Does.StartWith("Exception occurred on buffer flushing worker thread at "));
            Assert.That(e.InnerException.Message, Does.StartWith(exceptionMessage));
        }

        [Test]
        public void Stop_ExceptionOccursOnWorkerThreadProcessingRemainingEvents()
        {
            const string exceptionMessage = "Mock worker thread exception.";
            testSizeLimitedBufferFlushStrategy.BufferFlushed -= flushHandler;
            flushHandler = (Object sender, EventArgs e) => { throw new Exception(exceptionMessage); };
            testSizeLimitedBufferFlushStrategy.BufferFlushed += flushHandler;

            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 1;

            BufferFlushingException e = Assert.Throws<BufferFlushingException>(delegate
            {
                testSizeLimitedBufferFlushStrategy.Stop();
            });

            Assert.That(e.Message, Does.StartWith("Exception occurred on buffer flushing worker thread at "));
            Assert.That(e.InnerException.Message, Does.StartWith(exceptionMessage));
        }

        [Test]
        public void Stop()
        {
            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.GroupEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.EntityTypeEventBufferItemCount = 1;
            Thread.Sleep(millisecondsToWaitBeforeStop);

            Assert.AreEqual(1, flushEventsRaised);

            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 1;

            testSizeLimitedBufferFlushStrategy.Stop();
            workerThreadCompleteSignal.WaitOne();

            Assert.AreEqual(2, flushEventsRaised);
        }

        /// <summary>
        /// Tests a scenario where 'bufferSizeLimit' is reached, and then immediately after that another event is buffered before the worker thread has zeroed the queue item counts.  Ensures that this does not trigger another immediate subsequent flush (as was happening when member 'bufferProcessSignal' was an AutoResetEvent rather than a ManualResetEvent).
        /// </summary>
        [Test]
        public void UserEventBufferItemCount_AdditionalItemBufferedBeforeCountsAreReset()
        {
            testSizeLimitedBufferFlushStrategy.BufferFlushed -= flushHandler;
            flushHandler = (Object sender, EventArgs e) =>
            {
                flushEventsRaised++;
                Thread.Sleep(250);
                // The following property sets simulate resetting that occurs in the AccessManagerTemporalEventPersisterBuffer.Flush() method
                testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.GroupEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.EntityTypeEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.EntityEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 0;
                testSizeLimitedBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 0;
            };
            testSizeLimitedBufferFlushStrategy.BufferFlushed += flushHandler;

            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 2;
            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 3;
            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 4;
            // Sleep to try to ensure the worker thread has enough time to sleep itself and complete one flush process
            Thread.Sleep(500);

            testSizeLimitedBufferFlushStrategy.Stop();
            workerThreadCompleteSignal.WaitOne();

            Assert.AreEqual(1, flushEventsRaised);
        }
    }
}
