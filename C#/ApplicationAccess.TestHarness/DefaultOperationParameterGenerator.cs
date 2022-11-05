﻿/*
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
    /// Default implementation of <see cref="IOperationParameterGenerator{TUser, TGroup, TComponent, TAccess}"/>.
    /// </summary>
    public class DefaultOperationParameterGenerator<TUser, TGroup, TComponent, TAccess> : IOperationParameterGenerator<TUser, TGroup, TComponent, TAccess>
    {
        protected Random randomGenerator;
        protected DataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer;
        protected INewUserGenerator<TUser> newUserGenerator;
        protected INewGroupGenerator<TGroup> newGroupGenerator;
        protected INewApplicationComponentGenerator<TComponent> newApplicationComponentGenerator;
        protected INewAccessLevelGenerator<TAccess> newwAccessLevelGenerator;
        protected INewEntityGenerator newEntityGenerator;
        protected INewEntityTypeGenerator newEntityTypeGenerator;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.DefaultOperationParameterGenerator class.
        /// </summary>
        /// <param name="dataElementStorer"></param>
        /// <param name="newUserGenerator">Generator for new users.</param>
        /// <param name="newGroupGenerator">Generator for new groups.</param>
        /// <param name="newApplicationComponentGenerator">Generator for new application components.</param>
        /// <param name="newwAccessLevelGenerator">Generator for new access levels.</param>
        /// <param name="newEntityGenerator">Generator for new entities.</param>
        /// <param name="newEntityTypeGenerator">Generator for new entity types.</param>
        public DefaultOperationParameterGenerator
        (
            DataElementStorer<TUser, TGroup, TComponent, TAccess> dataElementStorer,
            INewUserGenerator<TUser> newUserGenerator,
            INewGroupGenerator<TGroup> newGroupGenerator,
            INewApplicationComponentGenerator<TComponent> newApplicationComponentGenerator,
            INewAccessLevelGenerator<TAccess> newwAccessLevelGenerator,
            INewEntityGenerator newEntityGenerator,
            INewEntityTypeGenerator newEntityTypeGenerator
        )
        {
            randomGenerator = new Random();
            this.dataElementStorer = dataElementStorer;
            this.newUserGenerator = newUserGenerator;
            this.newGroupGenerator = newGroupGenerator;
            this.newApplicationComponentGenerator = newApplicationComponentGenerator;
            this.newwAccessLevelGenerator = newwAccessLevelGenerator;
            this.newEntityGenerator = newEntityGenerator;
            this.newEntityTypeGenerator = newEntityTypeGenerator;
        }

        public TUser GenerateAddUserParameter()
        {
            return newUserGenerator.Generate();
        }

        public TUser GenerateContainsUserParameter()
        {
            if (randomGenerator.Next(2) == 0)
            {
                return newUserGenerator.Generate();
            }
            else
            {
                return dataElementStorer.GetRandomUser();
            }
        }

        public TUser GenerateRemoveUserParameter()
        {
            return dataElementStorer.GetRandomUser();
        }

        public TGroup GenerateAddGroupParameter()
        {
            return newGroupGenerator.Generate();
        }

        public TGroup GenerateContainsGroupParameter()
        {
            if (randomGenerator.Next(2) == 0)
            {
                return newGroupGenerator.Generate();
            }
            else
            {
                return dataElementStorer.GetRandomGroup();
            }
        }

        public TGroup GenerateRemoveGroupParameter()
        {
            return dataElementStorer.GetRandomGroup();
        }

        public Tuple<TUser, TGroup> GenerateAddUserToGroupMappingParameters()
        {
            TUser user = dataElementStorer.GetRandomUser();
            TGroup group = dataElementStorer.GetRandomGroup();

            return new Tuple<TUser, TGroup>(user, group);
        }

        public TUser GenerateGetUserToGroupMappingsParameter()
        {
            return dataElementStorer.GetRandomUser();
        }

        public Tuple<TUser, TGroup> GenerateRemoveUserToGroupMappingParameters()
        {
            return dataElementStorer.GetRandomUserToGroupMapping();
        }

        public Tuple<TGroup, TGroup> GenerateAddGroupToGroupMappingParameters()
        {
            TGroup fromGroup = dataElementStorer.GetRandomGroup();
            TGroup toGroup = dataElementStorer.GetRandomGroup();

            return new Tuple<TGroup, TGroup>(fromGroup, toGroup);
        }

        public TGroup GenerateGetGroupToGroupMappingsParameter()
        {
            return dataElementStorer.GetRandomGroup();
        }

        public Tuple<TGroup, TGroup> GenerateRemoveGroupToGroupMappingParameters()
        {
            return dataElementStorer.GetRandomGroupToGroupMapping();
        }

        public Tuple<TUser, TComponent, TAccess> GenerateAddUserToApplicationComponentAndAccessLevelMappingParameters()
        {
            TUser user = dataElementStorer.GetRandomUser();
            TComponent applicationComponent = newApplicationComponentGenerator.Generate();
            TAccess accessLevel = newwAccessLevelGenerator.Generate();

            return new Tuple<TUser, TComponent, TAccess>(user, applicationComponent, accessLevel);
        }

        public TUser GenerateGetUserToApplicationComponentAndAccessLevelMappingsParameter()
        {
            return dataElementStorer.GetRandomUser();
        }

        public Tuple<TUser, TComponent, TAccess> GenerateRemoveUserToApplicationComponentAndAccessLevelMappingParameters()
        {
            return dataElementStorer.GetRandomUserToApplicationComponentAndAccessLevelMapping();
        }

        public Tuple<TGroup, TComponent, TAccess> GenerateAddGroupToApplicationComponentAndAccessLevelMappingParameters()
        {
            TGroup group = dataElementStorer.GetRandomGroup();
            TComponent applicationComponent = newApplicationComponentGenerator.Generate();
            TAccess accessLevel = newwAccessLevelGenerator.Generate();

            return new Tuple<TGroup, TComponent, TAccess>(group, applicationComponent, accessLevel);
        }

        public TGroup GenerateGetGroupToApplicationComponentAndAccessLevelMappingsParameter()
        {
            return dataElementStorer.GetRandomGroup();
        }

        public Tuple<TGroup, TComponent, TAccess> GenerateRemoveGroupToApplicationComponentAndAccessLevelMappingParameters()
        {
            return dataElementStorer.GetRandomGroupToApplicationComponentAndAccessLevelMapping();
        }

        public String GenerateAddEntityTypeParameter()
        {
            return newEntityTypeGenerator.Generate();
        }

        public String GenerateContainsEntityTypeParameter()
        {
            if (randomGenerator.Next(2) == 0)
            {
                return newEntityTypeGenerator.Generate();
            }
            else
            {
                return dataElementStorer.GetRandomEntityType();
            }
        }

        public String GenerateRemoveEntityTypeParameter()
        {
            return dataElementStorer.GetRandomEntityType();
        }

        public Tuple<String, String> GenerateAddEntityParameters()
        {
            String entityType = dataElementStorer.GetRandomEntityType();
            String entity = newEntityGenerator.Generate();

            return new Tuple<String, String>(entityType, entity);
        }

        public String GenerateGetEntitiesParameter()
        {
            return dataElementStorer.GetRandomEntityType();
        }

        public Tuple<String, String> GenerateContainsEntityParameters()
        {
            if (randomGenerator.Next(2) == 0)
            {
                String entityType = newEntityTypeGenerator.Generate();
                String entity = newEntityGenerator.Generate();

                return new Tuple<String, String>(entityType, entity);
            }
            else
            {
                return dataElementStorer.GetRandomEntity();
            }
        }

        public Tuple<String, String> GenerateRemoveEntityParameters()
        {
            return dataElementStorer.GetRandomEntity();
        }

        public Tuple<TUser, String, String> GenerateAddUserToEntityMappingParameters()
        {
            TUser user = dataElementStorer.GetRandomUser();
            Tuple<String, String> entity = dataElementStorer.GetRandomEntity();

            return new Tuple<TUser, String, String>(user, entity.Item1, entity.Item2);
        }

        public TUser GenerateGetUserToEntityMappingsParameter()
        {
            return dataElementStorer.GetRandomUser();
        }

        public Tuple<TUser, String> GenerateGetUserToEntityMappingsEntityTypeOverloadParameters()
        {
            TUser user = dataElementStorer.GetRandomUser();
            String entityType = dataElementStorer.GetRandomEntityType();

            return new Tuple<TUser, String>(user, entityType);
        }

        public Tuple<TUser, String, String> GenerateRemoveUserToEntityMappingParameters()
        {
            return dataElementStorer.GetRandomUserToEntityMapping();
        }

        public Tuple<TGroup, String, String> GenerateAddGroupToEntityMappingParameters()
        {
            TGroup group = dataElementStorer.GetRandomGroup();
            Tuple<String, String> entity = dataElementStorer.GetRandomEntity();

            return new Tuple<TGroup, String, String>(group, entity.Item1, entity.Item2);
        }

        public TGroup GenerateGetGroupToEntityMappingsParameter()
        {
            return dataElementStorer.GetRandomGroup();
        }

        public Tuple<TGroup, String> GenerateGetGroupToEntityMappingsEntityTypeOverloadParameters()
        {
            TGroup group = dataElementStorer.GetRandomGroup();
            String entityType = dataElementStorer.GetRandomEntityType();

            return new Tuple<TGroup, String>(group, entityType);
        }

        public Tuple<TGroup, String, String> GenerateRemoveGroupToEntityMappingParameters()
        {
            return dataElementStorer.GetRandomGroupToEntityMapping();
        }

        public Tuple<TUser, TComponent, TAccess> GenerateHasAccessToApplicationComponentParameters()
        {
            TUser user = dataElementStorer.GetRandomUser();
            TComponent applicationComponent = newApplicationComponentGenerator.Generate();
            TAccess accessLevel = newwAccessLevelGenerator.Generate();

            return new Tuple<TUser, TComponent, TAccess>(user, applicationComponent, accessLevel);
        }

        public Tuple<TUser, String, String> GenerateHasAccessToEntityParameters()
        {
            TUser user = dataElementStorer.GetRandomUser();
            Tuple<String, String> entity = dataElementStorer.GetRandomEntity();

            return new Tuple<TUser, String, String>(user, entity.Item1, entity.Item2);
        }

        public TUser GenerateGetApplicationComponentsAccessibleByUserParameter()
        {
            return dataElementStorer.GetRandomUser();
        }

        public TGroup GenerateGetApplicationComponentsAccessibleByGroupParameter()
        {
            return dataElementStorer.GetRandomGroup();
        }

        public Tuple<TUser, String> GenerateGetEntitiesAccessibleByUserParameters()
        {
            TUser user =  dataElementStorer.GetRandomUser();
            String entityType = dataElementStorer.GetRandomEntityType();

            return new Tuple<TUser, String>(user, entityType);
        }

        public Tuple<TGroup, String> GenerateGetEntitiesAccessibleByGroupParameters()
        {
            TGroup group = dataElementStorer.GetRandomGroup();
            String entityType = dataElementStorer.GetRandomEntityType();

            return new Tuple<TGroup, String>(group, entityType);
        }
    }
}