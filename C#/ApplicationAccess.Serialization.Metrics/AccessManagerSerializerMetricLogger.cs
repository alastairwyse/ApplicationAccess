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
using ApplicationMetrics;

namespace ApplicationAccess.Serialization.Metrics
{
    /// <summary>
    /// Logs metric events for an implementation of IAccessManagerSerializer.
    /// </summary>
    /// <typeparam name="TSerializedObject">The type of object to serialize to and from.</typeparam>
    /// <remarks>Uses a facade pattern to front the IAccessManagerSerializer, capturing metrics and forwarding method calls to the IAccessManagerSerializer.</remarks>
    public class AccessManagerSerializerMetricLogger<TSerializedObject> : IAccessManagerSerializer<TSerializedObject>
    {
        /// <summary>The IAccessManagerSerializer implementation to log metrics for.</summary>
        protected IAccessManagerSerializer<TSerializedObject> serializer;
        /// <summary>The logger for metrics.</summary>
        protected IMetricLogger metricLogger;

        /// <include file='..\ApplicationAccess.Serialization\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Serialization.IAccessManagerSerializer`1.Serialize``4(ApplicationAccess.AccessManagerBase{``0,``1,``2,``3},ApplicationAccess.IUniqueStringifier{``0},ApplicationAccess.IUniqueStringifier{``1},ApplicationAccess.IUniqueStringifier{``2},ApplicationAccess.IUniqueStringifier{``3})"]/*'/>
        public TSerializedObject Serialize<TUser, TGroup, TComponent, TAccess>(AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManager, IUniqueStringifier<TUser> userStringifier, IUniqueStringifier<TGroup> groupStringifier, IUniqueStringifier<TComponent> applicationComponentStringifier, IUniqueStringifier<TAccess> accessLevelStringifier)
        {
            TSerializedObject result;
            metricLogger.Begin(new AccessManagerSerializeTime());
            try
            {
                result = serializer.Serialize(accessManager, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier);
            }
            catch
            {
                metricLogger.CancelBegin(new AccessManagerSerializeTime());
                throw;
            }
            metricLogger.End(new AccessManagerSerializeTime());
            metricLogger.Increment(new AccessManagerSerialization());

            return result;
        }

        /// <include file='..\ApplicationAccess.Serialization\InterfaceDocumentationComments.xml' path='doc/members/member[@name="M:ApplicationAccess.Serialization.IAccessManagerSerializer`1.Deserialize``4(`0,ApplicationAccess.IUniqueStringifier{``0},ApplicationAccess.IUniqueStringifier{``1},ApplicationAccess.IUniqueStringifier{``2},ApplicationAccess.IUniqueStringifier{``3},ApplicationAccess.AccessManagerBase{``0,``1,``2,``3})"]/*'/>
        public void Deserialize<TUser, TGroup, TComponent, TAccess>
        (
            TSerializedObject serializedAccessManager,
            IUniqueStringifier<TUser> userStringifier,
            IUniqueStringifier<TGroup> groupStringifier,
            IUniqueStringifier<TComponent> applicationComponentStringifier,
            IUniqueStringifier<TAccess> accessLevelStringifier,
            AccessManagerBase<TUser, TGroup, TComponent, TAccess> accessManagerToDeserializeTo
        )
        {
            metricLogger.Begin(new AccessManagerDeserializeTime());
            try
            {
                serializer.Deserialize<TUser, TGroup, TComponent, TAccess>(serializedAccessManager, userStringifier, groupStringifier, applicationComponentStringifier, accessLevelStringifier, accessManagerToDeserializeTo);
            }
            catch
            {
                metricLogger.CancelBegin(new AccessManagerDeserializeTime());
                throw;
            }
            metricLogger.End(new AccessManagerDeserializeTime());
            metricLogger.Increment(new AccessManagerDeserialization());
        }
    }
}
