﻿/*
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
using System.Collections.Generic;
using System.Text;
using ApplicationMetrics;

namespace ApplicationAccess.Persistence.SqlServer
{
    #pragma warning disable 1591

    /// <summary>
    /// Count metric which records the number of times execution of a SQL command resulted in a transient error and was retried.
    /// </summary>
    public class SqlCommandExecutionsRetried : CountMetric
    {
        protected static String staticName = "SqlCommandExecutionsRetried";
        protected static String staticDescription = "The number of times execution of a SQL command resulted in a transient error and was retried.";

        public SqlCommandExecutionsRetried()
        {
            base.name = staticName;
            base.description = staticDescription;
        }
    }

    #pragma warning restore 1591
}
