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
using System.Text;

namespace ApplicationAccess.Serialization
{
    /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="T:ApplicationAccess.Serialization.IAccessManagerSerializer`1"]/*'/>
    public interface IAccessManagerSerializer<TSerializedObject>
    {
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Serialization.IAccessManagerSerializer`1.Serialize``4(ApplicationAccess.AccessManagerBase{``0,``1,``2,``3},ApplicationAccess.IUniqueStringifier{``0},ApplicationAccess.IUniqueStringifier{``1},ApplicationAccess.IUniqueStringifier{``2},ApplicationAccess.IUniqueStringifier{``3})"]/*'/>
        TSerializedObject Serialize<TUser, TGroup, TComponent, TAccess>
        (
            AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManager, 
            IUniqueStringifier<TUser> userStringifier, 
            IUniqueStringifier<TGroup> groupStringifier, 
            IUniqueStringifier<TComponent> applicationComponentStringifier, 
            IUniqueStringifier<TAccess> accessLevelStringifier
        );

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Serialization.IAccessManagerSerializer`1.Deserialize``4(`0,ApplicationAccess.IUniqueStringifier{``0},ApplicationAccess.IUniqueStringifier{``1},ApplicationAccess.IUniqueStringifier{``2},ApplicationAccess.IUniqueStringifier{``3},ApplicationAccess.AccessManagerBase{``0,``1,``2,``3})"]/*'/>
        void Deserialize<TUser, TGroup, TComponent, TAccess>
        (
            TSerializedObject serializedAccessManager,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToDeserializeTo
        );
    }
}
