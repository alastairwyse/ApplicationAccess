﻿/*
 * Copyright 2023 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Hosting.Models.DataTransferObjects
{
    /// <summary>
    /// DTO container class holding a group, an entity type, and an entity of that type.
    /// </summary>
    public class GroupAndEntity<TGroup> : EntityTypeAndEntity
    {
        public TGroup Group { get; set; }

        public GroupAndEntity(TGroup group, string entityType, string entity)
            : base(entityType, entity)
        {
            Group = group;
        }
    }
}
