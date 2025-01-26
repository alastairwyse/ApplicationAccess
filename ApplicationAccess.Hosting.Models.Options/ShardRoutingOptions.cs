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
using System.ComponentModel.DataAnnotations;
using ApplicationAccess.Distribution.Models;

namespace ApplicationAccess.Hosting.Models.Options
{
    /// <summary>
    /// Container class storing options for a router component in a distributed AccessManager implementation, and following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class ShardRoutingOptions
    {
        #pragma warning disable 0649

        public const String ShardRoutingOptionsName = "ShardRouting";

        [Required(ErrorMessage = $"Configuration for '{nameof(DataElementType)}' is required.")]
        public DataElement DataElementType { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(SourceQueryShardBaseUrl)}' is required.")]
        public String SourceQueryShardBaseUrl { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(SourceEventShardBaseUrl)}' is required.")]
        public String SourceEventShardBaseUrl { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(SourceShardHashRangeStart)}' is required.")]
        public Int32 SourceShardHashRangeStart { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(SourceShardHashRangeEnd)}' is required.")]
        public Int32 SourceShardHashRangeEnd { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(TargetQueryShardBaseUrl)}' is required.")]
        public String TargetQueryShardBaseUrl { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(TargetEventShardBaseUrl)}' is required.")]
        public String TargetEventShardBaseUrl { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(TargetShardHashRangeStart)}' is required.")]
        public Int32 TargetShardHashRangeStart { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(TargetShardHashRangeEnd)}' is required.")]
        public Int32 TargetShardHashRangeEnd { get; set; }

        [Required(ErrorMessage = $"Configuration for '{nameof(RoutingInitiallyOn)}' is required.")]
        public Nullable<Boolean> RoutingInitiallyOn { get; set; }

        public ShardRoutingOptions()
        {
            SourceQueryShardBaseUrl = "";
            SourceEventShardBaseUrl = "";
            TargetQueryShardBaseUrl = "";
            TargetEventShardBaseUrl = "";
            RoutingInitiallyOn = false;
        }

        #pragma warning restore 0649
    }
}
