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

namespace ApplicationAccess.Utilities
{
    /// <summary>
    /// Defines methods to pause/hold processing of a collection of threads, and then subsequently allow them to continue processing.
    /// </summary>
    public interface IThreadPauser
    {
        /// <summary>
        /// Should be called by the threads performing the processing to check whether the <see cref="IThreadPauser"/> is paused.  If paused, the thread will wait until the <see cref="IThreadPauser.Unpause"/> method is called.  If not paused, the method will return immediately.
        /// </summary>
        void TestPaused();

        /// <summary>
        /// Pauses/holds any threads which subsequently call the <see cref="IThreadPauser.TestPaused"/> method.
        /// </summary>
        void Pause();

        /// <summary>
        /// Releases any threads which are currently paused/held, and allows subsequent calls to <see cref="IThreadPauser.TestPaused"/> to return immediately.
        /// </summary>
        void Unpause();
    }
}
