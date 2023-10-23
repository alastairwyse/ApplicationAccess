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
using System.Text;

namespace ApplicationAccess.Distribution
{
    /// <summary>
    /// Model/container class holding configuration information for a single shard in a distributed AccessManager implemenetation.
    /// </summary>
    public class ShardConfiguration : IEquatable<ShardConfiguration>
    {
        // TODO: ShardConfigurationSet... should also be equatable
        //   Will need to reference IAsync client interfaces from here
        //     Interfaces (not implementations) should be moved out of 'Rest' projects and into a more generic place (this project or plain AccessManager project).

        /// <summary>The type of data element managed by the shard.</summary>
        public DataElement DataElementType { get; protected set; }

        /// <summary>The type of operation supported by the shard.</summary>
        public Operation OperationType { get; protected set; }

        /// <summary>The first (inclusive) in the range of hash codes of data elements the shard manages.</summary>
        public Int32 HashRangeStart { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.ShardConfiguration class.
        /// </summary>
        /// <param name="dataElementType">The type of data element managed by the shard.</param>
        /// <param name="operationType">The type of operation supported by the shard.</param>
        /// <param name="hashRangeStart">The first (inclusive) in the range of hash codes of data elements the shard manages.</param>
        public ShardConfiguration(DataElement dataElementType, Operation operationType, Int32 hashRangeStart)
        {
            this.DataElementType = dataElementType;
            this.OperationType = operationType;
            this.HashRangeStart = hashRangeStart;
        }

        /// <inheritdoc/>
        public Boolean Equals(ShardConfiguration other)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override Int32 GetHashCode()
        {
            // Look at using that hash combiner thing

            throw new NotImplementedException();
        }

        /// <inheritdoc/>
        public override String ToString()
        {
            // Something user readable so that details of this shard config can be put in logging, metrics category etc...

            throw new NotImplementedException();
        }
    }
}
