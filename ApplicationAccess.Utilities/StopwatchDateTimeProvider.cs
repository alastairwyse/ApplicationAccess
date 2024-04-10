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
using System.Diagnostics;

namespace ApplicationAccess.Utilities
{
    /// <summary>
    /// Returns the current date and time using an underlying Stopwatch class for increased accuracy over the DefaultDateTimeProvider class.
    /// </summary>
    public class StopwatchDateTimeProvider : IDateTimeProvider
    {
        /// <summary>Stopwatch to use for calculating the current date and time.</summary>
        protected Stopwatch stopwatch;
        /// <summary>The value of the 'Frequency' property of the StopWatch object.</summary>
        protected readonly Int64 stopWatchFrequency;
        /// <summary>The time at which the stopwatch was started.</summary>
        protected DateTime stopwatchStartTime;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Utilities.StopwatchDateTimeProvider class.
        /// </summary>
        public StopwatchDateTimeProvider()
        {
            stopwatch = new Stopwatch();
            stopWatchFrequency = Stopwatch.Frequency;
            // There is potential for a gap here between starting the stopwatch and populating 'stopwatchStartTime'
            //   However I think this is acceptable... the goal here is to provide high resolution and accurate timestamps (with respect to the difference between successive gets of Now/UtcNow) moreso than time time being 100% accurate to real-world GMT/UTC...
            //   Hence a small 'gap' is acceptable.
            stopwatch.Start();
            stopwatchStartTime = DateTime.UtcNow;
        }

        /// <inheritdoc/>
        public DateTime UtcNow()
        {
            // Copied this code from ApplicationMetrics.MetricLoggerBuffer.GetStopWatchUtcNow().  It's not tested here, but covered by thorough tests in ApplicationMetrics.

            Int64 elapsedDateTimeTicks;
            if (stopWatchFrequency == 10000000)
            {
                // On every system I've tested the StopWatch.Frequency property on, it's returned 10,000,000
                //   Guessing this is maybe an upper limit of the property (since there's arguably not much point in supporting a frequency greated than the DateTime.Ticks resolution which is also 10,000,000/sec)
                //   In any case, assuming the value is 10,000,000 on many systems, adding this shortcut to avoid conversion to double and overflow handling
                elapsedDateTimeTicks = stopwatch.ElapsedTicks;
            }
            else
            {
                Double stopWatchTicksPerDateTimeTick = 10000000.0 / Convert.ToDouble(stopWatchFrequency);
                Double elapsedDateTimeTicksDouble = stopWatchTicksPerDateTimeTick * Convert.ToDouble(stopwatch.ElapsedTicks);
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
            }

            if ((System.DateTime.MaxValue - stopwatchStartTime).Ticks < elapsedDateTimeTicks)
            {
                return System.DateTime.MaxValue.ToUniversalTime();
            }
            else
            {
                return stopwatchStartTime.AddTicks(elapsedDateTimeTicks);
            }
        }
    }
}
