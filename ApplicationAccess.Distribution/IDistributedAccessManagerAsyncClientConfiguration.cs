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
using System.Collections.Generic;
using System.Text;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Model/container class holding configuration which can be used to instantiate a distributed AccessManager async client.
    /// </summary>
    /// <remarks>The client instantiated from this configuration should implement <see cref="IAccessManagerAsyncQueryProcessor{TUser, TGroup, TComponent, TAccess}"/>, <see cref="IAccessManagerAsyncEventProcessor{TUser, TGroup, TComponent, TAccess}"/>, and <see cref="IDistributedAccessManagerAsyncQueryProcessor{TUser, TGroup, TComponent, TAccess}"/>.</remarks>
    public interface IDistributedAccessManagerAsyncClientConfiguration
    {
        /// <summary>
        /// A user-readable description of the client configuration, primarily used to identify a client created from it in exception messages.
        /// </summary>
        /// <remarks>Implementing classes should create the description with the classes' type name and a set of name/value pairs, similar to that produced ny a <see href="https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/builtin-types/record#built-in-formatting-for-display">record type's ToString() method</see>.</remarks>
        String Description { get; }
    }
}
