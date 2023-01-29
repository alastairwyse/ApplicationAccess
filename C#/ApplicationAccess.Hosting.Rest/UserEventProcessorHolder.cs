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

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Holds an instance of <see cref="IAccessManagerUserEventProcessor{TUser, TGroup, TComponent, TAccess}"/>.
    /// </summary>
    public class UserEventProcessorHolder
    {
        #pragma warning disable 0649

        public IAccessManagerUserEventProcessor<String, String, String, String> UserEventProcessor { get; set; }

        #pragma warning restore 0649
    }
}
