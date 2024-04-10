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

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Defines methods to to serialize AccessManager operations and their corresponding parameters.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    /// <typeparam name="TSerializedObject">The type of object to serialize to.</typeparam>
    public interface IParameterizedOperationSerializer<TUser, TGroup, TComponent, TAccess, TSerializedObject>
    {
        TSerializedObject Serialize(AccessManagerOperation operation, TUser user);

        TSerializedObject Serialize(AccessManagerOperation operation, TGroup group);

        TSerializedObject Serialize(AccessManagerOperation operation, TUser user, TGroup group);

        TSerializedObject Serialize(AccessManagerOperation operation, TGroup fromGroup, TGroup toGroup);

        TSerializedObject Serialize(AccessManagerOperation operation, TUser user, TComponent applicationComponent, TAccess accessLevel);

        TSerializedObject Serialize(AccessManagerOperation operation, TGroup group, TComponent applicationComponent, TAccess accessLevel);

        TSerializedObject Serialize(AccessManagerOperation operation, String entityType);

        TSerializedObject Serialize(AccessManagerOperation operation, String entityType, String entity);

        TSerializedObject Serialize(AccessManagerOperation operation, TUser user, String entityType, String entity);

        TSerializedObject Serialize(AccessManagerOperation operation, TUser user, String entityType);

        TSerializedObject Serialize(AccessManagerOperation operation, TGroup group, String entityType, String entity);

        TSerializedObject Serialize(AccessManagerOperation operation, TGroup group, String entityType);
    }
}
