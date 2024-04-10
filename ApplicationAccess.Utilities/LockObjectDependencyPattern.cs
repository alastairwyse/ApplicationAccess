/*
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
using System.Collections.Generic;
using System.Text;

namespace ApplicationAccess.Utilities
{
    /// <summary>
    /// Defines different patterns of dependency between objects with respect to acquiring mutual-exclusion locks.
    /// </summary>
    public enum LockObjectDependencyPattern
    {
        /// <summary>Acquire locks on a specified object, and the objects it depends on.</summary>
        ObjectAndObjectsItDependsOn,
        /// <summary>Acquire locks on a specified object, and the objects which depend on it.</summary>
        ObjectAndObjectsWhichAreDependentOnIt
    }
}
