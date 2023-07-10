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
using System.Diagnostics;

namespace ApplicationAccess.Hosting.Launcher
{
    /// <summary>
    /// Sets up parameters/arguments for and starts one of the access manager components as a separate process.
    /// </summary>
    public class ComponentLauncher
    {
        /// <summary>Maps an access manager component to the name of its executable.</summary>
        protected Dictionary<AccessManagerComponent, String> componentToExecutableNameMap;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Launcher.ComponentLauncher class.
        /// </summary>
        public ComponentLauncher()
        {
            componentToExecutableNameMap = new Dictionary<AccessManagerComponent, String>()
            {
                { AccessManagerComponent.EventCacheNode, "ApplicationAccess.Hosting.Rest.EventCache.exe" },
                { AccessManagerComponent.ReaderNode, "ApplicationAccess.Hosting.Rest.Reader.exe" },
                { AccessManagerComponent.ReaderWriterNode, "ApplicationAccess.Hosting.Rest.ReaderWriter.exe" },
                { AccessManagerComponent.WriterNode, "ApplicationAccess.Hosting.Rest.Writer.exe" }
            };
        }

        /// <summary>
        /// Launches/starts the specified access manager component.
        /// </summary>
        /// <param name="component">The component to launch.</param>
        /// <param name="arguments">The validated command line parameters/arguments to pass to the component.</param>
        public void Launch(AccessManagerComponent component, Dictionary<String, String> arguments)
        {
            var launchProcessStartInfo = new ProcessStartInfo(componentToExecutableNameMap[component], RenderArguments(arguments));
            launchProcessStartInfo.CreateNoWindow = true;
            launchProcessStartInfo.ErrorDialog = false;
            launchProcessStartInfo.RedirectStandardError = true;
            launchProcessStartInfo.RedirectStandardInput = true;
            launchProcessStartInfo.RedirectStandardOutput = true;
            launchProcessStartInfo.UseShellExecute = false;

            using (var launchProcess = new Process())
            {
                launchProcess.StartInfo = launchProcessStartInfo;
                Console.CancelKeyPress += (Object sender, ConsoleCancelEventArgs e) =>
                {
                    // This sends a CTRL-C to the launced process
                    launchProcess.StandardInput.WriteLine("\x3");
                };
                launchProcess.Start();
                launchProcess.WaitForExit();
            }
        }

        /// <summary>
        /// Converts a set of command line arguments to a single string which can be passed to the <see cref="ProcessStartInfo" /> class.
        /// </summary>
        /// <param name="arguments">The arguments to render.</param>
        /// <returns>The rendered arguments.</returns>
        protected String RenderArguments(Dictionary<String, String> arguments)
        {
            var renderedStringBuilder = new StringBuilder();
            renderedStringBuilder.Append($"--urls http://*:{arguments[NameConstants.ListenPortArgumentName]} ");
            renderedStringBuilder.Append($"--environment Production ");

            return renderedStringBuilder.ToString();
        }
    }
}
