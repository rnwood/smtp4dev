using System.IO;
using System.Reflection;

namespace Rnwood.Smtp4dev.Tests.Features.RulesEngine
{
    public static class ResourceHelper
    {
        /// <summary>
        /// Load rules from Embedded rules.json
        /// </summary>
        public static string LoadRules()
        {
            var assembly = Assembly.GetExecutingAssembly();
            var ns = typeof(RulesEngineTests).Namespace;
            var resourceName = $"{ns}.rules.json";

            using var stream = assembly.GetManifestResourceStream(resourceName);
            using var reader = new StreamReader(stream);
            return reader.ReadToEnd();
        }
    }
}