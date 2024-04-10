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

using System.Collections.Generic;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Represents a structure that is used to store the data elements within an AccessManager (e.g. <see cref="Dictionary{TKey, TValue}"/> or <see cref="HashSet{T}"/>).
    /// </summary>
    public enum StorageStructure
    {
        Users, 
        Groups, 
        UserToGroupMap, 
        GroupToGroupMap,
        // Application components and access levels aren't actually stored in lookup structures in an AccessManager, but they're included here to facilitate proability calculations for operations which depend on them.
        ApplicationComponent, 
        AccessLevel, 
        UserToComponentMap, 
        GroupToComponentMap, 
        // Entity types and entities are actually stored in a single Dictionary, but will keep them as separate enums so that (for example) counts can be kept separately for each.
        EntityTypes,
        Entities,
        UserToEntityMap, 
        GroupToEntityMap
    }
}
