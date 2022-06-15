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

    /// <summary>
    /// Count metric which records the total number of calls to the AddUser() method.
    /// </summary>
    public class UsersAdded : CountMetric
    {
        protected static String staticName = "UsersAdded";
        protected static String staticDescription = "The total number of calls to the AddUser() method";

        public UsersAdded()
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
    /// Count metric which records the total number of calls to the RemoveUser() method.
    /// </summary>
    public class UsersRemoved : CountMetric
    {
        protected static String staticName = "UsersRemoved";
        protected static String staticDescription = "The total number of calls to the RemoveUser() method";

        public UsersRemoved()
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
    /// Count metric which records the total number of calls to the AddGroup() method.
    /// </summary>
    public class GroupsAdded : CountMetric
    {
        protected static String staticName = "GroupsAdded";
        protected static String staticDescription = "The total number of calls to the AddGroup() method";

        public GroupsAdded()
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
    /// Count metric which records the total number of calls to the RemoveGroup() method.
    /// </summary>
    public class GroupsRemoved : CountMetric
    {
        protected static String staticName = "GroupsRemoved";
        protected static String staticDescription = "The total number of calls to the RemoveGroup() method";

        public GroupsRemoved()
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
    /// Count metric which records the total number of calls to the AddUserToGroupMapping() method.
    /// </summary>
    public class UserToGroupMappingsAdded : CountMetric
    {
        protected static String staticName = "UserToGroupMappingsAdded";
        protected static String staticDescription = "The total number of calls to the AddUserToGroupMapping() method";

        public UserToGroupMappingsAdded()
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
    /// Count metric which records the total number of calls to the RemoveUserToGroupMapping() method.
    /// </summary>
    public class UserToGroupMappingsRemoved : CountMetric
    {
        protected static String staticName = "UserToGroupMappingsRemoved";
        protected static String staticDescription = "The total number of calls to the RemoveUserToGroupMapping() method";

        public UserToGroupMappingsRemoved()
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
    /// Count metric which records the total number of calls to the AddGroupToGroupMapping() method.
    /// </summary>
    public class GroupToGroupMappingsAdded : CountMetric
    {
        protected static String staticName = "GroupToGroupMappingsAdded";
        protected static String staticDescription = "The total number of calls to the AddGroupToGroupMapping() method";

        public GroupToGroupMappingsAdded()
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
    /// Count metric which records the total number of calls to the RemoveGroupToGroupMapping() method.
    /// </summary>
    public class GroupToGroupMappingsRemoved : CountMetric
    {
        protected static String staticName = "GroupToGroupMappingsRemoved";
        protected static String staticDescription = "The total number of calls to the RemoveGroupToGroupMapping() method";

        public GroupToGroupMappingsRemoved()
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
    /// Count metric which records the total number of calls to the AddUserToApplicationComponentAndAccessLevelMapping() method.
    /// </summary>
    public class UserToApplicationComponentAndAccessLevelMappingsAdded : CountMetric
    {
        protected static String staticName = "UserToApplicationComponentAndAccessLevelMappingsAdded";
        protected static String staticDescription = "The total number of calls to the AddUserToApplicationComponentAndAccessLevelMapping() method";

        public UserToApplicationComponentAndAccessLevelMappingsAdded()
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
    /// Count metric which records the total number of calls to the RemoveUserToApplicationComponentAndAccessLevelMapping() method.
    /// </summary>
    public class UserToApplicationComponentAndAccessLevelMappingsRemoved : CountMetric
    {
        protected static String staticName = "UserToApplicationComponentAndAccessLevelMappingsRemoved";
        protected static String staticDescription = "The total number of calls to the RemoveUserToApplicationComponentAndAccessLevelMapping() method";

        public UserToApplicationComponentAndAccessLevelMappingsRemoved()
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
    /// Count metric which records the total number of calls to the AddGroupToApplicationComponentAndAccessLevelMapping() method.
    /// </summary>
    public class GroupToApplicationComponentAndAccessLevelMappingsAdded : CountMetric
    {
        protected static String staticName = "GroupToApplicationComponentAndAccessLevelMappingsAdded";
        protected static String staticDescription = "The total number of calls to the AddGroupToApplicationComponentAndAccessLevelMapping() method";

        public GroupToApplicationComponentAndAccessLevelMappingsAdded()
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
    /// Count metric which records the total number of calls to the RemoveGroupToApplicationComponentAndAccessLevelMapping() method.
    /// </summary>
    public class GroupToApplicationComponentAndAccessLevelMappingsRemoved : CountMetric
    {
        protected static String staticName = "GroupToApplicationComponentAndAccessLevelMappingsRemoved";
        protected static String staticDescription = "The total number of calls to the RemoveGroupToApplicationComponentAndAccessLevelMapping() method";

        public GroupToApplicationComponentAndAccessLevelMappingsRemoved()
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
    /// Count metric which records the total number of calls to the AddEntityType() method.
    /// </summary>
    public class EntityTypesAdded : CountMetric
    {
        protected static String staticName = "EntityTypesAdded";
        protected static String staticDescription = "The total number of calls to the AddEntityType() method";

        public EntityTypesAdded()
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
    /// Count metric which records the total number of calls to the RemoveEntityType() method.
    /// </summary>
    public class EntityTypesRemoved : CountMetric
    {
        protected static String staticName = "EntityTypesRemoved";
        protected static String staticDescription = "The total number of calls to the RemoveEntityType() method";

        public EntityTypesRemoved()
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
    /// Count metric which records the total number of calls to the AddEntity() method.
    /// </summary>
    public class EntitiesAdded : CountMetric
    {
        protected static String staticName = "EntitiesAdded";
        protected static String staticDescription = "The total number of calls to the AddEntity() method";

        public EntitiesAdded()
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
    /// Count metric which records the total number of calls to the RemoveEntity() method.
    /// </summary>
    public class EntitiesRemoved : CountMetric
    {
        protected static String staticName = "EntitiesRemoved";
        protected static String staticDescription = "The total number of calls to the RemoveEntity() method";

        public EntitiesRemoved()
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
    /// Count metric which records the total number of calls to the AddUserToEntityMapping() method.
    /// </summary>
    public class UserToEntityMappingsAdded : CountMetric
    {
        protected static String staticName = "UserToEntityMappingsAdded";
        protected static String staticDescription = "The total number of calls to the AddUserToEntityMapping() method";

        public UserToEntityMappingsAdded()
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
    /// Count metric which records the total number of calls to the RemoveUserToEntityMapping() method.
    /// </summary>
    public class UserToEntityMappingsRemoved : CountMetric
    {
        protected static String staticName = "UserToEntityMappingsRemoved";
        protected static String staticDescription = "The total number of calls to the RemoveUserToEntityMapping() method";

        public UserToEntityMappingsRemoved()
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
    /// Count metric which records the total number of calls to the AddGroupToEntityMapping() method.
    /// </summary>
    public class GroupToEntityMappingsAdded : CountMetric
    {
        protected static String staticName = "GroupToEntityMappingsAdded";
        protected static String staticDescription = "The total number of calls to the AddGroupToEntityMapping() method";

        public GroupToEntityMappingsAdded()
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
    /// Count metric which records the total number of calls to the RemoveGroupToEntityMapping() method.
    /// </summary>
    public class GroupToEntityMappingsRemoved : CountMetric
    {
        protected static String staticName = "GroupToEntityMappingsRemoved";
        protected static String staticDescription = "The total number of calls to the RemoveGroupToEntityMapping() method";

        public GroupToEntityMappingsRemoved()
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
    /// Count metric which records the total number of gets on the Users property.
    /// </summary>
    public class UsersPropertyQueries : CountMetric
    {
        protected static String staticName = "UsersPropertyQueries";
        protected static String staticDescription = "The total number of gets on the Users property";

        public UsersPropertyQueries()
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
    /// Count metric which records the total number of gets on the Groups property.
    /// </summary>
    public class GroupsPropertyQueries : CountMetric
    {
        protected static String staticName = "GroupsPropertyQueries";
        protected static String staticDescription = "The total number of gets on the Groups property";

        public GroupsPropertyQueries()
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
    /// Count metric which records the total number of gets on the EntityTypes property.
    /// </summary>
    public class EntityTypesPropertyQueries : CountMetric
    {
        protected static String staticName = "EntityTypesPropertyQueries";
        protected static String staticDescription = "The total number of gets on the EntityTypes property";

        public EntityTypesPropertyQueries()
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
    /// Count metric which records the total number of calls to the ContainsUser() method.
    /// </summary>
    public class ContainsUserQueries : CountMetric
    {
        protected static String staticName = "ContainsUserQueries";
        protected static String staticDescription = "The total number of calls to the ContainsUser() method";

        public ContainsUserQueries()
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
    /// Count metric which records the total number of calls to the ContainsGroup() method.
    /// </summary>
    public class ContainsGroupQueries : CountMetric
    {
        protected static String staticName = "ContainsGroupQueries";
        protected static String staticDescription = "The total number of calls to the ContainsGroup() method";

        public ContainsGroupQueries()
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
    /// Count metric which records the total number of calls to the GetUserToGroupMappings() method.
    /// </summary>
    public class GetUserToGroupMappingsQueries : CountMetric
    {
        protected static String staticName = "GetUserToGroupMappingsQueries";
        protected static String staticDescription = "The total number of calls to the GetUserToGroupMappings() method";

        public GetUserToGroupMappingsQueries()
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
    /// Count metric which records the total number of calls to the GetGroupToGroupMappings() method.
    /// </summary>
    public class GetGroupToGroupMappingsQueries : CountMetric
    {
        protected static String staticName = "GetGroupToGroupMappingsQueries";
        protected static String staticDescription = "The total number of calls to the GetGroupToGroupMappings() method";

        public GetGroupToGroupMappingsQueries()
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
    /// Count metric which records the total number of calls to the GetUserToApplicationComponentAndAccessLevelMappings() method.
    /// </summary>
    public class GetUserToApplicationComponentAndAccessLevelMappingsQueries : CountMetric
    {
        protected static String staticName = "GetUserToApplicationComponentAndAccessLevelMappingsQueries";
        protected static String staticDescription = "The total number of calls to the GetUserToApplicationComponentAndAccessLevelMappings() method";

        public GetUserToApplicationComponentAndAccessLevelMappingsQueries()
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
    /// Count metric which records the total number of calls to the GetGroupToApplicationComponentAndAccessLevelMappings() method.
    /// </summary>
    public class GetGroupToApplicationComponentAndAccessLevelMappingsQueries : CountMetric
    {
        protected static String staticName = "GetGroupToApplicationComponentAndAccessLevelMappingsQueries";
        protected static String staticDescription = "The total number of calls to the GetGroupToApplicationComponentAndAccessLevelMappings() method";

        public GetGroupToApplicationComponentAndAccessLevelMappingsQueries()
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
    /// Count metric which records the total number of calls to the ContainsEntityType() method.
    /// </summary>
    public class ContainsEntityTypeQueries : CountMetric
    {
        protected static String staticName = "ContainsEntityTypeQueries";
        protected static String staticDescription = "The total number of calls to the ContainsEntityType() method";

        public ContainsEntityTypeQueries()
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
    /// Count metric which records the total number of calls to the GetEntities() method.
    /// </summary>
    public class GetEntitiesQueries : CountMetric
    {
        protected static String staticName = "GetEntitiesQueries";
        protected static String staticDescription = "The total number of calls to the GetEntities() method";

        public GetEntitiesQueries()
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
    /// Count metric which records the total number of calls to the ContainsEntity() method.
    /// </summary>
    public class ContainsEntityQueries : CountMetric
    {
        protected static String staticName = "ContainsEntityQueries";
        protected static String staticDescription = "The total number of calls to the ContainsEntity() method";

        public ContainsEntityQueries()
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
    /// Count metric which records the total number of calls to the GetUserToEntityMappingsForUser() method.
    /// </summary>
    public class GetUserToEntityMappingsForUserQueries : CountMetric
    {
        protected static String staticName = "GetUserToEntityMappingsForUserQueries";
        protected static String staticDescription = "The total number of calls to the GetUserToEntityMappingsForUser() method";

        public GetUserToEntityMappingsForUserQueries()
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
    /// Count metric which records the total number of calls to the GetUserToEntityMappingsForUserAndEntityType() method.
    /// </summary>
    public class GetUserToEntityMappingsForUserAndEntityTypeQueries : CountMetric
    {
        protected static String staticName = "GetUserToEntityMappingsForUserAndEntityTypeQueries";
        protected static String staticDescription = "The total number of calls to the GetUserToEntityMappingsForUserAndEntityType() method";

        public GetUserToEntityMappingsForUserAndEntityTypeQueries()
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
    /// Count metric which records the total number of calls to the GetGroupToEntityMappingsForGroup() method.
    /// </summary>
    public class GetGroupToEntityMappingsForGroupQueries : CountMetric
    {
        protected static String staticName = "GetGroupToEntityMappingsForGroupQueries";
        protected static String staticDescription = "The total number of calls to the GetGroupToEntityMappingsForGroup() method";

        public GetGroupToEntityMappingsForGroupQueries()
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
    /// Count metric which records the total number of calls to the GetGroupToEntityMappingsForGroupAndEntityType() method.
    /// </summary>
    public class GetGroupToEntityMappingsForGroupAndEntityTypeQueries : CountMetric
    {
        protected static String staticName = "GetGroupToEntityMappingsForGroupAndEntityTypeQueries";
        protected static String staticDescription = "The total number of calls to the GetGroupToEntityMappingsForGroupAndEntityType() method";

        public GetGroupToEntityMappingsForGroupAndEntityTypeQueries()
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
    /// Count metric which records the total number of calls to the HasAccessToApplicationComponent() method.
    /// </summary>
    public class HasAccessToApplicationComponentQueries : CountMetric
    {
        protected static String staticName = "HasAccessToApplicationComponentQueries";
        protected static String staticDescription = "The total number of calls to the HasAccessToApplicationComponent() method";

        public HasAccessToApplicationComponentQueries()
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
    /// Count metric which records the total number of calls to the HasAccessToEntity() method.
    /// </summary>
    public class HasAccessToEntityQueries : CountMetric
    {
        protected static String staticName = "HasAccessToEntityQueries";
        protected static String staticDescription = "The total number of calls to the HasAccessToEntity() method";

        public HasAccessToEntityQueries()
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
    /// Count metric which records the total number of calls to the GetAccessibleEntities() method.
    /// </summary>
    public class GetAccessibleEntitiesQueries : CountMetric
    {
        protected static String staticName = "GetAccessibleEntitiesQueries";
        protected static String staticDescription = "The total number of calls to the GetAccessibleEntities() method";

        public GetAccessibleEntitiesQueries()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetAccessibleEntities() method.
    /// </summary>
    public class GetAccessibleEntitiesQueryTime : IntervalMetric
    {
        protected static String staticName = "GetAccessibleEntitiesQueryTime";
        protected static String staticDescription = "The time taken to execute the GetAccessibleEntities() method";

        public GetAccessibleEntitiesQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

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

    #pragma warning restore 1591
}
