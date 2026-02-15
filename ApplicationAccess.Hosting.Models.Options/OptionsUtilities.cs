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
using System.ComponentModel.DataAnnotations;

namespace ApplicationAccess.Hosting.Models.Options
{
    /// <summary>
    /// Utility methods for classes following the <see href="https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/options?view=aspnetcore-6.0">ASP.NET Core Options pattern</see>.
    /// </summary>
    public class OptionsUtilities
    {
        /// <summary>
        /// Generates a common/standard prefix for <see cref="ValidationException"/> messages.
        /// </summary>
        /// <param name="optionsName">The name of the Options object validation failed for.</param>
        /// <returns>The exception message prefix.</returns>
        public String GenerateValidationExceptionMessagePrefix(String optionsName)
        {
            return $"Error validating {optionsName} options.";
        }
    }
}
