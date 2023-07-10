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

namespace ApplicationAccess.Hosting.Launcher
{
    public class Program
    {
        static void Main(string[] args)
        {
            try
            {
                var argumentReader = new ArgumentReader();
                Dictionary<String, String> arguments = argumentReader.Read(args);
                var argumentValidatorConverter = new ArgumentValidatorConverter();
                argumentValidatorConverter.Validate(arguments);
                // If 'mode' is 'Launch'
                if (arguments[NameConstants.ModeArgumentName] == LauncherMode.Launch.ToString())
                {
                    // Launch the specified component
                    AccessManagerComponent component = argumentValidatorConverter.Convert<AccessManagerComponent>(NameConstants.ComponentArgumentName, arguments[NameConstants.ComponentArgumentName]);
                    var launcher = new ComponentLauncher();
                    launcher.Launch(component, arguments);
                }
                // If 'mode' is 'EncodeConfiguration'
                else if (arguments[NameConstants.ModeArgumentName] == LauncherMode.EncodeConfiguration.ToString())
                {
                    using (var fileStream = File.OpenRead(arguments[NameConstants.ConfigurationFilePathArgumentName]))
                    using (var streamReader = new StreamReader(fileStream, Encoding.UTF8))
                    {
                        // Encode the contents of the specified file and write the result to the console
                        String fileContents = streamReader.ReadToEnd();
                        var encoder = new Base64StringEncoder();
                        var encodedString = encoder.Encode(fileContents);
                        Console.WriteLine(encodedString);
                    }
                }
                else
                {
                    throw new Exception($"Encountered unhandled {nameof(LauncherMode)} '{arguments[NameConstants.ModeArgumentName]}'.");
                }
            }
            catch (CommandLineArgumentInvalidException e)
            {
                Console.WriteLine($"ERROR: {e.Message}");
                Console.WriteLine(GenerateUsageMessage());
            }
        }

        /// <summary>
        /// Generates a 'usage' message for the launcher, to display to the user.
        /// </summary>
        /// <returns>The 'usage' message.</returns>
        protected static String GenerateUsageMessage()
        {
            var argumentValidatorConverter = new ArgumentValidatorConverter();
            var usageMessageStringBuilder = new StringBuilder();
            String executableName = System.AppDomain.CurrentDomain.FriendlyName;
            usageMessageStringBuilder.AppendLine("USAGE: ");
            usageMessageStringBuilder.AppendLine($"  {executableName} -{NameConstants.ModeArgumentName} Launch -{NameConstants.ComponentArgumentName} [ApplicationAccess component] -{NameConstants.ListenPortArgumentName} [TCP port to listen on] -{NameConstants.MinimumLogLevelArgumentName} [log level] -{NameConstants.EncodedJsonConfigurationArgumentName} [Base64 encoded JSON configuration]");
            usageMessageStringBuilder.AppendLine($"  {executableName} -{NameConstants.ModeArgumentName} EncodeConfiguration -{NameConstants.ConfigurationFilePathArgumentName} [path to JSON configuration to encode]");
            usageMessageStringBuilder.AppendLine();
            usageMessageStringBuilder.AppendLine("EXAMPLES: ");
            usageMessageStringBuilder.AppendLine($"  {executableName} -{NameConstants.ModeArgumentName} {LauncherMode.Launch} -{NameConstants.ComponentArgumentName} {AccessManagerComponent.ReaderWriterNode} -{NameConstants.ListenPortArgumentName} 5001 -{NameConstants.MinimumLogLevelArgumentName} {LogLevel.Critical} -{NameConstants.EncodedJsonConfigurationArgumentName} H4sIAAAAAAACCqpWUApLzClNVbJSUPIsVvAK9vdTUqgFAAAA//8=");
            usageMessageStringBuilder.AppendLine($"  {executableName} -{NameConstants.ModeArgumentName} {LauncherMode.EncodeConfiguration} C:\\Temp\\ReaderWriterNodeConfiguration.json");
            usageMessageStringBuilder.AppendLine();
            usageMessageStringBuilder.AppendLine("ARGUMENT VALUES: ");
            usageMessageStringBuilder.AppendLine($"  -{NameConstants.ModeArgumentName}: {argumentValidatorConverter.StringifyEnumValues<LauncherMode>()}");
            usageMessageStringBuilder.AppendLine($"  -{NameConstants.ComponentArgumentName}: {argumentValidatorConverter.StringifyEnumValues<AccessManagerComponent>()}");
            usageMessageStringBuilder.AppendLine($"  -{NameConstants.MinimumLogLevelArgumentName}: {argumentValidatorConverter.StringifyEnumValues<LogLevel>()}");

            return usageMessageStringBuilder.ToString();
        }
    }
}