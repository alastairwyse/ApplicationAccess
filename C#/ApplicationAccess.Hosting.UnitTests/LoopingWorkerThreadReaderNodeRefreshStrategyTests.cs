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
        private LoopingWorkerThreadReaderNodeRefreshStrategy testLoopingWorkerThreadReaderNodeRefreshStrategy;

        [SetUp]
        protected void SetUp()
        {
            testLoopingWorkerThreadReaderNodeRefreshStrategy = new LoopingWorkerThreadReaderNodeRefreshStrategy(250);
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

            Assert.That(e.Message, Does.StartWith("Exception occurred on reader node refreshing worker thread at "));
            Assert.AreEqual(e.InnerException, mockException);
        }

        [Test]
        public void Stop_ExceptionOccurredOnWorkerThread()
        {
            var mockException = new Exception("Worker thread refresh exception.");
            testLoopingWorkerThreadReaderNodeRefreshStrategy.ReaderNodeRefreshed += (Object sender, EventArgs e) => { throw mockException; };
            testLoopingWorkerThreadReaderNodeRefreshStrategy.Start();
            // Wait for the worker thread loop iterval to elapse
            Thread.Sleep(500);

            var e = Assert.Throws<ReaderNodeRefreshException>(delegate
            {
                testLoopingWorkerThreadReaderNodeRefreshStrategy.Stop();
            });

            Assert.That(e.Message, Does.StartWith("Exception occurred on reader node refreshing worker thread at "));
            Assert.AreEqual(e.InnerException, mockException);
        }
    }
}
