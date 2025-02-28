﻿/*
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
    /// Count metric which records that routing functionality was switched on.
    /// </summary>
    public class RoutingSwitchedOn : CountMetric
    {
        protected static String staticName = "RoutingSwitchedOn";
        protected static String staticDescription = "Routing functionality was switched on";

        public RoutingSwitchedOn()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that routing functionality was switched off.
    /// </summary>
    public class RoutingSwitchedOff : CountMetric
    {
        protected static String staticName = "RoutingSwitchedOff";
        protected static String staticDescription = "Routing functionality was switched off";

        public RoutingSwitchedOff()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that operation processing in a router component was paused.
    /// </summary>
    public class RouterPaused : CountMetric
    {
        protected static String staticName = "RouterPaused";
        protected static String staticDescription = "Operation processing in a router component was paused";

        public RouterPaused()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that operation processing in a router component was resumed.
    /// </summary>
    public class RouterResumed : CountMetric
    {
        protected static String staticName = "RouterResumed";
        protected static String staticDescription = "Operation processing in a router component was resumed";

        public RouterResumed()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a buffer flush operation triggered by a manual method call.
    /// </summary>
    public class BufferFlushOperationTriggeredByManualAction : CountMetric
    {
        protected static String staticName = "BufferFlushOperationTriggeredByManualAction";
        protected static String staticDescription = "A buffer flush operation triggered by a manual method call";

        public BufferFlushOperationTriggeredByManualAction()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #pragma warning restore 1591
}
