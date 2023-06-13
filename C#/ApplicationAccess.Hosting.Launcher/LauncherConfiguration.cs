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

namespace ApplicationAccess.Hosting.Launcher
{
    /// <summary>
    /// Model/container class holding access manager launcher configuration.
    /// </summary>
    public class LauncherConfiguration
    {
        /// <summary>The launcher mode.</summary>
        public LauncherMode? Mode;

        /// <summary>The component to launch.</summary>
        /// <remarks>Only applicable in <see cref="LauncherMode.Launch">Launch</see> mode.</remarks>
        public AccessManagerComponent? Component;

        /// <summary>The TCP port the launched component should listen on.</summary>
        /// <remarks>Only applicable in <see cref="LauncherMode.Launch">Launch</see> mode.</remarks>
        public Int32 ListenPort;

        /// <summary>The minimum level log events.</summary>
        /// <remarks>Only applicable in <see cref="LauncherMode.Launch">Launch</see> mode.</remarks>
        public LogLevel? MinimumLogLevel;

        /// <summary>String containing the JSON comfiguration for the component to launch, encoded so it can be received as a command line parameter.</summary>
        /// <remarks>Only applicable in <see cref="LauncherMode.Launch">Launch</see> mode.</remarks>
        public String EncodedJsonConfiguration;

        /// <summary>The full path to the JSON file containing component configuration.</summary>
        /// <remarks>Only applicable in <see cref="LauncherMode.EncodeConfiguration">EncodeConfiguration</see> mode.</remarks>
        public String ConfigurationFilePath;

        /// <summary>
        /// Initialises a new instance of the ApplicationAccess.Hosting.Launcher.LauncherConfiguration class.
        /// </summary>
        public LauncherConfiguration()
        {
            Mode = null;
            Component = null;
            ListenPort = -1;
            MinimumLogLevel = null;
            EncodedJsonConfiguration = null;
            ConfigurationFilePath = null;
        }
    }
}
