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

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Defines methods to generate parameters for AccessManager operations.
    /// </summary>
    public interface IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess>
    {
        TUser GenerateAddUserParameter();

        TUser GenerateContainsUserParameter();

        TUser GenerateRemoveUserParameter();

        TGroup GenerateAddGroupParameter();

        TGroup GenerateContainsGroupParameter();

        TGroup GenerateRemoveGroupParameter();

        Tuple<TUser, TGroup> GenerateAddUserToGroupMappingParameters();

        TUser GenerateGetUserToGroupMappingsParameter();

        Tuple<TUser, TGroup> GenerateRemoveUserToGroupMappingParameters();
        
        Tuple<TGroup, TGroup> GenerateAddGroupToGroupMappingParameters();

        TGroup GenerateGetGroupToGroupMappingsParameter();

        Tuple<TGroup, TGroup> GenerateRemoveGroupToGroupMappingParameters();

        Tuple<TUser, TComponent, TAccess> GenerateAddUserToApplicationComponentAndAccessLevelMappingParameters();

        TUser GenerateGetUserToApplicationComponentAndAccessLevelMappingsParameter();

        Tuple<TUser, TComponent, TAccess> GenerateRemoveUserToApplicationComponentAndAccessLevelMappingParameters();

        Tuple<TGroup, TComponent, TAccess> GenerateAddGroupToApplicationComponentAndAccessLevelMappingParameters();

        TGroup GenerateGetGroupToApplicationComponentAndAccessLevelMappingsParameter();

        Tuple<TGroup, TComponent, TAccess> GenerateRemoveGroupToApplicationComponentAndAccessLevelMappingParameters();

        String GenerateAddEntityTypeParameter();

        String GenerateContainsEntityTypeParameter();

        String GenerateRemoveEntityTypeParameter();

        Tuple<String, String> GenerateAddEntityParameters();

        String GenerateGetEntitiesParameter();

        Tuple<String, String> GenerateContainsEntityParameters();

        Tuple<String, String> GenerateRemoveEntityParameters();

        Tuple<TUser, String, String> GenerateAddUserToEntityMappingParameters();

        TUser GenerateGetUserToEntityMappingsParameter();

        Tuple<TUser, String> GenerateGetUserToEntityMappingsEntityTypeOverloadParameters();

        Tuple<TUser, String, String> GenerateRemoveUserToEntityMappingParameters();

        Tuple<TGroup, String, String> GenerateAddGroupToEntityMappingParameters();

        TGroup GenerateGetGroupToEntityMappingsParameter();

        Tuple<TGroup, String> GenerateGetGroupToEntityMappingsEntityTypeOverloadParameters();

        Tuple<TGroup, String, String> GenerateRemoveGroupToEntityMappingParameters();

        Tuple<TUser, TComponent, TAccess> GenerateHasAccessToApplicationComponentParameters();

        Tuple<TUser, String, String> GenerateHasAccessToEntityParameters();

        TUser GenerateGetApplicationComponentsAccessibleByUserParameter();

        TGroup GenerateGetApplicationComponentsAccessibleByGroupParameter();

        Tuple<TUser, String> GenerateGetEntitiesAccessibleByUserParameters();

        Tuple<TGroup, String> GenerateGetEntitiesAccessibleByGroupParameters();
    }
}
