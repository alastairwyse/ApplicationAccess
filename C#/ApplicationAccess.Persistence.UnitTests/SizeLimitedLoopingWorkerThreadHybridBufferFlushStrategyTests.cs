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
using System.Threading;
using ApplicationAccess.Utilities;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy class.
    /// </summary>
    public class SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategyTests
    {
        // As per notes in other test classes in this namespace, am using Thread.Sleep() statements to synchronise activity between the main thread, the buffer flushing worker thread, and the looping timer thread, and hence results could be non-deterministic depending on system thread scheduling and performance.
        // Given the class under test uses 3 threads, having fully deterministic tests would mean having to create a lot of test-only event wait handles which would have to be continually checked for null in non-test operation.
        // As per comments for other test classes, the current implementation strikes a balance between having fully deterministic tests, and not interfering too much with normal runtime code/operation.

        private IDateTimeProvider mockDateTimeProvider;
        private AutoResetEvent loopingTriggerThreadLoopCompleteSignal;
        private ManualResetEvent workerThreadCompleteSignal;
        private EventHandler flushHandler;
        private SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategyWithProtectedMethods testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy;
        private Int32 flushEventsRaised;

        [SetUp]
        protected void SetUp()
        {
            mockDateTimeProvider = Substitute.For<IDateTimeProvider>();
            loopingTriggerThreadLoopCompleteSignal = new AutoResetEvent(false);
            workerThreadCompleteSignal = new ManualResetEvent(false);
            flushHandler = (Object sender, EventArgs e) =>
            {
                flushEventsRaised++;
                // The following property sets simulate resetting that occurs in the AccessManagerTemporalEventPersisterBuffer.Flush() method
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.UserEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.GroupEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.EntityTypeEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.EntityEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 0;
            };
            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy = new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategyWithProtectedMethods(3, 250, mockDateTimeProvider, loopingTriggerThreadLoopCompleteSignal, workerThreadCompleteSignal);
            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.BufferFlushed += flushHandler;
            flushEventsRaised = 0;
        }

        [TearDown]
        protected void TearDown()
        {
            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.BufferFlushed -= flushHandler;
            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.Dispose();
            workerThreadCompleteSignal.Dispose();
            loopingTriggerThreadLoopCompleteSignal.Dispose();
        }

        [Test]
        public void Constructor_FlushLoopIntervalParameterLessThan1()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy = new SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategyWithProtectedMethods(3, 0, mockDateTimeProvider, loopingTriggerThreadLoopCompleteSignal, workerThreadCompleteSignal);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'flushLoopInterval' with value 0 cannot be less than 1."));
            Assert.AreEqual(e.ParamName, "flushLoopInterval");
        }

        /// <summary>
        /// Simulates where a flush is triggered twice by the looping trigger thread.
        /// </summary>
        [Test]
        public void Start_BufferFlushedEventsRaisedByLoopingTriggerThread()
        {
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                // This is the call to UtcNow() from the Start() method
                CreateDataTimeFromString("2025-05-02 18:13:00.000"),
                // Simulates the call to UtcNow() at the end of the first flush, triggered by the first iteration of the looping trigger thread
                CreateDataTimeFromString("2025-05-02 18:13:00.100"),
                // Simulates the call to UtcNow() from the looping trigger thread to calculate the sleep time on the second loop iteration
                CreateDataTimeFromString("2025-05-02 18:13:00.250"),
                // Simulates the call to UtcNow() at the end of the second flush, triggered by the third iteration of the looping trigger thread
                CreateDataTimeFromString("2025-05-02 18:13:00.350")
            );

            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.Start();
            // Wait for all threads to start and for the looping trigger thread to complete one iteration
            Thread.Sleep(500);
            // Should have slept for the full 'flushLoopInterval' after signalling the buffer flushing worker thread
            Assert.AreEqual(250, testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.LastWaitInterval);
            // Signal the looping trigger thread and wait for another iteration
            loopingTriggerThreadLoopCompleteSignal.Set();
            Thread.Sleep(500);
            // 'lastFlushCompleteTime' should be the same as for the previous loop iteration, hence should have slept for just 100ms and NOT signalled the buffer flushing worker thread
            Assert.AreEqual(100, testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.LastWaitInterval);
            // Signal the looping trigger thread and wait for another iteration
            loopingTriggerThreadLoopCompleteSignal.Set();
            Thread.Sleep(500);
            // Should have again slept for the full 'flushLoopInterval' after signalling the buffer flushing worker thread
            Assert.AreEqual(250, testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.LastWaitInterval);
            // 250ms wait after being signalled will mean that 'stopMethodCalled' will be true on the next looping trigger thread iteration, due to below call to Stop()
            loopingTriggerThreadLoopCompleteSignal.Set();
            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.Stop();
            workerThreadCompleteSignal.WaitOne();

            Assert.AreEqual(2, flushEventsRaised);
            mockDateTimeProvider.Received(4).UtcNow();
        }

        /// <summary>
        /// Simulates where a flush is triggered twice by the looping trigger thread, during which the sleep time is calculated as less than 0.
        /// </summary>
        [Test]
        public void Start_LoopingTriggerThreadSleepTimeLessThan0()
        {
            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                // This is the call to UtcNow() from the Start() method
                CreateDataTimeFromString("2025-05-02 18:13:00.000"),
                // Simulates the call to UtcNow() at the end of the first flush, triggered by the first iteration of the looping trigger thread
                CreateDataTimeFromString("2025-05-02 18:13:00.100"),
                // Simulates the call to UtcNow() from the looping trigger thread to calculate the sleep time on the second loop iteration
                CreateDataTimeFromString("2025-05-02 18:13:00.450"),
                // Simulates the call to UtcNow() at the end of the second flush, triggered by the third iteration of the looping trigger thread
                CreateDataTimeFromString("2025-05-02 18:13:00.500")
            );

            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.Start();
            // Wait for all threads to start and for the looping trigger thread to complete one iteration
            Thread.Sleep(500);
            // Should have slept for the full 'flushLoopInterval' after signalling the buffer flushing worker thread
            Assert.AreEqual(250, testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.LastWaitInterval);
            // Signal the looping trigger thread and wait for another iteration
            loopingTriggerThreadLoopCompleteSignal.Set();
            Thread.Sleep(500);
            // 'lastFlushCompleteTime' should be the same as the previous loop iteration, and UtcNow() gives a time 100ms later than 'lastFlushCompleteTime' plus the 250ms loop interval, hence 'LastWaitInterval' should be -100
            Assert.AreEqual(-100, testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.LastWaitInterval);
            // Signal the looping trigger thread and wait for another iteration
            loopingTriggerThreadLoopCompleteSignal.Set();
            Thread.Sleep(500);
            // Should have again slept for the full 'flushLoopInterval' after signalling the buffer flushing worker thread
            Assert.AreEqual(250, testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.LastWaitInterval);
            // 250ms wait after being signalled will mean that 'stopMethodCalled' will be true on the next looping trigger thread iteration, due to below call to Stop()
            loopingTriggerThreadLoopCompleteSignal.Set();
            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.Stop();
            workerThreadCompleteSignal.WaitOne();

            Assert.AreEqual(2, flushEventsRaised);
            mockDateTimeProvider.Received(4).UtcNow();
        }

        /// <summary>
        /// Tests that the looping trigger thread waits without triggering a flush if it iterates while a flush is in progress.
        /// </summary>
        [Test]
        public void Start_LoopingTriggerThreadIteratesWhileFlushIsOccurring()
        {
            var flushCompleteSignal = new AutoResetEvent(false);
            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.BufferFlushed -= flushHandler;
            flushHandler = (Object sender, EventArgs e) =>
            {
                flushEventsRaised++;
                Thread.Sleep(250);
                // The following property sets simulate resetting that occurs in the AccessManagerTemporalEventPersisterBuffer.Flush() method
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.UserEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.GroupEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.EntityTypeEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.EntityEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 0;
                testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 0;
                flushCompleteSignal.WaitOne();
            };
            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.BufferFlushed += flushHandler;

            mockDateTimeProvider.UtcNow().Returns<DateTime>
            (
                // This is the call to UtcNow() from the Start() method
                CreateDataTimeFromString("2025-05-02 18:13:00.000"),
                // Simulates the call to UtcNow() at the end of the first flush, triggered by the first iteration of the looping trigger thread
                CreateDataTimeFromString("2025-05-02 18:13:00.100"),
                // Simulates the call to UtcNow() at the end of the second flush, triggered by buffering of events
                CreateDataTimeFromString("2025-05-02 18:13:00.300")
            );

            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.Start();
            // Wait for all threads to start and for the looping trigger thread to complete one iteration
            Thread.Sleep(500);
            // Signal the flush to complete
            flushCompleteSignal.Set();
            Thread.Sleep(250);
            // Should have slept for the full 'flushLoopInterval' after signalling the buffer flushing worker thread
            Assert.AreEqual(250, testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.LastWaitInterval);
            // Generate some events to trigger a 'size limit' flush
            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.EntityTypeEventBufferItemCount = 1;
            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.EntityTypeEventBufferItemCount = 2;
            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.EntityTypeEventBufferItemCount = 3;
            // Sleep to try to ensure the worker thread has enough time to process the above buffered events
            Thread.Sleep(250);
            // The buffer flushing worker thread should now be waiting on reset event 'flushCompleteSignal', so signal another iteration of the looping trigger thread
            loopingTriggerThreadLoopCompleteSignal.Set();
            Thread.Sleep(500);
            // Should have slept for the full 'flushLoopInterval' since member 'isFlushing' is true
            Assert.AreEqual(250, testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.LastWaitInterval);
            flushCompleteSignal.Set();
            // 250ms wait after being signalled will mean that 'stopMethodCalled' will be true on the next looping trigger thread iteration, due to below call to Stop()
            loopingTriggerThreadLoopCompleteSignal.Set();
            testSizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy.Stop();
            workerThreadCompleteSignal.WaitOne();

            Assert.AreEqual(2, flushEventsRaised);
            mockDateTimeProvider.Received(3).UtcNow();
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates a DateTime from the specified yyyy-MM-dd HH:mm:ss.fff format string.
        /// </summary>
        /// <param name="stringifiedDateTime">The stringified date/time to convert.</param>
        /// <returns>A DateTime.</returns>
        protected DateTime CreateDataTimeFromString(String stringifiedDateTime)
        {
            DateTime returnDateTime = DateTime.ParseExact(stringifiedDateTime, "yyyy-MM-dd HH:mm:ss.fff", DateTimeFormatInfo.InvariantInfo);

            return DateTime.SpecifyKind(returnDateTime, DateTimeKind.Utc);
        }

        #endregion

        #region Nested Classes

        /// <summary>
        /// Version of the SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy class where private and protected methods are exposed as public so that they can be unit tested.
        /// </summary>
        private class SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategyWithProtectedMethods : SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy
        {
            /// <summary>
            /// The most recent interval that the looping trigger thread waited for between iterations.
            /// </summary>
            public Int32 LastWaitInterval
            {
                get { return lastWaitInterval; }
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Persistence.UnitTests.SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategyTests+SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategyWithProtectedMethods class.
            /// </summary>
            /// <param name="bufferSizeLimit">The total size of the buffers which when reached, triggers flushing/processing of the buffer contents.</param>
            /// <param name="flushLoopInterval">The time to wait (in milliseconds) between buffer flushing/processing iterations.</param>
            /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
            /// <param name="loopingTriggerThreadLoopCompleteSignal">Signal that is waited on each time an iteration of the looping trigger thread completes (for unit testing).</param>
            /// <param name="workerThreadCompleteSignal">Signal that will be set when the worker thread processing is complete (for unit testing).</param>
            public SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategyWithProtectedMethods(Int32 bufferSizeLimit, Int32 flushLoopInterval, IDateTimeProvider dateTimeProvider, AutoResetEvent loopingTriggerThreadLoopCompleteSignal, ManualResetEvent workerThreadCompleteSignal)
                : base(bufferSizeLimit, flushLoopInterval, dateTimeProvider, loopingTriggerThreadLoopCompleteSignal, workerThreadCompleteSignal)
            {
            }
        }

        #endregion
    }
}
