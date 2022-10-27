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

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Default implementation of <see cref="IOperationGenerator"/>.
    /// </summary>
    public class DefaultOperationGenerator<TUser, TGroup, TComponent, TAccess> : IOperationGenerator
    {
        /// <summary>The data elements stored in the AccessManager instance under test.</summary>
        protected DataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer;
        /// <summary>Stores mappings of the storage structures in an AccessManager, to the operations that depend on them.</summary>
        protected Dictionary<StorageStructure, HashSet<AccessManagerOperation>> storageStructureToOperationDependencyMap;
        /// <summary>Stores mappings of the operations in an AccessManager, to the storage structures that they depend on.</summary>
        protected Dictionary<HashSet<AccessManagerOperation>, StorageStructure> operationToStorageStructureDependencyMap;
        /// <summary>All operations which retrieve data elements from an AccessManager.</summary>
        protected HashSet<AccessManagerOperation> getOperations;
        /// <summary>All operations which add data elements to an AccessManager.</summary>
        protected HashSet<AccessManagerOperation> addOperations;
        /// <summary>All operations which remove data elements from an AccessManager.</summary>
        protected HashSet<AccessManagerOperation> removeOperations;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.DefaultOperationGenerator class.
        /// </summary>
        /// <param name="dataElementStorer">The data elements stored in the AccessManager instance under test.</param>
        public DefaultOperationGenerator(DataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer)
        {
            this.dataElementStorer = dataElementStorer;
            InitializeDependencyMaps();
            InitializeOperationClassificationSets();
        }

        /// <summary>
        /// Generates an <see cref="AccessManagerOperation"/> to perform against the AccessManager instance under test.
        /// </summary>
        /// <returns>The operation to perform.</returns>
        public AccessManagerOperation Generate()
        {
            throw new NotImplementedException();
        }

        #region Private/Protected Methods

        /// <summary>
        /// Gets the current counts of items in each <see cref="StorageStructure"/> in the <see cref="DefaultOperationGenerator{TUser, TGroup, TComponent, TAccess}.dataElementStorer">'dataElementStorer'</see> member.
        /// </summary>
        /// <returns>The item counts for each <see cref="StorageStructure"/>.</returns>
        protected Dictionary<StorageStructure, Int32> GetStorageStructureCounts()
        {
            var dataElementStorerCountProperties = new List<Tuple<StorageStructure, Func<Int32>>>()
            {
                new Tuple<StorageStructure, Func<Int32>>(StorageStructure.Users, () => { return dataElementStorer.UserCount; }),
                new Tuple<StorageStructure, Func<Int32>>(StorageStructure.Groups, () => { return dataElementStorer.GroupCount; }),
                new Tuple<StorageStructure, Func<Int32>>(StorageStructure.UserToGroupMap, () => { return dataElementStorer.UserToGroupMappingCount; }),
                new Tuple<StorageStructure, Func<Int32>>(StorageStructure.GroupToGroupMap, () => { return dataElementStorer.GroupToGroupMappingCount; }),
                new Tuple<StorageStructure, Func<Int32>>(StorageStructure.UserToComponentMap, () => { return dataElementStorer.UserToComponentMappingCount; }),
                new Tuple<StorageStructure, Func<Int32>>(StorageStructure.GroupToComponentMap, () => { return dataElementStorer.GroupToComponentMappingCount; }),
                new Tuple<StorageStructure, Func<Int32>>(StorageStructure.EntityTypes,() => { return dataElementStorer.EntityTypeCount; }),
                new Tuple<StorageStructure, Func<Int32>>(StorageStructure.Entities,() => { return dataElementStorer.EntityCount; }),
                new Tuple<StorageStructure, Func<Int32>>(StorageStructure.UserToEntityMap, () => { return dataElementStorer.UserToEntityMappingCount; }),
                new Tuple<StorageStructure, Func<Int32>>(StorageStructure.GroupToEntityMap, () => { return dataElementStorer.GroupToEntityMappingCount; })
            };
            var returnDictionary = new Dictionary<StorageStructure, Int32>();
            foreach (Tuple<StorageStructure, Func<Int32>> currentCountProperty in dataElementStorerCountProperties)
            {
                returnDictionary.Add(currentCountProperty.Item1, currentCountProperty.Item2.Invoke());
            }

            return returnDictionary;
        }

        /// <summary>
        /// Calculates the base probabilities of each <see cref="AccessManagerOperation"/> being executed.
        /// </summary>
        /// <param name="storageStructureCounts">The counts of items in each <see cref="StorageStructure"/>.</param>
        /// <returns>The probability for each operation.</returns>
        /// <remarks>Operations with 0 probability will not be included in the results.</remarks>
        protected Dictionary<AccessManagerOperation, Double> CalculateBaseOperationProbabilities(Dictionary<StorageStructure, Int32> storageStructureCounts)
        {
            var returnDictionary = new Dictionary<AccessManagerOperation, Double>();

            foreach (AccessManagerOperation currentRemoveOperation in removeOperations)
            {
                var countsOfDependentStorageStructures = new Dictionary<StorageStructure, Int32>();
                Boolean dependentStorageStructureIsEmpty = false;
                foreach (StorageStructure currentDependentStorageStructure in )
            }
        }

        #region Storage Structure Initialization Methods

        /// <summary>
        /// Initializes the '*DependencyMap' members.
        /// </summary>
        protected void InitializeDependencyMaps()
        {
            // TODO: There are some 'partial' dependencies which are not included below.  E.g. AccessManagerOperation.HasAccessToApplicationComponent needs StorageStructure.UserToGroupMap and StorageStructure.Groups to exercise full functionality (i.e. traverse to groups in the user to group map)
            //   but doesn't require these group structures to be populated to call the methods.  As the intended use of this map is to decide which method to call, these partial depnedencies will be omitted.

            storageStructureToOperationDependencyMap = new Dictionary<StorageStructure, HashSet<AccessManagerOperation>>();
            // Add the storage structures
            storageStructureToOperationDependencyMap.Add(StorageStructure.Users, new HashSet<AccessManagerOperation>());
            storageStructureToOperationDependencyMap.Add(StorageStructure.Groups, new HashSet<AccessManagerOperation>());
            storageStructureToOperationDependencyMap.Add(StorageStructure.UserToGroupMap, new HashSet<AccessManagerOperation>());
            storageStructureToOperationDependencyMap.Add(StorageStructure.GroupToGroupMap, new HashSet<AccessManagerOperation>());
            storageStructureToOperationDependencyMap.Add(StorageStructure.UserToComponentMap, new HashSet<AccessManagerOperation>());
            storageStructureToOperationDependencyMap.Add(StorageStructure.GroupToComponentMap, new HashSet<AccessManagerOperation>());
            storageStructureToOperationDependencyMap.Add(StorageStructure.EntityTypes, new HashSet<AccessManagerOperation>());
            storageStructureToOperationDependencyMap.Add(StorageStructure.Entities, new HashSet<AccessManagerOperation>());
            storageStructureToOperationDependencyMap.Add(StorageStructure.UserToEntityMap, new HashSet<AccessManagerOperation>());
            storageStructureToOperationDependencyMap.Add(StorageStructure.GroupToEntityMap, new HashSet<AccessManagerOperation>());
            // Add the dependencies
            storageStructureToOperationDependencyMap[StorageStructure.Users].UnionWith(new AccessManagerOperation[] 
            {
                AccessManagerOperation.UsersPropertyGet,
                AccessManagerOperation.AddUser,
                AccessManagerOperation.ContainsUser,
                AccessManagerOperation.RemoveUser,
                AccessManagerOperation.AddUserToGroupMapping,
                AccessManagerOperation.GetUserToGroupMappings,
                AccessManagerOperation.RemoveUserToGroupMapping,
                AccessManagerOperation.AddUserToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.GetUserToApplicationComponentAndAccessLevelMappings,
                AccessManagerOperation.RemoveUserToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.AddUserToEntityMapping,
                AccessManagerOperation.GetUserToEntityMappings,
                AccessManagerOperation.GetUserToEntityMappingsEntityTypeOverload,
                AccessManagerOperation.RemoveUserToEntityMapping,
                AccessManagerOperation.HasAccessToApplicationComponent,
                AccessManagerOperation.HasAccessToEntity,
                AccessManagerOperation.GetApplicationComponentsAccessibleByUser,
                AccessManagerOperation.GetEntitiesAccessibleByUser 
            });
            storageStructureToOperationDependencyMap[StorageStructure.Groups].UnionWith(new AccessManagerOperation[] 
            { 
                AccessManagerOperation.GroupsPropertyGet,
                AccessManagerOperation.AddGroup,
                AccessManagerOperation.ContainsGroup,
                AccessManagerOperation.RemoveGroup,
                AccessManagerOperation.AddUserToGroupMapping,
                AccessManagerOperation.GetUserToGroupMappings,
                AccessManagerOperation.RemoveUserToGroupMapping,
                AccessManagerOperation.AddGroupToGroupMapping,
                AccessManagerOperation.GetGroupToGroupMappings,
                AccessManagerOperation.RemoveGroupToGroupMapping,
                AccessManagerOperation.AddGroupToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.GetGroupToApplicationComponentAndAccessLevelMappings,
                AccessManagerOperation.RemoveGroupToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.AddGroupToEntityMapping,
                AccessManagerOperation.GetGroupToEntityMappings,
                AccessManagerOperation.GetGroupToEntityMappingsEntityTypeOverload,
                AccessManagerOperation.RemoveGroupToEntityMapping,
                AccessManagerOperation.GetApplicationComponentsAccessibleByGroup,
                AccessManagerOperation.GetEntitiesAccessibleByGroup 
            });
            storageStructureToOperationDependencyMap[StorageStructure.UserToGroupMap].UnionWith(new AccessManagerOperation[] 
            { 
                AccessManagerOperation.AddUserToGroupMapping,
                AccessManagerOperation.GetUserToGroupMappings,
                AccessManagerOperation.RemoveUserToGroupMapping 
            });
            storageStructureToOperationDependencyMap[StorageStructure.GroupToGroupMap].UnionWith(new AccessManagerOperation[] 
            {
                AccessManagerOperation.AddUserToGroupMapping,
                AccessManagerOperation.GetUserToGroupMappings,
                AccessManagerOperation.RemoveUserToGroupMapping,
                AccessManagerOperation.AddGroupToGroupMapping,
                AccessManagerOperation.GetGroupToGroupMappings,
                AccessManagerOperation.RemoveGroupToGroupMapping
            });
            storageStructureToOperationDependencyMap[StorageStructure.UserToComponentMap].UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.AddUserToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.GetUserToApplicationComponentAndAccessLevelMappings,
                AccessManagerOperation.RemoveUserToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.HasAccessToApplicationComponent,
                AccessManagerOperation.GetApplicationComponentsAccessibleByUser
            });
            storageStructureToOperationDependencyMap[StorageStructure.GroupToComponentMap].UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.AddGroupToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.GetGroupToApplicationComponentAndAccessLevelMappings,
                AccessManagerOperation.RemoveGroupToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.GetApplicationComponentsAccessibleByGroup
            });
            storageStructureToOperationDependencyMap[StorageStructure.EntityTypes].UnionWith(new AccessManagerOperation[] 
            {
                AccessManagerOperation.EntityTypesPropertyGet,
                AccessManagerOperation.AddEntityType,
                AccessManagerOperation.ContainsEntityType,
                AccessManagerOperation.RemoveEntityType,
                AccessManagerOperation.AddEntity,
                AccessManagerOperation.GetEntities,
                AccessManagerOperation.ContainsEntity,
                AccessManagerOperation.RemoveEntity,
                AccessManagerOperation.AddUserToEntityMapping,
                AccessManagerOperation.GetUserToEntityMappings,
                AccessManagerOperation.GetUserToEntityMappingsEntityTypeOverload,
                AccessManagerOperation.RemoveUserToEntityMapping,
                AccessManagerOperation.AddGroupToEntityMapping,
                AccessManagerOperation.GetGroupToEntityMappings,
                AccessManagerOperation.GetGroupToEntityMappingsEntityTypeOverload,
                AccessManagerOperation.RemoveGroupToEntityMapping,
                AccessManagerOperation.HasAccessToEntity,
                AccessManagerOperation.GetEntitiesAccessibleByUser,
                AccessManagerOperation.GetEntitiesAccessibleByGroup
            });
            storageStructureToOperationDependencyMap[StorageStructure.Entities].UnionWith(new AccessManagerOperation[] 
            {
                AccessManagerOperation.AddEntity,
                AccessManagerOperation.GetEntities,
                AccessManagerOperation.ContainsEntity,
                AccessManagerOperation.RemoveEntity,
                AccessManagerOperation.AddUserToEntityMapping,
                AccessManagerOperation.GetUserToEntityMappings,
                AccessManagerOperation.GetUserToEntityMappingsEntityTypeOverload,
                AccessManagerOperation.RemoveUserToEntityMapping,
                AccessManagerOperation.AddGroupToEntityMapping,
                AccessManagerOperation.GetGroupToEntityMappings,
                AccessManagerOperation.GetGroupToEntityMappingsEntityTypeOverload,
                AccessManagerOperation.RemoveGroupToEntityMapping,
                AccessManagerOperation.HasAccessToEntity,
                AccessManagerOperation.GetEntitiesAccessibleByUser,
                AccessManagerOperation.GetEntitiesAccessibleByGroup
            });
            storageStructureToOperationDependencyMap[StorageStructure.UserToEntityMap].UnionWith(new AccessManagerOperation[] 
            { 
                AccessManagerOperation.AddUserToEntityMapping,
                AccessManagerOperation.GetUserToEntityMappings,
                AccessManagerOperation.GetUserToEntityMappingsEntityTypeOverload,
                AccessManagerOperation.RemoveUserToEntityMapping,
                AccessManagerOperation.GetEntitiesAccessibleByUser 
            });
            storageStructureToOperationDependencyMap[StorageStructure.GroupToEntityMap].UnionWith(new AccessManagerOperation[] 
            {
                AccessManagerOperation.AddGroupToEntityMapping,
                AccessManagerOperation.GetGroupToEntityMappings,
                AccessManagerOperation.GetGroupToEntityMappingsEntityTypeOverload,
                AccessManagerOperation.RemoveGroupToEntityMapping,
                AccessManagerOperation.GetEntitiesAccessibleByGroup
            });

            TODO: Generate operationToStorageStructureDependencyMap from storageStructureToOperationDependencyMap
        }

        /// <summary>
        /// Initializes the '*Operations' members which classify all <see cref="AccessManagerOperation">AccessManagerOperations</see> into different types.
        /// </summary>
        protected void InitializeOperationClassificationSets()
        {
            getOperations = new HashSet<AccessManagerOperation>();
            addOperations = new HashSet<AccessManagerOperation>();
            removeOperations = new HashSet<AccessManagerOperation>();

            getOperations.UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.UsersPropertyGet, 
                AccessManagerOperation.GroupsPropertyGet,
                AccessManagerOperation.EntityTypesPropertyGet,
                AccessManagerOperation.ContainsUser,
                AccessManagerOperation.ContainsGroup,
                AccessManagerOperation.GetUserToGroupMappings,
                AccessManagerOperation.GetGroupToGroupMappings,
                AccessManagerOperation.GetUserToApplicationComponentAndAccessLevelMappings,
                AccessManagerOperation.GetGroupToApplicationComponentAndAccessLevelMappings,
                AccessManagerOperation.ContainsEntityType,
                AccessManagerOperation.GetEntities,
                AccessManagerOperation.ContainsEntity,
                AccessManagerOperation.GetUserToEntityMappings,
                AccessManagerOperation.GetUserToEntityMappingsEntityTypeOverload,
                AccessManagerOperation.GetGroupToEntityMappings,
                AccessManagerOperation.GetGroupToEntityMappingsEntityTypeOverload,
                AccessManagerOperation.HasAccessToApplicationComponent,
                AccessManagerOperation.HasAccessToEntity,
                AccessManagerOperation.GetApplicationComponentsAccessibleByUser,
                AccessManagerOperation.GetApplicationComponentsAccessibleByGroup,
                AccessManagerOperation.GetEntitiesAccessibleByUser,
                AccessManagerOperation.GetEntitiesAccessibleByGroup
            });

            addOperations.UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.AddUser,
                AccessManagerOperation.AddGroup,
                AccessManagerOperation.AddUserToGroupMapping,
                AccessManagerOperation.AddGroupToGroupMapping,
                AccessManagerOperation.AddUserToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.AddGroupToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.AddEntityType,
                AccessManagerOperation.AddEntity,
                AccessManagerOperation.AddUserToEntityMapping,
                AccessManagerOperation.AddGroupToEntityMapping,
            });

            removeOperations.UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.RemoveUser,
                AccessManagerOperation.RemoveGroup,
                AccessManagerOperation.RemoveUserToGroupMapping,
                AccessManagerOperation.RemoveGroupToGroupMapping,
                AccessManagerOperation.RemoveUserToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.RemoveGroupToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.RemoveEntityType,
                AccessManagerOperation.RemoveEntity,
                AccessManagerOperation.RemoveUserToEntityMapping,
                AccessManagerOperation.RemoveGroupToEntityMapping
            });
        }

        #endregion

        #endregion
    }
}
