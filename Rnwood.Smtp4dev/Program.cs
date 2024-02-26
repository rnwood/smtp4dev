using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading;
using System.Threading.Tasks;
using CommandLiners;
using CommandLiners.Options;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mono.Options;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Service;
using Serilog;

namespace Rnwood.Smtp4dev
{
    public class Program
    {
        public static bool IsService { get; private set; }

        private static ILogger _log;

        public static async Task Main(string[] args)
        {

            try
            {
                var host = await StartApp(args, false, null);

                if (host == null)
                {
                    Environment.Exit(1);
                }
                else
                {
                    await host.WaitForShutdownAsync();
                }
                Log.Information("Exiting");
            }
            catch (CommandLineOptionsException ex)
            {
                if (ex.IsHelpRequest)
                {
                    Log.Information(ex.Message);
                }
                else
                {
                    Log.Fatal(ex.Message);
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

        public static async Task<IHost> StartApp(IEnumerable<string> args, bool isDesktopApp, Action<CommandLineOptions> fixedOptions)
        {
            SetupStaticLogger(args);
            _log = Log.ForContext<Program>();

            string version = Assembly.GetExecutingAssembly().GetCustomAttribute<AssemblyInformationalVersionAttribute>().InformationalVersion;
            _log.Information("smtp4dev version {version}", version);
            _log.Information("https://github.com/rnwood/smtp4dev");
            _log.Information(".NET Core runtime version: {netcoreruntime}", System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription);


            if (!Debugger.IsAttached && args.Contains("--service"))
                IsService = true;

            MapOptions<CommandLineOptions> commandLineOptions = CommandLineParser.TryParseCommandLine(args, isDesktopApp);

            CommandLineOptions cmdLineOptions = new CommandLineOptions();
            new ConfigurationBuilder().AddCommandLineOptions(commandLineOptions).Build().Bind(cmdLineOptions);
            fixedOptions?.Invoke(cmdLineOptions);

            if (!string.IsNullOrEmpty(cmdLineOptions.InstallPath))
            {
                Directory.SetCurrentDirectory(cmdLineOptions.InstallPath);
            }

            _log.Information("Install location: {installpath}", Directory.GetCurrentDirectory());

            var host = BuildWebHost(args.Where(arg => arg != "--service").ToArray(), cmdLineOptions, commandLineOptions);

            await host.StartAsync();


            var addressesFeature = host.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>();
            var urls = addressesFeature.Addresses;

            foreach (var url in urls)
            {
                _log.Information("Now listening on: {url}", url);
            }

            return host;


        }

        private static string GetContentRoot()
        {
            string installLocation = AppContext.BaseDirectory;

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

        private static IHost BuildWebHost(string[] args, CommandLineOptions cmdLineOptions, MapOptions<CommandLineOptions> commandLineOptions)
        {


            var contentRoot = GetContentRoot();
            var dataDir = GetOrCreateDataDir(cmdLineOptions);
            _log.Information("DataDir: {dataDir}", dataDir);
            Directory.SetCurrentDirectory(dataDir);

            IHostBuilder builder = Host.CreateDefaultBuilder(args)
                .UseSerilog()
                .UseContentRoot(contentRoot)
                .ConfigureAppConfiguration(
                    (hostingContext, configBuilder) =>
                    {
                        IHostEnvironment env = hostingContext.HostingEnvironment;

                        IConfigurationBuilder cb = configBuilder
                            .SetBasePath(env.ContentRootPath)
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true);

                        if (!cmdLineOptions.NoUserSettings)
                        {
                            cb = cb.AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true);
                            cb = cb.AddJsonFile(Path.Join(dataDir, "appsettings.json"), optional: true, reloadOnChange: true);
                        }

                        cb.AddEnvironmentVariables()
                            .AddCommandLineOptions(commandLineOptions);

                        IConfigurationRoot config = cb
                            .Build();

                        hostingContext.HostingEnvironment.EnvironmentName = config["Environment"];

                        if (cmdLineOptions.DebugSettings)
                        {
                            Console.WriteLine(JsonSerializer.Serialize(new SettingsDebugInfo
                            {
                                CmdLineArgs = Environment.GetCommandLineArgs(),
                                CmdLineOptions = cmdLineOptions,
                                ServerOptions = config.GetSection("ServerOptions").Get<ServerOptions>(),
                                RelayOption = config.GetSection("RelayOptions").Get<RelayOptions>()
                            }, SettingsDebugInfoSerializationContext.Default.SettingsDebugInfo));
                        }

                    });


            builder.UseWindowsService(s => s.ServiceName = "smtp4dev");
            builder.ConfigureWebHostDefaults(c =>
            {
                c.UseStartup<Startup>();
                c.UseShutdownTimeout(TimeSpan.FromSeconds(10));

                if (!string.IsNullOrEmpty(cmdLineOptions.Urls))
                {

                    c.UseUrls(cmdLineOptions.Urls.Split(';', StringSplitOptions.RemoveEmptyEntries).Select(u => u.Trim()).ToArray());
                }
            });



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

        public static void SetupStaticLogger(IEnumerable<string> args)
        {
            try
            {
                IConfigurationRoot configuration =
                   new ConfigurationBuilder()
                       .AddJsonFile("appsettings.json")
                       .Build();

                var logConfigBuilder = new LoggerConfiguration()
    .ReadFrom.Configuration(configuration);
                if (args.Any(a => a.Equals("--service", StringComparison.OrdinalIgnoreCase)))
                {
                    logConfigBuilder.WriteTo.EventLog("smtp4dev");
                }
                Log.Logger = logConfigBuilder
                    .CreateLogger();
            }
            catch
            {
                //Ensure output goes somewhere if there's a config error.
                var logConfigBuilder = new LoggerConfiguration();
                if (args.Any(a => a.Equals("--service", StringComparison.OrdinalIgnoreCase)))
                {
                    logConfigBuilder.WriteTo.EventLog("smtp4dev");
                }else
                {
                    logConfigBuilder.WriteTo.Console();
                }
                Log.Logger = logConfigBuilder.CreateLogger();
                throw;
            }


        }
    }

    internal class SettingsDebugInfo
    {
        public string[] CmdLineArgs { get; set; }
        public CommandLineOptions CmdLineOptions { get; set; }
        public ServerOptions ServerOptions { get; set; }
        public RelayOptions RelayOption { get; set; }
    }

    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(SettingsDebugInfo))]
    internal partial class SettingsDebugInfoSerializationContext : JsonSerializerContext {

    }
}
