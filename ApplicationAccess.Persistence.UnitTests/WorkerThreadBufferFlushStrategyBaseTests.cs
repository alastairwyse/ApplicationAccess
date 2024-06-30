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
    /// Unit tests for the ApplicationAccess.Persistence.WorkerThreadBufferFlushStrategyBase class.
    /// </summary>
    /// <remarks>Since WorkerThreadBufferProcessorBase is an abstract class, tests are performed via derived class SizeLimitedBufferFlushStrategy and others.</remarks>
    public class WorkerThreadBufferFlushStrategyBaseTests
    {
        // Some of these tests use Thread.Sleep() statements to synchronise activity between the main thread and buffer processing worker thread, and hence results could be non-deterministic depending on system thread scheduling and performance.
        // Decided to do this, as making things fully deterministic would involve adding more test-only thread synchronising mechanisms (in addition to the existing WorkerThreadBufferFlushStrategyBase.workerThreadCompleteSignal property), which would mean more redundtant statements executing during normal runtime.
        // I think the current implementation strikes a balance between having fully deterministic tests, and not interfering too much with normal runtime code/operation.

        private ManualResetEvent workerThreadCompleteSignal;
        private EventHandler flushHandler;
        private BufferFlushingException testFlushingException;
        private Int32 flushingExceptionActionCallCount;
        private Action<BufferFlushingException> testFlushingExceptionAction;
        private SizeLimitedBufferFlushStrategy testSizeLimitedBufferFlushStrategy;
        private Int32 flushEventsRaised;
        private Int32 millisecondsToWaitBeforeStop;

        [SetUp]
        protected void SetUp()
        {
            millisecondsToWaitBeforeStop = 250;
            flushEventsRaised = 0;
            workerThreadCompleteSignal = new ManualResetEvent(false);
            testFlushingException = null;
            flushingExceptionActionCallCount = 0;
            testFlushingExceptionAction = (BufferFlushingException bufferFlushingException) =>
            {
                testFlushingException = bufferFlushingException;
                flushingExceptionActionCallCount++;
            };
            testSizeLimitedBufferFlushStrategy = new SizeLimitedBufferFlushStrategy(3, new NullMetricLogger(), testFlushingExceptionAction, workerThreadCompleteSignal);
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
        public void Start_BufferFlushingActionNotSet()
        {
            var testBufferFlushStrategyWithNoWorkerThreadImplementation = new BufferFlushStrategyWithNoWorkerThreadImplementation();

            InvalidOperationException e = Assert.Throws<InvalidOperationException>(delegate
            {
                testBufferFlushStrategyWithNoWorkerThreadImplementation.Start();
            });

            Assert.That(e.Message, Does.StartWith("Property 'BufferFlushingAction' has not been set."));
        }

        [Test]
        public void SetUserEventBufferItemCount_ValueLessThan0()
        {
            ArgumentOutOfRangeException e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = -1;
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'value' with value -1 cannot be less than 0."));
            Assert.AreEqual("value", e.ParamName);
        }

        [Test]
        public void SetGroupEventBufferItemCount_ValueLessThan0()
        {
            ArgumentOutOfRangeException e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSizeLimitedBufferFlushStrategy.GroupEventBufferItemCount = -1;
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'value' with value -1 cannot be less than 0."));
            Assert.AreEqual("value", e.ParamName);
        }

        [Test]
        public void SetUserToGroupMappingEventBufferItemCount_ValueLessThan0()
        {
            ArgumentOutOfRangeException e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSizeLimitedBufferFlushStrategy.UserToGroupMappingEventBufferItemCount = -1;
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'value' with value -1 cannot be less than 0."));
            Assert.AreEqual("value", e.ParamName);
        }

        [Test]
        public void SetGroupToGroupMappingEventBufferItemCount_ValueLessThan0()
        {
            ArgumentOutOfRangeException e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSizeLimitedBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount = -1;
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'value' with value -1 cannot be less than 0."));
            Assert.AreEqual("value", e.ParamName);
        }

        [Test]
        public void SetUserToApplicationComponentAndAccessLevelMappingEventBufferItemCount_ValueLessThan0()
        {
            ArgumentOutOfRangeException e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSizeLimitedBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount = -1;
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'value' with value -1 cannot be less than 0."));
            Assert.AreEqual("value", e.ParamName);
        }

        [Test]
        public void SetGroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount_ValueLessThan0()
        {
            ArgumentOutOfRangeException e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSizeLimitedBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount = -1;
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'value' with value -1 cannot be less than 0."));
            Assert.AreEqual("value", e.ParamName);
        }

        [Test]
        public void SetEntityTypeEventBufferItemCount_ValueLessThan0()
        {
            ArgumentOutOfRangeException e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSizeLimitedBufferFlushStrategy.EntityTypeEventBufferItemCount = -1;
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'value' with value -1 cannot be less than 0."));
            Assert.AreEqual("value", e.ParamName);
        }

        [Test]
        public void SetEntityEventBufferItemCount_ValueLessThan0()
        {
            ArgumentOutOfRangeException e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSizeLimitedBufferFlushStrategy.EntityEventBufferItemCount = -1;
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'value' with value -1 cannot be less than 0."));
            Assert.AreEqual("value", e.ParamName);
        }

        [Test]
        public void SetUserToEntityMappingEventBufferItemCount_ValueLessThan0()
        {
            ArgumentOutOfRangeException e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSizeLimitedBufferFlushStrategy.UserToEntityMappingEventBufferItemCount = -1;
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'value' with value -1 cannot be less than 0."));
            Assert.AreEqual("value", e.ParamName);
        }

        [Test]
        public void SetGroupToEntityMappingEventBufferItemCount_ValueLessThan0()
        {
            ArgumentOutOfRangeException e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testSizeLimitedBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount = -1;
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'value' with value -1 cannot be less than 0."));
            Assert.AreEqual("value", e.ParamName);
        }

        [Test]
        public void BufferFlushingAction_ExceptionOnWorkerThread()
        {
            const string exceptionMessage = "Mock worker thread exception.";
            testSizeLimitedBufferFlushStrategy.BufferFlushed -= flushHandler;
            var flushInnerException = new Exception(exceptionMessage);
            flushHandler = (Object sender, EventArgs e) => { throw flushInnerException; };
            testSizeLimitedBufferFlushStrategy.BufferFlushed += flushHandler;
            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 1;
            testSizeLimitedBufferFlushStrategy.GroupEventBufferItemCount = 1;

            testSizeLimitedBufferFlushStrategy.EntityTypeEventBufferItemCount = 1;

            workerThreadCompleteSignal.WaitOne();
            Assert.NotNull(testFlushingException);
            Assert.That(testFlushingException.Message, Does.StartWith("Exception occurred on buffer flushing worker thread at "));
            Assert.AreSame(testFlushingException.InnerException, flushInnerException);
            Assert.AreEqual(1, flushingExceptionActionCallCount);
        }

        [Test]
        public void BufferFlushingAction_ExceptionOccursOnWorkerThreadProcessingRemainingEventsAfterStop()
        {
            const string exceptionMessage = "Mock worker thread exception.";
            testSizeLimitedBufferFlushStrategy.BufferFlushed -= flushHandler;
            var flushInnerException = new Exception(exceptionMessage);
            flushHandler = (Object sender, EventArgs e) => { throw flushInnerException; };
            testSizeLimitedBufferFlushStrategy.BufferFlushed += flushHandler;
            testSizeLimitedBufferFlushStrategy.UserEventBufferItemCount = 1;

            testSizeLimitedBufferFlushStrategy.Stop();

            workerThreadCompleteSignal.WaitOne();
            Assert.NotNull(testFlushingException);
            Assert.That(testFlushingException.Message, Does.StartWith("Exception occurred on buffer flushing worker thread at "));
            Assert.AreSame(testFlushingException.InnerException, flushInnerException);
            Assert.AreEqual(1, flushingExceptionActionCallCount);
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
            Assert.Null(testFlushingException);
            Assert.AreEqual(0, flushingExceptionActionCallCount);
        }

        #region Nested Classes

        /// <summary>
        /// Implementation of WorkerThreadBufferFlushStrategyBase with no worker thread implementation.
        /// </summary>
        private class BufferFlushStrategyWithNoWorkerThreadImplementation : WorkerThreadBufferFlushStrategyBase
        {
            public BufferFlushStrategyWithNoWorkerThreadImplementation()
                : base()
            {
            }
        }

        #endregion
    }
}
