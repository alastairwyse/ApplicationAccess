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
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using ApplicationAccess.Hosting.Rest.Controllers;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Controllers
{
    /// <summary>
    /// Controller which exposes methods on the <see cref="IKubernetesDistributedInstanceManager"/> interface as REST methods.
    /// </summary>
    [ApiController]
    [ApiVersion("1")]
    [Route("api/v{version:apiVersion}")]
    public class KubernetesDistributedInstanceManagerController
    {
        #pragma warning disable 1591

        protected IKubernetesDistributedInstanceManager kubernetesDistributedInstanceManager;
        protected ILogger<KubernetesDistributedInstanceManagerController> logger;

        public KubernetesDistributedInstanceManagerController(KubernetesDistributedInstanceManagerHolder kubernetesDistributedInstanceManagerHolder, ILogger<KubernetesDistributedInstanceManagerController> logger)
        {
            kubernetesDistributedInstanceManager = kubernetesDistributedInstanceManagerHolder.KubernetesDistributedInstanceManager;
            this.logger = logger;
        }

        #pragma warning restore 1591

        /// <summary>
        /// Sets the URL for the distributed operation router component used for shard group splitting.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <response code="201">The url was set.</response>
        [HttpPost]
        [Route("DistributedOperationRouterUrl/{url}")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult SetDistributedOperationRouterUrl(String url)
        {
            String decodedUrl = Uri.UnescapeDataString(url);
            kubernetesDistributedInstanceManager.DistributedOperationRouterUrl = ParseUriFromString(decodedUrl, nameof(url));

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// URL for a first writer component which is part of a shard group undergoing a split or merge operation.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <response code="201">The url was set.</response>
        [HttpPost]
        [Route("Writer1Url/{url}")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult SetWriter1Url(String url)
        {
            String decodedUrl = Uri.UnescapeDataString(url);
            kubernetesDistributedInstanceManager.Writer1Url = ParseUriFromString(decodedUrl, nameof(url));

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// URL for a second writer component which is part of a shard group undergoing a split or merge operation.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <response code="201">The url was set.</response>
        [HttpPost]
        [Route("Writer2Url/{url}")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult SetWriter2Url(String url)
        {
            String decodedUrl = Uri.UnescapeDataString(url);
            kubernetesDistributedInstanceManager.Writer2Url = ParseUriFromString(decodedUrl, nameof(url));

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        /// <summary>
        /// URL for the distributed operation coordinator component.
        /// </summary>
        /// <param name="url">The url.</param>
        /// <response code="201">The url was set.</response>
        [HttpPost]
        [Route("DistributedOperationCoordinatorUrl/{url}")]
        [ApiExplorerSettings(GroupName = "KubernetesDistributedInstanceManager")]
        [ProducesResponseType(StatusCodes.Status201Created)]
        public StatusCodeResult SetDistributedOperationCoordinatorUrl(String url)
        {
            String decodedUrl = Uri.UnescapeDataString(url);
            kubernetesDistributedInstanceManager.DistributedOperationCoordinatorUrl = ParseUriFromString(decodedUrl, nameof(url));

            return new StatusCodeResult(StatusCodes.Status201Created);
        }

        #region Private/Protected Methods

        /// <summary>
        /// Attempts to parse a stringified URL.
        /// </summary>
        /// <param name="stringifiedUrl">The stringified URL to parse.</param>
        /// <param name="stringifiedUrlParameterName">The name of the parameter holding the stringified URL.</param>
        /// <returns>The parsed and converted URL.</returns>
        protected Uri ParseUriFromString(String stringifiedUrl, String stringifiedUrlParameterName)
        {
            try
            {
                return new Uri(stringifiedUrl);
            }
            catch (Exception e)
            {
                throw new ArgumentException($"Parameter value '{stringifiedUrl}' could not be parsed as a URL.", stringifiedUrlParameterName, e);
            }
        }

        #endregion
    }
}
