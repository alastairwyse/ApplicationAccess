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

namespace ApplicationAccess.Redistribution.Metrics
{
    // TODO: Some of these metrics are Kubernetes specific

    #pragma warning disable 1591

    /// <summary>
    /// Amount metric which records the number of events copied from one or more source shard groups to a target shard group as part of a shard group split or merge operation.
    /// </summary>
    public class EventsCopiedFromSourceToTargetShardGroup : AmountMetric
    {
        protected static String staticName = "EventsCopiedFromSourceToTargetShardGroup";
        protected static String staticDescription = "The number of events copied from one or more source shard groups to a target shard group as part of a shard group split or merge operation";

        public EventsCopiedFromSourceToTargetShardGroup()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records completion of copying a batch of events from a source to a target shard group as part of a shard group split operation.
    /// </summary>
    public class EventBatchCopyCompleted : CountMetric
    {
        protected static String staticName = "EventBatchCopyCompleted";
        protected static String staticDescription = "Completion of copying a batch of events from a source to a target shard group as part of a shard group split operation";

        public EventBatchCopyCompleted()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to read a batch of events from a source shard group as part of a shard group split operation.
    /// </summary>
    public class EventBatchReadTime : IntervalMetric
    {
        protected static String staticName = "EventBatchReadTime";
        protected static String staticDescription = "The time taken to read a batch of events from a source shard group as part of a shard group split operation";

        public EventBatchReadTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to write a batch of events to a target shard group as part of a shard group split operation.
    /// </summary>
    public class EventBatchWriteTime : IntervalMetric
    {
        protected static String staticName = "EventBatchWriteTime";
        protected static String staticDescription = "The time taken to write a batch of events to a target shard group as part of a shard group split operation";

        public EventBatchWriteTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to copy events from a source shard group to a target shard group as part of a shard group split operation.
    /// </summary>
    public class EventCopyTime : IntervalMetric
    {
        protected static String staticName = "EventCopyTime";
        protected static String staticDescription = "The time taken to copy events from a source shard group to a target shard group as part of a shard group split operation";

        public EventCopyTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to delete events from a source shard group as part of a shard group split operation.
    /// </summary>
    public class EventDeleteTime : IntervalMetric
    {
        protected static String staticName = "EventDeleteTime";
        protected static String staticDescription = "The time taken to delete events from a source shard group as part of a shard group split operation";

        public EventDeleteTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of active events being processed by the writer node in a shard group of a distributed AccessManager implementation.
    /// </summary>
    public class WriterNodeEventProcessingCount : StatusMetric
    {
        protected static String staticName = "WriterNodeEventProcessingCount";
        protected static String staticDescription = "The number of active events being processed by the writer node in a shard group of a distributed AccessManager implementation";

        public WriterNodeEventProcessingCount()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records retrying checking the number of active events being processed by the writer node during a shard group split operation, due to the number of active events being greater than 0.
    /// </summary>
    public class EventProcessingCountCheckRetried : CountMetric
    {
        protected static String staticName = "EventProcessingCountCheckRetried";
        protected static String staticDescription = "Retrying checking the number of active events being processed by the writer node during a shard group split operation, due to the number of active events being greater than 0";

        public EventProcessingCountCheckRetried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that a reader node was created in a distributed AccessManager implementation.
    /// </summary>
    public class ReaderNodeCreated : CountMetric
    {
        protected static String staticName = "ReaderNodeCreated";
        protected static String staticDescription = "A reader node was created in a distributed AccessManager implementation";

        public ReaderNodeCreated()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that an event cache node was created in a distributed AccessManager implementation.
    /// </summary>
    public class EventCacheNodeCreated : CountMetric
    {
        protected static String staticName = "EventCacheNodeCreated";
        protected static String staticDescription = "An event cache node was created in a distributed AccessManager implementation";

        public EventCacheNodeCreated()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that a writer node was created in a distributed AccessManager implementation.
    /// </summary>
    public class WriterNodeCreated : CountMetric
    {
        protected static String staticName = "WriterNodeCreated";
        protected static String staticDescription = "A writer node was created in a distributed AccessManager implementation";

        public WriterNodeCreated()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that a distributed operation coordinator node was created in a distributed AccessManager implementation.
    /// </summary>
    public class DistributedOperationCoordinatorNodeCreated : CountMetric
    {
        protected static String staticName = "DistributedOperationCoordinatorNodeCreated";
        protected static String staticDescription = "A distributed operation coordinator node was created in a distributed AccessManager implementation";

        public DistributedOperationCoordinatorNodeCreated()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that a distributed operation router node was created in a distributed AccessManager implementation.
    /// </summary>
    public class DistributedOperationRouterNodeCreated : CountMetric
    {
        protected static String staticName = "DistributedOperationRouterNodeCreated";
        protected static String staticDescription = "A distributed operation router node was created in a distributed AccessManager implementation";

        public DistributedOperationRouterNodeCreated()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that a persistent storage instance was created for a distributed AccessManager implementation.
    /// </summary>
    public class PersistentStorageInstanceCreated : CountMetric
    {
        protected static String staticName = "PersistentStorageInstanceCreated";
        protected static String staticDescription = "A persistent storage instance was created for a distributed AccessManager implementation";

        public PersistentStorageInstanceCreated()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that a shard group was created in a distributed AccessManager implementation.
    /// </summary>
    public class ShardGroupCreated : CountMetric
    {
        protected static String staticName = "ShardGroupCreated";
        protected static String staticDescription = "A shard group was created in a distributed AccessManager implementation";

        public ShardGroupCreated()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that a shard group was restarted in a distributed AccessManager implementation.
    /// </summary>
    public class ShardGroupRestarted : CountMetric
    {
        protected static String staticName = "ShardGroupRestarted";
        protected static String staticDescription = "A shard group was restarted in a distributed AccessManager implementation";

        public ShardGroupRestarted()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that a distributed AccessManager instance was created.
    /// </summary>
    public class DistributedAccessManagerInstanceCreated : CountMetric
    {
        protected static String staticName = "DistributedAccessManagerInstanceCreated";
        protected static String staticDescription = "A distributed AccessManager instance was created";

        public DistributedAccessManagerInstanceCreated()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that a shard group in a distributed AccessManager instance was split.
    /// </summary>
    public class ShardGroupSplit : CountMetric
    {
        protected static String staticName = "ShardGroupSplit";
        protected static String staticDescription = "A shard group in a distributed AccessManager instance was split";

        public ShardGroupSplit()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that an invalid 'add' event for a primary element was received when attempting to merge shard groups.
    /// </summary>
    public class InvalidAddPrimaryElementEventReceived : CountMetric
    {
        protected static String staticName = "InvalidAddPrimaryElementEventReceived";
        protected static String staticDescription = "An invalid 'add' event for a primary element was received when attempting to merge shard groups";

        public InvalidAddPrimaryElementEventReceived()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records that an invalid 'remove' event for a primary element was received when attempting to merge shard groups.
    /// </summary>
    public class InvalidRemovePrimaryElementEventReceived : CountMetric
    {
        protected static String staticName = "InvalidRemovePrimaryElementEventReceived";
        protected static String staticDescription = "An invalid 'remove' event for a primary element was received when attempting to merge shard groups";

        public InvalidRemovePrimaryElementEventReceived()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to create a reader node in a distributed AccessManager implementation.
    /// </summary>
    public class ReaderNodeCreateTime : IntervalMetric
    {
        protected static String staticName = "ReaderNodeCreateTime";
        protected static String staticDescription = "The time taken to create a reader node in a distributed AccessManager implementation";

        public ReaderNodeCreateTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to create an event cache node in a distributed AccessManager implementation.
    /// </summary>
    public class EventCacheNodeCreateTime : IntervalMetric
    {
        protected static String staticName = "EventCacheNodeCreateTime";
        protected static String staticDescription = "The time taken to create an event cache node in a distributed AccessManager implementation";

        public EventCacheNodeCreateTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to create a writer node in a distributed AccessManager implementation.
    /// </summary>
    public class WriterNodeCreateTime : IntervalMetric
    {
        protected static String staticName = "WriterNodeCreateTime";
        protected static String staticDescription = "The time taken to create a writer node in a distributed AccessManager implementation";

        public WriterNodeCreateTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to create a distributed operation coordinator node in a distributed AccessManager implementation.
    /// </summary>
    public class DistributedOperationCoordinatorNodeCreateTime : IntervalMetric
    {
        protected static String staticName = "DistributedOperationCoordinatorNodeCreateTime";
        protected static String staticDescription = "The time taken to create a distributed operation coordinator node in a distributed AccessManager implementation";

        public DistributedOperationCoordinatorNodeCreateTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to create a distributed operation router node in a distributed AccessManager implementation.
    /// </summary>
    public class DistributedOperationRouterNodeCreateTime : IntervalMetric
    {
        protected static String staticName = "DistributedOperationRouterNodeCreateTime";
        protected static String staticDescription = "The time taken to create a distributed operation router node in a distributed AccessManager implementation";

        public DistributedOperationRouterNodeCreateTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to create a persistent storage instance for a distributed AccessManager implementation.
    /// </summary>
    public class PersistentStorageInstanceCreateTime : IntervalMetric
    {
        protected static String staticName = "PersistentStorageInstanceCreateTime";
        protected static String staticDescription = "The time taken to create a persistent storage instance for a distributed AccessManager implementation";

        public PersistentStorageInstanceCreateTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to create a shard group in a distributed AccessManager implementation.
    /// </summary>
    public class ShardGroupCreateTime : IntervalMetric
    {
        protected static String staticName = "ShardGroupCreateTime";
        protected static String staticDescription = "The time taken to create a shard group in a distributed AccessManager implementation";

        public ShardGroupCreateTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to restart a shard group in a distributed AccessManager implementation.
    /// </summary>
    public class ShardGroupRestartTime : IntervalMetric
    {
        protected static String staticName = "ShardGroupRestartTime";
        protected static String staticDescription = "The time taken to restart a shard group in a distributed AccessManager implementation";

        public ShardGroupRestartTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to create a distributed AccessManager instance.
    /// </summary>
    public class DistributedAccessManagerInstanceCreateTime : IntervalMetric
    {
        protected static String staticName = "DistributedAccessManagerInstanceCreateTime";
        protected static String staticDescription = "The time taken to create a distributed AccessManager instance";

        public DistributedAccessManagerInstanceCreateTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to split a shard group in a distributed AccessManager instance.
    /// </summary>
    public class ShardGroupSplitTime : IntervalMetric
    {
        protected static String staticName = "ShardGroupSplitTime";
        protected static String staticDescription = "The time taken to split a shard group in a distributed AccessManager instance";

        public ShardGroupSplitTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #pragma warning restore 1591
}
