﻿/*
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

namespace ApplicationAccess.TestHarness.Configuration
{
    #pragma warning disable 0649

    /// <summary>
    /// Model/container class holding access manager REST client configuration.
    /// </summary>
    class AccessManagerRestClientConfiguration
    {
        public String AccessManagerQueryUrl;
        public IList<String> AccessManagerEventUrls;
        public Int32 RetryCount;
        public Int32 RetryInterval;
        public Boolean LogMetrics;
        public Boolean LogIntervalMetrics;
    }

    #pragma warning restore 0649
}
