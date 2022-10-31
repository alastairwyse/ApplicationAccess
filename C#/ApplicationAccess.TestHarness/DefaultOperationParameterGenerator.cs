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
using System.Text;

namespace ApplicationAccess.TestHarness
{
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
            throw new NotImplementedException();
        }

        public TUser GenerateGetUserToGroupMappingsParameter()
        {
            throw new NotImplementedException();
        }

        public Tuple<TUser, TGroup> GenerateRemoveUserToGroupMappingParameters()
        {
            throw new NotImplementedException();
        }

        public Tuple<TGroup, TGroup> GenerateAddGroupToGroupMappingParameters()
        {
            throw new NotImplementedException();
        }

        public TGroup GenerateGetGroupToGroupMappingsParameter()
        {
            throw new NotImplementedException();
        }

        public Tuple<TGroup, TGroup> GenerateRemoveGroupToGroupMappingParameters()
        {
            throw new NotImplementedException();
        }

        public Tuple<TUser, TComponent, TAccess> GenerateAddUserToApplicationComponentAndAccessLevelMappingParameters()
        {
            throw new NotImplementedException();
        }

        public TUser GenerateGetUserToApplicationComponentAndAccessLevelMappingsParameter()
        {
            throw new NotImplementedException();
        }

        public Tuple<TUser, TComponent, TAccess> GenerateRemoveUserToApplicationComponentAndAccessLevelMappingParameters()
        {
            throw new NotImplementedException();
        }

        public Tuple<TGroup, TComponent, TAccess> GenerateAddGroupToApplicationComponentAndAccessLevelMappingParameters()
        {
            throw new NotImplementedException();
        }

        public TGroup GenerateGetGroupToApplicationComponentAndAccessLevelMappingsParameter()
        {
            throw new NotImplementedException();
        }

        public Tuple<TGroup, TComponent, TAccess> GenerateRemoveGroupToApplicationComponentAndAccessLevelMappingParameters()
        {
            throw new NotImplementedException();
        }

        public String GenerateAddEntityTypeParameter()
        {
            throw new NotImplementedException();
        }

        public String GenerateContainsEntityTypeParameter()
        {
            throw new NotImplementedException();
        }

        public String GenerateRemoveEntityTypeParameter()
        {
            throw new NotImplementedException();
        }

        public Tuple<String, String> GenerateAddEntityParameters()
        {
            throw new NotImplementedException();
        }

        public String GenerateGetEntitiesParameter()
        {
            throw new NotImplementedException();
        }

        public Tuple<String, String> GenerateContainsKeyParameters()
        {
            throw new NotImplementedException();
        }

        public Tuple<String, String> GenerateRemoveEntityParameters()
        {
            throw new NotImplementedException();
        }

        public Tuple<TUser, String, String> GenerateAddUserToEntityMappingParameters()
        {
            throw new NotImplementedException();
        }

        public TUser GenerateGetUserToEntityMappingsParameter()
        {
            throw new NotImplementedException();
        }

        public Tuple<TUser, String> GenerateGetUserToEntityMappingsEntityTypeOverloadParameters()
        {
            throw new NotImplementedException();
        }

        public Tuple<TUser, String, String> GenerateRemoveUserToEntityMappingParameters()
        {
            throw new NotImplementedException();
        }

        public Tuple<TGroup, String, String> GenerateAddGroupToEntityMappingParameters()
        {
            throw new NotImplementedException();
        }

        public TGroup GenerateGetGroupToEntityMappingsParameter()
        {
            throw new NotImplementedException();
        }

        public Tuple<TGroup, String> GenerateGetGroupToEntityMappingsEntityTypeOverloadParameters()
        {
            throw new NotImplementedException();
        }

        public Tuple<TGroup, String, String> GenerateRemoveGroupToEntityMappingParameters()
        {
            throw new NotImplementedException();
        }

        public Tuple<TUser, TComponent, TAccess> GenerateHasAccessToApplicationComponentParameters()
        {
            throw new NotImplementedException();
        }

        public Tuple<TUser, String, String> GenerateHasAccessToEntityParameters()
        {
            throw new NotImplementedException();
        }

        public TUser GenerateGetApplicationComponentsAccessibleByUserParameter()
        {
            throw new NotImplementedException();
        }

        public TGroup GenerateGetApplicationComponentsAccessibleByGroupParameter()
        {
            throw new NotImplementedException();
        }

        public Tuple<TUser, String> GenerateGetEntitiesAccessibleByUserParameters()
        {
            throw new NotImplementedException();
        }

        public Tuple<TGroup, String> GenerateGetEntitiesAccessibleByGroupParameters()
        {
            throw new NotImplementedException();
        }
    }
}
