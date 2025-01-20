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

namespace ApplicationAccess.Distribution.Models
{
    /// <summary>
    /// Model/container class holding a <see cref="Models.DataElement"/> and an <see cref="Models.Operation"/> so together they can be used as the key in a Dictionary.
    /// </summary>
    public class DataElementAndOperation : IEquatable<DataElementAndOperation>
    {
#pragma warning disable 1591

        protected const int prime1 = 7;
        protected const int prime2 = 11;

#pragma warning restore 1591

        /// <summary>
        /// The <see cref="Models.DataElement"/>.
        /// </summary>
        public DataElement DataElement { get; protected set; }

        /// <summary>
        /// The <see cref="Models.Operation"/>.
        /// </summary>
        public Operation Operation { get; protected set; }

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Distribution.DataElementAndOperation class.
        /// </summary>
        /// <param name="dataElement">The <see cref="Models.DataElement"/>.</param>
        /// <param name="operation">The <see cref="Models.Operation"/>.</param>
        public DataElementAndOperation(DataElement dataElement, Operation operation)
        {
            DataElement = dataElement;
            Operation = operation;
        }

        /// <inheritdoc/>
        public bool Equals(DataElementAndOperation other)
        {
            return DataElement == other.DataElement && Operation == other.Operation;
        }

        /// <inheritdoc/>
        public override int GetHashCode()
        {
            return DataElement.GetHashCode() * prime1 + Operation.GetHashCode() * prime2;
        }
    }
}
