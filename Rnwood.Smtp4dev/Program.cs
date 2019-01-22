using System;
using System.Collections.Generic;
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
        public const int DEFAULT_WEB_PORT = 5000;

        public static bool IsService { get; private set; }

        public static void Main(string[] args)
        {
            Version version = typeof(Program).Assembly.GetName().Version;
            Console.WriteLine($"smtp4dev version {version}");
            Console.WriteLine("https://github.com/rnwood/smtp4dev\n");

            if (!Debugger.IsAttached && args.Contains("--service"))
                IsService = true;

            var host = BuildWebHost(args.Where(arg => arg != "--service").ToArray());

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
            string installLocation = Path.GetDirectoryName(typeof(Program).Assembly.Location);

            if (Directory.Exists(Path.Join(installLocation, "wwwroot")))
            {
                return installLocation;
            }

            string cwd = Directory.GetCurrentDirectory();
            if (Directory.Exists(Path.Join(cwd, "wwwroot")))
            {
                return cwd;
            }

            throw new ApplicationException($"Unable to find wwwroot in either '{installLocation}' or the CWD '{cwd}'");
        }

        private static IWebHost BuildWebHost(string[] args)
        {
            Directory.SetCurrentDirectory(GetContentRoot());
            var webPort = ReadWebPortFromArgs(args);

            return WebHost
                .CreateDefaultBuilder(args)
                .UseContentRoot(Directory.GetCurrentDirectory())
                .ConfigureAppConfiguration(
                    (hostingContext, config) =>
                        {
                            var env = hostingContext.HostingEnvironment;
                            config
                                .SetBasePath(env.ContentRootPath)
                                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                                .AddEnvironmentVariables()
                                .AddCommandLine(args, new
                                Dictionary<string, string>{
                                    { "--smtpport", "ServerOptions:Port"},
                                    { "--db", "ServerOptions:Database" },
                                    { "--messagestokeep", "ServerOptions:NumberOfMessagesToKeep" },
                                    { "--sessionstokeep", "ServerOptions:NumberOfSessionsToKeep" }
                                })
                                .Build();
                        })
                .UseStartup<Startup>()
                .UseUrls($"http://*:{webPort}")
                .Build();
        }

        private static int ReadWebPortFromArgs(string[] args)
        {
            var index = Array.IndexOf(args,"--webport");
            if (index < 0 || args.Length < index+2)
                return DEFAULT_WEB_PORT;
            
            if(!int.TryParse(args[index+1], out var webPort))
                return DEFAULT_WEB_PORT;

            return webPort;
        }
    }
}
