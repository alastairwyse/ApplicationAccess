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
using System.Text;
using ApplicationMetrics;
using MoreComplexDataStructures;
using ApplicationAccess.Utilities;

namespace ApplicationAccess.Metrics
{
    /// <summary>
    /// A thread-safe version of the AccessManager class, which can be accessed and modified by multiple threads concurrently, and which logs various metrics.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <remarks>Thread safety is implemented by using concurrent collections internally to represent the user, group, component, access level, and entity mappings (allows for concurrent read and enumeration operations), and locks to serialize modification operations.  Note that all generic type parameters must implement relevant methods to allow storing in a HashSet (at minimum IEquatable&lt;T&gt; and GetHashcode()).  This is not enforced as a generic type contraint in order to allow the type parameters to be enums.</remarks>
    public class MetricLoggingConcurrentAccessManager<TUser, TGroup, TComponent, TAccess> : ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The number of user to application component and access level mappings in the access manager.</summary>
        protected Int32 userToApplicationComponentAndAccessLevelMappingCount;
        /// <summary>The number of group to application component and access level mappings in the access manager.</summary>
        protected Int32 groupToApplicationComponentAndAccessLevelMappingCount;
        /// <summary>The number of entities in the access manager.</summary>
        protected Int32 entityCount;
        /// <summary>The number of entity mappings stored per user.</summary>
        protected FrequencyTable<TUser> entityMappingCountsPerUser;
        /// <summary>The number of entity mappings stored per group.</summary>
        protected FrequencyTable<TGroup> entityMappingCountsPerGroup;

        /// <summary>Whether interval metrics should be logged for methods belonging to the IAccessManagerQueryProcessor interface.</summary>
        protected Boolean logQueryProcessorIntervalMetrics;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;
        /// <summary>Metric mapper used by the 'userToGroupMap' DirectedGraph member, e.g. to map metrics for 'leaf vertices' to metrics for 'users'.</summary>
        protected MappingMetricLogger mappingMetricLogger;


        // 2022-06-18 TODO:
        //   Will need to add this constructor param for whether I want to log query processor interval metrics or not
        //     Will need a remark warning that turning param true might cause inaccuracy of interval metrics when concurrent calls are received... and explain why
        //   Add query processor methods
        //     Will probably have to make AccessManager query methods virtual




        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.MetricLoggingConcurrentAccessManager class.
        /// </summary>
        /// <param name="logQueryProcessorIntervalMetrics">Whether interval metrics should be logged for methods belonging to the IAccessManagerQueryProcessor interface.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>Methods and properties belonging to the IAccessManagerQueryProcessor interface (e.g. ContainsUser()) are not wrapped in mutual exclusion locks due to the class using underlying concurrent collections which can be accessed by multiple threads concurrently.  However this causes a potential issue for logging of interval metrics which require successive Begin() and End() calls to be logged... i.e. the potential for Begin() and End() calls from different threads becoming interleaved.  Errors arising from potential interleaving can be avoided if the IMetricLogger implementation supports ignoring of interleaving (e.g. as is done by the 'intervalMetricChecking' parameter on the ApplicationMetrics.MetricLoggers.MetricLoggerBuffer class), however this also has the side-effect that interval metrics could become inaccurate... e.g. in the case of 2 threads being interleaved and logging methods being called in order Begin() &gt; Begin() &gt; End() &gt; End(), the interval of the inner Begin()/End() pair would be captured (which is not accurate) and one of the intervals would be discarded.  Hence logging of interval metrics on these methods is configurable.</remarks>
        public MetricLoggingConcurrentAccessManager(Boolean logQueryProcessorIntervalMetrics, IMetricLogger metricLogger)
            : base(new MetricLoggingConcurrentDirectedGraph<TUser, TGroup>(false, new MappingMetricLogger(metricLogger)))
        {
            userToApplicationComponentAndAccessLevelMappingCount = 0;
            groupToApplicationComponentAndAccessLevelMappingCount = 0;
            entityCount = 0;
            entityMappingCountsPerUser = new FrequencyTable<TUser>();
            entityMappingCountsPerGroup = new FrequencyTable<TGroup>();

            this.logQueryProcessorIntervalMetrics = logQueryProcessorIntervalMetrics;
            this.metricLogger = metricLogger;
            // Casting should never fail, since we just newed the 'userToGroupMap' and 'MetricLogger' properties to these types.
            //   TODO: Find a cleaner way to do this... ideally don't want to expose the 'MetricLoggingConcurrentDirectedGraph.MetricLogger' property at all.
            mappingMetricLogger = (MappingMetricLogger)((MetricLoggingConcurrentDirectedGraph<TUser, TGroup>)userToGroupMap).MetricLogger;
            AdMappingMetricLoggerMappings();
        }

        // TODO: Need to change location of 'InterfaceDocumentationComments.xml' in all overridden methods
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUser(`0)"]/*'/>
        public override void AddUser(TUser user)
        {
            Action<TUser, Action<TUser, Action>> addUserAction = (actionUser, baseAction) =>
            {
                base.AddUser(user, baseAction);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TUser, UserAddTime, UsersAdded>(user, addUserAction);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUser(`0)"]/*'/>
        public override void RemoveUser(TUser user)
        {
            Action<TUser, Action> wrappingAction = (actionUser, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveUser(user, wrappingAction);
            throw new NotImplementedException();
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroup(`1)"]/*'/>
        public override void AddGroup(TGroup group)
        {
            Action<TGroup, Action<TGroup, Action>> addGroupAction = (actionGroup, baseAction) =>
            {
                base.AddGroup(group, baseAction);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TGroup, GroupAddTime, GroupsAdded>(group, addGroupAction);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroup(`1)"]/*'/>
        public override void RemoveGroup(TGroup group)
        {
            Action<TGroup, Action> wrappingAction = (actionGroup, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.RemoveGroup(group, wrappingAction);
            throw new NotImplementedException();
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToGroupMapping(`0,`1)"]/*'/>
        public override void AddUserToGroupMapping(TUser user, TGroup group)
        {
            Action<TUser, TGroup, Action<TUser, TGroup, Action>> addUserToGroupMappingAction = (actionUser, actionGroup, baseAction) =>
            {
                base.AddUserToGroupMapping(user, group, baseAction);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TUser, TGroup, UserToGroupMappingAddTime, UserToGroupMappingsAdded>(user, group, addUserToGroupMappingAction);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToGroupMapping(`0,`1)"]/*'/>
        public override void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            Action<TUser, TGroup, Action<TUser, TGroup, Action>> removeUserToGroupMappingAction = (actionUser, actionGroup, baseAction) =>
            {
                base.RemoveUserToGroupMapping(user, group, baseAction);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TUser, TGroup, UserToGroupMappingRemoveTime, UserToGroupMappingsRemoved>(user, group, removeUserToGroupMappingAction);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToGroupMapping(`1,`1)"]/*'/>
        public override void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Action<TGroup, TGroup, Action<TGroup, TGroup, Action>> addGroupToGroupMappingAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                base.AddGroupToGroupMapping(fromGroup, toGroup, baseAction);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TGroup, TGroup, GroupToGroupMappingAddTime, GroupToGroupMappingsAdded>(fromGroup, toGroup, addGroupToGroupMappingAction);
        }

        public override void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            Action<TGroup, TGroup, Action<TGroup, TGroup, Action>> removeGroupToGroupMappingAction = (actionFromGroup, actionToGroup, baseAction) =>
            {
                base.RemoveGroupToGroupMapping(fromGroup, toGroup, baseAction);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TGroup, TGroup, GroupToGroupMappingRemoveTime, GroupToGroupMappingsRemoved>(fromGroup, toGroup, removeGroupToGroupMappingAction);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public override void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TUser, TComponent, TAccess, Action> wrappingAction = (actionUser, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                metricLogger.Begin(new UserToApplicationComponentAndAccessLevelMappingAddTime());
                try
                {
                    baseAction.Invoke();
                }
                catch
                {
                    metricLogger.CancelBegin(new UserToApplicationComponentAndAccessLevelMappingAddTime());
                    throw;
                }
                metricLogger.End(new UserToApplicationComponentAndAccessLevelMappingAddTime());
                metricLogger.Increment(new UserToApplicationComponentAndAccessLevelMappingsAdded());
                userToApplicationComponentAndAccessLevelMappingCount++;
                metricLogger.Set(new UserToApplicationComponentAndAccessLevelMappingsStored(), userToApplicationComponentAndAccessLevelMappingCount);
            };
            this.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, wrappingAction);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveUserToApplicationComponentAndAccessLevelMapping(`0,`2,`3)"]/*'/>
        public override void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TUser, TComponent, TAccess, Action> wrappingAction = (actionUser, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                metricLogger.Begin(new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
                try
                {
                    baseAction.Invoke();
                }
                catch
                {
                    metricLogger.CancelBegin(new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
                    throw;
                }
                metricLogger.End(new UserToApplicationComponentAndAccessLevelMappingRemoveTime());
                metricLogger.Increment(new UserToApplicationComponentAndAccessLevelMappingsRemoved());
                userToApplicationComponentAndAccessLevelMappingCount--;
                metricLogger.Set(new UserToApplicationComponentAndAccessLevelMappingsStored(), userToApplicationComponentAndAccessLevelMappingCount);
            };
            this.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel, wrappingAction);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public override void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TGroup, TComponent, TAccess, Action> wrappingAction = (actionGroup, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                metricLogger.Begin(new GroupToApplicationComponentAndAccessLevelMappingAddTime());
                try
                {
                    baseAction.Invoke();
                }
                catch
                {
                    metricLogger.CancelBegin(new GroupToApplicationComponentAndAccessLevelMappingAddTime());
                    throw;
                }
                metricLogger.End(new GroupToApplicationComponentAndAccessLevelMappingAddTime());
                metricLogger.Increment(new GroupToApplicationComponentAndAccessLevelMappingsAdded());
                groupToApplicationComponentAndAccessLevelMappingCount++;
                metricLogger.Set(new GroupToApplicationComponentAndAccessLevelMappingsStored(), groupToApplicationComponentAndAccessLevelMappingCount);
            };
            this.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, wrappingAction);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.RemoveGroupToApplicationComponentAndAccessLevelMapping(`1,`2,`3)"]/*'/>
        public override void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            Action<TGroup, TComponent, TAccess, Action> wrappingAction = (actionGroup, actionApplicationComponent, actionAccessLevel, baseAction) =>
            {
                metricLogger.Begin(new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
                try
                {
                    baseAction.Invoke();
                }
                catch
                {
                    metricLogger.CancelBegin(new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
                    throw;
                }
                metricLogger.End(new GroupToApplicationComponentAndAccessLevelMappingRemoveTime());
                metricLogger.Increment(new GroupToApplicationComponentAndAccessLevelMappingsRemoved());
                groupToApplicationComponentAndAccessLevelMappingCount--;
                metricLogger.Set(new GroupToApplicationComponentAndAccessLevelMappingsStored(), groupToApplicationComponentAndAccessLevelMappingCount);
            };
            this.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel, wrappingAction);
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddEntityType(System.String)"]/*'/>
        public override void AddEntityType(String entityType)
        {
            // TODO: 2022-06-23
            //   Set EntityTypesStored (from entities.Count)

            Action<String, Action> wrappingAction = (actionEntityType, baseAction) =>
            {
                baseAction.Invoke();
            };
            this.AddEntityType(entityType, wrappingAction);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Adds required metric class mappings to the 'mappingMetricLogger' member.
        /// </summary>
        protected void AdMappingMetricLoggerMappings()
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
                metricLogger.Begin(new TIntervalMetric());
                try
                {
                    baseAction.Invoke();
                }
                catch
                {
                    metricLogger.CancelBegin(new TIntervalMetric());
                    throw;
                }
                metricLogger.End(new TIntervalMetric());
                metricLogger.Increment(new TCountMetric());
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
                metricLogger.Begin(new TIntervalMetric());
                try
                {
                    baseAction.Invoke();
                }
                catch
                {
                    metricLogger.CancelBegin(new TIntervalMetric());
                    throw;
                }
                metricLogger.End(new TIntervalMetric());
                metricLogger.Increment(new TCountMetric());
            };
            eventProcessorMethodAction.Invoke(parameterValue1, parameterValue2, wrappingAction);
        }

        #endregion
    }
}
