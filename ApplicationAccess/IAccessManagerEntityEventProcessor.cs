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

namespace ApplicationAccess
{
    /// <summary>
    /// Defines methods to process events which change entity-based structures in an AccessManager implementation.
    /// </summary>
    public interface IAccessManagerEntityEventProcessor
    {
        /// <summary>
        /// Adds an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        void AddEntityType(String entityType);

        /// <summary>
        /// Removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        void RemoveEntityType(String entityType);

        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        void AddEntity(String entityType, String entity);

        /// <summary>
        /// Removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        void RemoveEntity(String entityType, String entity);
    }
}
