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
using ApplicationAccess.Metrics;
using ApplicationMetrics;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Subclass of <see cref="MetricLoggingDependencyFreeAccessManager{TUser, TGroup, TComponent, TAccess}"/> which implements additional methods defined on interface <see cref="IDistributedAccessManager{TUser, TGroup, TComponent, TAccess}"/> to support an implementation where responsibility for subsets of elements stored by the AccessManager is distributed across multiple computers.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class DistributedAccessManager<TUser, TGroup, TComponent, TAccess> : MetricLoggingDependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess>, IDistributedAccessManager<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.DistributedAccessManager class.
        /// </summary>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DistributedAccessManager(IMetricLogger metricLogger)
            : base(metricLogger)
        {
        }

        /// <inheritdoc/>
        public HashSet<TUser> GetGroupToUserMappings(IEnumerable<TGroup> groups)
        {
            Func<IEnumerable<TGroup>, HashSet<TUser>> getGroupToUserMappingsFunc = (IEnumerable<TGroup> funcGroups) =>
            {
                var returnUsers = new HashSet<TUser>();
                foreach (TGroup currentGroup in funcGroups)
                {
                    try
                    {
                        foreach (TUser currentUser in userToGroupMap.GetLeafReverseEdges(currentGroup))
                        {
                            if (returnUsers.Contains(currentUser) == false)
                            {
                                returnUsers.Add(currentUser);
                            }
                        }
                    }
                    catch (NonLeafVertexNotFoundException<TGroup>)
                    {
                        // Ignore and continue if 'currentGroup' doesn't exist in the map
                    }
                }

                return returnUsers;
            };

           return metricLoggingWrapper.GetGroupToUserMappings(groups, getGroupToUserMappingsFunc);
        }

        /// <inheritdoc/>
        public virtual HashSet<TGroup> GetGroupToGroupMappings(IEnumerable<TGroup> groups)
        {
            Func<IEnumerable<TGroup>, HashSet<TGroup>> getGroupToGroupMappingsFunc = (IEnumerable<TGroup> funcGroups) =>
            {
                var returnGroups = new HashSet<TGroup>();
                foreach (TGroup currentGroup in funcGroups)
                {
                    if (returnGroups.Contains(currentGroup) == false)
                    {
                        Func<TGroup, Boolean> vertexAction = (TGroup currentTraversalGroup) =>
                        {
                            if ((returnGroups.Contains(currentTraversalGroup) == false))
                            {
                                returnGroups.Add(currentTraversalGroup);
                            }

                            return true;
                        };
                        try
                        {
                            userToGroupMap.TraverseFromNonLeaf(currentGroup, vertexAction);
                        }
                        catch (NonLeafVertexNotFoundException<TGroup>)
                        {
                            // Ignore and continue if 'currentGroup' doesn't exist in the map
                        }
                    }
                }

                return returnGroups;
            };

            return metricLoggingWrapper.GetGroupToGroupMappings(groups, getGroupToGroupMappingsFunc);
        }

        /// <inheritdoc/>
        public HashSet<TGroup> GetGroupToGroupReverseMappings(IEnumerable<TGroup> groups)
        {
            Func<IEnumerable<TGroup>, HashSet<TGroup>> getGroupToGroupReverseMappingsFunc = (IEnumerable<TGroup> funcGroups) =>
            {
                var returnGroups = new HashSet<TGroup>();
                foreach (TGroup currentGroup in funcGroups)
                {
                    if (returnGroups.Contains(currentGroup) == false)
                    {
                        Func<TGroup, Boolean> noneLeafvertexAction = (TGroup currentTraversalGroup) =>
                        {
                            if ((returnGroups.Contains(currentTraversalGroup) == false))
                            {
                                returnGroups.Add(currentTraversalGroup);
                            }

                            return true;
                        }; 
                        Func<TUser, Boolean> leafvertexAction = (TUser currentTraversalUser) =>
                        {
                            return true;
                        };
                        try
                        {
                            userToGroupMap.TraverseReverseFromNonLeaf(currentGroup, noneLeafvertexAction, leafvertexAction);
                        }
                        catch (NonLeafVertexNotFoundException<TGroup>)
                        {
                            // Ignore and continue if 'currentGroup' doesn't exist in the map
                        }
                    }
                }

                return returnGroups;
            };

            return metricLoggingWrapper.GetGroupToGroupReverseMappings(groups, getGroupToGroupReverseMappingsFunc);
        }

        /// <inheritdoc/>
        public virtual Boolean HasAccessToApplicationComponent(IEnumerable<TGroup> groups, TComponent applicationComponent, TAccess accessLevel)
        {
            Func<IEnumerable<TGroup>, TComponent, TAccess, Boolean> hasAccessToApplicationComponentFunc = 
            (
                IEnumerable<TGroup> funcGroups, 
                TComponent funcApplicationComponent, 
                TAccess funcAccessLevel
            ) =>
            {
                var comparisonComponentAndAccess = new ApplicationComponentAndAccessLevel<TComponent, TAccess>(funcApplicationComponent, funcAccessLevel);
                foreach (TGroup currentGroup in funcGroups)
                {
                    Boolean containsGroup = groupToComponentMap.TryGetValue(currentGroup, out ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>> componentsAndAccessInMapping);
                    if (containsGroup == true && componentsAndAccessInMapping.Contains(comparisonComponentAndAccess) == true)
                    {
                        return true;
                    }
                }

                return false;
            };

            return metricLoggingWrapper.HasAccessToApplicationComponent(groups, applicationComponent, accessLevel, hasAccessToApplicationComponentFunc);
        }

        /// <inheritdoc/>
        public virtual Boolean HasAccessToEntity(IEnumerable<TGroup> groups, String entityType, String entity)
        {
            Func<IEnumerable<TGroup>, String, String, Boolean> hasAccessToEntityFunc =
            (
                IEnumerable<TGroup> funcGroups,
                String funcEntityType,
                String funcEntity
            ) =>
            {
                foreach (TGroup currentGroup in funcGroups)
                {
                    Boolean containsGroup = groupToEntityMap.TryGetValue(currentGroup, out IDictionary<String, ISet<String>> entitiesAndTypesInMapping);
                    if (containsGroup == true)
                    {
                        Boolean containsEntity = entitiesAndTypesInMapping.TryGetValue(funcEntityType, out ISet<String> entitiesInMapping);
                        if (containsEntity == true && entitiesInMapping.Contains(funcEntity) == true)
                        {
                            return true;
                        }
                    }
                }

                return false;
            };

            return metricLoggingWrapper.HasAccessToEntity(groups, entityType, entity, hasAccessToEntityFunc);
        }


        /// <inheritdoc/>
        public virtual HashSet<Tuple<TComponent, TAccess>> GetApplicationComponentsAccessibleByGroups(IEnumerable<TGroup> groups)
        {
            Func<IEnumerable<TGroup>, HashSet<Tuple<TComponent, TAccess>>> getApplicationComponentsAccessibleByGroupsFunc =
            (
                IEnumerable<TGroup> funcGroups
            ) =>
            {
                var returnComponents = new HashSet<ApplicationComponentAndAccessLevel<TComponent, TAccess>>();
                foreach (TGroup currentGroup in funcGroups)
                {
                    Boolean containsGroup = groupToComponentMap.TryGetValue(currentGroup, out ISet<ApplicationComponentAndAccessLevel<TComponent, TAccess>> currentComponents);
                    if (containsGroup == true)
                    {
                        returnComponents.UnionWith(currentComponents);
                    }
                }
                var returnHashSet = new HashSet<Tuple<TComponent, TAccess>>();
                foreach (ApplicationComponentAndAccessLevel<TComponent, TAccess> currentApplicationComponentAndAccessLevel in returnComponents)
                {
                    returnHashSet.Add(new Tuple<TComponent, TAccess>(currentApplicationComponentAndAccessLevel.ApplicationComponent, currentApplicationComponentAndAccessLevel.AccessLevel));
                }

                return returnHashSet;
            };

            return metricLoggingWrapper.GetApplicationComponentsAccessibleByGroups(groups, getApplicationComponentsAccessibleByGroupsFunc);
        }

        /// <inheritdoc/>
        public virtual HashSet<Tuple<String, String>> GetEntitiesAccessibleByGroups(IEnumerable<TGroup> groups)
        {
            Func<IEnumerable<TGroup>, HashSet<Tuple<String, String>>> getEntitiesAccessibleByGroupsFunc =(IEnumerable<TGroup> funcGroups) =>
            {
                var returnEntities = new HashSet<Tuple<String, String>>();
                foreach (TGroup currentGroup in funcGroups)
                {
                    Boolean containsGroup = groupToEntityMap.TryGetValue(currentGroup, out IDictionary<String, ISet<String>> currentEntityType);
                    if (containsGroup == true)
                    {
                        foreach(KeyValuePair<String, ISet<String>> currentKvp in currentEntityType)
                        {
                            foreach (String currentEntity in currentKvp.Value)
                            {
                                var currentEntityTypeAndEntity = new Tuple<String, String>(currentKvp.Key, currentEntity);
                                if (returnEntities.Contains(currentEntityTypeAndEntity) == false)
                                {
                                    returnEntities.Add(currentEntityTypeAndEntity);
                                }
                            }
                        }
                    }
                }

                return returnEntities;
            };

            return metricLoggingWrapper.GetEntitiesAccessibleByGroups(groups, getEntitiesAccessibleByGroupsFunc);
        }

        /// <inheritdoc/>
        public virtual HashSet<String> GetEntitiesAccessibleByGroups(IEnumerable<TGroup> groups, String entityType)
        {
            Func<IEnumerable<TGroup>, String, HashSet<String>> getEntitiesAccessibleByGroupsFunc = (IEnumerable<TGroup> funcGroups, String funcEntityType) =>
            {
                var returnEntities = new HashSet<String>();
                foreach (TGroup currentGroup in funcGroups)
                {
                    Boolean containsGroup = groupToEntityMap.TryGetValue(currentGroup, out IDictionary<String, ISet<String>> currentEntityType);
                    if (containsGroup == true)
                    {
                        Boolean containsEntityType = currentEntityType.TryGetValue(entityType, out ISet<String> currentEntities);
                        if (containsEntityType == true)
                        {
                            returnEntities.UnionWith(currentEntities);
                        }
                    }
                }

                return returnEntities;
            };

            return metricLoggingWrapper.GetEntitiesAccessibleByGroups(groups, entityType, getEntitiesAccessibleByGroupsFunc);
        }


        #region Private/Protected Methods

        #region Base Class Overrides

        /// <inheritdoc/>
        protected override void RemoveEntityType(String entityType, Action<String, Action> wrappingAction)
        {
            // We override this method so that we can generate prepended 'remove entity' events before we start logging metrics.
            //   Reasoning is explained in comment for method MetricLoggingDependencyFreeAccessManager.RemoveEntityType(String entityType, Action<String, Action> wrappingAction, Boolean logMetrics).
            // This is implemented here by...
            //   1. Creating a 'wrappingAction' lambda (i.e. below 'prependedEventRemoveAction' Action) which we pass to the base class which...
            //      a. Generates prepended 'remove entity' events
            //      b. Then calls the metric logging functionality in base class GenerateRemoveEntityTypeMetricLoggingWrappingAction() method
            //   2. Passing this 'wrappingAction' parameter to a special implementation of base class RemoveEntityType() method which disables metric logging there (we don't want to double log
            //        the metrics).
            // Strictly speaking a more correct/clean implementation of this would be to let DistributedAccessManager derive directly from DependencyFreeAccessManager, and then create a 
            //   MetricLoggingDistributedAccessManager by using ConcurrentAccessManagerMetricLogger (same as is done for DependencyFreeAccessManager and MetricLoggingDependencyFreeAccessManager,
            //   and ConcurrentAccessManager and MetricLoggingConcurrentAccessManager)... the prepended 'remove entity' could then be implemented in
            //   MetricLoggingDistributedAccessManager.RemoveEntityType() inline with the metric logging, without having to implement the slightly 'dirty' disabling of base class functionality
            //   which we're doing here.  However, since it's expected no further prepended 'remove' event generation will be required (since deciding to implement shard group merging via merged
            //   event streams rather than merging in-place into an existing persistent store), this technique shouldn't need to be extended any further.

            Action<String, Action> prependedEventRemoveAction = (actionEntityType, baseAction) =>
            {
                RemoveEntitiesForType(entityType);

                Action<String, Action> metricLoggingAction = metricLoggingWrapper.GenerateRemoveEntityTypeMetricLoggingWrappingAction(entityType, wrappingAction, entities, base.RemoveEntityType);

                metricLoggingAction.Invoke(actionEntityType, () => { baseAction.Invoke(); });
            };

            base.RemoveEntityType(entityType, prependedEventRemoveAction, false);
        }

        #endregion

        #region Secondary 'Remove' Methods Supporting Optional Event Generation

        /// <summary>
        /// Idempotently removes an entity.
        /// </summary>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity to remove.</param>
        /// <param name="generateEvent">Whether to write an event to the 'eventProcessor' member.</param>
        /// <remarks>This method is designed to be called from the <see cref="DistributedAccessManager{TUser, TGroup, TComponent, TAccess}.RemoveEntityType(String, Action{String, Action})">RemoveEntityType()</see> method as part of prepended secondary remove element event generation.  The method should only be called if mutual exclusion locks for RemoveEntityType() have already been set.</remarks>
        protected virtual void RemoveEntity(String entityType, String entity, Boolean generateEvent)
        {
            if (entities.ContainsKey(entityType) == true && entities[entityType].Contains(entity) == true)
            {
                if (generateEvent == true)
                {
                    // TODO: Gets a bit confusing here with multiple wrapping actions.
                    //   Basically we're following a similar pattern to methods like MetricLoggingDependencyFreeAccessManager.AddEntity() where we get the metric logging wrapping action
                    //   from ConcurrentAccessManagerMetricLogger, pass it a wrapping action which doesn't wrap anything, and then invoke that metric logging wrapping action passing
                    //   an inner action which just calls the base level (i.e. AccessManagerBase) RemoveEntity() method (or equivalent).
                    // The 'wrapping action which doesn't wrap anything' mentioned above is because we don't actually want to add any wrapping functionality in this case... we simply
                    //   allow metrics to be 'Begun()' call the base action to do the entity delete, finish the metrics, and be done.  The event generation is done outside of the 
                    //   wrapping action (although this is simply adding an event to a buffer/queue, so somewhat inconsequential as to whether it's in the wrapping action or not).
                    // Would be nice to simplify all this.  Basically the pattern with wrapping actions estabilshed in ConcurrentAccessManager was quite nice and clean once you got
                    //   your head around it.  However since introducing ConcurrentAccessManagerMetricLogger, it's made things a bit hard to understand in terms of what is wrapping what
                    //   (without debugging the code).  ConcurrentAccessManagerMetricLogger gave a big benefit in terms of being able to introduce consistent metric logging for 
                    //   multiple subclasses of ConcurrentAccessManager (and hence avoid a lot of repeated code, whilst allowing us to preserve a clean inheritance hierarchy from
                    //   ConcurrentAccessManager), but came at the cost of increased complexity (esp in this case after coming back to this functionality after a couple of years
                    //   and having to re-familiarize myself).
                    Action<String, String, Action> metricLoggingWrappingAction = metricLoggingWrapper.GenerateRemoveEntityMetricLoggingWrappingAction
                    (
                        entityType, 
                        entity,
                        (String wrappingActionEntityType, String wrappingActionEntity, Action wrappingActionBaseAction) => { wrappingActionBaseAction.Invoke(); }, 
                        base.RemoveEntity
                    );
                    metricLoggingWrappingAction.Invoke(entityType, entity, () => 
                    {
                        RemoveEntity(entityType, entity, (actionUser, actionEntityType, actionEntity) => { }, (actionGroup, actionEntityType, actionEntity) => { });
                    });
                    eventProcessor.RemoveEntity(entityType, entity);
                }
                else
                {
                    throw new NotImplementedException();
                }
            }
        }

        #endregion

        /// <summary>
        /// Removes all entities of the specified type.
        /// </summary>
        /// <param name="entityType">The type of the entities to remove.</param>
        protected void RemoveEntitiesForType(String entityType)
        {
            if (entities.ContainsKey(entityType) == true)
            {
                var removeEntities = new List<String>();
                ISet<String> removeEntitiesSet = entities[entityType];
                foreach (String currentEntity in removeEntitiesSet)
                {
                    removeEntities.Add(currentEntity);
                }
                foreach (String currentEntity in removeEntities)
                {
                    RemoveEntity(entityType, currentEntity, true);
                }
            }
        }

        #endregion
    }
}
