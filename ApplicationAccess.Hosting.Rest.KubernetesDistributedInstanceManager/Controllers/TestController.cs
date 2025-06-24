using System;
using System.Collections.Generic;
using System.Net.Mime;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Models.Options;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TestController : ControllerBase
    {
        protected DistributedAccessManagerInstanceOptions distributedAccessManagerInstanceOptions;
        protected ReaderNodeAppSettingsConfigurationTemplate readerNodeAppSettingsConfigurationTemplate;

        public TestController
        (
            ReaderNodeAppSettingsConfigurationTemplate readerNodeAppSettingsConfigurationTemplate, 
            IOptions<DistributedAccessManagerInstanceOptions> distributedAccessManagerInstanceOptions
        )
        {
            this.readerNodeAppSettingsConfigurationTemplate = readerNodeAppSettingsConfigurationTemplate;
            this.distributedAccessManagerInstanceOptions = distributedAccessManagerInstanceOptions.Value;
        }

        [HttpGet]
        [Route("something")]
        public String Users()
        {
            Console.WriteLine("hmmm...");

            return readerNodeAppSettingsConfigurationTemplate.ToString(Newtonsoft.Json.Formatting.Indented);
        }
    }
}
