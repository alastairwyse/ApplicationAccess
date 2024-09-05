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
using ApplicationMetrics.MetricLoggers;
using NUnit.Framework;
using NUnit.Framework.Internal;

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Persistence.LoopingWorkerThreadBufferFlushStrategy class.
    /// </summary>
    public class LoopingWorkerThreadBufferFlushStrategyTests
    {
        private ManualResetEvent workerThreadCompleteSignal;
        private EventHandler flushHandler;
        private BufferFlushingException testFlushingException;
        private Int32 flushingExceptionActionCallCount;
        private Action<BufferFlushingException> testFlushingExceptionAction;
        private LoopingWorkerThreadBufferFlushStrategy testLoopingWorkerThreadBufferFlushStrategy;
        private Int32 flushEventsRaised;

        [SetUp]
        protected void SetUp()
        {
            flushEventsRaised = 0;
            workerThreadCompleteSignal = new ManualResetEvent(false);
            testFlushingException = null;
            flushingExceptionActionCallCount = 0;
            testFlushingExceptionAction = (BufferFlushingException bufferFlushingException) =>
            {
                testFlushingException = bufferFlushingException;
                flushingExceptionActionCallCount++;
            };
            testLoopingWorkerThreadBufferFlushStrategy = new LoopingWorkerThreadBufferFlushStrategy(250, false, new NullMetricLogger(), testFlushingExceptionAction, workerThreadCompleteSignal, 2); 
            flushHandler = (Object sender, EventArgs e) =>
            {
                flushEventsRaised++;
                // The following property sets simulate resetting that occurs in the AccessManagerTemporalEventPersisterBuffer.Flush() method
                testLoopingWorkerThreadBufferFlushStrategy.UserEventBufferItemCount = 0;
                testLoopingWorkerThreadBufferFlushStrategy.GroupEventBufferItemCount = 0;
                testLoopingWorkerThreadBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = 0;
                testLoopingWorkerThreadBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = 0;
                testLoopingWorkerThreadBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                testLoopingWorkerThreadBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = 0;
                testLoopingWorkerThreadBufferFlushStrategy.EntityTypeEventBufferItemCount = 0;
                testLoopingWorkerThreadBufferFlushStrategy.EntityEventBufferItemCount = 0;
                testLoopingWorkerThreadBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = 0;
                testLoopingWorkerThreadBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = 0;
            };
            testLoopingWorkerThreadBufferFlushStrategy.BufferFlushed += flushHandler;
        }

        [TearDown]
        protected void TearDown()
        {
            testLoopingWorkerThreadBufferFlushStrategy.Dispose();
        }

        [Test]
        public void Constructor_FlushLoopIntervalParameterLessThan1()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testLoopingWorkerThreadBufferFlushStrategy = new LoopingWorkerThreadBufferFlushStrategy(0, false);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'flushLoopInterval' with value 0 cannot be less than 1."));
            Assert.AreEqual(e.ParamName, "flushLoopInterval");
        }

        [Test]
        public void Constructor_FlushLoopIterationCountParameterLessThan1()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testLoopingWorkerThreadBufferFlushStrategy = new LoopingWorkerThreadBufferFlushStrategy(1000, false, new NullMetricLogger(), testFlushingExceptionAction, new ManualResetEvent(false), 0);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'flushLoopIterationCount' with value 0 cannot be less than 1."));
            Assert.AreEqual(e.ParamName, "flushLoopIterationCount");
        }

        [Test]
        public void BufferFlushedEventsRaised()
        {
            testLoopingWorkerThreadBufferFlushStrategy.Start();
            workerThreadCompleteSignal.WaitOne();

            Assert.AreEqual(2, flushEventsRaised);
        }

        [Test]
        public void Stop()
        {
            using (var secondCallToFlushSignal = new ManualResetEvent(false))
            {
                testLoopingWorkerThreadBufferFlushStrategy.BufferFlushed -= flushHandler; 
                flushHandler = (Object sender, EventArgs e) =>
                {
                    // On the second raising of the event, simulate adding a buffered event and set the 'secondCallToFlushSignal' call (main thread then immediately calls Stop() which simulates events being buffered after Stop() is called)
                    if (flushEventsRaised == 1)
                    {
                        testLoopingWorkerThreadBufferFlushStrategy.UserEventBufferItemCount = 1;
                        secondCallToFlushSignal.Set();
                    }
                    flushEventsRaised++;
                };
                testLoopingWorkerThreadBufferFlushStrategy.BufferFlushed += flushHandler;

                testLoopingWorkerThreadBufferFlushStrategy.Start();
                secondCallToFlushSignal.WaitOne();
                testLoopingWorkerThreadBufferFlushStrategy.Stop();
                workerThreadCompleteSignal.WaitOne();

                Assert.AreEqual(3, flushEventsRaised);
                Assert.Null(testFlushingException);
                Assert.AreEqual(0, flushingExceptionActionCallCount);
            }
        }
    }
}
