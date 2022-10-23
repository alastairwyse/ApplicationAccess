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
    /// Defines methods to generate parameters for <see cref="AccessManagerOperation">AccessManagerOperations</see>.
    /// </summary>
    public interface IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess>
    {

        // TODO: Split these into 2 separate methods... one generic which creates the params (will need overloads of these with different numbers of arguments)
        //   And other which returns the action
        //   Need to have the params as individual objects so that they could be 'recorded' or for detailed exception handling

        /// <summary>
        /// Generates parameters for the specified operation, and returns them as an action which can be invoked against an AccessManager instance.
        /// </summary>
        /// <param name="operation">The operation to generate the parameters for.</param>
        /// <param name="accessManagerInstance">The AccessManager instance to create the action against.</param>
        /// <returns>An action which can be invoked against an AccessManager instance.</returns>
        Action GenerateParameters(AccessManagerOperation operation, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerInstance);
    }
}
