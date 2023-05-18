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
using System.Linq;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Prepares <see cref="Action">Actions</see> which execute operations against an <see cref="AccessManager{TUser, TGroup, TComponent, TAccess}"/> instance.
    /// </summary>
    public class OperationExecutionPreparer<TUser, TGroup, TComponent, TAccess>
    {
        protected IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess> parameterGenerator;
        protected IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess> accessManagerQueryProcessor;
        protected IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> accessManagerEventProcessor;
        protected DataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.OperationExecutionPreparer class.
        /// </summary>
        /// <param name="parameterGenerator">The generator to use for operation parameters.</param>
        /// <param name="accessManagerQueryProcessor">The <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> component of the AccessManager under test.</param>
        /// <param name="accessManagerEventProcessor">The <see cref="IAccessManagerQueryProcessor{TUser, TGroup, TComponent, TAccess}"/> component of the AccessManager under test.</param>
        /// <param name="dataElementStorer">The <see cref="DataElementStorer{TUser, TGroup, TComponent, TAccess}"/> to run any post processes/actions against.</param>
        public OperationExecutionPreparer
        (
            IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess> parameterGenerator, 
            IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess> accessManagerQueryProcessor,
            IAccessManagerEventProcessor<TUser, TGroup, TComponent, TAccess> accessManagerEventProcessor,
            DataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer
        )
        {
            this.parameterGenerator = parameterGenerator;
            this.accessManagerQueryProcessor = accessManagerQueryProcessor;
            this.accessManagerEventProcessor = accessManagerEventProcessor;
            this.dataElementStorer = dataElementStorer;
        }

        public PrepareExecutionReturnActions PrepareExecution(AccessManagerOperation operation)
        {
            switch (operation)
            {
                case AccessManagerOperation.UsersPropertyGet:
                    return WrapActionWithEmptyPostExecutionAction
                    (
                        new Action(() => { TUser last = accessManagerQueryProcessor.Users.LastOrDefault(); })
                    );

                case AccessManagerOperation.GroupsPropertyGet:
                    return WrapActionWithEmptyPostExecutionAction
                    (
                        new Action(() => { TGroup last = accessManagerQueryProcessor.Groups.LastOrDefault(); })
                    );

                case AccessManagerOperation.EntityTypesPropertyGet:
                    return WrapActionWithEmptyPostExecutionAction
                    (
                        new Action(() => { String last = accessManagerQueryProcessor.EntityTypes.LastOrDefault(); })
                    );

                case AccessManagerOperation.AddUser:
                    // Parameters may legitimately fail to generate (e.g. trying to get entity type and entity parameters where there are no entities for the type)
                    //   Hence in these cases just return an Action which does nothing (effectively just skipping this operation execution)
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateAddUserParameter();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.AddUser(parameter); }),
                            new Action(() => 
                            {
                                dataElementStorer.AddUser(parameter);
                            })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.ContainsUser:
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateContainsUserParameter();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { Boolean result = accessManagerQueryProcessor.ContainsUser(parameter); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.RemoveUser:
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateRemoveUserParameter();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.RemoveUser(parameter); }),
                            new Action(() => { dataElementStorer.RemoveUser(parameter); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.AddGroup:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateAddGroupParameter();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.AddGroup(parameter); }),
                            new Action(() => { dataElementStorer.AddGroup(parameter); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.ContainsGroup:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateContainsGroupParameter();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { Boolean result = accessManagerQueryProcessor.ContainsGroup(parameter); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.RemoveGroup:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateRemoveGroupParameter();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.RemoveGroup(parameter); }),
                            new Action(() => { dataElementStorer.RemoveGroup(parameter); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.AddUserToGroupMapping:
                    try
                    {
                        Tuple<TUser, TGroup> parameters = parameterGenerator.GenerateAddUserToGroupMappingParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.AddUserToGroupMapping(parameters.Item1, parameters.Item2); }),
                            new Action(() => { dataElementStorer.AddUserToGroupMapping(parameters.Item1, parameters.Item2); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetUserToGroupMappings:
                    try
                    {
                        Tuple<TUser, Boolean> parameters = parameterGenerator.GenerateGetUserToGroupMappingsParameters();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { HashSet<TGroup> result = accessManagerQueryProcessor.GetUserToGroupMappings(parameters.Item1, parameters.Item2); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.RemoveUserToGroupMapping:
                    try
                    {
                        Tuple<TUser, TGroup> parameters = parameterGenerator.GenerateRemoveUserToGroupMappingParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.RemoveUserToGroupMapping(parameters.Item1, parameters.Item2); }),
                            new Action(() => { dataElementStorer.RemoveUserToGroupMapping(parameters.Item1, parameters.Item2); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.AddGroupToGroupMapping:
                    try
                    {
                        Tuple<TGroup, TGroup> parameters = parameterGenerator.GenerateAddGroupToGroupMappingParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.AddGroupToGroupMapping(parameters.Item1, parameters.Item2); }),
                            new Action(() => { dataElementStorer.AddGroupToGroupMapping(parameters.Item1, parameters.Item2); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetGroupToGroupMappings:
                    try
                    {
                        Tuple<TGroup, Boolean> parameters = parameterGenerator.GenerateGetGroupToGroupMappingsParameters();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { HashSet<TGroup> result = accessManagerQueryProcessor.GetGroupToGroupMappings(parameters.Item1, parameters.Item2); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.RemoveGroupToGroupMapping:
                    try
                    {
                        Tuple<TGroup, TGroup> parameters = parameterGenerator.GenerateRemoveGroupToGroupMappingParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.RemoveGroupToGroupMapping(parameters.Item1, parameters.Item2); }),
                            new Action(() => { dataElementStorer.RemoveGroupToGroupMapping(parameters.Item1, parameters.Item2); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.AddUserToApplicationComponentAndAccessLevelMapping:
                    try
                    {
                        Tuple<TUser, TComponent, TAccess> parameters = parameterGenerator.GenerateAddUserToApplicationComponentAndAccessLevelMappingParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.AddUserToApplicationComponentAndAccessLevelMapping(parameters.Item1, parameters.Item2, parameters.Item3); }),
                            new Action(() => { dataElementStorer.AddUserToApplicationComponentAndAccessLevelMapping(parameters.Item1, parameters.Item2, parameters.Item3); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetUserToApplicationComponentAndAccessLevelMappings:
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateGetUserToApplicationComponentAndAccessLevelMappingsParameter();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { Tuple<TComponent, TAccess> last = accessManagerQueryProcessor.GetUserToApplicationComponentAndAccessLevelMappings(parameter).LastOrDefault(); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.RemoveUserToApplicationComponentAndAccessLevelMapping:
                    try
                    {
                        Tuple<TUser, TComponent, TAccess> parameters = parameterGenerator.GenerateRemoveUserToApplicationComponentAndAccessLevelMappingParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.RemoveUserToApplicationComponentAndAccessLevelMapping(parameters.Item1, parameters.Item2, parameters.Item3); }),
                            new Action(() => { dataElementStorer.RemoveUserToApplicationComponentAndAccessLevelMapping(parameters.Item1, parameters.Item2, parameters.Item3); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.AddGroupToApplicationComponentAndAccessLevelMapping:
                    try
                    {
                        Tuple<TGroup, TComponent, TAccess> parameters = parameterGenerator.GenerateAddGroupToApplicationComponentAndAccessLevelMappingParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.AddGroupToApplicationComponentAndAccessLevelMapping(parameters.Item1, parameters.Item2, parameters.Item3); }),
                            new Action(() => { dataElementStorer.AddGroupToApplicationComponentAndAccessLevelMapping(parameters.Item1, parameters.Item2, parameters.Item3); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetGroupToApplicationComponentAndAccessLevelMappings:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateGetGroupToApplicationComponentAndAccessLevelMappingsParameter();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { Tuple<TComponent, TAccess> last = accessManagerQueryProcessor.GetGroupToApplicationComponentAndAccessLevelMappings(parameter).LastOrDefault(); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.RemoveGroupToApplicationComponentAndAccessLevelMapping:
                    try
                    {
                        Tuple<TGroup, TComponent, TAccess> parameters = parameterGenerator.GenerateRemoveGroupToApplicationComponentAndAccessLevelMappingParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.RemoveGroupToApplicationComponentAndAccessLevelMapping(parameters.Item1, parameters.Item2, parameters.Item3); }),
                            new Action(() => { dataElementStorer.RemoveGroupToApplicationComponentAndAccessLevelMapping(parameters.Item1, parameters.Item2, parameters.Item3); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.AddEntityType:
                    try
                    {
                        String parameter = parameterGenerator.GenerateAddEntityTypeParameter();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.AddEntityType(parameter); }),
                            new Action(() => { dataElementStorer.AddEntityType(parameter); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.ContainsEntityType:
                    try
                    {
                        String parameter = parameterGenerator.GenerateContainsEntityTypeParameter();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { Boolean result = accessManagerQueryProcessor.ContainsEntityType(parameter); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.RemoveEntityType:
                    try
                    {
                        String parameter = parameterGenerator.GenerateRemoveEntityTypeParameter();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.RemoveEntityType(parameter); }),
                            new Action(() => { dataElementStorer.RemoveEntityType(parameter); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.AddEntity:
                    try
                    {
                        Tuple<String, String> parameters = parameterGenerator.GenerateAddEntityParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.AddEntity(parameters.Item1, parameters.Item2); }),
                            new Action(() => { dataElementStorer.AddEntity(parameters.Item1, parameters.Item2); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetEntities:
                    try
                    {
                        String parameter = parameterGenerator.GenerateGetEntitiesParameter();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { String last = accessManagerQueryProcessor.GetEntities(parameter).LastOrDefault(); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.ContainsEntity:
                    try
                    {
                        Tuple<String, String> parameters = parameterGenerator.GenerateContainsEntityParameters();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { Boolean result = accessManagerQueryProcessor.ContainsEntity(parameters.Item1, parameters.Item2); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.RemoveEntity:
                    try
                    {
                        Tuple<String, String> parameters = parameterGenerator.GenerateRemoveEntityParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.RemoveEntity(parameters.Item1, parameters.Item2); }),
                            new Action(() => { dataElementStorer.RemoveEntity(parameters.Item1, parameters.Item2); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.AddUserToEntityMapping:
                    try
                    {
                        Tuple<TUser, String, String> parameters = parameterGenerator.GenerateAddUserToEntityMappingParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.AddUserToEntityMapping(parameters.Item1, parameters.Item2, parameters.Item3); }),
                            new Action(() => { dataElementStorer.AddUserToEntityMapping(parameters.Item1, parameters.Item2, parameters.Item3); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetUserToEntityMappings:
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateGetUserToEntityMappingsParameter();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { Tuple<String, String> last = accessManagerQueryProcessor.GetUserToEntityMappings(parameter).LastOrDefault(); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetUserToEntityMappingsEntityTypeOverload:
                    try
                    {
                        Tuple<TUser, String> parameters = parameterGenerator.GenerateGetUserToEntityMappingsEntityTypeOverloadParameters();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { String last = accessManagerQueryProcessor.GetUserToEntityMappings(parameters.Item1, parameters.Item2).LastOrDefault(); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.RemoveUserToEntityMapping:
                    try
                    {
                        Tuple<TUser, String, String> parameters = parameterGenerator.GenerateRemoveUserToEntityMappingParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.RemoveUserToEntityMapping(parameters.Item1, parameters.Item2, parameters.Item3); }),
                            new Action(() => { dataElementStorer.RemoveUserToEntityMapping(parameters.Item1, parameters.Item2, parameters.Item3); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.AddGroupToEntityMapping:
                    try
                    {
                        Tuple<TGroup, String, String> parameters = parameterGenerator.GenerateAddGroupToEntityMappingParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.AddGroupToEntityMapping(parameters.Item1, parameters.Item2, parameters.Item3); }),
                            new Action(() => { dataElementStorer.AddGroupToEntityMapping(parameters.Item1, parameters.Item2, parameters.Item3); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetGroupToEntityMappings:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateGetGroupToEntityMappingsParameter();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { Tuple<String, String> last = accessManagerQueryProcessor.GetGroupToEntityMappings(parameter).LastOrDefault(); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetGroupToEntityMappingsEntityTypeOverload:
                    try
                    {
                        Tuple<TGroup, String> parameters = parameterGenerator.GenerateGetGroupToEntityMappingsEntityTypeOverloadParameters();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { String last = accessManagerQueryProcessor.GetGroupToEntityMappings(parameters.Item1, parameters.Item2).LastOrDefault(); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.RemoveGroupToEntityMapping:
                    try
                    {
                        Tuple<TGroup, String, String> parameters = parameterGenerator.GenerateRemoveGroupToEntityMappingParameters();
                        return new PrepareExecutionReturnActions
                        (
                            new Action(() => { accessManagerEventProcessor.RemoveGroupToEntityMapping(parameters.Item1, parameters.Item2, parameters.Item3); }),
                            new Action(() => { dataElementStorer.RemoveGroupToEntityMapping(parameters.Item1, parameters.Item2, parameters.Item3); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.HasAccessToApplicationComponent:
                    try
                    {
                        Tuple<TUser, TComponent, TAccess> parameters = parameterGenerator.GenerateHasAccessToApplicationComponentParameters();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { Boolean result = accessManagerQueryProcessor.HasAccessToApplicationComponent(parameters.Item1, parameters.Item2, parameters.Item3); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.HasAccessToEntity:
                    try
                    {
                        Tuple<TUser, String, String> parameters = parameterGenerator.GenerateHasAccessToEntityParameters();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { Boolean result = accessManagerQueryProcessor.HasAccessToEntity(parameters.Item1, parameters.Item2, parameters.Item3); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetApplicationComponentsAccessibleByUser:
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateGetApplicationComponentsAccessibleByUserParameter();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { HashSet<Tuple<TComponent, TAccess>> result = accessManagerQueryProcessor.GetApplicationComponentsAccessibleByUser(parameter); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetApplicationComponentsAccessibleByGroup:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateGetApplicationComponentsAccessibleByGroupParameter();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { HashSet<Tuple<TComponent, TAccess>> result = accessManagerQueryProcessor.GetApplicationComponentsAccessibleByGroup(parameter); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetEntitiesAccessibleByUser:
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateGetEntitiesAccessibleByUserParameter();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { HashSet<Tuple<String, String>> result = accessManagerQueryProcessor.GetEntitiesAccessibleByUser(parameter); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetEntitiesAccessibleByUserEntityTypeOverload:
                    try
                    {
                        Tuple<TUser, String> parameters = parameterGenerator.GenerateGetEntitiesAccessibleByUserEntityTypeOverloadParameters();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { HashSet<String> result = accessManagerQueryProcessor.GetEntitiesAccessibleByUser(parameters.Item1, parameters.Item2); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetEntitiesAccessibleByGroup:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateGetEntitiesAccessibleByGroupParameter();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { HashSet<Tuple<String, String>> result = accessManagerQueryProcessor.GetEntitiesAccessibleByGroup(parameter); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                case AccessManagerOperation.GetEntitiesAccessibleByGroupEntityTypeOverload:
                    try
                    {
                        Tuple<TGroup, String> parameters = parameterGenerator.GenerateGetEntitiesAccessibleByGroupEntityTypeOverloadParameters();
                        return WrapActionWithEmptyPostExecutionAction
                        (
                            new Action(() => { HashSet<String> result = accessManagerQueryProcessor.GetEntitiesAccessibleByGroup(parameters.Item1, parameters.Item2); })
                        );
                    }
                    catch (Exception)
                    {
                        return GenerateEmptyReturnActions();
                    }

                default:
                    throw new Exception($"Encountered unhandled {typeof(AccessManagerOperation).Name} '{operation}'.");
            }
        }

        protected PrepareExecutionReturnActions GenerateEmptyReturnActions()
        {
            return new PrepareExecutionReturnActions
            (
                new Action(() => { }),
                new Action(() => { })
            );
        }

        protected PrepareExecutionReturnActions WrapActionWithEmptyPostExecutionAction(Action executionAction)
        {
            return new PrepareExecutionReturnActions
            (
                executionAction,
                new Action(() => { })
            );
        }
    }
}
