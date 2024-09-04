﻿/*
 * Copyright 2024 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Persistence.File
{
    #pragma warning disable 1591

    /// <summary>
    /// Amount metric which records the number of events written to a file.
    /// </summary>
    public class EventsWrittenToFile : AmountMetric
    {
        protected static String staticName = "EventsWrittenToFile";
        protected static String staticDescription = "The number of events written to a file";

        public EventsWrittenToFile()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Interval metric which records the time taken to write events to a file.
    /// </summary>
    public class EventsFileWriteTime : IntervalMetric
    {
        protected static String staticName = "EventsFileWriteTime";
        protected static String staticDescription = "The time taken to write events to a file";

        public EventsFileWriteTime()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    /// <summary>
    /// Amount metric which records the number of events read from a file.
    /// </summary>
    public class EventsReadFromFile : AmountMetric
    {
        protected static String staticName = "EventsReadFromFile";
        protected static String staticDescription = "The number of events read from a file";

        public EventsReadFromFile()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #pragma warning restore 1591
}
