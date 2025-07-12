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
    /// Logs metrics for an instance or subclass of <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> by providing <see cref="Action"/> and <see cref="Func{T, TResult}"/> methods which can be passed to the ConcurrentAccessManager instance event methods which accept the 'wrappingAction' parameter (for the case of event methods), or by wrapping/decorating the ConcurrentAccessManager instance methods (for the case of query methods).
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>This class provides a solution for wanting to have a 'MetricLogging' version of both the <see cref="ConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> and <see cref="DependencyFreeAccessManager{TUser, TGroup, TComponent, TAccess}"/> classes.  The contained Metric logging functionality can be reused by 'MetricLogging' subclasses of each.</remarks>
    public class ConcurrentAccessManagerMetricLogger<TUser, TGroup, TComponent, TAccess>
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

        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Whether logging of metrics is enabled.</summary>
        protected volatile Boolean metricLoggingEnabled;

        /// <summary>
        /// Whether logging of metrics is enabled.
        /// </summary>
        public Boolean MetricLoggingEnabled
        {
            get
            {
                return metricLoggingEnabled;
            }

            set
            {
                metricLoggingEnabled = value;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.ConcurrentAccessManagerMetricLogger class.
        /// </summary>
        /// <param name="metricLogger">The logger for metrics.</param>
        public ConcurrentAccessManagerMetricLogger(IMetricLogger metricLogger)
        {
            InitializeItemAndMappingCountFields();
            this.metricLogger = metricLogger;
            metricLoggingEnabled = true;
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class Clear() method.
        /// </summary>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the Clear() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <param name="entities">The dictionary which stores entity types and entities, in the ConcurrentAccessManager subclass that metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<Action> GenerateClearMetricLoggingWrappingAction(Action<Action> wrappingAction, IDictionary<String, ISet<String>> entities)
        {
            return (Action baseAction) =>
            {
                wrappingAction.Invoke(() =>
                {
                    baseAction.Invoke();
                });
                InitializeItemAndMappingCountFields();
                SetStatusMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingsStored(), userToApplicationComponentAndAccessLevelMappingCount);
                SetStatusMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingsStored(), groupToApplicationComponentAndAccessLevelMappingCount);
                SetStatusMetricIfLoggingEnabled(new EntityTypesStored(), entities.Count);
                SetStatusMetricIfLoggingEnabled(new EntitiesStored(), entityCount);
                SetStatusMetricIfLoggingEnabled(new UserToEntityMappingsStored(), userToEntityMappingCount);
                SetStatusMetricIfLoggingEnabled(new GroupToEntityMappingsStored(), groupToEntityMappingCount);
            };
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class AddUser() method.
        /// </summary>
        /// <param name="user">The user to add.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the AddUser() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TUser, Action> GenerateAddUserMetricLoggingWrappingAction(TUser user, Action<TUser, Action> wrappingAction)
        {
            return (TUser metricLoggingActionUser, Action baseAction) =>
            {
                CallAccessManagerEventProcessingMethodWithMetricLogging<UserAddTime, UserAdded>
                (
                    () =>
                    {
                        wrappingAction.Invoke(metricLoggingActionUser, () =>
                        {
                            baseAction.Invoke();
                        });
                    }
                );
            };
        }

        /// <summary>
        /// Returns true if the specified user exists.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>True if the user exists.  False otherwise.</returns>
        public Boolean ContainsUser(TUser user, Func<TUser, Boolean> baseClassMethod)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<Boolean, ContainsUserQuery>(() => { return baseClassMethod.Invoke(user); });
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class RemoveUser() method.
        /// </summary>
        /// <param name="user">The user to remove.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the RemoveUser() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <param name="userToComponentMap">The dictionary which stores mappings between users, application components, and levels of access, in the ConcurrentAccessManager subclass that metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TUser, Action> GenerateRemoveUserMetricLoggingWrappingAction
        (
            TUser user, 
            Action<TUser, Action> wrappingAction, 
            IDictionary<TUser, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>> userToComponentMap
        )
        {
            return (TUser metricLoggingActionUser, Action baseAction) =>
            {
                Int32 newUserToApplicationComponentAndAccessLevelMappingCount = userToApplicationComponentAndAccessLevelMappingCount;
                if (userToComponentMap.ContainsKey(metricLoggingActionUser) == true)
                {
                    newUserToApplicationComponentAndAccessLevelMappingCount -= userToComponentMap[metricLoggingActionUser].Count;
                }
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new UserRemoveTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionUser, () =>
                    {
                        baseAction.Invoke();
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new UserRemoveTime());
                    if (typeof(IdempotentRemoveOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new UserRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new UserRemoved());
                userToApplicationComponentAndAccessLevelMappingCount = newUserToApplicationComponentAndAccessLevelMappingCount;
                SetStatusMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingsStored(), userToApplicationComponentAndAccessLevelMappingCount);
                userToEntityMappingCount -= userToEntityMappingCountPerUser.GetFrequency(metricLoggingActionUser);
                if (userToEntityMappingCountPerUser.GetFrequency(metricLoggingActionUser) > 0)
                {
                    userToEntityMappingCountPerUser.DecrementBy(metricLoggingActionUser, userToEntityMappingCountPerUser.GetFrequency(metricLoggingActionUser));
                }
                SetStatusMetricIfLoggingEnabled(new UserToEntityMappingsStored(), userToEntityMappingCount);
            };
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class AddGroup() method.
        /// </summary>
        /// <param name="group">The group to add.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the AddGroup() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TGroup, Action> GenerateAddGroupMetricLoggingWrappingAction(TGroup group, Action<TGroup, Action> wrappingAction)
        {
            return (TGroup metricLoggingActionGroup, Action baseAction) =>
            {
                CallAccessManagerEventProcessingMethodWithMetricLogging<GroupAddTime, GroupAdded>
                (
                    () =>
                    {
                        wrappingAction.Invoke(metricLoggingActionGroup, () =>
                        {
                            baseAction.Invoke();
                        });
                    }
                );
            };
        }

        /// <summary>
        /// Returns true if the specified group exists.
        /// </summary>
        /// <param name="group">The group to check for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>True if the group exists.  False otherwise.</returns>
        public Boolean ContainsGroup(TGroup group, Func<TGroup, Boolean> baseClassMethod)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<Boolean, ContainsGroupQuery>(() => { return baseClassMethod.Invoke(group); });
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class RemoveGroup() method.
        /// </summary>
        /// <param name="group">The group to remove.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the RemoveGroup() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <param name="groupsToComponentMap">The dictionary which stores mappings between groups, application components, and levels of access, in the ConcurrentAccessManager subclass that metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TGroup, Action> GenerateRemoveGroupMetricLoggingWrappingAction
        (
            TGroup group, 
            Action<TGroup, Action> wrappingAction,
            IDictionary<TGroup, ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>> groupsToComponentMap
        )
        {
            return (TGroup metricLoggingActionGroup, Action baseAction) =>
            {
                Int32 newGroupToApplicationComponentAndAccessLevelMappingCount = groupToApplicationComponentAndAccessLevelMappingCount;
                if (groupsToComponentMap.ContainsKey(metricLoggingActionGroup) == true)
                {
                    newGroupToApplicationComponentAndAccessLevelMappingCount -= groupsToComponentMap[metricLoggingActionGroup].Count;
                }
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GroupRemoveTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionGroup, () =>
                    {
                        baseAction.Invoke();
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GroupRemoveTime());
                    if (typeof(IdempotentRemoveOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GroupRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new GroupRemoved());
                groupToApplicationComponentAndAccessLevelMappingCount = newGroupToApplicationComponentAndAccessLevelMappingCount;
                SetStatusMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingsStored(), groupToApplicationComponentAndAccessLevelMappingCount);
                groupToEntityMappingCount -= groupToEntityMappingCountPerGroup.GetFrequency(metricLoggingActionGroup);
                if (groupToEntityMappingCountPerGroup.GetFrequency(metricLoggingActionGroup) > 0)
                {
                    groupToEntityMappingCountPerGroup.DecrementBy(metricLoggingActionGroup, groupToEntityMappingCountPerGroup.GetFrequency(metricLoggingActionGroup));
                }
                SetStatusMetricIfLoggingEnabled(new GroupToEntityMappingsStored(), groupToEntityMappingCount);
            };
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class AddUserToGroupMapping() method.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the AddUserToGroupMapping() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TUser, TGroup, Action> GenerateAddUserToGroupMappingMetricLoggingWrappingAction(TUser user, TGroup group, Action<TUser, TGroup, Action> wrappingAction)
        {
            return (TUser metricLoggingActionUser, TGroup metricLoggingActionGroup, Action baseAction) =>
            {
                CallAccessManagerEventProcessingMethodWithMetricLogging<UserToGroupMappingAddTime, UserToGroupMappingAdded>
                (
                    () =>
                    {
                        wrappingAction.Invoke(metricLoggingActionUser, metricLoggingActionGroup, () =>
                        {
                            baseAction.Invoke();
                        });
                    }
                );
            };
        }

        /// <summary>
        /// Gets the groups that the specified user is mapped to (i.e. is a member of).
        /// </summary>
        /// <param name="user">The user to retrieve the groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those that occur via group to group mappings).</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of groups the specified user is a member of.</returns>
        public HashSet<TGroup> GetUserToGroupMappings(TUser user, Boolean includeIndirectMappings, Func<TUser, Boolean, HashSet<TGroup>> baseClassMethod)
        {
            HashSet<TGroup> result;
            if (includeIndirectMappings == false)
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetUserToGroupMappingsQueryTime());
                try
                {
                    result = baseClassMethod.Invoke(user, includeIndirectMappings);
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
                    result = baseClassMethod.Invoke(user, includeIndirectMappings);
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
        /// Gets the users that are mapped to the specified group.
        /// </summary>
        /// <param name="group">The group to retrieve the users for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a user is mapped to the group via other groups).</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of users that are mapped to the specified group.</returns>
        public HashSet<TUser> GetGroupToUserMappings(TGroup group, Boolean includeIndirectMappings, Func<TGroup, Boolean, HashSet<TUser>> baseClassMethod)
        {
            HashSet<TUser> result;
            if (includeIndirectMappings == false)
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetGroupToUserMappingsForGroupQueryTime());
                try
                {
                    result = baseClassMethod.Invoke(group, includeIndirectMappings);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GetGroupToUserMappingsForGroupQueryTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GetGroupToUserMappingsForGroupQueryTime());
                IncrementCountMetricIfLoggingEnabled(new GetGroupToUserMappingsForGroupQuery());
            }
            else
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime());
                try
                {
                    result = baseClassMethod.Invoke(group, includeIndirectMappings);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GetGroupToUserMappingsForGroupWithIndirectMappingsQueryTime());
                IncrementCountMetricIfLoggingEnabled(new GetGroupToUserMappingsForGroupWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <summary>
        /// Gets the users that are directly mapped to any of the specified groups.
        /// </summary>
        /// <param name="groups">The groups to retrieve the users for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of users that are mapped to the specified groups.</returns>
        public HashSet<TUser> GetGroupToUserMappings(IEnumerable<TGroup> groups, Func<IEnumerable<TGroup>, HashSet<TUser>> baseClassMethod)
        {
            HashSet<TUser> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetGroupToUserMappingsForGroupsQueryTime());
            try
            {
                result = baseClassMethod.Invoke(groups);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new GetGroupToUserMappingsForGroupsQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new GetGroupToUserMappingsForGroupsQueryTime());
            IncrementCountMetricIfLoggingEnabled(new GetGroupToUserMappingsForGroupsQuery());

            return result;
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class RemoveUserToGroupMapping() method.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the RemoveUserToGroupMapping() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TUser, TGroup, Action> GenerateRemoveUserToGroupMappingMetricLoggingWrappingAction(TUser user, TGroup group, Action<TUser, TGroup, Action> wrappingAction)
        {
            return (TUser metricLoggingActionUser, TGroup metricLoggingActionGroup, Action baseAction) =>
            {
                CallAccessManagerEventProcessingMethodWithMetricLogging<UserToGroupMappingRemoveTime, UserToGroupMappingRemoved>
                (
                    () =>
                    {
                        wrappingAction.Invoke(metricLoggingActionUser, metricLoggingActionGroup, () =>
                        {
                            baseAction.Invoke();
                        });
                    }
                );
            };
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class AddGroupToGroupMapping() method.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the AddGroupToGroupMapping() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TGroup, TGroup, Action> GenerateAddGroupToGroupMappingMetricLoggingWrappingAction(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup, Action> wrappingAction)
        {
            return (TGroup metricLoggingActionFromGroup, TGroup metricLoggingActionToGroup, Action baseAction) =>
            {
                CallAccessManagerEventProcessingMethodWithMetricLogging<GroupToGroupMappingAddTime, GroupToGroupMappingAdded>
                (
                    () =>
                    {
                        wrappingAction.Invoke(metricLoggingActionFromGroup, metricLoggingActionToGroup, () =>
                        {
                            baseAction.Invoke();
                        });
                    }
                );
            };
        }

        /// <summary>
        /// Gets the groups that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mapped groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where the 'mapped to' group is itself mapped to further groups).</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of groups the specified group is mapped to.</returns>
        public HashSet<TGroup> GetGroupToGroupMappings(TGroup group, Boolean includeIndirectMappings, Func<TGroup, Boolean, HashSet<TGroup>> baseClassMethod)
        {
            HashSet<TGroup> result;
            if (includeIndirectMappings == false)
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetGroupToGroupMappingsForGroupQueryTime());
                try
                {
                    result = baseClassMethod.Invoke(group, includeIndirectMappings);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupMappingsForGroupQueryTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupMappingsForGroupQueryTime());
                IncrementCountMetricIfLoggingEnabled(new GetGroupToGroupMappingsForGroupQuery());
            }
            else
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime());
                try
                {
                    result = baseClassMethod.Invoke(group, includeIndirectMappings);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupMappingsForGroupWithIndirectMappingsQueryTime());
                IncrementCountMetricIfLoggingEnabled(new GetGroupToGroupMappingsForGroupWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <summary>
        /// Gets the groups that all of the specified groups are directly and indirectly mapped to.
        /// </summary>
        /// <param name="groups">The groups to retrieve the mapped groups for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of groups the specified groups are mapped to, and including the specified groups.</returns>
        public HashSet<TGroup> GetGroupToGroupMappings(IEnumerable<TGroup> groups, Func<IEnumerable<TGroup>, HashSet<TGroup>> baseClassMethod)
        {
            HashSet<TGroup> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetGroupToGroupMappingsForGroupsQueryTime());
            try
            {
                result = baseClassMethod.Invoke(groups);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupMappingsForGroupsQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupMappingsForGroupsQueryTime());
            IncrementCountMetricIfLoggingEnabled(new GetGroupToGroupMappingsForGroupsQuery());

            return result;
        }

        /// <summary>
        /// Gets the groups that are mapped to the specified group.
        /// </summary>
        /// <param name="group">The group to retrieve the mapped groups for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where the 'mapped from' group is itself mapped from further groups).</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of groups that are mapped to the specified group.</returns>
        public HashSet<TGroup> GetGroupToGroupReverseMappings(TGroup group, Boolean includeIndirectMappings, Func<TGroup, Boolean, HashSet<TGroup>> baseClassMethod)
        {
            HashSet<TGroup> result;
            if (includeIndirectMappings == false)
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetGroupToGroupReverseMappingsForGroupQueryTime());
                try
                {
                    result = baseClassMethod.Invoke(group, includeIndirectMappings);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupReverseMappingsForGroupQueryTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupReverseMappingsForGroupQueryTime());
                IncrementCountMetricIfLoggingEnabled(new GetGroupToGroupReverseMappingsForGroupQuery());
            }
            else
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime());
                try
                {
                    result = baseClassMethod.Invoke(group, includeIndirectMappings);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime());
                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQueryTime());
                IncrementCountMetricIfLoggingEnabled(new GetGroupToGroupReverseMappingsForGroupWithIndirectMappingsQuery());
            }

            return result;
        }

        /// <summary>
        /// Gets the groups that are directly and indirectly mapped to any of the specified groups.
        /// </summary>
        /// <param name="groups">The groups to retrieve the mapped groups for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of groups that are mapped to the specified groups.</returns>
        public HashSet<TGroup> GetGroupToGroupReverseMappings(IEnumerable<TGroup> groups, Func<IEnumerable<TGroup>, HashSet<TGroup>> baseClassMethod)
        {
            HashSet<TGroup> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetGroupToGroupReverseMappingsForGroupsQueryTime());
            try
            {
                result = baseClassMethod.Invoke(groups);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupReverseMappingsForGroupsQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new GetGroupToGroupReverseMappingsForGroupsQueryTime());
            IncrementCountMetricIfLoggingEnabled(new GetGroupToGroupReverseMappingsForGroupsQuery());

            return result;
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class RemoveGroupToGroupMapping() method.
        /// </summary>
        /// <param name="fromGroup">The 'from' group in the mapping.</param>
        /// <param name="toGroup">The 'to' group in the mapping.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the RemoveGroupToGroupMapping() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TGroup, TGroup, Action> GenerateRemoveGroupToGroupMappingMetricLoggingWrappingAction(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup, Action> wrappingAction)
        {
            return (TGroup metricLoggingActionFromGroup, TGroup metricLoggingActionToGroup, Action baseAction) =>
            {
                CallAccessManagerEventProcessingMethodWithMetricLogging<GroupToGroupMappingRemoveTime, GroupToGroupMappingRemoved>
                (
                    () =>
                    {
                        wrappingAction.Invoke(metricLoggingActionFromGroup, metricLoggingActionToGroup, () =>
                        {
                            baseAction.Invoke();
                        });
                    }
                );
            };
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class AddUserToApplicationComponentAndAccessLevelMapping() method.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the AddUserToApplicationComponentAndAccessLevelMapping() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TUser, TComponent, TAccess, Action> GenerateAddUserToApplicationComponentAndAccessLevelMappingMetricLoggingWrappingAction
        (
            TUser user, 
            TComponent applicationComponent, 
            TAccess accessLevel, 
            Action<TUser, TComponent, TAccess, Action> wrappingAction)
        {
            return (TUser metricLoggingActionUser, TComponent metricLoggingActionApplicationComponent, TAccess metricLoggingActionAccessLevel, Action baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingAddTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionUser, metricLoggingActionApplicationComponent, metricLoggingActionAccessLevel, () =>
                    {
                        baseAction.Invoke();
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new UserToApplicationComponentAndAccessLevelMappingAddTime());
                    if (typeof(IdempotentAddOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new UserToApplicationComponentAndAccessLevelMappingAddTime());
                IncrementCountMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingAdded());
                userToApplicationComponentAndAccessLevelMappingCount++;
                SetStatusMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingsStored(), userToApplicationComponentAndAccessLevelMappingCount);
            };
        }

        /// <summary>
        /// Gets the application component and access level pairs that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of Tuples containing the application component and access level pairs that the specified user is mapped to.</returns>
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user, Func<TUser, IEnumerable<Tuple<TComponent, TAccess>>> baseClassMethod)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<Tuple<TComponent, TAccess>>, GetUserToApplicationComponentAndAccessLevelMappingsQuery>(() =>
            {
                return baseClassMethod.Invoke(user);
            });
        }

        /// <summary>
        /// Gets the application component and access level pairs that the specified user is mapped to.
        /// </summary>
        /// <param name="applicationComponent">The application component to retrieve the mappings for.</param>
        /// <param name="accessLevel">The access level to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a user is mapped to an application component and access level via groups).</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of users that are mapped to the specified application component and access level.</returns>
        public IEnumerable<TUser> GetApplicationComponentAndAccessLevelToUserMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings, Func<TComponent, TAccess, Boolean, IEnumerable<TUser>> baseClassMethod)
        {
            if (includeIndirectMappings == false)
            {
                return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<TUser>, GetApplicationComponentAndAccessLevelToUserMappingsQuery>(() =>
                {
                    return baseClassMethod.Invoke(applicationComponent, accessLevel, includeIndirectMappings);
                });
            }
            else
            {
                return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<TUser>, GetApplicationComponentAndAccessLevelToUserMappingsWithIndirectMappingsQuery>(() =>
                {
                    return baseClassMethod.Invoke(applicationComponent, accessLevel, includeIndirectMappings);
                });
            }
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class RemoveUserToApplicationComponentAndAccessLevelMapping() method.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the RemoveUserToApplicationComponentAndAccessLevelMapping() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TUser, TComponent, TAccess, Action> GenerateRemoveUserToApplicationComponentAndAccessLevelMappingMetricLoggingWrappingAction
        (
            TUser user,
            TComponent applicationComponent,
            TAccess accessLevel,
            Action<TUser, TComponent, TAccess, Action> wrappingAction
        )
        {
            return (TUser metricLoggingActionUser, TComponent metricLoggingActionApplicationComponent, TAccess metricLoggingActionAccessLevel, Action baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionUser, metricLoggingActionApplicationComponent, metricLoggingActionAccessLevel, () =>
                    {
                        baseAction.Invoke();
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
                    if (typeof(IdempotentRemoveOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingRemoved());
                userToApplicationComponentAndAccessLevelMappingCount--;
                SetStatusMetricIfLoggingEnabled(new UserToApplicationComponentAndAccessLevelMappingsStored(), userToApplicationComponentAndAccessLevelMappingCount);
            };
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class AddGroupToApplicationComponentAndAccessLevelMapping() method.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the AddGroupToApplicationComponentAndAccessLevelMapping() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TGroup, TComponent, TAccess, Action> GenerateAddGroupToApplicationComponentAndAccessLevelMappingMetricLoggingWrappingAction
        (
            TGroup group,
            TComponent applicationComponent,
            TAccess accessLevel,
            Action<TGroup, TComponent, TAccess, Action> wrappingAction
        )
        {
            return (TGroup metricLoggingActionGroup, TComponent metricLoggingActionApplicationComponent, TAccess metricLoggingActionAccessLevel, Action baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingAddTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionGroup, metricLoggingActionApplicationComponent, metricLoggingActionAccessLevel, () =>
                    {
                        baseAction.Invoke();
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GroupToApplicationComponentAndAccessLevelMappingAddTime());
                    if (typeof(IdempotentAddOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GroupToApplicationComponentAndAccessLevelMappingAddTime());
                IncrementCountMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingAdded());
                groupToApplicationComponentAndAccessLevelMappingCount++;
                SetStatusMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingsStored(), groupToApplicationComponentAndAccessLevelMappingCount);
            };
        }

        /// <summary>
        /// Gets the application component and access level pairs that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of Tuples containing the application component and access level pairs that the specified group is mapped to.</returns>
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group, Func<TGroup, IEnumerable<Tuple<TComponent, TAccess>>> baseClassMethod)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<Tuple<TComponent, TAccess>>, GetGroupToApplicationComponentAndAccessLevelMappingsQuery>(() =>
            {
                return baseClassMethod.Invoke(group);
            });
        }

        /// <summary>
        /// Gets the groups that are mapped to the specified application component and access level pair.
        /// </summary>
        /// <param name="applicationComponent">The application component to retrieve the mappings for.</param>
        /// <param name="accessLevel">The access level to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a group is mapped to an application component and access level via other groups).</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of groups that are mapped to the specified application component and access level.</returns>
        public IEnumerable<TGroup> GetApplicationComponentAndAccessLevelToGroupMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings, Func<TComponent, TAccess, Boolean, IEnumerable<TGroup>> baseClassMethod)
        {
            if (includeIndirectMappings == false)
            {
                return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<TGroup>, GetApplicationComponentAndAccessLevelToGroupMappingsQuery>(() =>
                {
                    return baseClassMethod.Invoke(applicationComponent, accessLevel, includeIndirectMappings);
                });
            }
            else
            {
                return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<TGroup>, GetApplicationComponentAndAccessLevelToGroupMappingsWithIndirectMappingsQuery>(() =>
                {
                    return baseClassMethod.Invoke(applicationComponent, accessLevel, includeIndirectMappings);
                });
            }
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class RemoveGroupToApplicationComponentAndAccessLevelMapping() method.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="applicationComponent">The application component in the mapping.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the RemoveGroupToApplicationComponentAndAccessLevelMapping() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TGroup, TComponent, TAccess, Action> GenerateRemoveGroupToApplicationComponentAndAccessLevelMappingMetricLoggingWrappingAction
        (
            TGroup group,
            TComponent applicationComponent,
            TAccess accessLevel,
            Action<TGroup, TComponent, TAccess, Action> wrappingAction
        )
        {
            return (TGroup metricLoggingActionGroup, TComponent metricLoggingActionApplicationComponent, TAccess metricLoggingActionAccessLevel, Action baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionGroup, metricLoggingActionApplicationComponent, metricLoggingActionAccessLevel, () =>
                    {
                        baseAction.Invoke();
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
                    if (typeof(IdempotentRemoveOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingRemoved());
                groupToApplicationComponentAndAccessLevelMappingCount--;
                SetStatusMetricIfLoggingEnabled(new GroupToApplicationComponentAndAccessLevelMappingsStored(), groupToApplicationComponentAndAccessLevelMappingCount);
            };
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class AddEntityType() method.
        /// </summary>
        /// <param name="entityType">The entity type to add.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the AddEntityType() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <param name="entities">The dictionary which stores entity types and entities, in the ConcurrentAccessManager subclass that metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<String, Action> GenerateAddEntityTypeMetricLoggingWrappingAction
        (
            String entityType, 
            Action<String, Action> wrappingAction,
            IDictionary<String, ISet<String>> entities
        )
        {
            return (String metricLoggingActionEntityType, Action baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new EntityTypeAddTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionEntityType, () =>
                    {
                        baseAction.Invoke();
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new EntityTypeAddTime());
                    if (typeof(IdempotentAddOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new EntityTypeAddTime());
                IncrementCountMetricIfLoggingEnabled(new EntityTypeAdded());
                SetStatusMetricIfLoggingEnabled(new EntityTypesStored(), entities.Count);
            };
        }

        /// <summary>
        /// Returns true if the specified entity type exists.
        /// </summary>
        /// <param name="entityType">The entity type to check for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>True if the entity type exists.  False otherwise.</returns>
        public Boolean ContainsEntityType(String entityType, Func<String, Boolean> baseClassMethod)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<Boolean, ContainsEntityTypeQuery>(() => { return baseClassMethod.Invoke(entityType); });
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class RemoveEntityType() method.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the RemoveEntityType() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <param name="entities">The dictionary which stores entity types and entities, in the ConcurrentAccessManager subclass that metrics are being logged for.</param>
        /// <param name="removeEntityTypeWithPreRemovalActionsMethod">The RemoveEntityType() method overload with '*PreRemovalAction' parameters on the ConcurrentAccessManager subclass that metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<String, Action> GenerateRemoveEntityTypeMetricLoggingWrappingAction
        (
            String entityType, 
            Action<String, Action> wrappingAction,
            IDictionary<String, ISet<String>> entities, 
            Action<String, Action<TUser, String, IEnumerable<String>, Int32>, Action<TGroup, String, IEnumerable<String>, Int32>> removeEntityTypeWithPreRemovalActionsMethod
        )
        {
            return (String metricLoggingActionEntityType, Action baseAction) =>
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
                if (entities.ContainsKey(metricLoggingActionEntityType) == true)
                {
                    newEntityCount -= entities[metricLoggingActionEntityType].Count;
                }
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new EntityTypeRemoveTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionEntityType, () =>
                    {
                        removeEntityTypeWithPreRemovalActionsMethod(metricLoggingActionEntityType, userToEntityTypeMappingPreRemovalAction, groupToEntityTypeMappingPreRemovalAction);
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new EntityTypeRemoveTime());
                    if (typeof(IdempotentRemoveOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new EntityTypeRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new EntityTypeRemoved());
                SetStatusMetricIfLoggingEnabled(new EntityTypesStored(), entities.Count);
                entityCount = newEntityCount;
                SetStatusMetricIfLoggingEnabled(new EntitiesStored(), entityCount);
                SetStatusMetricIfLoggingEnabled(new UserToEntityMappingsStored(), userToEntityMappingCount);
                SetStatusMetricIfLoggingEnabled(new GroupToEntityMappingsStored(), groupToEntityMappingCount);
            };
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class AddEntity() method.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to add.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the AddEntity() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<String, String, Action> GenerateAddEntityMetricLoggingWrappingAction(String entityType, String entity, Action<String, String, Action> wrappingAction)
        {
            return (String metricLoggingActionEntityType, String metricLoggingActionEntity, Action baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new EntityAddTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionEntityType, metricLoggingActionEntity, () =>
                    {
                        baseAction.Invoke();
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new EntityAddTime());
                    if (typeof(IdempotentAddOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new EntityAddTime());
                IncrementCountMetricIfLoggingEnabled(new EntityAdded());
                entityCount++;
                SetStatusMetricIfLoggingEnabled(new EntitiesStored(), entityCount);
            };
        }

        /// <summary>
        /// Returns all entities of the specified type.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of all entities of the specified type.</returns>
        public IEnumerable<String> GetEntities(String entityType, Func<String, IEnumerable<String>> baseClassMethod)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<String>, GetEntitiesQuery>(() => { return baseClassMethod.Invoke(entityType); });
        }

        /// <summary>
        /// Returns true if the specified entity exists.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to check for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>True if the entity exists.  False otherwise.</returns>
        public Boolean ContainsEntity(String entityType, String entity, Func<String, String, Boolean> baseClassMethod)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<Boolean, ContainsEntityQuery>(() => { return baseClassMethod.Invoke(entityType, entity); });
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class RemoveEntity() method.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the RemoveEntity() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <param name="removeEntityWithPostRemovalActionsMethod">The RemoveEntity() method overload with '*PostRemovalAction' parameters on the ConcurrentAccessManager subclass that metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<String, String, Action> GenerateRemoveEntityMetricLoggingWrappingAction
        (
            String entityType, 
            String entity, 
            Action<String, String, Action> wrappingAction,
            Action<String, String, Action<TUser, String, String>, Action<TGroup, String, String>> removeEntityWithPostRemovalActionsMethod
        )
        {
            return (String metricLoggingActionEntityType, String metricLoggingActionEntity, Action baseAction) =>
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
                    wrappingAction.Invoke(metricLoggingActionEntityType, metricLoggingActionEntity, () => 
                    {
                        removeEntityWithPostRemovalActionsMethod(metricLoggingActionEntityType, metricLoggingActionEntity, userToEntityMappingPostRemovalAction, groupToEntityMappingPostRemovalAction);
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new EntityRemoveTime());
                    if (typeof(IdempotentRemoveOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new EntityRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new EntityRemoved());
                entityCount--;
                SetStatusMetricIfLoggingEnabled(new EntitiesStored(), entityCount);
                SetStatusMetricIfLoggingEnabled(new UserToEntityMappingsStored(), userToEntityMappingCount);
                SetStatusMetricIfLoggingEnabled(new GroupToEntityMappingsStored(), groupToEntityMappingCount);
            };
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class AddUserToEntityMapping() method.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the AddUserToEntityMapping() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TUser, String, String, Action> GenerateAddUserToEntityMappingMetricLoggingWrappingAction(TUser user, String entityType, String entity, Action<TUser, String, String, Action> wrappingAction)
        {
            return (TUser metricLoggingActionUser, String metricLoggingActionEntityType, String metricLoggingActionEntity, Action baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new UserToEntityMappingAddTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionUser, metricLoggingActionEntityType , metricLoggingActionEntity, () =>
                    {
                        baseAction.Invoke();
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new UserToEntityMappingAddTime());
                    if (typeof(IdempotentAddOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new UserToEntityMappingAddTime());
                IncrementCountMetricIfLoggingEnabled(new UserToEntityMappingAdded());
                userToEntityMappingCount++;
                userToEntityMappingCountPerUser.Increment(metricLoggingActionUser);
                SetStatusMetricIfLoggingEnabled(new UserToEntityMappingsStored(), userToEntityMappingCount);
            };
        }

        /// <summary>
        /// Gets the entities that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the specified user is mapped to.</returns>
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user, Func<TUser, IEnumerable<Tuple<String, String>>> baseClassMethod)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<Tuple<String, String>>, GetUserToEntityMappingsForUserQuery>(() =>
            {
                return baseClassMethod.Invoke(user);
            });
        }

        /// <summary>
        /// Gets the entities of a given type that the specified user is mapped to.
        /// </summary>
        /// <param name="user">The user to retrieve the mappings for.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of entities that the specified user is mapped to.</returns>
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType, Func<TUser, String, IEnumerable<String>> baseClassMethod)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<String>, GetUserToEntityMappingsForUserAndEntityTypeQuery>(() =>
            {
                return baseClassMethod.Invoke(user, entityType);
            });
        }

        /// <summary>
        /// Gets the users that are mapped to the specified entity.
        /// </summary>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <param name="entity">The entity to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a user is mapped to the entity via groups).</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of users that are mapped to the specified entity.</returns>
        public IEnumerable<TUser> GetEntityToUserMappings(String entityType, String entity, Boolean includeIndirectMappings, Func<String, String, Boolean, IEnumerable<TUser>> baseClassMethod)
        {
            if (includeIndirectMappings == false)
            {
                return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<TUser>, GetEntityToUserMappingsQuery>(() =>
                {
                    return baseClassMethod.Invoke(entityType, entity, includeIndirectMappings);
                });
            }
            else
            {
                return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<TUser>, GetEntityToUserMappingsWithIndirectMappingsQuery>(() =>
                {
                    return baseClassMethod.Invoke(entityType, entity, includeIndirectMappings);
                });
            }
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class RemoveUserToEntityMapping() method.
        /// </summary>
        /// <param name="user">The user in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the RemoveUserToEntityMapping() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TUser, String, String, Action> GenerateRemoveUserToEntityMappingMetricLoggingWrappingAction(TUser user, String entityType, String entity, Action<TUser, String, String, Action> wrappingAction)
        {
            return (TUser metricLoggingActionUser, String metricLoggingActionEntityType, String metricLoggingActionEntity, Action baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new UserToEntityMappingRemoveTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionUser, metricLoggingActionEntityType, metricLoggingActionEntity, () =>
                    {
                        baseAction.Invoke();
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new UserToEntityMappingRemoveTime());
                    if (typeof(IdempotentRemoveOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new UserToEntityMappingRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new UserToEntityMappingRemoved());
                userToEntityMappingCount--;
                userToEntityMappingCountPerUser.Decrement(metricLoggingActionUser);
                SetStatusMetricIfLoggingEnabled(new UserToEntityMappingsStored(), userToEntityMappingCount);
            };
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class AddGroupToEntityMapping() method.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the AddGroupToEntityMapping() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TGroup, String, String, Action> GenerateAddGroupToEntityMappingMetricLoggingWrappingAction(TGroup group, String entityType, String entity, Action<TGroup, String, String, Action> wrappingAction)
        {
            return (TGroup metricLoggingActionGroup, String metricLoggingActionEntityType, String metricLoggingActionEntity, Action baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GroupToEntityMappingAddTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionGroup, metricLoggingActionEntityType, metricLoggingActionEntity, () =>
                    {
                        baseAction.Invoke();
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GroupToEntityMappingAddTime());
                    if (typeof(IdempotentAddOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GroupToEntityMappingAddTime());
                IncrementCountMetricIfLoggingEnabled(new GroupToEntityMappingAdded());
                groupToEntityMappingCount++;
                groupToEntityMappingCountPerGroup.Increment(metricLoggingActionGroup);
                SetStatusMetricIfLoggingEnabled(new GroupToEntityMappingsStored(), groupToEntityMappingCount);
            };
        }

        /// <summary>
        /// Gets the entities that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the specified group is mapped to.</returns>
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group, Func<TGroup, IEnumerable<Tuple<String, String>>> baseClassMethod)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<Tuple<String, String>>, GetGroupToEntityMappingsForGroupQuery>(() =>
            {
                return baseClassMethod.Invoke(group);
            });
        }

        /// <summary>
        /// Gets the entities of a given type that the specified group is mapped to.
        /// </summary>
        /// <param name="group">The group to retrieve the mappings for.</param>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of entities that the specified group is mapped to.</returns>
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType, Func<TGroup, String, IEnumerable<String>> baseClassMethod)
        {
            return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<String>, GetGroupToEntityMappingsForGroupAndEntityTypeQuery>(() =>
            {
                return baseClassMethod.Invoke(group, entityType);
            });
        }

        /// <summary>
        /// Gets the groups that are mapped to the specified entity.
        /// </summary>
        /// <param name="entityType">The entity type to retrieve the mappings for.</param>
        /// <param name="entity">The entity to retrieve the mappings for.</param>
        /// <param name="includeIndirectMappings">Whether to include indirect mappings (i.e. those where a group is mapped to the entity via other groups).</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of groups that are mapped to the specified entity.</returns>
        public IEnumerable<TGroup> GetEntityToGroupMappings(String entityType, String entity, Boolean includeIndirectMappings, Func<String, String, Boolean, IEnumerable<TGroup>> baseClassMethod)
        {
            if (includeIndirectMappings == false)
            {
                return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<TGroup>, GetEntityToGroupMappingsQuery>(() =>
                {
                    return baseClassMethod.Invoke(entityType, entity, includeIndirectMappings);
                });
            }
            else
            {
                return CallAccessManagerQueryProcessingMethodWithMetricLogging<IEnumerable<TGroup>, GetEntityToGroupMappingsWithIndirectMappingsQuery>(() =>
                {
                    return baseClassMethod.Invoke(entityType, entity, includeIndirectMappings);
                });
            }
        }

        /// <summary>
        /// Generates a 'wrappingAction' implementing metric logging to pass to the base class RemoveGroupToEntityMapping() method.
        /// </summary>
        /// <param name="group">The group in the mapping.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity in the mapping.</param>
        /// <param name="wrappingAction">The 'wrappingAction' parameter passed to the RemoveGroupToEntityMapping() method of the ConcurrentAccessManager subclass metrics are being logged for.</param>
        /// <returns>The 'wrappingAction'.</returns>
        public Action<TGroup, String, String, Action> GenerateRemoveGroupToEntityMappingMetricLoggingWrappingAction(TGroup group, String entityType, String entity, Action<TGroup, String, String, Action> wrappingAction)
        {
            return (TGroup metricLoggingActionGroup, String metricLoggingActionEntityType, String metricLoggingActionEntity, Action baseAction) =>
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GroupToEntityMappingRemoveTime());
                try
                {
                    wrappingAction.Invoke(metricLoggingActionGroup, metricLoggingActionEntityType, metricLoggingActionEntity, () =>
                    {
                        baseAction.Invoke();
                    });
                }
                catch (Exception e)
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new GroupToEntityMappingRemoveTime());
                    if (typeof(IdempotentRemoveOperationException).IsAssignableFrom(e.GetType()) == true)
                    {
                        return;
                    }

                    throw;
                }
                EndIntervalMetricIfLoggingEnabled(beginId, new GroupToEntityMappingRemoveTime());
                IncrementCountMetricIfLoggingEnabled(new GroupToEntityMappingRemoved());
                groupToEntityMappingCount--;
                groupToEntityMappingCountPerGroup.Decrement(metricLoggingActionGroup);
                SetStatusMetricIfLoggingEnabled(new GroupToEntityMappingsStored(), groupToEntityMappingCount);
            };
        }

        /// <summary>
        /// Checks whether the specified user (or a group that the user is a member of) has access to an application component at the specified level of access.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <param name="applicationComponent">The application component.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>True if the user has access the component.  False otherwise.</returns>
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel, Func<TUser, TComponent, TAccess, Boolean> baseClassMethod)
        {
            Boolean result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new HasAccessToApplicationComponentForUserQueryTime());
            try
            {
                result = baseClassMethod.Invoke(user, applicationComponent, accessLevel);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new HasAccessToApplicationComponentForUserQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new HasAccessToApplicationComponentForUserQueryTime());
            IncrementCountMetricIfLoggingEnabled(new HasAccessToApplicationComponentForUserQuery());

            return result;
        }

        /// <summary>
        /// Checks whether any of the specified groups have access to an application component at the specified level of access.
        /// </summary>
        /// <param name="groups">The groups to check for.</param>
        /// <param name="applicationComponent">The application component.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>True if any of the groups have access the component.  False otherwise.</returns>
        public Boolean HasAccessToApplicationComponent(IEnumerable<TGroup> groups, TComponent applicationComponent, TAccess accessLevel, Func<IEnumerable<TGroup>, TComponent, TAccess, Boolean> baseClassMethod)
        {
            Boolean result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new HasAccessToApplicationComponentForGroupsQueryTime());
            try
            {
                result = baseClassMethod.Invoke(groups, applicationComponent, accessLevel);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new HasAccessToApplicationComponentForGroupsQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new HasAccessToApplicationComponentForGroupsQueryTime());
            IncrementCountMetricIfLoggingEnabled(new HasAccessToApplicationComponentForGroupsQuery());

            return result;
        }

        /// <summary>
        /// Checks whether the specified user (or a group that the user is a member of) has access to the specified entity.
        /// </summary>
        /// <param name="user">The user to check for.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>True if the user has access the entity.  False otherwise.</returns>
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity, Func<TUser, String, String, Boolean> baseClassMethod)
        {
            Boolean result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new HasAccessToEntityForUserQueryTime());
            try
            {
                result = baseClassMethod.Invoke(user, entityType, entity);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new HasAccessToEntityForUserQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new HasAccessToEntityForUserQueryTime());
            IncrementCountMetricIfLoggingEnabled(new HasAccessToEntityForUserQuery());

            return result;
        }

        /// <summary>
        /// Checks whether any of the specified groups have access to the specified entity.
        /// </summary>
        /// <param name="groups">The groups to check for.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>True if any of the groups have access the entity.  False otherwise.</returns>
        public Boolean HasAccessToEntity(IEnumerable<TGroup> groups, String entityType, String entity, Func<IEnumerable<TGroup>, String, String, Boolean> baseClassMethod)
        {
            Boolean result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new HasAccessToEntityForGroupsQueryTime());
            try
            {
                result = baseClassMethod.Invoke(groups, entityType, entity);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new HasAccessToEntityForGroupsQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new HasAccessToEntityForGroupsQueryTime());
            IncrementCountMetricIfLoggingEnabled(new HasAccessToEntityForGroupsQuery());

            return result;
        }

        /// <summary>
        /// Gets all application components and levels of access that the specified user (or a group that the user is a member of) has access to.
        /// </summary>
        /// <param name="user">The user to retrieve the application components and levels of access for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>The application components and levels of access to those application components that the user has.</returns>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user, Func<TUser, HashSet<Tuple<TComponent, TAccess>>> baseClassMethod)
        {
            HashSet<Tuple<TComponent, TAccess>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetApplicationComponentsAccessibleByUserQueryTime());
            try
            {
                result = baseClassMethod.Invoke(user);
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
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>The application components and levels of access to those application components that the group has.</returns>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group, Func<TGroup, HashSet<Tuple<TComponent, TAccess>>> baseClassMethod)
        {
            HashSet<Tuple<TComponent, TAccess>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetApplicationComponentsAccessibleByGroupQueryTime());
            try
            {
                result = baseClassMethod.Invoke(group);
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
        /// Gets all application components and levels of access that the specified groups (or groups that the specified groups are mapped to) have access to.
        /// </summary>
        /// <param name="groups">The groups to retrieve the application components and levels of access for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>The application components and levels of access to those application components that the groups have.</returns>
        public HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroups(IEnumerable<TGroup> groups, Func<IEnumerable<TGroup>, HashSet<Tuple<TComponent, TAccess>>> baseClassMethod)
        {
            HashSet<Tuple<TComponent, TAccess>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetApplicationComponentsAccessibleByGroupsQueryTime());
            try
            {
                result = baseClassMethod.Invoke(groups);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new GetApplicationComponentsAccessibleByGroupsQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new GetApplicationComponentsAccessibleByGroupsQueryTime());
            IncrementCountMetricIfLoggingEnabled(new GetApplicationComponentsAccessibleByGroupsQuery());

            return result;
        }

        /// <summary>
        /// Gets all entities that the specified user (or a group that the user is a member of) has access to.
        /// </summary>
        /// <param name="user">The user to retrieve the entities for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the user has access to.</returns>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByUser(TUser user, Func<TUser, HashSet<Tuple<String, String>>> baseClassMethod)
        {
            HashSet<Tuple<String, String>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByUserQueryTime());
            try
            {
                result = baseClassMethod.Invoke(user);
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
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>The entities the user has access to.</returns>
        public HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType, Func<TUser, String, HashSet<String>> baseClassMethod)
        {
            HashSet<String> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByUserQueryTime());
            try
            {
                result = baseClassMethod.Invoke(user, entityType);
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
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the group has access to.</returns>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroup(TGroup group, Func<TGroup, HashSet<Tuple<String, String>>> baseClassMethod)
        {
            HashSet<Tuple<String, String>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByGroupQueryTime());
            try
            {
                result = baseClassMethod.Invoke(group);
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
        /// Gets all entities that the specified groups (or groups that the specified groups are mapped to) have access to.
        /// </summary>
        /// <param name="groups">The groups to retrieve the entities for.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>A collection of Tuples containing the entity type and entity that the groups have access to.</returns>
        public HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroups(IEnumerable<TGroup> groups, Func<IEnumerable<TGroup>, HashSet<Tuple<String, String>>> baseClassMethod)
        {
            HashSet<Tuple<String, String>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByGroupsQueryTime());
            try
            {
                result = baseClassMethod.Invoke(groups);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new GetEntitiesAccessibleByGroupsQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new GetEntitiesAccessibleByGroupsQueryTime());
            IncrementCountMetricIfLoggingEnabled(new GetEntitiesAccessibleByGroupsQuery());

            return result;
        }

        /// <summary>
        /// Gets all entities of a given type that the specified group (or group that the specified group is mapped to) has access to.
        /// </summary>
        /// <param name="group">The group to retrieve the entities for.</param>
        /// <param name="entityType">The type of entities to retrieve.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>The entities the group has access to.</returns>
        public HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType, Func<TGroup, String, HashSet<String>> baseClassMethod)
        {
            HashSet<String> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByGroupQueryTime());
            try
            {
                result = baseClassMethod.Invoke(group, entityType);
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
        /// Gets all entities of a given type that the specified groups (or groups that the specified groups are mapped to) have access to.
        /// </summary>
        /// <param name="groups">The groups to retrieve the entities for.</param>
        /// <param name="entityType">The type of entities to retrieve.</param>
        /// <param name="baseClassMethod">The equivalent method on the ConcurrentAccessManager instance or subclass metrics are being logged for.</param>
        /// <returns>The entities the groups have access to.</returns>
        public HashSet<String> GetEntitiesAccessibleByGroups(IEnumerable<TGroup> groups, String entityType, Func<IEnumerable<TGroup>, String, HashSet<String>> baseClassMethod)
        {
            HashSet<String> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByGroupsQueryTime());
            try
            {
                result = baseClassMethod.Invoke(groups, entityType);
            }
            catch
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new GetEntitiesAccessibleByGroupsQueryTime());
                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new GetEntitiesAccessibleByGroupsQueryTime());
            IncrementCountMetricIfLoggingEnabled(new GetEntitiesAccessibleByGroupsQuery());

            return result;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the fields which store counts of items and mappings.
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
        /// <typeparam name="TIntervalMetric">The type of interval metric to log.</typeparam>
        /// <typeparam name="TCountMetric">The type of count metric to log.</typeparam>
        /// <param name="eventAction">Action which calls the access manager IAccessManagerEventProcessor method.</param>
        protected void CallAccessManagerEventProcessingMethodWithMetricLogging<TIntervalMetric, TCountMetric>(Action eventAction)
            where TIntervalMetric : IntervalMetric, new()
            where TCountMetric : CountMetric, new()
        {
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new TIntervalMetric());
            try
            {
                eventAction.Invoke();
            }
            catch (Exception e)
            {
                CancelIntervalMetricIfLoggingEnabled(beginId, new TIntervalMetric());
                if (e is IdempotentAddOperationException || e is IdempotentRemoveOperationException)
                {
                    return;
                }

                throw;
            }
            EndIntervalMetricIfLoggingEnabled(beginId, new TIntervalMetric());
            IncrementCountMetricIfLoggingEnabled(new TCountMetric());
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
