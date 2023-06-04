/*
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

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Defines methods to store data elements managed by an AccessManager instance under test.
    /// </summary>
    public interface IDataElementStorer<TUser, TGroup, TComponent, TAccess> : 
        IAccessManagerUserEventProcessor<TUser, TGroup, TComponent, TAccess>, 
        IAccessManagerGroupEventProcessor<TGroup, TComponent, TAccess>,
        IAccessManagerGroupToGroupEventProcessor<TGroup>, 
        IAccessManagerEntityEventProcessor
    {
        Int32 UserCount { get; }

        Int32 GroupCount { get; }

        Int32 UserToGroupMappingCount { get; }

        Int32 GroupToGroupMappingCount { get; }

        Int32 UserToComponentMappingCount { get; }

        Int32 GroupToComponentMappingCount { get; }

        Int32 EntityTypeCount { get; }

        Int32 EntityCount { get; }

        Int32 UserToEntityMappingCount { get; }

        Int32 GroupToEntityMappingCount { get; }

        TUser GetRandomUser();

        TGroup GetRandomGroup();

        Tuple<TUser, TGroup> GetRandomUserToGroupMapping();

        Tuple<TGroup, TGroup> GetRandomGroupToGroupMapping();

        Tuple<TUser, TComponent, TAccess> GetRandomUserToApplicationComponentAndAccessLevelMapping();

        Tuple<TGroup, TComponent, TAccess> GetRandomGroupToApplicationComponentAndAccessLevelMapping();

        String GetRandomEntityType();

        Tuple<String, String> GetRandomEntity();

        Tuple<TUser, String, String> GetRandomUserToEntityMapping();

        Tuple<TGroup, String, String> GetRandomGroupToEntityMapping();
    }
}
