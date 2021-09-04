using System;
using System.IO;

namespace Rnwood.Smtp4dev
{
    public static class DirectoryHelper
    {
        public static string GetDataDir(CommandLineOptions options)
        {
            return string.IsNullOrEmpty(options.BaseAppDataPath)
                ? Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "smtp4dev")
                : options.BaseAppDataPath;
        }
    }
}