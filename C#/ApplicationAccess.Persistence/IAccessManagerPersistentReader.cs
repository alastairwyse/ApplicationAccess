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
using ApplicationAccess;

namespace ApplicationAccess.Persistence
{
    /// <summary>
    /// Defines methods to read an AccessManager class from persistent storage.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IAccessManagerPersistentReader<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Loads the access manager from persistent storage.
        /// </summary>
        /// <param name="accessManagerToLoadTo">The AccessManager instance to load in to.</param>
        /// <returns>Values representing the state of the access manager loaded.  The returned tuple contains 2 values: The id of the most recent event persisted into the access manager at the returned state, and the UTC timestamp the event occurred at.</returns>
        Tuple<Guid, DateTime> Load(AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo);
    }
}
