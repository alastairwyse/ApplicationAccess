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
using System.Threading;
using ApplicationLogging;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Test harness for <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> instances.
    /// </summary>
    public class TestHarness<TUser, TGroup, TComponent, TAccess> : IDisposable
    {
        protected IAccessManager<TUser, TGroup, TComponent, TAccess> testAccessManager;
        protected Int32 workerThreadCount;
        protected DataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer;
        protected IList<IOperationGenerator> operationGenerators;
        protected IList<IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess>> parameterGenerators;
        protected IList<IOperationTriggerer> operationTriggerers;
        protected IList<IApplicationLogger> exceptionLoggers;
        protected Double exceptionsPerSecondThreshold;
        protected Int32 previousExceptionOccurenceTimeWindowSize;
        protected volatile Boolean stopMethodCalled;
        protected List<Thread> workerThreads;
        protected Boolean disposed;

        /// <summary>
        ///  Initialises a new instance of the ApplicationAccess.TestHarness.TestHarness class.
        /// </summary>
        /// <param name="testAccessManager">The access manager to test.</param>
        /// <param name="workerThreadCount">The number of worker threads to use to generate operations.</param>
        /// <param name="operationGenerators">The operation generators to use for each worker thread.</param>
        /// <param name="parameterGenerators">The parameter generators to use for each worker thread.</param>
        /// <param name="operationTriggerers">The operation triggerers to use for each worker thread.</param>
        /// <param name="exceptionLoggers">The exception loggers to use for each worker thread</param>
        /// <param name="exceptionsPerSecondThreshold">The threshold for the allowed number of exceptions per worker thread, per second.</param>
        /// <param name="previousExceptionOccurenceTimeWindowSize">The number of previous exception occurence timestamps to keep, in order to calculate the number of exceptions occurring per worker thread, per second.</param>
        public TestHarness
        (
            IAccessManager<TUser, TGroup, TComponent, TAccess> testAccessManager, 
            Int32 workerThreadCount, 
            IList<IOperationGenerator> operationGenerators, 
            IList<IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess>> parameterGenerators, 
            IList<IOperationTriggerer> operationTriggerers,
            IList<IApplicationLogger> exceptionLoggers,
            Double exceptionsPerSecondThreshold,
            Int32 previousExceptionOccurenceTimeWindowSize
        )
        {
            if (workerThreadCount < 1)
                throw new ArgumentOutOfRangeException(nameof(workerThreadCount), $"Parameter '{nameof(workerThreadCount)}' with value {workerThreadCount} must be greater than or equal to 1.");
            if (operationGenerators.Count != workerThreadCount)
                throw new ArgumentException($"Parameter '{nameof(operationGenerators)}' is expected contain the same number of elements {workerThreadCount} as in parameter '{nameof(workerThreadCount)}' but contained {operationGenerators.Count}.", nameof(operationGenerators));
            if (parameterGenerators.Count != workerThreadCount)
                throw new ArgumentException($"Parameter '{nameof(parameterGenerators)}' is expected contain the same number of elements {workerThreadCount} as in parameter '{nameof(workerThreadCount)}' but contained {parameterGenerators.Count}.", nameof(parameterGenerators));
            if (operationTriggerers.Count != workerThreadCount)
                throw new ArgumentException($"Parameter '{nameof(operationTriggerers)}' is expected contain the same number of elements {workerThreadCount} as in parameter '{nameof(workerThreadCount)}' but contained {operationTriggerers.Count}.", nameof(operationTriggerers));
            if (exceptionLoggers.Count != workerThreadCount)
                throw new ArgumentException($"Parameter '{nameof(exceptionLoggers)}' is expected contain the same number of elements {workerThreadCount} as in parameter '{nameof(workerThreadCount)}' but contained {exceptionLoggers.Count}.", nameof(exceptionLoggers));
            if (exceptionsPerSecondThreshold <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(exceptionsPerSecondThreshold), $"Parameter '{nameof(exceptionsPerSecondThreshold)}' with value {exceptionsPerSecondThreshold} cannot be less than or equal to 0.");
            if (previousExceptionOccurenceTimeWindowSize < 2)
                throw new ArgumentOutOfRangeException(nameof(previousExceptionOccurenceTimeWindowSize), $"Parameter '{nameof(previousExceptionOccurenceTimeWindowSize)}' with value {previousExceptionOccurenceTimeWindowSize} cannot be less than 0.");

            this.testAccessManager = testAccessManager;
            this.workerThreadCount = workerThreadCount;
            dataElementStorer = new DataElementStorer<TUser, TGroup, TComponent, TAccess>();
            this.operationGenerators = operationGenerators;
            this.parameterGenerators = parameterGenerators;
            this.operationTriggerers = operationTriggerers;
            this.exceptionLoggers = exceptionLoggers;
            this.exceptionsPerSecondThreshold = exceptionsPerSecondThreshold;
            this.previousExceptionOccurenceTimeWindowSize = previousExceptionOccurenceTimeWindowSize;
            stopMethodCalled = false;
            disposed = false;
        }

        /// <summary>
        /// Starts the testing.
        /// </summary>
        public void Start()
        {
            workerThreads = new List<Thread>(workerThreadCount);
            for (Int32 i = 0; i < workerThreadCount; i++)
            {
                var currentThread = new Thread(() => { WorkerThreadRoutine(operationGenerators[i], parameterGenerators[i], operationTriggerers[i], exceptionLoggers[i]); });
                currentThread.Name = $"ApplicationAccess.TestHarness.TestHarness worker thread {i}.";
                currentThread.IsBackground = true;
                workerThreads.Add(currentThread);
                currentThread.Start();
            }
        }

        /// <summary>
        /// Stops the testing.
        /// </summary>
        public void Stop()
        {
            stopMethodCalled = true;
            foreach (IOperationTriggerer currentOperationTriggerer in operationTriggerers)
            {
                currentOperationTriggerer.Stop();
            }
            foreach (Thread currentWorkerThread in workerThreads)
            {
                // Don't call Join() if the current thread is the same as the worker thread
                //   Prevents a thread joining itself if Stop() was called from one of the worker threads, rather than the client thread
                if (Thread.CurrentThread != currentWorkerThread)
                {
                    currentWorkerThread.Join();
                }
            }
        }

        #region Private/Protected Methods

        protected void WorkerThreadRoutine
        (
            IOperationGenerator operationGenerator, 
            IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess> parameterGenerator, 
            IOperationTriggerer operationTriggerer, 
            IApplicationLogger exceptionLogger
        )
        {
            Console.WriteLine($"Starting test worker thread with name '{Thread.CurrentThread.Name}' and id {Thread.CurrentThread.ManagedThreadId}");

            operationTriggerer.Start();
            var operationExecutor = new OperationExecutionPreparer<TUser, TGroup, TComponent, TAccess>(parameterGenerator, testAccessManager, dataElementStorer);
            var exceptionHandler = new ExceptionHandler(exceptionsPerSecondThreshold, previousExceptionOccurenceTimeWindowSize, exceptionLogger);

            while (stopMethodCalled == false)
            {
                // TODO: Doing this here rather than after the WaitForNextTrigger() call means there's a gap, and risk the state of the access manager changed between preparing the operation and executing it
                //   Will need to see if this causes issues
                AccessManagerOperation nextOperation = operationGenerator.Generate();
                PrepareExecutionReturnActions actions = operationExecutor.PrepareExecution(nextOperation);
                operationTriggerer.WaitForNextTrigger();
                try
                {
                    actions.ExecutionAction.Invoke();
                    actions.PostExecutionAction.Invoke();
                }
                catch (Exception e)
                {
                    try
                    {
                        exceptionHandler.HandleException(e);
                    }
                    catch (Exception)
                    {
                        Stop();
                        throw new Exception($"Exception occurred on worker thread with name '{Thread.CurrentThread.Name}' and id {Thread.CurrentThread.ManagedThreadId}", e);
                    }
                }
            }

            Console.WriteLine($"Stopping test worker thread with name '{Thread.CurrentThread.Name}' and id {Thread.CurrentThread.ManagedThreadId}");
        }

        #endregion

        #region Finalize / Dispose Methods

        /// <summary>
        /// Releases the unmanaged resources used by the TestHarness.
        /// </summary>
        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #pragma warning disable 1591

        ~TestHarness()
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
                    foreach (IOperationTriggerer currentOperationTriggerer in operationTriggerers)
                    {
                        currentOperationTriggerer.Dispose();
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