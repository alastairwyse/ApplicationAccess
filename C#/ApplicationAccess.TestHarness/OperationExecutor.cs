﻿/*
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
using System.Threading;
using ApplicationLogging;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Executes test operations against an AccessManager under test, working with a counterpart <see cref="TwoWaySignallingParticipantBase"/> instance to define the wait time between executions.
    /// </summary>
    public class OperationExecutor<TUser, TGroup, TComponent, TAccess> : TwoWaySignallingParticipantBase
    {
        /// <summary>Stores the data elements in the AccessManager instance under test.</summary>
        protected IDataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer;
        /// <summary>The <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> component of the AccessManager under test.</summary>
        protected IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess> testAccessManagerQueryProcessor;
        /// <summary>The <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}"/> component of the AccessManager under test.</summary>
        protected IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> testAccessManagerEventProcessor;
        /// <summary>Generates the test operations to perform.</summary>
        protected IOperationGenerator operationGenerator;
        /// <summary>Generates parameters for the test operations.</summary>
        protected IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess> parameterGenerator;
        /// <summary>Logger for any exceptions which occur during testing.</summary>
        protected IApplicationLogger exceptionLogger;
        /// <summary>Used to count the number of operations executed.</summary>
        protected OperationCounter operationCounter;
        /// <summary>Signal used to notify that testing should be stopped/cancelled (e.g. in the case of critical error).</summary>
        protected EventWaitHandle stopSignal;
        /// <summary>The threshold for the allowed number of exceptions per worker thread, per second.</summary>
        protected Double exceptionsPerSecondThreshold;
        /// <summary>The number of previous exception occurence timestamps to keep, in order to calculate the number of exceptions occurring per worker thread, per second.</summary>
        protected Int32 previousExceptionOccurenceTimeWindowSize;
        /// <summary>Whether to ignore exceptions which are known to be generated by the <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/>, e.g. <see cref="ArgumentException">ArgumentExceptions</see>.</summary>
        protected Boolean ignoreKnownAccessManagerExceptions;
        /// <summary>The next test actions to be executed.</summary>
        protected PrepareExecutionReturnActions nextActions;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.OperationExecutor class.
        /// </summary>
        /// <param name="dataElementStorer">Stores the data elements in the AccessManager instance under test.</param>
        /// <param name="testAccessManagerQueryProcessor">The <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> component of the AccessManager under test.</param>
        /// <param name="testAccessManagerEventProcessor">The <see cref="IAccessManagerEventProcessor{TUser, TGroup, TComponent, TAccess}"/> component of the AccessManager under test.</param>
        /// <param name="operationGenerator">Generates the test operations to perform.</param>
        /// <param name="parameterGenerator">Generates parameters for the test operations.</param>
        /// <param name="exceptionLogger">Logger for any exceptions which occur during testing.</param>
        /// <param name="operationCounter">Used to count the number of operations executed.</param>
        /// <param name="stopSignal">Signal used to notify that testing should be stopped/cancelled (e.g. in the case of critical error).</param>
        /// <param name="exceptionsPerSecondThreshold">The threshold for the allowed number of exceptions per worker thread, per second.</param>
        /// <param name="previousExceptionOccurenceTimeWindowSize">The number of previous exception occurence timestamps to keep, in order to calculate the number of exceptions occurring per worker thread, per second.</param>
        /// <param name="ignoreKnownAccessManagerExceptions">Whether to ignore exceptions which are known to be generated by the <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/>, e.g. <see cref="ArgumentException">ArgumentExceptions</see>.</param>
        /// <param name="id">An optional unique id for this OperationTriggerer instance.</param>
        public OperationExecutor
        (
            IDataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer,
            IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess> testAccessManagerQueryProcessor,
            IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> testAccessManagerEventProcessor, 
            IOperationGenerator operationGenerator,
            IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess> parameterGenerator,
            IApplicationLogger exceptionLogger,
            OperationCounter operationCounter,
            EventWaitHandle stopSignal, 
            Double exceptionsPerSecondThreshold,
            Int32 previousExceptionOccurenceTimeWindowSize, 
            Boolean ignoreKnownAccessManagerExceptions,
            String id = ""
        ) : base()
        {
            if (exceptionsPerSecondThreshold <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(exceptionsPerSecondThreshold), $"Parameter '{nameof(exceptionsPerSecondThreshold)}' with value {exceptionsPerSecondThreshold} cannot be less than or equal to 0.");
            if (previousExceptionOccurenceTimeWindowSize < 2)
                throw new ArgumentOutOfRangeException(nameof(previousExceptionOccurenceTimeWindowSize), $"Parameter '{nameof(previousExceptionOccurenceTimeWindowSize)}' with value {previousExceptionOccurenceTimeWindowSize} cannot be less than 0.");

            this.dataElementStorer = dataElementStorer;
            this.testAccessManagerQueryProcessor = testAccessManagerQueryProcessor;
            this.testAccessManagerEventProcessor = testAccessManagerEventProcessor;
            this.operationGenerator = operationGenerator;
            this.parameterGenerator = parameterGenerator;
            this.exceptionLogger = exceptionLogger;
            this.operationCounter = operationCounter;
            this.stopSignal = stopSignal;
            this.exceptionsPerSecondThreshold = exceptionsPerSecondThreshold;
            this.previousExceptionOccurenceTimeWindowSize = previousExceptionOccurenceTimeWindowSize;
            this.ignoreKnownAccessManagerExceptions = ignoreKnownAccessManagerExceptions;
            base.workerThreadName = $"ApplicationAccess.TestHarness.OperationExecutor worker thread";
            if (String.IsNullOrEmpty(id) == false)
            {
                workerThreadName = workerThreadName + $" id: {id}";
            }
        }

        /// <inheritdoc/>
        public override void Start()
        {
            var operationExecutor = new OperationExecutionPreparer<TUser, TGroup, TComponent, TAccess>(parameterGenerator, testAccessManagerQueryProcessor, testAccessManagerEventProcessor, dataElementStorer);
            var exceptionHandler = new ExceptionHandler(exceptionsPerSecondThreshold, previousExceptionOccurenceTimeWindowSize, ignoreKnownAccessManagerExceptions, exceptionLogger);
            //  Generate the first actions to execute
            AccessManagerOperation nextOperation = operationGenerator.Generate();
            nextActions = operationExecutor.PrepareExecution(nextOperation);
            workerThreadIterationAction = () =>
            {
                try
                {
                    nextActions.ExecutionAction.Invoke();
                    nextActions.PostExecutionAction.Invoke();
                }
                catch (Exception e)
                {
                    try
                    {
                        exceptionHandler.HandleException(e);
                    }
                    catch (Exception)
                    {
                        stopSignal.Set();
                        throw new Exception($"Exception occurred on ApplicationAccess.TestHarness.OperationExecutor instance worker thread with thread name '{Thread.CurrentThread.Name}' and thread id {Thread.CurrentThread.ManagedThreadId}", e);
                    }
                }
                operationCounter.Increment();
                // Generate the next action to execute
                AccessManagerOperation nextOperation = operationGenerator.Generate();
                nextActions = operationExecutor.PrepareExecution(nextOperation);
            };
            base.Start();
        }
    }
}
