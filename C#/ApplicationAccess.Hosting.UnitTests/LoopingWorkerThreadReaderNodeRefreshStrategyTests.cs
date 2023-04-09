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
        private LoopingWorkerThreadReaderNodeRefreshStrategy testLoopingWorkerThreadReaderNodeRefreshStrategy;

        [SetUp]
        protected void SetUp()
        {
            workerThreadCompleteSignal = new ManualResetEvent(false);
            testLoopingWorkerThreadReaderNodeRefreshStrategy = new LoopingWorkerThreadReaderNodeRefreshStrategy(250, workerThreadCompleteSignal, 3);
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
                testLoopingWorkerThreadReaderNodeRefreshStrategy = new LoopingWorkerThreadReaderNodeRefreshStrategy(0);
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'refreshLoopInterval' with value 0 cannot be less than 1."));
            Assert.AreEqual("refreshLoopInterval", e.ParamName);
        }

        [Test]
        public void NotifyQueryMethodCalled_ExceptionOccurredOnWorkerThread()
        {
            var mockException = new Exception("Worker thread refresh exception.");
            testLoopingWorkerThreadReaderNodeRefreshStrategy.ReaderNodeRefreshed += (Object sender, EventArgs e) => { throw mockException; };
            testLoopingWorkerThreadReaderNodeRefreshStrategy.Start();
            // Wait for the worker thread loop iterval to elapse
            Thread.Sleep(500);

            var e = Assert.Throws<ReaderNodeRefreshException>(delegate
            {
                testLoopingWorkerThreadReaderNodeRefreshStrategy.NotifyQueryMethodCalled();
            });

            workerThreadCompleteSignal.WaitOne();
            Assert.That(e.Message, Does.StartWith("Exception occurred on reader node refreshing worker thread at "));
            Assert.AreEqual(e.InnerException, mockException);
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
        public void Stop_ExceptionOccurredOnWorkerThread()
        {
            var mockException = new Exception("Worker thread refresh exception.");
            testLoopingWorkerThreadReaderNodeRefreshStrategy.ReaderNodeRefreshed += (Object sender, EventArgs e) => { throw mockException; };
            testLoopingWorkerThreadReaderNodeRefreshStrategy.Start();
            // Wait for the worker thread loop iterval to elapse
            Thread.Sleep(300);

            var e = Assert.Throws<ReaderNodeRefreshException>(delegate
            {
                testLoopingWorkerThreadReaderNodeRefreshStrategy.Stop();
            });

            workerThreadCompleteSignal.WaitOne();
            Assert.That(e.Message, Does.StartWith("Exception occurred on reader node refreshing worker thread at "));
            Assert.AreEqual(e.InnerException, mockException);
        }
        
        [Test]
        public void Stop_ExceptionOccursOnWorkerThreadPerformingFinalRefresh()
        {
            var mockException = new Exception("Worker thread refresh exception.");
            testLoopingWorkerThreadReaderNodeRefreshStrategy.ReaderNodeRefreshed += (Object sender, EventArgs e) => { throw mockException; };
            testLoopingWorkerThreadReaderNodeRefreshStrategy.Start();
            // Wait a short time and then call stop
            //  At the first call to CheckAndThrowRefreshException() in the Stop() method, the worked thread is still waiting... hence second call to CheckAndThrowRefreshException() in Stop() should rethrow the exception
            Thread.Sleep(50);

            var e = Assert.Throws<ReaderNodeRefreshException>(delegate
            {
                testLoopingWorkerThreadReaderNodeRefreshStrategy.Stop();
            });

            workerThreadCompleteSignal.WaitOne();
            Assert.That(e.Message, Does.StartWith("Exception occurred on reader node refreshing worker thread at "));
            Assert.AreEqual(e.InnerException, mockException);
        }
        
        [Test]
        public void Stop()
        {
            testLoopingWorkerThreadReaderNodeRefreshStrategy.Dispose();
            workerThreadCompleteSignal = new ManualResetEvent(false);
            testLoopingWorkerThreadReaderNodeRefreshStrategy = new LoopingWorkerThreadReaderNodeRefreshStrategy(500, workerThreadCompleteSignal, 10);
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
