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
using ApplicationMetrics;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Implementation of <see cref="IMetricLogger"/> which wraps another IMetricLogger using the decorator pattern, and only logs specified types of metrics.
    /// </summary>
    public class MetricLoggerFilter: IMetricLogger
    {
        /// <summary>The <see cref="IMetricLogger"/> which is wrapped by the filter.</summary>
        protected IMetricLogger wrappedMetricLogger;
        /// <summary>Whether or not to log instances of <see cref="CountMetric">CountMetrics</see>.</summary>
        protected Boolean logCountMetrics;
        /// <summary>Whether or not to log instances of <see cref="AmountMetric">AmountMetrics</see>.</summary>
        protected Boolean logAmounttMetrics;
        /// <summary>Whether or not to log instances of <see cref="StatusMetric">StatusMetrics</see>.</summary>
        protected Boolean logStatustMetrics;
        /// <summary>Whether or not to log instances of <see cref="IntervalMetric">IntervalMetrics</see>.</summary>
        protected Boolean logIntervalMetrics;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.MetricLoggerFilter class.
        /// </summary>
        /// <param name="wrappedMetricLogger">The <see cref="IMetricLogger"/> which is wrapped by the filter.</param>
        /// <param name="logCountMetrics">Whether or not to log instances of <see cref="CountMetric">CountMetrics</see>.</param>
        /// <param name="logAmounttMetrics">Whether or not to log instances of <see cref="AmountMetric">AmountMetrics</see>.</param>
        /// <param name="logStatustMetrics">Whether or not to log instances of <see cref="StatusMetric">StatusMetrics</see>.</param>
        /// <param name="logIntervalMetrics">Whether or not to log instances of <see cref="IntervalMetric">IntervalMetrics</see>.</param>
        public MetricLoggerFilter(IMetricLogger wrappedMetricLogger, Boolean logCountMetrics, Boolean logAmounttMetrics, Boolean logStatustMetrics, Boolean logIntervalMetrics)
        {
            this.wrappedMetricLogger = wrappedMetricLogger;
            this.logCountMetrics = logCountMetrics;
            this.logAmounttMetrics = logAmounttMetrics;
            this.logStatustMetrics = logStatustMetrics;
            this.logIntervalMetrics = logIntervalMetrics;
        }

        /// <inheritdoc/>
        public void Increment(CountMetric countMetric)
        {
            if (logCountMetrics == true)
            {
                wrappedMetricLogger.Increment(countMetric);
            }
        }

        /// <inheritdoc/>
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationMetrics.IMetricLogger.Add(ApplicationMetrics.AmountMetric,System.Int64)"]/*'/>
        public void Add(AmountMetric amountMetric, long amount)
        {
            if (logAmounttMetrics == true)
            {
                wrappedMetricLogger.Add(amountMetric, amount);
            }
        }

        /// <inheritdoc/>
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationMetrics.IMetricLogger.Set(ApplicationMetrics.StatusMetric,System.Int64)"]/*'/>
        public void Set(StatusMetric statusMetric, long value)
        {
            if (logStatustMetrics == true)
            {
                wrappedMetricLogger.Set(statusMetric, value);
            }
        }

        /// <inheritdoc/>
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationMetrics.IMetricLogger.Begin(ApplicationMetrics.IntervalMetric)"]/*'/>
        public Guid Begin(IntervalMetric intervalMetric)
        {
            if (logIntervalMetrics == true)
            {
                return wrappedMetricLogger.Begin(intervalMetric);
            }
            else
            {
                return Guid.NewGuid();
            }
        }

        /// <inheritdoc/>
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationMetrics.IMetricLogger.End(ApplicationMetrics.IntervalMetric)"]/*'/>
        public void End(IntervalMetric intervalMetric)
        {
            if (logIntervalMetrics == true)
            {
                wrappedMetricLogger.End(intervalMetric);
            }
        }

        /// <inheritdoc/>
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationMetrics.IMetricLogger.End(System.Guid,ApplicationMetrics.IntervalMetric)"]/*'/>
        public void End(Guid beginId, IntervalMetric intervalMetric)
        {
            if (logIntervalMetrics == true)
            {
                wrappedMetricLogger.End(beginId, intervalMetric);
            }
        }

        /// <inheritdoc/>
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationMetrics.IMetricLogger.CancelBegin(ApplicationMetrics.IntervalMetric)"]/*'/>
        public void CancelBegin(IntervalMetric intervalMetric)
        {
            if (logIntervalMetrics == true)
            {
                wrappedMetricLogger.CancelBegin(intervalMetric);
            }
        }

        /// <inheritdoc/>
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationMetrics.IMetricLogger.CancelBegin(System.Guid,ApplicationMetrics.IntervalMetric)"]/*'/>
        public void CancelBegin(Guid beginId, IntervalMetric intervalMetric)
        {
            if (logIntervalMetrics == true)
            {
                wrappedMetricLogger.CancelBegin(beginId, intervalMetric);
            }
        }
    }
}
