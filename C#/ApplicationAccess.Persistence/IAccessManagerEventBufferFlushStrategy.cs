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

namespace ApplicationAccess.Persistence
{
    /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="T:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy"]/*'/>
    public interface IAccessManagerEventBufferFlushStrategy
    {
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="E:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy.BufferFlushed"]/*'/>
        event EventHandler BufferFlushed;

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy.UserEventBufferItemCount"]/*'/>
        Int32 UserEventBufferItemCount
        {
            set;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy.GroupEventBufferItemCount"]/*'/>
        Int32 GroupEventBufferItemCount
        {
            set;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy.UserToGroupMappingEventBufferItemCount"]/*'/>
        Int32 UserToGroupMappingEventBufferItemCount
        {
            set;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy.GroupToGroupMappingEventBufferItemCount"]/*'/>
        Int32 GroupToGroupMappingEventBufferItemCount
        {
            set;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy.UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount"]/*'/>
        Int32 UserToApplicationComponentAndAccessLevelMappingEventBufferItemCount
        {
            set;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy.GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount"]/*'/>
        Int32 GroupToApplicationComponentAndAccessLevelMappingEventBufferItemCount
        {
            set;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy.EntityTypeEventBufferItemCount"]/*'/>
        Int32 EntityTypeEventBufferItemCount
        {
            set;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy.EntityEventBufferItemCount"]/*'/>
        Int32 EntityEventBufferItemCount
        {
            set;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy.UserToEntityMappingEventBufferItemCount"]/*'/>
        Int32 UserToEntityMappingEventBufferItemCount
        {
            set;
        }

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="P:ApplicationAccess.Persistence.IAccessManagerEventBufferFlushStrategy.GroupToEntityMappingEventBufferItemCount"]/*'/>
        Int32 GroupToEntityMappingEventBufferItemCount
        {
            set;
        }
    }
}
