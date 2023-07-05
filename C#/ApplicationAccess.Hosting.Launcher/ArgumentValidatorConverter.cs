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
using System.IO;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Hosting.Launcher
{
    /// <summary>
    /// Provides methods to validate command line arguments passed to the launcher, and to convert them to their correct data types (from strings).
    /// </summary>
    public class ArgumentValidatorConverter
    {
        /// <summary>Maps the name of a command line arguments to a Func which validates and converts the value of that argument to its correct type.</summary>
        protected Dictionary<String, Func<String, Object>> typeConversionOperationMap;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Launcher.ArgumentValidatorConverter class.
        /// </summary>
        public ArgumentValidatorConverter()
        {
            typeConversionOperationMap = new Dictionary<String, Func<String, Object>>();
            InitializeTypeConversionOperationMap();
        }

        /// <summary>
        /// Valdates the types of the specified name/value pair arguments, and ensures any dependency requirements are met..
        /// </summary>
        /// <param name="arguments">The arguments to validate.</param>
        /// <exception cref="Exception">If one or more of the arguments name's is invalid.</exception>
        /// <exception cref="Exception">If one or more of the arguments value's could not be converted to the correct data type.</exception>
        /// <exception cref="Exception">If one or more of the arguments value's is invaid.</exception>
        /// <exception cref="Exception">If one or more of the required arguments are not specified.</exception>
        public void Validate(Dictionary<String, String> arguments)
        {
            foreach (KeyValuePair<String, String> currentArgumentAndValue in arguments)
            {
                // Check that the parameter name is valid
                if (typeConversionOperationMap.ContainsKey(currentArgumentAndValue.Key) == false)
                    throw new CommandLineArgumentInvalidException($"Encountered unknown parameter '{currentArgumentAndValue.Key}'", currentArgumentAndValue.Key);

                // Check that the parameter name is valid
                typeConversionOperationMap[currentArgumentAndValue.Key].Invoke(currentArgumentAndValue.Value);
            }


            // TODO: Then also ensure dependencies are met depending on mode
            // These are
            // mode = 'Launch' > component, listenPort, minimumLogLevel, encodedJsonConfiguration
            // mode = 'EncodeConfiguration' > configurationFilePath
            // Set these up in a Dict<String, HashSet<String>>
        }

        /// <summary>
        /// Converts the value of the specified argument to its correct type, and validates its typed value.
        /// </summary>
        /// <typeparam name="T">The type to convert the argument value to.</typeparam>
        /// <param name="argumentName">The name of the argument.</param>
        /// <param name="argumentValue">The stringified value of the argument.</param>
        /// <returns>The value of the argument converted to its correct type</returns>
        public T Convert<T>(String argumentName, String argumentValue)
        {
            if (NameConstants.AllArguments.Contains(argumentName) == false)
                throw new Exception($"Encountered unknown argument name '{argumentName}'.");

            Type returnType = ArgumentTypes.GetArgumentType(argumentName);
            if (typeof(T) != returnType)
            {
                throw new Exception($"Generic parameter type '{typeof(T).Name}' is invalid for argument '{argumentName}'.");
            }
            Object convertedValue = typeConversionOperationMap[argumentName].Invoke(argumentValue);

            return (T)System.Convert.ChangeType(convertedValue, returnType);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Attempts to convert the specified string into a enum of the specified type.
        /// </summary>
        /// <typeparam name="TEnum">The type of enum to convert to.</typeparam>
        /// <param name="inputString">The string to convert.</param>
        /// <returns>The string as an enum.</returns>
        /// <exception cref="Exception">If the string could not be converted.</exception>
        protected TEnum ConvertStringToEnum<TEnum>(String inputString)
            where TEnum : struct, System.Enum
        {
            if (Enum.TryParse<TEnum>(inputString, out TEnum result) == false)
                throw new Exception($"Value '{inputString}' could not be converted to a '{typeof(TEnum).Name}'.");

            return result;
        }

        /// <summary>
        /// Attempts to convert the specified string into an <see cref="UInt16"/>.
        /// </summary>
        protected Int32 ConvertStringToUInt16(String inputString)
        {
            if (UInt16.TryParse(inputString, out UInt16 result) == false)
                throw new Exception($"Value '{inputString}' could not be converted to an '{typeof(UInt16).Name}'.");

            return result;
        }

        /// <summary>
        /// Initializes mappings in the 'typeConversionOperationMap' member.
        /// </summary>
        protected void InitializeTypeConversionOperationMap()
        {
            typeConversionOperationMap.Add
            (
                NameConstants.ModeArgumentName, 
                (String argumentValue) =>
                {
                    try
                    {
                        return ConvertStringToEnum<LauncherMode>(argumentValue);
                    }
                    catch(Exception)
                    {
                        String message = GenerateExceptionMessageForInvalidEnumArgument<LauncherMode>(NameConstants.ModeArgumentName, argumentValue);
                        throw new CommandLineArgumentInvalidException(message, NameConstants.ModeArgumentName);
                    }
                }
            );
            typeConversionOperationMap.Add
            (
                NameConstants.ComponentArgumentName,
                (String argumentValue) =>
                {
                    try
                    {
                        return ConvertStringToEnum<AccessManagerComponent>(argumentValue);
                    }
                    catch (Exception)
                    {
                        String message = GenerateExceptionMessageForInvalidEnumArgument<AccessManagerComponent>(NameConstants.ComponentArgumentName, argumentValue);
                        throw new CommandLineArgumentInvalidException(message, NameConstants.ComponentArgumentName);
                    }
                }
            );
            typeConversionOperationMap.Add
            (
                NameConstants.ListenPortArgumentName,
                (String argumentValue) =>
                {
                    if (UInt16.TryParse(argumentValue, out UInt16 result) == false)
                        throw new CommandLineArgumentInvalidException($"Value '{argumentValue}' is invalid for parameter '{NameConstants.ListenPortArgumentName}'.  Valid values are {UInt16.MinValue}-{UInt16.MaxValue}", NameConstants.ListenPortArgumentName);

                    return result;
                }
            );
            typeConversionOperationMap.Add
            (
                NameConstants.MinimumLogLevelArgumentName,
                (String argumentValue) =>
                {
                    try
                    {
                        return ConvertStringToEnum<LogLevel>(argumentValue);
                    }
                    catch (Exception)
                    {
                        String message = GenerateExceptionMessageForInvalidEnumArgument<LogLevel>(NameConstants.MinimumLogLevelArgumentName, argumentValue);
                        throw new CommandLineArgumentInvalidException(message, NameConstants.MinimumLogLevelArgumentName);
                    }
                }
            );
            typeConversionOperationMap.Add
            (
                NameConstants.EncodedJsonConfigurationArgumentName,
                (String argumentValue) =>
                {
                    var encoder = new Base64StringEncoder();
                    try
                    {
                        String decodedString = encoder.Decode(argumentValue);
                        return JObject.Parse(decodedString);
                    }
                    catch (Exception)
                    {
                        throw new CommandLineArgumentInvalidException($"Value for parameter '{NameConstants.EncodedJsonConfigurationArgumentName}' could not be decoded", NameConstants.EncodedJsonConfigurationArgumentName);
                    }
                }
            );
            typeConversionOperationMap.Add
            (
                NameConstants.ConfigurationFilePathArgumentName,
                (String argumentValue) =>
                {
                    try
                    {
                        var result = File.OpenRead(argumentValue);
                    }
                    catch(FileNotFoundException)
                    {
                        throw new CommandLineArgumentInvalidException($"File '{argumentValue}' could not be found", NameConstants.ConfigurationFilePathArgumentName);
                    }
                    catch (DirectoryNotFoundException)
                    {
                        throw new CommandLineArgumentInvalidException($"File '{argumentValue}' could not be found", NameConstants.ConfigurationFilePathArgumentName);
                    }

                    return argumentValue;
                }
            );
        }

        /// <summary>
        /// Returns all values of the specified enum in a comma-separated string.
        /// </summary>
        /// <typeparam name="TEnum">The type of enum to stringify the values of.</typeparam>
        /// <returns>All values of the specified enum in a comma-separated string.</returns>
        protected String StringifyEnumValues<TEnum>()
            where TEnum : struct, System.Enum
        {
            TEnum[] allValues = Enum.GetValues<TEnum>();
            var returnStringBuilder = new StringBuilder();
            for (Int32 i = 0; i < allValues.Length; i++)
            {
                returnStringBuilder.Append($"'{allValues[i]}'");
                if (i < (allValues.Length - 1))
                {
                    returnStringBuilder.Append(", ");
                }
            }

            return returnStringBuilder.ToString();
        }

        protected String GenerateExceptionMessageForInvalidEnumArgument<TEnum>(String argumentName, String argumentValue)
            where TEnum : struct, System.Enum
        {
            return $"Value '{argumentValue}' is invalid for parameter '{argumentName}'.  Valid values are {StringifyEnumValues<TEnum>()}";
        }

        #endregion
    }
}
