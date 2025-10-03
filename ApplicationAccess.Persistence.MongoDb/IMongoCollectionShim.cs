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
using MongoDB.Driver;

namespace ApplicationAccess.Persistence.MongoDb
{
    /// <summary>
    /// Acts as a <see href="https://en.wikipedia.org/wiki/Shim_(computing)">shim</see> to an <see cref="IMongoCollection{TDocument}"/> implementation, so that calls to extension methods can be unit tested.
    /// </summary>
    public interface IMongoCollectionShim
    {
        #pragma warning disable 1591

        public IFindFluent<T, T> Find<T>(IMongoCollection<T> mongoCollection, FilterDefinition<T> filter);

        public void InsertOne<T>(IMongoCollection<T> mongoCollection, T document);

        #pragma warning restore 1591
    }
}
