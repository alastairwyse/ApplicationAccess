/*
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

using System;
using System.ComponentModel.DataAnnotations;

namespace ApplicationAccess.Hosting.Models.Options
{
    /// <summary>
    /// Container class storing options for refreshing from cache of AccessManager events, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class EventCacheRefreshOptions
    {
        #pragma warning disable 0649

        public const String EventCacheRefreshOptionsName = "EventCacheRefresh";

        [Required(ErrorMessage = $"Configuration for '{nameof(RefreshInterval)}' is required.")]
        [Range(1, 2147483647, ErrorMessage = "Value for {0} must be between {1} and {2}.")]
        public Int32 RefreshInterval { get; set; }

        public EventCacheRefreshOptions()
        {
            RefreshInterval = 0;
        }

        #pragma warning restore 0649
    }
}
