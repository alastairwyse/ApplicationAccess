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
using System.Collections.Generic;

namespace ApplicationAccess
{
    /// <summary>
    ///  Defines methods which query the state of entity-based structures in an AccessManager implementation.
    /// </summary>
    public interface IAccessManagerEntityQueryProcessor
    {
        /// <summary>
        /// Returns a collection of all entity types in the access manager.
        /// </summary>
        IEnumerable<String> EntityTypes
        {
            get;
        }

        /// <summary>
        /// Returns true if the specified entity type exists.
        /// </summary>
        /// <param name="entityType">The entity type to check for.</param>
        /// <returns>True if the entity type exists.  False otherwise.</returns>
        Boolean ContainsEntityType(String entityType);

        /// <summary>
        /// Returns all entities of the specified type.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <returns>A collection of all entities of the specified type.</returns>
        IEnumerable<String> GetEntities(String entityType);

        /// <summary>
        /// Returns true if the specified entity exists.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to check for.</param>
        /// <returns>True if the entity exists.  False otherwise.</returns>
        Boolean ContainsEntity(String entityType, String entity);
    }
}
