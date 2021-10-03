using System;
using System.Collections.Generic;
using System.Linq;
using CommandLiners;
using Mono.Options;

namespace Rnwood.Smtp4dev
{
    public static class CommandLineParser
    {
        public static MapOptions<CommandLineOptions> TryParseCommandLine(string[] args)
        {
            MapOptions<CommandLineOptions> map = new MapOptions<CommandLineOptions>();

            bool help = false;

            OptionSet options = new OptionSet
            {
                { "h|help|?", "Shows this message and exits", _ =>  help = true},
                { "service", "Required to run when registered as a Windows service. To register service: sc.exe create Smtp4dev binPath= \"{PathToExe} --service\"", _ => { } },
                { "urls=", "The URLs the web interface should listen on. For example, http://localhost:123. Use `*` in place of hostname to listen for requests on any IP address or hostname using the specified port and protocol (for example, http://*:5000)", data => map.Add(data, x => x.Urls) },
                { "hostname=", "Specifies the server hostname. Used in auto-generated TLS certificate if enabled.", data => map.Add(data, x => x.ServerOptions.HostName) },
                { "baseappdatapath=","Set the base config and appData path", data=> map.Add(data,x=>x.BaseAppDataPath)},
                { "allowremoteconnections", "Specifies if remote connections will be allowed to the SMTP and IMAP servers. Use -allowremoteconnections+ to enable or -allowremoteconnections- to disable", data => map.Add((data !=null).ToString(), x => x.ServerOptions.AllowRemoteConnections) },
                { "smtpport=", "Set the port the SMTP server listens on. Specify 0 to assign automatically", data => map.Add(data, x => x.ServerOptions.Port) },
                { "db=", "Specifies the path where the database will be stored relative to APPDATA env var on Windows or XDG_CONFIG_HOME on non-Windows. Specify \"\" to use an in memory database.", data => map.Add(data, x => x.ServerOptions.Database) },
                { "messagestokeep=", "Specifies the number of messages to keep", data => map.Add(data, x=> x.ServerOptions.NumberOfMessagesToKeep) },
                { "sessionstokeep=", "Specifies the number of sessions to keep", data => map.Add(data, x=> x.ServerOptions.NumberOfSessionsToKeep) },
                { "tlsmode=", "Specifies the TLS mode to use. None=Off. StartTls=On demand if client supports STARTTLS. ImplicitTls=TLS as soon as connection is established.", data => map.Add(data, x=> x.ServerOptions.TlsMode) },
                { "tlscertificate=", "Specifies the TLS certificate to use if TLS is enabled/requested. Specify \"\" to use an auto-generated self-signed certificate (then see console output on first startup)", data => map.Add(data, x=> x.ServerOptions.TlsCertificate) },
                { "tlscertificateprivatekey=", "Specifies the TLS certificate private key. Ignored if tlscertificate is blank", data => map.Add(data, x=> x.ServerOptions.TlsCertificatePrivateKey) },
                { "basepath=", "Specifies the virtual path from web server root where SMTP4DEV web interface will be hosted. e.g. \"/\" or \"/smtp4dev\"", data => map.Add(data, x => x.ServerOptions.BasePath) },
                { "relaysmtpserver=", "Sets the name of the SMTP server that will be used to relay messages or \"\" if messages relay should not be allowed", data => map.Add(data, x=> x.RelayOptions.SmtpServer) },
                { "relaysmtpport=", "Sets the port number for the SMTP server used to relay messages", data => map.Add(data, x=> x.RelayOptions.SmtpPort) },
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
    }
}