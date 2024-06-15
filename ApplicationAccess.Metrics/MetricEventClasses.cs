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

    #region Base Classes

    /// <summary>
    /// Base for metrics which count the number of query operations.
    /// </summary>
    public abstract class QueryCountMetric : CountMetric
    {
    }

    /// <summary>
    /// Base for metrics which record the time taken to perform a query operation.
    /// </summary>
    public abstract class QueryIntervalMetric : IntervalMetric
    {
    }

    #endregion

    #region Query Operation Metrics

    /// <summary>
    /// Count metric which records a get on the Users property.
    /// </summary>
    public class UsersPropertyQuery : QueryCountMetric
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
    public class UsersPropertyQueryTime : QueryIntervalMetric
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
    public class GroupsPropertyQuery : QueryCountMetric
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
    public class GroupsPropertyQueryTime : QueryIntervalMetric
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
    public class EntityTypesPropertyQuery : QueryCountMetric
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
    public class EntityTypesPropertyQueryTime : QueryIntervalMetric
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
    public class ContainsUserQuery : QueryCountMetric
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
    public class ContainsUserQueryTime : QueryIntervalMetric
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
    public class ContainsGroupQuery : QueryCountMetric
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
    public class ContainsGroupQueryTime : QueryIntervalMetric
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
    public class GetUserToGroupMappingsQuery : QueryCountMetric
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
    public class GetUserToGroupMappingsWithIndirectMappingsQuery : QueryCountMetric
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
    public class GetUserToGroupMappingsQueryTime : QueryIntervalMetric
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
    public class GetUserToGroupMappingsWithIndirectMappingsQueryTime : QueryIntervalMetric
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
    /// Count metric which records a call to the GetGroupToUserMappings() method overload with 'group' parameter.
    /// </summary>
    public class GetGroupToUserMappingsForGroupQuery : QueryCountMetric
    {
        protected static String staticName = "GetGroupToUserMappingsForGroupQuery";
        protected static String staticDescription = "A call to the GetGroupToUserMappings() method overload with 'group' parameter";

        public GetGroupToUserMappingsForGroupQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToUserMappings() method overload with 'group' parameter and the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetGroupToUserMappingsForGroupWithIndirectMappingsQuery : QueryCountMetric
    {
        protected static String staticName = "GetGroupToUserMappingsForGroupWithIndirectMappingsQuery";
        protected static String staticDescription = "A call to the GetGroupToUserMappings() method overload with 'group' parameter and the 'includeIndirectMappings' parameter set true";

        public GetGroupToUserMappingsForGroupWithIndirectMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToUserMappings() method overload with 'group' parameter.
    /// </summary>
    public class GetGroupToUserMappingsForGroupQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetGroupToUserMappingsForGroupQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToUserMappings() method overload with 'group' parameter";

        public GetGroupToUserMappingsForGroupQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToUserMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToUserMappings() method overload with 'group' parameter and the 'includeIndirectMappings' parameter set true";

        public GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToGroupMappings() method overload with 'group' parameter.
    /// </summary>
    public class GetGroupToGroupMappingsForGroupQuery : QueryCountMetric
    {
        protected static String staticName = "GetGroupToGroupMappingsForGroupQuery";
        protected static String staticDescription = "A call to the GetGroupToGroupMappings() method overload with 'group' parameter";

        public GetGroupToGroupMappingsForGroupQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToUserMappings() method overload with 'groups' parameter.
    /// </summary>
    public class GetGroupToUserMappingsForGroupsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetGroupToUserMappingsForGroupsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToUserMappings() method overload with 'groups' parameter";

        public GetGroupToUserMappingsForGroupsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToUserMappings() method overload with 'groups' parameter.
    /// </summary>
    public class GetGroupToUserMappingsForGroupsQuery : QueryCountMetric
    {
        protected static String staticName = "GetGroupToUserMappingsForGroupsQuery";
        protected static String staticDescription = "A call to the GetGroupToUserMappings() method overload with 'groups' parameter";

        public GetGroupToUserMappingsForGroupsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToGroupMappings() method overload with 'group' parameter.
    /// </summary>
    public class GetGroupToGroupMappingsForGroupQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetGroupToGroupMappingsForGroupQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToGroupMappings() method overload with 'group' parameter.";

        public GetGroupToGroupMappingsForGroupQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToGroupMappings() method overload with 'group' parameter and the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetGroupToGroupMappingsForGroupWithIndirectMappingsQuery : QueryCountMetric
    {
        protected static String staticName = "GetGroupToGroupMappingsForGroupWithIndirectMappingsQuery";
        protected static String staticDescription = "A call to the GetGroupToGroupMappings() method overload with 'group' parameter and the 'includeIndirectMappings' parameter set true";

        public GetGroupToGroupMappingsForGroupWithIndirectMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToGroupMappings() method overload with 'group' parameter and the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToGroupMappings() method overload with 'group' parameter and the 'includeIndirectMappings' parameter set true";

        public GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToGroupMappings() method overload with 'groups' parameter.
    /// </summary>
    public class GetGroupToGroupMappingsForGroupsQuery : QueryCountMetric
    {
        protected static String staticName = "GetGroupToGroupMappingsForGroupsQuery";
        protected static String staticDescription = "A call to the GetGroupToGroupMappings() method overload with 'groups' parameter";

        public GetGroupToGroupMappingsForGroupsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToGroupMappings() method overload with 'groups' parameter.
    /// </summary>
    public class GetGroupToGroupMappingsForGroupsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetGroupToGroupMappingsForGroupsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToGroupMappings() method overload with 'groups' parameter.";

        public GetGroupToGroupMappingsForGroupsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToGroupReverseMappings() method overload with 'group' parameter.
    /// </summary>
    public class GetGroupToGroupReverseMappingsForGroupQuery : QueryCountMetric
    {
        protected static String staticName = "GetGroupToGroupReverseMappingsForGroupQuery";
        protected static String staticDescription = "A call to the GetGroupToGroupReverseMappings() method overload with 'group' parameter";

        public GetGroupToGroupReverseMappingsForGroupQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToGroupReverseMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQuery : QueryCountMetric
    {
        protected static String staticName = "GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQuery";
        protected static String staticDescription = "A call to the GetGroupToGroupReverseMappings() method overload with 'group' parameter and the 'includeIndirectMappings' parameter set true";

        public GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToGroupReverseMappings() method overload with 'group' parameter.
    /// </summary>
    public class GetGroupToGroupReverseMappingsForGroupQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetGroupToGroupReverseMappingsForGroupQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToGroupReverseMappings() method overload with 'group' parameter";

        public GetGroupToGroupReverseMappingsForGroupQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToGroupReverseMappings() method overload with 'group' parameter and the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToGroupReverseMappings() method overload with 'group' parameter and the 'includeIndirectMappings' parameter set true";

        public GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToGroupReverseMappings() method overload with 'groups' parameter.
    /// </summary>
    public class GetGroupToGroupReverseMappingsForGroupsQuery : QueryCountMetric
    {
        protected static String staticName = "GetGroupToGroupReverseMappingsForGroupsQuery";
        protected static String staticDescription = "A call to the GetGroupToGroupReverseMappings() method overload with 'groups' parameter";

        public GetGroupToGroupReverseMappingsForGroupsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToGroupReverseMappings() method overload with 'groups' parameter.
    /// </summary>
    public class GetGroupToGroupReverseMappingsForGroupsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetGroupToGroupReverseMappingsForGroupsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToGroupReverseMappings() method overload with 'groups' parameter";

        public GetGroupToGroupReverseMappingsForGroupsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetUserToApplicationComponentAndAccessLevelMappings() method.
    /// </summary>
    public class GetUserToApplicationComponentAndAccessLevelMappingsQuery : QueryCountMetric
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
    public class GetUserToApplicationComponentAndAccessLevelMappingsQueryTime : QueryIntervalMetric
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
    /// Count metric which records a call to the GetApplicationComponentAndAccessLevelToUserMappings() method.
    /// </summary>
    public class GetApplicationComponentAndAccessLevelToUserMappingsQuery : QueryCountMetric
    {
        protected static String staticName = "GetApplicationComponentAndAccessLevelToUserMappingsQuery";
        protected static String staticDescription = "A call to the GetApplicationComponentAndAccessLevelToUserMappings() method";

        public GetApplicationComponentAndAccessLevelToUserMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetApplicationComponentAndAccessLevelToUserMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQuery : QueryCountMetric
    {
        protected static String staticName = "GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQuery";
        protected static String staticDescription = "A call to the GetApplicationComponentAndAccessLevelToUserMappings() method with the 'includeIndirectMappings' parameter set true";

        public GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetApplicationComponentAndAccessLevelToUserMappings() method.
    /// </summary>
    public class GetApplicationComponentAndAccessLevelToUserMappingsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetApplicationComponentAndAccessLevelToUserMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetApplicationComponentAndAccessLevelToUserMappings() method";

        public GetApplicationComponentAndAccessLevelToUserMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetApplicationComponentAndAccessLevelToUserMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetApplicationComponentAndAccessLevelToUserMappings() method with the 'includeIndirectMappings' parameter set true";

        public GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToApplicationComponentAndAccessLevelMappings() method.
    /// </summary>
    public class GetGroupToApplicationComponentAndAccessLevelMappingsQuery : QueryCountMetric
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
    public class GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime : QueryIntervalMetric
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
    /// Count metric which records a call to the GetApplicationComponentAndAccessLevelToGroupMappings() method.
    /// </summary>
    public class GetApplicationComponentAndAccessLevelToGroupMappingsQuery : QueryCountMetric
    {
        protected static String staticName = "GetApplicationComponentAndAccessLevelToGroupMappingsQuery";
        protected static String staticDescription = "A call to the GetApplicationComponentAndAccessLevelToGroupMappings() method";

        public GetApplicationComponentAndAccessLevelToGroupMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetApplicationComponentAndAccessLevelToGroupMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQuery : QueryCountMetric
    {
        protected static String staticName = "GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQuery";
        protected static String staticDescription = "A call to the GetApplicationComponentAndAccessLevelToGroupMappings() method with the 'includeIndirectMappings' parameter set true";

        public GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetApplicationComponentAndAccessLevelToGroupMappings() method.
    /// </summary>
    public class GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetApplicationComponentAndAccessLevelToGroupMappings() method";

        public GetApplicationComponentAndAccessLevelToGroupMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetApplicationComponentAndAccessLevelToGroupMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetApplicationComponentAndAccessLevelToGroupMappings() method with the 'includeIndirectMappings' parameter set true";

        public GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the ContainsEntityType() method.
    /// </summary>
    public class ContainsEntityTypeQuery : QueryCountMetric
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
    public class ContainsEntityTypeQueryTime : QueryIntervalMetric
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
    public class GetEntitiesQuery : QueryCountMetric
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
    public class GetEntitiesQueryTime : QueryIntervalMetric
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
    public class ContainsEntityQuery : QueryCountMetric
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
    public class ContainsEntityQueryTime : QueryIntervalMetric
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
    /// Count metric which records a call to the GetUserToEntityMappings() method overload with 'user' parameter.
    /// </summary>
    public class GetUserToEntityMappingsForUserQuery : QueryCountMetric
    {
        protected static String staticName = "GetUserToEntityMappingsForUserQuery";
        protected static String staticDescription = "A call to the GetUserToEntityMappings() method overload with 'user' parameter";

        public GetUserToEntityMappingsForUserQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetUserToEntityMappings() method overload with 'user' parameter.
    /// </summary>
    public class GetUserToEntityMappingsForUserQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetUserToEntityMappingsForUserQueryTime";
        protected static String staticDescription = "The time taken to execute the GetUserToEntityMappings() method overload with 'user' parameter";

        public GetUserToEntityMappingsForUserQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetUserToEntityMappings() method overload with 'user' and 'entityType' parameters.
    /// </summary>
    public class GetUserToEntityMappingsForUserAndEntityTypeQuery : QueryCountMetric
    {
        protected static String staticName = "GetUserToEntityMappingsForUserAndEntityTypeQuery";
        protected static String staticDescription = "A call to the GetUserToEntityMappings() method overload with 'user' and 'entityType' parameters";

        public GetUserToEntityMappingsForUserAndEntityTypeQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetUserToEntityMappings() method overload with 'user' and 'entityType' parameters.
    /// </summary>
    public class GetUserToEntityMappingsForUserAndEntityTypeQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetUserToEntityMappingsForUserAndEntityTypeQueryTime";
        protected static String staticDescription = "The time taken to execute the GetUserToEntityMappings() method overload with 'user' and 'entityType' parameters";

        public GetUserToEntityMappingsForUserAndEntityTypeQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetEntityToUserMappings() method.
    /// </summary>
    public class GetEntityToUserMappingsQuery : QueryCountMetric
    {
        protected static String staticName = "GetEntityToUserMappingsQuery";
        protected static String staticDescription = "A call to the GetEntityToUserMappings() method";

        public GetEntityToUserMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetEntityToUserMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetEntityToUserMappingsWithIndirectMappingsQuery : QueryCountMetric
    {
        protected static String staticName = "GetEntityToUserMappingsWithIndirectMappingsQuery";
        protected static String staticDescription = "A call to the GetEntityToUserMappings() method with the 'includeIndirectMappings' parameter set true";

        public GetEntityToUserMappingsWithIndirectMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetEntityToUserMappings() method.
    /// </summary>
    public class GetEntityToUserMappingsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetEntityToUserMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetEntityToUserMappings() method";

        public GetEntityToUserMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetEntityToUserMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetEntityToUserMappingsWithIndirectMappingsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetEntityToUserMappingsWithIndirectMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetEntityToUserMappings() method with the 'includeIndirectMappings' parameter set true";

        public GetEntityToUserMappingsWithIndirectMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToEntityMappings() method overload with 'group' parameter.
    /// </summary>
    public class GetGroupToEntityMappingsForGroupQuery : QueryCountMetric
    {
        protected static String staticName = "GetGroupToEntityMappingsForGroupQuery";
        protected static String staticDescription = "A call to the GetGroupToEntityMappings() method overload with 'group' parameter";

        public GetGroupToEntityMappingsForGroupQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToEntityMappings() method overload with 'group' parameter.
    /// </summary>
    public class GetGroupToEntityMappingsForGroupQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetGroupToEntityMappingsForGroupQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToEntityMappings() method overload with 'group' parameter";

        public GetGroupToEntityMappingsForGroupQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetGroupToEntityMappings() method overload with 'group' and 'entityType' parameters.
    /// </summary>
    public class GetGroupToEntityMappingsForGroupAndEntityTypeQuery : QueryCountMetric
    {
        protected static String staticName = "GetGroupToEntityMappingsForGroupAndEntityTypeQuery";
        protected static String staticDescription = "A call to the GetGroupToEntityMappings() method overload with 'group' and 'entityType' parameters";

        public GetGroupToEntityMappingsForGroupAndEntityTypeQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetGroupToEntityMappings() method overload with 'group' and 'entityType' parameters.
    /// </summary>
    public class GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime";
        protected static String staticDescription = "The time taken to execute the GetGroupToEntityMappings() method overload with 'group' and 'entityType' parameters";

        public GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetEntityToGroupMappings() method.
    /// </summary>
    public class GetEntityToGroupMappingsQuery : QueryCountMetric
    {
        protected static String staticName = "GetEntityToGroupMappingsQuery";
        protected static String staticDescription = "A call to the GetEntityToGroupMappings() method";

        public GetEntityToGroupMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetEntityToGroupMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetEntityToGroupMappingsWithIndirectMappingsQuery : QueryCountMetric
    {
        protected static String staticName = "GetEntityToGroupMappingsWithIndirectMappingsQuery";
        protected static String staticDescription = "A call to the GetEntityToGroupMappings() method with the 'includeIndirectMappings' parameter set true";

        public GetEntityToGroupMappingsWithIndirectMappingsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetEntityToGroupMappings() method.
    /// </summary>
    public class GetEntityToGroupMappingsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetEntityToGroupMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetEntityToGroupMappings() method";

        public GetEntityToGroupMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetEntityToGroupMappings() method with the 'includeIndirectMappings' parameter set true.
    /// </summary>
    public class GetEntityToGroupMappingsWithIndirectMappingsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetEntityToGroupMappingsWithIndirectMappingsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetEntityToGroupMappings() method with the 'includeIndirectMappings' parameter set true";

        public GetEntityToGroupMappingsWithIndirectMappingsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the HasAccessToApplicationComponent() method overload with 'user' parameter.
    /// </summary>
    public class HasAccessToApplicationComponentForUserQuery : QueryCountMetric
    {
        protected static String staticName = "HasAccessToApplicationComponentForUserQuery";
        protected static String staticDescription = "A call to the HasAccessToApplicationComponent() method overload with 'user' parameter";

        public HasAccessToApplicationComponentForUserQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the HasAccessToApplicationComponent() method overload with 'user' parameter.
    /// </summary>
    public class HasAccessToApplicationComponentForUserQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "HasAccessToApplicationComponentForUserQueryTime";
        protected static String staticDescription = "The time taken to execute the HasAccessToApplicationComponent() method overload with 'user' parameter";

        public HasAccessToApplicationComponentForUserQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the HasAccessToApplicationComponent() method overload with 'groups' parameter.
    /// </summary>
    public class HasAccessToApplicationComponentForGroupsQuery : QueryCountMetric
    {
        protected static String staticName = "HasAccessToApplicationComponentForGroupsQuery";
        protected static String staticDescription = "A call to the HasAccessToApplicationComponent() method overload with 'groups' parameter";

        public HasAccessToApplicationComponentForGroupsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the HasAccessToApplicationComponent() method overload with 'groups' parameter.
    /// </summary>
    public class HasAccessToApplicationComponentForGroupsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "HasAccessToApplicationComponentForGroupsQueryTime";
        protected static String staticDescription = "The time taken to execute the HasAccessToApplicationComponent() method overload with 'groups' parameter";

        public HasAccessToApplicationComponentForGroupsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the HasAccessToEntity() method overload with 'user' parameter.
    /// </summary>
    public class HasAccessToEntityForUserQuery : QueryCountMetric
    {
        protected static String staticName = "HasAccessToEntityForUserQuery";
        protected static String staticDescription = "A call to the HasAccessToEntity() method overload with 'user' parameter";

        public HasAccessToEntityForUserQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the HasAccessToEntity() method overload with 'user' parameter.
    /// </summary>
    public class HasAccessToEntityForUserQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "HasAccessToEntityForUserQueryTime";
        protected static String staticDescription = "The time taken to execute the HasAccessToEntity() method overload with 'user' parameter";

        public HasAccessToEntityForUserQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the HasAccessToEntity() method overload with 'groups' parameter.
    /// </summary>
    public class HasAccessToEntityForGroupsQuery : QueryCountMetric
    {
        protected static String staticName = "HasAccessToEntityForGroupsQuery";
        protected static String staticDescription = "A call to the HasAccessToEntity() method overload with 'groups' parameter";

        public HasAccessToEntityForGroupsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the HasAccessToEntity() method overload with 'groups' parameter.
    /// </summary>
    public class HasAccessToEntityForGroupsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "HasAccessToEntityForGroupsQueryTime";
        protected static String staticDescription = "The time taken to execute the HasAccessToEntity() method overload with 'groups' parameter";

        public HasAccessToEntityForGroupsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetApplicationComponentsAccessibleByUser() method.
    /// </summary>
    public class GetApplicationComponentsAccessibleByUserQuery : QueryCountMetric
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
    public class GetApplicationComponentsAccessibleByUserQueryTime : QueryIntervalMetric
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
    public class GetApplicationComponentsAccessibleByGroupQuery : QueryCountMetric
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
    public class GetApplicationComponentsAccessibleByGroupQueryTime : QueryIntervalMetric
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
    /// Count metric which records a call to the GetApplicationComponentsAccessibleByGroups() method.
    /// </summary>
    public class GetApplicationComponentsAccessibleByGroupsQuery : QueryCountMetric
    {
        protected static String staticName = "GetApplicationComponentsAccessibleByGroupsQuery";
        protected static String staticDescription = "A call to the GetApplicationComponentsAccessibleByGroups() method";

        public GetApplicationComponentsAccessibleByGroupsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetApplicationComponentsAccessibleByGroups() method.
    /// </summary>
    public class GetApplicationComponentsAccessibleByGroupsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetApplicationComponentsAccessibleByGroupsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetApplicationComponentsAccessibleByGroups() method";

        public GetApplicationComponentsAccessibleByGroupsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetEntitiesAccessibleByUser() method.
    /// </summary>
    public class GetEntitiesAccessibleByUserQuery : QueryCountMetric
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
    public class GetEntitiesAccessibleByUserQueryTime : QueryIntervalMetric
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
    public class GetEntitiesAccessibleByGroupQuery : QueryCountMetric
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
    public class GetEntitiesAccessibleByGroupQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetEntitiesAccessibleByGroupQueryTime";
        protected static String staticDescription = "The time taken to execute the GetEntitiesAccessibleByGroup() method";

        public GetEntitiesAccessibleByGroupQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a call to the GetEntitiesAccessibleByGroups() method.
    /// </summary>
    public class GetEntitiesAccessibleByGroupsQuery : QueryCountMetric
    {
        protected static String staticName = "GetEntitiesAccessibleByGroupsQuery";
        protected static String staticDescription = "A call to the GetEntitiesAccessibleByGroups() method";

        public GetEntitiesAccessibleByGroupsQuery()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the GetEntitiesAccessibleByGroups() method.
    /// </summary>
    public class GetEntitiesAccessibleByGroupsQueryTime : QueryIntervalMetric
    {
        protected static String staticName = "GetEntitiesAccessibleByGroupsQueryTime";
        protected static String staticDescription = "The time taken to execute the GetEntitiesAccessibleByGroups() method";

        public GetEntitiesAccessibleByGroupsQueryTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #endregion

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
