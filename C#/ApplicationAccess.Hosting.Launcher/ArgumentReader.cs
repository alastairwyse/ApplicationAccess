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
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ApplicationAccess.Hosting.Launcher
{
    /// <summary>
    /// Reads the command line arguments passed to the launcher, performing basic structure validation, and parsing them as name/value pairs.
    /// </summary>
    public class ArgumentReader
    {
        public Dictionary<String, String> Read(String[] arguments)
        {
            var returnDictionary = new Dictionary<String, String>();
            for (Int32 i = 0; i < arguments.Length; i = i + 2)
            {
                String currentParameterName = arguments[i].Trim();
                if (currentParameterName[0] != '-')
                {
                    throw new Exception($"Encountered unknown parameter name '{currentParameterName}'");
                }
                currentParameterName = currentParameterName.Substring(1);
                if (NameConstants.AllArguments.Contains(currentParameterName) == false)
                {
                    throw new Exception($"Encountered unknown parameter name '{currentParameterName}'");
                }
                if (i == (arguments.Length - 1))
                {
                    throw new Exception($"Missing value for parameter '{currentParameterName}'");
                }
                String currentParameterValue = arguments[i + 1].Trim();
                returnDictionary.Add(currentParameterName, currentParameterValue);
            }

            return returnDictionary;
        }
    }
}
