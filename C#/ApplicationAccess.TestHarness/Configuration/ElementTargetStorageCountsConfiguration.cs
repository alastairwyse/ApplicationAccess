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

namespace ApplicationAccess.TestHarness.Configuration
{
    #pragma warning disable 0649

    /// <summary>
    /// Model/container class holding test harness target element count configuration.
    /// </summary>
    class ElementTargetStorageCountsConfiguration
    {
        public Int32 Users;
        public Int32 Groups;
        public Int32 UserToGroupMap;
        public Int32 GroupToGroupMap;
        public Int32 UserToComponentMap;
        public Int32 GroupToComponentMap;
        public Int32 EntityTypes;
        public Int32 Entities;
        public Int32 UserToEntityMap;
        public Int32 GroupToEntityMap;
    }

    #pragma warning restore 0649
}
