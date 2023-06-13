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
using NUnit.Framework;

namespace ApplicationAccess.Hosting.Launcher.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.Launcher.ArgumentReader class.
    /// </summary>
    public class ArgumentReaderTests
    {
        private ArgumentReader testArgumentReader;

        [SetUp]
        protected void SetUp()
        {
            testArgumentReader = new ArgumentReader();
        }

        [Test]
        public void Read_ParameterNameNotPrefixedWithDash()
        {
            throw new NotImplementedException();
        }
    }
}