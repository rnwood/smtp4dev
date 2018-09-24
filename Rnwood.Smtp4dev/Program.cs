using System;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;

using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;

using Rnwood.Smtp4dev.Service;

namespace Rnwood.Smtp4dev
{
    public class Program
    {
        public static bool IsService { get; private set; }
    
        public static void Main(string[] args)
        {
            if (!Debugger.IsAttached && args.Contains("--service"))
                IsService = true;

            var host = CreateWebHost(args.Where(arg => arg != "--service").ToArray());
            
            if (IsService)
            {
                host.RunAsSmtp4devService();
            }
            else
            {
                host.Run();
            }
        }

        private static string GetContentRoot()
        {
            if (!IsService)
                return Directory.GetCurrentDirectory();

            var pathToExe = Process.GetCurrentProcess().MainModule.FileName;
            return Path.GetDirectoryName(pathToExe);
        }

        private static IWebHost CreateWebHost(string[] args)
        {
            return WebHost
                .CreateDefaultBuilder(args)
                .UseContentRoot(GetContentRoot())
                .ConfigureAppConfiguration(
                    (hostingContext, config) =>
                        {
                            var env = hostingContext.HostingEnvironment;
                            config
                                .SetBasePath(env.ContentRootPath)
                                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                .AddEnvironmentVariables()
                                .Build();
                        })
                .UseStartup<Startup>()
                .Build();
        }
    }
}
