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
using ApplicationAccess.Hosting.Rest.Client;
using ApplicationLogging;

namespace ApplicationAccess.TestHarness
{
    #pragma warning disable 8032

    /// <summary>
    /// Test harness for <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> instances.
    /// </summary>
    public class TestHarness<TUser, TGroup, TComponent, TAccess> : IDisposable
    {
        protected Int32 workerThreadCount;
        protected IDataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer;
        /// <summary>The <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> component of the AccessManager under test.</summary>
        protected IList<IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess>> testAccessManagerQueryProcessors;
        /// <summary>The <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}"/> component of the AccessManager under test.</summary>
        protected IList<IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>> testAccessManagerEventProcessors;
        protected IList<IOperationGenerator> operationGenerators;
        protected IList<IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess>> parameterGenerators;
        protected IList<OperationTriggerer> operationTriggerers;
        protected IList<OperationExecutor<TUser, TGroup, TComponent, TAccess>> operationExecutors;
        protected IList<IApplicationLogger> exceptionLoggers;
        protected EventWaitHandle stopSignal;
        protected Double targetOperationsPerSecond;
        protected Int32 operationsPerSecondPrintFrequency;
        protected Int32 previousOperationInitiationTimeWindowSize;
        protected OperationCounter operationCounter;
        protected Double exceptionsPerSecondThreshold;
        protected Int32 previousExceptionOccurenceTimeWindowSize;
        protected Boolean ignoreKnownAccessManagerExceptions;
        protected Boolean generatePrimaryAddOperations;
        protected Boolean disposed;

        /// <summary>
        ///  Initialises a new instance of the ApplicationAccess.TestHarness.TestHarness class.
        /// </summary>
        /// <param name="workerThreadCount">The number of worker threads to use to generate operations.</param>
        /// <param name="dataElementStorer">Stores the data elements in the AccessManager instance under test.</param>
        /// <param name="testAccessManagerQueryProcessors">The <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> components of the AccessManager to test (the same should be assigned to each element), or the equivalent client instances which interface to the remote access manager to test.</param>
        /// <param name="testAccessManagerEventProcessors">The <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}"/> components of the AccessManager to test (the same should be assigned to each element), or the equivalent client instances which interface to the remote access manager to test.</param>
        /// <param name="operationGenerators">The operation generators to use for each worker thread.</param>
        /// <param name="parameterGenerators">The parameter generators to use for each worker thread.</param>
        /// <param name="exceptionLoggers">The exception loggers to use for each worker thread</param>
        /// <param name="stopSignal">Signal used to notify that testing should be stopped/cancelled (e.g. in the case of critical error).</param>
        /// <param name="targetOperationsPerSecond">The target number of operations per second to trigger.  A value of 0.0 will trigger operations continuously at the maximum possible frequency.</param>
        /// <param name="operationsPerSecondPrintFrequency">The number of times per operation iteration that the actual 'operations per second' value should be printed to the console.</param>
        /// <param name="previousOperationInitiationTimeWindowSize">The number of previous operation occurence timestamps to keep, in order to calculate the number of operations occurring per worker thread, per second.</param>
        /// <param name="exceptionsPerSecondThreshold">The threshold for the allowed number of exceptions per worker thread, per second.</param>
        /// <param name="previousExceptionOccurenceTimeWindowSize">The number of previous exception occurence timestamps to keep, in order to calculate the number of exceptions occurring per worker thread, per second.</param>
        /// <param name="operationLimit">The maximum number of operations to generate (set to 0 for no limit).  Testing will stop once this limit is reached.</param>
        /// <param name="ignoreKnownAccessManagerExceptions">Whether to ignore exceptions which are known to be generated by the <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/>, e.g. <see cref="ArgumentException">ArgumentExceptions</see>.</param>
        /// <remarks>Parameter 'testAccessManagers' allows a separate <see cref="IAccessManager{TUser, TGroup, TComponent, TAccess}"/> instance to be defined per thread, to accomodate remote instances where separate <see cref="AccessManagerClient{TUser, TGroup, TComponent, TAccess}"/> instances are required per thread.  In the case of a local AccessManager being tested, that AccessManager should be passed to each thread.</remarks>
        public TestHarness
        (
            Int32 workerThreadCount,
            IDataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer,
            IList<IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess>> testAccessManagerQueryProcessors,
            IList<IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess>> testAccessManagerEventProcessors, 
            IList<IOperationGenerator> operationGenerators,
            IList<IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess>> parameterGenerators,
            IList<IApplicationLogger> exceptionLoggers,
            EventWaitHandle stopSignal, 
            Double targetOperationsPerSecond,
            Int32 operationsPerSecondPrintFrequency, 
            Int32 previousOperationInitiationTimeWindowSize, 
            Double exceptionsPerSecondThreshold,
            Int32 previousExceptionOccurenceTimeWindowSize,
            Int64 operationLimit, 
            Boolean ignoreKnownAccessManagerExceptions,
            Boolean generatePrimaryAddOperations
        )
        {
            if (workerThreadCount < 1)
                throw new ArgumentOutOfRangeException(nameof(workerThreadCount), $"Parameter '{nameof(workerThreadCount)}' with value {workerThreadCount} must be greater than or equal to 1.");
            if (testAccessManagerQueryProcessors.Count != workerThreadCount)
                throw new ArgumentException($"Parameter '{nameof(testAccessManagerQueryProcessors)}' is expected contain the same number of elements {workerThreadCount} as in parameter '{nameof(workerThreadCount)}' but contained {testAccessManagerQueryProcessors.Count}.", nameof(testAccessManagerQueryProcessors));
            if (testAccessManagerEventProcessors.Count != workerThreadCount)
                throw new ArgumentException($"Parameter '{nameof(testAccessManagerEventProcessors)}' is expected contain the same number of elements {workerThreadCount} as in parameter '{nameof(workerThreadCount)}' but contained {testAccessManagerEventProcessors.Count}.", nameof(testAccessManagerEventProcessors));
            if (operationGenerators.Count != workerThreadCount)
                throw new ArgumentException($"Parameter '{nameof(operationGenerators)}' is expected contain the same number of elements {workerThreadCount} as in parameter '{nameof(workerThreadCount)}' but contained {operationGenerators.Count}.", nameof(operationGenerators)); 
            if (parameterGenerators.Count != workerThreadCount)
                throw new ArgumentException($"Parameter '{nameof(parameterGenerators)}' is expected contain the same number of elements {workerThreadCount} as in parameter '{nameof(workerThreadCount)}' but contained {parameterGenerators.Count}.", nameof(parameterGenerators));
            if (exceptionLoggers.Count != workerThreadCount)
                throw new ArgumentException($"Parameter '{nameof(exceptionLoggers)}' is expected contain the same number of elements {workerThreadCount} as in parameter '{nameof(workerThreadCount)}' but contained {exceptionLoggers.Count}.", nameof(exceptionLoggers));
            if (exceptionsPerSecondThreshold <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(exceptionsPerSecondThreshold), $"Parameter '{nameof(exceptionsPerSecondThreshold)}' with value {exceptionsPerSecondThreshold} cannot be less than or equal to 0.");
            if (operationLimit < 0)
                throw new ArgumentOutOfRangeException(nameof(operationLimit), $"Parameter '{nameof(operationLimit)}' with value {operationLimit} must be greater than or equal to 0.");
            if (previousExceptionOccurenceTimeWindowSize < 2)
                throw new ArgumentOutOfRangeException(nameof(previousExceptionOccurenceTimeWindowSize), $"Parameter '{nameof(previousExceptionOccurenceTimeWindowSize)}' with value {previousExceptionOccurenceTimeWindowSize} cannot be less than 0.");

            this.workerThreadCount = workerThreadCount;
            this.dataElementStorer = dataElementStorer;
            this.testAccessManagerQueryProcessors = testAccessManagerQueryProcessors;
            this.testAccessManagerEventProcessors = testAccessManagerEventProcessors;
            this.operationGenerators = operationGenerators;
            this.parameterGenerators = parameterGenerators;
            operationTriggerers = new List<OperationTriggerer>(workerThreadCount);
            operationExecutors = new List<OperationExecutor<TUser, TGroup, TComponent, TAccess>>(workerThreadCount);
            this.exceptionLoggers = exceptionLoggers;
            this.stopSignal = stopSignal;
            this.targetOperationsPerSecond = targetOperationsPerSecond;
            this.operationsPerSecondPrintFrequency = operationsPerSecondPrintFrequency;
            this.previousOperationInitiationTimeWindowSize = previousOperationInitiationTimeWindowSize;
            this.exceptionsPerSecondThreshold = exceptionsPerSecondThreshold;
            this.previousExceptionOccurenceTimeWindowSize = previousExceptionOccurenceTimeWindowSize;
            this.ignoreKnownAccessManagerExceptions = ignoreKnownAccessManagerExceptions;
            this.generatePrimaryAddOperations = generatePrimaryAddOperations;
            operationCounter = new OperationCounter(operationLimit, stopSignal);
            disposed = false;
        }

        /// <summary>
        /// Starts the testing.
        /// </summary>
        public void Start()
        {
            for (Int32 i = 0; i < workerThreadCount; i++)
            {
                var currentOperationTriggerer = new OperationTriggerer(targetOperationsPerSecond, previousOperationInitiationTimeWindowSize, operationsPerSecondPrintFrequency, i.ToString());
                var currentOperationExecutor = new OperationExecutor<TUser, TGroup, TComponent, TAccess>
                (
                    dataElementStorer,
                    testAccessManagerQueryProcessors[i],
                    testAccessManagerEventProcessors[i],
                    operationGenerators[i], 
                    parameterGenerators[i], 
                    exceptionLoggers[i], 
                    operationCounter,
                    stopSignal, 
                    exceptionsPerSecondThreshold, 
                    previousExceptionOccurenceTimeWindowSize, 
                    ignoreKnownAccessManagerExceptions, 
                    generatePrimaryAddOperations, 
                    i.ToString()
                );
                currentOperationTriggerer.Counterpart = currentOperationExecutor;
                currentOperationExecutor.Counterpart = currentOperationTriggerer;
                currentOperationExecutor.OperationTriggerer = currentOperationTriggerer;
                operationTriggerers.Add(currentOperationTriggerer);
                operationExecutors.Add(currentOperationExecutor);
                currentOperationTriggerer.Start();
                currentOperationExecutor.Start();
            }
        }

        /// <summary>
        /// Stops the testing.
        /// </summary>
        public void Stop()
        {
            for (Int32 i = 0; i < workerThreadCount; i++)
            {
                operationTriggerers[i].Stop();
                operationExecutors[i].Stop();
            }
        }

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
                    foreach (OperationTriggerer currentOperationTriggerer in operationTriggerers)
                    {
                        currentOperationTriggerer.Dispose();
                    }
                    foreach (OperationExecutor<TUser, TGroup, TComponent, TAccess> currentOperationExecutor in operationExecutors)
                    {
                        currentOperationExecutor.Dispose();
                    }
                }
                // Free your own state (unmanaged objects).

                // Set large fields to null.

                disposed = true;
            }
        }

        #endregion
    }

    #pragma warning restore 8032
}
