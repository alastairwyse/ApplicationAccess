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

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Defines methods which query the state of group-based structures in a distributed implementation of an AccessManager.
    /// </summary>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IDistributedAccessManagerGroupQueryProcessor<TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Checks whether any of the specified groups have access to an application component at the specified level of access.
        /// </summary>
        /// <param name="groups">The groups to check for.</param>
        /// <param name="applicationComponent">The application component.</param>
        /// <param name="accessLevel">The level of access to the component.</param>
        /// <returns>True if any of the groups have access the component.  False otherwise.</returns>
        /// <remarks>Unlike the <see cref="IAccessManagerUserQueryProcessor{TUser, TGroup, TComponent, TAccess}.HasAccessToApplicationComponent(TUser, TComponent, TAccess)">'user' parameter overload of the method</see>, this method does not check access of groups indirectly mapped from the groups specified in the <paramref name="groups"/> parameter.</remarks>
        Boolean HasAccessToApplicationComponent(IEnumerable<TGroup> groups, TComponent applicationComponent, TAccess accessLevel);

        /// <summary>
        /// Checks whether any of the specified groups have access to the specified entity.
        /// </summary>
        /// <param name="groups">The groups to check for.</param>
        /// <param name="entityType">The type of the entity.</param>
        /// <param name="entity">The entity.</param>
        /// <returns>True if any of the groups have access the entity.  False otherwise.</returns>
        /// <remarks>Unlike the <see cref="IAccessManagerUserQueryProcessor{TUser, TGroup, TComponent, TAccess}.HasAccessToEntity(TUser, String, String)">'user' parameter overload of the method</see>, this method does not check access of groups indirectly mapped from the groups specified in the <paramref name="groups"/> parameter.</remarks>
        Boolean HasAccessToEntity(IEnumerable<TGroup> groups, String entityType, String entity);
    }
}
