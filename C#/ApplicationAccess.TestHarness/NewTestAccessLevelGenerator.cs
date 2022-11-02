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

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Generator for <see cref="TestAccessLevel">TestAccessLevels</see>.   
    /// </summary>
    public class NewTestAccessLevelGenerator : INewAccessLevelGenerator<TestAccessLevel>
    {
        protected Random randomGenerator;

        public NewTestAccessLevelGenerator()
        {
            randomGenerator = new Random();
        }

        public TestAccessLevel Generate()
        {
            Array enumValues = Enum.GetValues(typeof(TestAccessLevel));
            Int32 index = randomGenerator.Next(enumValues.Length);

            return (TestAccessLevel)index;
        }
    }
}
