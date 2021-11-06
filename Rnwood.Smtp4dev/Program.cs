using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.Json;
using CommandLiners;
using CommandLiners.Options;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Mono.Options;
using Newtonsoft.Json.Linq;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Service;
using Serilog;

namespace Rnwood.Smtp4dev
{
    public class Program
    {
        public static bool IsService { get; private set; }
        private static ILogger log;

        public static void Main(string[] args)
        {
            SetupStaticLogger();
            log = Log.ForContext<Program>();

            try
            {
                string version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
                log.Information("smtp4dev version {version}",version);
                log.Information("https://github.com/rnwood/smtp4dev");
                log.Information(".NET Core runtime version: {netcoreruntime}", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);


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
            catch (Exception ex)
            {
                Log.Fatal(ex, "A unhandled exception occurred.");
            }
            finally
            {
                Log.CloseAndFlush();
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
            MapOptions<CommandLineOptions> commandLineOptions = CommandLineParser.TryParseCommandLine(args);
            if (commandLineOptions == null)
            {
                Environment.Exit(1);
                return null;
            }

            CommandLineOptions cmdLineOptions = new CommandLineOptions();
            new ConfigurationBuilder().AddCommandLineOptions(commandLineOptions).Build().Bind(cmdLineOptions);

            var contentRoot = GetContentRoot();
            var dataDir = GetOrCreateDataDir(cmdLineOptions);
            log.Information("DataDir: {dataDir}", dataDir);
            Directory.SetCurrentDirectory(dataDir);

            IWebHostBuilder builder = WebHost
                .CreateDefaultBuilder(args)
                .UseSerilog()
                .UseContentRoot(contentRoot)
                .ConfigureAppConfiguration(
                    (hostingContext, configBuilder) =>
                    {
                        IWebHostEnvironment env = hostingContext.HostingEnvironment;

                        IConfigurationBuilder cb = configBuilder
                            .SetBasePath(env.ContentRootPath)
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                        if (!cmdLineOptions.NoUserSettings)
                        {
                            cb = cb.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                            cb = cb.AddJsonFile(Path.Join(dataDir, "appsettings.json"), optional: true, reloadOnChange: true);
                        }

                        cb
.AddEnvironmentVariables()
.AddCommandLineOptions(commandLineOptions);

                        IConfigurationRoot config = cb
                            .Build();

                        hostingContext.HostingEnvironment.EnvironmentName = config["Environment"];

                        if (cmdLineOptions.DebugSettings)
                        {
                            JsonSerializerOptions jsonSettings = new JsonSerializerOptions { WriteIndented = true };
                            Console.WriteLine(JsonSerializer.Serialize(new
                            {
                                CmdLineArgs = Environment.GetCommandLineArgs(),
                                CmdLineOptions = cmdLineOptions,
                                ServerOptions = config.GetSection("ServerOptions").Get<ServerOptions>(),
                                RelayOption = config.GetSection("RelayOptions").Get<RelayOptions>()
                            }, jsonSettings));
                        }

                    })
                .UseStartup<Startup>();

            if (!string.IsNullOrEmpty(cmdLineOptions.Urls))
            {
                builder.UseUrls(cmdLineOptions.Urls);
            }

            return builder.Build();
        }


        private static string GetOrCreateDataDir(CommandLineOptions cmdLineOptions)
        {
            var dataDir = DirectoryHelper.GetDataDir(cmdLineOptions);
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }
            return dataDir;
        }

        private static void SetupStaticLogger()
        {
            var configuration = new ConfigurationBuilder()
                .AddJsonFile("appsettings.json")
                .Build();

            Log.Logger = new LoggerConfiguration()
                .ReadFrom.Configuration(configuration)
                .CreateLogger();
        }
    }
}
