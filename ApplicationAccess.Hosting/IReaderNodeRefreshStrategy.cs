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

namespace ApplicationAccess.Hosting
{
    /// <summary>
    /// Defines a strategy/methodology for refreshing/updating the contents of a <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/>.
    /// </summary>
    public interface IReaderNodeRefreshStrategy
    {
        /// <summary>Occurs when the contents of the reader node are updated to reflect the latest changes/updates to the system.</summary>
        event EventHandler ReaderNodeRefreshed;

        /// <summary>
        /// Notifies the strategy that a <see cref="ReaderNode{TUser, TGroup, TComponent, TAccess}"/> query method was called.
        /// </summary>
        /// <exception cref="ReaderNodeRefreshException">An exception occurred whilst attempting to refresh/update the reader node.</exception>
        void NotifyQueryMethodCalled();
    }
}
