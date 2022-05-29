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
    /// <summary>
    /// Count metric which records the total number of 'AddUser' events buffered.
    /// </summary>
    public class AddUserEventBuffered : CountMetric
    {
        public AddUserEventBuffered()
        {
            base.name = "AddUserEventProcessed";
            base.description = "The total number of 'AddUser' events buffered";
        }
    }

    /// <summary>
    /// Count metric which records the total number of 'RemoveUser' events buffered.
    /// </summary>
    public class RemoveUserEventBuffered : CountMetric
    {
        public RemoveUserEventBuffered()
        {
            base.name = "RemoveUserEventBuffered";
            base.description = "The total number of 'RemoveUser' events buffered";
        }
    }

    /// <summary>
    /// Status metric which records the number of 'User' events currently buffered.
    /// </summary>
    public class UserEventsBuffered : StatusMetric
    {
        public UserEventsBuffered()
        {
            base.name = "UserEventsBuffered";
            base.description = "The number of 'User' events currently buffered";
        }
    }
}
