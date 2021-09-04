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
                log.Information(".NET Core runtime version: {netcoreruntime}",Directory.GetParent(typeof(object).Assembly.Location).Name);


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

            MapOptions<CommandLineOptions> commandLineOptions = TryParseCommandLine(args);
            if (commandLineOptions == null)
            {
                Environment.Exit(1);
                return null;
            }

            CommandLineOptions cmdLineOptions = new CommandLineOptions();
            new ConfigurationBuilder().AddCommandLineOptions(commandLineOptions).Build().Bind(cmdLineOptions);

            var contentRoot = GetContentRoot();
            var dataDir = GetOrCreateDataDir(cmdLineOptions);
            Console.WriteLine($"DataDir: {dataDir}");
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


        private static MapOptions<CommandLineOptions> TryParseCommandLine(string[] args)
        {
            MapOptions<CommandLineOptions> map = new MapOptions<CommandLineOptions>();

            bool help = false;

            OptionSet options = new OptionSet
            {
                { "h|help|?", "Shows this message and exits", _ =>  help = true},
                { "service", "Required to run when registered as a Windows service. To register service: sc.exe create Smtp4dev binPath= \"{PathToExe} --service\"", _ => { } },
                { "urls=", "The URLs the web interface should listen on. For example, http://localhost:123. Use `*` in place of hostname to listen for requests on any IP address or hostname using the specified port and protocol (for example, http://*:5000)", data => map.Add(data, x => x.Urls) },
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
                { "relaytlsmode=",  "Sets the TLS mode when connecting to relay SMTP server. See: http://www.mimekit.net/docs/html/T_MailKit_Security_SecureSocketOptions.htm", data => map.Add(data, x=> x.RelayOptions.TlsMode) },
                { "imapport=", "Specifies the port the IMAP server will listen on - allows standard email clients to view/retrieve messages", data => map.Add(data, x=> x.ServerOptions.ImapPort) },
                { "nousersettings", "Skip loading of appsetttings.json file in %APPDATA%", data => map.Add((data !=null).ToString(), x=> x.NoUserSettings) },
                { "debugsettings", "Prints out most settings values on startup", data => map.Add((data !=null).ToString(), x=> x.DebugSettings) },
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
