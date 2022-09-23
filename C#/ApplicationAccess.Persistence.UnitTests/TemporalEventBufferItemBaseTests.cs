/*
 * Copyright 2021 Alastair Wyse (https://github.com/alastairwyse/ApplicationAccess/)
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

namespace ApplicationAccess.Persistence.UnitTests
{
    /// <summary>
    /// Unit tests for the TemporalEventBufferItemBase class.
    /// </summary>
    /// <remarks>Tests are performed via derived (non-abstract) class UserEventBufferItem.</remarks>
    public class TemporalEventBufferItemBaseTests
    {
        [Test]
        public void Constructor_OccurredTimeNotUtc()
        {
            var e = Assert.Throws<ArgumentException>(delegate
            {
                var testUserEventBufferItem = new UserEventBufferItem<String>(Guid.NewGuid(), EventAction.Add, "user1", new DateTime(2021, 11, 3));
            });

            Assert.That(e.Message, Does.StartWith("Parameter 'occurredTime' must be based in UTC (i.e. 'Kind' property must be 'Utc')."));
            Assert.AreEqual("occurredTime", e.ParamName);
        }
    }
}
