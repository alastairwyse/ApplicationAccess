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
using System.Collections.Generic;
using System.Globalization;
using ApplicationAccess.UnitTests;
using ApplicationAccess.Persistence;
using NUnit.Framework;
using NSubstitute;

namespace ApplicationAccess.Hosting.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.Hosting.ReaderNode class.
    /// </summary>
    public class ReaderNodeTests
    {
        private Tuple<Guid, DateTime> returnedLoadState;
        private IReaderNodeRefreshStrategy mockRefreshStrategy;
        private IAccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel> mockEventCache;
        private IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel> mockPersistentReader;
        private ReaderNode<String, String, ApplicationScreen, AccessLevel> testReaderNode;

        [SetUp]
        protected void SetUp()
        {
            returnedLoadState = new Tuple<Guid, DateTime>(Guid.Parse("5555795a-6408-4084-aa86-a70f8731376a"), CreateDataTimeFromString("2022-10-06 19:27:01"));
            mockRefreshStrategy = Substitute.For<IReaderNodeRefreshStrategy>();
            mockEventCache = Substitute.For<IAccessManagerTemporalEventCache<String, String, ApplicationScreen, AccessLevel>>();
            mockPersistentReader = Substitute.For<IAccessManagerTemporalPersistentReader<String, String, ApplicationScreen, AccessLevel>>();
            testReaderNode = new ReaderNode<string, string, ApplicationScreen, AccessLevel>(mockRefreshStrategy, mockEventCache, mockPersistentReader);
        }

        [Test]
        public void Load_CallToPersisterFails()
        {
            var mockException = new Exception("Failed to load.");
            mockPersistentReader.When((reader) => reader.Load(Arg.Any<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>())).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                testReaderNode.Load();
            });

            Assert.That(e.Message, Does.StartWith("Failed to load access manager state from persistent storage."));
            Assert.AreEqual(mockException, e.InnerException);
        }

        [Test]
        public void Users()
        {
            const String user = "user1";
            var loadAction = new Action<AccessManagerBase<String, String, ApplicationScreen, AccessLevel>>((accessManager) =>
            {
                accessManager.AddUser(user);
            });
            mockPersistentReader.Load(Arg.Do(loadAction)).Returns(returnedLoadState);
            testReaderNode.Load();

            var result = new List<String>(testReaderNode.Users);

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(user, result[0]);
        }

        [Test]
        public void Users_RefreshException()
        {
            var mockException = new Exception("Failure on refresh worker thread.");
            mockRefreshStrategy.When((strategy) => strategy.NotifyQueryMethodCalled()).Do((callInfo) => throw mockException);

            var e = Assert.Throws<Exception>(delegate
            {
                var result = testReaderNode.Users;
            });

            Assert.AreEqual(mockException, e);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Creates a DateTime from the specified yyyy-MM-dd HH:mm:ss format string.
        /// </summary>
        /// <param name="stringifiedDateTime">The stringified date/time to convert.</param>
        /// <returns>A DateTime.</returns>
        protected DateTime CreateDataTimeFromString(String stringifiedDateTime)
        {
            DateTime returnDateTime = DateTime.ParseExact(stringifiedDateTime, "yyyy-MM-dd HH:mm:ss", DateTimeFormatInfo.InvariantInfo);

            return DateTime.SpecifyKind(returnDateTime, DateTimeKind.Utc);
        }

        #endregion
    }
}
