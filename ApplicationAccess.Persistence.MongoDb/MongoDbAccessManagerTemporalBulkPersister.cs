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
using ApplicationAccess.Persistence;
using ApplicationAccess.Persistence.Models;

namespace ApplicationAccess.Persistence.MongoDb
{
    /// <summary>
    /// An implementation of <see cref="IAccessManagerTemporalEventBulkPersister{TUser, TGroup, TComponent, TAccess}"/> which persists access manager events in bulk to, and allows reading of <see cref="AccessManagerBase{TUser, TGroup, TComponent, TAccess}"/> objects from a MongoDB database.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class MongoDbAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess> : IAccessManagerTemporalBulkPersister<TUser, TGroup, TComponent, TAccess>
    {
        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public void PersistEvents(IList<TemporalEventBufferItemBase> events, bool ignorePreExistingEvents)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public AccessManagerState Load(AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public AccessManagerState Load(Guid eventId, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public AccessManagerState Load(DateTime stateTime, AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToLoadTo)
        {
            throw new NotImplementedException();
        }

        #region Finalize / Dispose Methods

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
