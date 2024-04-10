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
using ApplicationAccess;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Defines methods to read the current or historic state of an AccessManager class from persistent storage.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> : IAccessManagerPersistentReader<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Loads the access manager with state corresponding to the specified event id from persistent storage.
        /// </summary>
        /// <param name="eventId">The id of the most recent event persisted into the access manager, at the desired state to load.</param>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        /// <returns>The state of the access manager loaded.</returns>
        /// <remarks>
        ///   <para>Any existing items and mappings stored in parameter 'accessManagerToLoadTo' will be cleared.</para>
        ///   <para>The AccessManager instance is passed as a parameter rather than returned from the method, to allow loading into types derived from AccessManager aswell as AccessManager itself.</para>
        /// </remarks>
        AccessManagerState Load(Guid eventId, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo);

        /// <summary>
        /// Loads the access manager with state corresponding to the specified timestamp from persistent storage.
        /// </summary>
        /// <param name="stateTime">The time equal to or sequentially after (in terms of event sequence) the state of the access manager to load.</param>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        /// <returns>The state of the access manager loaded.</returns>
        /// <remarks>
        ///   <para>Any existing items and mappings stored in parameter 'accessManagerToLoadTo' will be cleared.</para>
        ///   <para>The AccessManager instance is passed as a parameter rather than returned from the method, to allow loading into types derived from AccessManager aswell as AccessManager itself.</para>
        /// </remarks>
        AccessManagerState Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo);
    }
}
