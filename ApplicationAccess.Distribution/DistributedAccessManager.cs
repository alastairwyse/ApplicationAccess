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
                            // Ignore and continue if 'currentGroup' doesn't exist in the group
                        }
                    }
                }

                return returnGroups;
            };

            return metricLoggingWrapper.GetGroupToGroupMappings(groups, getGroupToGroupMappingsFunc);
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
    }
}
