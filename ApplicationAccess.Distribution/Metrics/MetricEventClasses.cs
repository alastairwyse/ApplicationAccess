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
using ApplicationMetrics;

namespace ApplicationAccess.Distribution.Metrics
{
    #pragma warning disable 1591

    /// <summary>
    /// Count metric which records a call to the RefreshConfiguration() method where the shard configuration was subsequently refreshed.
    /// </summary>
    public class ConfigurationRefreshed : CountMetric
    {
        protected static String staticName = "ConfigurationRefreshed";
        protected static String staticDescription = "A call to the RefreshConfiguration() method where the shard configuration was subsequently refreshed";

        public ConfigurationRefreshed()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to refresh the shard configuration in the RefreshConfiguration() method.
    /// </summary>
    public class ConfigurationRefreshTime : IntervalMetric
    {
        protected static String staticName = "ConfigurationRefreshTime";
        protected static String staticDescription = "The time taken to refresh the shard configuration in the RefreshConfiguration() method";

        public ConfigurationRefreshTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }
    
    public class GetUserToGroupMappingsGroupsMappedToUser : AmountMetric
    {
        protected static String staticName = "GetUserToGroupMappingsGroupsMappedToUser";
        protected static String staticDescription = "The total number of groups directly and indirectly mapped to a user, found as part of a distributed GetUserToGroupMappings() method call with the 'includeIndirectMappings' parameter set true";

        public GetUserToGroupMappingsGroupsMappedToUser()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the total number of groups directly and indirectly mapped to a user, found as part of a distributed HasAccessToApplicationComponent() method call.
    /// </summary>
    public class HasAccessToApplicationComponentGroupsMappedToUser : AmountMetric
    {
        protected static String staticName = "HasAccessToApplicationComponentGroupsMappedToUser";
        protected static String staticDescription = "The total number of groups directly and indirectly mapped to a user, found as part of a distributed HasAccessToApplicationComponent() method call";

        public HasAccessToApplicationComponentGroupsMappedToUser()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of group shards queried as part of a distributed HasAccessToApplicationComponent() method call.
    /// </summary>
    public class HasAccessToApplicationComponentGroupShardsQueried : AmountMetric
    {
        protected static String staticName = "HasAccessToApplicationComponentGroupShardsQueried";
        protected static String staticDescription = "The number of group shards queried as part of a distributed HasAccessToApplicationComponent() method call";

        public HasAccessToApplicationComponentGroupShardsQueried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of group shards queried as part of a distributed HasAccessToApplicationComponent() method call overload with 'groups' parameter.
    /// </summary>
    public class HasAccessToApplicationComponentForGroupsGroupShardsQueried : AmountMetric
    {
        protected static String staticName = "HasAccessToApplicationComponentForGroupsGroupShardsQueried";
        protected static String staticDescription = "The number of group shards queried as part of a distributed HasAccessToApplicationComponent() method call overload with 'groups' parameter";

        public HasAccessToApplicationComponentForGroupsGroupShardsQueried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the total number of groups directly and indirectly mapped to a user, found as part of a distributed HasAccessToEntity() method call.
    /// </summary>
    public class HasAccessToEntityGroupsMappedToUser : AmountMetric
    {
        protected static String staticName = "HasAccessToEntityGroupsMappedToUser";
        protected static String staticDescription = "The total number of groups directly and indirectly mapped to a user, found as part of a distributed HasAccessToEntity() method call";

        public HasAccessToEntityGroupsMappedToUser()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of group shards queried as part of a distributed HasAccessToEntity() method call.
    /// </summary>
    public class HasAccessToEntityGroupShardsQueried : AmountMetric
    {
        protected static String staticName = "HasAccessToEntityGroupShardsQueried";
        protected static String staticDescription = "The number of group shards queried as part of a distributed HasAccessToEntity() method call";

        public HasAccessToEntityGroupShardsQueried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of group shards queried as part of a distributed HasAccessToEntity() method call overload with 'groups' parameter.
    /// </summary>
    public class HasAccessToEntityForGroupsGroupShardsQueried : AmountMetric
    {
        protected static String staticName = "HasAccessToEntityForGroupsGroupShardsQueried";
        protected static String staticDescription = "The number of group shards queried as part of a distributed HasAccessToEntity() method call overload with 'groups' parameter";

        public HasAccessToEntityForGroupsGroupShardsQueried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the total number of groups directly and indirectly mapped to a user, found as part of a distributed GetApplicationComponentsAccessibleByUser() method call.
    /// </summary>
    public class GetApplicationComponentsAccessibleByUserGroupsMappedToUser : AmountMetric
    {
        protected static String staticName = "GetApplicationComponentsAccessibleByUserGroupsMappedToUser";
        protected static String staticDescription = "The total number of groups directly and indirectly mapped to a user, found as part of a distributed GetApplicationComponentsAccessibleByUser() method call";

        public GetApplicationComponentsAccessibleByUserGroupsMappedToUser()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of group shards queried as part of a distributed GetApplicationComponentsAccessibleByUser() method call.
    /// </summary>
    public class GetApplicationComponentsAccessibleByUserGroupShardsQueried : AmountMetric
    {
        protected static String staticName = "GetApplicationComponentsAccessibleByUserGroupShardsQueried";
        protected static String staticDescription = "The number of group shards queried as part of a distributed GetApplicationComponentsAccessibleByUser() method call";

        public GetApplicationComponentsAccessibleByUserGroupShardsQueried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the total number of groups directly and indirectly mapped to a group, found as part of a distributed GetApplicationComponentsAccessibleByGroup() method call.
    /// </summary>
    public class GetApplicationComponentsAccessibleByGroupGroupsMappedToGroup : AmountMetric
    {
        protected static String staticName = "GetApplicationComponentsAccessibleByGroupGroupsMappedToGroup";
        protected static String staticDescription = "The total number of groups directly and indirectly mapped to a group, found as part of a distributed GetApplicationComponentsAccessibleByGroup() method call";

        public GetApplicationComponentsAccessibleByGroupGroupsMappedToGroup()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of group shards queried as part of a distributed GetApplicationComponentsAccessibleByGroup() method call.
    /// </summary>
    public class GetApplicationComponentsAccessibleByGroupGroupShardsQueried : AmountMetric
    {
        protected static String staticName = "GetApplicationComponentsAccessibleByGroupGroupShardsQueried";
        protected static String staticDescription = "The number of group shards queried as part of a distributed GetApplicationComponentsAccessibleByGroup() method call";

        public GetApplicationComponentsAccessibleByGroupGroupShardsQueried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of group shards queried as part of a distributed GetApplicationComponentsAccessibleByGroups() method call.
    /// </summary>
    public class GetApplicationComponentsAccessibleByGroupsGroupShardsQueried : AmountMetric
    {
        protected static String staticName = "GetApplicationComponentsAccessibleByGroupsGroupShardsQueried";
        protected static String staticDescription = "The number of group shards queried as part of a routed GetApplicationComponentsAccessibleByGroups() method call";

        public GetApplicationComponentsAccessibleByGroupsGroupShardsQueried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the total number of groups directly and indirectly mapped to a user, found as part of a distributed GetEntitiesAccessibleByUser() method call.
    /// </summary>
    public class GetEntitiesAccessibleByUserGroupsMappedToUser : AmountMetric
    {
        protected static String staticName = "GetEntitiesAccessibleByUserGroupsMappedToUser";
        protected static String staticDescription = "The total number of groups directly and indirectly mapped to a user, found as part of a distributed GetEntitiesAccessibleByUser() method call";

        public GetEntitiesAccessibleByUserGroupsMappedToUser()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of group shards queried as part of a distributed GetEntitiesAccessibleByUser() method call.
    /// </summary>
    public class GetEntitiesAccessibleByUserGroupShardsQueried : AmountMetric
    {
        protected static String staticName = "GetEntitiesAccessibleByUserGroupShardsQueried";
        protected static String staticDescription = "The number of group shards queried as part of a distributed GetEntitiesAccessibleByUser() method call";

        public GetEntitiesAccessibleByUserGroupShardsQueried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the total number of groups directly and indirectly mapped to a group, found as part of a distributed GetEntitiesAccessibleByGroup() method call.
    /// </summary>
    public class GetEntitiesAccessibleByGroupGroupsMappedToGroup : AmountMetric
    {
        protected static String staticName = "GetEntitiesAccessibleByGroupGroupsMappedToGroup";
        protected static String staticDescription = "The total number of groups directly and indirectly mapped to a group, found as part of a distributed GetEntitiesAccessibleByGroup() method call";

        public GetEntitiesAccessibleByGroupGroupsMappedToGroup()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of group shards queried as part of a distributed GetEntitiesAccessibleByGroup() method call.
    /// </summary>
    public class GetEntitiesAccessibleByGroupGroupShardsQueried : AmountMetric
    {
        protected static String staticName = "GetEntitiesAccessibleByGroupGroupShardsQueried";
        protected static String staticDescription = "The number of group shards queried as part of a distributed GetEntitiesAccessibleByGroup() method call";

        public GetEntitiesAccessibleByGroupGroupShardsQueried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of group shards queried as part of a distributed GetEntitiesAccessibleByGroups() method call.
    /// </summary>
    public class GetEntitiesAccessibleByGroupsGroupShardsQueried : AmountMetric
    {
        protected static String staticName = "GetEntitiesAccessibleByGroupsGroupShardsQueried";
        protected static String staticDescription = "The number of group shards queried as part of a distributed GetEntitiesAccessibleByGroups() method call";

        public GetEntitiesAccessibleByGroupsGroupShardsQueried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #pragma warning restore 1591
}
