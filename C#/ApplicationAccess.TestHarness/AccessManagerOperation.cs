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

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Represents an operation (method call or property get/set) which can be performed on an <see cref="AccessManagerBase{TUser, TGroup, TComponent, TAccess}"/> instance.
    /// </summary>
    public enum AccessManagerOperation
    {
        UsersPropertyGet, 
        GroupsPropertyGet, 
        EntityTypesPropertyGet, 
        AddUser, 
        ContainsUser, 
        RemoveUser, 
        AddGroup, 
        ContainsGroup, 
        RemoveGroup, 
        AddUserToGroupMapping, 
        GetUserToGroupMappings, 
        RemoveUserToGroupMapping, 
        AddGroupToGroupMapping, 
        GetGroupToGroupMappings, 
        RemoveGroupToGroupMapping, 
        AddUserToApplicationComponentAndAccessLevelMapping, 
        GetUserToApplicationComponentAndAccessLevelMappings, 
        RemoveUserToApplicationComponentAndAccessLevelMapping, 
        AddGroupToApplicationComponentAndAccessLevelMapping, 
        GetGroupToApplicationComponentAndAccessLevelMappings, 
        RemoveGroupToApplicationComponentAndAccessLevelMapping, 
        AddEntityType, 
        ContainsEntityType, 
        RemoveEntityType, 
        AddEntity, 
        GetEntities, 
        ContainsEntity, 
        RemoveEntity, 
        AddUserToEntityMapping, 
        GetUserToEntityMappings, 
        GetUserToEntityMappingsEntityTypeOverload, 
        RemoveUserToEntityMapping, 
        AddGroupToEntityMapping, 
        GetGroupToEntityMappings,
        GetGroupToEntityMappingsEntityTypeOverload, 
        RemoveGroupToEntityMapping, 
        HasAccessToApplicationComponent, 
        HasAccessToEntity, 
        GetApplicationComponentsAccessibleByUser, 
        GetApplicationComponentsAccessibleByGroup, 
        GetEntitiesAccessibleByUser,
        GetEntitiesAccessibleByUserEntityTypeOverload,
        GetEntitiesAccessibleByGroup,
        GetEntitiesAccessibleByGroupEntityTypeOverload
    }
}
