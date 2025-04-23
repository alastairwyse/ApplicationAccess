/*
 * Copyright 2025 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Redistribution.Kubernetes.Metrics
{
    #pragma warning disable 1591

    /// <summary>
    /// Count metric which records that a shard group was scaled down in a distributed AccessManager implementation.
    /// </summary>
    public class ShardGroupScaledDown : CountMetric
    {
        protected static String staticName = "ShardGroupScaledDown";
        protected static String staticDescription = "A shard group was scaled down in a distributed AccessManager implementation";

        public ShardGroupScaledDown()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that a shard group was scaled up in a distributed AccessManager implementation.
    /// </summary>
    public class ShardGroupScaledUp : CountMetric
    {
        protected static String staticName = "ShardGroupScaledUp";
        protected static String staticDescription = "A shard group was scaled up in a distributed AccessManager implementation";

        public ShardGroupScaledUp()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that a load balancer service was created in a distributed AccessManager implementation.
    /// </summary>
    public class LoadBalancerServiceCreated : CountMetric
    {
        protected static String staticName = "LoadBalancerServiceCreated";
        protected static String staticDescription = "A load balancer service was created in a distributed AccessManager implementation";

        public LoadBalancerServiceCreated()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to scale down a shard group in a distributed AccessManager implementation.
    /// </summary>
    public class ShardGroupScaleDownTime : IntervalMetric
    {
        protected static String staticName = "ShardGroupScaleDownTime";
        protected static String staticDescription = "The time taken to scale down a shard group in a distributed AccessManager implementation";

        public ShardGroupScaleDownTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to scale up a shard group in a distributed AccessManager implementation.
    /// </summary>
    public class ShardGroupScaleUpTime : IntervalMetric
    {
        protected static String staticName = "ShardGroupScaleUpTime";
        protected static String staticDescription = "The time taken to scale up a shard group in a distributed AccessManager implementation";

        public ShardGroupScaleUpTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to create a load balancer service in a distributed AccessManager implementation.
    /// </summary>
    public class LoadBalancerServiceCreateTime : IntervalMetric
    {
        protected static String staticName = "LoadBalancerServiceCreateTime";
        protected static String staticDescription = "The time taken to create a load balancer service in a distributed AccessManager implementation";

        public LoadBalancerServiceCreateTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #pragma warning restore 1591
}
