using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.IO;
using System.Linq;
using CommandLiners;
using CommandLiners.Options;
using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Builder.Extensions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Mono.Options;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Service;

namespace Rnwood.Smtp4dev
{
    public class Program
    {
        public static bool IsService { get; private set; }

        public static void Main(string[] args)
        {
            string version = FileVersionInfo.GetVersionInfo(typeof(Program).Assembly.Location).ProductVersion;
            Console.WriteLine($"smtp4dev version {version}");
            Console.WriteLine("https://github.com/rnwood/smtp4dev\n");
            Console.WriteLine($".NET Core runtime version: {Directory.GetParent(typeof(object).Assembly.Location).Name}\n");


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
            string contentRoot = GetContentRoot();

            string dataDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "smtp4dev");
            if (!Directory.Exists(dataDir))
            {
                Directory.CreateDirectory(dataDir);
            }

            //Migrate to new location
            if (File.Exists(Path.Join(contentRoot, "database.db")) && !File.Exists(Path.Join(dataDir, "database.db")))
            {
                File.Move(
                    Path.Join(contentRoot, "database.db"),
                    Path.Join(dataDir, "database.db")
                );
            }

            MapOptions<CommandLineOptions> commandLineOptions = TryParseCommandLine(args);
            if (commandLineOptions == null)
            {
                Environment.Exit(1);
                return null;
            }

            Directory.SetCurrentDirectory(dataDir);

            return WebHost
                .CreateDefaultBuilder(args)
                .UseContentRoot(contentRoot)
                .ConfigureAppConfiguration(
                    (hostingContext, configBuilder) =>
                    {
                        var env = hostingContext.HostingEnvironment;
                        var config = configBuilder
                            .SetBasePath(env.ContentRootPath)
                            .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                            .AddJsonFile($"appsettings.{env.EnvironmentName}.json", optional: true, reloadOnChange: true)
                            .AddJsonFile(Path.Join(dataDir, "appsettings.json"), optional: true, reloadOnChange: true)
                            .AddEnvironmentVariables()
                            .AddCommandLineOptions(commandLineOptions)
                            .Build();

                        hostingContext.HostingEnvironment.EnvironmentName = config["Environment"];
                    })
                .UseStartup<Startup>()
                .Build();
        }

        private static MapOptions<CommandLineOptions> TryParseCommandLine(string[] args)
        {
            MapOptions<CommandLineOptions> map = new MapOptions<CommandLineOptions>();

            bool help = false;

            OptionSet options = new OptionSet
            {
                { "h|help|?", "Shows this message and exits", _ =>  help = true},
                { "hostname=", "Specifies the server hostname. Used in auto-generated TLS certificate if enabled.", data => map.Add(data, x => x.ServerOptions.HostName) },
                { "allowremoteconnections", "Specifies if remote connections will be allowed to the SMTP server.", data => map.Add(data, x => x.ServerOptions.AllowRemoteConnections) },
                { "smtpport=", "Set the port the SMTP server listens on. Specify 0 to assign automatically", data => map.Add(data, x => x.ServerOptions.Port) },
                { "db=", "Specifies the path where the database will be stored relative to APPDATA env var on Windows or XDG_CONFIG_HOME on non-Windows. Specify \"\" to use an in memory database.", data => map.Add(data, x => x.ServerOptions.Database) },
                { "messagestokeep=", "Specifies the number of messages to keep", data => map.Add(data, x=> x.ServerOptions.NumberOfMessagesToKeep) },
                { "sessionstokeep=", "Specifies the number of sessions to keep", data => map.Add(data, x=> x.ServerOptions.NumberOfSessionsToKeep) },
                { "tlsmode=", "Specifies the TLS mode to use. None=Off. StartTls=On demand if client supports STARTTLS. ImplicitTls=TLS as soon as connection is established.", data => map.Add(data, x=> x.ServerOptions.TlsMode) },
                { "tlscertificate=", "Specifies the TLS certificate to use if TLS is enabled/requested. Specify \"\" to use an auto-generated self-signed certificate (then see console output on first startup)", data => map.Add(data, x=> x.ServerOptions.TlsCertificate) },
                { "rootpath=", "Specifies the virtual path from web server root where SMTP4DEV web interface will be hosted. e.g. '/smtp4dev'", data => map.Add(data, x => x.ServerOptions.RootPath) }
            };

            try
            {
                List<string> badArgs = options.Parse(args);
                if (badArgs.Any())
                {
                    Console.Error.WriteLine("Unrecognised command line arguments: " + string.Join(" ", badArgs));
                    help = true;
                }

            } catch (OptionException e)
            {
                Console.Error.WriteLine("Invalid command line: " + e.Message);
                help = true;
            }

            if (help)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine("For information about default values see documentation in appsettings.json.");
                Console.Error.WriteLine();
                options.WriteOptionDescriptions(Console.Error);
                return null;
            }

            return map;
        }
    }

    class CommandLineOptions
    {
        public ServerOptions ServerOptions { get; set; }
    }
}
