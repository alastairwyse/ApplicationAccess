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
using MathNet.Numerics.Distributions;

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
        /// <summary>The available data element counter for application componenets.</summary>
        protected IAvailableDataElementCounter<TComponent> componentAvailableDataElementCounter;
        /// <summary>The available data element counter for access levels.</summary>
        protected IAvailableDataElementCounter<TAccess> accessLeveAvailableDataElementCounter;
        /// <summary>The ratio of query operations (get) to event operations (add/remove).</summary>
        protected Double queryToEventOperationRatio;
        /// <summary>Used to calculate the base probabilities for 'secondary' event operations (e.g. AddUserToGroupMapping())</summary>
        protected ISecondaryEventOperationBaseProbabilityCalculator secondaryEventOperationBaseProbabilityCalculator;
        /// <summary>Maps AccessManager operations to the primary storage structure which holds elements for that operation.</summary>
        protected Dictionary<AccessManagerOperation, StorageStructure> operationToPrimaryStorageStructureMap;
        /// <summary>Maps AccessManager operations to the primary storage structures which have a secondary dependence for the operation (e.g. as the <see cref="StorageStructure.Users"/> structure is for <see cref="AccessManagerOperation.AddUserToEntityMapping"/>).</summary>
        protected Dictionary<AccessManagerOperation, HashSet<StorageStructure>> operationToSecondaryStorageStructureMap;
        /// <summary>Maps AccessManager query operations to storage structures which must be populated in order to perform the query operation.</summary>
        protected Dictionary<AccessManagerOperation, HashSet<StorageStructure>> queryOperationToDependentStorageStructureMap;
        /// <summary>AccessManager query operations which are either object property 'gets' or Contains*() methods.</summary>
        protected HashSet<AccessManagerOperation> propertyAndContainsQueryOperations;
        /// <summary>AccessManager Add*() operations which add fundamental elements with no dependencies.</summary>
        protected HashSet<AccessManagerOperation> primaryAddOperations;
        /// <summary>AccessManager Add*() operations which add fundamental elements with no dependencies and are scaled by a beta function.</summary>
        protected HashSet<AccessManagerOperation> betaScaledAddOperations;
        /// <summary>Beta scaling parameters for AccessManager Add*() operations which are scaled by a beta function.</summary>
        protected Dictionary<AccessManagerOperation, BetaScalingParameters> betaScaledAddOperationParameters;
        /// <summary>AccessManager Add*() operations which add secondary elements with dependencies on other add operations having to be performed before these can be performed.</summary>
        protected HashSet<AccessManagerOperation> secondaryAddOperations;
        /// <summary>AccessManager event operations, and their inverse/opposite operation.</summary>
        protected Dictionary<AccessManagerOperation, AccessManagerOperation> inverseEventOperationMap;
        /// <summary>AccessManager Remove*() operations.</summary>
        protected HashSet<AccessManagerOperation> removeOperations;
        /// <summary>AccessManager Get*() and Has*() operations.</summary>
        protected HashSet<AccessManagerOperation> getAndHasOperations;
        /// <summary>AccessManager query operations.</summary>
        protected HashSet<AccessManagerOperation> queryOperations;
        /// <summary>The number of calls to the Generate() method.</summary>
        protected Int64 generationCounter;
        /// <summary>How often counts of items in the 'dataElementStorer' member should be printed to the console (e.g. a value of 1000 would print once every 1000 calls to the Generate() method).</summary>
        protected Int32 dataElementStorerCountPrintFrequency;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.DefaultOperationGenerator class.
        /// </summary>
        /// <param name="dataElementStorer">The data elements stored in the AccessManager instance under test.</param>
        /// <param name="targetStorageStructureCounts">The target elements counts for each storage structure.</param>
        /// <param name="componentAvailableDataElementCounter">The available data element counter for application componenets.</param>
        /// <param name="accessLeveAvailableDataElementCounter">The available data element counter for access levels.</param>
        /// <param name="queryToEventOperationRatio">The ratio of query operations (get) to event operations (add/remove).  E.g. a value of 2.0 would make query operations twice as likely as event operations.</param>
        /// <param name="dataElementStorerCountPrintFrequency">>How often counts of items in the 'dataElementStorer' member should be printed to the console (e.g. a value of 1000 would print once every 1000 calls to the Generate() method).</param>
        public DefaultOperationGenerator
        (
            DataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer, 
            Dictionary<StorageStructure, Int32> targetStorageStructureCounts,
            IAvailableDataElementCounter<TComponent> componentAvailableDataElementCounter,
            IAvailableDataElementCounter<TAccess> accessLeveAvailableDataElementCounter, 
            Double queryToEventOperationRatio,
            Int32 dataElementStorerCountPrintFrequency
        )
        {
            if (queryToEventOperationRatio <= 0.0)
                throw new ArgumentOutOfRangeException(nameof(queryToEventOperationRatio), $"Parameter '{nameof(queryToEventOperationRatio)}' must be greater than 0.");
            if (dataElementStorerCountPrintFrequency < 0)
                throw new ArgumentOutOfRangeException(nameof(dataElementStorerCountPrintFrequency), $"Parameter '{nameof(dataElementStorerCountPrintFrequency)}' must be greater than 0.");

            ValidateTargetStorageStructureCounts(targetStorageStructureCounts);
            this.dataElementStorer = dataElementStorer;
            this.targetStorageStructureCounts = targetStorageStructureCounts;
            this.componentAvailableDataElementCounter = componentAvailableDataElementCounter;
            this.accessLeveAvailableDataElementCounter = accessLeveAvailableDataElementCounter;
            this.queryToEventOperationRatio = queryToEventOperationRatio;
            generationCounter = 0;
            this.dataElementStorerCountPrintFrequency = dataElementStorerCountPrintFrequency;
            InitializeDependencyMaps();
            InitializeOperationClassificationSets();
            secondaryEventOperationBaseProbabilityCalculator = new PrimaryDataStructureRatioBaseProbabilityCalculator
            (
                secondaryAddOperations, 
                this.targetStorageStructureCounts, 
                operationToPrimaryStorageStructureMap, 
                operationToSecondaryStorageStructureMap, 
                inverseEventOperationMap
            );
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
            generationCounter++;
            if (dataElementStorerCountPrintFrequency > 0 && generationCounter % dataElementStorerCountPrintFrequency == 0)
            {
                PrintDataElementStorerCounts(dataElementStorer);
            }

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
                new Tuple<StorageStructure, Func<Int32>>(StorageStructure.ApplicationComponent, () => { return componentAvailableDataElementCounter.GetAvailableElements(); }),
                new Tuple<StorageStructure, Func<Int32>>(StorageStructure.AccessLevel, () => { return accessLeveAvailableDataElementCounter.GetAvailableElements(); }),
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

            foreach (AccessManagerOperation currentOperation in propertyAndContainsQueryOperations)
            {
                // These operations (e.g. 'Users' property and ContainsEntity() method) can be called at any time (incorrect params in the case of methods should just return false)
                //   Hence just set all probabilities to 0.5
                returnDictionary.Add(currentOperation, 0.5);
            }

            foreach (AccessManagerOperation currentOperation in primaryAddOperations)
            {
                // These operations (e.g. AddUser() method) can be called at any time based on probability derived from comparing to the target count for their underlying storage structure
                Double currentCount = Convert.ToDouble(storageStructureCounts[operationToPrimaryStorageStructureMap[currentOperation]]);
                Double targetCount = Convert.ToDouble(targetStorageStructureCounts[operationToPrimaryStorageStructureMap[currentOperation]]);
                Double baseProbability = 1.0 - (currentCount / (2.0 * targetCount));
                if (baseProbability != 0.0)
                {
                    returnDictionary.Add(currentOperation, baseProbability);
                }
                // Add the inverse probability for the inverse operation (e.g. RemoveUser() method)
                //   TODO: Probably easier to do Remove*() first... don't have to do '1.0 - ' twice
                AccessManagerOperation inverseOperation = inverseEventOperationMap[currentOperation];
                if (baseProbability != 1.0)
                {
                    returnDictionary.Add(inverseOperation, 1.0 - baseProbability);
                }
            }

            // Calculate the probabilities for beta scaled operations
            foreach (AccessManagerOperation currentOperation in betaScaledAddOperations)
            {
                Int32 actualElementCount = storageStructureCounts[operationToPrimaryStorageStructureMap[currentOperation]];
                Int32 targetElementCount = targetStorageStructureCounts[operationToPrimaryStorageStructureMap[currentOperation]];
                Double betaCurveParameter = betaScaledAddOperationParameters[currentOperation].BetaCurveParameter;
                Double minimumValue = betaScaledAddOperationParameters[currentOperation].MinimumValue;
                Double addBaseProbability = CalculateBetaScaledBaseRemoveProbability(2 * targetElementCount - actualElementCount, targetElementCount, 1.0, betaCurveParameter, minimumValue);
                if (addBaseProbability > 0.0)
                {
                    returnDictionary.Add(currentOperation, addBaseProbability);
                }
                AccessManagerOperation removeOperation = inverseEventOperationMap[currentOperation];
                Double removeBaseProbability = CalculateBetaScaledBaseRemoveProbability(actualElementCount, targetElementCount, 1.0, betaCurveParameter, minimumValue);
                if (removeBaseProbability > 0.0)
                {
                    returnDictionary.Add(removeOperation, removeBaseProbability);
                }
            }

            Dictionary<AccessManagerOperation, Double> secondaryEventOperationBaseProbabilities = secondaryEventOperationBaseProbabilityCalculator.CalculateBaseProbabilities(storageStructureCounts);
            foreach (KeyValuePair<AccessManagerOperation, Double> currentProbability in secondaryEventOperationBaseProbabilities)
            {
                returnDictionary.Add(currentProbability.Key, currentProbability.Value);
            }

            foreach (AccessManagerOperation currentOperation in getAndHasOperations)
            {
                // These operations (e.g. GetEntities() method) get a probability of 0.5 if all the storage structures corresponding to their parameters are populated, and 0 if not
                Boolean dependentStorageStructureEmpty = false;
                foreach (StorageStructure currentStorageStructure in queryOperationToDependentStorageStructureMap[currentOperation])
                {
                    if (storageStructureCounts[currentStorageStructure] == 0)
                    {
                        dependentStorageStructureEmpty = true;
                        break;
                    }
                }
                if (dependentStorageStructureEmpty == false)
                {
                    returnDictionary.Add(currentOperation, 0.5);
                }
            }

            /*
            PrintDataElementStorerCounts(dataElementStorer);
            Console.WriteLine("-- Base Probabilities --");
            foreach (KeyValuePair<AccessManagerOperation, Double> currKvp in returnDictionary)
            {
                Console.WriteLine($"{currKvp.Key}: {currKvp.Value}");
            }
            */

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
                if (queryOperations.Contains(currentKvp.Key) == true)
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
                foreach (AccessManagerOperation currentQueryOperation in queryOperations)
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
            // Don't check for counts for application components nor access levels, as they're enums in the TestHarness
            var applicationComponentIntegerValue = (Int32)StorageStructure.ApplicationComponent;
            var accessLevelIntegerValue = (Int32)StorageStructure.AccessLevel;
            foreach (Int32 currentStorageStructureValue in Enum.GetValues(typeof(StorageStructure)))
            {
                if (currentStorageStructureValue != applicationComponentIntegerValue && currentStorageStructureValue != accessLevelIntegerValue)
                {
                    if (targetStorageStructureCounts.ContainsKey((StorageStructure)currentStorageStructureValue) == false)
                        throw new ArgumentException($"Parameter '{nameof(targetStorageStructureCounts)}' does not contain a target storage structure count for '{(StorageStructure)currentStorageStructureValue}'.", nameof(targetStorageStructureCounts));

                    if (targetStorageStructureCounts[(StorageStructure)currentStorageStructureValue] < 1)
                        throw new ArgumentOutOfRangeException(nameof(targetStorageStructureCounts), $"Parameter '{nameof(targetStorageStructureCounts)}' with count {targetStorageStructureCounts[(StorageStructure)currentStorageStructureValue]} for storage structure '{(StorageStructure)currentStorageStructureValue}' cannot be less than 1.");
                }
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

        /// <summary>
        /// Calculates base probablility for a remove operation, using the cumulative distribution beta function to scale the probability lower, as the current item count nears the target item count.
        /// </summary>
        /// <param name="currentItemCount">The current number of items.</param>
        /// <param name="targetItemCount">The target number of items.</param>
        /// <param name="betaCurveAlphaParameter">The alpha shape parameter of the beta distribution.</param>
        /// <param name="betaCurveBetaParameter">The beta shape parameter of the beta distribution.</param>
        /// <param name="minimumValue">The minimum value which should be allowed when the current item count matches the target.</param>  
        /// <returns>The base probability.</returns>
        public Double CalculateBetaScaledBaseRemoveProbability(Int32 currentItemCount, Int32 targetItemCount, Double betaCurveAlphaParameter, Double betaCurveBetaParameter, Double minimumValue)
        {
            var betaScaler = new Beta(betaCurveAlphaParameter, betaCurveBetaParameter);
            Double betaScaleFactor;
            if (currentItemCount >= targetItemCount)
            {
                Double betaInput = (Convert.ToDouble(currentItemCount) / Convert.ToDouble(targetItemCount)) - 1.0;
                betaScaleFactor = betaScaler.CumulativeDistribution(betaInput);

            }
            else
            {
                Double betaInput = Convert.ToDouble(currentItemCount) / Convert.ToDouble(targetItemCount);
                betaScaleFactor = betaScaler.CumulativeDistribution(1.0 - betaInput);
            }
            // Scale the scale factor between 0.1 and 1.0
            betaScaleFactor *= (1.0 - minimumValue);
            betaScaleFactor += minimumValue;
            Double baseProbabililty = Convert.ToDouble(currentItemCount) / (2.0 * Convert.ToDouble(targetItemCount));
            
            return betaScaleFactor * baseProbabililty;
        }

        protected void PrintDataElementStorerCounts(DataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer)
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
            Console.WriteLine($":: DataElementStorer Counts at {DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss.fffffff")} ::");
            foreach (Tuple<StorageStructure, Func<Int32>> currentCount in dataElementStorerCountProperties)
            {
                Console.WriteLine($"{currentCount.Item1}: {currentCount.Item2.Invoke()}");
            }
            Console.WriteLine();
        }

        #region Storage Structure Initialization Methods

        /// <summary>
        /// Initializes the operation dependency map members.
        /// </summary>
        protected void InitializeDependencyMaps()
        {
            operationToPrimaryStorageStructureMap = new Dictionary<AccessManagerOperation, StorageStructure>()
            {
                { AccessManagerOperation.AddUser, StorageStructure.Users },
                { AccessManagerOperation.AddGroup, StorageStructure.Groups },
                { AccessManagerOperation.AddEntityType, StorageStructure.EntityTypes },
                { AccessManagerOperation.AddEntity, StorageStructure.Entities },
                { AccessManagerOperation.AddGroupToApplicationComponentAndAccessLevelMapping, StorageStructure.GroupToComponentMap },
                { AccessManagerOperation.AddGroupToEntityMapping, StorageStructure.GroupToEntityMap },
                { AccessManagerOperation.AddGroupToGroupMapping, StorageStructure.GroupToGroupMap },
                { AccessManagerOperation.AddUserToApplicationComponentAndAccessLevelMapping, StorageStructure.UserToComponentMap },
                { AccessManagerOperation.AddUserToEntityMapping, StorageStructure.UserToEntityMap },
                { AccessManagerOperation.AddUserToGroupMapping, StorageStructure.UserToGroupMap },
                { AccessManagerOperation.RemoveUser, StorageStructure.Users },
                { AccessManagerOperation.RemoveGroup, StorageStructure.Groups },
                { AccessManagerOperation.RemoveEntityType, StorageStructure.EntityTypes },
                { AccessManagerOperation.RemoveEntity, StorageStructure.Entities },
                { AccessManagerOperation.RemoveGroupToApplicationComponentAndAccessLevelMapping, StorageStructure.GroupToComponentMap },
                { AccessManagerOperation.RemoveGroupToEntityMapping, StorageStructure.GroupToEntityMap },
                { AccessManagerOperation.RemoveGroupToGroupMapping, StorageStructure.GroupToGroupMap },
                { AccessManagerOperation.RemoveUserToApplicationComponentAndAccessLevelMapping, StorageStructure.UserToComponentMap },
                { AccessManagerOperation.RemoveUserToEntityMapping, StorageStructure.UserToEntityMap },
                { AccessManagerOperation.RemoveUserToGroupMapping, StorageStructure.UserToGroupMap }
            };

            operationToSecondaryStorageStructureMap = new Dictionary<AccessManagerOperation, HashSet<StorageStructure>>()
            {
                { 
                    AccessManagerOperation.AddUserToGroupMapping, 
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Users, StorageStructure.Groups
                    }
                },
                {
                    AccessManagerOperation.AddGroupToGroupMapping,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Groups
                    }
                },
                {
                    AccessManagerOperation.AddUserToApplicationComponentAndAccessLevelMapping,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Users, StorageStructure.ApplicationComponent, StorageStructure.AccessLevel
                    }
                },
                {
                    AccessManagerOperation.AddGroupToApplicationComponentAndAccessLevelMapping,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Groups, StorageStructure.ApplicationComponent, StorageStructure.AccessLevel
                    }
                },
                {
                    AccessManagerOperation.AddEntity,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.EntityTypes
                    }
                },
                {
                    AccessManagerOperation.AddUserToEntityMapping,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Users, StorageStructure.Entities
                    }
                },
                {
                    AccessManagerOperation.AddGroupToEntityMapping,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Groups, StorageStructure.Entities
                    }
                },
            };

            queryOperationToDependentStorageStructureMap = new Dictionary<AccessManagerOperation, HashSet<StorageStructure>>()
            {
                {
                    AccessManagerOperation.GetApplicationComponentsAccessibleByGroup,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Groups
                    }
                },
                {
                    AccessManagerOperation.GetApplicationComponentsAccessibleByUser,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Users
                    }
                },
                {
                    AccessManagerOperation.GetEntities,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.EntityTypes
                    }
                },
                {
                    AccessManagerOperation.GetEntitiesAccessibleByGroup,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Groups, StorageStructure.EntityTypes
                    }
                },
                {
                    AccessManagerOperation.GetEntitiesAccessibleByUser,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Users, StorageStructure.EntityTypes
                    }
                },
                {
                    AccessManagerOperation.GetGroupToApplicationComponentAndAccessLevelMappings,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Groups
                    }
                },
                {
                    AccessManagerOperation.GetGroupToEntityMappings,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Groups
                    }
                },
                {
                    AccessManagerOperation.GetGroupToEntityMappingsEntityTypeOverload,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Groups, StorageStructure.EntityTypes
                    }
                },
                {
                    AccessManagerOperation.GetGroupToGroupMappings,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Groups
                    }
                },
                {
                    AccessManagerOperation.GetUserToApplicationComponentAndAccessLevelMappings,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Users
                    }
                },
                {
                    AccessManagerOperation.GetUserToEntityMappings,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Users
                    }
                },
                {
                    AccessManagerOperation.GetUserToEntityMappingsEntityTypeOverload,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Users, StorageStructure.EntityTypes
                    }
                },
                {
                    AccessManagerOperation.GetUserToGroupMappings,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Users
                    }
                },
                {
                    AccessManagerOperation.HasAccessToApplicationComponent,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Users
                    }
                },
                {
                    AccessManagerOperation.HasAccessToEntity,
                    new HashSet<StorageStructure>()
                    {
                        StorageStructure.Users, StorageStructure.EntityTypes, StorageStructure.Entities
                    }
                }
            };

            inverseEventOperationMap = new Dictionary<AccessManagerOperation, AccessManagerOperation>()
            {
                { AccessManagerOperation.AddUser, AccessManagerOperation.RemoveUser },
                { AccessManagerOperation.AddGroup, AccessManagerOperation.RemoveGroup },
                { AccessManagerOperation.AddUserToGroupMapping, AccessManagerOperation.RemoveUserToGroupMapping },
                { AccessManagerOperation.AddGroupToGroupMapping, AccessManagerOperation.RemoveGroupToGroupMapping },
                { AccessManagerOperation.AddUserToApplicationComponentAndAccessLevelMapping, AccessManagerOperation.RemoveUserToApplicationComponentAndAccessLevelMapping },
                { AccessManagerOperation.AddGroupToApplicationComponentAndAccessLevelMapping, AccessManagerOperation.RemoveGroupToApplicationComponentAndAccessLevelMapping },
                { AccessManagerOperation.AddEntityType, AccessManagerOperation.RemoveEntityType },
                { AccessManagerOperation.AddEntity, AccessManagerOperation.RemoveEntity },
                { AccessManagerOperation.AddUserToEntityMapping, AccessManagerOperation.RemoveUserToEntityMapping },
                { AccessManagerOperation.AddGroupToEntityMapping, AccessManagerOperation.RemoveGroupToEntityMapping },
            };
            // Add the inverse/opposite mappings
            var inversedEventOperations = new Dictionary<AccessManagerOperation, AccessManagerOperation>();
            foreach (KeyValuePair<AccessManagerOperation, AccessManagerOperation> currKvp in inverseEventOperationMap)
            {
                inversedEventOperations.Add(currKvp.Value, currKvp.Key);
            }
            foreach (KeyValuePair<AccessManagerOperation, AccessManagerOperation> currKvp in inversedEventOperations)
            {
                inverseEventOperationMap.Add(currKvp.Key, currKvp.Value);
            }
        }

        /// <summary>
        /// Initializes the '*Operations' members which classify all <see cref="AccessManagerOperation">AccessManagerOperations</see> into different types.
        /// </summary>
        protected void InitializeOperationClassificationSets()
        {
            propertyAndContainsQueryOperations = new HashSet<AccessManagerOperation>();
            primaryAddOperations = new HashSet<AccessManagerOperation>();
            betaScaledAddOperations = new HashSet<AccessManagerOperation>();
            secondaryAddOperations = new HashSet<AccessManagerOperation>();
            removeOperations = new HashSet<AccessManagerOperation>();
            getAndHasOperations = new HashSet<AccessManagerOperation>();
            queryOperations = new HashSet<AccessManagerOperation>();

            propertyAndContainsQueryOperations.UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.UsersPropertyGet,  
                AccessManagerOperation.GroupsPropertyGet, 
                AccessManagerOperation.EntityTypesPropertyGet, 
                AccessManagerOperation.ContainsEntity, 
                AccessManagerOperation.ContainsEntityType, 
                AccessManagerOperation.ContainsGroup, 
                AccessManagerOperation.ContainsUser
            });

            primaryAddOperations.UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.AddUser 
            });

            betaScaledAddOperations.UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.AddEntityType, 
                AccessManagerOperation.AddGroup
            });

            betaScaledAddOperationParameters = new Dictionary<AccessManagerOperation, BetaScalingParameters>()
            {
                { AccessManagerOperation.AddEntityType, new BetaScalingParameters(2.0, 0.05) }, 
                { AccessManagerOperation.AddGroup, new BetaScalingParameters(2.0, 0.05) }
            };

            secondaryAddOperations.UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.AddEntity, 
                AccessManagerOperation.AddGroupToApplicationComponentAndAccessLevelMapping, 
                AccessManagerOperation.AddGroupToEntityMapping, 
                AccessManagerOperation.AddGroupToGroupMapping, 
                AccessManagerOperation.AddUserToApplicationComponentAndAccessLevelMapping, 
                AccessManagerOperation.AddUserToEntityMapping, 
                AccessManagerOperation.AddUserToGroupMapping
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

            getAndHasOperations.UnionWith(new AccessManagerOperation[]
            {
                AccessManagerOperation.GetUserToGroupMappings,
                AccessManagerOperation.GetGroupToGroupMappings,
                AccessManagerOperation.GetUserToApplicationComponentAndAccessLevelMappings,
                AccessManagerOperation.GetGroupToApplicationComponentAndAccessLevelMappings,
                AccessManagerOperation.GetEntities,
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

            queryOperations.UnionWith(propertyAndContainsQueryOperations);
            queryOperations.UnionWith(getAndHasOperations);
        }

        #endregion

        #endregion

        #region Nested Classes

        protected class BetaScalingParameters
        {
            public Double BetaCurveParameter
            {
                get;
            }

            public Double MinimumValue
            {
                get;
            }

            public BetaScalingParameters(Double betaCurveParameter, Double minimumValue)
            {
                this.BetaCurveParameter = betaCurveParameter;
                this.MinimumValue = minimumValue;
            }
        }

        /// <summary>
        /// Defines a method which calculates base probabilities for secondary event operations (like AddUserToGropuMapping()).
        /// </summary>
        protected interface ISecondaryEventOperationBaseProbabilityCalculator
        {
            /// <summary>
            /// Calculates base probabilities for event operations.
            /// </summary>
            /// <param name="storageStructureCounts">The counts of items in each <see cref="StorageStructure"/>.</param>
            /// <returns>The base probabilities.</returns>
            Dictionary<AccessManagerOperation, Double> CalculateBaseProbabilities(Dictionary<StorageStructure, Int32> storageStructureCounts);
        }

        /// <summary>
        /// Implementation of <see cref="ISecondaryEventOperationBaseProbabilityCalculator"/> which calculates the probability based on the actual vs target element counts of the primary data storage structure.
        /// </summary>
        protected class PrimaryDataStructureRatioBaseProbabilityCalculator : ISecondaryEventOperationBaseProbabilityCalculator
        {
            protected HashSet<AccessManagerOperation> secondaryAddOperations;
            protected Dictionary<StorageStructure, Int32> targetStorageStructureCounts;
            protected Dictionary<AccessManagerOperation, StorageStructure> operationToPrimaryStorageStructureMap;
            protected Dictionary<AccessManagerOperation, HashSet<StorageStructure>> operationToSecondaryStorageStructureMap;
            protected Dictionary<AccessManagerOperation, AccessManagerOperation> inverseEventOperationMap;

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.TestHarness.DefaultOperationGenerator+PrimaryDataRatioBaseProbabilityCalculator class.
            /// </summary>
            /// <param name="secondaryAddOperations">AccessManager secondary event Add*() operations (like AddUserToGroupMapping()).</param>
            /// <param name="targetStorageStructureCounts">The target elements counts for each storage structure.</param>
            /// <param name="operationToPrimaryStorageStructureMap">Maps AccessManager operations to the primary storage structure which holds elements for that operation.</param>
            /// <param name="operationToSecondaryStorageStructureMap">Maps AccessManager operations to the primary storage structures which have a secondary dependence for the operation (e.g. as the <see cref="StorageStructure.Users"/> structure is for <see cref="AccessManagerOperation.AddUserToEntityMapping"/>).</param>
            /// <param name="inverseEventOperationMap">AccessManager event operations, and their inverse/opposite operation.</param>
            public PrimaryDataStructureRatioBaseProbabilityCalculator
            (
                HashSet<AccessManagerOperation> secondaryAddOperations, 
                Dictionary<StorageStructure, Int32> targetStorageStructureCounts, 
                Dictionary<AccessManagerOperation, StorageStructure> operationToPrimaryStorageStructureMap, 
                Dictionary<AccessManagerOperation, HashSet<StorageStructure>> operationToSecondaryStorageStructureMap,
                Dictionary<AccessManagerOperation, AccessManagerOperation> inverseEventOperationMap
            )
            {
                this.secondaryAddOperations = secondaryAddOperations;
                this.targetStorageStructureCounts = targetStorageStructureCounts;
                this.operationToPrimaryStorageStructureMap = operationToPrimaryStorageStructureMap;
                this.operationToSecondaryStorageStructureMap = operationToSecondaryStorageStructureMap;
                this.inverseEventOperationMap = inverseEventOperationMap;
            }

            /// <inheritdoc/>
            public Dictionary<AccessManagerOperation, Double> CalculateBaseProbabilities(Dictionary<StorageStructure, Int32> storageStructureCounts)
            {
                var returnDictionary = new Dictionary<AccessManagerOperation, Double>();

                foreach (AccessManagerOperation currentAddOperation in secondaryAddOperations)
                {
                    // Check to see whether any of the secondary dependency storage structures (like 'users' storage structure in the case of the AddUserToGroupMapping() operation) are empty, and if so, set the proability for the operation to be 0
                    Boolean dependentStorageStructureIsEmpty = false;
                    Int32 maxPossibleItems = 1;
                    foreach (StorageStructure currentDependentStorageStructure in operationToSecondaryStorageStructureMap[currentAddOperation])
                    {
                        maxPossibleItems *= storageStructureCounts[currentDependentStorageStructure];
                        if (storageStructureCounts[currentDependentStorageStructure] == 0)
                        {
                            dependentStorageStructureIsEmpty = true;
                        }
                    }
                    if (dependentStorageStructureIsEmpty == true)
                    {
                        // Do nothing... probabilty should be 0, so just don't add
                        //   Probability for inverse RemoveX() operation should also be 0
                    }
                    else
                    {
                        Double currentCount = Convert.ToDouble(storageStructureCounts[operationToPrimaryStorageStructureMap[currentAddOperation]]);
                        Double targetCount = Convert.ToDouble(targetStorageStructureCounts[operationToPrimaryStorageStructureMap[currentAddOperation]]);
                        Double baseProbability = 1.0 - (currentCount / (2.0 * targetCount));
                        // If the current actual count is not equal to the maximum possible (i.e. product of the counts of all secondary dependent structures) include the AddX() probability
                        if (maxPossibleItems != storageStructureCounts[operationToPrimaryStorageStructureMap[currentAddOperation]] || currentAddOperation == AccessManagerOperation.AddEntity)
                        {
                            // Ignore the above check for entities
                            //   Entities are a special case since they only have one dependent storage structure... without this exception, the 'maxPossibleItems' calculation would limit them having the same count as entity types 
                            if (baseProbability != 0.0)
                            {
                                returnDictionary.Add(currentAddOperation, baseProbability);
                            }
                        }
                        // Add the equivalent/inverse RemoveX() probability
                        Double removeProbability = 1.0 - baseProbability;
                        if (removeProbability != 0.0)
                        {
                            AccessManagerOperation removeOperation = inverseEventOperationMap[currentAddOperation];
                            returnDictionary.Add(removeOperation, removeProbability);
                        }
                    }
                }

                return returnDictionary;
            }
        }

        #endregion
    }
}
