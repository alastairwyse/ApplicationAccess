/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Diagnostics;
using System.Threading;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Signals another instance (counterpart instance) of <see cref="TwoWaySignallingParticipantBase"/> when the next test operation should be performed against an AccessManager under test.
    /// </summary>
    public class OperationTriggerer : TwoWaySignallingParticipantBase
    {
        /// <summary>Used to accurately measure frequency of operations triggered.</summary>
        protected Stopwatch stopwatch;
        /// <summary>The timestamp at which the Start() method was called.</summary>
        protected DateTime startTime;
        /// <summary>Holds the times of previous operation initiations.  The <see cref="LinkedList{T}.Last">Last</see> property holds the time of the most recently initiated.</summary>
        protected LinkedList<DateTime> previousInitiationTimeWindow;
        /// <summary>The target number of operations per second to trigger.</summary>
        protected Double targetOperationsPerSecond;
        /// <summary>The number of items to keep in member 'previousInitiationTimeWindow'.</summary>
        protected Int32 previousInitiationTimeWindowSize;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.OperationTriggerer class.
        /// </summary>
        /// <param name="targetOperationsPerSecond">The target number of operations per second to trigger.</param>
        /// <param name="previousInitiationTimeWindowSize">The number of items to keep in member 'previousInitiationTimeWindow'.</param>
        /// <param name="id">An optional unique id for this OperationTriggerer instance.</param>
        public OperationTriggerer(Double targetOperationsPerSecond, Int32 previousInitiationTimeWindowSize, String id = "")
            : base()
        {
            if (targetOperationsPerSecond < 0.0)
                throw new ArgumentOutOfRangeException(nameof(targetOperationsPerSecond), $"Parameter '{nameof(targetOperationsPerSecond)}' with value {targetOperationsPerSecond} cannot be less than or equal to 0.");
            if (previousInitiationTimeWindowSize < 0)
                throw new ArgumentOutOfRangeException(nameof(previousInitiationTimeWindowSize), $"Parameter '{nameof(previousInitiationTimeWindowSize)}' with value {previousInitiationTimeWindowSize} cannot be less than 0.");


            stopwatch = new Stopwatch();
            previousInitiationTimeWindow = new LinkedList<DateTime>();
            this.targetOperationsPerSecond = targetOperationsPerSecond;
            this.previousInitiationTimeWindowSize = previousInitiationTimeWindowSize;
            base.workerThreadName = $"{this.GetType().FullName} worker thread";
            if (String.IsNullOrEmpty(id) == false)
            {
                workerThreadName = workerThreadName + $" id: {id}";
            }
        }

        /// <inheritdoc/>
        public override void Start()
        {
            workerThreadIterationAction = () =>
            {
                Int32 waitTime = CalculateWaitTimeForNextOperation();
                if (waitTime > 0)
                {
                    Thread.Sleep(waitTime);
                }
            };
            stopwatch.Reset();
            stopwatch.Start();
            startTime = DateTime.UtcNow;
            base.Start();
        }

        #region Private/Protected Methods

        /// <summary>
        /// Calculates the wait time for the next operation in milliseconds.
        /// </summary>
        protected Int32 CalculateWaitTimeForNextOperation()
        {
            if (targetOperationsPerSecond == 0.0)
            {
                return 0;
            }

            Double waitTime = 1000.0 / targetOperationsPerSecond;
            lock (previousInitiationTimeWindow)
            {
                if (previousInitiationTimeWindow.Count > 1)
                {
                    Double timeWindowTotalLength = (previousInitiationTimeWindow.Last.Value - previousInitiationTimeWindow.First.Value).TotalMilliseconds;
                    Double averageOperationInterval = timeWindowTotalLength / Convert.ToDouble(previousInitiationTimeWindow.Count - 1);
                    Double actualOperationsPerSecond = 1000.0 / averageOperationInterval;
                    Double accuracyRatio = actualOperationsPerSecond / targetOperationsPerSecond;
                    // Compensation factor doubles the difference between the accuracy ratio and 1 (to try and move more quickly back to the target rate)
                    Double compensationFactor = accuracyRatio - (1.0 - accuracyRatio);
                    // Multiple the base wait time be the compensation factor to try to get closer to the target rate
                    waitTime = waitTime * compensationFactor;
                }
            }

            if (waitTime < 0.0)
            {
                return 0;
            }
            else
            {
                return Convert.ToInt32(Math.Round(waitTime));
            }
        }

        protected DateTime GetStopWatchUtcNow()
        {
            return startTime.AddTicks(stopwatch.ElapsedTicks);
        }

        #endregion
    }
}
