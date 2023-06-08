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
using ApplicationMetrics;

namespace ApplicationAccess.Metrics
{
    /// <summary>
    /// Filters metrics passed to an <see cref="IMetricLogger"/> instance, by only allowing subclasses of specified metrics to be logged.  Follows the <see href="https://en.wikipedia.org/wiki/Decorator_pattern">GOF decorator pattern</see>.
    /// </summary>
    public class MetricLoggerBaseTypeInclusionFilter : IMetricLogger
    {
        /// <summary>The <see cref="IMetricLogger"/> implementation to filter.</summary>
        protected readonly IMetricLogger filteredMetricLogger;
        /// <summary>The base types of the count metrics which should be logged.</summary>
        protected readonly HashSet<Type> countMetricBaseTypes;
        /// <summary>The base types of the amount metrics which should be logged.</summary>
        protected readonly HashSet<Type> amountMetricBaseTypes;
        /// <summary>The base types of the status metrics which should be logged.</summary>
        protected readonly HashSet<Type> statusMetricBaseTypes;
        /// <summary>The base types of the interval metrics which should be logged.</summary>
        protected readonly HashSet<Type> intervalMetricBaseTypes;

        // Store caches of the types of metrics which should be logged (and not logged) at runtime to prevent having to call Type.IsAssignableFrom() on every metric logged
        /// <summary>The types of count metrics encountered which should be logged.</summary>
        protected readonly HashSet<Type> includedCountMetricTypes;
        /// <summary>The types of amount metris encountered which should be logged.</summary>
        protected readonly HashSet<Type> includedAmountMetricTypes;
        /// <summary>The types of status metrics encountered which should be logged.</summary>
        protected readonly HashSet<Type> includedStatusMetricTypes;
        /// <summary>The types of interval metrics encountered which should be logged.</summary>
        protected readonly HashSet<Type> includedIntervalMetricTypes;
        /// <summary>The types of count metrics encountered which should not be logged.</summary>
        protected readonly HashSet<Type> excludedCountMetricTypes;
        /// <summary>The types of amount metrics encountered which should not be logged.</summary>
        protected readonly HashSet<Type> excludedAmountMetricTypes;
        /// <summary>The types of status metrics encountered which should not be logged.</summary>
        protected readonly HashSet<Type> excludedStatusMetricTypes;
        /// <summary>The types of interval metrics encountered which should not be logged.</summary>
        protected readonly HashSet<Type> excludedIntervalMetricTypes;

        // Lock objects for updates to the caches
        protected readonly Object countMetricTypesLock;
        protected readonly Object amountMetricTypesLock;
        protected readonly Object statusMetricTypesLock;
        protected readonly Object intervalMetricTypesLock;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.MetricLoggerBaseTypeInclusionFilter class.
        /// </summary>
        /// <param name="filteredMetricLogger">The <see cref="IMetricLogger"/> implementation to filter.</param>
        /// <param name="countMetricBaseTypes">The base types of the count metrics which should be logged.</param>
        /// <param name="amountMetricBaseTypes">The base types of the amount metrics which should be logged.</param>
        /// <param name="statusMetricBaseTypes">The base types of the status metrics which should be logged.</param>
        /// <param name="intervalMetricBaseTypes">The base types of the interval metrics which should be logged.</param>
        public MetricLoggerBaseTypeInclusionFilter
        (
            IMetricLogger filteredMetricLogger, 
            IEnumerable<Type> countMetricBaseTypes, 
            IEnumerable<Type> amountMetricBaseTypes,
            IEnumerable<Type> statusMetricBaseTypes, 
            IEnumerable<Type> intervalMetricBaseTypes
        )
        {
            this.countMetricBaseTypes = new HashSet<Type>();
            this.amountMetricBaseTypes = new HashSet<Type>();
            this.statusMetricBaseTypes = new HashSet<Type>();
            this.intervalMetricBaseTypes = new HashSet<Type>();
            includedCountMetricTypes = new HashSet<Type>();
            includedAmountMetricTypes = new HashSet<Type>();
            includedStatusMetricTypes = new HashSet<Type>();
            includedIntervalMetricTypes = new HashSet<Type>();
            excludedCountMetricTypes = new HashSet<Type>();
            excludedAmountMetricTypes = new HashSet<Type>();
            excludedStatusMetricTypes = new HashSet<Type>();
            excludedIntervalMetricTypes = new HashSet<Type>();
            countMetricTypesLock = new object();
            amountMetricTypesLock = new object();
            statusMetricTypesLock = new object();
            intervalMetricTypesLock = new object();

            this.filteredMetricLogger = filteredMetricLogger;
            ValidateAndSetMetricBaseTypesParameter<CountMetric>(countMetricBaseTypes, this.countMetricBaseTypes, nameof(countMetricBaseTypes), "count");
            ValidateAndSetMetricBaseTypesParameter<AmountMetric>(amountMetricBaseTypes, this.amountMetricBaseTypes, nameof(amountMetricBaseTypes), "amount");
            ValidateAndSetMetricBaseTypesParameter<StatusMetric>(statusMetricBaseTypes, this.statusMetricBaseTypes, nameof(statusMetricBaseTypes), "status");
            ValidateAndSetMetricBaseTypesParameter<IntervalMetric>(intervalMetricBaseTypes, this.intervalMetricBaseTypes, nameof(intervalMetricBaseTypes), "interval");
        }

        /// <inheritdoc/>
        public void Increment(CountMetric countMetric)
        {
            /*
            Type metricType = countMetric.GetType();
            if (includedCountMetricTypes.Contains(metricType) == true)
            {
                filteredMetricLogger.Increment(countMetric);
            }
            else if (excludedCountMetricTypes.Contains(metricType) == true)
            {
                // Do nothing
            }
            else
            {
                lock (countMetricTypesLock)
                {
                    Type currentBaseType = metricType.BaseType;
                    while (currentBaseType != typeof(MetricBase))
                    {
                        if (countMetricBaseTypes.Contains(currentBaseType) == true)
                        {
                            // Need to check below again, as it wasn't checked under lock context at the top of the method
                            if (includedCountMetricTypes.Contains(metricType) == false)
                            {
                                includedCountMetricTypes.Add(metricType);
                                filteredMetricLogger.Increment(countMetric);
                                return;
                            }
                        }

                        currentBaseType = currentBaseType.BaseType;
                    }
                    if (excludedCountMetricTypes.Contains(metricType) == false)
                    {
                        excludedCountMetricTypes.Add(metricType);
                    }
                }
            }
            */

            Func<Guid> metricProcessFunction = () =>
            {
                filteredMetricLogger.Increment(countMetric);
                return Guid.Empty;
            };
            ProcessMetric(countMetric, countMetricBaseTypes, includedCountMetricTypes, excludedCountMetricTypes, countMetricTypesLock, metricProcessFunction);
        }

        /// <inheritdoc/>
        public void Add(AmountMetric amountMetric, Int64 amount)
        {
            Func<Guid> metricProcessFunction = () =>
            {
                filteredMetricLogger.Add(amountMetric, amount);
                return Guid.Empty;
            };
            ProcessMetric(amountMetric, amountMetricBaseTypes, includedAmountMetricTypes, excludedAmountMetricTypes, amountMetricTypesLock, metricProcessFunction);
        }

        /// <inheritdoc/>
        public void Set(StatusMetric statusMetric, Int64 value)
        {
            Func<Guid> metricProcessFunction = () =>
            {
                filteredMetricLogger.Set(statusMetric, value);
                return Guid.Empty;
            };
            ProcessMetric(statusMetric, statusMetricBaseTypes, includedStatusMetricTypes, excludedStatusMetricTypes, statusMetricTypesLock, metricProcessFunction);
        }

        /// <inheritdoc/>
        public Guid Begin(IntervalMetric intervalMetric)
        {
            Func<Guid> metricProcessFunction = () =>
            {
                return filteredMetricLogger.Begin(intervalMetric);
            };
            return ProcessMetric(intervalMetric, intervalMetricBaseTypes, includedIntervalMetricTypes, excludedIntervalMetricTypes, intervalMetricTypesLock, metricProcessFunction);
        }

        /// <inheritdoc/>
        public void End(IntervalMetric intervalMetric)
        {
            Func<Guid> metricProcessFunction = () =>
            {
                filteredMetricLogger.End(intervalMetric);
                return Guid.Empty;
            };
            ProcessMetric(intervalMetric, intervalMetricBaseTypes, includedIntervalMetricTypes, excludedIntervalMetricTypes, intervalMetricTypesLock, metricProcessFunction);
        }

        /// <inheritdoc/>
        public void End(Guid beginId, IntervalMetric intervalMetric)
        {
            Func<Guid> metricProcessFunction = () =>
            {
                filteredMetricLogger.End(beginId, intervalMetric);
                return Guid.Empty;
            };
            ProcessMetric(intervalMetric, intervalMetricBaseTypes, includedIntervalMetricTypes, excludedIntervalMetricTypes, intervalMetricTypesLock, metricProcessFunction);
        }

        /// <inheritdoc/>
        public void CancelBegin(IntervalMetric intervalMetric)
        {
            Func<Guid> metricProcessFunction = () =>
            {
                filteredMetricLogger.CancelBegin(intervalMetric);
                return Guid.Empty;
            };
            ProcessMetric(intervalMetric, intervalMetricBaseTypes, includedIntervalMetricTypes, excludedIntervalMetricTypes, intervalMetricTypesLock, metricProcessFunction);
        }

        /// <inheritdoc/>
        public void CancelBegin(Guid beginId, IntervalMetric intervalMetric)
        {
            Func<Guid> metricProcessFunction = () =>
            {
                filteredMetricLogger.CancelBegin(beginId, intervalMetric);
                return Guid.Empty;
            };
            ProcessMetric(intervalMetric, intervalMetricBaseTypes, includedIntervalMetricTypes, excludedIntervalMetricTypes, intervalMetricTypesLock, metricProcessFunction);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Validate the specified metric base type parameter value and sets its contents on the specified metric base type field.
        /// </summary>
        /// <typeparam name="TMetric">The type of metrics contained in the parameter and field.</typeparam>
        /// <param name="metricBaseTypesParameterValue">The value of the parameter to validate.</param>
        /// <param name="metricBaseTypesField">The field to set the validated parameter contents on.</param>
        /// <param name="metricBaseTypesParameterName">The name of the parameter to validate.</param>
        /// <param name="metricName">The human-readable name of the metric type being validated (e.g. 'count').</param>
        protected void ValidateAndSetMetricBaseTypesParameter<TMetric>(IEnumerable<Type> metricBaseTypesParameterValue, HashSet<Type> metricBaseTypesField, String metricBaseTypesParameterName, String metricName)
            where TMetric : MetricBase
        {
            foreach (Type currentMetricBaseType in metricBaseTypesParameterValue)
            {
                if (typeof(TMetric).IsAssignableFrom(currentMetricBaseType) == false)
                    throw new ArgumentException($"Parameter '{metricBaseTypesParameterName}' contains type '{currentMetricBaseType.FullName}' which is not assignable to '{typeof(TMetric).Name}'.", metricBaseTypesParameterName);
                if (metricBaseTypesField.Contains(currentMetricBaseType) == true)
                    throw new ArgumentException($"Parameter '{metricBaseTypesParameterName}' contains duplicate base {metricName} metrics of type '{currentMetricBaseType.FullName}'.", metricBaseTypesParameterName);

                metricBaseTypesField.Add(currentMetricBaseType);
            }
        }

        /// <summary>
        /// Generic/common method for logging/processing different types of metrics.
        /// </summary>
        /// <typeparam name="TMetric">The type of the metric to process.</typeparam>
        /// <param name="metric">The instance of the metric to process.</param>
        /// <param name="metricBaseTypes">The base types of the metrics which should be logged.</param>
        /// <param name="includedMetricTypes">The types of metrics encountered which should be logged.</param>
        /// <param name="excludedMetricTypes">The types of metrics encountered which should not be logged.</param>
        /// <param name="metricTypesLock">The lock object for changes to parameters 'includeMetricTypes' and 'excludedMetricTypes'.</param>
        /// <param name="metricProcessFunction">Function which performs the metric processing.  Optionally returns a unique id for the starting of an interval metric (for interval metric calls to Begin()).</param>
        /// <returns>A unique id for the starting of an interval metric (for interval metric calls to Begin()).</returns>
        protected Guid ProcessMetric<TMetric>
        (
            TMetric metric,
            HashSet<Type> metricBaseTypes,
            HashSet<Type> includedMetricTypes,
            HashSet<Type> excludedMetricTypes,
            Object metricTypesLock,
            Func<Guid> metricProcessFunction
        )
            where TMetric : MetricBase
        {
            Type metricType = metric.GetType();
            if (includedMetricTypes.Contains(metricType) == true)
            {
                return metricProcessFunction.Invoke();
            }
            else if (excludedMetricTypes.Contains(metricType) == true)
            {
                // Do nothing
            }
            else
            {
                lock (metricTypesLock)
                {
                    Type currentBaseType = metricType.BaseType;
                    while (currentBaseType != typeof(MetricBase))
                    {
                        if (metricBaseTypes.Contains(currentBaseType) == true)
                        {
                            // Need to check below again, as it wasn't checked under lock context at the top of the method
                            if (includedMetricTypes.Contains(metricType) == false)
                            {
                                includedMetricTypes.Add(metricType);
                            }
                            return metricProcessFunction.Invoke();
                        }

                        currentBaseType = currentBaseType.BaseType;
                    }
                    if (excludedMetricTypes.Contains(metricType) == false)
                    {
                        excludedMetricTypes.Add(metricType);
                    }
                }
            }

            return Guid.Empty;
        }

        #endregion
    }
}
