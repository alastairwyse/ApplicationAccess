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
using ApplicationAccess.Persistence;
using ApplicationAccess.Metrics;
using ApplicationMetrics;

namespace ApplicationAccess.Persistence.Metrics
{
    /// <summary>
    /// Logs metric events for an implementation of <see cref="IAccessManagerTemporalPersistentReader{TUser, TGroup, TComponent, TAccess}"/>.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the IAccessManagerTemporalPersistentReader implementation.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the IAccessManagerTemporalPersistentReader implementation.</typeparam>
    /// <typeparam name="TComponent">The type of components in the IAccessManagerTemporalPersistentReader implementation.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access in the IAccessManagerTemporalPersistentReader implementation.</typeparam>
    /// <remarks>Uses a facade pattern to front the IAccessManagerTemporalPersistentReader, capturing metrics and forwarding method calls to the IAccessManagerTemporalPersistentReader.</remarks>
    public class AccessManagerTemporalPersistentReaderMetricLogger<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The IAccessManagerTemporalPersistentReader implementation to log metrics for.</summary>
        protected IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> reader;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.Metrics.AccessManagerTemporalPersistentReaderMetricLogger class.
        /// </summary>
        /// <param name="reader">The IAccessManagerTemporalPersistentReader implementation to log metrics for.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerTemporalPersistentReaderMetricLogger(IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> reader, IMetricLogger metricLogger)
        {
            this.reader = reader;
            this.metricLogger = metricLogger;
        }

        /// <inheritdoc/>
        public Tuple<Guid, DateTime> Load(AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            Tuple<Guid, DateTime> stateInfo;
            Guid beginId = metricLogger.Begin(new LoadTime());
            try
            {
                stateInfo = reader.Load(accessManagerToLoadTo);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new LoadTime());
                throw;
            }
            metricLogger.End(beginId, new LoadTime());

            return stateInfo;
        }

        /// <inheritdoc/>
        public Tuple<Guid, DateTime> Load(Guid eventId, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            Tuple<Guid, DateTime> stateInfo;
            Guid beginId = metricLogger.Begin(new LoadTime());
            try
            {
                stateInfo = reader.Load(eventId, accessManagerToLoadTo);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new LoadTime());
                throw;
            }
            metricLogger.End(beginId, new LoadTime());

            return stateInfo;
        }

        /// <inheritdoc/>
        public Tuple<Guid, DateTime> Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            Tuple<Guid, DateTime> stateInfo;
            Guid beginId = metricLogger.Begin(new LoadTime());
            try
            {
                stateInfo = reader.Load(stateTime, accessManagerToLoadTo);
            }
            catch
            {
                metricLogger.CancelBegin(beginId, new LoadTime());
                throw;
            }
            metricLogger.End(beginId, new LoadTime());

            return stateInfo;
        }
    }
}
