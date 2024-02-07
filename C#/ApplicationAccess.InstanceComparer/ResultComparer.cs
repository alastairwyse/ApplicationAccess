/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.InstanceComparer
{
    /// <summary>
    /// Compares results returned by methods in the Access Manager instances being compared.
    /// </summary>
    class ResultComparer
    {
        /// <summary>Whether the comparer should throw an exception when different results are encountered.</summary>
        protected Boolean throwExceptionOnDifference;
        /// <summary>Logger.</summary>
        protected IApplicationLogger logger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.InstanceComparer.ResultComparer class.
        /// </summary>
        /// <param name="throwExceptionOnDifference">Whether the comparer should throw an exception when different results are encountered.</param>
        /// <param name="logger">Logger instance.</param>
        public ResultComparer(Boolean throwExceptionOnDifference, IApplicationLogger logger)
        {
            this.throwExceptionOnDifference = throwExceptionOnDifference;
            this.logger = logger;
        }

        /// <summary>
        /// Compares the specified method results.
        /// </summary>
        /// <param name="sourceResult">The result from the source Access Manager instance.</param>
        /// <param name="targetResult">The result from the target Access Manager instance.</param>
        public void Compare(Boolean sourceResult, Boolean targetResult)
        {
            if (sourceResult != targetResult)
            {
                String message = $"Encountered different {typeof(Boolean).Name} results.  Source result was '{sourceResult}', target result was '{targetResult}'.";
                LogAndThrowExceptionIfThrowExceptionOnDifferenceIsTrue(message);
            }
        }

        /// <summary>
        /// Compares the specified method results.
        /// </summary>
        /// <param name="sourceResult">The result from the source Access Manager instance.</param>
        /// <param name="targetResult">The result from the target Access Manager instance.</param>
        public void Compare(HashSet<String> sourceResult, HashSet<String> targetResult)
        {
            CompareHashsets(sourceResult, targetResult);
        }

        /// <summary>
        /// Compares the specified method results.
        /// </summary>
        /// <param name="sourceResult">The result from the source Access Manager instance.</param>
        /// <param name="targetResult">The result from the target Access Manager instance.</param>
        public void Compare(HashSet<Tuple<String, String>> sourceResult, HashSet<Tuple<String, String>> targetResult)
        {
            CompareHashsets(sourceResult, targetResult);
        }

        /// <summary>
        /// Compares the specified method results.
        /// </summary>
        /// <param name="sourceResult">The result from the source Access Manager instance.</param>
        /// <param name="targetResult">The result from the target Access Manager instance.</param>
        public void Compare(IEnumerable<String> sourceResult, IEnumerable<String> targetResult)
        {
            CompareHashsets(new HashSet<String>(sourceResult), new HashSet<String>(targetResult));
        }

        /// <summary>
        /// Compares the specified method results.
        /// </summary>
        /// <param name="sourceResult">The result from the source Access Manager instance.</param>
        /// <param name="targetResult">The result from the target Access Manager instance.</param>
        public void Compare(IEnumerable<Tuple<String, String>> sourceResult, IEnumerable<Tuple<String, String>> targetResult)
        {
            CompareHashsets(new HashSet<Tuple<String, String>>(sourceResult), new HashSet<Tuple<String, String>>(targetResult));
        }

        /// <summary>
        /// Compares the specified HashSets.
        /// </summary>
        /// <typeparam name="T">The type of object held in the HashSet.</typeparam>
        /// <param name="sourceResult">The HashSet obtained from the source Access Manager instance method result.</param>
        /// <param name="targetResult">The HashSet obtained from the target Access Manager instance method result.</param>
        protected void CompareHashsets<T>(HashSet<T> sourceResult, HashSet<T> targetResult)
        {
            if (sourceResult.Count != targetResult.Count)
            {
                String message = GenerateSetCountsNotEqualMessage(sourceResult.Count, targetResult.Count);
                LogAndThrowExceptionIfThrowExceptionOnDifferenceIsTrue(message);
            }
            foreach (T sourceString in sourceResult)
            {
                if (targetResult.Contains(sourceString) == false)
                {
                    String message = GenerateTargetResultDidntContainItemMessage(sourceString);
                    LogAndThrowExceptionIfThrowExceptionOnDifferenceIsTrue(message);
                }
            }
        }        

        protected void LogAndThrowExceptionIfThrowExceptionOnDifferenceIsTrue(String message)
        {
            logger.Log(LogLevel.Error, message);
            if (throwExceptionOnDifference == true)
                throw new Exception(message);
        }

        protected String GenerateSetCountsNotEqualMessage(Int32 sourceResultCount, Int32 targetResultCount)
        {
            return $"Set results contained different item counts.  Source count was {sourceResultCount}, target count was {targetResultCount}.";
        }

        protected String GenerateTargetResultDidntContainItemMessage(Object item)
        {
            return $"Target set result did not contain item '{item.ToString()}' which existed in source set result.";
        }
    }
}
