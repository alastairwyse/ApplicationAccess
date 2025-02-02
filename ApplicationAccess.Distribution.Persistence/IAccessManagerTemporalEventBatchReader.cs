/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Distribution.Persistence
{
    /// <summary>
    /// Defines methods to read events from an AccessManager instance persistent storage in batches.  
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerTemporalEventBatchReader<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Retrieves an <see cref="AccessManagerState"/> representing the event creating the first/initial state of the AccessManager.
        /// </summary>
        /// <returns>The <see cref="AccessManagerState"/>.</returns>
        AccessManagerState GetInitialState();

        /// <summary>
        /// Retrieves the <see cref="AccessManagerState"/> representing the event and state immediately after that specified. 
        /// </summary>
        /// <param name="inputState">The state to get the next event/state for.</param>
        /// <returns>The next event/state, or null of the specified state is the latest.</returns>
        AccessManagerState GetNextStateAfter(AccessManagerState inputState);

        /// <summary>
        /// Retrieves the sequence of events which follow (and include) the specified event/state.
        /// </summary>
        /// <param name="initialState">The <see cref="AccessManagerState"/> representing the first event/state in the sequence.</param>
        /// <param name="eventCount">The number of events to retrieve (including that specified in <paramref name="initialState"/>).</param>
        /// <returns>A tuple containing: the sequence of events in order of ascending date/time and including that specified in <paramref name="initialState"/>, and the <see cref="AccessManagerState"/> representing the next event/state after the last one in the sequence (or null if the last one in the sequence is the latest).</returns>
        Tuple<IList<TemporalEventBufferItemBase>, AccessManagerState> GetEvents(AccessManagerState initialState, Int32 eventCount);

        /// <summary>
        /// Retrieves the sequence of all events which follow (and include) the specified event/state.
        /// </summary>
        /// <param name="initialState">The <see cref="AccessManagerState"/> representing the first event/state in the sequence.</param>
        /// <returns>The sequence of events in order of ascending date/time, and including that specified in <paramref name="initialState"/>, or an empty list if the event/state represented by <paramref name="initialState"/> is the latest.</returns>
        IList<TemporalEventBufferItemBase> GetEvents(AccessManagerState initialState);
    }
}
