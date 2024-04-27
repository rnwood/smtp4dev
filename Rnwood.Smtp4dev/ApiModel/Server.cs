using Rnwood.Smtp4dev.Server.Settings;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class Server
    {
        public bool SettingsAreEditable { get; internal set; }

        public IDictionary<string, string> LockedSettings { get; internal set; }

        public bool IsRunning { get; set; }

        public string Exception { get; internal set; }

        public int Port { get; set; }

        public string HostName { get; set; }

        public bool AllowRemoteConnections { get; set; }

        public int NumberOfMessagesToKeep { get; set; }

        public int NumberOfSessionsToKeep { get; set; }

        public int? ImapPort { get; set; }

        public bool DisableMessageSanitisation { get; set; }
        public string TlsMode { get; set; }
        public bool AuthenticationRequired { get; set; }
        public string CredentialsValidationExpression { get; set; }
        public bool SecureConnectionRequired { get; set; }
        public string RecipientValidationExpression { get; set; }
        public string MessageValidationExpression { get; set; }
		public bool DisableIPv6 { get; set; }
        public UserOptions[] Users { get; set; }

        public MailboxOptions[] Mailboxes { get; set; }

        public string RelaySmtpServer { get; set; }

        public int RelaySmtpPort { get; set; }

        public string[] RelayAutomaticEmails { get; set; }

        public string RelaySenderAddress { get; set; }

        public string RelayLogin { get; set; }

        public string RelayPassword { get; set; }

        public string RelayTlsMode { get; set; }
        public string RelayAutomaticRelayExpression { get; set; }
        public bool WebAuthenticationRequired { get; set; }
        public bool DesktopMinimiseToTrayIcon { get;  set; }
        public bool IsDesktopApp { get; internal set; }
        public bool SmtpAllowAnyCredentials { get; set; }

        public string[] SmtpEnabledAuthTypesWhenSecureConnection { get; set; }
        public string[] SmtpEnabledAuthTypesWhenNotSecureConnection { get; set; }

        public string CurrentUserName { get; set; }

        public string CurrentUserDefaultMailboxName { get; set; }
    }

}
