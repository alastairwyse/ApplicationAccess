﻿/*
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
using System.Collections.Generic;
using ApplicationLogging;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Handles any exceptions generated by the <see cref="TestHarness{TUser, TGroup, TComponent, TAccess}"/> class, logging exceptions rather than re-throwing if the rate of exceptions stays below a specified threshold.
    /// </summary>
    public class ExceptionHandler
    {
        /// <summary>The set of known exceptions that an <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> instance can generate... e.g. for incorrect method parameters.</summary>
        protected HashSet<Type> knownAccessManagerExceptions;
        /// <summary>Holds the times of previous exception occurrences.  The <see cref="LinkedList{T}.Last">Last</see> property holds the time of the most recent.</summary>
        protected LinkedList<DateTime> previousExceptionOccurenceTimeWindow;
        /// <summary>The threshold for the allowed number of exceptions per second.</summary>
        protected Double exceptionsPerSecondThreshold;
        /// <summary>The number of items to keep in member 'previousExceptionOccurenceTimeWindow'.</summary>
        protected Int32 previousExceptionOccurenceTimeWindowSize;
        /// <summary>Whether to ignore exceptions which are known to be generated by the <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/>, e.g. <see cref="ArgumentException">ArgumentExceptions</see>.</summary>
        protected Boolean ignoreKnownAccessManagerExceptions;
        /// <summary>The logger to use for any handled exceptions.</summary>
        protected IApplicationLogger logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.ExceptionHandler class.
        /// </summary>
        /// <param name="exceptionsPerSecondThreshold">The threshold for the allowed number of exceptions per second.</param>
        /// <param name="previousExceptionOccurenceTimeWindowSize">The number of previous exception occurence timestamps to keep, in order to calculate the number of exceptions occurring per second.</param>
        /// <param name="ignoreKnownAccessManagerExceptions">Whether to ignore exceptions which are known to be generated by the <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/>, e.g. <see cref="ArgumentException">ArgumentExceptions</see>.</param>
        /// <param name="logger">The logger to use for any handled exceptions.</param>
        public ExceptionHandler(Double exceptionsPerSecondThreshold, Int32 previousExceptionOccurenceTimeWindowSize, Boolean ignoreKnownAccessManagerExceptions, IApplicationLogger logger)
        {
            if (exceptionsPerSecondThreshold <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(exceptionsPerSecondThreshold), $"Parameter '{nameof(exceptionsPerSecondThreshold)}' with value {exceptionsPerSecondThreshold} cannot be less than or equal to 0.");
            if (previousExceptionOccurenceTimeWindowSize < 10)
                throw new ArgumentOutOfRangeException(nameof(previousExceptionOccurenceTimeWindowSize), $"Parameter '{nameof(previousExceptionOccurenceTimeWindowSize)}' with value {previousExceptionOccurenceTimeWindowSize} cannot be less than 10.");

            knownAccessManagerExceptions = new HashSet<Type>() { typeof(ArgumentException) };
            previousExceptionOccurenceTimeWindow = new LinkedList<DateTime>();
            this.exceptionsPerSecondThreshold = exceptionsPerSecondThreshold;
            this.previousExceptionOccurenceTimeWindowSize = previousExceptionOccurenceTimeWindowSize;
            this.ignoreKnownAccessManagerExceptions = ignoreKnownAccessManagerExceptions;
            this.logger = logger;
        }

        public void HandleException(Exception inputException)
        {
            if (knownAccessManagerExceptions.Contains(inputException.GetType()) == false || ignoreKnownAccessManagerExceptions == false)
            {
                previousExceptionOccurenceTimeWindow.AddLast(DateTime.UtcNow);
                while (previousExceptionOccurenceTimeWindow.Count > previousExceptionOccurenceTimeWindowSize)
                {
                    previousExceptionOccurenceTimeWindow.RemoveFirst();
                }
                if (previousExceptionOccurenceTimeWindow.Count >= 2)
                {
                    Double timeWindowTotalLength = (previousExceptionOccurenceTimeWindow.Last.Value - previousExceptionOccurenceTimeWindow.First.Value).TotalMilliseconds;
                    Double averageOperationInterval = timeWindowTotalLength / Convert.ToDouble(previousExceptionOccurenceTimeWindow.Count - 1);
                    Double exceptionsPerSecond = 1000.0 / averageOperationInterval;
                    if (exceptionsPerSecond > exceptionsPerSecondThreshold)
                    {
                        String exceptionMessage = $"{this.GetType().Name} '{nameof(exceptionsPerSecond)}' threshold exceeded.";
                        logger.Log(LogLevel.Critical, exceptionMessage, inputException);
                        throw new Exception(exceptionMessage, inputException);
                    }
                    else
                    {
                        logger.Log(LogLevel.Information, "Exception occurred", inputException);
                    }
                }
            }
        }
    }
}
