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
using ApplicationAccess.Persistence;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Loads data from an <see cref="IAccessManagerPersistentReader{TUser, TGroup, TComponent, TAccess}"/> to a <see cref="DataElementStorer{TUser, TGroup, TComponent, TAccess}"/>.
    /// </summary>
    public class DataElementStorerLoader<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Loads data from an <see cref="IAccessManagerPersistentReader{TUser, TGroup, TComponent, TAccess}"/> to a <see cref="DataElementStorer{TUser, TGroup, TComponent, TAccess}"/>.
        /// </summary>
        /// <param name="reader">The <see cref="IAccessManagerPersistentReader{TUser, TGroup, TComponent, TAccess}"/> to load from.</param>
        /// <param name="dataElementStorer">The <see cref="DataElementStorer{TUser, TGroup, TComponent, TAccess}"/> to load to. </param>
        /// <param name="throwExceptionIfStorageIsEmpty">Throws an exception if the storage is empty.</param>
        public void Load(IAccessManagerTemporalPersistentReader<TUser, TGroup, TComponent, TAccess> reader, DataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer, Boolean throwExceptionIfStorageIsEmpty)
        {
            // Read into a temporary/intermediate AccessManager instance
            var intermediateAccessManager = new AccessManager<TUser, TGroup, TComponent, TAccess>();
            try
            {
                reader.Load(intermediateAccessManager);
            }
            catch (PersistentStorageEmptyException)
            {
                if (throwExceptionIfStorageIsEmpty == true)
                    throw;
            }
            catch (Exception e)
            {
                throw new Exception("Failed to load from AccessManager reader.", e);
            }

            // Copy all elements into the DataElementStorer
            foreach (TUser currentUser in intermediateAccessManager.Users)
            {
                dataElementStorer.AddUser(currentUser);
            }
            foreach (TGroup currentGroup in intermediateAccessManager.Groups)
            {
                dataElementStorer.AddGroup(currentGroup);
            }
            foreach (TUser currentUser in intermediateAccessManager.Users)
            {
                foreach (TGroup currentGroup in intermediateAccessManager.GetUserToGroupMappings(currentUser, false))
                {
                    dataElementStorer.AddUserToGroupMapping(currentUser, currentGroup);
                }
            }
            foreach (TGroup currentFromGroup in intermediateAccessManager.Groups)
            {
                foreach (TGroup currentToGroup in intermediateAccessManager.GetGroupToGroupMappings(currentFromGroup, false))
                {
                    dataElementStorer.AddGroupToGroupMapping(currentFromGroup, currentToGroup);
                }
            }
            foreach (TUser currentUser in intermediateAccessManager.Users)
            {
                foreach (Tuple<TComponent, TAccess> currentApplicationComponentAndAccessLevel in intermediateAccessManager.GetUserToApplicationComponentAndAccessLevelMappings(currentUser))
                {
                    dataElementStorer.AddUserToApplicationComponentAndAccessLevelMapping(currentUser, currentApplicationComponentAndAccessLevel.Item1, currentApplicationComponentAndAccessLevel.Item2);
                }
            }
            foreach (TGroup currentGroup in intermediateAccessManager.Groups)
            {
                foreach (Tuple<TComponent, TAccess> currentApplicationComponentAndAccessLevel in intermediateAccessManager.GetGroupToApplicationComponentAndAccessLevelMappings(currentGroup))
                {
                    dataElementStorer.AddGroupToApplicationComponentAndAccessLevelMapping(currentGroup, currentApplicationComponentAndAccessLevel.Item1, currentApplicationComponentAndAccessLevel.Item2);
                }
            }
            foreach (String currentEntityType in intermediateAccessManager.EntityTypes)
            {
                dataElementStorer.AddEntityType(currentEntityType);
            }
            foreach (String currentEntityType in intermediateAccessManager.EntityTypes)
            {
                foreach (String currentEntity in intermediateAccessManager.GetEntities(currentEntityType))
                {
                    dataElementStorer.AddEntity(currentEntityType, currentEntity);
                }
            }
            foreach (TUser currentUser in intermediateAccessManager.Users)
            {
                foreach (Tuple<String, String> currentEntity in intermediateAccessManager.GetUserToEntityMappings(currentUser))
                {
                    dataElementStorer.AddUserToEntityMapping(currentUser, currentEntity.Item1, currentEntity.Item2);
                }
            }
            foreach (TGroup currentGroup in intermediateAccessManager.Groups)
            {
                foreach (Tuple<String, String> currentEntity in intermediateAccessManager.GetGroupToEntityMappings(currentGroup))
                {
                    dataElementStorer.AddGroupToEntityMapping(currentGroup, currentEntity.Item1, currentEntity.Item2);
                }
            }
        }
    }
}
