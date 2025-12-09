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
using System.Collections.Generic;
using System.Text;

namespace ApplicationAccess.TestHarness
{
    /// <summary>
    /// Parses a <see cref="Command"/> passed as a string.
    /// </summary>
    public class CommandValidatorConverter
    {
        protected Dictionary<String, Command> stringToCommandMap;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.TestHarness.CommandValidatorConverter class.
        /// </summary>
        public CommandValidatorConverter()
        {
            stringToCommandMap = new Dictionary<string, Command>()
            {
                { "q", Command.Quit },
                { "quit", Command.Quit },
                { "p", Command.Pause },
                { "pause", Command.Pause },
                { "r", Command.Resume },
                { "resume", Command.Resume },
            };
        }

        /// <summary>
        /// Attempts to validate the specified <see cref="Command"/> represented as a string, and return it as a <see cref="Command"/>.
        /// </summary>
        /// <param name="commandAsString">The string to validate and convert.</param>
        /// <returns>The converted <see cref="Command"/>.</returns>
        /// <exception cref="CommandValidationException">If the specified string could not be converted to a <see cref="Command"/>.</exception>
        public Command Convert(String commandAsString)
        {
            String commandAsStringLowerCase = commandAsString.ToLower();
            if (stringToCommandMap.ContainsKey(commandAsStringLowerCase) == true)
            {
                return stringToCommandMap[commandAsStringLowerCase];
            }
            else
            {
                throw new CommandValidationException($"'{commandAsString}' was not recognized as a command.  Valid commands are {StringifyEnumValues<Command>()}.");
            }
        }

        #region Private/Protected Methods

        /// <summary>
        /// Returns all values of the specified enum in a comma-separated string.
        /// </summary>
        /// <typeparam name="TEnum">The type of enum to stringify the values of.</typeparam>
        /// <returns>All values of the specified enum values in a comma-separated string.</returns>
        protected String StringifyEnumValues<TEnum>()
            where TEnum : struct, System.Enum
        {
            TEnum[] allValues = Enum.GetValues<TEnum>();
            var returnStringBuilder = new StringBuilder();
            for (Int32 i = 0; i < allValues.Length; i++)
            {
                // Wrap the first character of the stringified enum in square brackets
                String currentEnumValue = $"[{allValues[i].ToString()[0]}]{allValues[i].ToString().Substring(1)}";
                returnStringBuilder.Append($"'{currentEnumValue}'");
                if (i < (allValues.Length - 1))
                {
                    returnStringBuilder.Append(", ");
                }
            }

            return returnStringBuilder.ToString();
        }

        #endregion
    }
}
