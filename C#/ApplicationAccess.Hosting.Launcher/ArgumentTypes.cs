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
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Hosting.Launcher
{
    /// <summary>
    /// Contains the correct types of the launder's command line arguments.
    /// </summary>
    public static class ArgumentTypes
    {
        private static Dictionary<String, Type> argumentTypes;

        static ArgumentTypes()
        {
            argumentTypes = new Dictionary<String, Type>()
            {
                { NameConstants.ModeArgumentName, typeof(LauncherMode) },
                { NameConstants.ComponentArgumentName, typeof(AccessManagerComponent) },
                { NameConstants.ListenPortArgumentName, typeof(UInt16) },
                { NameConstants.MinimumLogLevelArgumentName, typeof(LogLevel) },
                { NameConstants.EncodedJsonConfigurationArgumentName, typeof(JObject) },
                { NameConstants.ConfigurationFilePathArgumentName, typeof(String) },
            };
        }

        /// <summary>
        /// Gets the correct types of the argument with the specified name.
        /// </summary>
        /// <param name="argumentName">The name of the argument to get the type for.</param>
        /// <returns>The type of the argument.</returns>
        public static Type GetArgumentType(String argumentName)
        {
            if (argumentTypes.ContainsKey(argumentName) == false)
                throw new Exception("No type defined for argument with name '{argumentName}'.");

            return argumentTypes[argumentName];
        }
    }
}
