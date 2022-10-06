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

namespace ApplicationAccess.Persistence
{
    #pragma warning disable 1591

    /// <summary>
    /// Count metric which records an 'AddUser' event buffered.
    /// </summary>
    public class AddUserEventBuffered : CountMetric
    {
        protected static String staticName = "AddUserEventBuffered";
        protected static String staticDescription = "An 'AddUser' event buffered";

        public AddUserEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a 'RemoveUser' event buffered.
    /// </summary>
    public class RemoveUserEventBuffered : CountMetric
    {
        protected static String staticName = "RemoveUserEventBuffered";
        protected static String staticDescription = "A 'RemoveUser' event buffered";

        public RemoveUserEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of 'User' events currently buffered.
    /// </summary>
    public class UserEventsBuffered : StatusMetric
    {
        protected static String staticName = "UserEventsBuffered";
        protected static String staticDescription = "The number of 'User' events currently buffered";

        public UserEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records an 'AddGroup' event buffered.
    /// </summary>
    public class AddGroupEventBuffered : CountMetric
    {
        protected static String staticName = "AddGroupEventBuffered";
        protected static String staticDescription = "An 'AddGroup' event buffered";

        public AddGroupEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which a records a 'RemoveGroup' event buffered.
    /// </summary>
    public class RemoveGroupEventBuffered : CountMetric
    {
        protected static String staticName = "RemoveGroupEventBuffered";
        protected static String staticDescription ="A 'RemoveGroup' event buffered";

        public RemoveGroupEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of 'Group' events currently buffered.
    /// </summary>
    public class GroupEventsBuffered : StatusMetric
    {
        protected static String staticName = "GroupEventsBuffered";
        protected static String staticDescription = "The number of 'Group' events currently buffered";

        public GroupEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records an 'AddUserToGroupMapping' event buffered.
    /// </summary>
    public class AddUserToGroupMappingEventBuffered : CountMetric
    {
        protected static String staticName = "AddUserToGroupMappingEventBuffered";
        protected static String staticDescription = "An 'AddUserToGroupMapping' event buffered";

        public AddUserToGroupMappingEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a 'RemoveUserToGroupMapping' event buffered.
    /// </summary>
    public class RemoveUserToGroupMappingEventBuffered : CountMetric
    {
        protected static String staticName = "RemoveUserToGroupMappingEventBuffered";
        protected static String staticDescription = "A 'RemoveUserToGroupMapping' event buffered";

        public RemoveUserToGroupMappingEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of 'UserToGroupMapping' events currently buffered.
    /// </summary>
    public class UserToGroupMappingEventsBuffered : StatusMetric
    {
        protected static String staticName = "UserToGroupMappingEventsBuffered";
        protected static String staticDescription = "The number of 'UserToGroupMapping' events currently buffered";

        public UserToGroupMappingEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records an 'AddGroupToGroupMapping' event buffered.
    /// </summary>
    public class AddGroupToGroupMappingEventBuffered : CountMetric
    {
        protected static String staticName = "AddGroupToGroupMappingEventBuffered";
        protected static String staticDescription = "An 'AddGroupToGroupMapping' event buffered";

        public AddGroupToGroupMappingEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a 'RemoveGroupToGroupMapping' event buffered.
    /// </summary>
    public class RemoveGroupToGroupMappingEventBuffered : CountMetric
    {
        protected static String staticName = "RemoveGroupToGroupMappingEventBuffered";
        protected static String staticDescription = "A 'RemoveGroupToGroupMapping' event buffered";

        public RemoveGroupToGroupMappingEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of 'GroupToGroupMapping' events currently buffered.
    /// </summary>
    public class GroupToGroupMappingEventsBuffered : StatusMetric
    {
        protected static String staticName = "GroupToGroupMappingEventsBuffered";
        protected static String staticDescription = "The number of 'GroupToGroupMapping' events currently buffered";

        public GroupToGroupMappingEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records an 'AddUserToApplicationComponentAndAccessLevelMapping' event buffered.
    /// </summary>
    public class AddUserToApplicationComponentAndAccessLevelMappingEventBuffered : CountMetric
    {
        protected static String staticName = "AddUserToApplicationComponentAndAccessLevelMappingEventBuffered";
        protected static String staticDescription = "An 'AddUserToApplicationComponentAndAccessLevelMapping' event buffered";

        public AddUserToApplicationComponentAndAccessLevelMappingEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a 'RemoveUserToApplicationComponentAndAccessLevelMapping' event buffered.
    /// </summary>
    public class RemoveUserToApplicationComponentAndAccessLevelMappingEventBuffered : CountMetric
    {
        protected static String staticName = "RemoveUserToApplicationComponentAndAccessLevelMappingEventBuffered";
        protected static String staticDescription = "A 'RemoveUserToApplicationComponentAndAccessLevelMapping' event buffered";

        public RemoveUserToApplicationComponentAndAccessLevelMappingEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of 'UserToApplicationComponentAndAccessLevelMapping' events currently buffered.
    /// </summary>
    public class UserToApplicationComponentAndAccessLevelMappingEventsBuffered : StatusMetric
    {
        protected static String staticName = "UserToApplicationComponentAndAccessLevelMappingEventsBuffered";
        protected static String staticDescription = "The number of 'UserToApplicationComponentAndAccessLevelMapping' events currently buffered";

        public UserToApplicationComponentAndAccessLevelMappingEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records an 'AddGroupToApplicationComponentAndAccessLevelMapping' event buffered.
    /// </summary>
    public class AddGroupToApplicationComponentAndAccessLevelMappingEventBuffered : CountMetric
    {
        protected static String staticName = "AddGroupToApplicationComponentAndAccessLevelMappingEventBuffered";
        protected static String staticDescription = "An 'AddGroupToApplicationComponentAndAccessLevelMapping' event buffered";

        public AddGroupToApplicationComponentAndAccessLevelMappingEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a 'RemoveGroupToApplicationComponentAndAccessLevelMapping' event buffered.
    /// </summary>
    public class RemoveGroupToApplicationComponentAndAccessLevelMappingEventBuffered : CountMetric
    {
        protected static String staticName = "RemoveGroupToApplicationComponentAndAccessLevelMappingEventBuffered";
        protected static String staticDescription = "A 'RemoveGroupToApplicationComponentAndAccessLevelMapping' event buffered";

        public RemoveGroupToApplicationComponentAndAccessLevelMappingEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of 'GroupToApplicationComponentAndAccessLevelMapping' events currently buffered.
    /// </summary>
    public class GroupToApplicationComponentAndAccessLevelMappingEventsBuffered : StatusMetric
    {
        protected static String staticName = "GroupToApplicationComponentAndAccessLevelMappingEventsBuffered";
        protected static String staticDescription = "The number of 'GroupToApplicationComponentAndAccessLevelMapping' events currently buffered";

        public GroupToApplicationComponentAndAccessLevelMappingEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records an 'AddEntityType' event buffered.
    /// </summary>
    public class AddEntityTypeEventBuffered : CountMetric
    {
        protected static String staticName = "AddEntityTypeEventBuffered";
        protected static String staticDescription = "An 'AddEntityType' event buffered";

        public AddEntityTypeEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a 'RemoveEntityType' event buffered.
    /// </summary>
    public class RemoveEntityTypeEventBuffered : CountMetric
    {
        protected static String staticName = "RemoveEntityTypeEventBuffered";
        protected static String staticDescription = "A 'RemoveEntityType' event buffered";

        public RemoveEntityTypeEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of 'EntityType' events currently buffered.
    /// </summary>
    public class EntityTypeEventsBuffered : StatusMetric
    {
        protected static String staticName = "EntityTypeEventsBuffered";
        protected static String staticDescription = "The number of 'EntityType' events currently buffered";

        public EntityTypeEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records an 'AddEntity' event buffered.
    /// </summary>
    public class AddEntityEventBuffered : CountMetric
    {
        protected static String staticName = "AddEntityEventBuffered";
        protected static String staticDescription = "An 'AddEntity' event buffered";

        public AddEntityEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a 'RemoveEntity' event buffered.
    /// </summary>
    public class RemoveEntityEventBuffered : CountMetric
    {
        protected static String staticName = "RemoveEntityEventBuffered";
        protected static String staticDescription = "A 'RemoveEntity' event buffered";

        public RemoveEntityEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of 'Entity' events currently buffered.
    /// </summary>
    public class EntityEventsBuffered : StatusMetric
    {
        protected static String staticName = "EntityEventsBuffered";
        protected static String staticDescription = "The number of 'Entity' events currently buffered";

        public EntityEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records an 'AddUserToEntityMapping' event buffered.
    /// </summary>
    public class AddUserToEntityMappingEventBuffered : CountMetric
    {
        protected static String staticName = "AddUserToEntityMappingEventBuffered";
        protected static String staticDescription = "An 'AddUserToEntityMapping' event buffered";

        public AddUserToEntityMappingEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a 'RemoveUserToEntityMapping' event buffered.
    /// </summary>
    public class RemoveUserToEntityMappingEventBuffered : CountMetric
    {
        protected static String staticName = "RemoveUserToEntityMappingEventBuffered";
        protected static String staticDescription = "A 'RemoveUserToEntityMapping' event buffered";

        public RemoveUserToEntityMappingEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of 'UserToEntityMapping' events currently buffered.
    /// </summary>
    public class UserToEntityMappingEventsBuffered : StatusMetric
    {
        protected static String staticName = "UserToEntityMappingEventsBuffered";
        protected static String staticDescription = "The number of 'UserToEntityMapping' events currently buffered";

        public UserToEntityMappingEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records an 'AddGroupToEntityMapping' event buffered.
    /// </summary>
    public class AddGroupToEntityMappingEventBuffered : CountMetric
    {
        protected static String staticName = "AddGroupToEntityMappingEventBuffered";
        protected static String staticDescription = "An 'AddGroupToEntityMapping' event buffered";

        public AddGroupToEntityMappingEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a'RemoveGroupToEntityMapping' event buffered.
    /// </summary>
    public class RemoveGroupToEntityMappingEventBuffered : CountMetric
    {
        protected static String staticName = "RemoveGroupToEntityMappingEventBuffered";
        protected static String staticDescription = "A 'RemoveGroupToEntityMapping' event buffered";

        public RemoveGroupToEntityMappingEventBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Status metric which records the number of 'GroupToEntityMapping' events currently buffered.
    /// </summary>
    public class GroupToEntityMappingEventsBuffered : StatusMetric
    {
        protected static String staticName = "GroupToEntityMappingEventsBuffered";
        protected static String staticDescription = "The number of 'GroupToEntityMapping' events currently buffered";

        public GroupToEntityMappingEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of buffered events flushed.
    /// </summary>
    public class BufferedEventsFlushed : AmountMetric
    {
        protected static String staticName = "BufferedEventsFlushed";
        protected static String staticDescription = "The number of buffered events flushed";

        public BufferedEventsFlushed()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the a buffer flush operation triggered by the buffer size limit being reached.
    /// </summary>
    public class BufferFlushOperationTriggeredBySizeLimit : CountMetric
    {
        protected static String staticName = "BufferFlushOperationTriggeredBySizeLimit";
        protected static String staticDescription = "A buffer flush operation triggered by the buffer size limit being reached";

        public BufferFlushOperationTriggeredBySizeLimit()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a buffer flush operation triggered by the worker thread loop interval expiring.
    /// </summary>
    public class BufferFlushOperationTriggeredByLoopIntervalExpiration : CountMetric
    {
        protected static String staticName = "BufferFlushOperationTriggeredByLoopIntervalExpiration";
        protected static String staticDescription = "A buffer flush operation triggered by the worker thread loop interval expiring";

        public BufferFlushOperationTriggeredByLoopIntervalExpiration()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a buffer flush operation completed.
    /// </summary>
    public class BufferFlushOperationCompleted : CountMetric
    {
        protected static String staticName = "BufferFlushOperationCompleted";
        protected static String staticDescription = "A buffer flush operation completed";

        public BufferFlushOperationCompleted()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to perform buffer flush operations.
    /// </summary>
    public class FlushTime : IntervalMetric
    {
        protected static String staticName = "FlushTime";
        protected static String staticDescription = "The time taken to perform buffer flush operations";

        public FlushTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of events excluded from flush operations (i.e. left in the buffer as they occurred after the start of the flush operation).
    /// </summary>
    public class EventsExcludedFromFlush : AmountMetric
    {
        protected static String staticName = "EventsExcludedFromFlush";
        protected static String staticDescription = "The number of events excluded from flush operations (i.e. left in the buffer as they occurred after the start of the flush operation)";

        public EventsExcludedFromFlush()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of events still buffered when the Stop() method was called on the buffer flush strategy object.
    /// </summary>
    public class EventsBufferedAfterFlushStrategyStop : AmountMetric
    {
        protected static String staticName = "EventsBufferedAfterFlushStrategyStop";
        protected static String staticDescription = "The number of events still buffered when the Stop() method was called on the buffer flush strategy object";

        public EventsBufferedAfterFlushStrategyStop()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the worker thread loop interval expiring while a flush operation was already in progress.
    /// </summary>
    /// <remarks>Created specifically for class SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy which can trigger flushes from either the worker thread loop interval expiring or the buffer size limit being reached.</remarks>
    public class BufferFlushLoopIntervalExpirationWhileFlushOperationInProgress : CountMetric
    {
        protected static String staticName = "BufferFlushLoopIntervalExpirationWhileFlushOperationInProgress";
        protected static String staticDescription = "The worker thread loop interval expiring while a flush operation was already in progress";

        public BufferFlushLoopIntervalExpirationWhileFlushOperationInProgress()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the time in milliseconds the buffer flushing worker thread slept for.
    /// </summary>
    public class BufferFlushLoopIntervalSleepTime : AmountMetric
    {
        protected static String staticName = "BufferFlushLoopIntervalSleepTime";
        protected static String staticDescription = "The time in milliseconds the buffer flushing worker thread slept for";

        public BufferFlushLoopIntervalSleepTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records a buffer flush being triggered due to the buffer size limit being reached, whilst the buffer flushing worker thread was sleeping.
    /// </summary>
    /// <remarks>Created specifically for class SizeLimitedLoopingWorkerThreadHybridBufferFlushStrategy which can trigger flushes from either the worker thread loop interval expiring or the buffer size limit being reached.</remarks>
    public class SizeLimitBufferFlushTriggeredDuringLoopInterval : CountMetric
    {
        protected static String staticName = "SizeLimitBufferFlushTriggeredDuringLoopInterval";
        protected static String staticDescription = "A buffer flush being triggered due to the buffer size limit being reached, whilst the buffer flushing worker thread was sleeping";

        public SizeLimitBufferFlushTriggeredDuringLoopInterval()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records an event cached.
    /// </summary>
    public class EventCached : CountMetric
    {
        protected static String staticName = "EventCached";
        protected static String staticDescription = "An event cached";

        public EventCached()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of events read from the cache.
    /// </summary>
    public class CachedEventsRead : AmountMetric
    {
        protected static String staticName = "CachedEventsRead";
        protected static String staticDescription = "The number of events read from the cache.";

        public CachedEventsRead()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #pragma warning restore 1591
}
