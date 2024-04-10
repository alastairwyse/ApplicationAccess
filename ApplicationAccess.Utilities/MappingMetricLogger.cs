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
using System.Collections.Generic;
using System.Text;
using ApplicationMetrics;

namespace ApplicationAccess.Utilities
{
    /// <summary>
    /// Implementation of IMetricLogger which fronts another IMetricLogger using a facade pattern, and maps metrics from one type to another.
    /// </summary>
    public class MappingMetricLogger : IMetricLogger
    {
        /// <summary>Dictionary which maps count metrics from one type to another.</summary>
        protected Dictionary<Type, CountMetric> countMetricMap;
        /// <summary>Dictionary which maps count amount from one type to another.</summary>
        protected Dictionary<Type, AmountMetric> amountMetricMap;
        /// <summary>Dictionary which maps count status from one type to another.</summary>
        protected Dictionary<Type, StatusMetric> statusMetricMap;
        /// <summary>Dictionary which maps count interval from one type to another.</summary>
        protected Dictionary<Type, IntervalMetric> intervalMetricMap;
        /// <summary>The IMetricLogger to pass the metrics to after mapping.</summary>
        protected IMetricLogger downstreamMetricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Utilities.MappingMetricLogger class.
        /// </summary>
        /// <param name="downstreamMetricLogger">The IMetricLogger to pass the metrics to after mapping.</param>
        public MappingMetricLogger(IMetricLogger downstreamMetricLogger)
        {
            countMetricMap = new Dictionary<Type, CountMetric>();
            amountMetricMap = new Dictionary<Type, AmountMetric>();
            statusMetricMap = new Dictionary<Type, StatusMetric>();
            intervalMetricMap = new Dictionary<Type, IntervalMetric>();
            this.downstreamMetricLogger = downstreamMetricLogger;
        }

        /// <summary>
        /// Adds a mapping for a count metric.
        /// </summary>
        /// <param name="countMetricType">The type (assignable to CountMetric) to map from.</param>
        /// <param name="mappedMetric">The count metric to map to.</param>
        public void AddCountMetricMapping(Type countMetricType, CountMetric mappedMetric)
        {
            if (typeof(CountMetric).IsAssignableFrom(countMetricType) == false)
                throw new ArgumentException($"Type '{countMetricType.FullName}' in parameter '{nameof(countMetricType)}' is not assignable to '{typeof(CountMetric).FullName}'.", nameof(countMetricType));

            countMetricMap.Add(countMetricType, mappedMetric);
        }

        /// <summary>
        /// Adds a mapping for an amount metric.
        /// </summary>
        /// <param name="amountMetricType">The type (assignable to AmountMetric) to map from.</param>
        /// <param name="mappedMetric">The amount metric to map to.</param>
        public void AddAmountMetricMapping(Type amountMetricType, AmountMetric mappedMetric)
        {
            if (typeof(AmountMetric).IsAssignableFrom(amountMetricType) == false)
                throw new ArgumentException($"Type '{amountMetricType.FullName}' in parameter '{nameof(amountMetricType)}' is not assignable to '{typeof(AmountMetric).FullName}'.", nameof(amountMetricType));

            amountMetricMap.Add(amountMetricType, mappedMetric);
        }

        /// <summary>
        /// Adds a mapping for a status metric.
        /// </summary>
        /// <param name="statusMetricType">The type (assignable to StatusMetric) to map from.</param>
        /// <param name="mappedMetric">The status metric to map to.</param>
        public void AddStatusMetricMapping(Type statusMetricType, StatusMetric mappedMetric)
        {
            if (typeof(StatusMetric).IsAssignableFrom(statusMetricType) == false)
                throw new ArgumentException($"Type '{statusMetricType.FullName}' in parameter '{nameof(statusMetricType)}' is not assignable to '{typeof(StatusMetric).FullName}'.", nameof(statusMetricType));

            statusMetricMap.Add(statusMetricType, mappedMetric);
        }

        /// <summary>
        /// Adds a mapping for an interval metric.
        /// </summary>
        /// <param name="intervalMetricType">The type (assignable to IntervalMetric) to map from.</param>
        /// <param name="mappedMetric">The interval metric to map to.</param>
        public void AddIntervalMetricMapping(Type intervalMetricType, IntervalMetric mappedMetric)
        {
            if (typeof(IntervalMetric).IsAssignableFrom(intervalMetricType) == false)
                throw new ArgumentException($"Type '{intervalMetricType.FullName}' in parameter '{nameof(intervalMetricType)}' is not assignable to '{typeof(IntervalMetric).FullName}'.", nameof(intervalMetricType));

            intervalMetricMap.Add(intervalMetricType, mappedMetric);
        }

        /// <summary>
        /// Records a single instance of the specified count event.
        /// </summary>
        /// <param name="countMetric">The count metric that occurred.</param>
        public void Increment(CountMetric countMetric)
        {
            ThrowExceptionIfMetricMapDoesntContainType(countMetric.GetType(), nameof(countMetric), countMetricMap);

            downstreamMetricLogger.Increment(countMetricMap[countMetric.GetType()]);
        }

        /// <summary>
        /// Records an instance of the specified amount metric event, and the associated amount.
        /// </summary>
        /// <param name="amountMetric">The amount metric that occurred.</param>
        /// <param name="amount">The amount associated with the instance of the amount metric.</param>
        public void Add(AmountMetric amountMetric, long amount)
        {
            ThrowExceptionIfMetricMapDoesntContainType(amountMetric.GetType(), nameof(amountMetric), amountMetricMap);

            downstreamMetricLogger.Add(amountMetricMap[amountMetric.GetType()], amount);
        }

        /// <summary>
        /// Records an instance of the specified status metric event, and the associated value.
        /// </summary>
        /// <param name="statusMetric">The status metric that occurred.</param>
        /// <param name="value">The value associated with the instance of the status metric.</param>
        public void Set(StatusMetric statusMetric, long value)
        {
            ThrowExceptionIfMetricMapDoesntContainType(statusMetric.GetType(), nameof(statusMetric), statusMetricMap);

            downstreamMetricLogger.Set(statusMetricMap[statusMetric.GetType()], value);
        }

        /// <summary>
        /// Records the starting of the specified interval metric event.
        /// </summary>
        /// <param name="intervalMetric">The interval metric that started.</param>
        /// <returns>A unique id for the starting of the interval metric, which should be subsequently passed to the <see cref="IMetricLogger.End(System.Guid,ApplicationMetrics.IntervalMetric)"/> or <see cref="IMetricLogger.CancelBegin(System.Guid,ApplicationMetrics.IntervalMetric)"/> methods, when using the class in interleaved mode.</returns>
        public Guid Begin(IntervalMetric intervalMetric)
        {
            ThrowExceptionIfMetricMapDoesntContainType(intervalMetric.GetType(), nameof(intervalMetric), intervalMetricMap);

            return downstreamMetricLogger.Begin(intervalMetricMap[intervalMetric.GetType()]);
        }

        /// <summary>
        /// Records the completion of the specified interval metric event when using the class in non-interleaved mode.
        /// </summary>
        /// <param name="intervalMetric">The interval metric that completed.</param>
        public void End(IntervalMetric intervalMetric)
        {
            ThrowExceptionIfMetricMapDoesntContainType(intervalMetric.GetType(), nameof(intervalMetric), intervalMetricMap);

            downstreamMetricLogger.End(intervalMetricMap[intervalMetric.GetType()]);
        }

        /// <summary>
        /// Records the completion of the specified interval metric event when using the class in interleaved mode.
        /// </summary>
        /// <param name="beginId">The id corresponding to the starting of the specified interval metric event (i.e. returned when the <see cref="IMetricLogger.Begin(ApplicationMetrics.IntervalMetric)"/> method was called).</param>
        /// <param name="intervalMetric">The interval metric that completed.</param>
        public void End(Guid beginId, IntervalMetric intervalMetric)
        {
            ThrowExceptionIfMetricMapDoesntContainType(intervalMetric.GetType(), nameof(intervalMetric), intervalMetricMap);

            downstreamMetricLogger.End(beginId, intervalMetricMap[intervalMetric.GetType()]);
        }

        /// <summary>
        /// Cancels the starting of the specified interval metric event when using the class in non-interleaved mode (e.g. in the case that an exception occurs between the starting and completion of the event).
        /// </summary>
        /// <param name="intervalMetric">The interval metric that should be cancelled.</param>
        public void CancelBegin(IntervalMetric intervalMetric)
        {
            ThrowExceptionIfMetricMapDoesntContainType(intervalMetric.GetType(), nameof(intervalMetric), intervalMetricMap);

            downstreamMetricLogger.CancelBegin(intervalMetricMap[intervalMetric.GetType()]);
        }

        /// <summary>
        /// Cancels the starting of the specified interval metric event when using the class in interleaved mode (e.g. in the case that an exception occurs between the starting and completion of the event).
        /// </summary>
        /// <param name="beginId">The id corresponding to the starting of the specified interval metric event (i.e. returned when the <see cref="IMetricLogger.Begin(ApplicationMetrics.IntervalMetric)"/> method was called).</param>
        /// <param name="intervalMetric">The interval metric that should be cancelled.</param>
        public void CancelBegin(Guid beginId, IntervalMetric intervalMetric)
        {
            ThrowExceptionIfMetricMapDoesntContainType(intervalMetric.GetType(), nameof(intervalMetric), intervalMetricMap);

            downstreamMetricLogger.CancelBegin(beginId, intervalMetricMap[intervalMetric.GetType()]);
        }

        #region Private/Protected Methods

        #pragma warning disable 1591

        protected void ThrowExceptionIfMetricMapDoesntContainType<TMapToMetric>(Type mapFromType, string mapFromTypeParameterName, Dictionary<Type, TMapToMetric> metricMap)
        {
            if (metricMap.ContainsKey(mapFromType) == false)
                throw new ArgumentException($"No mapping exists for metric of type '{mapFromType.FullName}' in parameter '{mapFromTypeParameterName}'.", mapFromTypeParameterName);
        }

        #pragma warning restore 1591

        #endregion
    }
}
