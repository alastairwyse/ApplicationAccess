﻿/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using System.Threading;
using System.Runtime.ExceptionServices;
using ApplicationMetrics;
using ApplicationMetrics.MetricLoggers;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Provides common base functionality for implementations of IAccessManagerEventBufferFlushStrategy which use a worker thread to perform buffer flushes.
    /// </summary>
    public abstract class WorkerThreadBufferFlushStrategyBase : IAccessManagerEventBufferFlushStrategy, IDisposable
    {
        /// <summary>The number of user events currently buffered</summary>
        private Int32 userEventsBuffered;
        /// <summary>The number of group events currently buffered</summary>
        private Int32 groupEventsBuffered;
        /// <summary>The number of user to group mapping events currently buffered</summary>
        private Int32 userToGroupMappingEventsBuffered;
        /// <summary>The number of group to group mapping events currently buffered</summary>
        private Int32 groupToGroupMappingEventsBuffered;
        /// <summary>The number of user to application component and access level mapping events currently buffered</summary>
        private Int32 userToApplicationComponentAndAccessLevelMappingEventsBuffered;
        /// <summary>The number of group to application component and access level mapping events currently buffered</summary>
        private Int32 groupToApplicationComponentAndAccessLevelMappingEventsBuffered;
        /// <summary>The number of entity type events currently buffered</summary>
        private Int32 entityTypeEventsBuffered;
        /// <summary>The number of entity events currently buffered</summary>
        private Int32 entityEventsBuffered;
        /// <summary>The number of user to entity mapping events currently buffered</summary>
        private Int32 userToEntityMappingEventsBuffered;
        /// <summary>The number of group to entity mapping events currently buffered</summary>
        private Int32 groupToEntityMappingEventsBuffered;

        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Worker thread which implements the strategy to flush/process the contents of the buffers.</summary>
        private Thread bufferFlushingWorkerThread;
        /// <summary>An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</summary>
        protected Action<BufferFlushingException> flushingExceptionAction;
        /// <summary>Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</summary>
        protected Boolean flushRemainingEventsAfterException;
        /// <summary>Whether request to stop the worker thread has been received via the Stop() method.</summary>
        protected volatile Boolean stopMethodCalled;
        /// <summary>Signal that is set after the worker thread completes, either via explicit stopping or an exception occurring (for unit testing).</summary>
        protected ManualResetEvent workerThreadCompleteSignal;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected bool disposed;

        /// <inheritdoc/>
        public event EventHandler BufferFlushed;

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual Int32 UserEventBufferItemCount
        {
            set 
            {
                ThrowExceptionIfParameterLessThanZero(nameof(value), value);

                Interlocked.Exchange(ref userEventsBuffered, value);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual Int32 GroupEventBufferItemCount
        {
            set
            {
                ThrowExceptionIfParameterLessThanZero(nameof(value), value);

                Interlocked.Exchange(ref groupEventsBuffered, value);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual Int32 UserToGroupMappingEventBufferItemCount
        {
            set
            {
                ThrowExceptionIfParameterLessThanZero(nameof(value), value);

                Interlocked.Exchange(ref userToGroupMappingEventsBuffered, value);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual Int32 GroupToGroupMappingEventBufferItemCount
        {
            set
            {
                ThrowExceptionIfParameterLessThanZero(nameof(value), value);

                Interlocked.Exchange(ref groupToGroupMappingEventsBuffered, value);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual Int32 UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount
        {
            set
            {
                ThrowExceptionIfParameterLessThanZero(nameof(value), value);

                Interlocked.Exchange(ref userToApplicationComponentAndAccessLevelMappingEventsBuffered, value);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual Int32 GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount
        {
            set
            {
                ThrowExceptionIfParameterLessThanZero(nameof(value), value);

                Interlocked.Exchange(ref groupToApplicationComponentAndAccessLevelMappingEventsBuffered, value);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual Int32 EntityTypeEventBufferItemCount
        {
            set
            {
                ThrowExceptionIfParameterLessThanZero(nameof(value), value);

                Interlocked.Exchange(ref entityTypeEventsBuffered, value);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual Int32 EntityEventBufferItemCount
        {
            set
            {
                ThrowExceptionIfParameterLessThanZero(nameof(value), value);

                Interlocked.Exchange(ref entityEventsBuffered, value);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual Int32 UserToEntityMappingEventBufferItemCount
        {
            set
            {
                ThrowExceptionIfParameterLessThanZero(nameof(value), value);

                Interlocked.Exchange(ref userToEntityMappingEventsBuffered, value);
            }
        }

        /// <inheritdoc/>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual Int32 GroupToEntityMappingEventBufferItemCount
        {
            set
            {
                ThrowExceptionIfParameterLessThanZero(nameof(value), value);

                Interlocked.Exchange(ref groupToEntityMappingEventsBuffered, value);
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.WorkerThreadBufferFlushStrategyBase class.
        /// </summary>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        public WorkerThreadBufferFlushStrategyBase(Boolean flushRemainingEventsAfterException)
        {
            userEventsBuffered = 0;
            groupEventsBuffered = 0;
            userToGroupMappingEventsBuffered = 0;
            groupToGroupMappingEventsBuffered = 0;
            userToApplicationComponentAndAccessLevelMappingEventsBuffered = 0;
            groupToApplicationComponentAndAccessLevelMappingEventsBuffered = 0;
            entityTypeEventsBuffered = 0;
            entityEventsBuffered = 0;
            userToEntityMappingEventsBuffered = 0;
            groupToEntityMappingEventsBuffered = 0;

            metricLogger = new NullMetricLogger();
            flushingExceptionAction = (BufferFlushingException flushingException) => { };
            this.flushRemainingEventsAfterException = flushRemainingEventsAfterException;
            stopMethodCalled = false;
            workerThreadCompleteSignal = null;
            disposed = false;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.WorkerThreadBufferFlushStrategyBase class.
        /// </summary>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        public WorkerThreadBufferFlushStrategyBase(Boolean flushRemainingEventsAfterException, Action<BufferFlushingException> flushingExceptionAction)
            : this(flushRemainingEventsAfterException)
        {
            this.flushingExceptionAction = flushingExceptionAction;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.WorkerThreadBufferFlushStrategyBase class.
        /// </summary>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public WorkerThreadBufferFlushStrategyBase(Boolean flushRemainingEventsAfterException, IMetricLogger metricLogger)
            : this(flushRemainingEventsAfterException)
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.WorkerThreadBufferFlushStrategyBase class.
        /// </summary>
        /// <param name="flushRemainingEventsAfterException">Whether any events remaining in the buffers should be attempted to be flushed/processed after an exception occurs during a previous flush operation.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="flushingExceptionAction">An action to invoke if an error occurs during buffer flushing.  Accepts a single parameter which is the <see cref="BufferFlushingException"/> containing details of the error.</param>
        public WorkerThreadBufferFlushStrategyBase(Boolean flushRemainingEventsAfterException, IMetricLogger metricLogger, Action<BufferFlushingException> flushingExceptionAction)
            : this(flushRemainingEventsAfterException)
        {
            this.metricLogger = metricLogger;
            this.flushingExceptionAction = flushingExceptionAction;
        }

        /// <summary>
        /// Starts the worker thread which performs buffer flushes.
        /// </summary>
        public virtual void Start()
        {
            if (bufferFlushingWorkerThread == null)
                throw new InvalidOperationException($"Property '{nameof(this.BufferFlushingAction)}' has not been set.");

            stopMethodCalled = false;
            bufferFlushingWorkerThread.Name = $"{this.GetType().FullName} event buffer flushing worker thread.";
            bufferFlushingWorkerThread.IsBackground = true;
            bufferFlushingWorkerThread.Start();
        }

        /// <summary>
        /// Stops the worker thread which performs buffer flushes.
        /// </summary>
        /// <exception cref="ApplicationAccess.Persistence.BufferFlushingException">An exception occurred on the worker thread while attempting to flush the buffers.</exception>
        public virtual void Stop()
        {
            stopMethodCalled = true;
            // Wait for the worker thread to finish
            JoinWorkerThread();
        }

        #region Private/Protected Methods

        /// <summary>
        /// The action to execute on the worker thread that implements the buffer flush strategy.
        /// </summary>
        protected Action BufferFlushingAction
        {
            set
            {
                bufferFlushingWorkerThread = new Thread(() =>
                {
                    Boolean exceptionOccurred = false;
                    String exceptionMessagePrefix = "Exception occurred on buffer flushing worker thread at";
                    try
                    {
                        value.Invoke();
                    }
                    catch (Exception e)
                    {
                        exceptionOccurred = true;
                        var wrappedException = new BufferFlushingException($"{exceptionMessagePrefix} {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")}.", e);
                        flushingExceptionAction.Invoke(wrappedException);
                    }
                    // If no exception has occurred, flush any remaining buffered events
                    if ((exceptionOccurred == false || flushRemainingEventsAfterException == true) && TotalEventsBuffered > 0)
                    {
                        metricLogger.Add(new EventsBufferedAfterFlushStrategyStop(), TotalEventsBuffered);
                        try
                        {
                            OnBufferFlushed(EventArgs.Empty);
                        }
                        catch (Exception e)
                        {
                            exceptionOccurred = true;
                            var wrappedException = new BufferFlushingException($"{exceptionMessagePrefix} {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")}.", e);
                            flushingExceptionAction.Invoke(wrappedException);
                        }
                    }
                    if (workerThreadCompleteSignal != null)
                    {
                        workerThreadCompleteSignal.Set();
                    }
                });
            }
        }

        /// <summary>
        /// Calls Join() on the worker thread, waiting until it terminates.
        /// </summary>
        protected void JoinWorkerThread()
        {
            if (bufferFlushingWorkerThread != null)
            {
                bufferFlushingWorkerThread.Join();
            }
        }

        /// <summary>
        /// The total number of events currently stored across all buffers.
        /// </summary>
        /// <remarks>Note that the counter members accessed in this property may be accessed by multiple threads (i.e. the worker thread in member bufferFlushingWorkerThread and the client code in the main thread).  This property should only be read from methods which have locks around the queues in the corresponding <see cref="AccessManagerTemporalEventPersisterBuffer{TUser, TGroup, TComponent, TAccess}"/> class (e.g. overrides of the virtual setters defined in this class, which are called from the AddUser(), AddGroup(), etc... methods in the AccessManagerTemporalEventPersisterBuffer class).</remarks>
        protected virtual long TotalEventsBuffered
        {
            get
            {
                return userEventsBuffered +
                    groupEventsBuffered +
                    userToGroupMappingEventsBuffered +
                    groupToGroupMappingEventsBuffered +
                    userToApplicationComponentAndAccessLevelMappingEventsBuffered +
                    groupToApplicationComponentAndAccessLevelMappingEventsBuffered +
                    entityTypeEventsBuffered +
                    entityEventsBuffered +
                    userToEntityMappingEventsBuffered +
                    groupToEntityMappingEventsBuffered;
            }
        }

        /// <summary>
        /// Raises the BufferFlushed event.
        /// </summary>
        /// <param name="e">An EventArgs that contains the event data.</param>
        protected virtual void OnBufferFlushed(EventArgs e)
        {
            if (BufferFlushed != null)
            {
                BufferFlushed(this, e);
            }
        }

        /// <summary>
        /// Throws an ArgumentOutOfRangeException if the value of the specified integer parameter is less than 0.
        /// </summary>
        /// <param name="parameterName">The name of the parameter.</param>
        /// <param name="parameterValue">The value of the parameter.</param>
        protected void ThrowExceptionIfParameterLessThanZero(String parameterName, Int32 parameterValue)
        {
            if (parameterValue < 0)
                throw new ArgumentOutOfRangeException(parameterName, $"Parameter '{parameterName}' with value {parameterValue} cannot be less than 0.");
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the WorkerThreadBufferFlushStrategyBase.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591
        ~WorkerThreadBufferFlushStrategyBase()
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
                    if (workerThreadCompleteSignal != null)
                    {
                        workerThreadCompleteSignal.Dispose();
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
