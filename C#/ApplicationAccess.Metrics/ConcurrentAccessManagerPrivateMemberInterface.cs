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
using System.Collections.Generic;

namespace ApplicationAccess.Metrics
{
    /// <summary>
    /// An interface to the private and protected members of a <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> instance.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>Designed to be used in conjunction with a <see cref="ConcurrentAccessManagerMetricLoggingInternalDecorator{TUser, TGroup, TComponent, TAccess}"/> instance.  Needs to be instantiated within the implementation of a subclass of <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> so that the constructor can be passed references to the relevent private/protected members and methods.</remarks>
    public class ConcurrentAccessManagerPrivateMemberInterface<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The DirectedGraph which stores the user to group mappings.</summary>
        public DirectedGraphBase<TUser, TGroup> UserToGroupMap { get; }

        /// <summary>A dictionary which stores mappings between a user, and application component, and a level of access to that component.</summary>
        public IDictionary<TUser, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>> UserToComponentMap;

        /// <summary>A dictionary which HashSet mappings between a group, and application component, and a level of access to that component.</summary>
        public IDictionary<TGroup, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>> GroupToComponentMap;

        /// <summary>Holds all valid entity types and values within the access manager.  The Dictionary key holds the types of all entities, and each respective value holds the valid entity values within that type (e.g. the entity type could be 'ClientAccount', and values could be the names of all client accounts).</summary>
        public IDictionary<String, ISet<String>> Entities { get; }

        /// <summary>The Clear() method.</summary>
        public Action ClearMethod { get; }

        /// <summary>The AddUser() method overload with 'wrappingAction' parameter.</summary>
        public Action<TUser, Action<TUser, Action>> AddUserWithWrappingActionMethod { get; }

        /// <summary>The ContainsUser() method.</summary>
        public Func<TUser, Boolean> ContainsUserMethod { get; }

        /// <summary>The RemoveUser() method overload with 'wrappingAction' parameter.</summary>
        public Action<TUser, Action<TUser, Action>> RemoveUserWithWrappingActionMethod { get; }

        /// <summary>The AddGroup() method overload with 'wrappingAction' parameter.</summary>
        public Action<TGroup, Action<TGroup, Action>> AddGroupWithWrappingActionMethod { get; }

        /// <summary>The ContainsGroup() method.</summary>
        public Func<TGroup, Boolean> ContainsGroupMethod { get; }

        /// <summary>The RemoveGroup() method overload with 'wrappingAction' parameter.</summary>
        public Action<TGroup, Action<TGroup, Action>> RemoveGroupWithWrappingActionMethod { get; }

        /// <summary>The AddUserToGroupMapping() method overload with 'wrappingAction' parameter.</summary>
        public Action<TUser, TGroup, Action<TUser, TGroup, Action>> AddUserToGroupMappingWithWrappingActionMethod { get; }

        /// <summary>The GetUserToGroupMappings() method.</summary>
        public Func<TUser, Boolean, HashSet<TGroup>> GetUserToGroupMappingsMethod { get; }

        /// <summary>The RemoveUserToGroupMapping() method overload with 'wrappingAction' parameter.</summary>
        public Action<TUser, TGroup, Action<TUser, TGroup, Action>> RemoveUserToGroupMappingWithWrappingActionMethod { get; }

        /// <summary>The AddGroupToGroupMapping() method overload with 'wrappingAction' parameter.</summary>
        public Action<TGroup, TGroup, Action<TGroup, TGroup, Action>> AddGroupToGroupMappingWithWrappingActionMethod { get; }

        /// <summary>The GetGroupToGroupMappings() method.</summary>
        public Func<TGroup, Boolean, HashSet<TGroup>> GetGroupToGroupMappingsMethod { get; }

        /// <summary>The RemoveGroupToGroupMapping() method overload with 'wrappingAction' parameter.</summary>
        public Action<TGroup, TGroup, Action<TGroup, TGroup, Action>> RemoveGroupToGroupMappingWithWrappingActionMethod { get; }

        /// <summary>The AddUserToApplicationComponentAndAccessLevelMapping() method overload with 'wrappingAction' parameter.</summary>
        public Action<TUser, TComponent, TAccess, Action<TUser, TComponent, TAccess, Action>> AddUserToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod { get; }

        /// <summary>The GetUserToApplicationComponentAndAccessLevelMappings() method.</summary>
        public Func<TUser, IEnumerable<Tuple<TComponent, TAccess>>> GetUserToApplicationComponentAndAccessLevelMappingsMethod { get; }

        /// <summary>The RemoveUserToApplicationComponentAndAccessLevelMapping() method overload with 'wrappingAction' parameter.</summary>
        public Action<TUser, TComponent, TAccess, Action<TUser, TComponent, TAccess, Action>> RemoveUserToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod { get; }

        /// <summary>The AddGroupToApplicationComponentAndAccessLevelMapping() method overload with 'wrappingAction' parameter.</summary>
        public Action<TGroup, TComponent, TAccess, Action<TGroup, TComponent, TAccess, Action>> AddGroupToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod { get; }

        /// <summary>The GetGroupToApplicationComponentAndAccessLevelMappings() method.</summary>
        public Func<TGroup, IEnumerable<Tuple<TComponent, TAccess>>> GetGroupToApplicationComponentAndAccessLevelMappingsMethod { get; }

        /// <summary>The RemoveGroupToApplicationComponentAndAccessLevelMapping() method overload with 'wrappingAction' parameter.</summary>
        public Action<TGroup, TComponent, TAccess, Action<TGroup, TComponent, TAccess, Action>> RemoveGroupToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod { get; }

        /// <summary>The AddEntityType() method overload with 'wrappingAction' parameter.</summary>
        public Action<String, Action<String, Action>> AddEntityTypeWithWrappingActionMethod { get; }

        /// <summary>The ContainsEntityType() method.</summary>
        public Func<String, Boolean> ContainsEntityTypeMethod { get; }

        /// <summary>The RemoveEntityType() method overload with 'wrappingAction' parameter.</summary>
        public Action<String, Action<String, Action>> RemoveEntityTypeWithWrappingActionMethod { get; }

        /// <summary>The RemoveEntityType() method overload with '*PreRemovalAction' parameters.</summary>
        public Action<String ,Action<TUser, String, IEnumerable<String>, Int32>, Action<TGroup, String, IEnumerable<String>, Int32>> RemoveEntityTypeWithPreRemovalActionsMethod { get; }

        /// <summary>The AddEntity() method overload with 'wrappingAction' parameter.</summary>
        public Action<String, String, Action<String, String, Action>> AddEntityWithWrappingActionMethod { get; }

        /// <summary>The GetEntitiesMethod() method.</summary>
        public Func<String, IEnumerable<String>> GetEntitiesMethod { get; }

        /// <summary>The ContainsEntity() method.</summary>
        public Func<String, String, Boolean> ContainsEntityMethod { get; }

        /// <summary>The RemoveEntity() method overload with 'wrappingAction' parameter.</summary>
        public Action<String, String, Action<String, String, Action>> RemoveEntityWithWrappingActionMethod { get; }

        /// <summary>The RemoveEntity() method overload with '*PostRemovalAction' parameters.</summary>
        public Action<String, String, Action<TUser, String, String>, Action<TGroup, String, String>> RemoveEntityWithPostRemovalActionsMethod { get; }

        /// <summary>The AddUserToEntityMapping() method overload with 'wrappingAction' parameter.</summary>
        public Action<TUser, String, String, Action<TUser, String, String, Action>> AddUserToEntityMappingWithWrappingActionMethod { get; }

        /// <summary>The GetUserToEntityMappings() method.</summary>
        public Func<TUser, IEnumerable<Tuple<String, String>>> GetUserToEntityMappingsMethod { get; }

        /// <summary>The GetUserToEntityMappings() method overload which accepts an 'entityType' parameter.</summary>
        public Func<TUser, String, IEnumerable<String>> GetUserToEntityMappingsWithEntityTypeMethod { get; }

        /// <summary>The RemoveUserToEntityMapping() method overload with '*PostRemovalAction' parameters.</summary>
        public Action<TUser, String, String, Action<TUser, String, String, Action>> RemoveUserToEntityMappingWithPostRemovalActionsMethod { get; }

        /// <summary>The AddGroupToEntityMapping() method overload with 'wrappingAction' parameter.</summary>
        public Action<TGroup, String, String, Action<TGroup, String, String, Action>> AddGroupToEntityMappingWithWrappingActionMethod { get; }

        /// <summary>The GetGroupToEntityMappings() method.</summary>
        public Func<TGroup, IEnumerable<Tuple<String, String>>> GetGroupToEntityMappingsMethod { get; }

        /// <summary>The GetGrouprToEntityMappings() method overload which accepts an 'entityType' parameter.</summary>
        public Func<TGroup, String, IEnumerable<String>> GetGroupToEntityMappingsWithEntityTypeMethod { get; }

        /// <summary>The RemoveGroupToEntityMapping() method overload with '*PostRemovalAction' parameters.</summary>
        public Action<TGroup, String, String, Action<TGroup, String, String, Action>> RemoveGroupToEntityMappingWithPostRemovalActionsMethod { get; }

        /// <summary>The HasAccessToApplicationComponent() method.</summary>
        public Func<TUser, TComponent, TAccess, Boolean> HasAccessToApplicationComponentMethod { get; }

        /// <summary>The HasAccessToEntity() method.</summary>
        public Func<TUser, String, String, Boolean> HasAccessToEntityMethod { get; }

        /// <summary>The GetApplicationComponentsAccessibleByUser() method.</summary>
        public Func<TUser, HashSet<Tuple<TComponent, TAccess>>> GetApplicationComponentsAccessibleByUserMethod { get; }

        /// <summary>The GetApplicationComponentsAccessibleByGroup() method.</summary>
        public Func<TGroup, HashSet<Tuple<TComponent, TAccess>>> GetApplicationComponentsAccessibleByGroupMethod { get; }

        /// <summary>The GetEntitiesAccessibleByUserMethod() method.</summary>
        public Func<TUser, HashSet<Tuple<String, String>>> GetEntitiesAccessibleByUserMethod { get; }

        /// <summary>The GetEntitiesAccessibleByUserMethod() method overload which accepts an 'entityType' parameter.</summary>
        public Func<TUser, String, HashSet<String>> GetEntitiesAccessibleByUserWithEntityTypeMethod { get; }

        /// <summary>The GetEntitiesAccessibleByGroupMethod() method.</summary>
        public Func<TGroup, HashSet<Tuple<String, String>>> GetEntitiesAccessibleByGroupMethod { get; }

        /// <summary>The GetEntitiesAccessibleByGroupMethod() method overload which accepts an 'entityType' parameter.</summary>
        public Func<TGroup, String, HashSet<String>> GetEntitiesAccessibleByGroupWithEntityTypeMethod { get; }

        #pragma warning disable 1591

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.ConcurrentAccessManagerPrivateMemberInterface class.
        /// </summary>
        /// <remarks>Constructor parameters are not documented explicitly, but should be set with the relevant name-matching private/protected members and methods of the <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> instance which metric logging is being implemented for.</remarks>
        public ConcurrentAccessManagerPrivateMemberInterface
        (
            DirectedGraphBase<TUser, TGroup> userToGroupMap,
            IDictionary<TUser, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>> userToComponentMap,
            IDictionary<TGroup, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>> groupToComponentMap,
            IDictionary<String, ISet<String>> entities,
            Action clearMethod, 
            Action<TUser, Action<TUser, Action>> addUserWithWrappingActionMethod,
            Func<TUser, Boolean> containsUserMethod,
            Action<TUser, Action<TUser, Action>> removeUserWithWrappingActionMethod,
            Action<TGroup, Action<TGroup, Action>> addGroupWithWrappingActionMethod, 
            Func<TGroup, Boolean> containsGroupMethod,
            Action<TGroup, Action<TGroup, Action>> removeGroupWithWrappingActionMethod,
            Action<TUser, TGroup, Action<TUser, TGroup, Action>> addUserToGroupMappingWithWrappingActionMethod,
            Func<TUser, Boolean, HashSet<TGroup>> getUserToGroupMappingsMethod,
            Action<TUser, TGroup, Action<TUser, TGroup, Action>> removeUserToGroupMappingWithWrappingActionMethod,
            Action<TGroup, TGroup, Action<TGroup, TGroup, Action>> addGroupToGroupMappingWithWrappingActionMethod,
            Func<TGroup, Boolean, HashSet<TGroup>> getGroupToGroupMappingsMethod, 
            Action<TGroup, TGroup, Action<TGroup, TGroup, Action>> removeGroupToGroupMappingWithWrappingActionMethod,
            Action<TUser, TComponent, TAccess, Action<TUser, TComponent, TAccess, Action>> addUserToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod,
            Func<TUser, IEnumerable<Tuple<TComponent, TAccess>>> getUserToApplicationComponentAndAccessLevelMappingsMethod,
            Action<TUser, TComponent, TAccess, Action<TUser, TComponent, TAccess, Action>> removeUserToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod, 
            Action<TGroup, TComponent, TAccess, Action<TGroup, TComponent, TAccess, Action>> addGroupToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod,
            Func<TGroup, IEnumerable<Tuple<TComponent, TAccess>>> getGroupToApplicationComponentAndAccessLevelMappingsMethod,
            Action<TGroup, TComponent, TAccess, Action<TGroup, TComponent, TAccess, Action>> removeGroupToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod,
            Action<String, Action<String, Action>> addEntityTypeWithWrappingActionMethod,
            Func<String, Boolean> containsEntityTypeMethod,
            Action<String, Action<String, Action>> removeEntityTypeWithWrappingActionMethod,
            Action<String, Action<TUser, String, IEnumerable<String>, Int32>, Action<TGroup, String, IEnumerable<String>, Int32>> removeEntityTypeWithPreRemovalActionsMethod, 
            Action<String, String, Action<String, String, Action>> addEntityWithWrappingActionMethod,
            Func<String, IEnumerable<String>> getEntitiesMethod,
            Func<String, String, Boolean> containsEntityMethod,
            Action<String, String, Action<String, String, Action>> removeEntityWithWrappingActionMethod,
            Action<String, String, Action<TUser, String, String>, Action<TGroup, String, String>> removeEntityWithPostRemovalActionsMethod,
            Action<TUser, String, String, Action<TUser, String, String, Action>> addUserToEntityMappingWithWrappingActionMethod,
            Func<TUser, IEnumerable<Tuple<String, String>>> getUserToEntityMappingsMethod,
            Func<TUser, String, IEnumerable<String>> getUserToEntityMappingsWithEntityTypeMethod,
            Action<TUser, String, String, Action<TUser, String, String, Action>> removeUserToEntityMappingWithPostRemovalActionsMethod, 
            Action<TGroup, String, String, Action<TGroup, String, String, Action>> addGroupToEntityMappingWithWrappingActionMethod,
            Func<TGroup, IEnumerable<Tuple<String, String>>> getGroupToEntityMappingsMethod,
            Func<TGroup, String, IEnumerable<String>> getGroupToEntityMappingsWithEntityTypeMethod,
            Action<TGroup, String, String, Action<TGroup, String, String, Action>> removeGroupToEntityMappingWithPostRemovalActionsMethod,
            Func<TUser, TComponent, TAccess, Boolean> hasAccessToApplicationComponentMethod,
            Func<TUser, String, String, Boolean> hasAccessToEntityMethod, 
            Func<TUser, HashSet<Tuple<TComponent, TAccess>>> getApplicationComponentsAccessibleByUserMethod,
            Func<TGroup, HashSet<Tuple<TComponent, TAccess>>> getApplicationComponentsAccessibleByGroupMethod,
            Func<TUser, HashSet<Tuple<String, String>>> getEntitiesAccessibleByUserMethod,
            Func<TUser, String, HashSet<String>> getEntitiesAccessibleByUserWithEntityTypeMethod,
            Func<TGroup, HashSet<Tuple<String, String>>> getEntitiesAccessibleByGroupMethod,
            Func<TGroup, String, HashSet<String>> getEntitiesAccessibleByGroupWithEntityTypeMethod
        )
        {
            UserToGroupMap = userToGroupMap;
            UserToComponentMap = userToComponentMap;
            GroupToComponentMap = groupToComponentMap;
            Entities = entities;
            ClearMethod = clearMethod;
            AddUserWithWrappingActionMethod = addUserWithWrappingActionMethod;
            ContainsUserMethod = containsUserMethod;
            RemoveUserWithWrappingActionMethod = removeUserWithWrappingActionMethod;
            AddGroupWithWrappingActionMethod = addGroupWithWrappingActionMethod;
            ContainsGroupMethod = containsGroupMethod;
            RemoveGroupWithWrappingActionMethod = removeGroupWithWrappingActionMethod;
            AddUserToGroupMappingWithWrappingActionMethod = addUserToGroupMappingWithWrappingActionMethod;
            GetUserToGroupMappingsMethod = getUserToGroupMappingsMethod;
            RemoveUserToGroupMappingWithWrappingActionMethod = removeUserToGroupMappingWithWrappingActionMethod;
            AddGroupToGroupMappingWithWrappingActionMethod = addGroupToGroupMappingWithWrappingActionMethod;
            GetGroupToGroupMappingsMethod = getGroupToGroupMappingsMethod;
            RemoveGroupToGroupMappingWithWrappingActionMethod = removeGroupToGroupMappingWithWrappingActionMethod;
            AddUserToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod = addUserToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod;
            GetUserToApplicationComponentAndAccessLevelMappingsMethod = getUserToApplicationComponentAndAccessLevelMappingsMethod;
            RemoveUserToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod = removeUserToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod;
            AddGroupToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod = addGroupToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod;
            GetGroupToApplicationComponentAndAccessLevelMappingsMethod = getGroupToApplicationComponentAndAccessLevelMappingsMethod;
            RemoveGroupToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod = removeGroupToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod;
            AddEntityTypeWithWrappingActionMethod = addEntityTypeWithWrappingActionMethod;
            ContainsEntityTypeMethod = containsEntityTypeMethod;
            RemoveEntityTypeWithWrappingActionMethod = removeEntityTypeWithWrappingActionMethod;
            RemoveEntityTypeWithPreRemovalActionsMethod = removeEntityTypeWithPreRemovalActionsMethod;
            AddEntityWithWrappingActionMethod = addEntityWithWrappingActionMethod;
            GetEntitiesMethod = getEntitiesMethod;
            ContainsEntityMethod = containsEntityMethod;
            RemoveEntityWithWrappingActionMethod = removeEntityWithWrappingActionMethod;
            RemoveEntityWithPostRemovalActionsMethod = removeEntityWithPostRemovalActionsMethod;
            AddUserToEntityMappingWithWrappingActionMethod = addUserToEntityMappingWithWrappingActionMethod;
            GetUserToEntityMappingsMethod = getUserToEntityMappingsMethod;
            GetUserToEntityMappingsWithEntityTypeMethod = getUserToEntityMappingsWithEntityTypeMethod;
            RemoveUserToEntityMappingWithPostRemovalActionsMethod = removeUserToEntityMappingWithPostRemovalActionsMethod;
            AddGroupToEntityMappingWithWrappingActionMethod = addGroupToEntityMappingWithWrappingActionMethod;
            GetGroupToEntityMappingsMethod = getGroupToEntityMappingsMethod;
            GetGroupToEntityMappingsWithEntityTypeMethod = getGroupToEntityMappingsWithEntityTypeMethod;
            RemoveGroupToEntityMappingWithPostRemovalActionsMethod = removeGroupToEntityMappingWithPostRemovalActionsMethod;
            HasAccessToApplicationComponentMethod = hasAccessToApplicationComponentMethod;
            HasAccessToEntityMethod = hasAccessToEntityMethod;
            GetApplicationComponentsAccessibleByUserMethod = getApplicationComponentsAccessibleByUserMethod;
            GetApplicationComponentsAccessibleByGroupMethod = getApplicationComponentsAccessibleByGroupMethod;
            GetEntitiesAccessibleByUserMethod = getEntitiesAccessibleByUserMethod;
            GetEntitiesAccessibleByUserWithEntityTypeMethod = getEntitiesAccessibleByUserWithEntityTypeMethod;
            GetEntitiesAccessibleByGroupMethod = getEntitiesAccessibleByGroupMethod;
            GetEntitiesAccessibleByGroupWithEntityTypeMethod = getEntitiesAccessibleByGroupWithEntityTypeMethod;
        }

        #pragma warning restore 1591
    }
}
