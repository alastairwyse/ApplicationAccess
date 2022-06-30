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
using ApplicationMetrics;

namespace ApplicationAccess.Metrics
{
    /// <summary>
    /// Logs metric events for an implementation of IAccessManagerQueryProcessor.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the IAccessManagerQueryProcessor implementation.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the IAccessManagerQueryProcessor implementation.</typeparam>
    /// <typeparam name="TComponent">The type of components in the IAccessManagerQueryProcessor implementation.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access in the IAccessManagerQueryProcessor implementation.</typeparam>
    /// <remarks>Uses a facade pattern to front the IAccessManagerQueryProcessor, capturing metrics and forwarding method calls to the IAccessManagerQueryProcessor.</remarks>
    public class AccessManagerQueryProcessorMetricLogger<TUser, TGroup, TComponent, TAccess> : IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>The IAccessManagerQueryProcessor implementation to log metrics for.</summary>
        protected IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess> queryProcessor;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Metrics.AccessManagerQueryProcessorMetricLogger class.
        /// </summary>
        /// <param name="eventProcessor">The IAccessManagerQueryProcessor implementation to log metrics for.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public AccessManagerQueryProcessorMetricLogger(IAccessManagerQueryProcessor<TUser, TGroup, TComponent, TAccess> queryProcessor, IMetricLogger metricLogger)
        {
            this.queryProcessor = queryProcessor;
            this.metricLogger = metricLogger;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.Users"]/*'/>
        public IEnumerable<TUser> Users
        {
            get
            {
                IEnumerable<TUser> result;
                metricLogger.Begin(new UsersPropertyQueryTime());
                try
                {
                    result = queryProcessor.Users;
                }
                catch
                {
                    metricLogger.CancelBegin(new UsersPropertyQueryTime());
                    throw;
                }
                metricLogger.End(new UsersPropertyQueryTime());
                metricLogger.Increment(new UsersPropertyQueries());

                return result;
            }
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.Groups"]/*'/>
        public IEnumerable<TGroup> Groups
        {
            get
            {
                IEnumerable<TGroup> result;
                metricLogger.Begin(new GroupsPropertyQueryTime());
                try
                {
                    result = queryProcessor.Groups;
                }
                catch
                {
                    metricLogger.CancelBegin(new GroupsPropertyQueryTime());
                    throw;
                }
                metricLogger.End(new GroupsPropertyQueryTime());
                metricLogger.Increment(new GroupsPropertyQueries());

                return result;
            }
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.IAccessManagerQueryProcessor`4.EntityTypes"]/*'/>
        public IEnumerable<String> EntityTypes
        {
            get
            {
                IEnumerable<String> result;
                metricLogger.Begin(new EntityTypesPropertyQueryTime());
                try
                {
                    result = queryProcessor.EntityTypes;
                }
                catch
                {
                    metricLogger.CancelBegin(new EntityTypesPropertyQueryTime());
                    throw;
                }
                metricLogger.End(new EntityTypesPropertyQueryTime());
                metricLogger.Increment(new EntityTypesPropertyQueries());

                return result;
            }
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsUser(`0)"]/*'/>
        public Boolean ContainsUser(TUser user)
        {
            Boolean result;
            metricLogger.Begin(new ContainsUserQueryTime());
            try
            {
                result = queryProcessor.ContainsUser(user);
            }
            catch
            {
                metricLogger.CancelBegin(new ContainsUserQueryTime());
                throw;
            }
            metricLogger.End(new ContainsUserQueryTime());
            metricLogger.Increment(new ContainsUserQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsGroup(`1)"]/*'/>
        public Boolean ContainsGroup(TGroup group)
        {
            Boolean result;
            metricLogger.Begin(new ContainsGroupQueryTime());
            try
            {
                result = queryProcessor.ContainsGroup(group);
            }
            catch
            {
                metricLogger.CancelBegin(new ContainsGroupQueryTime());
                throw;
            }
            metricLogger.End(new ContainsGroupQueryTime());
            metricLogger.Increment(new ContainsGroupQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToGroupMappings(`0)"]/*'/>
        public IEnumerable<TGroup> GetUserToGroupMappings(TUser user)
        {
            IEnumerable<TGroup> result;
            metricLogger.Begin(new GetUserToGroupMappingsQueryTime());
            try
            {
                result = queryProcessor.GetUserToGroupMappings(user);
            }
            catch
            {
                metricLogger.CancelBegin(new GetUserToGroupMappingsQueryTime());
                throw;
            }
            metricLogger.End(new GetUserToGroupMappingsQueryTime());
            metricLogger.Increment(new GetUserToGroupMappingsQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToGroupMappings(`1)"]/*'/>
        public IEnumerable<TGroup> GetGroupToGroupMappings(TGroup group)
        {
            IEnumerable<TGroup> result;
            metricLogger.Begin(new GetGroupToGroupMappingsQueryTime());
            try
            {
                result = queryProcessor.GetGroupToGroupMappings(group);
            }
            catch
            {
                metricLogger.CancelBegin(new GetGroupToGroupMappingsQueryTime());
                throw;
            }
            metricLogger.End(new GetGroupToGroupMappingsQueryTime());
            metricLogger.Increment(new GetGroupToGroupMappingsQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToApplicationComponentAndAccessLevelMappings(`0)"]/*'/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetUserToApplicationComponentAndAccessLevelMappings(TUser user)
        {
            IEnumerable<Tuple<TComponent, TAccess>> result;
            metricLogger.Begin(new GetUserToApplicationComponentAndAccessLevelMappingsQueryTime());
            try
            {
                result = queryProcessor.GetUserToApplicationComponentAndAccessLevelMappings(user);
            }
            catch
            {
                metricLogger.CancelBegin(new GetUserToApplicationComponentAndAccessLevelMappingsQueryTime());
                throw;
            }
            metricLogger.End(new GetUserToApplicationComponentAndAccessLevelMappingsQueryTime());
            metricLogger.Increment(new GetUserToApplicationComponentAndAccessLevelMappingsQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToApplicationComponentAndAccessLevelMappings(`1)"]/*'/>
        public IEnumerable<Tuple<TComponent, TAccess>> GetGroupToApplicationComponentAndAccessLevelMappings(TGroup group)
        {
            IEnumerable<Tuple<TComponent, TAccess>> result;
            metricLogger.Begin(new GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime());
            try
            {
                result = queryProcessor.GetGroupToApplicationComponentAndAccessLevelMappings(group);
            }
            catch
            {
                metricLogger.CancelBegin(new GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime());
                throw;
            }
            metricLogger.End(new GetGroupToApplicationComponentAndAccessLevelMappingsQueryTime());
            metricLogger.Increment(new GetGroupToApplicationComponentAndAccessLevelMappingsQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsEntityType(System.String)"]/*'/>
        public Boolean ContainsEntityType(String entityType)
        {
            Boolean result;
            metricLogger.Begin(new ContainsEntityTypeQueryTime());
            try
            {
                result = queryProcessor.ContainsEntityType(entityType);
            }
            catch
            {
                metricLogger.CancelBegin(new ContainsEntityTypeQueryTime());
                throw;
            }
            metricLogger.End(new ContainsEntityTypeQueryTime());
            metricLogger.Increment(new ContainsEntityTypeQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetEntities(System.String)"]/*'/>
        public IEnumerable<String> GetEntities(String entityType)
        {
            IEnumerable<String> result;
            metricLogger.Begin(new GetEntitiesQueryTime());
            try
            {
                result = queryProcessor.GetEntities(entityType);
            }
            catch
            {
                metricLogger.CancelBegin(new GetEntitiesQueryTime());
                throw;
            }
            metricLogger.End(new GetEntitiesQueryTime());
            metricLogger.Increment(new GetEntitiesQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.ContainsEntity(System.String,System.String)"]/*'/>
        public Boolean ContainsEntity(String entityType, String entity)
        {
            Boolean result;
            metricLogger.Begin(new ContainsEntityQueryTime());
            try
            {
                result = queryProcessor.ContainsEntity(entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(new ContainsEntityQueryTime());
                throw;
            }
            metricLogger.End(new ContainsEntityQueryTime());
            metricLogger.Increment(new ContainsEntityQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToEntityMappings(`0)"]/*'/>
        public IEnumerable<Tuple<String, String>> GetUserToEntityMappings(TUser user)
        {
            IEnumerable<Tuple<String, String>> result;
            metricLogger.Begin(new GetUserToEntityMappingsForUserQueryTime());
            try
            {
                result = queryProcessor.GetUserToEntityMappings(user);
            }
            catch
            {
                metricLogger.CancelBegin(new GetUserToEntityMappingsForUserQueryTime());
                throw;
            }
            metricLogger.End(new GetUserToEntityMappingsForUserQueryTime());
            metricLogger.Increment(new GetUserToEntityMappingsForUserQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetUserToEntityMappings(`0,System.String)"]/*'/>
        public IEnumerable<String> GetUserToEntityMappings(TUser user, String entityType)
        {
            IEnumerable<String> result;
            metricLogger.Begin(new GetUserToEntityMappingsForUserAndEntityTypeQueryTime());
            try
            {
                result = queryProcessor.GetUserToEntityMappings(user, entityType);
            }
            catch
            {
                metricLogger.CancelBegin(new GetUserToEntityMappingsForUserAndEntityTypeQueryTime());
                throw;
            }
            metricLogger.End(new GetUserToEntityMappingsForUserAndEntityTypeQueryTime());
            metricLogger.Increment(new GetUserToEntityMappingsForUserAndEntityTypeQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToEntityMappings(`1)"]/*'/>
        public IEnumerable<Tuple<String, String>> GetGroupToEntityMappings(TGroup group)
        {
            IEnumerable<Tuple<String, String>> result;
            metricLogger.Begin(new GetGroupToEntityMappingsForGroupQueryTime());
            try
            {
                result = queryProcessor.GetGroupToEntityMappings(group);
            }
            catch
            {
                metricLogger.CancelBegin(new GetGroupToEntityMappingsForGroupQueryTime());
                throw;
            }
            metricLogger.End(new GetGroupToEntityMappingsForGroupQueryTime());
            metricLogger.Increment(new GetGroupToEntityMappingsForGroupQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetGroupToEntityMappings(`1,System.String)"]/*'/>
        public IEnumerable<String> GetGroupToEntityMappings(TGroup group, String entityType)
        {
            IEnumerable<String> result;
            metricLogger.Begin(new GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime());
            try
            {
                result = queryProcessor.GetGroupToEntityMappings(group, entityType);
            }
            catch
            {
                metricLogger.CancelBegin(new GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime());
                throw;
            }
            metricLogger.End(new GetGroupToEntityMappingsForGroupAndEntityTypeQueryTime());
            metricLogger.Increment(new GetGroupToEntityMappingsForGroupAndEntityTypeQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.HasAccess(`0,`2,`3)"]/*'/>
        public Boolean HasAccessToApplicationComponent(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            Boolean result;
            metricLogger.Begin(new HasAccessToApplicationComponentQueryTime());
            try
            {
                result = queryProcessor.HasAccessToApplicationComponent(user, applicationComponent, accessLevel);
            }
            catch
            {
                metricLogger.CancelBegin(new HasAccessToApplicationComponentQueryTime());
                throw;
            }
            metricLogger.End(new HasAccessToApplicationComponentQueryTime());
            metricLogger.Increment(new HasAccessToApplicationComponentQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.HasAccess(`0,System.String,System.String)"]/*'/>
        public Boolean HasAccessToEntity(TUser user, String entityType, String entity)
        {
            Boolean result;
            metricLogger.Begin(new HasAccessToEntityQueryTime());
            try
            {
                result = queryProcessor.HasAccessToEntity(user, entityType, entity);
            }
            catch
            {
                metricLogger.CancelBegin(new HasAccessToEntityQueryTime());
                throw;
            }
            metricLogger.End(new HasAccessToEntityQueryTime());
            metricLogger.Increment(new HasAccessToEntityQueries());

            return result;
        }

        /// <include file='..\ApplicationAccess\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.IAccessManagerQueryProcessor`4.GetAccessibleEntities(`0,System.String)"]/*'/>
        public HashSet<String> GetAccessibleEntities(TUser user, String entityType)
        {
            HashSet<String> result;
            metricLogger.Begin(new GetAccessibleEntitiesQueryTime());
            try
            {
                result = queryProcessor.GetAccessibleEntities(user, entityType);
            }
            catch
            {
                metricLogger.CancelBegin(new GetAccessibleEntitiesQueryTime());
                throw;
            }
            metricLogger.End(new GetAccessibleEntitiesQueryTime());
            metricLogger.Increment(new GetAccessibleEntitiesQueries());

            return result;
        }
    }
}
