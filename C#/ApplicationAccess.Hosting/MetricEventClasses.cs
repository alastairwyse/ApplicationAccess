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

namespace ApplicationAccess.Hosting
{
    #pragma warning disable 1591

    /// <summary>
    /// Base for count metrics logged by the <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/> class.
    /// </summary>
    public abstract class ReaderNodeCountMetric : CountMetric
    {
    }

    /// <summary>
    /// Base for amount metrics logged by the <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/> class.
    /// </summary>
    public abstract class ReaderNodeCAmountMetric : AmountMetric
    {
    }

    /// <summary>
    /// Base for interval metrics logged by the <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/> class.
    /// </summary>
    public abstract class ReaderNodeIntervalMetric : IntervalMetric
    {
    }

    /// <summary>
    /// Count metric which records a cache miss occurring.
    /// </summary>
    /// <remarks>Created for the <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/> class, and records when a refresh operation finds that events subsequent to the most recently stored event are not stored in the event cache.</remarks>
    public class CacheMiss : ReaderNodeCountMetric
    {
        protected static String staticName = "CacheMiss";
        protected static String staticDescription = "A cache miss occurred";

        public CacheMiss()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of events received in response to a request to an event cache.
    /// </summary>
    public class CachedEventsReceived : ReaderNodeCAmountMetric
    {
        protected static String staticName = "CachedEventsReceived";
        protected static String staticDescription = "The number of events received in response to a request to an event cache";

        public CachedEventsReceived()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the time in milliseconds between the original occurence of an event, and when that change was processed / applied to a reader node.
    /// </summary>
    public class EventProcessingDelay : ReaderNodeCAmountMetric
    {
        protected static String staticName = "EventProcessingDelay";
        protected static String staticDescription = "The time in milliseconds between the original occurence of an event, and when that change was processed / applied to a reader node";

        public EventProcessingDelay()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the time taken to load the entire contents of a reader node.
    /// </summary>
    public class ReaderNodeLoadTime : ReaderNodeIntervalMetric
    {
        protected static String staticName = "ReaderNodeLoadTime";
        protected static String staticDescription = "The time taken to load the entire contents of a reader node";

        public ReaderNodeLoadTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a reader node being refreshed with the latest data.
    /// </summary>
    public class RefreshOperationCompleted : ReaderNodeCountMetric
    {
        protected static String staticName = "RefreshOperationCompleted";
        protected static String staticDescription = "A reader node was refreshed with the latest data";

        public RefreshOperationCompleted()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to refresh a reader node.
    /// </summary>
    public class RefreshTime : ReaderNodeIntervalMetric
    {
        protected static String staticName = "RefreshTime";
        protected static String staticDescription = "The time taken to refresh a reader node";

        public RefreshTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to load the entire contents of a reader/writer node.
    /// </summary>
    public class ReaderWriterNodeLoadTime : IntervalMetric
    {
        protected static String staticName = "ReaderWriterNodeLoadTime";
        protected static String staticDescription = "The time taken to load the entire contents of a reader/writer node";

        public ReaderWriterNodeLoadTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to load the entire contents of a writer node.
    /// </summary>
    public class WriterNodeLoadTime : IntervalMetric
    {
        protected static String staticName = "WriterNodeLoadTime";
        protected static String staticDescription = "The time taken to load the entire contents of a writer node";

        public WriterNodeLoadTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #pragma warning restore 1591
}
