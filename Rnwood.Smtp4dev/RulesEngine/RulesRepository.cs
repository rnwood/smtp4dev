using System.Linq;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Rnwood.Smtp4dev.RulesEngine
{
    public class RulesRepository : IRulesRepository
    {
        private readonly IConfiguration configuration;

        public RulesRepository(IConfiguration configuration)
        {
            this.configuration = configuration;
        }

        private JToken Serialize(IConfiguration config)
        {
            JObject obj = new JObject();

            foreach (var child in config.GetChildren())
            {
                if (child.Path.EndsWith(":0"))
                {
                    var arr = new JArray();

                    foreach (var arrayChild in config.GetChildren())
                    {
                        arr.Add(Serialize(arrayChild));
                    }

                    return arr;
                }
                else
                {
                    obj.Add(child.Key, Serialize(child));
                }
            }

            if (!obj.HasValues && config is IConfigurationSection section)
            {
                if (bool.TryParse(section.Value, out bool boolean))
                {
                    return new JValue(boolean);
                }

                if (decimal.TryParse(section.Value, out decimal real))
                {
                    return new JValue(real);
                }

                if (long.TryParse(section.Value, out long integer))
                {
                    return new JValue(integer);
                }

                return new JValue(section.Value);
            }

            return obj;
        }

        public string GetWorkflowRulesAsJsonString(string workflowName)
        {
            const string WorkflowName = "WorkflowName";
            var cfg = Serialize(configuration);
            var rules = cfg?["Rules"];
            if (rules != null)
            {
                foreach (var ruleToken in rules)
                {
                    if (ruleToken[WorkflowName] == null) continue;
                    if (ruleToken[WorkflowName].ToString().Equals(workflowName))
                    {
                        return JsonConvert.SerializeObject(ruleToken);
                    }
                }
            }

            return string.Empty;
        }
    }
}