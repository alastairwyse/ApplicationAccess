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
    public class Program
    {
        static void Main(string[] args)
        {
            // TODO:
            //   Read command line params... return error back to console in case of error
            //   Should be ComponentName, Port, Base64'd config, Log level (should be warning or information)
            //   Decode the config... look for 'appsettings-template.json'... inject the log level and other config into the file on memory and then force overwrite appsettings.json
            //   Have 2 level options for logging... info and warning (where info will show HTTP request info)
            //     Should be validated and replace value in Logging.LogLevel.Microsoft.AspNetCore 
            //   Start the exe, doing the console redirect thing
            //   ALSO - See if the name of the exe that Windows reports can be changed to be the underlying component rather than 'Launcher'
            //     (e.g. for better cleanliness when errors appear in event viewer etc...)
            //   Set environment (should be able to do with '--environment "Production"')
            //     Also don't forget the port opening command line thing '--urls http://0.0.0.0:5000'
            //   Test what happens if config is wrong etc... can I clearly see the error?
            //   Should have a file appSettingsTemplate.json (or above with dash... figure out which is more correct) in each project with the base/default values
            //   SOME KIND OF 'MODE' type param to either emcrypt config, OR launch a component
            //     -mode encrypt -file C:\Temp\Somefile.jxon
            //     -mode launch -component ReaderWriter -port 5001 -config [base64]
            //     'mode' options should be 'encodeConfiguration' and 'launch'

            Console.WriteLine("Hello, World!");
        }
    }
}