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
        /// <summary>The <see cref="Stopwatch.Frequency"/> property value.</summary>
        protected Int64 stopwatchFrequency;
        /// <summary>The timestamp at which the Start() method was called.</summary>
        protected DateTime startTime;
        /// <summary>Holds the times of previous operation initiations.  The <see cref="LinkedList{T}.Last">Last</see> property holds the time of the most recently initiated.</summary>
        protected LinkedList<DateTime> previousInitiationTimeWindow;
        /// <summary>The target number of operations per second to trigger.</summary>
        protected Double targetOperationsPerSecond;
        /// <summary>The number of items to keep in member 'previousInitiationTimeWindow'.</summary>
        protected Int32 previousInitiationTimeWindowSize;
        /// <summary>The number of times per operation iteration that the actual 'operations per second' value should be printed to the console.  Set to 0 to not print.</summary>
        protected Int32 operationsPerSecondPrintFrequency;
        /// <summary>The number of operations generated.</summary>
        protected Int64 generationCounter;
        /// <summary>The total time waited between operation triggers.</summary>
        protected Double totalWaitTime;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.OperationTriggerer class.
        /// </summary>
        /// <param name="targetOperationsPerSecond">The target number of operations per second to trigger.</param>
        /// <param name="previousInitiationTimeWindowSize">The number of items to keep in member 'previousInitiationTimeWindow'.</param>
        /// <param name="operationsPerSecondPrintFrequency">The number of times per operation iteration that the actual 'operations per second' value should be printed to the console.  Set to 0 to not print.</param>
        /// <param name="id">An optional unique id for this OperationTriggerer instance.</param>
        public OperationTriggerer(Double targetOperationsPerSecond, Int32 previousInitiationTimeWindowSize, Int32 operationsPerSecondPrintFrequency, String id = "")
            : base()
        {
            if (targetOperationsPerSecond < 0.0)
                throw new ArgumentOutOfRangeException(nameof(targetOperationsPerSecond), $"Parameter '{nameof(targetOperationsPerSecond)}' with value {targetOperationsPerSecond} cannot be less than or equal to 0.");
            if (previousInitiationTimeWindowSize < 10)
                throw new ArgumentOutOfRangeException(nameof(previousInitiationTimeWindowSize), $"Parameter '{nameof(previousInitiationTimeWindowSize)}' with value {previousInitiationTimeWindowSize} cannot be less than 10.");
            if (operationsPerSecondPrintFrequency < 0)
                throw new ArgumentOutOfRangeException(nameof(operationsPerSecondPrintFrequency), $"Parameter '{nameof(operationsPerSecondPrintFrequency)}' with value {operationsPerSecondPrintFrequency} cannot be less than 0.");

            stopwatch = new Stopwatch();
            stopwatchFrequency = Stopwatch.Frequency;
            previousInitiationTimeWindow = new LinkedList<DateTime>();
            this.targetOperationsPerSecond = targetOperationsPerSecond;
            this.previousInitiationTimeWindowSize = previousInitiationTimeWindowSize;
            this.operationsPerSecondPrintFrequency = operationsPerSecondPrintFrequency;
            generationCounter = 0;
            totalWaitTime = 0.0;
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

        public void NotifyOperationInitiated()
        {
            DateTime now = GetStopWatchUtcNow();
            lock (previousInitiationTimeWindow)
            {
                previousInitiationTimeWindow.AddLast(now);
                while (previousInitiationTimeWindow.Count > previousInitiationTimeWindowSize)
                {
                    previousInitiationTimeWindow.RemoveFirst();
                }
                generationCounter++;

                if (operationsPerSecondPrintFrequency > 0)
                {
                    if (generationCounter % operationsPerSecondPrintFrequency == 0)
                    {
                        Double timeWindowTotalLength = (previousInitiationTimeWindow.Last.Value - previousInitiationTimeWindow.First.Value).TotalMilliseconds;
                        Double averageOperationInterval = timeWindowTotalLength / Convert.ToDouble(previousInitiationTimeWindow.Count - 1);
                        Double actualOperationsPerSecond = 1000.0 / averageOperationInterval;
                        Console.WriteLine($"{workerThreadName}: operations per second: {actualOperationsPerSecond}");
                    }
                }
            }
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

            Int32 returnWaitTime = 0;
            if (waitTime > 0.0)
            {
                returnWaitTime = Convert.ToInt32(Math.Round(waitTime));
            }
            if (operationsPerSecondPrintFrequency > 0)
            {
                totalWaitTime += Math.Max(waitTime, 0.0);
                if (generationCounter % operationsPerSecondPrintFrequency == 0)
                {
                    Double averageWaitTime = totalWaitTime / Convert.ToDouble(generationCounter);
                    Console.WriteLine($"{workerThreadName}: average wait between operation triggers: {averageWaitTime}");
                }
            }

            return returnWaitTime;
        }

        protected DateTime GetStopWatchUtcNow()
        {
            if (stopwatchFrequency == 10000000)
            {
                return startTime.AddTicks(stopwatch.ElapsedTicks);
            }
            else
            {
                Double stopWatchTicksPerDateTimeTick = 10000000.0 / Convert.ToDouble(stopwatchFrequency);
                Double elapsedDateTimeTicksDouble = stopWatchTicksPerDateTimeTick * Convert.ToDouble(stopwatch.ElapsedTicks);
                Int64 elapsedDateTimeTicks;
                try
                {
                    // Would like to not prevent overflow with a try/catch, but can't find any better way to do this
                    //   Chance should be extremely low of ever hitting the catch block... time since starting the stopwatch would have to be > 29,000 years
                    elapsedDateTimeTicks = Convert.ToInt64(elapsedDateTimeTicksDouble);
                }
                catch (OverflowException)
                {
                    elapsedDateTimeTicks = Int64.MaxValue;
                }

                return startTime.AddTicks(elapsedDateTimeTicks);
            }
        }

        #endregion
    }
}
