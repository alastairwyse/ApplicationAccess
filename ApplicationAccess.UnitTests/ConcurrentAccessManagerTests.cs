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
using System.Collections.Generic;
using System.Threading;
using NUnit.Framework;
using NUnit.Framework.Internal;
using NSubstitute;

namespace ApplicationAccess.UnitTests
{
    /// <summary>
    /// Unit tests for the ApplicationAccess.ConcurrentAccessManager class.
    /// </summary>
    public class ConcurrentAccessManagerTests
    {
        private ConcurrentAccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel> testConcurrentAccessManager;

        [SetUp]
        protected void SetUp()
        {
            testConcurrentAccessManager = new ConcurrentAccessManagerWithProtectedMembers<String, String, ApplicationScreen, AccessLevel>();
        }

        [Test]
        public void Constructor_UserToGroupMapAcquireLocksParameterSetCorrectlyOnComposedFields()
        {
            ConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel> testConcurrentAccessManager;
            var fieldNamePath = new List<String>() { "userToGroupMap", "acquireLocks" };
            testConcurrentAccessManager = new ConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel>();

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testConcurrentAccessManager);


            testConcurrentAccessManager = new ConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel>(new ConcurrentDirectedGraph<String, String>(false));

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testConcurrentAccessManager);


            testConcurrentAccessManager = new ConcurrentAccessManager<String, String, ApplicationScreen, AccessLevel>(new ConcurrentCollectionFactory());

            NonPublicFieldAssert.HasValue<Boolean>(fieldNamePath, false, testConcurrentAccessManager);
        }

        [Test]
        public void AddUser_LocksAreSet()
        {
            String testUser = "user1";
            Boolean assertionsWereChecked = false;

            Action<String> postProcessingAction = (actionUser) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UsersLock));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.AddUser(testUser, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void RemoveUser_LocksAreSet()
        {
            String testUser = "user1";
            testConcurrentAccessManager.AddUser(testUser);
            Boolean assertionsWereChecked = false;

            Action<String> postProcessingAction = (actionUser) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UserToGroupMapLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UserToComponentMap));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UserToEntityMap));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.RemoveUser(testUser, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void AddGroup_LocksAreSet()
        {
            String testGroup = "group1";
            Boolean assertionsWereChecked = false;

            Action<String> postProcessingAction = (actionGroup) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupsLock));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.AddGroup(testGroup, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void RemoveGroup_LocksAreSet()
        {
            String testGroup = "group1";
            testConcurrentAccessManager.AddGroup(testGroup);
            Boolean assertionsWereChecked = false;

            Action<String> postProcessingAction = (actionGroup) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UserToGroupMapLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupToGroupMapLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupToComponentMap));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupToEntityMap));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.RemoveGroup(testGroup, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void AddUserToGroupMapping_LocksAreSet()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testConcurrentAccessManager.AddUser(testUser);
            testConcurrentAccessManager.AddGroup(testGroup);
            Boolean assertionsWereChecked = false;

            Action<String, String> postProcessingAction = (actionUser, actionGroup) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(testGroup, actionGroup);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UserToGroupMapLock));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void RemoveUserToGroupMapping_LocksAreSet()
        {
            String testUser = "user1";
            String testGroup = "group1";
            testConcurrentAccessManager.AddUser(testUser);
            testConcurrentAccessManager.AddGroup(testGroup);
            testConcurrentAccessManager.AddUserToGroupMapping(testUser, testGroup);
            Boolean assertionsWereChecked = false;

            Action<String, String> postProcessingAction = (actionUser, actionGroup) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(testGroup, actionGroup);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UserToGroupMapLock));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.RemoveUserToGroupMapping(testUser, testGroup, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void AddGroupToGroupMapping_LocksAreSet()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testConcurrentAccessManager.AddGroup(testFromGroup);
            testConcurrentAccessManager.AddGroup(testToGroup);
            Boolean assertionsWereChecked = false;

            Action<String, String> postProcessingAction = (actionFromGroup, actionToGroup) =>
            {
                Assert.AreEqual(testFromGroup, actionFromGroup);
                Assert.AreEqual(testToGroup, actionToGroup);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupToGroupMapLock));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void RemoveGroupToGroupMapping_LocksAreSet()
        {
            String testFromGroup = "group1";
            String testToGroup = "group2";
            testConcurrentAccessManager.AddGroup(testFromGroup);
            testConcurrentAccessManager.AddGroup(testToGroup);
            testConcurrentAccessManager.AddGroupToGroupMapping(testFromGroup, testToGroup);
            Boolean assertionsWereChecked = false;

            Action<String, String> postProcessingAction = (actionFromGroup, actionToGroup) =>
            {
                Assert.AreEqual(testFromGroup, actionFromGroup);
                Assert.AreEqual(testToGroup, actionToGroup);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupToGroupMapLock));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.RemoveGroupToGroupMapping(testFromGroup, testToGroup, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void AddUserToApplicationComponentAndAccessLevelMapping_LocksAreSet()
        {
            String testUser = "user1";
            testConcurrentAccessManager.AddUser(testUser);
            Boolean assertionsWereChecked = false;

            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (actionUser, actionApplicationComponent, actionAccessLevel) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(ApplicationScreen.Order, actionApplicationComponent);
                Assert.AreEqual(AccessLevel.Create, actionAccessLevel);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UserToComponentMap));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.Create, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void RemoveUserToApplicationComponentAndAccessLevelMapping_LocksAreSet()
        {
            String testUser = "user1";
            testConcurrentAccessManager.AddUser(testUser);
            testConcurrentAccessManager.AddUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.Create);
            Boolean assertionsWereChecked = false;

            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (actionUser, actionApplicationComponent, actionAccessLevel) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(ApplicationScreen.Order, actionApplicationComponent);
                Assert.AreEqual(AccessLevel.Create, actionAccessLevel);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UserToComponentMap));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.RemoveUserToApplicationComponentAndAccessLevelMapping(testUser, ApplicationScreen.Order, AccessLevel.Create, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void AddGroupToApplicationComponentAndAccessLevelMapping_LocksAreSet()
        {
            String testGroup = "group1";
            testConcurrentAccessManager.AddGroup(testGroup);
            Boolean assertionsWereChecked = false;

            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (actionGroup, actionApplicationComponent, actionAccessLevel) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.AreEqual(ApplicationScreen.Order, actionApplicationComponent);
                Assert.AreEqual(AccessLevel.Create, actionAccessLevel);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupToComponentMap));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Create, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void RemoveGroupToApplicationComponentAndAccessLevelMapping_LocksAreSet()
        {
            String testGroup = "group1";
            testConcurrentAccessManager.AddGroup(testGroup);
            testConcurrentAccessManager.AddGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Create);
            Boolean assertionsWereChecked = false;

            Action<String, ApplicationScreen, AccessLevel> postProcessingAction = (actionGroup, actionApplicationComponent, actionAccessLevel) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.AreEqual(ApplicationScreen.Order, actionApplicationComponent);
                Assert.AreEqual(AccessLevel.Create, actionAccessLevel);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupToComponentMap));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.RemoveGroupToApplicationComponentAndAccessLevelMapping(testGroup, ApplicationScreen.Order, AccessLevel.Create, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
       
        [Test]
        public void AddEntityType_LocksAreSet()
        {
            String testEntityType = "ClientAccount";
            Boolean assertionsWereChecked = false;

            Action<String> postProcessingAction = (actionEntityType) =>
            {
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.Entities));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.AddEntityType(testEntityType, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void RemoveEntityType_LocksAreSet()
        {
            String testEntityType = "ClientAccount";
            testConcurrentAccessManager.AddEntityType(testEntityType);
            Boolean assertionsWereChecked = false;

            Action<String> postProcessingAction = (actionEntityType) =>
            {
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UserToEntityMap));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupToEntityMap));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.RemoveEntityType(testEntityType, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void AddEntity_LocksAreSet()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testConcurrentAccessManager.AddEntityType(testEntityType);
            Boolean assertionsWereChecked = false;

            Action<String, String> postProcessingAction = (actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.Entities));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.AddEntity(testEntityType, testEntity, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void GetEntities()
        {
            // ConcurrentAccessManager methods which returned IEnumerable<T> we just returning the underlying ISet<T> implementation in a previous version of the class
            //   Problem was that the ISet<T> implementation was a ConcurrentHashSet<T> which left several ISet<T> methods not implemented
            //   This would usually be OK, but found cases where the IEnumerable<T> was being up-cast, and then the unimplemented methods attempting to be called and failing
            //   Was suprised that this happens in one of the constructor overloads of List<T> (https://www.dotnetframework.org/default.aspx/Net/Net/3@5@50727@3053/DEVDIV/depot/DevDiv/releases/whidbey/netfxsp/ndp/clr/src/BCL/System/Collections/Generic/List@cs/2/List@cs)
            //   Hence including this test and other similar to ensure an 'uncastable' IEnumerable<T> is returned

            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);

            var result = new List<String>(testConcurrentAccessManager.GetEntities(testEntityType));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(testEntity, result[0]);
        }

        [Test]
        public void RemoveEntity_LocksAreSet()
        {
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            Boolean assertionsWereChecked = false;

            Action<String, String> postProcessingAction = (actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UserToEntityMap));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupToEntityMap));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.RemoveEntity(testEntityType, testEntity, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void AddUserToEntityMapping_LocksAreSet()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testConcurrentAccessManager.AddUser(testUser);
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            Boolean assertionsWereChecked = false;

            Action<String, String, String> postProcessingAction = (actionUser, actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UsersLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UserToEntityMap));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void GetUserToEntityMappings()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testConcurrentAccessManager.AddUser(testUser);
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);

            var result = new List<String>(testConcurrentAccessManager.GetUserToEntityMappings(testUser, testEntityType));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(testEntity, result[0]);
        }

        [Test]
        public void RemoveUserToEntityMapping_LocksAreSet()
        {
            String testUser = "user1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testConcurrentAccessManager.AddUser(testUser);
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testConcurrentAccessManager.AddUserToEntityMapping(testUser, testEntityType, testEntity);
            Boolean assertionsWereChecked = false;

            Action<String, String, String> postProcessingAction = (actionUser, actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testUser, actionUser);
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.UserToEntityMap));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.RemoveUserToEntityMapping(testUser, testEntityType, testEntity, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }
        
        [Test]
        public void AddGroupToEntityMapping_LocksAreSet()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testConcurrentAccessManager.AddGroup(testGroup);
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            Boolean assertionsWereChecked = false;

            Action<String, String, String> postProcessingAction = (actionGroup, actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity); 
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupsLock));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.Entities));
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupToEntityMap));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }

        [Test]
        public void GetGroupToEntityMappings()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testConcurrentAccessManager.AddGroup(testGroup);
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);

            var result = new List<String>(testConcurrentAccessManager.GetGroupToEntityMappings(testGroup, testEntityType));

            Assert.AreEqual(1, result.Count);
            Assert.AreEqual(testEntity, result[0]);
        }

        [Test]
        public void RemoveGroupToEntityMapping_LocksAreSet()
        {
            String testGroup = "group1";
            String testEntityType = "ClientAccount";
            String testEntity = "CompanyA";
            testConcurrentAccessManager.AddGroup(testGroup);
            testConcurrentAccessManager.AddEntityType(testEntityType);
            testConcurrentAccessManager.AddEntity(testEntityType, testEntity);
            testConcurrentAccessManager.AddGroupToEntityMapping(testGroup, testEntityType, testEntity);
            Boolean assertionsWereChecked = false;

            Action<String, String, String> postProcessingAction = (actionGroup, actionEntityType, actionEntity) =>
            {
                Assert.AreEqual(testGroup, actionGroup);
                Assert.AreEqual(testEntityType, actionEntityType);
                Assert.AreEqual(testEntity, actionEntity);
                Assert.IsTrue(Monitor.IsEntered(testConcurrentAccessManager.GroupToEntityMap));
                assertionsWereChecked = true;
            };

            testConcurrentAccessManager.RemoveGroupToEntityMapping(testGroup, testEntityType, testEntity, postProcessingAction);

            Assert.IsTrue(assertionsWereChecked);
        }

        #region Nested Classes

        /// <summary>
        /// Version of the ConcurrentAccessManager class where private and protected methods are exposed as public so that they can be unit tested.
        /// </summary>
        /// <typeparam name="TUser">The type of users in the application.</typeparam>
        /// <typeparam name="TGroup">The type of groups in the application.</typeparam>
        /// <typeparam name="TComponent">The type of components in the application to manage access to.</typeparam>
        /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
        protected class ConcurrentAccessManagerWithProtectedMembers<TUser, TGroup, TComponent, TAccess> : ConcurrentAccessManager<TUser, TGroup, TComponent, TAccess>
        {
            /// <summary>Lock object for the users collection.</summary>
            public Object UsersLock
            {
                get { return usersLock; }
            }

            /// <summary>Lock object for the groups collection.</summary>
            public Object GroupsLock
            {
                get { return groupsLock; }
            }

            /// <summary>Lock object for the user to group map.</summary>
            public Object UserToGroupMapLock
            {
                get { return userToGroupMapLock; }
            }

            /// <summary>Lock object for the group to group map.</summary>
            public Object GroupToGroupMapLock
            {
                get { return groupToGroupMapLock; }
            }

            /// <summary>The user to application component and access level map as an object (to check for locking).</summary>
            public Object UserToComponentMap
            {
                get { return userToComponentMap; }
            }

            /// <summary>The group to application component and access level map as an object (to check for locking).</summary>
            public Object GroupToComponentMap
            {
                get { return groupToComponentMap; }
            }

            /// <summary>The entities colection as an object (to check for locking).</summary>
            public Object Entities
            {
                get { return entities; }
            }

            /// <summary>The user to entity map as an object (to check for locking).</summary>
            public Object UserToEntityMap
            {
                get { return userToEntityMap; }
            }

            /// <summary>The group to entity map as an object (to check for locking).</summary>
            public Object GroupToEntityMap
            {
                get { return groupToEntityMap; }
            }

            public ConcurrentAccessManagerWithProtectedMembers()
                : base()
            {
            }
        }

        #endregion
    }
}
