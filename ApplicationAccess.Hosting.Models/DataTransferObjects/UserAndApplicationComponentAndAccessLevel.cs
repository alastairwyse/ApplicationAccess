﻿/*
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

namespace ApplicationAccess.Hosting.Models.DataTransferObjects
{
    /// <summary>
    /// DTO container class holding a user, an application component, and level of access to that component.
    /// </summary>
    public class UserAndApplicationComponentAndAccessLevel<TUser, TComponent, TAccess>
    {
        public TUser User { get; set; }

        public TComponent ApplicationComponent { get; set; }

        public TAccess AccessLevel { get; set; }

        public UserAndApplicationComponentAndAccessLevel(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            User = user;
            ApplicationComponent = applicationComponent;
            AccessLevel = accessLevel;
        }
    }
}