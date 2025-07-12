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

using System;
using System.Collections.Generic;
using ApplicationAccess.Persistence;
using ApplicationAccess.Utilities;
using ApplicationAccess.Validation;
using ApplicationMetrics;

namespace ApplicationAccess.Distribution.Persistence
{
    /// <summary>
    /// Subclass of <see cref="DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer{TUser, TGroup, TComponent, TAccess}"/> which supports prepending of secondary 'remove element' events on removal of a primary element.  The class is designed to work in conjunction with an <see cref="IAccessManagerEventValidator{TUser, TGroup, TComponent, TAccess}"/> implementation which wraps a <see cref="DistributedAccessManager{TUser, TGroup, TComponent, TAccess}"/> instance, which takes care of generating any required events to be prepended before the requested event.
    /// </summary>
    /// <typeparam name="TUser">The type of users in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TGroup">The type of groups in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TComponent">The type of components in the application managed by the AccessManager.</typeparam>
    /// <typeparam name="TAccess">The type of levels of access which can be assigned to an application component.</typeparam>
    public class DistributedAccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess> : DependencyFreeAccessManagerTemporalEventBulkPersisterBuffer<TUser, TGroup, TComponent, TAccess>
    {
        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.DistributedAccessManagerTemporalEventBulkPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventPersister">The bulk persister to use to write flushed events to permanent storage.</param>
        public DistributedAccessManagerTemporalEventBulkPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister
        ) : base(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator, eventPersister)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.DistributedAccessManagerTemporalEventBulkPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventPersister">The bulk persister to use to write flushed events to permanent storage.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        public DistributedAccessManagerTemporalEventBulkPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IMetricLogger metricLogger
        ) : base(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator, eventPersister, metricLogger)
        {
        }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Persistence.DistributedAccessManagerTemporalEventBulkPersisterBuffer class.
        /// </summary>
        /// <param name="eventValidator">The validator to use to validate events.</param>
        /// <param name="bufferFlushStrategy">The strategy to use for flushing the buffers.</param>
        /// <param name="userHashCodeGenerator">The hash code generator for users.</param>
        /// <param name="groupHashCodeGenerator">The hash code generator for groups.</param>
        /// <param name="entityTypeHashCodeGenerator">The hash code generator for entity types.</param>
        /// <param name="eventPersister">The bulk persister to use to write flushed events to permanent storage.</param>
        /// <param name="metricLogger">The logger for metrics.</param>
        /// <param name="guidProvider">The provider to use for random Guids.</param>
        /// <param name="dateTimeProvider">The provider to use for the current date and time.</param>
        /// <remarks>This constructor is included to facilitate unit testing.</remarks>
        public DistributedAccessManagerTemporalEventBulkPersisterBuffer
        (
            IAccessManagerEventValidator<TUser, TGroup, TComponent, TAccess> eventValidator,
            IAccessManagerEventBufferFlushStrategy bufferFlushStrategy,
            IHashCodeGenerator<TUser> userHashCodeGenerator,
            IHashCodeGenerator<TGroup> groupHashCodeGenerator,
            IHashCodeGenerator<String> entityTypeHashCodeGenerator,
            IAccessManagerTemporalEventBulkPersister<TUser, TGroup, TComponent, TAccess> eventPersister,
            IMetricLogger metricLogger,
            IGuidProvider guidProvider,
            IDateTimeProvider dateTimeProvider
        ) : base(eventValidator, bufferFlushStrategy, userHashCodeGenerator, groupHashCodeGenerator, entityTypeHashCodeGenerator, eventPersister, metricLogger, guidProvider, dateTimeProvider)
        {
        }

        /// <inheritdoc/>
        public override void RemoveEntityType(string entityType)
        {
            lockManager.AcquireLocksAndInvokeAction(entityEventBufferLock, LockObjectDependencyPattern.ObjectAndObjectsItDependsOn, new Action(() =>
            {
                ThrowExceptionIfValidationFails(eventValidator.ValidateRemoveEntityType(entityType, BufferRemoveEntityTypeEventAction));
            }));
        }

        /// <inheritdoc/>
        public override void RemoveEntity(String entityType, String entity)
        {
            if (lockManager.LockObjectIsLockedByCurrentThread(entityEventBufferLock) == true)
            {
                BufferRemoveEntityEventAction.Invoke(entityType, entity);
            }
            else
            {
                base.RemoveEntity(entityType, entity);
            }
        }
    }
}
