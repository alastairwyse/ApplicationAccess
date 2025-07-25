﻿/*
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
using System.Linq;
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
    public class MetricLoggingDependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess> : DependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess>, IMetricLoggingComponent
    {
        /// <summary>Class which wraps and methods with, and generates methods that log the metrics.</summary>
        protected ConcurrentAccessManagerMetricLogger<TUser, TGroup, TComponent, TAccess> metricLoggingWrapper;
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
                return metricLoggingWrapper.MetricLoggingEnabled;
            }

            set
            {
                // TODO: Ideally there should be some kind of lock around these two statements, but will leave off for now
                //   This method should only be called when the event and query methods are not being invoked (e.g. before loading contents at startup)
                metricLoggingWrapper.MetricLoggingEnabled = value;
                ((MetricLoggingConcurrentDirectedGraph<TUser, TGroup>)userToGroupMap).MetricLoggingEnabled = value;
            }
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.MetricLoggingDependencyFreeAccessManager class.
        /// </summary>
        /// <param name="metricLogger">The logger for metrics.</param>
        public MetricLoggingDependencyFreeAccessManager(IMetricLogger metricLogger)
            : base(new MetricLoggingConcurrentDirectedGraph<TUser, TGroup>(false, new MappingMetricLogger(metricLogger)), true)
        {
            // We hard-code base class parameter 'throwIdempotencyExceptions' to true, even though this class (and those derived from it like
            //   DistributedAccessManager) don't throw idempotency exceptions.  It's set true in this case class because wrapping methods in
            //   ConcurrentAccessManagerMetricLogger catch the idempotency exceptions to decide what metrics to log in the case of failure
            //   and then 'swallow' those exceptions to make the class act idempotently.  Hence this class and those derived from it
            //   basically stop exposing the option to throw exceptions when an idempotent event occurs.

            // Below casting should never fail, since we just newed the 'userToGroupMap' and 'MetricLogger' properties to these types.
            //   TODO: Find a cleaner way to do this... ideally don't want to expose the 'MetricLoggingConcurrentDirectedGraph.MetricLogger' property at all.
            mappingMetricLogger = (MappingMetricLogger)((MetricLoggingConcurrentDirectedGraph<TUser, TGroup>)userToGroupMap).MetricLogger;
            metricLoggingWrapper = new ConcurrentAccessManagerMetricLogger<TUser, TGroup, TComponent, TAccess>(metricLogger);
            AddMappingMetricLoggerMappings();
        }

        /// <inheritdoc/>
        public override Boolean ContainsUser(TUser user)
        {
            return metricLoggingWrapper.ContainsUser(user, base.ContainsUser);
        }

        /// <inheritdoc/>
        public override Boolean ContainsGroup(TGroup group)
        {
            return metricLoggingWrapper.ContainsGroup(group, base.ContainsGroup);
        }

        /// <inheritdoc/>
        public override HashSet<TGroup> GetUserToGroupMappings(TUser user, Boolean includeIndirectMappings)
        {
            return metricLoggingWrapper.GetUserToGroupMappings(user, includeIndirectMappings, base.GetUserToGroupMappings);
        }

        /// <inheritdoc/>
        public override HashSet<TUser> GetGroupToUserMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return metricLoggingWrapper.GetGroupToUserMappings(group, includeIndirectMappings, base.GetGroupToUserMappings);
        }

        /// <inheritdoc/>
        public override HashSet<TGroup> GetGroupToGroupMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return metricLoggingWrapper.GetGroupToGroupMappings(group, includeIndirectMappings, base.GetGroupToGroupMappings);
        }

        /// <inheritdoc/>
        public override HashSet<TGroup> GetGroupToGroupReverseMappings(TGroup group, Boolean includeIndirectMappings)
        {
            return metricLoggingWrapper.GetGroupToGroupReverseMappings(group, includeIndirectMappings, base.GetGroupToGroupReverseMappings);
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            return metricLoggingWrapper.GetUserToApplicationComponentAndAccessLevelMappings(user, base.GetUserToApplicationComponentAndAccessLevelMappings);
        }

        /// <inheritdoc/>
        public override IEnumerable<TUser> GetApplicationComponentAndAccessLevelToUserMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            return metricLoggingWrapper.GetApplicationComponentAndAccessLevelToUserMappings(applicationComponent, accessLevel, includeIndirectMappings, base.GetApplicationComponentAndAccessLevelToUserMappings);
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            return metricLoggingWrapper.GetGroupToApplicationComponentAndAccessLevelMappings(group, base.GetGroupToApplicationComponentAndAccessLevelMappings);
        }

        /// <inheritdoc/>
        public override IEnumerable<TGroup> GetApplicationComponentAndAccessLevelToGroupMappings(TComponent applicationComponent, TAccess accessLevel, Boolean includeIndirectMappings)
        {
            return metricLoggingWrapper.GetApplicationComponentAndAccessLevelToGroupMappings(applicationComponent, accessLevel, includeIndirectMappings, base.GetApplicationComponentAndAccessLevelToGroupMappings);
        }

        /// <inheritdoc/>
        public override Boolean ContainsEntityType(String entityType)
        {
            return metricLoggingWrapper.ContainsEntityType(entityType, base.ContainsEntityType);
        }

        /// <inheritdoc/>
        public override IEnumerable<String> GetEntities(String entityType)
        {
            return metricLoggingWrapper.GetEntities(entityType, base.GetEntities);
        }

        /// <inheritdoc/>
        public override Boolean ContainsEntity(String entityType, String entity)
        {
            return metricLoggingWrapper.ContainsEntity(entityType, entity, base.ContainsEntity);
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            return metricLoggingWrapper.GetUserToEntityMappings(user, base.GetUserToEntityMappings);
        }

        /// <inheritdoc/>
        public override IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            return metricLoggingWrapper.GetUserToEntityMappings(user, entityType, base.GetUserToEntityMappings);
        }

        /// <inheritdoc/>
        public override IEnumerable<TUser> GetEntityToUserMappings(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return metricLoggingWrapper.GetEntityToUserMappings(entityType, entity, includeIndirectMappings, base.GetEntityToUserMappings);
        }

        /// <inheritdoc/>
        public override IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            return metricLoggingWrapper.GetGroupToEntityMappings(group, base.GetGroupToEntityMappings);
        }

        /// <inheritdoc/>
        public override IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            return metricLoggingWrapper.GetGroupToEntityMappings(group, entityType, base.GetGroupToEntityMappings);
        }

        /// <inheritdoc/>
        public override IEnumerable<TGroup> GetEntityToGroupMappings(String entityType, String entity, Boolean includeIndirectMappings)
        {
            return metricLoggingWrapper.GetEntityToGroupMappings(entityType, entity, includeIndirectMappings, base.GetEntityToGroupMappings);
        }

        /// <inheritdoc/>
        public override Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            return metricLoggingWrapper.HasAccessToApplicationComponent(user, applicationComponent, accessLevel, base.HasAccessToApplicationComponent);
        }

        /// <inheritdoc/>
        public override Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            return metricLoggingWrapper.HasAccessToEntity(user, entityType, entity, base.HasAccessToEntity);
        }

        /// <inheritdoc/>
        public override HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByUser(TUser user)
        {
            return metricLoggingWrapper.GetApplicationComponentsAccessibleByUser(user, base.GetApplicationComponentsAccessibleByUser);
        }

        /// <inheritdoc/>
        public override HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroup(TGroup group)
        {
            return metricLoggingWrapper.GetApplicationComponentsAccessibleByGroup(group, base.GetApplicationComponentsAccessibleByGroup);
        }

        /// <inheritdoc/>
        public override HashSet<Tuple<String, String>> GetEntitiesAccessibleByUser(TUser user)
        {
            return metricLoggingWrapper.GetEntitiesAccessibleByUser(user, base.GetEntitiesAccessibleByUser);
        }

        /// <inheritdoc/>
        public override HashSet<String> GetEntitiesAccessibleByUser(TUser user, String entityType)
        {
            return metricLoggingWrapper.GetEntitiesAccessibleByUser(user, entityType, base.GetEntitiesAccessibleByUser);
        }

        /// <inheritdoc/>
        public override HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroup(TGroup group)
        {
            return metricLoggingWrapper.GetEntitiesAccessibleByGroup(group, base.GetEntitiesAccessibleByGroup);
        }

        /// <inheritdoc/>
        public override HashSet<String> GetEntitiesAccessibleByGroup(TGroup group, String entityType)
        {
            return metricLoggingWrapper.GetEntitiesAccessibleByGroup(group, entityType, base.GetEntitiesAccessibleByGroup);
        }

        #region Private/Protected Methods

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

        #region Base Class Methods with 'wrappingAction' Parameter Overrides

        /// <inheritdoc/>
        protected override void Clear(Action<Action> wrappingAction)
        {
            Action<Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateClearMetricLoggingWrappingAction(wrappingAction, entities);
            base.Clear(metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void AddUser(TUser user, Action<TUser, Action> wrappingAction)
        {
            Action<TUser, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddUserMetricLoggingWrappingAction(user, wrappingAction);
            base.AddUser(user, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void RemoveUser(TUser user, Action<TUser, Action> wrappingAction)
        {
            Action<TUser, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateRemoveUserMetricLoggingWrappingAction(user, wrappingAction, userToComponentMap);
            base.RemoveUser(user, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void AddGroup(TGroup group, Action<TGroup, Action> wrappingAction)
        {
            Action<TGroup, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddGroupMetricLoggingWrappingAction(group, wrappingAction);
            base.AddGroup(group, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void RemoveGroup(TGroup group, Action<TGroup, Action> wrappingAction)
        {
            Action<TGroup, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateRemoveGroupMetricLoggingWrappingAction(group, wrappingAction, groupToComponentMap);
            base.RemoveGroup(group, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void AddUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup, Action> wrappingAction)
        {
            Action<TUser, TGroup, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddUserToGroupMappingMetricLoggingWrappingAction(user, group, wrappingAction);
            base.AddUserToGroupMapping(user, group, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void RemoveUserToGroupMapping(TUser user, TGroup group, Action<TUser, TGroup, Action> wrappingAction)
        {
            Action<TUser, TGroup, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateRemoveUserToGroupMappingMetricLoggingWrappingAction(user, group, wrappingAction);
            base.RemoveUserToGroupMapping(user, group, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup, Action> wrappingAction)
        {
            Action<TGroup, TGroup, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddGroupToGroupMappingMetricLoggingWrappingAction(fromGroup, toGroup, wrappingAction);
            base.AddGroupToGroupMapping(fromGroup, toGroup, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup, Action<TGroup, TGroup, Action> wrappingAction)
        {
            Action<TGroup, TGroup, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateRemoveGroupToGroupMappingMetricLoggingWrappingAction(fromGroup, toGroup, wrappingAction);
            base.RemoveGroupToGroupMapping(fromGroup, toGroup, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess, Action> wrappingAction)
        {
            Action<TUser, TComponent, TAccess, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddUserToApplicationComponentAndAccessLevelMappingMetricLoggingWrappingAction(user, applicationComponent, accessLevel, wrappingAction);
            base.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel, Action<TUser, TComponent, TAccess, Action> wrappingAction)
        {
            Action<TUser, TComponent, TAccess, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateRemoveUserToApplicationComponentAndAccessLevelMappingMetricLoggingWrappingAction(user, applicationComponent, accessLevel, wrappingAction);
            base.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess, Action> wrappingAction)
        {
            Action<TGroup, TComponent, TAccess, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddGroupToApplicationComponentAndAccessLevelMappingMetricLoggingWrappingAction(group, applicationComponent, accessLevel, wrappingAction);
            base.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel, Action<TGroup, TComponent, TAccess, Action> wrappingAction)
        {
            Action<TGroup, TComponent, TAccess, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateRemoveGroupToApplicationComponentAndAccessLevelMappingMetricLoggingWrappingAction(group, applicationComponent, accessLevel, wrappingAction);
            base.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void AddEntityType(String entityType, Action<String, Action> wrappingAction)
        {
            Action<String, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddEntityTypeMetricLoggingWrappingAction(entityType, wrappingAction, entities);
            base.AddEntityType(entityType, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void RemoveEntityType(String entityType, Action<String, Action> wrappingAction)
        {
            Action<String, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateRemoveEntityTypeMetricLoggingWrappingAction(entityType, wrappingAction, entities, base.RemoveEntityType);
            base.RemoveEntityType(entityType, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void AddEntity(String entityType, String entity, Action<String, String, Action> wrappingAction)
        {
            Action<String, String, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddEntityMetricLoggingWrappingAction(entityType, entity, wrappingAction);
            base.AddEntity(entityType, entity, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void RemoveEntity(String entityType, String entity, Action<String, String, Action> wrappingAction)
        {
            Action<String, String, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateRemoveEntityMetricLoggingWrappingAction(entityType, entity, wrappingAction, base.RemoveEntity);
            base.RemoveEntity(entityType, entity, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void AddUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String, Action> wrappingAction)
        {
            Action<TUser, String, String, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddUserToEntityMappingMetricLoggingWrappingAction(user, entityType, entity, wrappingAction);
            base.AddUserToEntityMapping(user, entityType, entity, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void RemoveUserToEntityMapping(TUser user, String entityType, String entity, Action<TUser, String, String, Action> wrappingAction)
        {
            Action<TUser, String, String, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateRemoveUserToEntityMappingMetricLoggingWrappingAction(user, entityType, entity, wrappingAction);
            base.RemoveUserToEntityMapping(user, entityType, entity, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void AddGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String, Action> wrappingAction)
        {
            Action<TGroup, String, String, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddGroupToEntityMappingMetricLoggingWrappingAction(group, entityType, entity, wrappingAction);
            base.AddGroupToEntityMapping(group, entityType, entity, metricLoggingWrappingAction);
        }

        /// <inheritdoc/>
        protected override void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity, Action<TGroup, String, String, Action> wrappingAction)
        {
            Action<TGroup, String, String, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateRemoveGroupToEntityMappingMetricLoggingWrappingAction(group, entityType, entity, wrappingAction);
            base.RemoveGroupToEntityMapping(group, entityType, entity, metricLoggingWrappingAction);
        }

        #endregion

        #region Idempotent 'Add' Method Overrides for Primary Elements

        /// <inheritdoc/>
        protected override void AddUser(TUser user, Boolean generateEvent)
        {
            if (userToGroupMap.ContainsLeafVertex(user) == false)
            {
                // Parameter 'generateEvent' is only set true when these methods are called as a prepending for a secondary Add*() event
                //   so we only log metrics when the prepended element doesn't already exist AND when this is called as part of prepending.
                //   We don't log metrics when this is called as part of a non-prepended event, since that metric logging is done already 
                //   in the 'wrappingAction' parameter overload of this method (which itself calls this method).
                if (generateEvent == true)
                {
                    Action<TUser, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddUserMetricLoggingWrappingAction(user, (TUser wrappingActionUser, Action wrappingActionBaseAction) => { wrappingActionBaseAction.Invoke(); });
                    metricLoggingWrappingAction.Invoke(user, () => { userToGroupMap.AddLeafVertex(user); });
                    eventProcessor.AddUser(user);
                }
                else
                {
                    userToGroupMap.AddLeafVertex(user);
                }
            }
            else
            {
                if (throwIdempotencyExceptions == true)
                    throw new IdempotentAddOperationException();
            }
        }

        /// <inheritdoc/>
        protected override void AddGroup(TGroup group, Boolean generateEvent)
        {
            if (userToGroupMap.ContainsNonLeafVertex(group) == false)
            {
                if (generateEvent == true)
                {
                    Action<TGroup, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddGroupMetricLoggingWrappingAction(group, (TGroup wrappingActionGroup, Action wrappingActionBaseAction) => { wrappingActionBaseAction.Invoke(); });
                    metricLoggingWrappingAction.Invoke(group, () => { userToGroupMap.AddNonLeafVertex(group); });
                    eventProcessor.AddGroup(group);
                }
                else
                {
                    userToGroupMap.AddNonLeafVertex(group);
                }
            }
            else
            {
                if (throwIdempotencyExceptions == true)
                    throw new IdempotentAddOperationException();
            }
        }

        /// <inheritdoc/>
        protected override void AddEntityType(String entityType, Boolean generateEvent)
        {
            if (entities.ContainsKey(entityType) == false)
            {
                if (generateEvent == true)
                {
                    Action<String, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddEntityTypeMetricLoggingWrappingAction
                    (
                        entityType,
                        (String wrappingActionEntityType, Action wrappingActionBaseAction) => { wrappingActionBaseAction.Invoke(); },
                        entities
                    );
                    metricLoggingWrappingAction.Invoke(entityType, () => { entities.Add(entityType, collectionFactory.GetSetInstance<String>()); });
                    eventProcessor.AddEntityType(entityType);
                }
                else
                {
                    entities.Add(entityType, collectionFactory.GetSetInstance<String>());
                }
            }
            else
            {
                if (throwIdempotencyExceptions == true)
                    throw new IdempotentAddOperationException();
            }
        }

        /// <inheritdoc/>
        protected override void AddEntity(String entityType, String entity, Boolean generateEvent)
        {
            if (entities.ContainsKey(entityType) == false)
            {
                AddEntityType(entityType, true);
            }
            if (entities[entityType].Contains(entity) == false)
            {
                if (generateEvent == true)
                {
                    Action<String, String, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateAddEntityMetricLoggingWrappingAction
                    (
                        entityType,
                        entity,
                        (String wrappingActionEntityType, String wrappingActionEntity, Action wrappingActionBaseAction) => { wrappingActionBaseAction.Invoke(); }
                    );
                    metricLoggingWrappingAction.Invoke(entityType, entity, () => { entities[entityType].Add(entity); });
                    eventProcessor.AddEntity(entityType, entity);
                }
                else
                {
                    entities[entityType].Add(entity);
                }
            }
            else
            {
                if (throwIdempotencyExceptions == true)
                    throw new IdempotentAddOperationException();
            }
        }

        #endregion

        #region Event Methods With Switchable Metric Logging

        // Likely seems strange to have a method override which makes metric logging optional in a class called 'MetricLoggingDependencyFreeAccessManager'.  Reason is that
        //   derived class DistributedAccessManager has method overrides which allow prepending secondary element remove events on remove of a primary element (e.g. RemoveEntityType()
        //   creates prepended 'remove entity' events to allow for accurate splitting and merging of shard groups in distributed implementations).  The usual pattern of override of
        //   event methods with a 'wrappingAction' parameter is to call the base method, placing the overridden funcionality in the passed 'wrappingAction' parameter, which will cause 
        //   program flow to first call the ConcurrentAccessManager method implementation which sets mutual exclusion locks, and then move back up the inheritance hiearachy (from
        //   ConcurrentAccessManager to most-derived) calling each method implementation, before finally calling the actual event implementation in AccessManagerBase.  The problem 
        //   with this for the aforementioned 'prepended secondary element remove event' method implementations in DistributedAccessManager is that metric logging in this class gets 'Begun()'
        //   before the prepended secondary remove events have been created.  Hence the prepended remove events are included in internal metrics for the primary remove event, which both
        //   skews/bloats those metrics and is inconsistent with the prepended of dependent 'primary add' events in DependencyFreeAccessManager (where prepended 'add' events are NOT included
        //   in the main event's metrics).
        //
        // Hence we create the below methods which allow derived classes to actually bypass the metric logging in this class, and instead implement the metric logging in their own
        //   method along with additional functionality, but permitting the derived class to run things before interval metrics are started.

        /// <summary>
        /// Removes an entity type.
        /// </summary>
        /// <param name="entityType">The entity type to remove.</param>
        /// <param name="wrappingAction">An action to invoke after removing the entity type but whilst any mutual-exclusion locks are still acquired.</param>
        /// <param name="logMetrics">Whether to log metrics.</param>
        protected void RemoveEntityType(String entityType, Action<String, Action> wrappingAction, Boolean logMetrics)
        {
            if (logMetrics == true)
            {
                RemoveEntityType(entityType, wrappingAction);
            }
            else
            {
                base.RemoveEntityType(entityType, wrappingAction);
            }
        }

        #endregion

        #endregion
    }
}
