using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using NLog;
using NLog.Web;
using Rnwood.Smtp4dev.Service;
using LogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace Rnwood.Smtp4dev
{
    public class Program
    {
        public static bool IsService { get; private set; }

        public static void Main(string[] args)
        {
            var logger = NLogBuilder.ConfigureNLog("nlog.config").GetCurrentClassLogger();
            if (!Debugger.IsAttached && args.Contains("--service"))
                IsService = true;

            try
            {
                logger.Debug("SMTP4DEV starting");
                var host = CreateWebHost(args.Where(arg => arg != "--service").ToArray());

                if (IsService)
                    host.RunAsSmtp4devService();
                else
                    host.Run();
            }
            catch (Exception e)
            {
                logger.Fatal(e, "Fatal exception encountered");
                throw;
            }
            finally
            {
                logger.Debug("SMTP4DEV stopping");
                LogManager.Flush();
                LogManager.Shutdown();
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
                            .AddJsonFile("appsettings.json", false, true)
                            .AddEnvironmentVariables()
                            .Build();
                    })
                .UseStartup<Startup>()
                .ConfigureLogging(logging =>
                {
                    logging.ClearProviders();
                    logging.SetMinimumLevel(LogLevel.Trace);
                })
                .UseNLog()
                .Build();
        }
    }
}