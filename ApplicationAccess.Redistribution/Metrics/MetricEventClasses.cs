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
    #pragma warning disable 1591

    /// <summary>
    /// Amount metric which records the number of events copied from a source to a target shard as part of a shard split operation.
    /// </summary>
    public class EventsCopiedFromSourceToTargetShard : AmountMetric
    {
        protected static String staticName = "EventsCopiedFromSourceToTargetShard";
        protected static String staticDescription = "The number of events copied from a source to a target shard as part of a shard split operation";

        public EventsCopiedFromSourceToTargetShard()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records completion of copying a batch of events from a source to a target shard as part of a shard split operation.
    /// </summary>
    public class EventBatchCopyCompleted : CountMetric
    {
        protected static String staticName = "EventBatchCopyCompleted";
        protected static String staticDescription = "Completion of copying a batch of events from a source to a target shard as part of a shard split operation";

        public EventBatchCopyCompleted()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to read a batch of events from a source shard as part of a shard split operation.
    /// </summary>
    public class EventBatchReadTime : IntervalMetric
    {
        protected static String staticName = "EventBatchReadTime";
        protected static String staticDescription = "The time taken to read a batch of events from a source shard as part of a shard split operation";

        public EventBatchReadTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to write a batch of events to a target shard as part of a shard split operation.
    /// </summary>
    public class EventBatchWriteTime : IntervalMetric
    {
        protected static String staticName = "EventBatchWriteTime";
        protected static String staticDescription = "The time taken to write a batch of events to a target shard as part of a shard split operation";

        public EventBatchWriteTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to delete events from a source shard as part of a shard split operation.
    /// </summary>
    public class EventDeleteTime : IntervalMetric
    {
        protected static String staticName = "EventDeleteTime";
        protected static String staticDescription = "The time taken to delete events from a source shard as part of a shard split operation";

        public EventDeleteTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of active events being processed by the writer node in a shard of a distributed AccessManager implementation.
    /// </summary>
    public class WriterNodeEventProcessingCount : StatusMetric
    {
        protected static String staticName = "WriterNodeEventProcessingCount";
        protected static String staticDescription = "The number of active events being processed by the writer node in a shard of a distributed AccessManager implementation";

        public WriterNodeEventProcessingCount()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records retrying checking the number of active events being processed by the writer node during a shard split operation, due to the number of active events being greater than 0.
    /// </summary>
    public class EventProcessingCountCheckRetried : CountMetric
    {
        protected static String staticName = "EventProcessingCountCheckRetried";
        protected static String staticDescription = "Retrying checking the number of active events being processed by the writer node during a shard split operation, due to the number of active events being greater than 0";

        public EventProcessingCountCheckRetried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #pragma warning restore 1591
}
