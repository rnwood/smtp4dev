using System;
using System.IO;

namespace Rnwood.Smtp4dev.Tests.Resources
{
    public static class ResourceHelper
    {
        public static string GetResourcePath(string resourceName)
        {
            var path = AppDomain.CurrentDomain.BaseDirectory;
            return Path.Join(path, $"Resources{Path.DirectorySeparatorChar}{resourceName}");
        }
    }
}