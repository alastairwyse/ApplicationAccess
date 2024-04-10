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

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Delays adding user, group, and entity elements to an instance of <see cref="IDataElementStorer{TUser, TGroup, TComponent, TAccess}"/>, for a specified period.
    /// </summary>
    public class DataElementStorerDelayBuffer<TUser, TGroup, TComponent, TAccess> : IDataElementStorer<TUser, TGroup, TComponent, TAccess>
    {
        protected IDataElementStorer<TUser, TGroup, TComponent, TAccess> underlyingStorer;
        protected Int32 delay;
        protected DelayedActionBuffer<TUser> userBuffer;
        protected DelayedActionBuffer<TGroup> groupBuffer;
        protected DelayedActionBuffer<String> entityTypeBuffer;
        protected DelayedActionBuffer<Tuple<String, String>> entityBuffer;
        protected Object userLockObject;
        protected Object groupLockObject;
        protected Object entityTypeLockObject;
        protected Object entityLockObject;

        public DataElementStorerDelayBuffer(Int32 delay, IDataElementStorer<TUser, TGroup, TComponent, TAccess> underlyingStorer)
        {
            if (delay < 1)
                throw new ArgumentOutOfRangeException(nameof(delay), $"Parameter 'nameof(delay)' with value {delay} must be greater than 0.");

            this.underlyingStorer = underlyingStorer;
            this.delay = delay;
            Action<TUser> addUserAction = (TUser user) =>
            {
                underlyingStorer.AddUser(user);
            };
            Action<TGroup> addGroupAction = (TGroup group) =>
            {
                underlyingStorer.AddGroup(group);
            };
            Action<String> addEntityTypeAction = (String entityType) =>
            {
                underlyingStorer.AddEntityType(entityType);
            };
            Action<Tuple<String, String>> addEntityAction = (Tuple <String, String> entityTypeAndEntity) =>
            {
                underlyingStorer.AddEntity(entityTypeAndEntity.Item1, entityTypeAndEntity.Item2);
            };
            userBuffer = new DelayedActionBuffer<TUser>(delay, addUserAction);
            groupBuffer = new DelayedActionBuffer<TGroup>(delay, addGroupAction);
            entityTypeBuffer = new DelayedActionBuffer<String>(delay, addEntityTypeAction);
            entityBuffer = new DelayedActionBuffer<Tuple<String, String>>(delay, addEntityAction);
            userLockObject = new Object();
            groupLockObject = new Object();
            entityTypeLockObject = new Object();
            entityLockObject = new Object();
        }

        public Int32 UserCount
        {
            get
            {
                return underlyingStorer.UserCount + userBuffer.Count;
            }
        }

        public Int32 GroupCount
        {
            get
            {
                return underlyingStorer.GroupCount + groupBuffer.Count;
            }
        }

        public Int32 UserToGroupMappingCount
        {
            get
            {
                return underlyingStorer.UserToGroupMappingCount;
            }
        }

        public Int32 GroupToGroupMappingCount
        {
            get
            {
                return underlyingStorer.GroupToGroupMappingCount;
            }
        }

        public Int32 UserToComponentMappingCount
        {
            get
            {
                return underlyingStorer.UserToComponentMappingCount;
            }
        }

        public Int32 GroupToComponentMappingCount
        {
            get
            {
                return underlyingStorer.GroupToComponentMappingCount;
            }
        }

        public Int32 EntityTypeCount
        {
            get
            {
                return underlyingStorer.EntityTypeCount + entityTypeBuffer.Count;
            }
        }

        public Int32 EntityCount
        {
            get
            {
                return underlyingStorer.EntityCount + entityBuffer.Count;
            }
        }

        public Int32 UserToEntityMappingCount
        {
            get
            {
                return underlyingStorer.UserToEntityMappingCount;
            }
        }

        public Int32 GroupToEntityMappingCount
        {
            get
            {
                return underlyingStorer.GroupToEntityMappingCount;
            }
        }

        public void AddUser(TUser user)
        {
            lock (userLockObject)
            {
                userBuffer.BufferValue(user);
            }
        }

        public void RemoveUser(TUser user)
        {
            underlyingStorer.RemoveUser(user);
        }

        public TUser GetRandomUser()
        {
            return underlyingStorer.GetRandomUser();
        }

        public void AddGroup(TGroup group)
        {
            lock (groupLockObject)
            {
                groupBuffer.BufferValue(group);
            }
        }

        public void RemoveGroup(TGroup group)
        {
            underlyingStorer.RemoveGroup(group);
        }

        public TGroup GetRandomGroup()
        {
            return underlyingStorer.GetRandomGroup();
        }

        public void AddUserToGroupMapping(TUser user, TGroup group)
        {
            underlyingStorer.AddUserToGroupMapping(user, group);
        }

        public void RemoveUserToGroupMapping(TUser user, TGroup group)
        {
            underlyingStorer.RemoveUserToGroupMapping(user, group);
        }

        public Tuple<TUser, TGroup> GetRandomUserToGroupMapping()
        {
            return underlyingStorer.GetRandomUserToGroupMapping();
        }

        public void AddGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            underlyingStorer.AddGroupToGroupMapping(fromGroup, toGroup);
        }

        public void RemoveGroupToGroupMapping(TGroup fromGroup, TGroup toGroup)
        {
            underlyingStorer.RemoveGroupToGroupMapping(fromGroup, toGroup);
        }

        public Tuple<TGroup, TGroup> GetRandomGroupToGroupMapping()
        {
            return underlyingStorer.GetRandomGroupToGroupMapping();
        }

        public void AddUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            underlyingStorer.AddUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
        }

        public void RemoveUserToApplicationComponentAndAccessLevelMapping(TUser user, TComponent applicationComponent, TAccess accessLevel)
        {
            underlyingStorer.RemoveUserToApplicationComponentAndAccessLevelMapping(user, applicationComponent, accessLevel);
        }

        public Tuple<TUser, TComponent, TAccess> GetRandomUserToApplicationComponentAndAccessLevelMapping()
        {
            return underlyingStorer.GetRandomUserToApplicationComponentAndAccessLevelMapping();
        }

        public void AddGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            underlyingStorer.AddGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
        }

        public void RemoveGroupToApplicationComponentAndAccessLevelMapping(TGroup group, TComponent applicationComponent, TAccess accessLevel)
        {
            underlyingStorer.RemoveGroupToApplicationComponentAndAccessLevelMapping(group, applicationComponent, accessLevel);
        }

        public Tuple<TGroup, TComponent, TAccess> GetRandomGroupToApplicationComponentAndAccessLevelMapping()
        {
            return underlyingStorer.GetRandomGroupToApplicationComponentAndAccessLevelMapping();
        }

        public void AddEntityType(String entityType)
        {
            lock (entityTypeLockObject)
            {
                entityTypeBuffer.BufferValue(entityType);
            }
        }

        public void RemoveEntityType(String entityType)
        {
            underlyingStorer.RemoveEntityType(entityType);
        }

        public String GetRandomEntityType()
        {
            return underlyingStorer.GetRandomEntityType();
        }

        public void AddEntity(String entityType, String entity)
        {
            lock (entityTypeLockObject)
            {
                lock (entityLockObject)
                {
                    entityBuffer.BufferValue(new Tuple<String, String>(entityType, entity));
                }
            }
        }

        public void RemoveEntity(String entityType, String entity)
        {
            underlyingStorer.RemoveEntity(entityType, entity);
        }

        public Tuple<String, String> GetRandomEntity()
        {
            return underlyingStorer.GetRandomEntity();
        }

        public void AddUserToEntityMapping(TUser user, String entityType, String entity)
        {
            underlyingStorer.AddUserToEntityMapping(user, entityType, entity);
        }

        public void RemoveUserToEntityMapping(TUser user, String entityType, String entity)
        {
            underlyingStorer.RemoveUserToEntityMapping(user, entityType, entity);
        }

        public Tuple<TUser, String, String> GetRandomUserToEntityMapping()
        {
            return underlyingStorer.GetRandomUserToEntityMapping();
        }

        public void AddGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            underlyingStorer.AddGroupToEntityMapping(group, entityType, entity);
        }

        public void RemoveGroupToEntityMapping(TGroup group, String entityType, String entity)
        {
            underlyingStorer.RemoveGroupToEntityMapping(group, entityType, entity);
        }

        public Tuple<TGroup, String, String> GetRandomGroupToEntityMapping()
        {
            return underlyingStorer.GetRandomGroupToEntityMapping();
        }

        public IEnumerable<TUser> GetUnmappedUsers()
        {
            return underlyingStorer.GetUnmappedUsers();
        }

        public IEnumerable<TGroup> GetUnmappedGroups()
        {
            return underlyingStorer.GetUnmappedGroups();
        }

        public IEnumerable<Tuple<String, String>> GetUnmappedEntities()
        {
            return underlyingStorer.GetUnmappedEntities();
        }

        #region Nested Classes

        /// <summary>
        /// Delays invoking an action on a parameter for a specified period, by buffering parameters to the action in a queue.
        /// </summary>
        /// <typeparam name="T">The type of the parameter for the action.</typeparam>
        protected class DelayedActionBuffer<T>
        {
            protected Queue<ObjectAndTimeStamp<T>> bufferQueue;
            protected Int32 delay;
            protected Action<T> action;

            public Int32 Count
            {
                get
                {
                    return bufferQueue.Count;
                }
            }

            public DelayedActionBuffer(Int32 delay, Action<T> action)
            {
                if (delay < 1)
                    throw new ArgumentOutOfRangeException(nameof(delay), $"Parameter 'nameof(delay)' with value {delay} must be greater than 0.");

                bufferQueue = new Queue<ObjectAndTimeStamp<T>>();
                this.delay = delay;
                this.action = action;
            }

            public void BufferValue(T value)
            {
                DateTime now = DateTime.UtcNow;
                var bufferItem = new ObjectAndTimeStamp<T>(now, value);
                bufferQueue.Enqueue(bufferItem);
                var nowMinusDelay = now.AddSeconds(-delay);
                while ((bufferQueue.Count > 0) && (bufferQueue.Peek().TimeStamp < nowMinusDelay))
                {
                    action.Invoke(bufferQueue.Dequeue().Value);
                }
            }
        }

        protected class ObjectAndTimeStamp<T>
        {
            public DateTime TimeStamp { get; }
            public T Value { get; }

            public ObjectAndTimeStamp(DateTime timeStamp, T value)
            {
                TimeStamp = timeStamp;
                Value = value;
            }
        }

        #endregion
    }
}
