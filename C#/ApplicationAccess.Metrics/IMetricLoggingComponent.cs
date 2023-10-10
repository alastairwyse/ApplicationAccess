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

namespace ApplicationAccess.Metrics
{
    /// <summary>
    /// Defines methods and properties for components which log metrics.
    /// </summary>
    /// <remarks>This interface was mainly created to allow <see cref="MetricLoggingConcurrentAccessManager{TUser, TGroup, TComponent, TAccess}"/> and <see cref="MetricLoggingDependencyFreeAccessManager{TUser, TGroup, TComponent, TAccess}"/> to switch their metric logging on and off when they're declared as a common base type/interface, despite the fact that their inheritance hierarchies diverge.</remarks>
    public interface IMetricLoggingComponent
    {
        /// <summary>
        /// Whether logging of metrics is enabled.
        /// </summary>
        Boolean MetricLoggingEnabled { get; set; }
    }
}
