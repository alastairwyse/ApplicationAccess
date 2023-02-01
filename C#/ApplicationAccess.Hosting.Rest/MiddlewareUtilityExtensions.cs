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
using System.Net;
using System.Net.Mime;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Hosting.Rest
{
    /// <summary>
    /// Utility entension methods for configuring ASP.NET Core middleware.
    /// </summary>
    public static class MiddlewareUtilityExtensions
    {
        /// <summary>
        /// Overrides the default model-binding failure behaviour, to return a <see cref="HttpErrorResponse"/> object rather than the standard <see cref="ProblemDetails"/>.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/> to configure.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder MapModelBindingFailureToHttpErrorResponse(this IMvcBuilder builder)
        {
            builder.ConfigureApiBehaviorOptions((ApiBehaviorOptions behaviorOptions) =>
            {
                behaviorOptions.InvalidModelStateResponseFactory = (ActionContext context) =>
                {
                    var errorResponse = ConvertModelStateDictionaryToHttpErrorResponse(context.ModelState);
                    var serializer = new HttpErrorResponseJsonSerializer();
                    JObject serializedErrorResponse = serializer.Serialize(errorResponse);

                    var contentResult = new ContentResult();
                    contentResult.Content = serializedErrorResponse.ToString();
                    contentResult.ContentType = MediaTypeNames.Application.Json;
                    contentResult.StatusCode = StatusCodes.Status400BadRequest;

                    return contentResult;
                };
            });

            return builder;
        }

        /// <summary>
        /// Return an HTTP 406 (not acceptable) status if the content type specified in the request 'Accept' header is not supported.
        /// </summary>
        /// <param name="builder">The <see cref="IMvcBuilder"/> to configure.</param>
        /// <returns>The <see cref="IMvcBuilder"/>.</returns>
        public static IMvcBuilder ReturnHttpNotAcceptableOnUnsupportedAcceptHeader(this IMvcBuilder builder)
        {
            builder.AddMvcOptions(options =>
            {
                options.RespectBrowserAcceptHeader = true;
                options.ReturnHttpNotAcceptable = true;
            });

            return builder;
        }

        #region Private/Protected Methods

        /// <summary>
        /// Converts a <see cref="ModelStateDictionary"/> in an invalid state to a <see cref="HttpErrorResponse"/>.
        /// </summary>
        /// <param name="modelStateDictionary">The ModelStateDictionary to convert.</param>
        /// <returns>The ModelStateDictionary converted to an HttpErrorResponse.</returns>
        private static HttpErrorResponse ConvertModelStateDictionaryToHttpErrorResponse(ModelStateDictionary modelStateDictionary)
        {
            if (modelStateDictionary.IsValid == true)
                throw new Exception($"Cannot convert a {nameof(ModelStateDictionary)} in a valid state.");

            var errorAttributes = new List<Tuple<String, String>>();
            foreach (KeyValuePair<String, ModelStateEntry> currentKvp in modelStateDictionary)
            {
                if (currentKvp.Value.ValidationState == ModelValidationState.Invalid)
                {
                    foreach (ModelError currentError in currentKvp.Value.Errors)
                    {
                        errorAttributes.Add(new Tuple<String, String>("Property", currentKvp.Key));
                        errorAttributes.Add(new Tuple<String, String>("Error", currentError.ErrorMessage));
                    }
                }
            }
            var returnErrorResponse = new HttpErrorResponse(HttpStatusCode.BadRequest.ToString(), new ValidationProblemDetails().Title, errorAttributes);

            return returnErrorResponse;
        }

        #endregion
    }
}
