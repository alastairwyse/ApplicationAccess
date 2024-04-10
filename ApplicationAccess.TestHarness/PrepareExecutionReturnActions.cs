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

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Container class containing values returned from the <see cref="OperationExecutionPreparer{TUser, TGroup, TComponent, TAccess}.PrepareExecution(AccessManagerOperation)"/> method.
    /// </summary>
    public class PrepareExecutionReturnActions
    {
        /// <summary>
        /// Action which executes the operation.
        /// </summary>
        public Action ExecutionAction
        {
            get;
        }

        /// <summary>
        /// Action which runs any post-execution routines (e.g. updating data in the <see cref="IDataElementStorer{TUser, TGroup, TComponent, TAccess}"/> instance).
        /// </summary>
        public Action PostExecutionAction
        {
            get;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.PrepareExecutionReturnActions class.
        /// </summary>
        /// <param name="executionAction">Action which executes the operation.</param>
        /// <param name="postExecutionAction">Action which runs any post-execution routines (e.g. updating data in the <see cref="IDataElementStorer{TUser, TGroup, TComponent, TAccess}"/> instance).</param>
        public PrepareExecutionReturnActions(Action executionAction, Action postExecutionAction)
        {
            this.ExecutionAction = executionAction;
            this.PostExecutionAction = postExecutionAction;
        }
    }
}
