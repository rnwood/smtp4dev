using System;
using System.IO;

namespace Rnwood.Smtp4dev
{
    public static class DirectoryHelper
    {
        public static string GetDataDir(CommandLineOptions options)
        {
            var path = string.IsNullOrEmpty(options.BaseAppDataPath)
                ? Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "smtp4dev")
                : options.BaseAppDataPath;

            if (!Path.IsPathRooted(path)) {
                path = Path.GetFullPath(path);
            }

            return path;
        }
    }
}