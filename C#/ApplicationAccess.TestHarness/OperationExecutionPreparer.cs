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
        protected IAccessManager<TUser, TGroup, TComponent, TAccess> accessManager;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.OperationExecutionPreparer class.
        /// </summary>
        /// <param name="parameterGenerator">The generator to use for operation parameters.</param>
        /// <param name="accessManager">The access manager to execute the operation against.</param>
        public OperationExecutionPreparer(IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess> parameterGenerator, IAccessManager<TUser, TGroup, TComponent, TAccess> accessManager)
        {
            this.parameterGenerator = parameterGenerator;
            this.accessManager = accessManager;
        }

        public Action PrepareExecution(AccessManagerOperation operation)
        {
            switch (operation)
            {
                case AccessManagerOperation.UsersPropertyGet:
                    return new Action(() => { TUser last = accessManager.Users.Last(); });

                case AccessManagerOperation.GroupsPropertyGet:
                    return new Action(() => { TGroup last = accessManager.Groups.Last(); });

                case AccessManagerOperation.EntityTypesPropertyGet:
                    return new Action(() => { String last = accessManager.EntityTypes.Last(); });

                case AccessManagerOperation.AddUser:
                    // Parameters may legitimately fail to generate (e.g. trying to get entity type and entity parameters where there are no entities for the type)
                    //   Hence in these cases just return an Action which does nothing (effectively just skipping this operation execution)
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateAddUserParameter();
                        return new Action(() => { accessManager.AddUser(parameter); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.ContainsUser:
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateContainsUserParameter();
                        return new Action(() => { Boolean result = accessManager.ContainsUser(parameter); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.RemoveUser:
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateRemoveUserParameter();
                        return new Action(() => { accessManager.RemoveUser(parameter); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.AddGroup:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateAddGroupParameter();
                        return new Action(() => { accessManager.AddGroup(parameter); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.ContainsGroup:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateContainsGroupParameter();
                        return new Action(() => { Boolean result = accessManager.ContainsGroup(parameter); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.RemoveGroup:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateRemoveGroupParameter();
                        return new Action(() => { accessManager.RemoveGroup(parameter); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.AddUserToGroupMapping:
                    try
                    {
                        Tuple<TUser, TGroup> parameters = parameterGenerator.GenerateAddUserToGroupMappingParameters();
                        return new Action(() => { accessManager.AddUserToGroupMapping(parameters.Item1, parameters.Item2); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetUserToGroupMappings:
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateGetUserToGroupMappingsParameter();
                        return new Action(() => { TGroup last = accessManager.GetUserToGroupMappings(parameter).Last(); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.RemoveUserToGroupMapping:
                    try
                    {
                        Tuple<TUser, TGroup> parameters = parameterGenerator.GenerateRemoveUserToGroupMappingParameters();
                        return new Action(() => { accessManager.RemoveUserToGroupMapping(parameters.Item1, parameters.Item2); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.AddGroupToGroupMapping:
                    try
                    {
                        Tuple<TGroup, TGroup> parameters = parameterGenerator.GenerateAddGroupToGroupMappingParameters();
                        return new Action(() => { accessManager.AddGroupToGroupMapping(parameters.Item1, parameters.Item2); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetGroupToGroupMappings:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateGetGroupToGroupMappingsParameter();
                        return new Action(() => { TGroup last = accessManager.GetGroupToGroupMappings(parameter).Last(); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.RemoveGroupToGroupMapping:
                    try
                    {
                        Tuple<TGroup, TGroup> parameters = parameterGenerator.GenerateRemoveGroupToGroupMappingParameters();
                        return new Action(() => { accessManager.RemoveGroupToGroupMapping(parameters.Item1, parameters.Item2); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.AddUserToApplicationComponentAndAccessLevelMapping:
                    try
                    {
                        Tuple<TUser, TComponent, TAccess> parameters = parameterGenerator.GenerateAddUserToApplicationComponentAndAccessLevelMappingParameters();
                        return new Action(() => { accessManager.AddUserToApplicationComponentAndAccessLevelMapping(parameters.Item1, parameters.Item2, parameters.Item3); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetUserToApplicationComponentAndAccessLevelMappings:
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateGetUserToGroupMappingsParameter();
                        return new Action(() => { Tuple<TComponent, TAccess> last = accessManager.GetUserToApplicationComponentAndAccessLevelMappings(parameter).Last(); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.RemoveUserToApplicationComponentAndAccessLevelMapping:
                    try
                    {
                        Tuple<TUser, TComponent, TAccess> parameters = parameterGenerator.GenerateRemoveUserToApplicationComponentAndAccessLevelMappingParameters();
                        return new Action(() => { accessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(parameters.Item1, parameters.Item2, parameters.Item3); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.AddGroupToApplicationComponentAndAccessLevelMapping:
                    try
                    {
                        Tuple<TGroup, TComponent, TAccess> parameters = parameterGenerator.GenerateAddGroupToApplicationComponentAndAccessLevelMappingParameters();
                        return new Action(() => { accessManager.AddGroupToApplicationComponentAndAccessLevelMapping(parameters.Item1, parameters.Item2, parameters.Item3); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetGroupToApplicationComponentAndAccessLevelMappings:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateGetGroupToGroupMappingsParameter();
                        return new Action(() => { Tuple<TComponent, TAccess> last = accessManager.GetGroupToApplicationComponentAndAccessLevelMappings(parameter).Last(); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.RemoveGroupToApplicationComponentAndAccessLevelMapping:
                    try
                    {
                        Tuple<TGroup, TComponent, TAccess> parameters = parameterGenerator.GenerateRemoveGroupToApplicationComponentAndAccessLevelMappingParameters();
                        return new Action(() => { accessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(parameters.Item1, parameters.Item2, parameters.Item3); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.AddEntityType:
                    try
                    {
                        String parameter = parameterGenerator.GenerateAddEntityTypeParameter();
                        return new Action(() => { accessManager.AddEntityType(parameter); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.ContainsEntityType:
                    try
                    {
                        String parameter = parameterGenerator.GenerateContainsEntityTypeParameter();
                        return new Action(() => { Boolean result = accessManager.ContainsEntityType(parameter); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.RemoveEntityType:
                    try
                    {
                        String parameter = parameterGenerator.GenerateRemoveEntityTypeParameter();
                        return new Action(() => { accessManager.RemoveEntityType(parameter); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.AddEntity:
                    try
                    {
                        Tuple<String, String> parameters = parameterGenerator.GenerateAddEntityParameters();
                        return new Action(() => { accessManager.AddEntity(parameters.Item1, parameters.Item2); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetEntities:
                    try
                    {
                        String parameter = parameterGenerator.GenerateGetEntitiesParameter();
                        return new Action(() => { accessManager.GetEntities(parameter); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.ContainsEntity:
                    try
                    {
                        Tuple<String, String> parameters = parameterGenerator.GenerateContainsEntityParameters();
                        return new Action(() => { Boolean result = accessManager.ContainsEntity(parameters.Item1, parameters.Item2); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.RemoveEntity:
                    try
                    {
                        Tuple<String, String> parameters = parameterGenerator.GenerateRemoveEntityParameters();
                        return new Action(() => { accessManager.RemoveEntity(parameters.Item1, parameters.Item2); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.AddUserToEntityMapping:
                    try
                    {
                        Tuple<TUser, String, String> parameters = parameterGenerator.GenerateAddUserToEntityMappingParameters();
                        return new Action(() => { accessManager.AddUserToEntityMapping(parameters.Item1, parameters.Item2, parameters.Item3); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetUserToEntityMappings:
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateGetUserToEntityMappingsParameter();
                        return new Action(() => { Tuple<String, String> last = accessManager.GetUserToEntityMappings(parameter).Last(); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetUserToEntityMappingsEntityTypeOverload:
                    try
                    {
                        Tuple<TUser, String> parameters = parameterGenerator.GenerateGetUserToEntityMappingsEntityTypeOverloadParameters();
                        return new Action(() => { String last = accessManager.GetUserToEntityMappings(parameters.Item1, parameters.Item2).Last(); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.RemoveUserToEntityMapping:
                    try
                    {
                        Tuple<TUser, String, String> parameters = parameterGenerator.GenerateRemoveUserToEntityMappingParameters();
                        return new Action(() => { accessManager.RemoveUserToEntityMapping(parameters.Item1, parameters.Item2, parameters.Item3); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.AddGroupToEntityMapping:
                    try
                    {
                        Tuple<TGroup, String, String> parameters = parameterGenerator.GenerateAddGroupToEntityMappingParameters();
                        return new Action(() => { accessManager.AddGroupToEntityMapping(parameters.Item1, parameters.Item2, parameters.Item3); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetGroupToEntityMappings:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateGetGroupToEntityMappingsParameter();
                        return new Action(() => { Tuple<String, String> last = accessManager.GetGroupToEntityMappings(parameter).Last(); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetGroupToEntityMappingsEntityTypeOverload:
                    try
                    {
                        Tuple<TGroup, String> parameters = parameterGenerator.GenerateGetGroupToEntityMappingsEntityTypeOverloadParameters();
                        return new Action(() => { String last = accessManager.GetGroupToEntityMappings(parameters.Item1, parameters.Item2).Last(); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.RemoveGroupToEntityMapping:
                    try
                    {
                        Tuple<TGroup, String, String> parameters = parameterGenerator.GenerateRemoveGroupToEntityMappingParameters();
                        return new Action(() => { accessManager.RemoveGroupToEntityMapping(parameters.Item1, parameters.Item2, parameters.Item3); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.HasAccessToApplicationComponent:
                    try
                    {
                        Tuple<TUser, TComponent, TAccess> parameters = parameterGenerator.GenerateHasAccessToApplicationComponentParameters();
                        return new Action(() => { Boolean result = accessManager.HasAccessToApplicationComponent(parameters.Item1, parameters.Item2, parameters.Item3); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.HasAccessToEntity:
                    try
                    {
                        Tuple<TUser, String, String> parameters = parameterGenerator.GenerateHasAccessToEntityParameters();
                        return new Action(() => { Boolean result = accessManager.HasAccessToEntity(parameters.Item1, parameters.Item2, parameters.Item3); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetApplicationComponentsAccessibleByUser:
                    try
                    {
                        TUser parameter = parameterGenerator.GenerateGetApplicationComponentsAccessibleByUserParameter();
                        return new Action(() => { HashSet<Tuple<TComponent, TAccess>> result = accessManager.GetApplicationComponentsAccessibleByUser(parameter); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetApplicationComponentsAccessibleByGroup:
                    try
                    {
                        TGroup parameter = parameterGenerator.GenerateGetApplicationComponentsAccessibleByGroupParameter();
                        return new Action(() => { HashSet<Tuple<TComponent, TAccess>> result = accessManager.GetApplicationComponentsAccessibleByGroup(parameter); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetEntitiesAccessibleByUser:
                    try
                    {
                        Tuple<TUser, String> parameters = parameterGenerator.GenerateGetEntitiesAccessibleByUserParameters();
                        return new Action(() => { HashSet<String> result = accessManager.GetEntitiesAccessibleByUser(parameters.Item1, parameters.Item2); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                case AccessManagerOperation.GetEntitiesAccessibleByGroup:
                    try
                    {
                        Tuple<TGroup, String> parameters = parameterGenerator.GenerateGetEntitiesAccessibleByGroupParameters();
                        return new Action(() => { HashSet<String> result = accessManager.GetEntitiesAccessibleByGroup(parameters.Item1, parameters.Item2); });
                    }
                    catch (Exception)
                    {
                        return new Action(() => { });
                    }

                default:
                    throw new Exception($"Encountered unhandled {typeof(AccessManagerOperation).Name} '{operation}'.");
            }
        }
        
    }
}
