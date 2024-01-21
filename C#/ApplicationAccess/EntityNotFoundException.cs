/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
    /// The exception that is thrown when an entity specified in an parameter/argument does not exist.
    /// </summary>
    public class EntityNotFoundException : ArgumentException
    {
        /// <summary>The type of the entity which does not exist.</summary>
        protected String entityType;
        /// <summary>The entity which does not exist.</summary>
        protected String entity;

        /// <summary>
        /// The type of the entity which does not exist.
        /// </summary>
        public String EntityType
        {
            get
            {
                return entityType;
            }
        }

        /// <summary>
        /// The entity which does not exist.
        /// </summary>
        public String Entity
        {
            get
            {
                return entity;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.EntityNotFoundException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="parameterName">The name of the parameter containing the invalid entity.</param>
        /// <param name="entityType">The type of the entity which does not exist.</param>
        /// <param name="entity">The entity which does not exist.</param>
        public EntityNotFoundException(String message, String parameterName, String entityType, String entity)
            : base(message, parameterName)
        {
            this.entityType = entityType;
            this.entity = entity;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.EntityNotFoundException class.
        /// </summary>
        /// <param name="message">The message that describes the error.</param>
        /// <param name="parameterName">The name of the parameter containing the invalid entity.</param>
        /// <param name="entityType">The type of the entity which does not exist.</param>
        /// <param name="entity">The entity which does not exist.</param>
        /// <param name="innerException">The exception that is the cause of the current exception.</param>
        public EntityNotFoundException(String message, String parameterName, String entityType, String entity, Exception innerException)
            : base(message, parameterName, innerException)
        {
            this.entityType = entityType;
            this.entity = entity;
        }
    }
}
