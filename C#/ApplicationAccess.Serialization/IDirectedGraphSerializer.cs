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
    /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="T:ApplicationAccess.Serialization.IDirectedGraphSerializer`1"]/*'/>
    public interface IDirectedGraphSerializer<TSerializedObject>
    {
        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Serialization.DirectedGraphJsonSerializer.Serialize``2(ApplicationAccess.DirectedGraphBase{``0,``1},ApplicationAccess.IUniqueStringifier{``0},ApplicationAccess.IUniqueStringifier{``1})"]/*'/>
        TSerializedObject Serialize<TLeaf, TNonLeaf>(DirectedGraphBase<TLeaf, TNonLeaf> graph, IUniqueStringifier<TLeaf> leafStringifier, IUniqueStringifier<TNonLeaf> nonLeafStringifier);

        /// <include file='InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Serialization.IDirectedGraphSerializer`1.Deserialize``3(`0,ApplicationAccess.IUniqueStringifier{``1},ApplicationAccess.IUniqueStringifier{``2})"]/*'/>
        TDirectedGraph Deserialize<TDirectedGraph, TLeaf, TNonLeaf>(TSerializedObject serializedGraph, IUniqueStringifier<TLeaf> leafStringifier, IUniqueStringifier<TNonLeaf> nonLeafStringifier) where TDirectedGraph : DirectedGraphBase<TLeaf, TNonLeaf>, new();
    }
}
