using System;
using System.Text.Json.Nodes;
using Newtonsoft.Json.Linq;

namespace ApplicationAccess.Hosting.Rest.KubernetesDistributedInstanceManager
{
    public class SomeConfigurationOptions
    {
        public const String SomeConfigurationOptionsName = "SomeConfiguration";

        protected JObject jsonProp;

        public String FirstProperty { get; set; }
        public Int32 SecondProperty { get; set; }
        public Boolean ThirdProperty { get; set; }
        public Fruits FourthProperty { get; set; }

        public JObject JsonProp { set { jsonProp = value; } }

        public JObject GetJsonProp()
        {
            return jsonProp;
        }
    }
}
