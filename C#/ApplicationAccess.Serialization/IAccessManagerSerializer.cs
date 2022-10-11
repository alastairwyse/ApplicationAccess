/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Serialization
{
    /// <summary>
    /// Defines methods to serialize and deserialize an AccessManager.
    /// </summary>
    /// <typeparam name="TSerializedObject">The type of object to serialize to and from.</typeparam>
    public interface IAccessManagerSerializer<TSerializedObject>
    {
        /// <summary>
        /// Serializes the specified access manager.
        /// </summary>
        /// <typeparam name="TUser">The type of users stored in the access manager.</typeparam>
        /// <typeparam name="TGroup">The type of groups stored in the access manager.</typeparam>
        /// <typeparam name="TComponent">The type of application components stored in the access manager.</typeparam>
        /// <typeparam name="TAccess">The type of access levels stored in the access manager.</typeparam>
        /// <param name="accessManager">The access manager to serialize.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <returns>An object representing the access manager.</returns>
        TSerializedObject Serialize<TUser, TGroup, TComponent, TAccess>
        (
            AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManager, 
            IUniqueStringifier<TUser> userStringifier, 
            IUniqueStringifier<TGroup> groupStringifier, 
            IUniqueStringifier<TComponent> applicationComponentStringifier, 
            IUniqueStringifier<TAccess> accessLevelStringifier
        );

        /// <summary>
        /// Deserializes an access manager.
        /// </summary>
        /// <typeparam name="TUser"> The type of users stored in the access manager.</typeparam>
        /// <typeparam name="TGroup"> The type of groups stored in the access manager.</typeparam>
        /// <typeparam name="TComponent"> The type of application components stored in the access manager.</typeparam>
        /// <typeparam name="TAccess"> The type of access levels stored in the access manager.</typeparam>
        /// <param name="serializedAccessManager">The object to deserialize the access manager from.</param>
        /// <param name="userStringifier">A string converter for users.</param>
        /// <param name="groupStringifier">A string converter for groups.</param>
        /// <param name="applicationComponentStringifier">A string converter for application components.</param>
        /// <param name="accessLevelStringifier">A string converter for access levels.</param>
        /// <param name="accessManagerToDeserializeTo">The AccessManager instance to deserialize to.</param>
        /// <remarks>
        ///   <para>Any existing items and mappings stored in parameter 'accessManagerToDeserializeTo' will be cleared.</para>
        ///   <para>The AccessManager instance is passed as a parameter rather than returned from the method, to allow deserializing into types derived from AccessManager aswell as AccessManager itself.</para>
        /// </remarks>
        void Deserialize<TUser, TGroup, TComponent, TAccess>
        (
            TSerializedObject serializedAccessManager,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToDeserializeTo
        );
    }
}
