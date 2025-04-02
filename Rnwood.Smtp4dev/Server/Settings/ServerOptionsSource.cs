using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using Ardalis.GuardClauses;
using Esprima.Ast;
using LumiSoft.Net;

namespace Rnwood.Smtp4dev.Server.Settings
{
    public record ServerOptionsSource
    {
        public string Urls { get; set; }

        private int? port;

        public int? Port { get; set; }
        public bool? AllowRemoteConnections { get; set; }

        public string Database { get; set; }

        public int? NumberOfMessagesToKeep { get; set; }
        public int? NumberOfSessionsToKeep { get; set; }

        public string BasePath { get; set; }

        public TlsMode? TlsMode { get; set; }

        public string TlsCertificate { get; set; }
        public string TlsCertificatePrivateKey { get; set; }

        public string TlsCertificatePassword { get; set; }

        public string HostName { get; set; }

        public int? ImapPort { get; set; }

        public bool? RecreateDb { get; set; }

        public bool? LockSettings { get; set; }

        public bool? DisableMessageSanitisation { get; set; }
        public string CredentialsValidationExpression { get; set; }
        public bool? AuthenticationRequired { get; set; }

        public bool? SmtpAllowAnyCredentials { get; set; }
        public bool? SecureConnectionRequired { get; set; }
        public string RecipientValidationExpression { get; set; }

        public string MessageValidationExpression { get; set; }
        public bool? DisableIPv6 { get; set; }

        public UserOptions[] Users { get; set; }

        public bool? WebAuthenticationRequired { get; set; }
        public bool? DeliverMessagesToUsersDefaultMailbox { get; set; }
        public string SmtpEnabledAuthTypesWhenNotSecureConnection { get; set; }

        public string SmtpEnabledAuthTypesWhenSecureConnection { get; set; }

        public MailboxOptions[] Mailboxes { get; set; }

        public SslProtocols[] SslProtocols { get; set; }

		public string TlsCipherSuites { get; set; }

	}
}
