/*
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
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Provides common base functionality for implementations of IAccessManagerEventBufferFlushStrategy which use a worker thread to perform buffer flushes.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public abstract class WorkerThreadBufferFlushStrategyBase<TUser, TGroup, TComponent, TAccess> : IAccessManagerEventBufferFlushStrategy<TUser, TGroup, TComponent, TAccess>, IDisposable
    {
        // Threading and Concurrency Doco: TODO -> Tidy Up
        // Notify*() methods should be called by InMemoryEventBuffer at the point of the buffer contents being moved to temp queue
        //   I.e.  at the end of MoveEventsToTemporaryQueue(), and this is under a ME lock... and will be done by this classes' worker thread, seeing as it stems from the Flush() method
        //   Putting on this queue is serialized... the only other thing which touches
        //   Don't forget there's two concerns here... they queue itself, and this userEventsBuffered counter
        //     Make sure that both are only modified whilst in locks
        //     And convince myself it's OK to check their value when not under lock (or are they in fact under lock??)
        // Review AppAccess doc... 'C:\Development\C#\ApplicationMetrics\Documentation\2021-09-07 WorkerThreadBufferProcessorBase Thread Access 1.JPG'
        //   Make a formalized version in Visio... something I can refer back to so I don't have to rehash this locking in my head again


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

        /// <summary>Worker thread which implements the strategy to flush/process the contents of the buffers.</summary>
        private Thread bufferFlushingWorkerThread;
        /// <summary>Set with any exception which occurrs on the worker thread when flushing the buffers.  Null if no exception has occurred.</summary>
        private Exception flushingException;
        /// <summary>Whether request to stop the worker thread has been received via the Stop() method.</summary>
        protected volatile Boolean stopMethodCalled;
        /// <summary>Whether any events remaining in the buffers should be flushed when the Stop() method is called.</summary>
        protected volatile Boolean flushRemainingBufferedEventsOnStop;
        /// <summary>Signal that is set when the worker thread completes, either via explicit stopping or an exception occurring (for unit testing).</summary>
        protected ManualResetEvent workerThreadCompleteSignal;
        /// <summary>Indicates whether the object has been disposed.</summary>
        protected bool disposed;

        /// <summary>
        /// Contains an exception which occurred on the worker thread during buffer flushing.  Null if no exception has occurred.
        /// </summary>
        protected Exception FlushingException
        {
            get { return flushingException; }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="E:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy`4.BufferFlushed"]/*'/>
        public event EventHandler BufferFlushed;

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy`4.UserEventBufferItemCount"]/*'/>
        public Int32 UserEventBufferItemCount
        {
            set 
            {
                CheckAndThrowFlushingException();
                Interlocked.Exchange(ref userEventsBuffered, value);
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy`4.GroupEventBufferItemCount"]/*'/>
        public Int32 GroupEventBufferItemCount
        {
            set
            {
                CheckAndThrowFlushingException();
                Interlocked.Exchange(ref groupEventsBuffered, value);
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy`4.UserToGroupMappingEventBufferItemCount"]/*'/>
        public Int32 UserToGroupMappingEventBufferItemCount
        {
            set
            {
                CheckAndThrowFlushingException();
                Interlocked.Exchange(ref userToGroupMappingEventsBuffered, value);
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy`4.GroupToGroupMappingEventBufferItemCount"]/*'/>
        public Int32 GroupToGroupMappingEventBufferItemCount
        {
            set
            {
                CheckAndThrowFlushingException();
                Interlocked.Exchange(ref groupToGroupMappingEventsBuffered, value);
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy`4.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount"]/*'/>
        public Int32 UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount
        {
            set
            {
                CheckAndThrowFlushingException();
                Interlocked.Exchange(ref userToApplicationComponentAndAccessLevelMappingEventsBuffered, value);
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy`4.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount"]/*'/>
        public Int32 GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount
        {
            set
            {
                CheckAndThrowFlushingException();
                Interlocked.Exchange(ref groupToApplicationComponentAndAccessLevelMappingEventsBuffered, value);
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy`4.EntityTypeEventBufferItemCount"]/*'/>
        public Int32 EntityTypeEventBufferItemCount
        {
            set
            {
                CheckAndThrowFlushingException();
                Interlocked.Exchange(ref entityTypeEventsBuffered, value);
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy`4.EntityEventBufferItemCount"]/*'/>
        public Int32 EntityEventBufferItemCount
        {
            set
            {
                CheckAndThrowFlushingException();
                Interlocked.Exchange(ref entityEventsBuffered, value);
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy`4.UserToEntityMappingEventBufferItemCount"]/*'/>
        public Int32 UserToEntityMappingEventBufferItemCount
        {
            set
            {
                CheckAndThrowFlushingException();
                Interlocked.Exchange(ref userToEntityMappingEventsBuffered, value);
            }
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy`4.GroupToEntityMappingEventBufferItemCount"]/*'/>
        public Int32 GroupToEntityMappingEventBufferItemCount
        {
            set
            {
                CheckAndThrowFlushingException();
                Interlocked.Exchange(ref groupToEntityMappingEventsBuffered, value);
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.WorkerThreadBufferFlushStrategyBase class.
        /// </summary>
        public WorkerThreadBufferFlushStrategyBase()
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

            flushingException = null;
            stopMethodCalled = false;
            flushRemainingBufferedEventsOnStop = true;
            workerThreadCompleteSignal = null;
            disposed = false;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.WorkerThreadBufferFlushStrategyBase class.
        /// </summary>
        /// <param name="flushRemainingBufferedEventsOnStop">Whether any events remaining in the buffers should be flushed when the Stop() method is called.</param>
        public WorkerThreadBufferFlushStrategyBase(Boolean flushRemainingBufferedEventsOnStop)
            : this()
        {
            this.flushRemainingBufferedEventsOnStop = flushRemainingBufferedEventsOnStop;
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
        public virtual void Stop()
        {
            // Check whether any exceptions have occurred on the worker thread and re-throw
            CheckAndThrowFlushingException();
            stopMethodCalled = true;
            // Wait for the worker thread to finish
            JoinWorkerThread();
            // Check for exceptions again incase one occurred after joining the worker thread
            CheckAndThrowFlushingException();
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
                    String exceptionMessagePrefix = "Exception occurred on buffer flushing worker thread at ";

                    try
                    {
                        value.Invoke();
                    }
                    catch (Exception e)
                    {
                        var wrappedException = new Exception($"{exceptionMessagePrefix} {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")}.", e);
                        Interlocked.Exchange(ref flushingException, wrappedException);
                    }
                    // If no exception has occurred, and 'flushRemainingBufferedEventsOnStop' is set true, flush any remaining buffered events
                    if (flushingException == null && TotalEventsBufferred > 0 && flushRemainingBufferedEventsOnStop == true)
                    {
                        try
                        {
                            OnBufferFlushed(EventArgs.Empty);
                        }
                        catch (Exception e)
                        {
                            var wrappedException = new Exception($"{exceptionMessagePrefix} {DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff zzz")}.", e);
                            Interlocked.Exchange(ref flushingException, wrappedException);
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
        /// Checks whether property 'FlushingException' has been set, and re-throws the exception in the case that it has.
        /// </summary>
        protected void CheckAndThrowFlushingException()
        {
            if (flushingException != null)
            {
                throw flushingException;
            }
        }

        /// <summary>
        /// The total number of events currently stored across all buffers.
        /// </summary>
        /// <remarks>Note that the counter members accessed in this property may be accessed by multiple threads (i.e. the worker thread in member bufferFlushingWorkerThread and the client code in the main thread).  This property should only be read from methods which have locks around the queues in the corresponding InMemoryEventBuffer class (e.g. overrides of the virtual 'Notify' methods defined in this class, which are called from the AddUser(), AddGroup(), etc... methods in the InMemoryEventBuffer class).</remarks>
        protected virtual long TotalEventsBufferred
        {
            get
            {
                return userEventsBuffered;
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
