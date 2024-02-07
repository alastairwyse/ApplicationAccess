/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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
using ApplicationAccess.InstanceComparer.Configuration;
using ApplicationAccess.Hosting.Rest.Client;
using ApplicationLogging;
using System.Reflection;

namespace ApplicationAccess.InstanceComparer
{
    /// <summary>
    /// Compares the data between two Access Manager instances.
    /// </summary>
    class AccessManagerInstanceComparer
    {
        /// <summary>The source Access Manager instance to compare.</summary>
        protected IAccessManagerQueryProcessor<String, String, String, String> sourceInstance;
        /// <summary>The target Access Manager instance to compare.</summary>
        protected IAccessManagerQueryProcessor<String, String, String, String> targetInstance;
        /// <summary>Used to generate set of parameters to pass to query methods.</summary>
        protected ParameterCombinationGenerator parameterCombinationGenerator;
        /// <summary>Used to compare method results / return values.</summary>
        protected ResultComparer resultComparer;
        /// <summary>>Whether the comparer should throw an exception when different results are encountered (or log and continue comparing).</summary>
        protected Boolean throwExceptionOnDifference;
        /// <summary>Whether each individual mathod parameter set compared should be logged.</summary>
        protected Boolean logIndividualParameters;
        /// <summary>Logger.</summary>
        protected IApplicationLogger logger;
        /// <summary>Master set of users from the source Access Manager instance.</summary>
        protected IEnumerable<String> users;
        /// <summary>Master set of groups from the source Access Manager instance.</summary>
        protected IEnumerable<String> groups;
        /// <summary>Master set of application components.</summary>
        protected IEnumerable<String> applicationComponents;
        /// <summary>Master set of access levels.</summary>
        protected IEnumerable<String> accessLevels;
        /// <summary>Master set of entities and their types from the source Access Manager instance.</summary>
        protected Dictionary<String, HashSet<String>> entitiesAndTypes;
        /// <summary>The source instance users to exclude from comparison (e.g. because they have nothing mapped to them, and hence are known to not exist in the target instance).</summary>
        protected HashSet<String> excludeUsers;
        /// <summary>The source instance groups to exclude from comparison.</summary>
        protected HashSet<String> excludeGroups;
        /// <summary>The source instance entities to exclude from comparison.<</summary>
        protected Dictionary<String, HashSet<String>> excludeEntities;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.InstanceComparer.AccessManagerInstanceComparer class.
        /// </summary>
        /// <param name="sourceInstance">The source Access Manager instance to compare.</param>
        /// <param name="targetInstance">The target Access Manager instance to compare.</param>
        /// <param name="applicationComponents">Master set of application components to use in generating query parameter.</param>
        /// <param name="accessLevels">Master set of access levels to use in generating query parameter.</param>
        /// <param name="excludeUsers">The source instance users to exclude from comparison (e.g. because they have nothing mapped to them, and hence are known to not exist in the target instance).</param>
        /// <param name="excludeGroups">The source instance groups to exclude from comparison.</param>
        /// <param name="excludeEntities">The source instance entities to exclude from comparison.</param>
        /// <param name="throwExceptionOnDifference">Whether the comparer should throw an exception when different results are encountered (or log and continue comparing).</param>
        /// <param name="logIndividualParameters">Whether each individual mathod parameter set compared should be logged.</param>
        /// <param name="logger">Logger.</param>
        public AccessManagerInstanceComparer
        (
            IAccessManagerQueryProcessor<String, String, String, String> sourceInstance,
            IAccessManagerQueryProcessor<String, String, String, String> targetInstance,
            IEnumerable<String> applicationComponents,
            IEnumerable<String> accessLevels,
            HashSet<String> excludeUsers,
            HashSet<String> excludeGroups,
            IEnumerable<ExcludeEntitiesConfiguration> excludeEntities,
            Boolean throwExceptionOnDifference,
            Boolean logIndividualParameters,
            IApplicationLogger logger
        )
        {
            this.sourceInstance = sourceInstance;
            this.targetInstance = targetInstance;
            this.applicationComponents = applicationComponents;
            this.accessLevels = accessLevels;
            this.excludeUsers = excludeUsers;
            this.excludeGroups = excludeGroups;
            this.excludeEntities = new Dictionary<String, HashSet<String>>();
            foreach (ExcludeEntitiesConfiguration currentExcludeEntitiesConfiguration in excludeEntities)
            {
                this.excludeEntities.Add(currentExcludeEntitiesConfiguration.EntityType, new HashSet<String>());
                foreach (String currentExcludeEntity in currentExcludeEntitiesConfiguration.Entities)
                {
                    this.excludeEntities[currentExcludeEntitiesConfiguration.EntityType].Add(currentExcludeEntity);
                }
            }
            this.logIndividualParameters = logIndividualParameters;
            this.logger = logger;
            parameterCombinationGenerator = new ParameterCombinationGenerator();
            resultComparer = new ResultComparer(throwExceptionOnDifference, logger);
            this.throwExceptionOnDifference = throwExceptionOnDifference;
            this.users = new HashSet<String>();
            this.groups = new HashSet<String>();
            this.entitiesAndTypes = new Dictionary<String, HashSet<String>>();
        }

        /// <summary>
        /// Compares the contents / data of the source and target Access Manager instances.
        /// </summary>
        public void Compare()
        {
            // Compare the primary elements and populate master lists
            ComparePrimaryElementResultsAndPopulateMasterSet
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor) => { return queryProcessor.Users; },
                excludeUsers,
                (HashSet<String> masterElements) => { users = masterElements; },
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.Users)
            );
            ComparePrimaryElementResultsAndPopulateMasterSet
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor) => { return queryProcessor.Groups; },
                excludeGroups,
                (HashSet<String> masterElements) => { groups = masterElements; },
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.Groups)
            );
            entitiesAndTypes = new Dictionary<String, HashSet<String>>();
            var excludeEntityTypes = new HashSet<String>(excludeEntities.Keys);
            ComparePrimaryElementResultsAndPopulateMasterSet
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor) => { return queryProcessor.EntityTypes; },
                excludeEntityTypes,
                (HashSet<String> masterElements) =>
                {
                    foreach (String currentEntityType in masterElements)
                    {
                        entitiesAndTypes.Add(currentEntityType, new HashSet<String>());
                    }
                },
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.EntityTypes)
            );
            PopulateMasterEntitySet();
            
            // Compare the Contains*() methods 
            CompareBooleanMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, String parameter) =>
                {
                    return queryProcessor.ContainsUser(parameter);
                },
                users,
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.ContainsUser)
            );
            CompareBooleanMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, String parameter) =>
                {
                    return queryProcessor.ContainsGroup(parameter);
                },
                groups,
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.ContainsGroup)
            );
            CompareBooleanMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, String parameter) =>
                {
                    return queryProcessor.ContainsEntityType(parameter);
                },
                entitiesAndTypes.Keys,
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.ContainsEntityType)
            );

            // Compare other methods which return a boolean
            CompareBooleanMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, Tuple<String, String> parameters) =>
                {
                    return queryProcessor.ContainsEntity(parameters.Item1, parameters.Item2);
                },
                ConvertEntitiesAndTypesToIEnumerable(),
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.ContainsEntity)
            );
            CompareBooleanMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, Tuple<String, String, String> parameters) =>
                {
                    return queryProcessor.HasAccessToEntity(parameters.Item1, parameters.Item2, parameters.Item3);
                },
                parameterCombinationGenerator.Generate(users, entitiesAndTypes),
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.HasAccessToEntity)
            );
            CompareBooleanMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, Tuple<String, String, String> parameters) =>
                {
                    return queryProcessor.HasAccessToApplicationComponent(parameters.Item1, parameters.Item2, parameters.Item3);
                },
                parameterCombinationGenerator.Generate(users, applicationComponents, accessLevels),
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.HasAccessToApplicationComponent)
            );
           
            // Compare methods which return an enumerable of strings
            CompareSingleStringEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, String parameter) =>
                {
                    return queryProcessor.GetEntities(parameter);
                },
                entitiesAndTypes.Keys,
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetEntities)
            );
            CompareSingleStringEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, Tuple<String, String> parameters) =>
                {
                    return queryProcessor.GetUserToEntityMappings(parameters.Item1, parameters.Item2);
                },
                parameterCombinationGenerator.Generate(users, entitiesAndTypes.Keys),
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetUserToEntityMappings)
            );
            CompareSingleStringEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, Tuple<String, String> parameters) =>
                {
                    return queryProcessor.GetGroupToEntityMappings(parameters.Item1, parameters.Item2);
                },
                parameterCombinationGenerator.Generate(groups, entitiesAndTypes.Keys),
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetGroupToEntityMappings)
            );
            CompareSingleStringEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, Tuple<String, String> parameters) =>
                {
                    return queryProcessor.GetEntitiesAccessibleByUser(parameters.Item1, parameters.Item2);
                },
                parameterCombinationGenerator.Generate(users, entitiesAndTypes.Keys),
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetEntitiesAccessibleByUser)
            );
            CompareSingleStringEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, Tuple<String, String> parameters) =>
                {
                    return queryProcessor.GetEntitiesAccessibleByGroup(parameters.Item1, parameters.Item2);
                },
                parameterCombinationGenerator.Generate(groups, entitiesAndTypes.Keys),
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetEntitiesAccessibleByGroup)
            );
            CompareSingleStringEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, Tuple<String, Boolean> parameters) =>
                {
                    return queryProcessor.GetUserToGroupMappings(parameters.Item1, parameters.Item2);
                },
                parameterCombinationGenerator.Generate(users),
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetUserToGroupMappings)
            );
            CompareSingleStringEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, Tuple<String, Boolean> parameters) =>
                {
                    return queryProcessor.GetGroupToGroupMappings(parameters.Item1, parameters.Item2);
                },
                parameterCombinationGenerator.Generate(groups),
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetGroupToGroupMappings)
            );
            
            // Compare methods which return an enumerable of tuples of two strings
            CompareTupleOfStringsEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, String parameter) =>
                {
                    return queryProcessor.GetApplicationComponentsAccessibleByUser(parameter);
                },
                users,
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetApplicationComponentsAccessibleByUser)
            );
            CompareTupleOfStringsEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, String parameter) =>
                {
                    return queryProcessor.GetApplicationComponentsAccessibleByGroup(parameter);
                },
                groups,
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetApplicationComponentsAccessibleByGroup)
            );
            CompareTupleOfStringsEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, String parameter) =>
                {
                    return queryProcessor.GetEntitiesAccessibleByUser(parameter);
                },
                users,
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetEntitiesAccessibleByUser)
            );
            CompareTupleOfStringsEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, String parameter) =>
                {
                    return queryProcessor.GetEntitiesAccessibleByGroup(parameter);
                },
                groups,
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetEntitiesAccessibleByGroup)
            );
            CompareTupleOfStringsEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, String parameter) =>
                {
                    return queryProcessor.GetUserToApplicationComponentAndAccessLevelMappings(parameter);
                },
                users,
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetUserToApplicationComponentAndAccessLevelMappings)
            );
            CompareTupleOfStringsEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, String parameter) =>
                {
                    return queryProcessor.GetUserToEntityMappings(parameter);
                },
                users,
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetUserToEntityMappings)
            );
            CompareTupleOfStringsEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, String parameter) =>
                {
                    return queryProcessor.GetGroupToApplicationComponentAndAccessLevelMappings(parameter);
                },
                groups,
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetGroupToApplicationComponentAndAccessLevelMappings)
            );
            CompareTupleOfStringsEnumerableMethods
            (
                (IAccessManagerQueryProcessor<String, String, String, String> queryProcessor, String parameter) =>
                {
                    return queryProcessor.GetGroupToEntityMappings(parameter);
                },
                groups,
                nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetGroupToEntityMappings)
            );
        }

        #region Private/Protected Methods

        /// <summary>
        /// Queries and compares the results of a primary element between the source and target instances, and populates the master list of that element.
        /// </summary>
        /// <param name="queryMethod">A Func which takes an AccessManager query processor and returns an enumerable of the primary element, and which executes the query method.</param>
        /// <param name="excludeElements">The elements to exclude from the master list.</param>
        /// <param name="populateMasterListAction">Action which accepts a set of element values, and populates the master list of elements.</param>
        /// <param name="propertyName">The name of the property which returns the primary elements.</param>
        protected void ComparePrimaryElementResultsAndPopulateMasterSet
        (
            Func<IAccessManagerQueryProcessor<String, String, String, String>, IEnumerable<String>> queryMethod,
            HashSet<String> excludeElements,
            Action<HashSet<String>> populateMasterListAction,
            String propertyName
        )
        {
            logger.Log(LogLevel.Information, $"Comparing results of property '{propertyName}'");
            IEnumerable<String> sourceResults = Enumerable.Empty<String>();
            IEnumerable<String> targetResults = Enumerable.Empty<String>();
            try
            {
                sourceResults = queryMethod(sourceInstance);
            }
            catch (Exception e)
            {
                String errorMessage = $"Failed to query source property '{propertyName}'.";
                logger.Log(LogLevel.Error, errorMessage, e);
                throw new Exception(errorMessage, e);
            }
            try
            {
                targetResults = queryMethod(targetInstance);
            }
            catch (Exception e)
            {
                String errorMessage = $"Failed to query target property '{propertyName}'.";
                logger.Log(LogLevel.Error, errorMessage, e);
                throw new Exception(errorMessage, e);
            }
            // Filter the source elements and populate the master list
            var filteredSourceResults = new HashSet<String>();
            foreach (String currentSourceElement in sourceResults)
            {
                if (excludeElements.Contains(currentSourceElement) == false)
                {
                    filteredSourceResults.Add(currentSourceElement);
                }
            }
            populateMasterListAction(filteredSourceResults);
            resultComparer.Compare(filteredSourceResults, targetResults);
        }

        /// <summary>
        /// Populates the master list of entities.
        /// </summary>
        protected void PopulateMasterEntitySet()
        {
            logger.Log(LogLevel.Information, $"Populating master set of entities");
            foreach (String currentEntityType in entitiesAndTypes.Keys)
            {
                if (excludeEntities.ContainsKey(currentEntityType) == false)
                {
                    IEnumerable<String> entities = Enumerable.Empty<String>();
                    try
                    {
                        entities = sourceInstance.GetEntities(currentEntityType);
                    }
                    catch (Exception e)
                    {
                        String errorMessage = $"Failed to query source method '{nameof(IAccessManagerQueryProcessor<String, String, String, String>.GetEntities)}'.";
                        logger.Log(LogLevel.Error, errorMessage, e);
                        throw new Exception(errorMessage, e);
                    }
                    foreach (String currentEntity in entities)
                    {
                        if (!(excludeEntities.ContainsKey(currentEntityType) == true && excludeEntities[currentEntityType].Contains(currentEntity) == true))
                        {
                            entitiesAndTypes[currentEntityType].Add(currentEntity);
                        }
                    }
                }
            }
        }

        #region Compare*() methods for various parameter and return types

        /// <summary>
        /// Compares the results of a method which returns a boolean.
        /// </summary>
        /// <param name="queryMethod">Func which accepts an <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> and parameters for a query method and returns the boolean result (having called the query method).</param>
        /// <param name="queryMethodParameters">An enumerable of strings containing the parameters which should passed to the method.</param>
        /// <param name="methodName">The name of the method,</param>
        protected void CompareBooleanMethods
        (
            Func<IAccessManagerQueryProcessor<String, String, String, String>, String, Boolean> queryMethod,
            IEnumerable<String> queryMethodParameters,
            String methodName
        )
        {
            logger.Log(LogLevel.Information, $"Comparing results of method '{methodName}'");
            foreach (String currentParameterValue in queryMethodParameters)
            {
                LogStringIfLogIndividualParametersSetTrue($"Comparing with parameter '{currentParameterValue}'");
                Boolean sourceResult = false;
                Boolean targetResult = false;
                Exception sourceException = null;
                Exception targetException = null;
                try
                {
                    sourceResult = queryMethod(sourceInstance, currentParameterValue);
                }
                catch (Exception e)
                {
                    sourceException = e;
                }
                try
                {
                    targetResult = queryMethod(targetInstance, currentParameterValue);
                }
                catch (Exception e)
                {
                    targetException = e;
                }
                Action<Boolean, Boolean> resultCompareAction = (sourceResult, targetResult) =>
                {
                    resultComparer.Compare(sourceResult, targetResult);
                };
                ProcessResults(sourceException, targetException, sourceResult, targetResult, resultCompareAction);
            }
        }

        /// <summary>
        /// Compares the results of a method which returns a boolean.
        /// </summary>
        /// <param name="queryMethod">Func which accepts an <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> and parameters for a query method and returns the boolean result (having called the query method).</param>
        /// <param name="queryMethodParameters">An enumerable of tuples of two strings, containing the parameters which should passed to the method.</param>
        /// <param name="methodName">The name of the method,</param>
        protected void CompareBooleanMethods
        (
            Func<IAccessManagerQueryProcessor<String, String, String, String>, Tuple<String, String>, Boolean> queryMethod,
            IEnumerable<Tuple<String, String>> queryMethodParameters,
            String methodName
        )
        {
            logger.Log(LogLevel.Information, $"Comparing results of method '{methodName}'");
            foreach (Tuple<String, String> currentParameterValues in queryMethodParameters)
            {
                LogStringIfLogIndividualParametersSetTrue($"Comparing with parameters '{currentParameterValues}'");
                Boolean sourceResult = false;
                Boolean targetResult = false;
                Exception sourceException = null;
                Exception targetException = null;
                try
                {
                    sourceResult = queryMethod(sourceInstance, currentParameterValues);
                }
                catch (Exception e)
                {
                    sourceException = e;
                }
                try
                {
                    targetResult = queryMethod(targetInstance, currentParameterValues);
                }
                catch (Exception e)
                {
                    targetException = e;
                }
                Action<Boolean, Boolean> resultCompareAction = (sourceResult, targetResult) =>
                {
                    resultComparer.Compare(sourceResult, targetResult);
                };
                ProcessResults(sourceException, targetException, sourceResult, targetResult, resultCompareAction);
            }
        }

        /// <summary>
        /// Compares the results of a method which returns a boolean.
        /// </summary>
        /// <param name="queryMethod">Func which accepts an <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> and parameters for a query method and returns the boolean result (having called the query method).</param>
        /// <param name="queryMethodParameters">An enumerable of tuples of three strings, containing the parameters which should passed to the method.</param>
        /// <param name="methodName">The name of the method,</param>
        protected void CompareBooleanMethods
        (
            Func<IAccessManagerQueryProcessor<String, String, String, String>, Tuple<String, String, String>, Boolean> queryMethod,
            IEnumerable<Tuple<String, String, String>> queryMethodParameters,
            String methodName
        )
        {
            logger.Log(LogLevel.Information, $"Comparing results of method '{methodName}'");
            foreach (Tuple<String, String, String> currentParameterValues in queryMethodParameters)
            {
                LogStringIfLogIndividualParametersSetTrue($"Comparing with parameters '{currentParameterValues}'");
                Boolean sourceResult = false;
                Boolean targetResult = false;
                Exception sourceException = null;
                Exception targetException = null;
                try
                {
                    sourceResult = queryMethod(sourceInstance, currentParameterValues);
                }
                catch (Exception e)
                {
                    sourceException = e;
                }
                try
                {
                    targetResult = queryMethod(targetInstance, currentParameterValues);
                }
                catch (Exception e)
                {
                    targetException = e;
                }
                Action<Boolean, Boolean> resultCompareAction = (sourceResult, targetResult) =>
                {
                    resultComparer.Compare(sourceResult, targetResult);
                };
                ProcessResults(sourceException, targetException, sourceResult, targetResult, resultCompareAction);
            }
        }

        /// <summary>
        /// Compares the results of a method which returns an enumerable of strings.
        /// </summary>
        /// <param name="queryMethod">Func which accepts an <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> and parameters for a query method and returns the enumerable of strings result (having called the query method).</param>
        /// <param name="queryMethodParameters">An enumerable strings, containing the parameters which should passed to the method.</param>
        /// <param name="methodName">The name of the method,</param>
        public void CompareSingleStringEnumerableMethods
        (
            Func<IAccessManagerQueryProcessor<String, String, String, String>, String, IEnumerable<String>> queryMethod,
            IEnumerable<String> queryMethodParameters,
            String methodName
        )
        {
            logger.Log(LogLevel.Information, $"Comparing results of method '{methodName}'");
            foreach (String currentParameterValue in queryMethodParameters)
            {
                LogStringIfLogIndividualParametersSetTrue($"Comparing with parameter '{currentParameterValue}'");
                IEnumerable<String> sourceResult = Enumerable.Empty<String>();
                IEnumerable<String> targetResult = Enumerable.Empty<String>();
                Exception sourceException = null;
                Exception targetException = null;
                try
                {
                    sourceResult = queryMethod(sourceInstance, currentParameterValue);
                    sourceResult.Count();
                }
                catch (Exception e)
                {
                    sourceException = e;
                }
                try
                {
                    targetResult = queryMethod(targetInstance, currentParameterValue);
                    targetResult.Count();
                }
                catch (Exception e)
                {
                    targetException = e;
                }
                Action<IEnumerable<String>, IEnumerable<String>> resultCompareAction = (sourceResult, targetResult) =>
                {
                    resultComparer.Compare(sourceResult, targetResult);
                };
                ProcessResults(sourceException, targetException, sourceResult, targetResult, resultCompareAction);
            }
        }

        /// <summary>
        /// Compares the results of a method which returns an enumerable of strings.
        /// </summary>
        /// <param name="queryMethod">Func which accepts an <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> and parameters for a query method and returns the enumerable of strings result (having called the query method).</param>
        /// <param name="queryMethodParameters">An enumerable of tuples of two strings, containing the parameters which should passed to the method.</param>
        /// <param name="methodName">The name of the method,</param>
        public void CompareSingleStringEnumerableMethods
        (
            Func<IAccessManagerQueryProcessor<String, String, String, String>, Tuple<String, String>, IEnumerable<String>> queryMethod,
            IEnumerable<Tuple<String, String>> queryMethodParameters,
            String methodName
        )
        {
            logger.Log(LogLevel.Information, $"Comparing results of method '{methodName}'");
            foreach (Tuple<String, String> currentParameterValues in queryMethodParameters)
            {
                LogStringIfLogIndividualParametersSetTrue($"Comparing with parameters '{currentParameterValues}'");
                IEnumerable<String> sourceResult = Enumerable.Empty<String>();
                IEnumerable<String> targetResult = Enumerable.Empty<String>();
                Exception sourceException = null;
                Exception targetException = null;
                try
                {
                    sourceResult = queryMethod(sourceInstance, currentParameterValues);
                    sourceResult.Count();
                }
                catch (Exception e)
                {
                    sourceException = e;
                }
                try
                {
                    targetResult = queryMethod(targetInstance, currentParameterValues);
                    targetResult.Count();
                }
                catch (Exception e)
                {
                    targetException = e;
                }
                Action<IEnumerable<String>, IEnumerable<String>> resultCompareAction = (sourceResult, targetResult) =>
                {
                    resultComparer.Compare(sourceResult, targetResult);
                };
                ProcessResults(sourceException, targetException, sourceResult, targetResult, resultCompareAction);
            }
        }

        /// <summary>
        /// Compares the results of a method which returns an enumerable of strings.
        /// </summary>
        /// <param name="queryMethod">Func which accepts an <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> and parameters for a query method and returns the enumerable of strings result (having called the query method).</param>
        /// <param name="queryMethodParameters">An enumerable of tuples of a string and a boolean, containing the parameters which should passed to the method.</param>
        /// <param name="methodName">The name of the method,</param>
        public void CompareSingleStringEnumerableMethods
        (
            Func<IAccessManagerQueryProcessor<String, String, String, String>, Tuple<String, Boolean>, IEnumerable<String>> queryMethod,
            IEnumerable<Tuple<String, Boolean>> queryMethodParameters,
            String methodName
        )
        {
            logger.Log(LogLevel.Information, $"Comparing results of method '{methodName}'");
            foreach (Tuple<String, Boolean> currentParameterValues in queryMethodParameters)
            {
                LogStringIfLogIndividualParametersSetTrue($"Comparing with parameters '{currentParameterValues}'");
                IEnumerable<String> sourceResult = Enumerable.Empty<String>();
                IEnumerable<String> targetResult = Enumerable.Empty<String>();
                Exception sourceException = null;
                Exception targetException = null;
                try
                {
                    sourceResult = queryMethod(sourceInstance, currentParameterValues);
                    sourceResult.Count();
                }
                catch (Exception e)
                {
                    sourceException = e;
                }
                try
                {
                    targetResult = queryMethod(targetInstance, currentParameterValues);
                    targetResult.Count();
                }
                catch (Exception e)
                {
                    targetException = e;
                }
                Action<IEnumerable<String>, IEnumerable<String>> resultCompareAction = (sourceResult, targetResult) =>
                {
                    resultComparer.Compare(sourceResult, targetResult);
                };
                ProcessResults(sourceException, targetException, sourceResult, targetResult, resultCompareAction);
            }
        }

        /// <summary>
        /// Compares the results of a method which returns an enumerable of a tuple of two strings.
        /// </summary>
        /// <param name="queryMethod">Func which accepts an <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> and parameters for a query method and returns the enumerable of tuples of strings result (having called the query method).</param>
        /// <param name="queryMethodParameters">An enumerable of strings, containing the parameters which should passed to the method.</param>
        /// <param name="methodName">The name of the method,</param>
        public void CompareTupleOfStringsEnumerableMethods
        (
            Func<IAccessManagerQueryProcessor<String, String, String, String>, String, IEnumerable<Tuple<String, String>>> queryMethod,
            IEnumerable<String> queryMethodParameters,
            String methodName
        )
        {
            logger.Log(LogLevel.Information, $"Comparing results of method '{methodName}'");
            foreach (String currentParameterValue in queryMethodParameters)
            {
                LogStringIfLogIndividualParametersSetTrue($"Comparing with parameter '{currentParameterValue}'");
                IEnumerable<Tuple<String, String>> sourceResult = Enumerable.Empty<Tuple<String, String>>();
                IEnumerable<Tuple<String, String>> targetResult = Enumerable.Empty<Tuple<String, String>>();
                Exception sourceException = null;
                Exception targetException = null;
                try
                {
                    sourceResult = queryMethod(sourceInstance, currentParameterValue);
                    sourceResult.Count();
                }
                catch (Exception e)
                {
                    sourceException = e;
                }
                try
                {
                    targetResult = queryMethod(targetInstance, currentParameterValue);
                    targetResult.Count();
                }
                catch (Exception e)
                {
                    targetException = e;
                }
                Action<IEnumerable<Tuple<String, String>>, IEnumerable<Tuple<String, String>>> resultCompareAction = (sourceResult, targetResult) =>
                {
                    resultComparer.Compare(sourceResult, targetResult);
                };
                ProcessResults(sourceException, targetException, sourceResult, targetResult, resultCompareAction);
            }
        }

        #endregion

        /// <summary>
        /// Processes the results from calling method or property against the source and target Access Manager instances.
        /// </summary>
        /// <typeparam name="T">The type of the result of the method call or property.</typeparam>
        /// <param name="sourceException">The exception which occurred when calling the source method or property, or null if no exception occurred.</param>
        /// <param name="targetException">The exception which occurred when calling the target method or property, or null if no exception occurred.</param>
        /// <param name="sourceResult">The result from calling the source method or property.</param>
        /// <param name="targetResult">The result from calling the target method or property.</param>
        /// <param name="resultCompareAction">Action which compares the source and target results.</param>
        protected void ProcessResults<T>(Exception sourceException, Exception targetException, T sourceResult, T targetResult, Action<T, T> resultCompareAction)
        {
            if (sourceException != null && targetException != null)
            {
                if (!(sourceException.GetType() == targetException.GetType() && sourceException.Message == targetException.Message))
                {
                    String message = $"Encountered different {typeof(Exception).Name} results.  Source result threw {sourceException.GetType().Name} with message '{sourceException.Message}', target result threw  {targetException.GetType().Name} with message '{targetException.Message}'.";
                    logger.Log(LogLevel.Error, message, sourceException);
                    if (throwExceptionOnDifference == true)
                    {
                        throw new Exception(message);
                    }
                }
            }
            else if (sourceException == null && targetException == null)
            {
                resultCompareAction(sourceResult, targetResult);
            }
            else if (sourceException != null && targetException == null)
            {
                String message = $"Encountered different {typeof(Exception).Name} results.  Source result threw {sourceException.GetType().Name} with message '{sourceException.Message}', target result was '{targetResult}'.";
                logger.Log(LogLevel.Error, message, sourceException);
                if (throwExceptionOnDifference == true)
                {
                    throw new Exception(message);
                }
            }
            else
            {
                String message = $"Encountered different {typeof(Exception).Name} results.  Source result was {sourceResult}, target result threw  {targetException.GetType().Name} with message '{targetException.Message}'.";
                logger.Log(LogLevel.Error, message, targetException);
                if (throwExceptionOnDifference == true)
                {
                    throw new Exception(message);
                }
            }
        }

        protected IEnumerable<Tuple<String, String>> ConvertEntitiesAndTypesToIEnumerable()
        {
            foreach (KeyValuePair<String, HashSet<String>> currentKvp in entitiesAndTypes)
            {
                foreach(String currentEntity in currentKvp.Value)
                {
                    yield return Tuple.Create(currentKvp.Key, currentEntity);
                }
            }
        }

        protected IEnumerable<String> GetEntitiesAsEnumerable()
        {
            foreach (HashSet<String> currentEntitySet in entitiesAndTypes.Values)
            {
                foreach (String currentEntity in currentEntitySet)
                {
                    yield return currentEntity;
                }
            }
        }

        protected void LogStringIfLogIndividualParametersSetTrue(String logString)
        {
            if (logIndividualParameters == true)
            {
                logger.Log(LogLevel.Information, logString);
            }
        }

        #endregion
    }
}
