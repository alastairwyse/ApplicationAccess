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

    #pragma warning restore 1591
}
