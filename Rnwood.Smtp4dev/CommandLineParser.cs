using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CommandLiners;
using Mono.Options;

namespace Rnwood.Smtp4dev
{
    public static class CommandLineParser
    {
        public static MapOptions<CommandLineOptions> TryParseCommandLine(IEnumerable<string> args, bool isDesktopApp)
        {
            MapOptions<CommandLineOptions> map = new MapOptions<CommandLineOptions>();
            StringWriter errorStream = new StringWriter();

            bool help = false;
            bool hadBadArgs = false;

            OptionSet options = new OptionSet
            {
                { "h|help|?", "Shows this message and exits", _ =>  help = true},
                { "baseappdatapath=","Set the base config and appData path", data => map.Add(data, x => x.BaseAppDataPath)},
                { "hostname=", "Specifies the server hostname. Used in auto-generated TLS certificate for SMTP if enabled.", data => map.Add(data, x => x.ServerOptions.HostName) },
                { "allowremoteconnections", "Specifies if remote connections will be allowed to the SMTP and IMAP servers. Use -allowremoteconnections+ to enable or -allowremoteconnections- to disable", data => map.Add((data !=null).ToString(), x => x.ServerOptions.AllowRemoteConnections) },
                { "disableipv6", "If true, SMTP and IMAP servers will NOT listen using IPv6 Dual Stack", data => map.Add((data !=null).ToString(), x => x.ServerOptions.DisableIPv6)},
                { "smtpport=", "Set the port the SMTP server listens on. Specify 0 to assign automatically", data => map.Add(data, x => x.ServerOptions.Port) },
                { "db=", "Specifies the path where the database will be stored relative to APPDATA env var on Windows or XDG_CONFIG_HOME on non-Windows. Specify \"\" to use an in memory database.", data => map.Add(data, x => x.ServerOptions.Database) },
                { "messagestokeep=", "Specifies the number of messages to keep per mailbox", data => map.Add(data, x => x.ServerOptions.NumberOfMessagesToKeep) },
                { "sessionstokeep=", "Specifies the number of sessions to keep", data => map.Add(data, x => x.ServerOptions.NumberOfSessionsToKeep) },
                { "tlsmode=", "Specifies the TLS mode to use for SMTP. None=Off. StartTls=On demand if client supports STARTTLS. ImplicitTls=TLS as soon as connection is established.", data => map.Add(data, x => x.ServerOptions.TlsMode) },
                { "tlscertificate=", "Specifies the TLS certificate file to use for SMTP if TLS is enabled/requested. This must be an X509 certificate - generally a .CER, .CRT or .PFX file. If using .CER or .CRT, you must provide the private key separately using --tlscertificateprivatekey.  Specify \"\" to use an auto-generated self-signed certificate (then see console output on first startup).", data => map.Add(data, x => x.ServerOptions.TlsCertificate) },
                { "tlscertificateprivatekey=", "Specifies the corresponding private key file for the SMTP TLS certificate if the private key is not part of the TlsCertificate file", data => map.Add(data, x => x.ServerOptions.TlsCertificatePrivateKey) },
                { "tlscertificatepassword=", "Specifies the password for  SMTP TLSCertificate/TlsCertificatePrivateKey", data => map.Add(data, x => x.ServerOptions.TlsCertificatePassword) },
                { "relaysmtpserver=", "Sets the name of the SMTP server that will be used to relay messages or \"\" if messages relay should not be allowed", data => map.Add(data, x => x.RelayOptions.SmtpServer) },
                { "relaysmtpport=", "Sets the port number for the SMTP server used to relay messages", data => map.Add(data, x => x.RelayOptions.SmtpPort) },
                { "relayautomaticallyemails=", "A comma separated list of recipient addresses for which messages will be relayed automatically. An empty list means that no messages are relayed", data => map.Add(data, x => x.RelayOptions.AutomaticEmailsString) },
                { "relaysenderaddress=", "Specifies the address used in MAIL FROM when relaying messages. (Sender address in message headers is left unmodified). The sender of each message is used if not specified.", data => map.Add(data, x => x.RelayOptions.SenderAddress) },
                { "relayusername=", "The username for the SMTP server used to relay messages. If \"\" no authentication is attempted", data => map.Add(data, x => x.RelayOptions.Login) },
                { "relaypassword=", "The password for the SMTP server used to relay messages", data => map.Add(data, x => x.RelayOptions.Password) },
                { "relaytlsmode=",  "Sets the TLS mode when connecting to relay SMTP server. See: http://www.mimekit.net/docs/html/T_MailKit_Security_SecureSocketOptions.htm", data => map.Add(data, x => x.RelayOptions.TlsMode) },
                { "imapport=", "Specifies the port the IMAP server will listen on - allows standard email clients to view/retrieve messages", data => map.Add(data, x => x.ServerOptions.ImapPort) },
                { "nousersettings", "Skip loading of appsetttings.json file in %APPDATA%", data => map.Add((data != null).ToString(), x => x.NoUserSettings) },
                { "debugsettings", "Prints out most settings values on startup", data => map.Add((data != null).ToString(), x => x.DebugSettings) },
                { "recreatedb", "Recreates the DB on startup if it already exists", data => map.Add((data != null).ToString(), x => x.ServerOptions.RecreateDb) },
                { "locksettings", "Locks settings from being changed by user via web interface", data => map.Add((data != null).ToString(), x => x.ServerOptions.LockSettings) },
                { "installpath=", "Sets path to folder containing wwwroot and other files", data => map.Add(data, x => x.InstallPath) },
                { "disablemessagesanitisation", "Disables message HTML sanitisation.", data => map.Add((data != null).ToString(), x => x.ServerOptions.DisableMessageSanitisation) },
                { "applicationName=","",  data => map.Add(data, x => x.ApplicationName), true},
                { "authenticationrequired", "Requires that SMTP and IMAP clients authenticate", data => map.Add((data != null).ToString(), x => x.ServerOptions.AuthenticationRequired) },
                { "secureconnectionrequired", "Requires that SMTP clients use SSL/TLS", data => map.Add((data != null).ToString(), x => x.ServerOptions.SecureConnectionRequired) },
                { "smtpallowanycredentials", "True if the SMTP server will allow any credentials to be used without checking them again the 'Users'", data => map.Add((data != null).ToString(), x => x.ServerOptions.SmtpAllowAnyCredentials) },
                { "webauthenticationrequired", "Require authentication for web interface", data => map.Add((data != null).ToString(), x => x.ServerOptions.WebAuthenticationRequired) },
                { "user=", "Adds a user and password combination for web, SMTP and IMAP. Use format username=password. This option can be repeated to add multiple users.", data =>
                       map.Add(data, x => x.ServerOptions.Users)},
                { "SmtpAuthTypesNotSecure=", "SMTP auth type enabled when not using secure connection (choices: ANONYMOUS, PLAIN, LOGIN, CRAM-MD5). Separate values with comma.", data =>
                       map.Add(data, x => x.ServerOptions.SmtpEnabledAuthTypesWhenNotSecureConnection) },
                { "SmtpAuthTypesSecure=", "SMTP auth type enabled when  using secure connection (choices: ANONYMOUS, PLAIN, LOGIN, CRAM-MD5). Separate values with comma.", data =>
                       map.Add(data, x => x.ServerOptions.SmtpEnabledAuthTypesWhenSecureConnection) },
                { "mailbox=", "Adds a mailbox in Name=Recipients format (Recipients can contain comma separated wildcards or regex)see appsettings for more details). This option can be repeated to add multiple users.", data =>
                       map.Add(data, x => x.ServerOptions.Users)},


            };

            if (!isDesktopApp)
            {
                options.Add("service", "Required to run when registered as a Windows service. To register service: sc.exe create Smtp4dev binPath= \"{PathToExe} --service\"", _ => { });

                options.Add(
                 "urls=", "The URLs the web interface should listen on. For example, http://localhost:123. Use `*` in place of hostname to listen for requests on any IP address or hostname using the specified port and protocol (for example, http://*:5000). Separate multiple values with ;. For info about HTTPS see https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-8.0#configure-https-in-appsettingsjson", data => map.Add(data, x => x.Urls));

                options.Add(
                 "basepath=", "Specifies the virtual path from web server root where SMTP4DEV web interface will be hosted. e.g. \"/\" or \"/smtp4dev\"", data => map.Add(data, x => x.ServerOptions.BasePath));

            }


            try
            {
                List<string> badArgs = options.Parse(args);
                if (badArgs.Any())
                {
                    errorStream.WriteLine("Unrecognised command line arguments: " + string.Join(" ", badArgs));
                    hadBadArgs = true;
                }

            }
            catch (OptionException e)
            {
                errorStream.WriteLine("Invalid command line: " + e.Message);
hadBadArgs = true;
            }

            if (help || hadBadArgs)
{
    errorStream.WriteLine();
    errorStream.WriteLine(" > For information about default values see documentation in appsettings.json.");
    errorStream.WriteLine();
    options.WriteOptionDescriptions(errorStream);
    throw new CommandLineOptionsException(errorStream.ToString()) { IsHelpRequest = help };
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