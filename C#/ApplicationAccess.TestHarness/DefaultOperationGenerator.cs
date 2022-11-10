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

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Default implementation of <see cref="IOperationGenerator"/>.
    /// </summary>
    public class DefaultOperationGenerator<TUser, TGroup, TComponent, TAccess> : IOperationGenerator
    {
        /// <summary>The data elements stored in the AccessManager instance under test.</summary>
        protected DataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer;
        /// <summary>The target elements counts for each storage structure.</summary>
        protected Dictionary<StorageStructure, Int32> targetStorageStructureCounts;
        /// <summary>The ratio of query operations (get) to event operations (add/remove).</summary>
        protected Double queryToEventOperationRatio;
        /// <summary>Stores mappings of the storage structures in an AccessManager, to the operations that depend on them being populated.</summary>
        protected Dictionary<StorageStructure, HashSet<AccessManagerOperation>> storageStructureToOperationDependencyMap;
        /// <summary>Stores mappings of the operations in an AccessManager, to the storage structures that they depend on being populated.</summary>
        protected Dictionary<AccessManagerOperation, HashSet<StorageStructure>> operationToStorageStructureDependencyMap;
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
        /// <param name="targetStorageStructureCounts">The target elements counts for each storage structure.</param>
        /// <param name="queryToEventOperationRatio">The ratio of query operations (get) to event operations (add/remove).  E.g. a value of 2.0 would make query operations twice as likely as event operations.</param>
        public DefaultOperationGenerator(DataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer, Dictionary<StorageStructure, Int32> targetStorageStructureCounts, Double queryToEventOperationRatio)
        {
            if (queryToEventOperationRatio <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(queryToEventOperationRatio), $"Parameter '{nameof(queryToEventOperationRatio)}' must be greater than 0.");

            ValidateTargetStorageStructureCounts(targetStorageStructureCounts);
            this.dataElementStorer = dataElementStorer;
            this.targetStorageStructureCounts = targetStorageStructureCounts;
            this.queryToEventOperationRatio = queryToEventOperationRatio;
            InitializeDependencyMaps();
            InitializeOperationClassificationSets();
        }

        /// <summary>
        /// Generates an <see cref="AccessManagerOperation"/> to perform against the AccessManager instance under test.
        /// </summary>
        /// <returns>The operation to perform.</returns>
        public AccessManagerOperation Generate()
        {
            Dictionary<StorageStructure, Int32> storageStructureCounts = GetStorageStructureCounts();
            Dictionary<AccessManagerOperation, Double> baseProbabilities = CalculateBaseOperationProbabilities(storageStructureCounts);
            ApplyQueryToEventOperationRatio(baseProbabilities);
            AccessManagerOperation returnOperation = ChooseOperation(baseProbabilities);

            return returnOperation;
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

            Console.WriteLine(":: Structure Counts ::");
            foreach (KeyValuePair<StorageStructure, Int32> currKvp in returnDictionary)
            {
                Console.WriteLine($"{currKvp.Key}: {currKvp.Value}");
            }
            Console.WriteLine();

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

            // Set the probability for 'remove' operations
            foreach (AccessManagerOperation currentRemoveOperation in removeOperations)
            {
                Boolean dependentStorageStructureIsEmpty = false;
                Double currentOperationProbability = 0.0;
                // Average out the probability for each of the dependent structures
                foreach (StorageStructure currentDependentStorageStructure in operationToStorageStructureDependencyMap[currentRemoveOperation])
                {
                    if (storageStructureCounts[currentDependentStorageStructure] == 0)
                    {
                        // If any one of the dependent structures has 0 elements, set the probability to 0
                        dependentStorageStructureIsEmpty = true;
                        break;
                    }
                    else
                    {
                        // Probability is (current count) / (2 * target count)
                        currentOperationProbability += Convert.ToDouble(storageStructureCounts[currentDependentStorageStructure]) / (2.0 * Convert.ToDouble(targetStorageStructureCounts[currentDependentStorageStructure]));
                    }
                }
                if (dependentStorageStructureIsEmpty == true)
                {
                    returnDictionary[currentRemoveOperation] = 0.0;
                }
                else
                {
                    returnDictionary[currentRemoveOperation] = currentOperationProbability / Convert.ToDouble(operationToStorageStructureDependencyMap[currentRemoveOperation].Count);
                }
            }

            // Set the proability for 'add' and 'get' operations (inverse of the equivalent 'remove' operation)
            foreach (IEnumerable<AccessManagerOperation> currentOperationCollection in new HashSet<AccessManagerOperation>[] { addOperations, getOperations })
            {
                foreach (AccessManagerOperation currentOperation in currentOperationCollection)
                {
                    if (operationToStorageStructureDependencyMap.ContainsKey(currentOperation) == false)
                    {
                        // Some Add* operations don't have any dependencies, so set their probability to 1
                        returnDictionary[currentOperation] = 1.0;
                    }
                    else
                    {
                        Boolean dependentStorageStructureIsEmpty = false;
                        Double currentOperationProbability = 0.0;
                        // Average out the probability for each of the dependent structures
                        foreach (StorageStructure currentDependentStorageStructure in operationToStorageStructureDependencyMap[currentOperation])
                        {
                            if (storageStructureCounts[currentDependentStorageStructure] == 0)
                            {
                                // If any one of the dependent structures has 0 elements, set the probability to 1
                                dependentStorageStructureIsEmpty = true;
                                break;
                            }
                            else
                            {
                                currentOperationProbability += Convert.ToDouble(storageStructureCounts[currentDependentStorageStructure]) / (2.0 * Convert.ToDouble(targetStorageStructureCounts[currentDependentStorageStructure]));
                            }
                        }
                        if (dependentStorageStructureIsEmpty == true)
                        {
                            returnDictionary[currentOperation] = 0.0;
                        }
                        else
                        {
                            returnDictionary[currentOperation] = 1.0 - (currentOperationProbability / Convert.ToDouble(operationToStorageStructureDependencyMap[currentOperation].Count));
                        }
                    }
                }
            }

            Console.WriteLine("-- Base Probabilities --");
            foreach (KeyValuePair<AccessManagerOperation, Double> currKvp in returnDictionary)
            {
                Console.WriteLine($"{currKvp.Key}: {currKvp.Value}");
            }

            return returnDictionary;
        }

        /// <summary>
        /// Applies the query to event operation ratio to the specified set of operation probabilities.
        /// </summary>
        /// <param name="operationProbabilities">The relative probabilities of each <see cref="AccessManagerOperation"/> being executed.</param>
        protected void ApplyQueryToEventOperationRatio(Dictionary<AccessManagerOperation, Double> operationProbabilities)
        {
            // Sum the event operations vs query operations
            Double queryOperationsTotalProbabililty = 0.0;
            Double eventOperationsTotalProbabililty = 0.0;
            foreach (KeyValuePair<AccessManagerOperation, Double> currentKvp in operationProbabilities)
            {
                if (getOperations.Contains(currentKvp.Key) == true)
                {
                    queryOperationsTotalProbabililty += currentKvp.Value;
                }
                else
                {
                    eventOperationsTotalProbabililty += currentKvp.Value;
                }
            }
            // If probabilities are 0 for query operations (e.g. if all data structures are empty) or event operations, don't need to apply any scaling
            if (!(queryOperationsTotalProbabililty == 0.0 || eventOperationsTotalProbabililty == 0.0))
            {
                Double currentQueryToEventOperationRatio = queryOperationsTotalProbabililty / eventOperationsTotalProbabililty;
                Double requiredScaleFactor = queryToEventOperationRatio / currentQueryToEventOperationRatio;
                foreach (AccessManagerOperation currentQueryOperation in getOperations)
                {
                    if (operationProbabilities.ContainsKey(currentQueryOperation) == true)
                    {
                        operationProbabilities[currentQueryOperation] *= requiredScaleFactor;
                    }
                }
            }
        }

        /// <summary>
        /// Validates the specified set of target elements counts for each storage structure.
        /// </summary>
        /// <param name="targetStorageStructureCounts">The target elements counts to validate.</param>
        protected void ValidateTargetStorageStructureCounts(Dictionary<StorageStructure, Int32> targetStorageStructureCounts)
        {
            foreach (Int32 currentStorageStructureValue in Enum.GetValues(typeof(StorageStructure)))
            {
                if (targetStorageStructureCounts.ContainsKey((StorageStructure)currentStorageStructureValue) == false)
                    throw new ArgumentException($"Parameter '{nameof(targetStorageStructureCounts)}' does not contain a target storage structure count for '{(StorageStructure)currentStorageStructureValue}'.", nameof(targetStorageStructureCounts));

                if (targetStorageStructureCounts[(StorageStructure)currentStorageStructureValue] < 1)
                    throw new ArgumentOutOfRangeException(nameof(targetStorageStructureCounts), $"Parameter '{nameof(targetStorageStructureCounts)}' with count {targetStorageStructureCounts[(StorageStructure)currentStorageStructureValue]} for storage structure '{(StorageStructure)currentStorageStructureValue}' cannot be less than 1.");
            }
        }

        /// <summary>
        /// Randomly chooses an <see cref="AccessManagerOperation"/> based on the specified probabilities.
        /// </summary>
        /// <param name="operationProbabilities">The relative probabilities of each <see cref="AccessManagerOperation"/>.</param>
        /// <returns>A randomly chosen  <see cref="AccessManagerOperation"/>.</returns>
        protected AccessManagerOperation ChooseOperation(Dictionary<AccessManagerOperation, Double> operationProbabilities)
        {
            var randomGenerator = new WeightedRandomGenerator<AccessManagerOperation>();
            var weightings = new List<Tuple<AccessManagerOperation, Int64>>();
            
            // Get the total of all relative probabilities
            Double totalProbability = 0.0;
            foreach (Double currentProbability in operationProbabilities.Values)
            {
                totalProbability += currentProbability;
            }

            // Calculate and set the weightings
            Int64 weightingsTotal = Int64.MaxValue / 2; // Using Int64.MaxValue / 2 avoids possibility of multiple roundings up pushing the total into overflow, as could happen with Int64.MaxValue

            foreach (KeyValuePair<AccessManagerOperation, Double> currentKvp in operationProbabilities)
            {
                if (currentKvp.Value != 0.0)
                {
                    Int64 currentWeighting = Convert.ToInt64(Math.Round((currentKvp.Value / totalProbability) * weightingsTotal));
                    weightings.Add(new Tuple<AccessManagerOperation, Int64>(currentKvp.Key, currentWeighting));
                }
            }
            randomGenerator.SetWeightings(weightings);

            return randomGenerator.Generate();
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
                AccessManagerOperation.GetUserToGroupMappings,
                AccessManagerOperation.RemoveUserToGroupMapping
            });
            storageStructureToOperationDependencyMap[StorageStructure.GroupToGroupMap].UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.GetGroupToGroupMappings,
                AccessManagerOperation.RemoveGroupToGroupMapping
            });
            storageStructureToOperationDependencyMap[StorageStructure.UserToComponentMap].UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.GetUserToApplicationComponentAndAccessLevelMappings,
                AccessManagerOperation.RemoveUserToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.HasAccessToApplicationComponent,
                AccessManagerOperation.GetApplicationComponentsAccessibleByUser
            });
            storageStructureToOperationDependencyMap[StorageStructure.GroupToComponentMap].UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.GetGroupToApplicationComponentAndAccessLevelMappings,
                AccessManagerOperation.RemoveGroupToApplicationComponentAndAccessLevelMapping,
                AccessManagerOperation.GetApplicationComponentsAccessibleByGroup
            });
            storageStructureToOperationDependencyMap[StorageStructure.EntityTypes].UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.EntityTypesPropertyGet,
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
                AccessManagerOperation.GetUserToEntityMappings,
                AccessManagerOperation.GetUserToEntityMappingsEntityTypeOverload,
                AccessManagerOperation.RemoveUserToEntityMapping,
                AccessManagerOperation.GetEntitiesAccessibleByUser, 
                AccessManagerOperation.HasAccessToEntity
            });
            storageStructureToOperationDependencyMap[StorageStructure.GroupToEntityMap].UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.GetGroupToEntityMappings,
                AccessManagerOperation.GetGroupToEntityMappingsEntityTypeOverload,
                AccessManagerOperation.RemoveGroupToEntityMapping,
                AccessManagerOperation.GetEntitiesAccessibleByGroup
            });
            // Add reversed despendencies to 'operationToStorageStructureDependencyMap'
            operationToStorageStructureDependencyMap = new Dictionary<AccessManagerOperation, HashSet<StorageStructure>>();
            foreach (KeyValuePair<StorageStructure, HashSet<AccessManagerOperation>> currentMapping in storageStructureToOperationDependencyMap)
            {
                foreach (AccessManagerOperation currentOperation in currentMapping.Value)
                {
                    if (operationToStorageStructureDependencyMap.ContainsKey(currentOperation) == false)
                    {
                        operationToStorageStructureDependencyMap.Add(currentOperation, new HashSet<StorageStructure>());
                    }
                    operationToStorageStructureDependencyMap[currentOperation].Add(currentMapping.Key);
                }
            }
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
