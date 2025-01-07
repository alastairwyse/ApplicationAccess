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
using System.Threading;
using System.Threading.Tasks;
using NUnit.Framework;

namespace ApplicationAccess.Utilities.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Utilities.DefaultThreadPauser class.
    /// </summary>
    public class DefaultThreadPauserTests
    {
        private DefaultThreadPauser testThreadPauser;

        [SetUp]
        protected void SetUp()
        {
            testThreadPauser = new DefaultThreadPauser();
        }

        [TearDown]
        protected void TearDown()
        {
            testThreadPauser.Dispose();
        }

        [Test]
        public void Constructor_InitializesInUnpausedState()
        {
            using (var signal = new ManualResetEvent(false))
            {
                Task.Run(() =>
                {
                    testThreadPauser.TestPaused();
                    signal.Set();
                });

                // If the DefaultThreadPauser initializes paused, this statement will block forever
                signal.WaitOne();
            }
        }

        [Test]
        public void PauseUnpause_MultiplePausedThreads()
        {
            var eventSequence = new List<String>();
            using (var countdownEvent = new CountdownEvent(3))
            {
                testThreadPauser.Pause();
                Task.Run(() =>
                {
                    testThreadPauser.TestPaused();
                    eventSequence.Add("Thread 1 Unpaused");
                    countdownEvent.Signal();
                });
                Task.Run(() =>
                {
                    testThreadPauser.TestPaused();
                    eventSequence.Add("Thread 2 Unpaused");
                    countdownEvent.Signal();
                });
                Task.Run(() =>
                {
                    testThreadPauser.TestPaused();
                    eventSequence.Add("Thread 3 Unpaused");
                    countdownEvent.Signal();
                });

                Thread.Sleep(1000);
                eventSequence.Add("Calling Unpause()");
                testThreadPauser.Unpause();
                countdownEvent.Wait();
            }

            Assert.AreEqual(4, eventSequence.Count);
            Assert.AreEqual("Calling Unpause()", eventSequence[0]);
            Assert.IsTrue(eventSequence.Contains("Thread 1 Unpaused"));
            Assert.IsTrue(eventSequence.Contains("Thread 2 Unpaused"));
            Assert.IsTrue(eventSequence.Contains("Thread 3 Unpaused"));
        }
    }
}
