using System;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using Microsoft.Extensions.Hosting;

namespace Rnwood.Smtp4dev.Service
{
    public interface IHostingEnvironmentHelper
    {
        string GetSettingsFilePath();
    }

    public class HostingEnvironmentHelper : IHostingEnvironmentHelper
    {
        private readonly IHostEnvironment hostEnvironment;

        public HostingEnvironmentHelper(IHostEnvironment hostEnvironment)
        {
            this.hostEnvironment = hostEnvironment;
        }

        /// <summary>
        /// Check if this process is running on Windows in an in process instance in IIS
        /// </summary>
        /// <returns>True if Windows and in an in process instance on IIS, false otherwise</returns>
        private static bool IsRunningInProcessIIS()
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return false;
            }

            var processName = Path.GetFileNameWithoutExtension(Process.GetCurrentProcess().ProcessName);
            return (processName.Contains("w3wp", StringComparison.OrdinalIgnoreCase) ||
                    processName.Contains("iisexpress", StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>
        /// Get path to appsettings.json, for IIS this is the runtime path.
        /// </summary>
        /// <returns>appsettings.json filePath</returns>
        public string GetSettingsFilePath()
        {
            var dataDir = IsRunningInProcessIIS()
                ? Path.Join(hostEnvironment.ContentRootPath, "smtp4dev")
                : Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "smtp4dev");
            return Path.Join(dataDir, "appsettings.json");
        }
    }
}