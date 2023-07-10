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
using System.IO;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Hosting.Launcher
{
    /// <summary>
    /// Configures/sets the 'Logging' section of an application manager component's 'appsettings.json' file.
    /// </summary>
    public class AppsettingsLoggingConfigurer
    {
        protected const String loggingPropertyName = "Logging";
        protected const String logLevelPropertyName = "LogLevel";

        /// <summary>Maps a launcher log level to a collection of 'appsettings.json' property paths and corresponding Microsoft.Extensions.Logging.Loglevels to be set at those paths.</summary>
        protected Dictionary<LogLevel, IList<JsonPathAndLogLevel>> logLevelToJsonPathMap;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Launcher.AppsettingsLoggingConfigurer class.
        /// </summary>
        public AppsettingsLoggingConfigurer()
        {
            logLevelToJsonPathMap = new Dictionary<LogLevel, IList<JsonPathAndLogLevel>>
            {
                { 
                    LogLevel.Information, 
                    new List<JsonPathAndLogLevel>()
                    {
                        new JsonPathAndLogLevel("Microsoft.AspNetCore.Hosting.Diagnostics", "Information"), 
                        new JsonPathAndLogLevel("Microsoft.AspNetCore.Routing", "Warning"), 
                        new JsonPathAndLogLevel("Microsoft.AspNetCore.Mvc", "Warning"), 
                        new JsonPathAndLogLevel("Microsoft.AspNetCore", "Warning")
                    }
                },
                {
                    LogLevel.Warning,
                    new List<JsonPathAndLogLevel>()
                    {
                        new JsonPathAndLogLevel("Microsoft.AspNetCore.Hosting.Diagnostics", "Warning"),
                        new JsonPathAndLogLevel("Microsoft.AspNetCore.Routing", "Warning"),
                        new JsonPathAndLogLevel("Microsoft.AspNetCore.Mvc", "Warning"),
                        new JsonPathAndLogLevel("Microsoft.AspNetCore", "Warning")
                    }
                },
                {
                    LogLevel.Critical,
                    new List<JsonPathAndLogLevel>()
                    {
                        new JsonPathAndLogLevel("Microsoft.AspNetCore.Hosting.Diagnostics", "Critical"),
                        new JsonPathAndLogLevel("Microsoft.AspNetCore.Routing", "Critical"),
                        new JsonPathAndLogLevel("Microsoft.AspNetCore.Mvc", "Critical"),
                        new JsonPathAndLogLevel("Microsoft.AspNetCore", "Critical")
                    }
                }
            };
        }

        /// <summary>
        /// Configures the specified level of logging in an application manager component's 'appsettings.json' file.
        /// </summary>
        /// <param name="logLevel">The log level to configure/set.</param>
        /// <param name="appsettingsFilePath">The full path to the 'appsettings.json' file to set the log level in.</param>
        public void ConfigureLogging(LogLevel logLevel, String appsettingsFilePath)
        {
            try
            {
                using (var fileStream = new FileStream(appsettingsFilePath, FileMode.Open))
                using (var streamReader = new StreamReader(fileStream))
                using (var streamWriter = new StreamWriter(fileStream))
                {
                    JObject appsettingsFileContents = null;
                    try
                    {
                        appsettingsFileContents = JObject.Parse(streamReader.ReadToEnd());
                    }
                    catch (Exception e)
                    {
                        throw new Exception($"Failed to read contents of file '{appsettingsFilePath}' as JSON.", e);
                    }
                    if (appsettingsFileContents[loggingPropertyName] == null)
                        throw new Exception($"Could not find '{loggingPropertyName}' property within file '{appsettingsFilePath}'.");
                    if (appsettingsFileContents[loggingPropertyName][logLevelPropertyName] == null)
                        throw new Exception($"Could not find '{loggingPropertyName}.{logLevelPropertyName}' property within file '{appsettingsFilePath}'.");
                    if (!(appsettingsFileContents[loggingPropertyName][logLevelPropertyName] is JObject))
                        throw new Exception($"Property '{loggingPropertyName}.{logLevelPropertyName}' within file '{appsettingsFilePath}' could not be converted to a {typeof(JObject).Name}.");
                    JObject logLevelPropertyContents = (JObject)appsettingsFileContents[loggingPropertyName][logLevelPropertyName];
                    // Update each of the 'LogLevel' properies
                    foreach (JsonPathAndLogLevel currentJsonPathAndLogLevel in logLevelToJsonPathMap[logLevel])
                    {
                        if (logLevelPropertyContents[currentJsonPathAndLogLevel.PropertyName] == null)
                            throw new Exception($"Could not find '{loggingPropertyName}.{logLevelPropertyName}/{currentJsonPathAndLogLevel.PropertyName}' property within file '{appsettingsFilePath}'.");
                        logLevelPropertyContents[currentJsonPathAndLogLevel.PropertyName] = currentJsonPathAndLogLevel.PropertyValue;
                    }
                    // Write the updated JSON back to the file
                    fileStream.Position = 0;
                    streamWriter.Write(appsettingsFileContents.ToString());
                    streamWriter.Flush();
                    // TODO: Not sure if this will work correctly if JSON contains multi-byte characters.  Need to test and adjust if necessary.
                    fileStream.SetLength(appsettingsFileContents.ToString().Length);
                    streamWriter.Close();
                }
            }
            catch (FileNotFoundException e)
            {
                throw new Exception($"Could not open appsettings file at path '{appsettingsFilePath}'.", e);
            }
            catch (DirectoryNotFoundException e)
            {
                throw new Exception($"Could not open appsettings file at path '{appsettingsFilePath}'.", e);
            }
        }

        #region Inner Classes

        /// <summary>
        /// Container class holding the name of value of a property within the 'Logging.LogLevel' section of an 'appsettings.json' file.
        /// </summary>
        protected class JsonPathAndLogLevel
        {
            /// <summary>The name of the property.</summary>
            public String PropertyName
            {
                get;
                protected set;
            }

            /// <summary>The value of the property.</summary>
            public String PropertyValue
            {
                get;
                protected set;
            }

            /// <summary>
            /// Initialises a new instance of the ApplicationAccess.Hosting.Launcher.AppsettingsLoggingConfigurer+JsonPathAndLogLevel class.
            /// </summary>
            /// <param name="propertyName">The name of the property.</param>
            /// <param name="propertyValue">The value of the property.</param>
            public JsonPathAndLogLevel(String propertyName, String propertyValue)
            {
                this.PropertyName = propertyName;
                this.PropertyValue = propertyValue; 
            }
        }

        #endregion
    }
}
