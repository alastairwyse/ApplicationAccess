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
using System.Collections.Generic;
using MoreComplexDataStructures;
using ApplicationMetrics;
using ApplicationAccess.Utilities;

namespace ApplicationAccess.Metrics
{
    // TODO: This is the orogonal version of MetricLoggingConcurrentAccessManager before the current one which uses ConcurrentAccessManagerMetricLoggingInternalDecorator
    //   Keeping this until I've done sufficient testing to ensure new one is working correctly.

    /// <summary>
    /// A thread-safe version of the <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> class, which can be accessed and modified by multiple threads concurrently, and which logs various metrics.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>
    /// <para>Thread safety is implemented by using concurrent collections internally to represent the user, group, component, access level, and entity mappings (allows for concurrent read and enumeration operations), and locks to serialize modification operations.  Note that all generic type parameters must implement relevant methods to allow storing in a <see cref="System.Collections.Generic.HashSet{T}"/> (at minimum <see cref="IEquatable{T}"/> and <see cref="Object.GetHashCode">GetHashcode()</see>).  This is not enforced as a generic type contraint in order to allow the type parameters to be enums.</para>
    /// <para>Note that interval metrics are not logged for <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> methods that either return IEnumerable or perform simple lookups on HashSets or Dictionaries.  For methods returning IEnumerable, their 'work' is not done until the returned IEnumerable is iterated, so capturing an interval around just the return of the IEnumerable does not provide a realistic metric.  For methods that perform just HashSets or Dictionary lookups, the performance cost of these operations is negligible, hence capturing metrics around them does not provide much value.</para>
    /// </remarks>
    internal class MetricLoggingConcurrentAccessManagerOld<TUser, TGroup, TComponent, TAccess> : ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>
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
                ((MetricLoggingConcurrentDirectedGraph<TUser, TGroup>)userToGroupMap).MetricLoggingEnabled = value;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.MetricLoggingConcurrentAccessManager class.
        /// </summary>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the manager are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public MetricLoggingConcurrentAccessManagerOld(Boolean storeBidirectionalMappings, IMetricLogger metricLogger)
            : base(new MetricLoggingConcurrentDirectedGraph<TUser, TGroup>(storeBidirectionalMappings, new MappingMetricLogger(metricLogger)), storeBidirectionalMappings)
        {
            InitializeItemAndMappingCountFields();

            this.metricLogger = metricLogger;
            // Casting should never fail, since we just newed the 'userToGroupMap' and 'MetricLogger' properties to these types.
            //   TODO: Find a cleaner way to do this... ideally don't want to expose the 'MetricLoggingConcurrentDirectedGraph.MetricLogger' property at all.
            mappingMetricLogger = (MappingMetricLogger)((MetricLoggingConcurrentDirectedGraph<TUser, TGroup>)userToGroupMap).MetricLogger;
            AddMappingMetricLoggerMappings();
            metricLoggingEnabled = true;
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            base.Clear();
            InitializeItemAndMappingCountFields();
        }

        /// <inheritdoc/>
        public override void AddUser(TUser user)
        {
            AddUser(user, (postProcessingActionUser) => { });
        }

        /// <inheritdoc/>
        public override void AddUser(TUser user, Action<TUser> postProcessingAction)
        {
            Action<TUser, Action<TUser, Action>> addUserAction = (actionUser, baseAction) =>
            {
                base.AddUser(user, baseAction);
                postProcessingAction.Invoke(user);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TUser, UserAddTime, UserAdded>(user, addUserAction);
        }

        /// <inheritdoc/>
        public override Boolean ContainsUser(TUser user)
        {
            return CallBaseClassQueryProcessingMethodWithMetricLogging<Boolean, ContainsUserQuery>(() => { return base.ContainsUser(user); });
        }

        /// <inheritdoc/>
        public override void RemoveUser(TUser user)
        {
            RemoveUser(user, (postProcessingActionUser) => { });
        }

        /// <inheritdoc/>
        public override void RemoveUser(TUser user, Action<TUser> postProcessingAction)
        {
            Action<TUser, Action> wrappingAction = (actionUser, baseAction) =>
            {
                Int32 newUserToApplicationComponentAndAccessLevelMappingCount = userToApplicationComponentAndAccessLevelMappingCount;
                if (userToComponentMap.ContainsKey(user) == true)
                {
                    newUserToApplicationComponentAndAccessLevelMappingCount -= userToComponentMap[user].Count;
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
            this.RemoveUser(user, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddGroup(TGroup group)
        {
            AddGroup(group, (postProcessingActionGroup) => { });
        }

        /// <inheritdoc/>
        public override void AddGroup(TGroup group, Action<TGroup> postProcessingAction)
        {
            Action<TGroup, Action<TGroup, Action>> addGroupAction = (actionGroup, baseAction) =>
            {
                base.AddGroup(group, baseAction);
                postProcessingAction.Invoke(group);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TGroup, GroupAddTime, GroupAdded>(group, addGroupAction);
        }

        /// <inheritdoc/>
        public override Boolean ContainsGroup(TGroup group)
        {
            return CallBaseClassQueryProcessingMethodWithMetricLogging<Boolean, ContainsGroupQuery>(() => { return base.ContainsGroup(group); });
        }

        /// <inheritdoc/>
        public override void RemoveGroup(TGroup group)
        {
            RemoveGroup(group, (postProcessingActionGroup) => { });
        }

        /// <inheritdoc/>
        public override void RemoveGroup(TGroup group, Action<TGroup> postProcessingAction)
        {
            Action<TGroup, Action> wrappingAction = (actionGroup, baseAction) =>
            {
                Int32 newGroupToApplicationComponentAndAccessLevelMappingCount = groupToApplicationComponentAndAccessLevelMappingCount;
                if (groupToComponentMap.ContainsKey(group) == true)
                {
                    newGroupToApplicationComponentAndAccessLevelMappingCount -= groupToComponentMap[group].Count;
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
            this.RemoveGroup(group, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToGroupMapping(TUser user, TGroup group)
        {
            AddUserToGroupMapping(user, group, (postProcessingActionUser, postProcessingActionGroup) => { });
        }

        /// <inheritdoc/>
        public override void AddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postProcessingAction)
        {
            Action<TUser, TGroup, Action<TUser, TGroup, Action>> addUserToGroupMappingAction = (actionUser, actionGroup, baseAction) =>
            {
                base.AddUserToGroupMapping(user, group, baseAction);
                postProcessingAction.Invoke(user, group);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TUser, TGroup, UserToGroupMappingAddTime, UserToGroupMappingAdded>(user, group, addUserToGroupMappingAction);
        }

        /// <inheritdoc/>
        public override HashSet<TGroup> GetUserToGroupMappings(TUser user, Boolean includeIndirectMappings)
        {
            HashSet<TGroup> result;
            if (includeIndirectMappings == false)
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetUserToGroupMappingsQueryTime());
                try
                {
                    result = base.GetUserToGroupMappings(user, includeIndirectMappings);
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
                    result = base.GetUserToGroupMappings(user, includeIndirectMappings);
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

        /// <inheritdoc/>
        public override void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            RemoveUserToGroupMapping(user, group, (postProcessingActionUser, postProcessingActionGroup) => { });
        }

        /// <inheritdoc/>
        public override void RemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postProcessingAction)
        {
            Action<TUser, TGroup, Action<TUser, TGroup, Action>> removeUserToGroupMappingAction = (actionUser, actionGroup, baseAction) =>
            {
                base.RemoveUserToGroupMapping(user, group, baseAction);
                postProcessingAction.Invoke(user, group);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TUser, TGroup, UserToGroupMappingRemoveTime, UserToGroupMappingRemoved>(user, group, removeUserToGroupMappingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            AddGroupToGroupMapping(fromGroup, toGroup, (postProcessingActionFromGroup, postProcessingActionToGroup) => { });
        }

        /// <inheritdoc/>
        public override void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postProcessingAction)
        {
            Action<TGroup, TGroup, Action<TGroup, TGroup, Action>> addGroupToGroupMappingAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                base.AddGroupToGroupMapping(fromGroup, toGroup, baseAction);
                postProcessingAction.Invoke(fromGroup, toGroup);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TGroup, TGroup, GroupToGroupMappingAddTime, GroupToGroupMappingAdded>(fromGroup, toGroup, addGroupToGroupMappingAction);
        }

        /// <inheritdoc/>
        public override HashSet<TGroup> GetGroupToGroupMappings(TGroup group, Boolean includeIndirectMappings)
        {
            HashSet<TGroup> result;
            if (includeIndirectMappings == false)
            {
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetGroupToGroupMappingsQueryTime());
                try
                {
                    result = base.GetGroupToGroupMappings(group, includeIndirectMappings);
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
                    result = base.GetGroupToGroupMappings(group, includeIndirectMappings);
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

        /// <inheritdoc/>
        public override void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            RemoveGroupToGroupMapping(fromGroup, toGroup, (postProcessingActionFromGroup, postProcessingActionToGroup) => { });
        }

        /// <inheritdoc/>
        public override void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postProcessingAction)
        {
            Action<TGroup, TGroup, Action<TGroup, TGroup, Action>> removeGroupToGroupMappingAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                base.RemoveGroupToGroupMapping(fromGroup, toGroup, baseAction);
                postProcessingAction.Invoke(fromGroup, toGroup);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TGroup, TGroup, GroupToGroupMappingRemoveTime, GroupToGroupMappingRemoved>(fromGroup, toGroup, removeGroupToGroupMappingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, (postProcessingActionUser, postProcessingActionApplicationComponent, postProcessingActionAccessLevel) => { });
        }

        /// <inheritdoc/>
        public override void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postProcessingAction)
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
            this.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, wrappingAction);
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            return CallBaseClassQueryProcessingMethodWithMetricLogging<IEnumerable<Tuple<TComponent, TAccess>>, GetUserToApplicationComponentAndAccessLevelMappingsQuery>(() =>
            {
                return base.GetUserToApplicationComponentAndAccessLevelMappings(user);
            });
        }

        /// <inheritdoc/>
        public override void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, (postProcessingActionUser, postProcessingActionApplicationComponent, postProcessingActionAccessLevel) => { });
        }

        /// <inheritdoc/>
        public override void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postProcessingAction)
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
            this.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, (postProcessingActionGroup, postProcessingActionApplicationComponent, postProcessingActionAccessLevel) => { });
        }

        /// <inheritdoc/>
        public override void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postProcessingAction)
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
            this.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, wrappingAction);
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            return CallBaseClassQueryProcessingMethodWithMetricLogging<IEnumerable<Tuple<TComponent, TAccess>>, GetGroupToApplicationComponentAndAccessLevelMappingsQuery>(() =>
            {
                return base.GetGroupToApplicationComponentAndAccessLevelMappings(group);
            });
        }

        /// <inheritdoc/>
        public override void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, (postProcessingActionGroup, postProcessingActionApplicationComponent, postProcessingActionAccessLevel) => { });
        }

        /// <inheritdoc/>
        public override void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postProcessingAction)
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
            this.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddEntityType(String entityType)
        {
            AddEntityType(entityType, (postProcessingActionEntityType) => { });
        }

        /// <inheritdoc/>
        public override void AddEntityType(String entityType, Action<String> postProcessingAction)
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
                SetStatusMetricIfLoggingEnabled(new EntityTypesStored(), entities.Count);
            };
            this.AddEntityType(entityType, wrappingAction);
        }

        /// <inheritdoc/>
        public override Boolean ContainsEntityType(String entityType)
        {
            return CallBaseClassQueryProcessingMethodWithMetricLogging<Boolean, ContainsEntityTypeQuery>(() => { return base.ContainsEntityType(entityType); });
        }

        /// <inheritdoc/>
        public override void RemoveEntityType(String entityType)
        {
            RemoveEntityType(entityType, (postProcessingActionEntityType) => { });
        }

        /// <inheritdoc/>
        public override void RemoveEntityType(String entityType, Action<String> postProcessingAction)
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
                if (entities.ContainsKey(entityType) == true)
                {
                    newEntityCount -= entities[entityType].Count;
                }
                Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new EntityTypeRemoveTime());
                try
                {
                    base.RemoveEntityType(entityType, userToEntityTypeMappingPreRemovalAction, groupToEntityTypeMappingPreRemovalAction);
                    postProcessingAction.Invoke(entityType);
                }
                catch
                {
                    CancelIntervalMetricIfLoggingEnabled(beginId, new EntityTypeRemoveTime());
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
            this.RemoveEntityType(entityType, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddEntity(String entityType, String entity)
        {
            AddEntity(entityType, entity, (postProcessingActionEntityType, postProcessingActionEntity) => { });
        }

        /// <inheritdoc/>
        public override void AddEntity(String entityType, String entity, Action<String, String> postProcessingAction)
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
            this.AddEntity(entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override IEnumerable<String> GetEntities(String entityType)
        {
            return CallBaseClassQueryProcessingMethodWithMetricLogging<IEnumerable<String>, GetEntitiesQuery>(() => { return base.GetEntities(entityType); });
        }

        /// <inheritdoc/>
        public override Boolean ContainsEntity(String entityType, String entity)
        {
            return CallBaseClassQueryProcessingMethodWithMetricLogging<Boolean, ContainsEntityQuery>(() => { return base.ContainsEntity(entityType, entity); });
        }

        /// <inheritdoc/>
        public override void RemoveEntity(String entityType, String entity)
        {
            RemoveEntity(entityType, entity, (postProcessingActionEntityType, postProcessingActionEntity) => { });
        }

        /// <inheritdoc/>
        public override void RemoveEntity(String entityType, String entity, Action<String, String> postProcessingAction)
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
                    base.RemoveEntity(entityType, entity, userToEntityMappingPostRemovalAction, groupToEntityMappingPostRemovalAction);
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
            this.RemoveEntity(entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            AddUserToEntityMapping(user, entityType, entity, (postProcessingActionUser, postProcessingActionEntityType, postProcessingActionEntity) => { });
        }

        /// <inheritdoc/>
        public override void AddUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postProcessingAction)
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
            this.AddUserToEntityMapping(user, entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            return CallBaseClassQueryProcessingMethodWithMetricLogging<IEnumerable<Tuple<String, String>>, GetUserToEntityMappingsForUserQuery>(() =>
            {
                return base.GetUserToEntityMappings(user);
            });
        }

        /// <inheritdoc/>
        public override IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            return CallBaseClassQueryProcessingMethodWithMetricLogging<IEnumerable<String>, GetUserToEntityMappingsForUserAndEntityTypeQuery>(() =>
            {
                return base.GetUserToEntityMappings(user, entityType);
            });
        }

        /// <inheritdoc/>
        public override void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            RemoveUserToEntityMapping(user, entityType, entity, (postProcessingActionUser, postProcessingActionEntityType, postProcessingActionEntity) => { });
        }

        /// <inheritdoc/>
        public override void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postProcessingAction)
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
            this.RemoveUserToEntityMapping(user, entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            AddGroupToEntityMapping(group, entityType, entity, (postProcessingActionGroup, postProcessingActionEntityType, postProcessingActionEntity) => { });
        }

        /// <inheritdoc/>
        public override void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postProcessingAction)
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
            this.AddGroupToEntityMapping(group, entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            return CallBaseClassQueryProcessingMethodWithMetricLogging<IEnumerable<Tuple<String, String>>, GetGroupToEntityMappingsForGroupQuery>(() =>
            {
                return base.GetGroupToEntityMappings(group);
            });
        }

        /// <inheritdoc/>
        public override IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            return CallBaseClassQueryProcessingMethodWithMetricLogging<IEnumerable<String>, GetGroupToEntityMappingsForGroupAndEntityTypeQuery>(() =>
            {
                return base.GetGroupToEntityMappings(group, entityType);
            });
        }

        /// <inheritdoc/>
        public override void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            RemoveGroupToEntityMapping(group, entityType, entity, (postProcessingActionGroup, postProcessingActionEntityType, postProcessingActionEntity) => { });
        }

        /// <inheritdoc/>
        public override void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postProcessingAction)
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
            this.RemoveGroupToEntityMapping(group, entityType, entity, wrappingAction);
        }

        /// <inheritdoc/>
        public override Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Boolean result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new HasAccessToApplicationComponentQueryTime());
            try
            {
                result = base.HasAccessToApplicationComponent(user, applicationComponent, accessLevel);
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

        /// <inheritdoc/>
        public override Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            Boolean result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new HasAccessToEntityQueryTime());
            try
            {
                result = base.HasAccessToEntity(user, entityType, entity);
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

        /// <inheritdoc/>
        public override HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            HashSet<Tuple<TComponent, TAccess>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetApplicationComponentsAccessibleByUserQueryTime());
            try
            {
                result = base.GetApplicationComponentsAccessibleByUser(user);
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

        /// <inheritdoc/>
        public override HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            HashSet<Tuple<TComponent, TAccess>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetApplicationComponentsAccessibleByGroupQueryTime());
            try
            {
                result = base.GetApplicationComponentsAccessibleByGroup(group);
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

        /// <inheritdoc/>
        public override HashSet<Tuple<String, String>> GetEntitiesAccessibleByUser(TUser user)
        {
            HashSet<Tuple<String, String>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByUserQueryTime());
            try
            {
                result = base.GetEntitiesAccessibleByUser(user);
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

        /// <inheritdoc/>
        public override HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            HashSet<String> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByUserQueryTime());
            try
            {
                result = base.GetEntitiesAccessibleByUser(user, entityType);
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

        /// <inheritdoc/>
        public override HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroup(TGroup group)
        {
            HashSet<Tuple<String, String>> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByGroupQueryTime());
            try
            {
                result = base.GetEntitiesAccessibleByGroup(group);
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

        /// <inheritdoc/>
        public override HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            HashSet<String> result;
            Nullable<Guid> beginId = BeginIntervalMetricIfLoggingEnabled(new GetEntitiesAccessibleByGroupQueryTime());
            try
            {
                result = base.GetEntitiesAccessibleByGroup(group, entityType);
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
        /// Adds required metric class mappings to the 'mappingMetricLogger' member.
        /// </summary>
        protected void AddMappingMetricLoggerMappings()
        {
            mappingMetricLogger.AddStatusMetricMapping(typeof(LeafVerticesStored), new UsersStored());
            mappingMetricLogger.AddStatusMetricMapping(typeof(NonLeafVerticesStored), new GroupsStored());
            mappingMetricLogger.AddStatusMetricMapping(typeof(LeafToNonLeafEdgesStored), new UserToGroupMappingsStored());
            mappingMetricLogger.AddStatusMetricMapping(typeof(NonLeafToNonLeafEdgesStored), new GroupToGroupMappingsStored());
        }

        /// <summary>
        /// Calls one of the base class methods which implements IAccessManagerEventProcessor, wrapping the call with logging of metric events of the specified types.
        /// </summary>
        /// <typeparam name="TEventProcessorMethodParam">The type of the parameter which is passed to the IAccessManagerEventProcessor method.</typeparam>
        /// <typeparam name="TIntervalMetric">The type of interval metric to log.</typeparam>
        /// <typeparam name="TCountMetric">The type of count metric to log.</typeparam>
        /// <param name="parameterValue">The value of the parameter which is passed to the IAccessManagerEventProcessor method.</param>
        /// <param name="eventProcessorMethodAction">Action which calls the base class IAccessManagerEventProcessor method.  Accepts 2 parameters: the type of the parameter which is passed to the IAccessManagerEventProcessor method, and an inner action which performs the call to the base class IAccessManagerEventProcessor method (and which is invoked during the invocation of the outer 'eventProcessorMethodAction').</param>
        protected void CallBaseClassEventProcessingMethodWithMetricLogging<TEventProcessorMethodParam, TIntervalMetric, TCountMetric>
        (
            TEventProcessorMethodParam parameterValue, 
            Action<TEventProcessorMethodParam, Action<TEventProcessorMethodParam, Action>> eventProcessorMethodAction
        ) 
            where TIntervalMetric: IntervalMetric, new()
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
        /// Calls one of the base class methods which implements IAccessManagerEventProcessor, wrapping the call with logging of metric events of the specified types.
        /// </summary>
        /// <typeparam name="TEventProcessorMethodParam1">The type of the first parameter which is passed to the IAccessManagerEventProcessor method.</typeparam>
        /// <typeparam name="TEventProcessorMethodParam1">The type of the second parameter which is passed to the IAccessManagerEventProcessor method.</typeparam>
        /// <typeparam name="TIntervalMetric">The type of interval metric to log.</typeparam>
        /// <typeparam name="TCountMetric">The type of count metric to log.</typeparam>
        /// <param name="parameterValue1">The value of the first parameter which is passed to the IAccessManagerEventProcessor method.</param>
        /// <param name="parameterValue1">The value of the second parameter which is passed to the IAccessManagerEventProcessor method.</param>
        /// <param name="eventProcessorMethodAction">Action which calls the base class IAccessManagerEventProcessor method.  Accepts 3 parameters: the type of the first parameter which is passed to the IAccessManagerEventProcessor method, the type of the second parameter which is passed to the IAccessManagerEventProcessor method, and an inner action which performs the call to the base class IAccessManagerEventProcessor method (and which is invoked during the invocation of the outer 'eventProcessorMethodAction').</param>
        protected void CallBaseClassEventProcessingMethodWithMetricLogging<TEventProcessorMethodParam1, TEventProcessorMethodParam2, TIntervalMetric, TCountMetric>
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
        /// Calls one of the base class methods which implements IAccessManagerQueryProcessor, logging the specified count metric.
        /// </summary>
        /// <typeparam name="TQueryProcessorMethodReturn">The type of the value returned from the IAccessManagerQueryProcessor method or property.</typeparam>
        /// <typeparam name="TCountMetric">The type of count metric to log.</typeparam>
        /// <param name="queryProcessorMethodFunc">Func which calls the base class IAccessManagerQueryProcessor method or property.  Returns the value from the base class IAccessManagerQueryProcessor method or property.</param>
        /// <returns>The result of the IAccessManagerQueryProcessor method.</returns>
        protected TQueryProcessorMethodReturn CallBaseClassQueryProcessingMethodWithMetricLogging<TQueryProcessorMethodReturn, TCountMetric>(Func<TQueryProcessorMethodReturn> queryProcessorMethodFunc)
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
