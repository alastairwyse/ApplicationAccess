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

namespace ApplicationAccess
{
    /// <summary>
    /// Defines methods to manage the access of users and groups of users to components and entities within an application, without requiring dependent elements to be explicitly created (e.g. without requiring a user to be explicitly added before a user to group mapping is added which references that user).
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public interface IDependencyFreeAccessManager<TUser, TGroup, TComponent, TAccess> : IAccessManager<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Gets the groups that all of the specified groups are directly and indirectly mapped to.
        /// </summary>
        /// <param name="groups">The groups to retrieve the mapped groups for.</param>
        /// <returns>A collection of groups the specified groups are mapped to, and including the specified groups.</returns>
        /// <remarks>Note that <see cref="ArgumentException">ArgumentExceptions</see> are not thrown if any of the groups in <paramref name="groups"/> don't exist, since it's possible that groups could exist when this method call is initiated, but could be removed by a concurrent call whilst the initial call is being processed.</remarks>
        HashSet<TGroup> GetGroupToGroupMappings(IEnumerable<TGroup> groups);

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
