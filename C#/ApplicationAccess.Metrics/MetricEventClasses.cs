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
using ApplicationMetrics;

namespace ApplicationAccess.Metrics
{
    #pragma warning disable 1591

    #region IAccessManager Metrics

    /// <summary>
    /// Count metric which records a call to the AddUser() method.
    /// </summary>
    public class UserAdded : CountMetric
    {
        protected static String staticName = "UserAdded";
        protected static String staticDescription = "A call to the AddUser() method";

        public UserAdded()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the AddUser() method.
    /// </summary>
    public class UserAddTime : IntervalMetric
    {
        protected static String staticName = "UserAddTime";
        protected static String staticDescription = "The time taken to execute the AddUser() method";

        public UserAddTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the RemoveUser() method.
    /// </summary>
    public class UserRemoved : CountMetric
    {
        protected static String staticName = "UserRemoved";
        protected static String staticDescription = "A call to the RemoveUser() method";

        public UserRemoved()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the RemoveUser() method.
    /// </summary>
    public class UserRemoveTime : IntervalMetric
    {
        protected static String staticName = "UserRemoveTime";
        protected static String staticDescription = "The time taken to execute the RemoveUser() method";

        public UserRemoveTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the AddGroup() method.
    /// </summary>
    public class GroupAdded : CountMetric
    {
        protected static String staticName = "GroupAdded";
        protected static String staticDescription = "A call to the AddGroup() method";

        public GroupAdded()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the AddGroup() method.
    /// </summary>
    public class GroupAddTime : IntervalMetric
    {
        protected static String staticName = "GroupAddTime";
        protected static String staticDescription = "The time taken to execute the AddGroup() method";

        public GroupAddTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the RemoveGroup() method.
    /// </summary>
    public class GroupRemoved : CountMetric
    {
        protected static String staticName = "GroupRemoved";
        protected static String staticDescription = "A call to the RemoveGroup() method";

        public GroupRemoved()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the RemoveGroup() method.
    /// </summary>
    public class GroupRemoveTime : IntervalMetric
    {
        protected static String staticName = "GroupRemoveTime";
        protected static String staticDescription = "The time taken to execute the RemoveGroup() method";

        public GroupRemoveTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the AddUserToGroupMapping() method.
    /// </summary>
    public class UserToGroupMappingAdded : CountMetric
    {
        protected static String staticName = "UserToGroupMappingAdded";
        protected static String staticDescription = "A call to the AddUserToGroupMapping() method";

        public UserToGroupMappingAdded()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the AddUserToGroupMapping() method.
    /// </summary>
    public class UserToGroupMappingAddTime : IntervalMetric
    {
        protected static String staticName = "UserToGroupMappingAddTime";
        protected static String staticDescription = "The time taken to execute the AddUserToGroupMapping() method";

        public UserToGroupMappingAddTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the RemoveUserToGroupMapping() method.
    /// </summary>
    public class UserToGroupMappingRemoved : CountMetric
    {
        protected static String staticName = "UserToGroupMappingRemoved";
        protected static String staticDescription = "A call to the RemoveUserToGroupMapping() method";

        public UserToGroupMappingRemoved()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the RemoveUserToGroupMapping() method.
    /// </summary>
    public class UserToGroupMappingRemoveTime : IntervalMetric
    {
        protected static String staticName = "UserToGroupMappingRemoveTime";
        protected static String staticDescription = "The time taken to execute the RemoveUserToGroupMapping() method";

        public UserToGroupMappingRemoveTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the AddGroupToGroupMapping() method.
    /// </summary>
    public class GroupToGroupMappingAdded : CountMetric
    {
        protected static String staticName = "GroupToGroupMappingAdded";
        protected static String staticDescription = "A call to the AddGroupToGroupMapping() method";

        public GroupToGroupMappingAdded()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the AddGroupToGroupMapping() method.
    /// </summary>
    public class GroupToGroupMappingAddTime : IntervalMetric
    {
        protected static String staticName = "GroupToGroupMappingAddTime";
        protected static String staticDescription = "The time taken to execute the AddGroupToGroupMapping() method";

        public GroupToGroupMappingAddTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the RemoveGroupToGroupMapping() method.
    /// </summary>
    public class GroupToGroupMappingRemoved : CountMetric
    {
        protected static String staticName = "GroupToGroupMappingRemoved";
        protected static String staticDescription = "A call to the RemoveGroupToGroupMapping() method";

        public GroupToGroupMappingRemoved()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the RemoveGroupToGroupMapping() method.
    /// </summary>
    public class GroupToGroupMappingRemoveTime : IntervalMetric
    {
        protected static String staticName = "GroupToGroupMappingRemoveTime";
        protected static String staticDescription = "The time taken to execute the RemoveGroupToGroupMapping() method";

        public GroupToGroupMappingRemoveTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the AddUserToApplicationComponentAndAccessLevelMapping() method.
    /// </summary>
    public class UserToApplicationComponentAndAccessLevelMappingAdded : CountMetric
    {
        protected static String staticName = "UserToApplicationComponentAndAccessLevelMappingAdded";
        protected static String staticDescription = "A call to the AddUserToApplicationComponentAndAccessLevelMapping() method";

        public UserToApplicationComponentAndAccessLevelMappingAdded()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the AddUserToApplicationComponentAndAccessLevelMapping() method.
    /// </summary>
    public class UserToApplicationComponentAndAccessLevelMappingAddTime : IntervalMetric
    {
        protected static String staticName = "UserToApplicationComponentAndAccessLevelMappingAddTime";
        protected static String staticDescription = "The time taken to execute the AddUserToApplicationComponentAndAccessLevelMapping() method";

        public UserToApplicationComponentAndAccessLevelMappingAddTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the RemoveUserToApplicationComponentAndAccessLevelMapping() method.
    /// </summary>
    public class UserToApplicationComponentAndAccessLevelMappingRemoved : CountMetric
    {
        protected static String staticName = "UserToApplicationComponentAndAccessLevelMappingRemoved";
        protected static String staticDescription = "A call to the RemoveUserToApplicationComponentAndAccessLevelMapping() method";

        public UserToApplicationComponentAndAccessLevelMappingRemoved()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the RemoveUserToApplicationComponentAndAccessLevelMapping() method.
    /// </summary>
    public class UserToApplicationComponentAndAccessLevelMappingRemoveTime : IntervalMetric
    {
        protected static String staticName = "UserToApplicationComponentAndAccessLevelMappingRemoveTime";
        protected static String staticDescription = "The time taken to execute the RemoveUserToApplicationComponentAndAccessLevelMapping() method";

        public UserToApplicationComponentAndAccessLevelMappingRemoveTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the AddGroupToApplicationComponentAndAccessLevelMapping() method.
    /// </summary>
    public class GroupToApplicationComponentAndAccessLevelMappingAdded : CountMetric
    {
        protected static String staticName = "GroupToApplicationComponentAndAccessLevelMappingAdded";
        protected static String staticDescription = "A call to the AddGroupToApplicationComponentAndAccessLevelMapping() method";

        public GroupToApplicationComponentAndAccessLevelMappingAdded()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the AddGroupToApplicationComponentAndAccessLevelMapping() method.
    /// </summary>
    public class GroupToApplicationComponentAndAccessLevelMappingAddTime : IntervalMetric
    {
        protected static String staticName = "GroupToApplicationComponentAndAccessLevelMappingAddTime";
        protected static String staticDescription = "The time taken to execute the AddGroupToApplicationComponentAndAccessLevelMapping() method";

        public GroupToApplicationComponentAndAccessLevelMappingAddTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the RemoveGroupToApplicationComponentAndAccessLevelMapping() method.
    /// </summary>
    public class GroupToApplicationComponentAndAccessLevelMappingRemoved : CountMetric
    {
        protected static String staticName = "GroupToApplicationComponentAndAccessLevelMappingRemoved";
        protected static String staticDescription = "A call to the RemoveGroupToApplicationComponentAndAccessLevelMapping() method";

        public GroupToApplicationComponentAndAccessLevelMappingRemoved()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }


    /// <summary>
    /// Interval metric which records the time taken to execute the RemoveGroupToApplicationComponentAndAccessLevelMapping() method.
    /// </summary>
    public class GroupToApplicationComponentAndAccessLevelMappingRemoveTime : IntervalMetric
    {
        protected static String staticName = "GroupToApplicationComponentAndAccessLevelMappingRemoveTime";
        protected static String staticDescription = "The time taken to execute the RemoveGroupToApplicationComponentAndAccessLevelMapping() method";

        public GroupToApplicationComponentAndAccessLevelMappingRemoveTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the AddEntityType() method.
    /// </summary>
    public class EntityTypeAdded : CountMetric
    {
        protected static String staticName = "EntityTypeAdded";
        protected static String staticDescription = "A call to the AddEntityType() method";

        public EntityTypeAdded()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the AddEntityType() method.
    /// </summary>
    public class EntityTypeAddTime : IntervalMetric
    {
        protected static String staticName = "EntityTypeAddTime";
        protected static String staticDescription = "The time taken to execute the AddEntityType() method";

        public EntityTypeAddTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the RemoveEntityType() method.
    /// </summary>
    public class EntityTypeRemoved : CountMetric
    {
        protected static String staticName = "EntityTypeRemoved";
        protected static String staticDescription = "A call to the RemoveEntityType() method";

        public EntityTypeRemoved()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the RemoveEntityType() method.
    /// </summary>
    public class EntityTypeRemoveTime : IntervalMetric
    {
        protected static String staticName = "EntityTypeRemoveTime";
        protected static String staticDescription = "The time taken to execute the RemoveEntityType() method";

        public EntityTypeRemoveTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the AddEntity() method.
    /// </summary>
    public class EntityAdded : CountMetric
    {
        protected static String staticName = "EntityAdded";
        protected static String staticDescription = "A call to the AddEntity() method";

        public EntityAdded()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the AddEntity() method.
    /// </summary>
    public class EntityAddTime : IntervalMetric
    {
        protected static String staticName = "EntityAddTime";
        protected static String staticDescription = "The time taken to execute the AddEntity() method";

        public EntityAddTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the RemoveEntity() method.
    /// </summary>
    public class EntityRemoved : CountMetric
    {
        protected static String staticName = "EntityRemoved";
        protected static String staticDescription = "A call to the RemoveEntity() method";

        public EntityRemoved()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the RemoveEntity() method.
    /// </summary>
    public class EntityRemoveTime : IntervalMetric
    {
        protected static String staticName = "EntityRemoveTime";
        protected static String staticDescription = "The time taken to execute the RemoveEntity() method";

        public EntityRemoveTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the AddUserToEntityMapping() method.
    /// </summary>
    public class UserToEntityMappingAdded : CountMetric
    {
        protected static String staticName = "UserToEntityMappingAdded";
        protected static String staticDescription = "A call to the AddUserToEntityMapping() method";

        public UserToEntityMappingAdded()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the AddUserToEntityMapping() method.
    /// </summary>
    public class UserToEntityMappingAddTime : IntervalMetric
    {
        protected static String staticName = "UserToEntityMappingAddTime";
        protected static String staticDescription = "The time taken to execute the AddUserToEntityMapping() method";

        public UserToEntityMappingAddTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the RemoveUserToEntityMapping() method.
    /// </summary>
    public class UserToEntityMappingRemoved : CountMetric
    {
        protected static String staticName = "UserToEntityMappingRemoved";
        protected static String staticDescription = "A call to the RemoveUserToEntityMapping() method";

        public UserToEntityMappingRemoved()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the RemoveUserToEntityMapping() method.
    /// </summary>
    public class UserToEntityMappingRemoveTime : IntervalMetric
    {
        protected static String staticName = "UserToEntityMappingRemoveTime";
        protected static String staticDescription = "The time taken to execute the RemoveUserToEntityMapping() method";

        public UserToEntityMappingRemoveTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the AddGroupToEntityMapping() method.
    /// </summary>
    public class GroupToEntityMappingAdded : CountMetric
    {
        protected static String staticName = "GroupToEntityMappingAdded";
        protected static String staticDescription = "A call to the AddGroupToEntityMapping() method";

        public GroupToEntityMappingAdded()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the AddGroupToEntityMapping() method.
    /// </summary>
    public class GroupToEntityMappingAddTime : IntervalMetric
    {
        protected static String staticName = "GroupToEntityMappingAddTime";
        protected static String staticDescription = "The time taken to execute the AddGroupToEntityMapping() method";

        public GroupToEntityMappingAddTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the RemoveGroupToEntityMapping() method.
    /// </summary>
    public class GroupToEntityMappingRemoved : CountMetric
    {
        protected static String staticName = "GroupToEntityMappingRemoved";
        protected static String staticDescription = "A call to the RemoveGroupToEntityMapping() method";

        public GroupToEntityMappingRemoved()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the RemoveGroupToEntityMapping() method.
    /// </summary>
    public class GroupToEntityMappingRemoveTime : IntervalMetric
    {
        protected static String staticName = "GroupToEntityMappingRemoveTime";
        protected static String staticDescription = "The time taken to execute the RemoveGroupToEntityMapping() method";

        public GroupToEntityMappingRemoveTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a get on the Users property.
    /// </summary>
    public class UsersPropertyQuery : CountMetric
    {
        protected static String staticName = "UsersPropertyQuery";
        protected static String staticDescription = "A get on the Users property";

        public UsersPropertyQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the Users property.
    /// </summary>
    public class UsersPropertyQueryTime : IntervalMetric
    {
        protected static String staticName = "UsersPropertyQueryTime";
        protected static String staticDescription = "The time taken to execute the Users property";

        public UsersPropertyQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a get on the Groups property.
    /// </summary>
    public class GroupsPropertyQuery : CountMetric
    {
        protected static String staticName = "GroupsPropertyQuery";
        protected static String staticDescription = "A get on the Groups property";

        public GroupsPropertyQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the Groups property.
    /// </summary>
    public class GroupsPropertyQueryTime : IntervalMetric
    {
        protected static String staticName = "GroupsPropertyQueryTime";
        protected static String staticDescription = "The time taken to execute the Groups property";

        public GroupsPropertyQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a get on the EntityTypes property.
    /// </summary>
    public class EntityTypesPropertyQuery : CountMetric
    {
        protected static String staticName = "EntityTypesPropertyQuery";
        protected static String staticDescription = "A get on the EntityTypes property";

        public EntityTypesPropertyQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the EntityTypes property.
    /// </summary>
    public class EntityTypesPropertyQueryTime : IntervalMetric
    {
        protected static String staticName = "EntityTypesPropertyQueryTime";
        protected static String staticDescription = "The time taken to execute the EntityTypes property";

        public EntityTypesPropertyQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the ContainsUser() method.
    /// </summary>
    public class ContainsUserQuery : CountMetric
    {
        protected static String staticName = "ContainsUserQuery";
        protected static String staticDescription = "A call to the ContainsUser() method";

        public ContainsUserQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the ContainsUser() method.
    /// </summary>
    public class ContainsUserQueryTime : IntervalMetric
    {
        protected static String staticName = "ContainsUserQueryTime";
        protected static String staticDescription = "The time taken to execute the ContainsUser() method";

        public ContainsUserQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the ContainsGroup() method.
    /// </summary>
    public class ContainsGroupQuery : CountMetric
    {
        protected static String staticName = "ContainsGroupQuery";
        protected static String staticDescription = "A call to the ContainsGroup() method";

        public ContainsGroupQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the ContainsGroup() method.
    /// </summary>
    public class ContainsGroupQueryTime : IntervalMetric
    {
        protected static String staticName = "ContainsGroupQueryTime";
        protected static String staticDescription = "The time taken to execute the ContainsGroup() method";

        public ContainsGroupQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetUserToGroupMappings() method.
    /// </summary>
    public class GetUserToGroupMappingsQuery : CountMetric
    {
        protected static String staticName = "GetUserToGroupMappingsQuery";
        protected static String staticDescription = "A call to the GetUserToGroupMappings() method";

        public GetUserToGroupMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetUserToGroupMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetUserToGroupMappingsWithIndirectMappingsQuery : CountMetric
    {
        protected static String staticName = "GetUserToGroupMappingsWithIndirectMappingsQuery";
        protected static String staticDescription = "A call to the GetUserToGroupMappings() method with the 'includeIndirectMappings' parameter set true";

        public GetUserToGroupMappingsWithIndirectMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetUserToGroupMappings() method.
    /// </summary>
    public class GetUserToGroupMappingsQueryTime : IntervalMetric
    {
        protected static String staticName = "GetUserToGroupMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetUserToGroupMappings() method";

        public GetUserToGroupMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetUserToGroupMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetUserToGroupMappingsWithIndirectMappingsQueryTime : IntervalMetric
    {
        protected static String staticName = "GetUserToGroupMappingsWithIndirectMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetUserToGroupMappings() method with the 'includeIndirectMappings' parameter set true";

        public GetUserToGroupMappingsWithIndirectMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToGroupMappings() method.
    /// </summary>
    public class GetGroupToGroupMappingsQuery : CountMetric
    {
        protected static String staticName = "GetGroupToGroupMappingsQuery";
        protected static String staticDescription = "A call to the GetGroupToGroupMappings() method";

        public GetGroupToGroupMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToGroupMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetGroupToGroupMappingsWithIndirectMappingsQuery : CountMetric
    {
        protected static String staticName = "GetGroupToGroupMappingsWithIndirectMappingsQuery";
        protected static String staticDescription = "A call to the GetGroupToGroupMappings() method with the 'includeIndirectMappings' parameter set true";

        public GetGroupToGroupMappingsWithIndirectMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToGroupMappings() method.
    /// </summary>
    public class GetGroupToGroupMappingsQueryTime : IntervalMetric
    {
        protected static String staticName = "GetGroupToGroupMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToGroupMappings() method";

        public GetGroupToGroupMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToGroupMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetGroupToGroupMappingsWithIndirectMappingsQueryTime : IntervalMetric
    {
        protected static String staticName = "GetGroupToGroupMappingsWithIndirectMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToGroupMappings() method with the 'includeIndirectMappings' parameter set true";

        public GetGroupToGroupMappingsWithIndirectMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetUserToApplicationComponentAndAccessLevelMappings() method.
    /// </summary>
    public class GetUserToApplicationComponentAndAccessLevelMappingsQuery : CountMetric
    {
        protected static String staticName = "GetUserToApplicationComponentAndAccessLevelMappingsQuery";
        protected static String staticDescription = "A call to the GetUserToApplicationComponentAndAccessLevelMappings() method";

        public GetUserToApplicationComponentAndAccessLevelMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetUserToApplicationComponentAndAccessLevelMappings() method.
    /// </summary>
    public class GetUserToApplicationComponentAndAccessLevelMappingsQueryTime : IntervalMetric
    {
        protected static String staticName = "GetUserToApplicationComponentAndAccessLevelMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetUserToApplicationComponentAndAccessLevelMappings() method";

        public GetUserToApplicationComponentAndAccessLevelMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToApplicationComponentAndAccessLevelMappings() method.
    /// </summary>
    public class GetGroupToApplicationComponentAndAccessLevelMappingsQuery : CountMetric
    {
        protected static String staticName = "GetGroupToApplicationComponentAndAccessLevelMappingsQuery";
        protected static String staticDescription = "A call to the GetGroupToApplicationComponentAndAccessLevelMappings() method";

        public GetGroupToApplicationComponentAndAccessLevelMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToApplicationComponentAndAccessLevelMappings() method.
    /// </summary>
    public class GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime : IntervalMetric
    {
        protected static String staticName = "GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToApplicationComponentAndAccessLevelMappings() method";

        public GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the ContainsEntityType() method.
    /// </summary>
    public class ContainsEntityTypeQuery : CountMetric
    {
        protected static String staticName = "ContainsEntityTypeQuery";
        protected static String staticDescription = "A call to the ContainsEntityType() method";

        public ContainsEntityTypeQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the ContainsEntityType() method.
    /// </summary>
    public class ContainsEntityTypeQueryTime : IntervalMetric
    {
        protected static String staticName = "ContainsEntityTypeQueryTime";
        protected static String staticDescription = "The time taken to execute the ContainsEntityType() method";

        public ContainsEntityTypeQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetEntities() method.
    /// </summary>
    public class GetEntitiesQuery : CountMetric
    {
        protected static String staticName = "GetEntitiesQuery";
        protected static String staticDescription = "A call to the GetEntities() method";

        public GetEntitiesQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetEntities() method.
    /// </summary>
    public class GetEntitiesQueryTime : IntervalMetric
    {
        protected static String staticName = "GetEntitiesQueryTime";
        protected static String staticDescription = "The time taken to execute the GetEntities() method";

        public GetEntitiesQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the ContainsEntity() method.
    /// </summary>
    public class ContainsEntityQuery : CountMetric
    {
        protected static String staticName = "ContainsEntityQuery";
        protected static String staticDescription = "A call to the ContainsEntity() method";

        public ContainsEntityQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the ContainsEntity() method.
    /// </summary>
    public class ContainsEntityQueryTime : IntervalMetric
    {
        protected static String staticName = "ContainsEntityQueryTime";
        protected static String staticDescription = "The time taken to execute the ContainsEntity() method";

        public ContainsEntityQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetUserToEntityMappingsForUser() method.
    /// </summary>
    public class GetUserToEntityMappingsForUserQuery : CountMetric
    {
        protected static String staticName = "GetUserToEntityMappingsForUserQuery";
        protected static String staticDescription = "A call to the GetUserToEntityMappingsForUser() method";

        public GetUserToEntityMappingsForUserQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetUserToEntityMappingsForUser() method.
    /// </summary>
    public class GetUserToEntityMappingsForUserQueryTime : IntervalMetric
    {
        protected static String staticName = "GetUserToEntityMappingsForUserQueryTime";
        protected static String staticDescription = "The time taken to execute the GetUserToEntityMappingsForUser() method";

        public GetUserToEntityMappingsForUserQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetUserToEntityMappingsForUserAndEntityType() method.
    /// </summary>
    public class GetUserToEntityMappingsForUserAndEntityTypeQuery : CountMetric
    {
        protected static String staticName = "GetUserToEntityMappingsForUserAndEntityTypeQuery";
        protected static String staticDescription = "A call to the GetUserToEntityMappingsForUserAndEntityType() method";

        public GetUserToEntityMappingsForUserAndEntityTypeQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetUserToEntityMappingsForUserAndEntityType() method.
    /// </summary>
    public class GetUserToEntityMappingsForUserAndEntityTypeQueryTime : IntervalMetric
    {
        protected static String staticName = "GetUserToEntityMappingsForUserAndEntityTypeQueryTime";
        protected static String staticDescription = "The time taken to execute the GetUserToEntityMappingsForUserAndEntityType() method";

        public GetUserToEntityMappingsForUserAndEntityTypeQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToEntityMappingsForGroup() method.
    /// </summary>
    public class GetGroupToEntityMappingsForGroupQuery : CountMetric
    {
        protected static String staticName = "GetGroupToEntityMappingsForGroupQuery";
        protected static String staticDescription = "A call to the GetGroupToEntityMappingsForGroup() method";

        public GetGroupToEntityMappingsForGroupQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToEntityMappingsForGroup() method.
    /// </summary>
    public class GetGroupToEntityMappingsForGroupQueryTime : IntervalMetric
    {
        protected static String staticName = "GetGroupToEntityMappingsForGroupQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToEntityMappingsForGroup() method";

        public GetGroupToEntityMappingsForGroupQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToEntityMappingsForGroupAndEntityType() method.
    /// </summary>
    public class GetGroupToEntityMappingsForGroupAndEntityTypeQuery : CountMetric
    {
        protected static String staticName = "GetGroupToEntityMappingsForGroupAndEntityTypeQuery";
        protected static String staticDescription = "A call to the GetGroupToEntityMappingsForGroupAndEntityType() method";

        public GetGroupToEntityMappingsForGroupAndEntityTypeQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToEntityMappingsForGroupAndEntityType() method.
    /// </summary>
    public class GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime : IntervalMetric
    {
        protected static String staticName = "GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToEntityMappingsForGroupAndEntityType() method";

        public GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the HasAccessToApplicationComponent() method.
    /// </summary>
    public class HasAccessToApplicationComponentQuery : CountMetric
    {
        protected static String staticName = "HasAccessToApplicationComponentQuery";
        protected static String staticDescription = "A call to the HasAccessToApplicationComponent() method";

        public HasAccessToApplicationComponentQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the HasAccessToApplicationComponent() method.
    /// </summary>
    public class HasAccessToApplicationComponentQueryTime : IntervalMetric
    {
        protected static String staticName = "HasAccessToApplicationComponentQueryTime";
        protected static String staticDescription = "The time taken to execute the HasAccessToApplicationComponent() method";

        public HasAccessToApplicationComponentQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the HasAccessToEntity() method.
    /// </summary>
    public class HasAccessToEntityQuery : CountMetric
    {
        protected static String staticName = "HasAccessToEntityQuery";
        protected static String staticDescription = "A call to the HasAccessToEntity() method";

        public HasAccessToEntityQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the HasAccessToEntity() method.
    /// </summary>
    public class HasAccessToEntityQueryTime : IntervalMetric
    {
        protected static String staticName = "HasAccessToEntityQueryTime";
        protected static String staticDescription = "The time taken to execute the HasAccessToEntity() method";

        public HasAccessToEntityQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetApplicationComponentsAccessibleByUser() method.
    /// </summary>
    public class GetApplicationComponentsAccessibleByUserQuery : CountMetric
    {
        protected static String staticName = "GetApplicationComponentsAccessibleByUserQuery";
        protected static String staticDescription = "A call to the GetApplicationComponentsAccessibleByUser() method";

        public GetApplicationComponentsAccessibleByUserQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetApplicationComponentsAccessibleByUser() method.
    /// </summary>
    public class GetApplicationComponentsAccessibleByUserQueryTime : IntervalMetric
    {
        protected static String staticName = "GetApplicationComponentsAccessibleByUserQueryTime";
        protected static String staticDescription = "The time taken to execute the GetApplicationComponentsAccessibleByUser() method";

        public GetApplicationComponentsAccessibleByUserQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetApplicationComponentsAccessibleByGroup() method.
    /// </summary>
    public class GetApplicationComponentsAccessibleByGroupQuery : CountMetric
    {
        protected static String staticName = "GetApplicationComponentsAccessibleByGroupQuery";
        protected static String staticDescription = "A call to the GetApplicationComponentsAccessibleByGroup() method";

        public GetApplicationComponentsAccessibleByGroupQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetApplicationComponentsAccessibleByGroup() method.
    /// </summary>
    public class GetApplicationComponentsAccessibleByGroupQueryTime : IntervalMetric
    {
        protected static String staticName = "GetApplicationComponentsAccessibleByGroupQueryTime";
        protected static String staticDescription = "The time taken to execute the GetApplicationComponentsAccessibleByGroup() method";

        public GetApplicationComponentsAccessibleByGroupQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetEntitiesAccessibleByUser() method.
    /// </summary>
    public class GetEntitiesAccessibleByUserQuery : CountMetric
    {
        protected static String staticName = "GetEntitiesAccessibleByUserQuery";
        protected static String staticDescription = "A call to the GetEntitiesAccessibleByUser() method";

        public GetEntitiesAccessibleByUserQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetEntitiesAccessibleByUser() method.
    /// </summary>
    public class GetEntitiesAccessibleByUserQueryTime : IntervalMetric
    {
        protected static String staticName = "GetEntitiesAccessibleByUserQueryTime";
        protected static String staticDescription = "The time taken to execute the GetEntitiesAccessibleByUser() method";

        public GetEntitiesAccessibleByUserQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetEntitiesAccessibleByGroup() method.
    /// </summary>
    public class GetEntitiesAccessibleByGroupQuery : CountMetric
    {
        protected static String staticName = "GetEntitiesAccessibleByGroupQuery";
        protected static String staticDescription = "A call to the GetEntitiesAccessibleByGroup() method";

        public GetEntitiesAccessibleByGroupQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetEntitiesAccessibleByGroup() method.
    /// </summary>
    public class GetEntitiesAccessibleByGroupQueryTime : IntervalMetric
    {
        protected static String staticName = "GetEntitiesAccessibleByGroupQueryTime";
        protected static String staticDescription = "The time taken to execute the GetEntitiesAccessibleByGroup() method";

        public GetEntitiesAccessibleByGroupQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }
    #endregion

    #region AccessManager Metrics

    /// <summary>
    /// Status metric which records the number of users stored in an AccessManager instance.
    /// </summary>
    public class UsersStored : StatusMetric
    {
        protected static String staticName = "UsersStored";
        protected static String staticDescription = "The number of users stored in an AccessManager instance";

        public UsersStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of groups stored in an AccessManager instance.
    /// </summary>
    public class GroupsStored : StatusMetric
    {
        protected static String staticName = "GroupsStored";
        protected static String staticDescription = "The number of groups stored in an AccessManager instance";

        public GroupsStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of user to group mappings stored in an AccessManager instance.
    /// </summary>
    public class UserToGroupMappingsStored : StatusMetric
    {
        protected static String staticName = "UserToGroupMappingsStored";
        protected static String staticDescription = "The number of user to group mappings stored in an AccessManager instance";

        public UserToGroupMappingsStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of group to group mappings stored in an AccessManager instance.
    /// </summary>
    public class GroupToGroupMappingsStored : StatusMetric
    {
        protected static String staticName = "GroupToGroupMappingsStored";
        protected static String staticDescription = "The number of group to group mappings stored in an AccessManager instance";

        public GroupToGroupMappingsStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of user to application component and access level mappings stored in an AccessManager instance.
    /// </summary>
    public class UserToApplicationComponentAndAccessLevelMappingsStored : StatusMetric
    {
        protected static String staticName = "UserToApplicationComponentAndAccessLevelMappingsStored";
        protected static String staticDescription = "The number of user to application component and access level mappings stored in an AccessManager instance";

        public UserToApplicationComponentAndAccessLevelMappingsStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of group to application component and access level mappings stored in an AccessManager instance.
    /// </summary>
    public class GroupToApplicationComponentAndAccessLevelMappingsStored : StatusMetric
    {
        protected static String staticName = "GroupToApplicationComponentAndAccessLevelMappingsStored";
        protected static String staticDescription = "The number of group to application component and access level mappings stored in an AccessManager instance";

        public GroupToApplicationComponentAndAccessLevelMappingsStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of entity types stored in an AccessManager instance.
    /// </summary>
    public class EntityTypesStored : StatusMetric
    {
        protected static String staticName = "EntityTypesStored";
        protected static String staticDescription = "The number of entity types stored in an AccessManager instance";

        public EntityTypesStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of entities stored in an AccessManager instance.
    /// </summary>
    public class EntitiesStored : StatusMetric
    {
        protected static String staticName = "EntitiesStored";
        protected static String staticDescription = "The number of entities stored in an AccessManager instance";

        public EntitiesStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of user to entity mappings stored in an AccessManager instance.
    /// </summary>
    public class UserToEntityMappingsStored : StatusMetric
    {
        protected static String staticName = "UserToEntityMappingsStored";
        protected static String staticDescription = "The number of user to entity mappings stored in an AccessManager instance";

        public UserToEntityMappingsStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of group to entity mappings stored in an AccessManager instance.
    /// </summary>
    public class GroupToEntityMappingsStored : StatusMetric
    {
        protected static String staticName = "GroupToEntityMappingsStored";
        protected static String staticDescription = "The number of group to entity mappings stored in an AccessManager instance";

        public GroupToEntityMappingsStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #endregion

    #region DirectedGraph Metrics

    /// <summary>
    /// Status metric which records the number of leaf vertices stored in a DirectedGraph instance.
    /// </summary>
    public class LeafVerticesStored : StatusMetric
    {
        protected static String staticName = "LeafVerticesStored";
        protected static String staticDescription = "The number of leaf vertices stored in a DirectedGraph instance";

        public LeafVerticesStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of non-leaf vertices stored in a DirectedGraph instance.
    /// </summary>
    public class NonLeafVerticesStored : StatusMetric
    {
        protected static String staticName = "NonLeafVerticesStored";
        protected static String staticDescription = "The number of non-leaf vertices stored in a DirectedGraph instance";

        public NonLeafVerticesStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of leaf to non-leaf edges stored in a DirectedGraph instance.
    /// </summary>
    public class LeafToNonLeafEdgesStored : StatusMetric
    {
        protected static String staticName = "LeafToNonLeafEdgesStored";
        protected static String staticDescription = "The number of leaf to non-leaf edges stored in a DirectedGraph instance";

        public LeafToNonLeafEdgesStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of non-leaf to non-leaf edges stored in a DirectedGraph instance.
    /// </summary>
    public class NonLeafToNonLeafEdgesStored : StatusMetric
    {
        protected static String staticName = "NonLeafToNonLeafEdgesStored";
        protected static String staticDescription = "The number of non-leaf to non-leaf edges stored in a DirectedGraph instance";

        public NonLeafToNonLeafEdgesStored()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #endregion

    #pragma warning restore 1591
}
