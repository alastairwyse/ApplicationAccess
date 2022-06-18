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
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.MetricLoggingConcurrentAccessManager class.
        /// </summary>
        /// <param name="metricLogger">The logger for metrics.</param>
        public MetricLoggingConcurrentAccessManager(IMetricLogger metricLogger)
            : base()
        {
            this.metricLogger = metricLogger;
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.MetricLoggingConcurrentAccessManager class.
        /// </summary>
        /// <param name="collectionFactory">A mock collection factory.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public MetricLoggingConcurrentAccessManager(ICollectionFactory collectionFactory, IMetricLogger metricLogger)
            : base(collectionFactory)
        {
            this.metricLogger = metricLogger;
        }

        // TODO: Need to change location of 'InterfaceDocumentationComments.xml' in all overridden methods
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerEventProcessor`4.AddUser(`0)"]/*'/>
        public override void AddUser(TUser user)
        {
            /*
            Action<TUser, Action> wrappingAction = (actionUser, baseAction) =>
            {
                metricLogger.Begin(new UserAddTime());
                try
                {
                    baseAction.Invoke();
                }
                catch
                {
                    metricLogger.CancelBegin(new UserAddTime());
                    throw;
                }
                metricLogger.End(new UserAddTime());
                metricLogger.Increment(new UsersAdded());
            };
            base.AddUser(user, wrappingAction);
            */

            Action<TUser, Action<TUser, Action>> thisAction = (actionUser, baseAction) =>
            {
                base.AddUser(user, baseAction);
            };
            CallBaseClassEventProcessingMethodWithMetricLogging<TUser, UserAddTime, UsersAdded>(user, thisAction);
        }

        // 2022-06-18 TODO:
        //   Check below variables, type names and comments
        //     Finish off the comment for 'eventProcessorMethodAction' param
        //   Include a comment about how wrapping and re-wrapping of actions is getting a bit complex...make sure the xml comments are really clear as to what the method's doing and what all the params are
        //     Should have comments for all the generic types which shoudl help a lot
        //   Implement for all IAccessManagerEventProcessor methods
        //   Will need to add this constructor param for whether I want to log query processor interval metrics or not
        //     Will need a remark warning that turning param true might cause inaccuracy of interval metrics when concurrent calls are received... and explain why
        //   Add query processor methods
        //     Will probably have to make AccessManager query methods virtual


        /// <summary>
        /// Calls one of the base class methods which implements IAccessManagerEventProcessor, wrapping the call with logging of metric events of the specified types.
        /// </summary>
        /// <typeparam name="TEventProcessorMethodParam">The type of the parameter which is passed to the IAccessManagerEventProcessor method.</typeparam>
        /// <typeparam name="TIntervalMetric">The type of interval metric to log.</typeparam>
        /// <typeparam name="TCountMetric">The type of count metric to log.</typeparam>
        /// <param name="parameterValue">The value of the parameter which is passed to the IAccessManagerEventProcessor method.</param>
        /// <param name="eventProcessorMethodAction">Action which calls the IAccessManagerEventProcessor method.  Accepts 2 parameters: the type of the parameter which is passed to the IAccessManagerEventProcessor method, and an inner action **TODO**</param>
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
    }
}
