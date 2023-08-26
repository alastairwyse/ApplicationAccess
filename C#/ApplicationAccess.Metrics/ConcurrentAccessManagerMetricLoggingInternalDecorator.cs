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
using ApplicationMetrics;
using ApplicationAccess.Utilities;
using MoreComplexDataStructures;

namespace ApplicationAccess.Metrics
{
    /// <summary>
    /// Class which logs metrics for an instance or subclass of <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> by accessing required private and protected members of the instance or subclass through a defined interface, and wrapping/decorating method calls with metric logging functionality.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>This class provides a solution for wanting to have a 'MetricLogging' version of both the <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> and <see cref="DependencyFreeAccessManager{TUser, TGroup, TComponent, TAccess}"/> classes.  Metric logging functionality which wraps each of the public methods of both classes (and requires access to their private/protected members) is implemented in this class, and can be reused by 'MetricLogging' subclasses of each.</remarks>
    public class ConcurrentAccessManagerMetricLoggingInternalDecorator<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The number of user to application component and access level mappings in the access manager.</summary>
        protected Int32 userToApplicationComponentAndAccessLevelMappingCount;
        /// <summary>The number of group to application component and access level mappings in the access manager.</summary>
        protected Int32 groupToApplicationComponentAndAccessLevelMappingCount;
        /// <summary>The number of entities in the access manager.</summary>
        protected Int32 entityCount;
        /// <summary>The number of user to entity mappings stored.</summary>
        protected Int32 userToEntityMappingCount;
        /// <summary>The number of user to entity mappings stored for each user.</summary>
        protected FrequencyTable<TUser> userToEntityMappingCountPerUser;
        /// <summary>The number of group to entity mappings stored.</summary>
        protected Int32 groupToEntityMappingCount;
        /// <summary>The number of group to entity mappings stored for each group.</summary>
        protected FrequencyTable<TGroup> groupToEntityMappingCountPerGroup;

        /// <summary>The access manager to log metrics for.</summary>
        protected ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess> accessManager;
        /// <summary>Interface to the private members of the 'accessManager' member.</summary>
        protected ConcurrentAccessManagerPrivateMemberInterface<TUser, TGroup, TComponent, TAccess> accessManagerPrivateInterface;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Metric mapper used by the 'userToGroupMap' DirectedGraph member, e.g. to map metrics for 'leaf vertices' to metrics for 'users'.</summary>
        protected MappingMetricLogger mappingMetricLogger;
        /// <summary>Whether logging of metrics is enabled.</summary>
        protected volatile Boolean metricLoggingEnabled;

        /// <summary>
        /// Whether logging of metrics is enabled.
        /// </summary>
        /// <remarks>Generally this would be set true, but may need to be set false in some situations (e.g. when loading contents from a database).</remarks>
        public Boolean MetricLoggingEnabled
        {
            get
            {
                return metricLoggingEnabled;
            }

            set
            {
                metricLoggingEnabled = value;
                ((MetricLoggingConcurrentDirectedGraph<TUser, TGroup>)accessManagerPrivateInterface.UserToGroupMap).MetricLoggingEnabled = value;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.ConcurrentAccessManagerMetricLoggingInternalDecorator class.
        /// </summary>
        /// <param name="accessManager">The access manager to log metrics for.</param>
        /// <param name="accessManagerPrivateInterface">Interface to the private members of the access manager to log metrics for.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public ConcurrentAccessManagerMetricLoggingInternalDecorator
        (
            ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess> accessManager,
            ConcurrentAccessManagerPrivateMemberInterface<TUser, TGroup, TComponent, TAccess> accessManagerPrivateInterface,
            IMetricLogger metricLogger
        )
        {
            InitializeItemAndMappingCountFields();
            this.accessManager = accessManager;
            this.accessManagerPrivateInterface = accessManagerPrivateInterface;
            this.metricLogger = metricLogger;
            metricLoggingEnabled = true;
        }

        /// <summary>
        /// Removes all items and mappings from the access manager.
        /// </summary>
        public void Clear()
        {
            accessManagerPrivateInterface.ClearMethod();
            InitializeItemAndMappingCountFields();
        }

        /// <summary>
        /// Adds a user.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the user but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddUser(TUser user, Action<TUser> postProcessingAction)
        {
            Action<TUser, Action<TUser, Action>> addUserAction = (actionUser, baseAction) =>
            {
                accessManagerPrivateInterface.AddUserWithWrappingActionMethod(user, baseAction);
                postProcessingAction.Invoke(user);
            };
            CallAccessManagerEventProcessingMethodWithMetricLogging<TUser, UserAddTime, UserAdded>(user, addUserAction);
        }

        /// <summary>
        /// Returns true if the specified user exists.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <returns>True if the user exists.  False otherwise.</returns>
        public Boolean ContainsUser(TUser user)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<Boolean, ContainsUserQuery>(() => { return accessManagerPrivateInterface.ContainsUserMethod(user); });
        }

        /// <summary>
        /// Removes a user.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the user but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveUser(TUser user, Action<TUser> postProcessingAction)
        {
            Action<TUser, Action> wrappingAction = (actionUser, baseAction) =>
            {
                Int32 newUserToApplicationComponentAndAccessLevelMappingCount = userToApplicationComponentAndAccessLevelMappingCount;
                if (accessManagerPrivateInterface.UserToComponentMap.ContainsKey(user) == true)
                {
                    newUserToApplicationComponentAndAccessLevelMappingCount -= accessManagerPrivateInterface.UserToComponentMap[user].Count;
                }
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new UserRemoveTime());
                try
                {
                    baseAction.Invoke();
                    postProcessingAction.Invoke(user);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new UserRemoveTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new UserRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new UserRemoved());
                userToApplicationComponentAndAccessLevelMappingCount = newUserToApplicationComponentAndAccessLevelMappingCount;
                SetStatusMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingsStored(), userToApplicationComponentAndAccessLevelMappingCount);
                userToEntityMappingCount -= userToEntityMappingCountPerUser.GetFrequency(user);
                if (userToEntityMappingCountPerUser.GetFrequency(user) > 0)
                {
                    userToEntityMappingCountPerUser.DecrementBy(user, userToEntityMappingCountPerUser.GetFrequency(user));
                }
                SetStatusMetricIfLoggingEnabled(new UserToEntityMappingsStored(), userToEntityMappingCount);
            };
            accessManagerPrivateInterface.RemoveUserWithWrappingActionMethod(user, wrappingAction);
        }

        /// <summary>
        /// Adds a group.
        /// </summary>
        /// <param name="group">The group to add.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the group but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddGroup(TGroup group, Action<TGroup> postProcessingAction)
        {
            Action<TGroup, Action<TGroup, Action>> addGroupAction = (actionGroup, baseAction) =>
            {
                accessManagerPrivateInterface.AddGroupWithWrappingActionMethod(group, baseAction);
                postProcessingAction.Invoke(group);
            };
            CallAccessManagerEventProcessingMethodWithMetricLogging<TGroup, GroupAddTime, GroupAdded>(group, addGroupAction);
        }

        /// <summary>
        /// Returns true if the specified group exists.
        /// </summary>
        /// <param name="group">The group to check for.</param>
        /// <returns>True if the group exists.  False otherwise.</returns>
        public Boolean ContainsGroup(TGroup group)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<Boolean, ContainsGroupQuery>(() => { return accessManagerPrivateInterface.ContainsGroupMethod(group); });
        }

        /// <summary>
        /// Removes a group.
        /// </summary>
        /// <param name="group">The group to remove.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the group but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveGroup(TGroup group, Action<TGroup> postProcessingAction)
        {
            Action<TGroup, Action> wrappingAction = (actionGroup, baseAction) =>
            {
                Int32 newGroupToApplicationComponentAndAccessLevelMappingCount = groupToApplicationComponentAndAccessLevelMappingCount;
                if (accessManagerPrivateInterface.GroupToComponentMap.ContainsKey(group) == true)
                {
                    newGroupToApplicationComponentAndAccessLevelMappingCount -= accessManagerPrivateInterface.GroupToComponentMap[group].Count;
                }
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GroupRemoveTime());
                try
                {
                    baseAction.Invoke();
                    postProcessingAction.Invoke(group);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GroupRemoveTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GroupRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new GroupRemoved());
                groupToApplicationComponentAndAccessLevelMappingCount = newGroupToApplicationComponentAndAccessLevelMappingCount;
                SetStatusMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingsStored(), groupToApplicationComponentAndAccessLevelMappingCount);
                groupToEntityMappingCount -= groupToEntityMappingCountPerGroup.GetFrequency(group);
                if (groupToEntityMappingCountPerGroup.GetFrequency(group) > 0)
                {
                    groupToEntityMappingCountPerGroup.DecrementBy(group, groupToEntityMappingCountPerGroup.GetFrequency(group));
                }
                SetStatusMetricIfLoggingEnabled(new GroupToEntityMappingsStored(), groupToEntityMappingCount);
            };
            accessManagerPrivateInterface.RemoveGroupWithWrappingActionMethod(group, wrappingAction);
        }

        /// <summary>
        /// Adds a mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postProcessingAction)
        {
            Action<TUser, TGroup, Action<TUser, TGroup, Action>> addUserToGroupMappingAction = (actionUser, actionGroup, baseAction) =>
            {
                accessManagerPrivateInterface.AddUserToGroupMappingWithWrappingActionMethod(user, group, baseAction);
                postProcessingAction.Invoke(user, group);
            };
            CallAccessManagerEventProcessingMethodWithMetricLogging<TUser, TGroup, UserToGroupMappingAddTime, UserToGroupMappingAdded>(user, group, addUserToGroupMappingAction);
        }

        /// <summary>
        /// Gets the groups that the specified user is mapped to (i.e. is a member of).
        /// </summary>
        /// <param name="user">The user to retrieve the groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those that occur via group to group mappings).</param>
        /// <returns>A collection of groups the specified user is a member of.</returns>
        public HashSet<TGroup> GetUserToGroupMappings(TUser user, Boolean includeIndirectMappings)
        {
            HashSet<TGroup> result;
            if (includeIndirectMappings == false)
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetUserToGroupMappingsQueryTime());
                try
                {
                    result = accessManagerPrivateInterface.GetUserToGroupMappingsMethod(user, includeIndirectMappings);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GetUserToGroupMappingsQueryTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GetUserToGroupMappingsQueryTime());
                IncrementCountMetricIfLoggingEnabled(new GetUserToGroupMappingsQuery());
            }
            else
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetUserToGroupMappingsWithIndirectMappingsQueryTime());
                try
                {
                    result = accessManagerPrivateInterface.GetUserToGroupMappingsMethod(user, includeIndirectMappings);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GetUserToGroupMappingsWithIndirectMappingsQueryTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GetUserToGroupMappingsWithIndirectMappingsQueryTime());
                IncrementCountMetricIfLoggingEnabled(new GetUserToGroupMappingsWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <summary>
        /// Removes the mapping between the specified user and group.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postProcessingAction)
        {
            Action<TUser, TGroup, Action<TUser, TGroup, Action>> removeUserToGroupMappingAction = (actionUser, actionGroup, baseAction) =>
            {
                accessManagerPrivateInterface.RemoveUserToGroupMappingWithWrappingActionMethod(user, group, baseAction);
                postProcessingAction.Invoke(user, group);
            };
            CallAccessManagerEventProcessingMethodWithMetricLogging<TUser, TGroup, UserToGroupMappingRemoveTime, UserToGroupMappingRemoved>(user, group, removeUserToGroupMappingAction);
        }

        /// <summary>
        /// Adds a mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postProcessingAction)
        {
            Action<TGroup, TGroup, Action<TGroup, TGroup, Action>> addGroupToGroupMappingAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                accessManagerPrivateInterface.AddGroupToGroupMappingWithWrappingActionMethod(fromGroup, toGroup, baseAction);
                postProcessingAction.Invoke(fromGroup, toGroup);
            };
            CallAccessManagerEventProcessingMethodWithMetricLogging<TGroup, TGroup, GroupToGroupMappingAddTime, GroupToGroupMappingAdded>(fromGroup, toGroup, addGroupToGroupMappingAction);
        }

        /// <summary>
        /// Gets the groups that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mapped groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where the 'mapped to' group is itself mapped to further groups).</param>
        /// <returns>A collection of groups the specified group is mapped to.</returns>
        public HashSet<TGroup> GetGroupToGroupMappings(TGroup group, Boolean includeIndirectMappings)
        {
            HashSet<TGroup> result;
            if (includeIndirectMappings == false)
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetGroupToGroupMappingsQueryTime());
                try
                {
                    result = accessManagerPrivateInterface.GetGroupToGroupMappingsMethod(group, includeIndirectMappings);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupMappingsQueryTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupMappingsQueryTime());
                IncrementCountMetricIfLoggingEnabled(new GetGroupToGroupMappingsQuery());
            }
            else
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetGroupToGroupMappingsWithIndirectMappingsQueryTime());
                try
                {
                    result = accessManagerPrivateInterface.GetGroupToGroupMappingsMethod(group, includeIndirectMappings);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupMappingsWithIndirectMappingsQueryTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupMappingsWithIndirectMappingsQueryTime());
                IncrementCountMetricIfLoggingEnabled(new GetGroupToGroupMappingsWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <summary>
        /// Removes the mapping between the specified groups.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postProcessingAction)
        {
            Action<TGroup, TGroup, Action<TGroup, TGroup, Action>> removeGroupToGroupMappingAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                accessManagerPrivateInterface.RemoveGroupToGroupMappingWithWrappingActionMethod(fromGroup, toGroup, baseAction);
                postProcessingAction.Invoke(fromGroup, toGroup);
            };
            CallAccessManagerEventProcessingMethodWithMetricLogging<TGroup, TGroup, GroupToGroupMappingRemoveTime, GroupToGroupMappingRemoved>(fromGroup, toGroup, removeGroupToGroupMappingAction);
        }

        /// <summary>
        /// Adds a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postProcessingAction)
        {
            Action<TUser, TComponent, TAccess, Action> wrappingAction = (actionUser, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingAddTime());
                try
                {
                    baseAction.Invoke();
                    postProcessingAction.Invoke(user, applicationComponent, accessLevel);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new UserToApplicationComponentAndAccessLevelMappingAddTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new UserToApplicationComponentAndAccessLevelMappingAddTime());
                IncrementCountMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingAdded());
                userToApplicationComponentAndAccessLevelMappingCount++;
                SetStatusMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingsStored(), userToApplicationComponentAndAccessLevelMappingCount);
            };
            accessManagerPrivateInterface.AddUserToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod(user, applicationComponent, accessLevel, wrappingAction);
        }

        /// <summary>
        /// Gets the application component and access level pairs that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <returns>A collection of Tuples containing the application component and access level pairs that the specified user is mapped to.</returns>
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<Tuple<TComponent, TAccess>>, GetUserToApplicationComponentAndAccessLevelMappingsQuery>(() =>
            {
                return accessManagerPrivateInterface.GetUserToApplicationComponentAndAccessLevelMappingsMethod(user);
            });
        }

        /// <summary>
        /// Removes a mapping between the specified user, application component, and level of access to that component.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postProcessingAction)
        {
            Action<TUser, TComponent, TAccess, Action> wrappingAction = (actionUser, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
                try
                {
                    baseAction.Invoke();
                    postProcessingAction.Invoke(user, applicationComponent, accessLevel);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingRemoved());
                userToApplicationComponentAndAccessLevelMappingCount--;
                SetStatusMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingsStored(), userToApplicationComponentAndAccessLevelMappingCount);
            };
            accessManagerPrivateInterface.RemoveUserToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod(user, applicationComponent, accessLevel, wrappingAction);
        }

        /// <summary>
        /// Adds a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>/// <inheritdoc/>
        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postProcessingAction)
        {
            Action<TGroup, TComponent, TAccess, Action> wrappingAction = (actionGroup, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingAddTime());
                try
                {
                    baseAction.Invoke();
                    postProcessingAction.Invoke(group, applicationComponent, accessLevel);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GroupToApplicationComponentAndAccessLevelMappingAddTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GroupToApplicationComponentAndAccessLevelMappingAddTime());
                IncrementCountMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingAdded());
                groupToApplicationComponentAndAccessLevelMappingCount++;
                SetStatusMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingsStored(), groupToApplicationComponentAndAccessLevelMappingCount);
            };
            accessManagerPrivateInterface.AddGroupToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod(group, applicationComponent, accessLevel, wrappingAction);
        }

        /// <summary>
        /// Gets the application component and access level pairs that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <returns>A collection of Tuples containing the application component and access level pairs that the specified group is mapped to.</returns>
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<Tuple<TComponent, TAccess>>, GetGroupToApplicationComponentAndAccessLevelMappingsQuery>(() =>
            {
                return accessManagerPrivateInterface.GetGroupToApplicationComponentAndAccessLevelMappingsMethod(group);
            });
        }

        /// <summary>
        /// Removes a mapping between the specified group, application component, and level of access to that component.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postProcessingAction)
        {
            Action<TGroup, TComponent, TAccess, Action> wrappingAction = (actionGroup, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
                try
                {
                    baseAction.Invoke();
                    postProcessingAction.Invoke(group, applicationComponent, accessLevel);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingRemoved());
                groupToApplicationComponentAndAccessLevelMappingCount--;
                SetStatusMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingsStored(), groupToApplicationComponentAndAccessLevelMappingCount);
            };
            accessManagerPrivateInterface.RemoveGroupToApplicationComponentAndAccessLevelMappingWithWrappingActionMethod(group, applicationComponent, accessLevel, wrappingAction);
        }

        /// <summary>
        /// Adds an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the entity type but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddEntityType(String entityType, Action<String> postProcessingAction)
        {
            Action<String, Action> wrappingAction = (actionEntityType, baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new EntityTypeAddTime());
                try
                {
                    baseAction.Invoke();
                    postProcessingAction.Invoke(entityType);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new EntityTypeAddTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new EntityTypeAddTime());
                IncrementCountMetricIfLoggingEnabled(new EntityTypeAdded());
                SetStatusMetricIfLoggingEnabled(new EntityTypesStored(), accessManagerPrivateInterface.Entities.Count);
            };
            accessManagerPrivateInterface.AddEntityTypeWithWrappingActionMethod(entityType, wrappingAction);
        }

        /// <summary>
        /// Returns true if the specified entity type exists.
        /// </summary>
        /// <param name="entityType">The entity type to check for.</param>
        /// <returns>True if the entity type exists.  False otherwise.</returns>
        public Boolean ContainsEntityType(String entityType)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<Boolean, ContainsEntityTypeQuery>(() => { return accessManagerPrivateInterface.ContainsEntityTypeMethod(entityType); });
        }

        /// <summary>
        /// Removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the entity type but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveEntityType(String entityType, Action<String> postProcessingAction)
        {
            Action<String, Action> wrappingAction = (actionEntityType, baseAction) =>
            {
                Action<TUser, String, IEnumerable<String>, Int32> userToEntityTypeMappingPreRemovalAction = (preRemovalActionUser, preRemovalActionEntityType, preRemovalActionEntities, preRemovalActionCount) =>
                {
                    if (preRemovalActionCount > 0)
                    {
                        userToEntityMappingCount -= preRemovalActionCount;
                        userToEntityMappingCountPerUser.DecrementBy(preRemovalActionUser, preRemovalActionCount);
                    }
                };
                Action<TGroup, String, IEnumerable<String>, Int32> groupToEntityTypeMappingPreRemovalAction = (preRemovalActionGroup, preRemovalActionEntityType, preRemovalActionEntities, preRemovalActionCount) =>
                {
                    if (preRemovalActionCount > 0)
                    {
                        groupToEntityMappingCount -= preRemovalActionCount;
                        groupToEntityMappingCountPerGroup.DecrementBy(preRemovalActionGroup, preRemovalActionCount);
                    }
                };

                Int32 newEntityCount = entityCount;
                if (accessManagerPrivateInterface.Entities.ContainsKey(entityType) == true)
                {
                    newEntityCount -= accessManagerPrivateInterface.Entities[entityType].Count;
                }
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new EntityTypeRemoveTime());
                try
                {
                    accessManagerPrivateInterface.RemoveEntityTypeWithPreRemovalActionsMethod(entityType, userToEntityTypeMappingPreRemovalAction, groupToEntityTypeMappingPreRemovalAction);
                    postProcessingAction.Invoke(entityType);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new EntityTypeRemoveTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new EntityTypeRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new EntityTypeRemoved());
                SetStatusMetricIfLoggingEnabled(new EntityTypesStored(), accessManagerPrivateInterface.Entities.Count);
                entityCount = newEntityCount;
                SetStatusMetricIfLoggingEnabled(new EntitiesStored(), entityCount);
                SetStatusMetricIfLoggingEnabled(new UserToEntityMappingsStored(), userToEntityMappingCount);
                SetStatusMetricIfLoggingEnabled(new GroupToEntityMappingsStored(), groupToEntityMappingCount);
            };
            accessManagerPrivateInterface.RemoveEntityTypeWithWrappingActionMethod(entityType, wrappingAction);
        }

        /// <summary>
        /// Adds an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the entity but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddEntity(String entityType, String entity, Action<String, String> postProcessingAction)
        {
            Action<String, String, Action> wrappingAction = (actionEntityType, actionEntity, baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new EntityAddTime());
                try
                {
                    baseAction.Invoke();
                    postProcessingAction.Invoke(entityType, entity);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new EntityAddTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new EntityAddTime());
                IncrementCountMetricIfLoggingEnabled(new EntityAdded());
                entityCount++;
                SetStatusMetricIfLoggingEnabled(new EntitiesStored(), entityCount);
            };
            accessManagerPrivateInterface.AddEntityWithWrappingActionMethod(entityType, entity, wrappingAction);
        }

        /// <summary>
        /// Returns all entities of the specified type.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <returns>A collection of all entities of the specified type.</returns>
        public IEnumerable<String> GetEntities(String entityType)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<String>, GetEntitiesQuery>(() => { return accessManagerPrivateInterface.GetEntitiesMethod(entityType); });
        }

        /// <summary>
        /// Returns true if the specified entity exists.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to check for.</param>
        /// <returns>True if the entity exists.  False otherwise.</returns>
        public Boolean ContainsEntity(String entityType, String entity)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<Boolean, ContainsEntityQuery>(() => { return accessManagerPrivateInterface.ContainsEntityMethod(entityType, entity); });
        }

        /// <summary>
        /// Removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the entity but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveEntity(String entityType, String entity, Action<String, String> postProcessingAction)
        {
            Action<String, String, Action> wrappingAction = (actionEntityType, actionEntity, baseAction) =>
            {
                Action<TUser, String, String> userToEntityMappingPostRemovalAction = (postRemovalActionUser, postRemovalActionEntityType, postRemovalActionEntity) =>
                {
                    userToEntityMappingCount--;
                    userToEntityMappingCountPerUser.Decrement(postRemovalActionUser);
                };
                Action<TGroup, String, String> groupToEntityMappingPostRemovalAction = (postRemovalActionGroup, postRemovalActionEntityType, postRemovalActionEntity) =>
                {
                    groupToEntityMappingCount--;
                    groupToEntityMappingCountPerGroup.Decrement(postRemovalActionGroup);
                };

                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new EntityRemoveTime());
                try
                {
                    accessManagerPrivateInterface.RemoveEntityWithPostRemovalActionsMethod(entityType, entity, userToEntityMappingPostRemovalAction, groupToEntityMappingPostRemovalAction);
                    postProcessingAction.Invoke(entityType, entity);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new EntityRemoveTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new EntityRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new EntityRemoved());
                entityCount--;
                SetStatusMetricIfLoggingEnabled(new EntitiesStored(), entityCount);
                SetStatusMetricIfLoggingEnabled(new UserToEntityMappingsStored(), userToEntityMappingCount);
                SetStatusMetricIfLoggingEnabled(new GroupToEntityMappingsStored(), groupToEntityMappingCount);
            };
            accessManagerPrivateInterface.RemoveEntityWithWrappingActionMethod(entityType, entity, wrappingAction);
        }

        /// <summary>
        /// Adds a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postProcessingAction)
        {
            Action<TUser, String, String, Action> wrappingAction = (actionUser, actionEntityType, actionEntity, baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new UserToEntityMappingAddTime());
                try
                {
                    baseAction.Invoke();
                    postProcessingAction.Invoke(user, entityType, entity);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new UserToEntityMappingAddTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new UserToEntityMappingAddTime());
                IncrementCountMetricIfLoggingEnabled(new UserToEntityMappingAdded());
                userToEntityMappingCount++;
                userToEntityMappingCountPerUser.Increment(user);
                SetStatusMetricIfLoggingEnabled(new UserToEntityMappingsStored(), userToEntityMappingCount);
            };
            accessManagerPrivateInterface.AddUserToEntityMappingWithWrappingActionMethod(user, entityType, entity, wrappingAction);
        }

        /// <summary>
        /// Gets the entities that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the specified user is mapped to.</returns>
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<Tuple<String, String>>, GetUserToEntityMappingsForUserQuery>(() =>
            {
                return accessManagerPrivateInterface.GetUserToEntityMappingsMethod(user);
            });
        }

        /// <summary>
        /// Gets the entities of a given type that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <returns>A collection of entities that the specified user is mapped to.</returns>
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<String>, GetUserToEntityMappingsForUserAndEntityTypeQuery>(() =>
            {
                return accessManagerPrivateInterface.GetUserToEntityMappingsWithEntityTypeMethod(user, entityType);
            });
        }

        /// <summary>
        /// Removes a mapping between the specified user, and entity.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postProcessingAction)
        {
            Action<TUser, String, String, Action> wrappingAction = (actionUser, actionEntityType, actionEntity, baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new UserToEntityMappingRemoveTime());
                try
                {
                    baseAction.Invoke();
                    postProcessingAction.Invoke(user, entityType, entity);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new UserToEntityMappingRemoveTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new UserToEntityMappingRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new UserToEntityMappingRemoved());
                userToEntityMappingCount--;
                userToEntityMappingCountPerUser.Decrement(user);
                SetStatusMetricIfLoggingEnabled(new UserToEntityMappingsStored(), userToEntityMappingCount);
            };
            accessManagerPrivateInterface.RemoveUserToEntityMappingWithPostRemovalActionsMethod(user, entityType, entity, wrappingAction);
        }

        /// <summary>
        /// Adds a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after adding the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postProcessingAction)
        {
            Action<TGroup, String, String, Action> wrappingAction = (actionGroup, actionEntityType, actionEntity, baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GroupToEntityMappingAddTime());
                try
                {
                    baseAction.Invoke();
                    postProcessingAction.Invoke(group, entityType, entity);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GroupToEntityMappingAddTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GroupToEntityMappingAddTime());
                IncrementCountMetricIfLoggingEnabled(new GroupToEntityMappingAdded());
                groupToEntityMappingCount++;
                groupToEntityMappingCountPerGroup.Increment(group);
                SetStatusMetricIfLoggingEnabled(new GroupToEntityMappingsStored(), groupToEntityMappingCount);
            };
            accessManagerPrivateInterface.AddGroupToEntityMappingWithWrappingActionMethod(group, entityType, entity, wrappingAction);
        }

        /// <summary>
        /// Gets the entities that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the specified group is mapped to.</returns>
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<Tuple<String, String>>, GetGroupToEntityMappingsForGroupQuery>(() =>
            {
                return accessManagerPrivateInterface.GetGroupToEntityMappingsMethod(group);
            });
        }

        /// <summary>
        /// Gets the entities of a given type that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <returns>A collection of entities that the specified group is mapped to.</returns>
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<String>, GetGroupToEntityMappingsForGroupAndEntityTypeQuery>(() =>
            {
                return accessManagerPrivateInterface.GetGroupToEntityMappingsWithEntityTypeMethod(group, entityType);
            });
        }

        /// <summary>
        /// Removes a mapping between the specified group, and entity.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="postProcessingAction">An action to invoke after removing the mapping but whilst any mutual-exclusion locks are still acquired.</param>
        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postProcessingAction)
        {
            Action<TGroup, String, String, Action> wrappingAction = (actionGroup, actionEntityType, actionEntity, baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GroupToEntityMappingRemoveTime());
                try
                {
                    baseAction.Invoke();
                    postProcessingAction.Invoke(group, entityType, entity);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GroupToEntityMappingRemoveTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GroupToEntityMappingRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new GroupToEntityMappingRemoved());
                groupToEntityMappingCount--;
                groupToEntityMappingCountPerGroup.Decrement(group);
                SetStatusMetricIfLoggingEnabled(new GroupToEntityMappingsStored(), userToEntityMappingCount);
            };
            accessManagerPrivateInterface.RemoveGroupToEntityMappingWithPostRemovalActionsMethod(group, entityType, entity, wrappingAction);
        }

        /// <summary>
        /// Checks whether the specified user (or a group that the user is a member of) has access to an application component at the specified level of access.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <param name="applicationComponent">The application component.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <returns>True if the user has access the component.  False otherwise.</returns>
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Boolean result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new HasAccessToApplicationComponentQueryTime());
            try
            {
                result = accessManagerPrivateInterface.HasAccessToApplicationComponentMethod(user, applicationComponent, accessLevel);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new HasAccessToApplicationComponentQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new HasAccessToApplicationComponentQueryTime());
            IncrementCountMetricIfLoggingEnabled(new HasAccessToApplicationComponentQuery());

            return result;
        }

        /// <summary>
        /// Checks whether the specified user (or a group that the user is a member of) has access to the specified entity.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>True if the user has access the entity.  False otherwise.</returns>
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            Boolean result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new HasAccessToEntityQueryTime());
            try
            {
                result = accessManagerPrivateInterface.HasAccessToEntityMethod(user, entityType, entity);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new HasAccessToEntityQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new HasAccessToEntityQueryTime());
            IncrementCountMetricIfLoggingEnabled(new HasAccessToEntityQuery());

            return result;
        }

        /// <summary>
        /// Gets all application components and levels of access that the specified user (or a group that the user is a member of) has access to.
        /// </summary>
        /// <param name="user">The user to retrieve the application components and levels of access for.</param>
        /// <returns>The application components and levels of access to those application components that the user has.</returns>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            HashSet<Tuple<TComponent, TAccess>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetApplicationComponentsAccessibleByUserQueryTime());
            try
            {
                result = accessManagerPrivateInterface.GetApplicationComponentsAccessibleByUserMethod(user);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new GetApplicationComponentsAccessibleByUserQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new GetApplicationComponentsAccessibleByUserQueryTime());
            IncrementCountMetricIfLoggingEnabled(new GetApplicationComponentsAccessibleByUserQuery());

            return result;
        }

        /// <summary>
        /// Gets all application components and levels of access that the specified group (or group that the specified group is mapped to) has access to.
        /// </summary>
        /// <param name="group">The group to retrieve the application components and levels of access for.</param>
        /// <returns>The application components and levels of access to those application components that the group has.</returns>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            HashSet<Tuple<TComponent, TAccess>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetApplicationComponentsAccessibleByGroupQueryTime());
            try
            {
                result = accessManagerPrivateInterface.GetApplicationComponentsAccessibleByGroupMethod(group);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new GetApplicationComponentsAccessibleByGroupQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new GetApplicationComponentsAccessibleByGroupQueryTime());
            IncrementCountMetricIfLoggingEnabled(new GetApplicationComponentsAccessibleByGroupQuery());

            return result;
        }

        /// <summary>
        /// Gets all entities that the specified user (or a group that the user is a member of) has access to.
        /// </summary>
        /// <param name="user">The user to retrieve the entities for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the user has access to.</returns>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByUser(TUser user)
        {
            HashSet<Tuple<String, String>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByUserQueryTime());
            try
            {
                result = accessManagerPrivateInterface.GetEntitiesAccessibleByUserMethod(user);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new GetEntitiesAccessibleByUserQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new GetEntitiesAccessibleByUserQueryTime());
            IncrementCountMetricIfLoggingEnabled(new GetEntitiesAccessibleByUserQuery());

            return result;
        }

        /// <summary>
        /// Gets all entities of a given type that the specified user (or a group that the user is a member of) has access to.
        /// </summary>
        /// <param name="user">The user to retrieve the entities for.</param>
        /// <param name="entityType">The type of entities to retrieve.</param>
        /// <returns>The entities the user has access to.</returns>
        public HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            HashSet<String> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByUserQueryTime());
            try
            {
                result = accessManagerPrivateInterface.GetEntitiesAccessibleByUserWithEntityTypeMethod(user, entityType);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new GetEntitiesAccessibleByUserQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new GetEntitiesAccessibleByUserQueryTime());
            IncrementCountMetricIfLoggingEnabled(new GetEntitiesAccessibleByUserQuery());

            return result;
        }

        /// <summary>
        /// Gets all entities that the specified group (or group that the specified group is mapped to) has access to.
        /// </summary>
        /// <param name="group">The group to retrieve the entities for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the group has access to.</returns>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroup(TGroup group)
        {
            HashSet<Tuple<String, String>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByGroupQueryTime());
            try
            {
                result = accessManagerPrivateInterface.GetEntitiesAccessibleByGroupMethod(group);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new GetEntitiesAccessibleByGroupQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new GetEntitiesAccessibleByGroupQueryTime());
            IncrementCountMetricIfLoggingEnabled(new GetEntitiesAccessibleByGroupQuery());

            return result;
        }

        /// <summary>
        /// Gets all entities of a given type that the specified group (or group that the specified group is mapped to) has access to.
        /// </summary>
        /// <param name="group">The group to retrieve the entities for.</param>
        /// <param name="entityType">The type of entities to retrieve.</param>
        /// <returns>The entities the group has access to.</returns>
        public HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            HashSet<String> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByGroupQueryTime());
            try
            {
                result = accessManagerPrivateInterface.GetEntitiesAccessibleByGroupWithEntityTypeMethod(group, entityType);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new GetEntitiesAccessibleByGroupQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new GetEntitiesAccessibleByGroupQueryTime());
            IncrementCountMetricIfLoggingEnabled(new GetEntitiesAccessibleByGroupQuery());

            return result;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the class fields which store counts of items and mappings.
        /// </summary>
        protected void InitializeItemAndMappingCountFields()
        {
            userToApplicationComponentAndAccessLevelMappingCount = 0;
            groupToApplicationComponentAndAccessLevelMappingCount = 0;
            entityCount = 0;
            userToEntityMappingCount = 0;
            userToEntityMappingCountPerUser = new FrequencyTable<TUser>();
            groupToEntityMappingCount = 0;
            groupToEntityMappingCountPerGroup = new FrequencyTable<TGroup>();
        }

        /// <summary>
        /// Calls one of the <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> methods which implements IAccessManagerEventProcessor, wrapping the call with logging of metric events of the specified types.
        /// </summary>
        /// <typeparam name="TEventProcessorMethodParam">The type of the parameter which is passed to the IAccessManagerEventProcessor method.</typeparam>
        /// <typeparam name="TIntervalMetric">The type of interval metric to log.</typeparam>
        /// <typeparam name="TCountMetric">The type of count metric to log.</typeparam>
        /// <param name="parameterValue">The value of the parameter which is passed to the IAccessManagerEventProcessor method.</param>
        /// <param name="eventProcessorMethodAction">Action which calls the access manager IAccessManagerEventProcessor method.  Accepts 2 parameters: the type of the parameter which is passed to the IAccessManagerEventProcessor method, and an inner action which performs the call to the access manager IAccessManagerEventProcessor method (and which is invoked during the invocation of the outer 'eventProcessorMethodAction').</param>
        protected void CallAccessManagerEventProcessingMethodWithMetricLogging<TEventProcessorMethodParam, TIntervalMetric, TCountMetric>
        (
            TEventProcessorMethodParam parameterValue,
            Action<TEventProcessorMethodParam, Action<TEventProcessorMethodParam, Action>> eventProcessorMethodAction
        )
            where TIntervalMetric : IntervalMetric, new()
            where TCountMetric : CountMetric, new()
        {
            Action<TEventProcessorMethodParam, Action> wrappingAction = (actionParameter, baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new TIntervalMetric());
                try
                {
                    baseAction.Invoke();
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new TIntervalMetric());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new TIntervalMetric());
                IncrementCountMetricIfLoggingEnabled(new TCountMetric());
            };
            eventProcessorMethodAction.Invoke(parameterValue, wrappingAction);
        }

        /// <summary>
        /// Calls one of the <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> methods which implements IAccessManagerEventProcessor, wrapping the call with logging of metric events of the specified types.
        /// </summary>
        /// <typeparam name="TEventProcessorMethodParam1">The type of the first parameter which is passed to the IAccessManagerEventProcessor method.</typeparam>
        /// <typeparam name="TEventProcessorMethodParam1">The type of the second parameter which is passed to the IAccessManagerEventProcessor method.</typeparam>
        /// <typeparam name="TIntervalMetric">The type of interval metric to log.</typeparam>
        /// <typeparam name="TCountMetric">The type of count metric to log.</typeparam>
        /// <param name="parameterValue1">The value of the first parameter which is passed to the IAccessManagerEventProcessor method.</param>
        /// <param name="parameterValue1">The value of the second parameter which is passed to the IAccessManagerEventProcessor method.</param>
        /// <param name="eventProcessorMethodAction">Action which calls the access manager IAccessManagerEventProcessor method.  Accepts 3 parameters: the type of the first parameter which is passed to the IAccessManagerEventProcessor method, the type of the second parameter which is passed to the IAccessManagerEventProcessor method, and an inner action which performs the call to the access manager IAccessManagerEventProcessor method (and which is invoked during the invocation of the outer 'eventProcessorMethodAction').</param>
        protected void CallAccessManagerEventProcessingMethodWithMetricLogging<TEventProcessorMethodParam1, TEventProcessorMethodParam2, TIntervalMetric, TCountMetric>
        (
            TEventProcessorMethodParam1 parameterValue1,
            TEventProcessorMethodParam2 parameterValue2,
            Action<TEventProcessorMethodParam1, TEventProcessorMethodParam2, Action<TEventProcessorMethodParam1, TEventProcessorMethodParam2, Action>> eventProcessorMethodAction
        )
            where TIntervalMetric : IntervalMetric, new()
            where TCountMetric : CountMetric, new()
        {
            Action<TEventProcessorMethodParam1, TEventProcessorMethodParam2, Action> wrappingAction = (actionParameter1, actionParameter2, baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new TIntervalMetric());
                try
                {
                    baseAction.Invoke();
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new TIntervalMetric());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new TIntervalMetric());
                IncrementCountMetricIfLoggingEnabled(new TCountMetric());
            };
            eventProcessorMethodAction.Invoke(parameterValue1, parameterValue2, wrappingAction);
        }

        /// <summary>
        /// Calls one of the <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> methods which implements IAccessManagerQueryProcessor, logging the specified count metric.
        /// </summary>
        /// <typeparam name="TQueryProcessorMethodReturn">The type of the value returned from the IAccessManagerQueryProcessor method or property.</typeparam>
        /// <typeparam name="TCountMetric">The type of count metric to log.</typeparam>
        /// <param name="queryProcessorMethodFunc">Func which calls the access manager IAccessManagerQueryProcessor method or property.  Returns the value from the access manager IAccessManagerQueryProcessor method or property.</param>
        /// <returns>The result of the IAccessManagerQueryProcessor method.</returns>
        protected TQueryProcessorMethodReturn CallAccessManagerQueryProcessingMethodWithMetricLogging<TQueryProcessorMethodReturn, TCountMetric>(Func<TQueryProcessorMethodReturn> queryProcessorMethodFunc)
            where TCountMetric : CountMetric, new()
        {
            TQueryProcessorMethodReturn result = queryProcessorMethodFunc.Invoke();
            IncrementCountMetricIfLoggingEnabled(new TCountMetric());

            return result;
        }

        /// <summary>
        /// Logs the specified count metric if logging is enabled.
        /// </summary>
        /// <param name="statusMetric">The count metric to log.</param>
        protected void IncrementCountMetricIfLoggingEnabled(CountMetric countMetric)
        {
            if (metricLoggingEnabled == true)
                metricLogger.Increment(countMetric);
        }

        /// <summary>
        /// Logs the specified amount metric if logging is enabled.
        /// </summary>
        /// <param name="amountMetric">The amount metric to log.</param>
        /// <param name="value">The amount associated with the instance of the amount metric.</param>
        protected void AddStatusMetricIfLoggingEnabled(AmountMetric amountMetric, Int64 amount)
        {
            if (metricLoggingEnabled == true)
                metricLogger.Add(amountMetric, amount);
        }

        /// <summary>
        /// Logs the specified status metric if logging is enabled.
        /// </summary>
        /// <param name="statusMetric">The status metric to log.</param>
        /// <param name="value">The value associated with the instance of the status metric.</param>
        protected void SetStatusMetricIfLoggingEnabled(StatusMetric statusMetric, Int64 value)
        {
            if (metricLoggingEnabled == true)
                metricLogger.Set(statusMetric, value);
        }

        /// <summary>
        /// Logs the starting of the specified interval metric if logging is enabled.
        /// </summary>
        /// <param name="intervalMetric">The interval metric to start.</param>
        /// <returns>A unique id for the starting of the interval metric, or null if metric logging is disabled.</returns>
        protected Nullable<Guid> BeginIntervalMetricIfLoggingEnabled(IntervalMetric intervalMetric)
        {
            if (metricLoggingEnabled == true)
            {
                return metricLogger.Begin(intervalMetric);
            }
            else
            {
                return null;
            }
        }

        /// <summary>
        /// Logs the completion of the specified interval metric if logging is enabled.
        /// </summary>
        /// <param name="beginId">The id corresponding to the starting of the specified interval metric, or null if metric logging is disabled.</param>
        /// <param name="intervalMetric">The interval metric to complete.</param>
        protected void EndIntervalMetricIfLoggingEnabled(Nullable<Guid> beginId, IntervalMetric intervalMetric)
        {
            if (metricLoggingEnabled == true && beginId.HasValue == true)
            {
                metricLogger.End(beginId.Value, intervalMetric);
            }
        }

        /// <summary>
        /// Logs the cancellation of the starting of the specified interval metric if logging is enabled.
        /// </summary>
        /// <param name="beginId">The id corresponding to the starting of the specified interval metric, or null if metric logging is disabled.</param>
        /// <param name="intervalMetric">The interval metric to cancel.</param>
        protected void CancelIntervalMetricIfLoggingEnabled(Nullable<Guid> beginId, IntervalMetric intervalMetric)
        {
            if (metricLoggingEnabled == true && beginId.HasValue == true)
            {
                metricLogger.CancelBegin(beginId.Value, intervalMetric);
            }
        }

        #endregion
    }
}
