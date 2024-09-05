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
using ApplicationAccess.Utilities;
using ApplicationLogging;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Implementation of <see cref="IAccessManagerTemporalBulkPersister{TUser, TGroup, TComponent, TAccess}"/> which <see href="https://en.wikipedia.org/wiki/Decorator_pattern">decorates</see> another <see cref="IAccessManagerTemporalBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instance by attempting to persist events to a second <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instance in the case that a call to the <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}.PersistEvents(IList{TemporalEventBufferItemBase})">PersistEvents()</see> method fails, and also attempts to re-persist any events which previously failed to be persisted on startup.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>The <see cref="IDisposable.Dispose">Dispose()</see> method will call Dispose() on constructor parameters 'primaryReader', 'primaryPersister', and 'backupPersister' (assuming they implement <see cref="IDisposable"/>), even though those onjects are instantiated outside the class.</remarks>
    public class AccessManagerRedundantTemporalBulkPersister<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The primary (decorated) <see cref="IAccessManagerTemporalPersistentReader{TUser, TGroup, TComponent, TAccess}"/> instance.</summary>
        protected IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> primaryReader;
        /// <summary>The primary (decorated) <see cref="IAccessManagerIdempotentTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instance.</summary>
        protected IAccessManagerIdempotentTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> primaryPersister;
        /// <summary>The backup/secondary persister.</summary>
        protected IAccessManagerTemporalEventBulkPersisterReader<TUser, TGroup, TComponent, TAccess> backupPersister;
        /// <summary>The logger for general logging.</summary>
        protected IApplicationLogger logger;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Whether the first call to the PersistEvents() method has already occurred.</summary>
        protected Boolean firstCallToPersistEventsOccurred;
        /// <summary>Whether any failure has occurred during a call to the PersistEvents() method on the primary persister.</summary>
        protected Boolean primaryPersisterFailed;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected Boolean disposed;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerRedundantTemporalBulkPersister class.
        /// </summary>
        /// <param name="primaryReader">The primary (decorated) <see cref="IAccessManagerTemporalPersistentReader{TUser, TGroup, TComponent, TAccess}"/> instance.</param>
        /// <param name="primaryPersister">The primary (decorated) <see cref="IAccessManagerIdempotentTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instance.</param>
        /// <param name="backupPersister">The backup/secondary persister.</param>
        /// <param name="logger">The logger for general logging.</param>
        public AccessManagerRedundantTemporalBulkPersister
        (
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> primaryReader,
            IAccessManagerIdempotentTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> primaryPersister,
            IAccessManagerTemporalEventBulkPersisterReader<TUser, TGroup, TComponent, TAccess> backupPersister,
            IApplicationLogger logger
        )
        {
            this.primaryReader = primaryReader;
            this.primaryPersister = primaryPersister;
            this.backupPersister = backupPersister;
            this.logger = logger;
            this.metricLogger = new NullMetricLogger();
            firstCallToPersistEventsOccurred = false;
            primaryPersisterFailed = false;
            disposed = false;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.AccessManagerRedundantTemporalBulkPersister class.
        /// </summary>
        /// <param name="primaryReader">The primary (decorated) <see cref="IAccessManagerTemporalPersistentReader{TUser, TGroup, TComponent, TAccess}"/> instance.</param>
        /// <param name="primaryPersister">The primary (decorated) <see cref="IAccessManagerIdempotentTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> instance.</param>
        /// <param name="backupPersister">The backup/secondary persister.</param>
        /// <param name="logger">The logger for general logging.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerRedundantTemporalBulkPersister
        (
            IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> primaryReader,
            IAccessManagerIdempotentTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> primaryPersister,
            IAccessManagerTemporalEventBulkPersisterReader<TUser, TGroup, TComponent, TAccess> backupPersister,
            IApplicationLogger logger, 
            IMetricLogger metricLogger
        ) : this(primaryReader, primaryPersister, backupPersister, logger)
        {
            this.metricLogger = metricLogger;
        }

        /// <inheritdoc/>
        public AccessManagerState Load(AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return primaryReader.Load(accessManagerToLoadTo);
        }

        /// <inheritdoc/>
        public AccessManagerState Load(Guid eventId, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return primaryReader.Load(eventId, accessManagerToLoadTo);
        }

        /// <inheritdoc/>
        public AccessManagerState Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            return primaryReader.Load(stateTime, accessManagerToLoadTo);
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            PersistEvents(events, false);
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events, bool ignorePreExistingEvents)
        {
            if (firstCallToPersistEventsOccurred == false)
            {
                // Retrieve any persisted events from the backup persister
                IList<TemporalEventBufferItemBase> backedUpEvents = null;
                try
                {
                    backedUpEvents = backupPersister.GetAllEvents();
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to retrieve backed-up events from backup persister.", e);
                }
                if (backedUpEvents.Count > 0)
                {
                    metricLogger.Add(new EventsReadFromBackupPersister(), backedUpEvents.Count);
                    logger.Log(this, LogLevel.Information, $"Read {backedUpEvents.Count} events from backup event persister.");
                    // Persist the backed-up events ignoring any duplicates (since some of them may have succeeded on the initial attempt to persist)
                    try
                    {
                        primaryPersister.PersistEvents(backedUpEvents, true);
                    }
                    catch (Exception primaryPersisterException)
                    {
                        primaryPersisterFailed = true;
                        try
                        {
                            backupPersister.PersistEvents(backedUpEvents);
                            backupPersister.PersistEvents(events);
                        }
                        catch (Exception backupPersisterException)
                        {
                            throw new AggregateException("Failed to persist events to backup persister whilst handling exception generated in primary persister from attempting to persist previously backed-up events.", primaryPersisterException, backupPersisterException);
                        }
                        metricLogger.Add(new EventsWrittenToBackupPersister(), backedUpEvents.Count + events.Count);
                        metricLogger.Increment(new EventWriteToPrimaryPersisterFailed());
                        logger.Log(this, LogLevel.Error, $"Wrote {backedUpEvents.Count + events.Count} events to backup event persister due to exception encountered during persist operation on primary persister.", primaryPersisterException);
                        throw new Exception("Failed to persist previously backed-up events to primary persister.", primaryPersisterException);
                    }
                    metricLogger.Add(new BufferedEventsFlushed(), backedUpEvents.Count);
                }
                firstCallToPersistEventsOccurred = true;
            }

            // Persist events using the primary persister (or the backup persister if a failure has occured on the primary)
            if (primaryPersisterFailed == false)
            {
                try
                {
                    primaryPersister.PersistEvents(events, ignorePreExistingEvents);
                }
                catch (Exception primaryPersisterException)
                {
                    primaryPersisterFailed = true;
                    try
                    {
                        backupPersister.PersistEvents(events);
                    }
                    catch (Exception backupPersisterException)
                    {
                        throw new AggregateException("Failed to persist events to backup persister whilst handling exception generated in primary persister.", primaryPersisterException, backupPersisterException);
                    }
                    metricLogger.Add(new EventsWrittenToBackupPersister(), events.Count);
                    metricLogger.Increment(new EventWriteToPrimaryPersisterFailed());
                    logger.Log(this, LogLevel.Error, $"Wrote {events.Count} events to backup event persister due to exception encountered during persist operation on primary persister.", primaryPersisterException);
                    throw new Exception("Failed to persist events to primary persister.", primaryPersisterException);
                }
            }
            else
            {
                try
                {
                    backupPersister.PersistEvents(events);
                }
                catch (Exception e)
                {
                    throw new Exception("Failed to persist events to backup persister after previous exception on primary persister.", e);
                }
                metricLogger.Add(new EventsWrittenToBackupPersister(), events.Count);
                logger.Log(this, LogLevel.Error, $"Wrote {events.Count} events to backup event persister after previous exception on primary persister.");
            }
        }

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the AccessManagerRedundantTemporalBulkPersister.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~AccessManagerRedundantTemporalBulkPersister()
        {
            Dispose(false);
        }
    
        #pragma warning restore 1591

        /// <summary>
        /// Provides a method to free unmanaged resources used by this class.
        /// </summary>
        /// <param name="disposing">Whether the method is being called as part of an explicit Dispose routine, and hence whether managed resources should also be freed.</param>
        protected virtual void Dispose(bool disposing)
        {
            if (!disposed)
            {
                if (disposing)
                {
                    // Free other state (managed objects).
                    if (primaryReader is IDisposable)
                    {
                        ((IDisposable)primaryReader).Dispose();
                    }
                    if (primaryPersister is IDisposable)
                    {
                        ((IDisposable)primaryPersister).Dispose();
                    }
                    if (backupPersister is IDisposable)
                    {
                        ((IDisposable)backupPersister).Dispose();
                    }
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }
}
