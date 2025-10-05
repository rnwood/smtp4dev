using System;
using System.IO;

namespace Rnwood.Smtp4dev.TUI
{
    public static class DirectoryHelper
    {
        public static string GetDataDir(CommandLineOptions options)
        {
            string baseDataPath = options.BaseAppDataPath;
            
            if (string.IsNullOrEmpty(baseDataPath))
            {
                baseDataPath = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                    "smtp4dev"
                );
            }

            if (!Directory.Exists(baseDataPath))
            {
                Directory.CreateDirectory(baseDataPath);
            }

            return baseDataPath;
        }
    }
}
