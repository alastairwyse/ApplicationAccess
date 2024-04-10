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
using System.Reflection;

namespace ApplicationAccess.Hosting.LaunchPreparer
{
    public class Program
    {
        protected static String baseAppSettingsFileName = "appsettings.json";
        protected static String productionAppSettingsFileName = "appsettings.Production.json";

        static void Main(string[] args)
        {
            String currentExecutionPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);

            try
            {
                // Process and validate command line arguments
                var argumentReader = new ArgumentReader();
                Dictionary<String, String> arguments = argumentReader.Read(args);
                var argumentValidatorConverter = new ArgumentValidatorConverter();
                argumentValidatorConverter.Validate(arguments);

                // If 'mode' is 'Launch'
                if (arguments[NameConstants.ModeArgumentName] == LaunchPreparerMode.Launch.ToString())
                {
                    // Configure log levels
                    var loggingConfigurer = new AppsettingsLoggingConfigurer();
                    LogLevel minimumLogLevel = argumentValidatorConverter.Convert<LogLevel>(NameConstants.MinimumLogLevelArgumentName, arguments[NameConstants.MinimumLogLevelArgumentName]);
                    String appsettingsPath = Path.Combine(currentExecutionPath, baseAppSettingsFileName);
                    loggingConfigurer.ConfigureLogging(minimumLogLevel, appsettingsPath);

                    // Write the configuration from the command line to the appsettings file
                    String productionAppsettingsPath = Path.Combine(currentExecutionPath, productionAppSettingsFileName);
                    using (var fileStream = new FileStream(productionAppsettingsPath, FileMode.OpenOrCreate))
                    using (var streamWriter = new StreamWriter(fileStream))
                    {
                        var encoder = new Base64StringEncoder();
                        // ArgumentValidatorConverter takes care of confirming the contents are JSON in the Validate() call
                        String decodedConfiguration = encoder.Decode(arguments[NameConstants.EncodedJsonConfigurationArgumentName]);
                        streamWriter.Write(decodedConfiguration);
                        streamWriter.Flush();
                        // TODO: Not sure if this will work correctly if JSON contains multi-byte characters.  Need to test and adjust if necessary.
                        fileStream.SetLength(decodedConfiguration.ToString().Length);
                        streamWriter.Close();
                    }

                    // Output the name of the dll used to execute the specified component
                    AccessManagerComponent component = argumentValidatorConverter.Convert<AccessManagerComponent>(NameConstants.ComponentArgumentName, arguments[NameConstants.ComponentArgumentName]);
                    String dllName = ComponentToDllNameMap.GetDllNameForComponent(component);
                    Console.WriteLine(dllName);
                    Environment.Exit(0);
                }
                // If 'mode' is 'EncodeConfiguration'
                else if (arguments[NameConstants.ModeArgumentName] == LaunchPreparerMode.EncodeConfiguration.ToString())
                {
                    using (var fileStream = File.OpenRead(arguments[NameConstants.ConfigurationFilePathArgumentName]))
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        // Encode the contents of the specified file and write the result to the console
                        String fileContents = streamReader.ReadToEnd();
                        var encoder = new Base64StringEncoder();
                        var encodedString = encoder.Encode(fileContents);
                        Console.WriteLine(encodedString);
                        Environment.Exit(0);
                    }
                }
                else
                {
                    throw new Exception($"Encountered unhandled {nameof(LaunchPreparerMode)} '{arguments[NameConstants.ModeArgumentName]}'.");
                }
            }
            catch (CommandLineArgumentInvalidException e)
            {
                Console.WriteLine($"ERROR: {e.Message}");
                Console.WriteLine(GenerateUsageMessage());

                Environment.Exit(1);
            }
        }

        /// <summary>
        /// Generates a 'usage' message for the LaunchPreparer, to display to the user.
        /// </summary>
        /// <returns>The 'usage' message.</returns>
        protected static String GenerateUsageMessage()
        {
            var argumentValidatorConverter = new ArgumentValidatorConverter();
            var usageMessageStringBuilder = new StringBuilder();
            String executableName = System.AppDomain.CurrentDomain.FriendlyName;
            usageMessageStringBuilder.AppendLine("USAGE: ");
            usageMessageStringBuilder.AppendLine($"  {executableName} -{NameConstants.ModeArgumentName} {LaunchPreparerMode.Launch} -{NameConstants.ComponentArgumentName} [ApplicationAccess component] -{NameConstants.ListenPortArgumentName} [TCP port to listen on] -{NameConstants.MinimumLogLevelArgumentName} [log level] -{NameConstants.EncodedJsonConfigurationArgumentName} [Base64 encoded JSON configuration]");
            usageMessageStringBuilder.AppendLine($"  {executableName} -{NameConstants.ModeArgumentName} {LaunchPreparerMode.EncodeConfiguration} -{NameConstants.ConfigurationFilePathArgumentName} [path to JSON configuration to encode]");
            usageMessageStringBuilder.AppendLine();
            usageMessageStringBuilder.AppendLine("EXAMPLES: ");
            usageMessageStringBuilder.AppendLine($"  {executableName} -{NameConstants.ModeArgumentName} {LaunchPreparerMode.Launch} -{NameConstants.ComponentArgumentName} {AccessManagerComponent.ReaderWriterNode} -{NameConstants.ListenPortArgumentName} 5001 -{NameConstants.MinimumLogLevelArgumentName} {LogLevel.Critical} -{NameConstants.EncodedJsonConfigurationArgumentName} H4sIAAAAAAACCqpWUApLzClNVbJSUPIsVvAK9vdTUqgFAAAA//8=");
            usageMessageStringBuilder.AppendLine($"  {executableName} -{NameConstants.ModeArgumentName} {LaunchPreparerMode.EncodeConfiguration} -{NameConstants.ConfigurationFilePathArgumentName} C:\\Temp\\ReaderWriterNodeConfiguration.json");
            usageMessageStringBuilder.AppendLine();
            usageMessageStringBuilder.AppendLine("ARGUMENT VALUES: ");
            usageMessageStringBuilder.AppendLine($"  -{NameConstants.ModeArgumentName}: {argumentValidatorConverter.StringifyEnumValues<LaunchPreparerMode>()}");
            usageMessageStringBuilder.AppendLine($"  -{NameConstants.ComponentArgumentName}: {argumentValidatorConverter.StringifyEnumValues<AccessManagerComponent>()}");
            usageMessageStringBuilder.AppendLine($"  -{NameConstants.MinimumLogLevelArgumentName}: {argumentValidatorConverter.StringifyEnumValues<LogLevel>()}");

            return usageMessageStringBuilder.ToString();
        }
    }
}