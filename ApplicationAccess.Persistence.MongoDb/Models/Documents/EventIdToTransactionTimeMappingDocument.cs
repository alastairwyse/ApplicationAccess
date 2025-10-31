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
using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace ApplicationAccess.Persistence.MongoDb.Models.Documents
{
    /// <summary>
    /// Holds a MongoDB document which maps an AccessManager event to the timestamp when that event occurred.
    /// </summary>
    public record EventIdToTransactionTimeMappingDocument
    {
        #pragma warning disable 1591

        public ObjectId _id;

        [BsonGuidRepresentation(GuidRepresentation.Standard)]
        public required Guid EventId { get; init; }

        [BsonDateTimeOptions(Representation = BsonType.Int64, Kind = DateTimeKind.Utc)]
        public required DateTime TransactionTime { get; init; }

        public required Int32 TransactionSequence { get; init; }

        #pragma warning restore 1591
    }
}
