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
using System.Collections.Generic;
using System.Text;
using ApplicationMetrics;

namespace ApplicationAccess.Persistence
{
    #pragma warning disable 1591

    /// <summary>
    /// Count metric which records the total number of 'AddUser' events buffered.
    /// </summary>
    public class AddUserEventsBuffered : CountMetric
    {
        protected static String staticName = "AddUserEventsBuffered";
        protected static String staticDescription = "The total number of 'AddUser' events buffered";

        public AddUserEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the total number of 'RemoveUser' events buffered.
    /// </summary>
    public class RemoveUserEventsBuffered : CountMetric
    {
        protected static String staticName = "RemoveUserEventsBuffered";
        protected static String staticDescription = "The total number of 'RemoveUser' events buffered";

        public RemoveUserEventsBuffered()
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
    /// Count metric which records the total number of 'AddGroup' events buffered.
    /// </summary>
    public class AddGroupEventsBuffered : CountMetric
    {
        protected static String staticName = "AddGroupEventsBuffered";
        protected static String staticDescription = "The total number of 'AddGroup' events buffered";

        public AddGroupEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the total number of 'RemoveGroup' events buffered.
    /// </summary>
    public class RemoveGroupEventsBuffered : CountMetric
    {
        protected static String staticName = "RemoveGroupEventsBuffered";
        protected static String staticDescription = "The total number of 'RemoveGroup' events buffered";

        public RemoveGroupEventsBuffered()
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
    /// Count metric which records the total number of 'AddUserToGroupMapping' events buffered.
    /// </summary>
    public class AddUserToGroupMappingEventsBuffered : CountMetric
    {
        protected static String staticName = "AddUserToGroupMappingEventsBuffered";
        protected static String staticDescription = "The total number of 'AddUserToGroupMapping' events buffered";

        public AddUserToGroupMappingEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the total number of 'RemoveUserToGroupMapping' events buffered.
    /// </summary>
    public class RemoveUserToGroupMappingEventsBuffered : CountMetric
    {
        protected static String staticName = "RemoveUserToGroupMappingEventsBuffered";
        protected static String staticDescription = "The total number of 'RemoveUserToGroupMapping' events buffered";

        public RemoveUserToGroupMappingEventsBuffered()
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
    /// Count metric which records the total number of 'AddGroupToGroupMapping' events buffered.
    /// </summary>
    public class AddGroupToGroupMappingEventsBuffered : CountMetric
    {
        protected static String staticName = "AddGroupToGroupMappingEventsBuffered";
        protected static String staticDescription = "The total number of 'AddGroupToGroupMapping' events buffered";

        public AddGroupToGroupMappingEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the total number of 'RemoveGroupToGroupMapping' events buffered.
    /// </summary>
    public class RemoveGroupToGroupMappingEventsBuffered : CountMetric
    {
        protected static String staticName = "RemoveGroupToGroupMappingEventsBuffered";
        protected static String staticDescription = "The total number of 'RemoveGroupToGroupMapping' events buffered";

        public RemoveGroupToGroupMappingEventsBuffered()
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
    /// Count metric which records the total number of 'AddUserToApplicationComponentAndAccessLevelMapping' events buffered.
    /// </summary>
    public class AddUserToApplicationComponentAndAccessLevelMappingEventsBuffered : CountMetric
    {
        protected static String staticName = "AddUserToApplicationComponentAndAccessLevelMappingEventsBuffered";
        protected static String staticDescription = "The total number of 'AddUserToApplicationComponentAndAccessLevelMapping' events buffered";

        public AddUserToApplicationComponentAndAccessLevelMappingEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the total number of 'RemoveUserToApplicationComponentAndAccessLevelMapping' events buffered.
    /// </summary>
    public class RemoveUserToApplicationComponentAndAccessLevelMappingEventsBuffered : CountMetric
    {
        protected static String staticName = "RemoveUserToApplicationComponentAndAccessLevelMappingEventsBuffered";
        protected static String staticDescription = "The total number of 'RemoveUserToApplicationComponentAndAccessLevelMapping' events buffered";

        public RemoveUserToApplicationComponentAndAccessLevelMappingEventsBuffered()
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
    /// Count metric which records the total number of 'AddGroupToApplicationComponentAndAccessLevelMapping' events buffered.
    /// </summary>
    public class AddGroupToApplicationComponentAndAccessLevelMappingEventsBuffered : CountMetric
    {
        protected static String staticName = "AddGroupToApplicationComponentAndAccessLevelMappingEventsBuffered";
        protected static String staticDescription = "The total number of 'AddGroupToApplicationComponentAndAccessLevelMapping' events buffered";

        public AddGroupToApplicationComponentAndAccessLevelMappingEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the total number of 'RemoveGroupToApplicationComponentAndAccessLevelMapping' events buffered.
    /// </summary>
    public class RemoveGroupToApplicationComponentAndAccessLevelMappingEventsBuffered : CountMetric
    {
        protected static String staticName = "RemoveGroupToApplicationComponentAndAccessLevelMappingEventsBuffered";
        protected static String staticDescription = "The total number of 'RemoveGroupToApplicationComponentAndAccessLevelMapping' events buffered";

        public RemoveGroupToApplicationComponentAndAccessLevelMappingEventsBuffered()
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
    /// Count metric which records the total number of 'AddEntityType' events buffered.
    /// </summary>
    public class AddEntityTypeEventsBuffered : CountMetric
    {
        protected static String staticName = "AddEntityTypeEventsBuffered";
        protected static String staticDescription = "The total number of 'AddEntityType' events buffered";

        public AddEntityTypeEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the total number of 'RemoveEntityType' events buffered.
    /// </summary>
    public class RemoveEntityTypeEventsBuffered : CountMetric
    {
        protected static String staticName = "RemoveEntityTypeEventsBuffered";
        protected static String staticDescription = "The total number of 'RemoveEntityType' events buffered";

        public RemoveEntityTypeEventsBuffered()
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
    /// Count metric which records the total number of 'AddEntity' events buffered.
    /// </summary>
    public class AddEntityEventsBuffered : CountMetric
    {
        protected static String staticName = "AddEntityEventsBuffered";
        protected static String staticDescription = "The total number of 'AddEntity' events buffered";

        public AddEntityEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the total number of 'RemoveEntity' events buffered.
    /// </summary>
    public class RemoveEntityEventsBuffered : CountMetric
    {
        protected static String staticName = "RemoveEntityEventsBuffered";
        protected static String staticDescription = "The total number of 'RemoveEntity' events buffered";

        public RemoveEntityEventsBuffered()
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
    /// Count metric which records the total number of 'AddUserToEntityMapping' events buffered.
    /// </summary>
    public class AddUserToEntityMappingEventsBuffered : CountMetric
    {
        protected static String staticName = "AddUserToEntityMappingEventsBuffered";
        protected static String staticDescription = "The total number of 'AddUserToEntityMapping' events buffered";

        public AddUserToEntityMappingEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the total number of 'RemoveUserToEntityMapping' events buffered.
    /// </summary>
    public class RemoveUserToEntityMappingEventsBuffered : CountMetric
    {
        protected static String staticName = "RemoveUserToEntityMappingEventsBuffered";
        protected static String staticDescription = "The total number of 'RemoveUserToEntityMapping' events buffered";

        public RemoveUserToEntityMappingEventsBuffered()
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
    /// Count metric which records the total number of 'AddGroupToEntityMapping' events buffered.
    /// </summary>
    public class AddGroupToEntityMappingEventsBuffered : CountMetric
    {
        protected static String staticName = "AddGroupToEntityMappingEventsBuffered";
        protected static String staticDescription = "The total number of 'AddGroupToEntityMapping' events buffered";

        public AddGroupToEntityMappingEventsBuffered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the total number of 'RemoveGroupToEntityMapping' events buffered.
    /// </summary>
    public class RemoveGroupToEntityMappingEventsBuffered : CountMetric
    {
        protected static String staticName = "RemoveGroupToEntityMappingEventsBuffered";
        protected static String staticDescription = "The total number of 'RemoveGroupToEntityMapping' events buffered";

        public RemoveGroupToEntityMappingEventsBuffered()
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
    /// Count metric which records the number of buffer flush operations completed.
    /// </summary>
    public class BufferFlushOperationsTriggered : CountMetric
    {
        protected static String staticName = "BufferFlushOperationsTriggered";
        protected static String staticDescription = "The number of buffer flush operations triggered";

        public BufferFlushOperationsTriggered()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Count metric which records the number of buffer flush operations completed.
    /// </summary>
    public class BufferFlushOperationsCompleted : CountMetric
    {
        protected static String staticName = "BufferFlushOperationsCompleted";
        protected static String staticDescription = "The number of buffer flush operations completed";

        public BufferFlushOperationsCompleted()
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

    #pragma warning restore 1591
}
