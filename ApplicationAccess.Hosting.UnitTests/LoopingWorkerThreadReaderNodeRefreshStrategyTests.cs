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
using ApplicationAccess.Persistence;
using NUnit.Framework;

namespace ApplicationAccess.Hosting.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.LoopingWorkerThreadReaderNodeRefreshStrategy class.
    /// </summary>
    public class LoopingWorkerThreadReaderNodeRefreshStrategyTests
    {
        // Note - some tests in this class are not deterministic, and could fail under difference harware.  See note/disclaimer in class ApplicationAccess.Persistence.WorkerThreadBufferFlushStrategyBaseTests.

        private ManualResetEvent workerThreadCompleteSignal;
        private ReaderNodeRefreshException testRefreshException;
        private Int32 refreshExceptionActionCallCount;
        private Action<ReaderNodeRefreshException> testRefreshExceptionAction;
        private LoopingWorkerThreadReaderNodeRefreshStrategy testLoopingWorkerThreadReaderNodeRefreshStrategy;

        [SetUp]
        protected void SetUp()
        {
            workerThreadCompleteSignal = new ManualResetEvent(false);
            testRefreshException = null;
            refreshExceptionActionCallCount = 0;
            testRefreshExceptionAction = (ReaderNodeRefreshException readerNodeRefreshException) =>
            {
                testRefreshException = readerNodeRefreshException;
                refreshExceptionActionCallCount++;
            };
            testLoopingWorkerThreadReaderNodeRefreshStrategy = new LoopingWorkerThreadReaderNodeRefreshStrategy(250, testRefreshExceptionAction, workerThreadCompleteSignal, 3);
        }

        [TearDown]
        protected void TearDown()
        {
            testLoopingWorkerThreadReaderNodeRefreshStrategy.Dispose();
        }

        [Test]
        public void Constructor_RefreshLoopIntervalLessThan1()
        {
            var e = Assert.Throws<ArgumentOutOfRangeException>(delegate
            {
                testLoopingWorkerThreadReaderNodeRefreshStrategy = new LoopingWorkerThreadReaderNodeRefreshStrategy(0, testRefreshExceptionAction);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'refreshLoopInterval' with value 0 cannot be less than 1."));
            Assert.AreEqual("refreshLoopInterval", e.ParamName);
        }

        [Test]
        public void ReaderNodeRefreshed_ExceptionOnWorkerThread()
        {
            const string exceptionMessage = "Worker thread refresh exception.";
            var refreshInnerException = new Exception(exceptionMessage);
            testLoopingWorkerThreadReaderNodeRefreshStrategy.ReaderNodeRefreshed += (Object sender, EventArgs e) => { throw refreshInnerException; };
            testLoopingWorkerThreadReaderNodeRefreshStrategy.Start();

            // Wait for the worker thread loop iterval to elapse
            Thread.Sleep(500);

            workerThreadCompleteSignal.WaitOne();
            Assert.NotNull(testRefreshException);
            Assert.That(testRefreshException.Message, Does.StartWith("Exception occurred on reader node refreshing worker thread at "));
            Assert.AreSame(testRefreshException.InnerException, refreshInnerException);
            Assert.AreEqual(1, refreshExceptionActionCallCount);
        }

        [Test]
        public void Start()
        {
            Int32 refreshcount = 0;
            testLoopingWorkerThreadReaderNodeRefreshStrategy.ReaderNodeRefreshed += (Object sender, EventArgs e) => { refreshcount++; };

            testLoopingWorkerThreadReaderNodeRefreshStrategy.Start();

            workerThreadCompleteSignal.WaitOne();
            Assert.AreEqual(3, refreshcount);
        }
                
        [Test]
        public void Stop()
        {
            testLoopingWorkerThreadReaderNodeRefreshStrategy.Dispose();
            workerThreadCompleteSignal = new ManualResetEvent(false);
            testLoopingWorkerThreadReaderNodeRefreshStrategy = new LoopingWorkerThreadReaderNodeRefreshStrategy(500, testRefreshExceptionAction, workerThreadCompleteSignal, 10);
            Int32 refreshEventsRaised = 0;
            using (var secondCallToRefreshSignal = new ManualResetEvent(false))
            {
                testLoopingWorkerThreadReaderNodeRefreshStrategy.ReaderNodeRefreshed += (Object sender, EventArgs e) => 
                {
                    if (refreshEventsRaised == 1)
                    {
                        secondCallToRefreshSignal.Set();
                    }
                    refreshEventsRaised++;
                };

                testLoopingWorkerThreadReaderNodeRefreshStrategy.Start();
                secondCallToRefreshSignal.WaitOne();
                // Wait a short time to try to ensure that Stop() is not called before the start of the next worker thread loop iteration (in which case the worker thread would stop prematurely and 'refreshEventsRaised' count would only be 2 not 3)
                Thread.Sleep(200);
                testLoopingWorkerThreadReaderNodeRefreshStrategy.Stop();
                workerThreadCompleteSignal.WaitOne();

                Assert.AreEqual(3, refreshEventsRaised);
            }
        }
    }
}
