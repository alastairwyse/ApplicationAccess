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
        // TODO: 2022-10-24
        //   Should sets to postfixed with 'set'... e.g. 'UsersSet'?
        //   Should 'Entities' be 'EntitiyMap' since it holds a mapping
        //   What to do for storing counts against these... e.g. where the structure has a KeyCount and ValueCount?

        Users, 
        Groups, 
        UserToGroupMap, 
        GroupToGroupMap, 
        UserToComponentMap, 
        GroupToComponentMap, 
        Entities, 
        UserToEntityMap, 
        GroupToEntityMap
    }
}
