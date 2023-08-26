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
using ApplicationAccess.Utilities;
using ApplicationMetrics;

namespace ApplicationAccess.Metrics
{
    /// <summary>
    /// A version of the <see cref="DependencyFreeAccessManager{TUser, TGroup, TComponent, TAccess}"/> class which logs various metrics.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>
    /// <para>Thread safety is implemented by using concurrent collections internally to represent the user, group, component, access level, and entity mappings (allows for concurrent read and enumeration operations), and locks to serialize modification operations.  Note that all generic type parameters must implement relevant methods to allow storing in a <see cref="System.Collections.Generic.HashSet{T}"/> (at minimum <see cref="IEquatable{T}"/> and <see cref="Object.GetHashCode">GetHashcode()</see>).  This is not enforced as a generic type contraint in order to allow the type parameters to be enums.</para>
    /// <para>Note that interval metrics are not logged for <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> methods that either return IEnumerable or perform simple lookups on HashSets or Dictionaries.  For methods returning IEnumerable, their 'work' is not done until the returned IEnumerable is iterated, so capturing an interval around just the return of the IEnumerable does not provide a realistic metric.  For methods that perform just HashSets or Dictionary lookups, the performance cost of these operations is negligible, hence capturing metrics around them does not provide much value.</para>
    /// </remarks>
    public class MetricLoggingDependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess> : DependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>Interface to private and protected members of the class, used by the 'metricLoggingDecorator' member.</summary>
        protected ConcurrentAccessManagerPrivateMemberInterface<TUser, TGroup, TComponent, TAccess> privateMemberInterface;
        /// <summary>The logger for metrics.</summary>
        protected ConcurrentAccessManagerMetricLoggingInternalDecorator<TUser, TGroup, TComponent, TAccess> metricLoggingDecorator;
        /// <summary>Metric mapper used by the 'userToGroupMap' DirectedGraph member, e.g. to map metrics for 'leaf vertices' to metrics for 'users'.</summary>
        protected MappingMetricLogger mappingMetricLogger;

        /// <summary>
        /// Whether logging of metrics is enabled.
        /// </summary>
        /// <remarks>Generally this would be set true, but may need to be set false in some situations (e.g. when loading contents from a database).</remarks>
        public Boolean MetricLoggingEnabled
        {
            get
            {
                return metricLoggingDecorator.MetricLoggingEnabled;
            }

            set
            {
                metricLoggingDecorator.MetricLoggingEnabled = value;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.MetricLoggingDependencyFreeAccessManager class.
        /// </summary>
        /// <param name="storeBidirectionalMappings">Whether to store bidirectional mappings between elements.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>If parameter 'storeBidirectionalMappings' is set to True, mappings between elements in the manager are stored in both directions.  This avoids slow scanning of dictionaries which store the mappings in certain operations (like RemoveEntityType()), at the cost of addition storage and hence memory usage.</remarks>
        public MetricLoggingDependencyFreeAccessManager(Boolean storeBidirectionalMappings, IMetricLogger metricLogger)
            : base(new MetricLoggingConcurrentDirectedGraph<TUser, TGroup>(storeBidirectionalMappings, new MappingMetricLogger(metricLogger)), storeBidirectionalMappings)
        {
            // Casting should never fail, since we just newed the 'userToGroupMap' and 'MetricLogger' properties to these types.
            //   TODO: Find a cleaner way to do this... ideally don't want to expose the 'MetricLoggingConcurrentDirectedGraph.MetricLogger' property at all.
            mappingMetricLogger = (MappingMetricLogger)((MetricLoggingConcurrentDirectedGraph<TUser, TGroup>)userToGroupMap).MetricLogger;
            AddMappingMetricLoggerMappings();
            InitializeMetricLoggingDecorator(metricLogger);
        }

        /// <inheritdoc/>
        public override void Clear()
        {
            metricLoggingDecorator.Clear();
        }

        /// <inheritdoc/>
        public override void AddUser(TUser user)
        {
            AddUser(user, (postProcessingActionUser) => { });
        }

        /// <inheritdoc/>
        public override void AddUser(TUser user, Action<TUser> postProcessingAction)
        {
            metricLoggingDecorator.AddUser(user, postProcessingAction);
        }

        /// <inheritdoc/>
        public override Boolean ContainsUser(TUser user)
        {
            return metricLoggingDecorator.ContainsUser(user);
        }

        /// <inheritdoc/>
        public override void RemoveUser(TUser user)
        {
            RemoveUser(user, (postProcessingActionUser) => { });
        }

        /// <inheritdoc/>
        public override void RemoveUser(TUser user, Action<TUser> postProcessingAction)
        {
            metricLoggingDecorator.RemoveUser(user, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddGroup(TGroup group)
        {
            AddGroup(group, (postProcessingActionGroup) => { });
        }

        /// <inheritdoc/>
        public override void AddGroup(TGroup group, Action<TGroup> postProcessingAction)
        {
            metricLoggingDecorator.AddGroup(group, postProcessingAction);
        }

        /// <inheritdoc/>
        public override Boolean ContainsGroup(TGroup group)
        {
            return metricLoggingDecorator.ContainsGroup(group);
        }

        /// <inheritdoc/>
        public override void RemoveGroup(TGroup group)
        {
            RemoveGroup(group, (postProcessingActionGroup) => { });
        }

        /// <inheritdoc/>
        public override void RemoveGroup(TGroup group, Action<TGroup> postProcessingAction)
        {
            metricLoggingDecorator.RemoveGroup(group, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToGroupMapping(TUser user, TGroup group)
        {
            AddUserToGroupMapping(user, group, (postProcessingActionUser, postProcessingActionGroup) => { });
        }

        /// <inheritdoc/>
        public override void AddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postProcessingAction)
        {
            metricLoggingDecorator.AddUserToGroupMapping(user, group, postProcessingAction);
        }

        /// <inheritdoc/>
        public override HashSet<TGroup> GetUserToGroupMappings(TUser user, Boolean includeIndirectMappings)
        {
            return metricLoggingDecorator.GetUserToGroupMappings(user, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public override void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            RemoveUserToGroupMapping(user, group, (postProcessingActionUser, postProcessingActionGroup) => { });
        }

        /// <inheritdoc/>
        public override void RemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup> postProcessingAction)
        {
            metricLoggingDecorator.RemoveUserToGroupMapping(user, group, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            AddGroupToGroupMapping(fromGroup, toGroup, (postProcessingActionFromGroup, postProcessingActionToGroup) => { });
        }

        /// <inheritdoc/>
        public override void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postProcessingAction)
        {
            metricLoggingDecorator.AddGroupToGroupMapping(fromGroup, toGroup, postProcessingAction);
        }

        /// <inheritdoc/>
        public override HashSet<TGroup> GetGroupToGroupMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return metricLoggingDecorator.GetGroupToGroupMappings(group, includeIndirectMappings);
        }

        /// <inheritdoc/>
        public override void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            RemoveGroupToGroupMapping(fromGroup, toGroup, (postProcessingActionFromGroup, postProcessingActionToGroup) => { });
        }

        /// <inheritdoc/>
        public override void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup> postProcessingAction)
        {
            metricLoggingDecorator.RemoveGroupToGroupMapping(fromGroup, toGroup, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, (postProcessingActionUser, postProcessingActionApplicationComponent, postProcessingActionAccessLevel) => { });
        }

        /// <inheritdoc/>
        public override void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postProcessingAction)
        {
            metricLoggingDecorator.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, postProcessingAction);
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            return metricLoggingDecorator.GetUserToApplicationComponentAndAccessLevelMappings(user);
        }

        /// <inheritdoc/>
        public override void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, (postProcessingActionUser, postProcessingActionApplicationComponent, postProcessingActionAccessLevel) => { });
        }

        /// <inheritdoc/>
        public override void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess> postProcessingAction)
        {
            metricLoggingDecorator.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, (postProcessingActionGroup, postProcessingActionApplicationComponent, postProcessingActionAccessLevel) => { });
        }

        /// <inheritdoc/>
        public override void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postProcessingAction)
        {
            metricLoggingDecorator.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, postProcessingAction);
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            return metricLoggingDecorator.GetGroupToApplicationComponentAndAccessLevelMappings(group);
        }

        /// <inheritdoc/>
        public override void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, (postProcessingActionGroup, postProcessingActionApplicationComponent, postProcessingActionAccessLevel) => { });
        }

        /// <inheritdoc/>
        public override void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess> postProcessingAction)
        {
            metricLoggingDecorator.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddEntityType(String entityType)
        {
            AddEntityType(entityType, (postProcessingActionEntityType) => { });
        }

        /// <inheritdoc/>
        public override void AddEntityType(String entityType, Action<String> postProcessingAction)
        {
            metricLoggingDecorator.AddEntityType(entityType, postProcessingAction);
        }

        /// <inheritdoc/>
        public override Boolean ContainsEntityType(String entityType)
        {
            return metricLoggingDecorator.ContainsEntityType(entityType);
        }

        /// <inheritdoc/>
        public override void RemoveEntityType(String entityType)
        {
            RemoveEntityType(entityType, (postProcessingActionEntityType) => { });
        }

        /// <inheritdoc/>
        public override void RemoveEntityType(String entityType, Action<String> postProcessingAction)
        {
            metricLoggingDecorator.RemoveEntityType(entityType, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddEntity(String entityType, String entity)
        {
            AddEntity(entityType, entity, (postProcessingActionEntityType, postProcessingActionEntity) => { });
        }

        /// <inheritdoc/>
        public override void AddEntity(String entityType, String entity, Action<String, String> postProcessingAction)
        {
            metricLoggingDecorator.AddEntity(entityType, entity, postProcessingAction);
        }

        /// <inheritdoc/>
        public override IEnumerable<String> GetEntities(String entityType)
        {
            return metricLoggingDecorator.GetEntities(entityType);
        }

        /// <inheritdoc/>
        public override Boolean ContainsEntity(String entityType, String entity)
        {
            return metricLoggingDecorator.ContainsEntity(entityType, entity);
        }

        /// <inheritdoc/>
        public override void RemoveEntity(String entityType, String entity)
        {
            RemoveEntity(entityType, entity, (postProcessingActionEntityType, postProcessingActionEntity) => { });
        }

        /// <inheritdoc/>
        public override void RemoveEntity(String entityType, String entity, Action<String, String> postProcessingAction)
        {
            metricLoggingDecorator.RemoveEntity(entityType, entity, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            AddUserToEntityMapping(user, entityType, entity, (postProcessingActionUser, postProcessingActionEntityType, postProcessingActionEntity) => { });
        }

        /// <inheritdoc/>
        public override void AddUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postProcessingAction)
        {
            metricLoggingDecorator.AddUserToEntityMapping(user, entityType, entity, postProcessingAction);
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            return metricLoggingDecorator.GetUserToEntityMappings(user);
        }

        /// <inheritdoc/>
        public override IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            return metricLoggingDecorator.GetUserToEntityMappings(user, entityType);
        }

        /// <inheritdoc/>
        public override void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            RemoveUserToEntityMapping(user, entityType, entity, (postProcessingActionUser, postProcessingActionEntityType, postProcessingActionEntity) => { });
        }

        /// <inheritdoc/>
        public override void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String> postProcessingAction)
        {
            metricLoggingDecorator.RemoveUserToEntityMapping(user, entityType, entity, postProcessingAction);
        }

        /// <inheritdoc/>
        public override void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            AddGroupToEntityMapping(group, entityType, entity, (postProcessingActionGroup, postProcessingActionEntityType, postProcessingActionEntity) => { });
        }

        /// <inheritdoc/>
        public override void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postProcessingAction)
        {
            metricLoggingDecorator.AddGroupToEntityMapping(group, entityType, entity, postProcessingAction);
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            return metricLoggingDecorator.GetGroupToEntityMappings(group);
        }

        /// <inheritdoc/>
        public override IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            return metricLoggingDecorator.GetGroupToEntityMappings(group, entityType);
        }

        /// <inheritdoc/>
        public override void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            RemoveGroupToEntityMapping(group, entityType, entity, (postProcessingActionGroup, postProcessingActionEntityType, postProcessingActionEntity) => { });
        }

        /// <inheritdoc/>
        public override void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String> postProcessingAction)
        {
            metricLoggingDecorator.RemoveGroupToEntityMapping(group, entityType, entity, postProcessingAction);
        }

        /// <inheritdoc/>
        public override Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            return metricLoggingDecorator.HasAccessToApplicationComponent(user, applicationComponent, accessLevel);
        }

        /// <inheritdoc/>
        public override Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            return metricLoggingDecorator.HasAccessToEntity(user, entityType, entity);
        }

        /// <inheritdoc/>
        public override HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            return metricLoggingDecorator.GetApplicationComponentsAccessibleByUser(user);
        }

        /// <inheritdoc/>
        public override HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            return metricLoggingDecorator.GetApplicationComponentsAccessibleByGroup(group);
        }

        /// <inheritdoc/>
        public override HashSet<Tuple<String, String>> GetEntitiesAccessibleByUser(TUser user)
        {
            return metricLoggingDecorator.GetEntitiesAccessibleByUser(user);
        }

        /// <inheritdoc/>
        public override HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            return metricLoggingDecorator.GetEntitiesAccessibleByUser(user, entityType);
        }

        /// <inheritdoc/>
        public override HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroup(TGroup group)
        {
            return metricLoggingDecorator.GetEntitiesAccessibleByGroup(group);
        }

        /// <inheritdoc/>
        public override HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            return metricLoggingDecorator.GetEntitiesAccessibleByGroup(group, entityType);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Initializes the 'metricLoggingDecorator' member.
        /// </summary>
        /// <param name="metricLogger">The metric logger to pass to the decorator.</param>
        protected void InitializeMetricLoggingDecorator(IMetricLogger metricLogger)
        {
            privateMemberInterface = new ConcurrentAccessManagerPrivateMemberInterface<TUser, TGroup, TComponent, TAccess>
            (
                base.userToGroupMap,
                base.userToComponentMap,
                base.groupToComponentMap,
                base.entities,
                base.Clear,
                base.AddUser,
                base.ContainsUser,
                base.RemoveUser,
                base.AddGroup,
                base.ContainsGroup,
                base.RemoveGroup,
                base.AddUserToGroupMapping,
                base.GetUserToGroupMappings,
                base.RemoveUserToGroupMapping,
                base.AddGroupToGroupMapping,
                base.GetGroupToGroupMappings,
                base.RemoveGroupToGroupMapping,
                base.AddUserToApplicationComponentAndAccessLevelMapping,
                base.GetUserToApplicationComponentAndAccessLevelMappings,
                base.RemoveUserToApplicationComponentAndAccessLevelMapping,
                base.AddGroupToApplicationComponentAndAccessLevelMapping,
                base.GetGroupToApplicationComponentAndAccessLevelMappings,
                base.RemoveGroupToApplicationComponentAndAccessLevelMapping,
                base.AddEntityType,
                base.ContainsEntityType,
                base.RemoveEntityType,
                base.RemoveEntityType,
                base.AddEntity,
                base.GetEntities,
                base.ContainsEntity,
                base.RemoveEntity,
                base.RemoveEntity,
                base.AddUserToEntityMapping,
                base.GetUserToEntityMappings,
                base.GetUserToEntityMappings,
                base.RemoveUserToEntityMapping,
                base.AddGroupToEntityMapping,
                base.GetGroupToEntityMappings,
                base.GetGroupToEntityMappings,
                base.RemoveGroupToEntityMapping,
                base.HasAccessToApplicationComponent,
                base.HasAccessToEntity,
                base.GetApplicationComponentsAccessibleByUser,
                base.GetApplicationComponentsAccessibleByGroup,
                base.GetEntitiesAccessibleByUser,
                base.GetEntitiesAccessibleByUser,
                base.GetEntitiesAccessibleByGroup,
                base.GetEntitiesAccessibleByGroup
            );
            metricLoggingDecorator = new ConcurrentAccessManagerMetricLoggingInternalDecorator<TUser, TGroup, TComponent, TAccess>(this, privateMemberInterface, metricLogger);
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

        #endregion
    }
}
