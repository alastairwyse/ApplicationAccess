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
using Microsoft.Extensions.Configuration;

namespace ApplicationAccess.TestHarness.Configuration
{
    class ElementTargetStorageCountsConfigurationReader : ConfigurationReaderBase
    {
        protected const String usersPropertyName = "Users";
        protected const String groupsPropertyName = "Groups";
        protected const String userToGroupMapPropertyName = "UserToGroupMap";
        protected const String groupToGroupMapPropertyName = "GroupToGroupMap";
        protected const String userToComponentMapPropertyName = "UserToComponentMap";
        protected const String groupToComponentMapPropertyName = "GroupToComponentMap";
        protected const String entityTypesPropertyName = "EntityTypes";
        protected const String entitiesPropertyName = "Entities";
        protected const String userToEntityMapPropertyName = "UserToEntityMap";
        protected const String groupToEntityMapPropertyName = "GroupToEntityMap";

        public ElementTargetStorageCountsConfigurationReader()
            : base("Element target storage count")
        {
        }

        public ElementTargetStorageCountsConfiguration Read(IConfigurationSection configurationSection)
        {
            ThrowExceptionIfPropertyNotFound(usersPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(groupsPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(userToGroupMapPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(groupToGroupMapPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(userToComponentMapPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(groupToComponentMapPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(entityTypesPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(entitiesPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(userToEntityMapPropertyName, configurationSection);
            ThrowExceptionIfPropertyNotFound(groupToEntityMapPropertyName, configurationSection);

            var returnConfiguration = new ElementTargetStorageCountsConfiguration();

            returnConfiguration.Users = GetConfigurationValueAsInteger(usersPropertyName, configurationSection);
            returnConfiguration.Groups = GetConfigurationValueAsInteger(groupsPropertyName, configurationSection);
            returnConfiguration.UserToGroupMap = GetConfigurationValueAsInteger(userToGroupMapPropertyName, configurationSection);
            returnConfiguration.GroupToGroupMap = GetConfigurationValueAsInteger(groupToGroupMapPropertyName, configurationSection);
            returnConfiguration.UserToComponentMap = GetConfigurationValueAsInteger(userToComponentMapPropertyName, configurationSection);
            returnConfiguration.GroupToComponentMap = GetConfigurationValueAsInteger(groupToComponentMapPropertyName, configurationSection);
            returnConfiguration.EntityTypes = GetConfigurationValueAsInteger(entityTypesPropertyName, configurationSection);
            returnConfiguration.Entities = GetConfigurationValueAsInteger(entitiesPropertyName, configurationSection);
            returnConfiguration.UserToEntityMap = GetConfigurationValueAsInteger(userToEntityMapPropertyName, configurationSection);
            returnConfiguration.GroupToEntityMap = GetConfigurationValueAsInteger(groupToEntityMapPropertyName, configurationSection);

            return returnConfiguration;
        }
    }
}
