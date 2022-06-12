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
using ApplicationMetrics;

namespace ApplicationAccess.Serialization.Metrics
{
    #pragma warning disable 1591

    /// <summary>
    /// Count metric which records the total number of calls to the Serialize() method.
    /// </summary>
    public class AccessManagerSerializations : CountMetric
    {
        protected static String staticName = "AccessManagerSerializations";
        protected static String staticDescription = "The total number of calls to the Serialize() method";

        public AccessManagerSerializations()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the Serialize() method.
    /// </summary>
    public class AccessManagerSerializeTime : IntervalMetric
    {
        protected static String staticName = "AccessManagerSerializeTime";
        protected static String staticDescription = "The time taken to execute the Serialize() method";

        public AccessManagerSerializeTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the total number of calls to the Deserialize() method.
    /// </summary>
    public class AccessManagerDeserializations : CountMetric
    {
        protected static String staticName = "AccessManagerDeserializations";
        protected static String staticDescription = "The total number of calls to the Deserialize() method";

        public AccessManagerDeserializations()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to execute the Serialize() method.
    /// </summary>
    public class AccessManagerDeserializeTime : IntervalMetric
    {
        protected static String staticName = "AccessManagerDeserializeTime";
        protected static String staticDescription = "The time taken to execute the Deserialize() method";

        public AccessManagerDeserializeTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #pragma warning restore 1591
}
