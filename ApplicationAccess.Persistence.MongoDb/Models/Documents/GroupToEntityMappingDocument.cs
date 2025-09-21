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

namespace ApplicationAccess.Persistence.MongoDb.Models.Documents
{
    /// <summary>
    /// Holds a MongoDB document which maps a group to an entity.
    /// </summary>
    public record GroupToEntityMappingDocument : GroupDocument
    {
        #pragma warning disable 1591

        public required String EntityType { get; init; }

        public required String Entity { get; init; }

        #pragma warning restore 1591
    }
}
