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

            CommandLineOptions cmdLineOptions = new CommandLineOptions();
            new ConfigurationBuilder().AddCommandLineOptions(commandLineOptions).Build().Bind(cmdLineOptions);


            Directory.SetCurrentDirectory(dataDir);

            return WebHost
                .CreateDefaultBuilder(args)
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

                        IConfigurationRoot config =
                            cb
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
                { "service", "Required to run when registered as a Windows service. To register service: sc.exe create Smtp4dev binPath= \"{PathToExe} --service\"", _ => { } },
                { "urls=", "The URLs the web interface should listen on. For example, http://localhost:123. Use `*` in place of hostname to listen for requests on any IP address or hostname using the specified port and protocol (for example, http://*:5000)", _ => { } },
                { "hostname=", "Specifies the server hostname. Used in auto-generated TLS certificate if enabled.", data => map.Add(data, x => x.ServerOptions.HostName) },
                { "allowremoteconnections", "Specifies if remote connections will be allowed to the SMTP and IMAP servers. Use -allowremoteconnections+ to enable or -allowremoteconnections- to disable", data => map.Add((data !=null).ToString(), x => x.ServerOptions.AllowRemoteConnections) },
                { "smtpport=", "Set the port the SMTP server listens on. Specify 0 to assign automatically", data => map.Add(data, x => x.ServerOptions.Port) },
                { "db=", "Specifies the path where the database will be stored relative to APPDATA env var on Windows or XDG_CONFIG_HOME on non-Windows. Specify \"\" to use an in memory database.", data => map.Add(data, x => x.ServerOptions.Database) },
                { "messagestokeep=", "Specifies the number of messages to keep", data => map.Add(data, x=> x.ServerOptions.NumberOfMessagesToKeep) },
                { "sessionstokeep=", "Specifies the number of sessions to keep", data => map.Add(data, x=> x.ServerOptions.NumberOfSessionsToKeep) },
                { "tlsmode=", "Specifies the TLS mode to use. None=Off. StartTls=On demand if client supports STARTTLS. ImplicitTls=TLS as soon as connection is established.", data => map.Add(data, x=> x.ServerOptions.TlsMode) },
                { "tlscertificate=", "Specifies the TLS certificate to use if TLS is enabled/requested. Specify \"\" to use an auto-generated self-signed certificate (then see console output on first startup)", data => map.Add(data, x=> x.ServerOptions.TlsCertificate) },
                { "basepath=", "Specifies the virtual path from web server root where SMTP4DEV web interface will be hosted. e.g. \"/\" or \"/smtp4dev\"", data => map.Add(data, x => x.ServerOptions.BasePath) },
                { "relaysmtpserver=", "Sets the name of the SMTP server that will be used to relay messages or \"\" if messages relay should not be allowed", data => map.Add(data, x=> x.RelayOptions.SmtpServer) },
                { "relaysmtpport=", "Sets the port number for the SMTP server used to relay messages", data => map.Add(data, x=> x.RelayOptions.SmtpServer) },
                { "relayautomaticallyemails=", "A comma separated list of recipient addresses for which messages will be relayed automatically. An empty list means that no messages are relayed", data => map.Add(data, x=> x.RelayOptions.AutomaticEmailsString) },
                { "relaysenderaddress=", "Specifies the address used in MAIL FROM when relaying messages. (Sender address in message headers is left unmodified). The sender of each message is used if not specified.", data => map.Add(data, x=> x.RelayOptions.SenderAddress) },
                { "relayusername=", "The username for the SMTP server used to relay messages. If \"\" no authentication is attempted", data => map.Add(data, x=> x.RelayOptions.Login) },
                { "relaypassword=", "The password for the SMTP server used to relay messages", data => map.Add(data, x=> x.RelayOptions.Password) },
                { "imapport=", "Specifies the port the IMAP server will listen on - allows standard email clients to view/retrieve messages", data => map.Add(data, x=> x.ServerOptions.ImapPort) },
                { "nousersettings", "Skip loading of appsetttings.json file in %APPDATA%", data => map.Add((data !=null).ToString(), x=> x.NoUserSettings) },
                { "recreatedb", "Recreates the DB on startup if it already exists", data => map.Add((data !=null).ToString(), x=> x.ServerOptions.RecreateDb) }
            };

            try
            {
                List<string> badArgs = options.Parse(args);
                if (badArgs.Any())
                {
                    Console.Error.WriteLine("Unrecognised command line arguments: " + string.Join(" ", badArgs));
                    help = true;
                }

            }
            catch (OptionException e)
            {
                Console.Error.WriteLine("Invalid command line: " + e.Message);
                help = true;
            }

            if (help)
            {
                Console.Error.WriteLine();
                Console.Error.WriteLine(" > For information about default values see documentation in appsettings.json.");
                Console.Error.WriteLine();
                options.WriteOptionDescriptions(Console.Error);
                return null;
            }
            else
            {
                Console.WriteLine();
                Console.WriteLine(" > For help use argument --help");
                Console.WriteLine();
            }

            return map;
        }
    }

    class CommandLineOptions
    {
        public ServerOptions ServerOptions { get; set; }
        public RelayOptions RelayOptions { get; set; }

        public bool NoUserSettings { get; set; }

    }
}
