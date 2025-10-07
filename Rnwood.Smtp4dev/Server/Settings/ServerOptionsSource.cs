using System.ComponentModel;
using System.Dynamic;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using Esprima.Ast;
using LumiSoft.Net;

namespace Rnwood.Smtp4dev.Server.Settings
{
    public record ServerOptionsSource
    {
        public string Urls { get; set; }

        public int? Port { get; set; }
        public bool? AllowRemoteConnections { get; set; }
        public string BindAddress { get; set; }

        public string Database { get; set; }

        public int? NumberOfMessagesToKeep { get; set; }
        public int? NumberOfSessionsToKeep { get; set; }

        public string BasePath { get; set; }

        public TlsMode? TlsMode { get; set; }
        public TlsMode? Pop3TlsMode { get; set; }

        public string TlsCertificateStoreThumbprint { get; set; }
        public string TlsCertificate { get; set; }
        public string TlsCertificatePrivateKey { get; set; }

        public string TlsCertificatePassword { get; set; }

        public string HostName { get; set; }

        public int? ImapPort { get; set; }
        public int? Pop3Port { get; set; }
        public bool? Pop3SecureConnectionRequired { get; set; }

        public bool? RecreateDb { get; set; }

        public bool? LockSettings { get; set; }

        public bool? DisableMessageSanitisation { get; set; }
        public string CredentialsValidationExpression { get; set; }
        public bool? AuthenticationRequired { get; set; }

        public bool? SmtpAllowAnyCredentials { get; set; }
        public bool? SecureConnectionRequired { get; set; }
        public string RecipientValidationExpression { get; set; }
        public string MessageValidationExpression { get; set; }
        public string CommandValidationExpression { get; set; }
        public bool? DisableIPv6 { get; set; }

        public UserOptions[] Users { get; set; }
        public bool? WebAuthenticationRequired { get; set; }

        public bool? DeliverMessagesToUsersDefaultMailbox { get; set; }
        public string SmtpEnabledAuthTypesWhenNotSecureConnection { get; set; }     

        public string SmtpEnabledAuthTypesWhenSecureConnection { get; set; }  

        public MailboxOptions[] Mailboxes { get; set; }

        public string SslProtocols { get; set; }

		public string TlsCipherSuites { get; set; }

        public string HtmlValidateConfig { get; set; }

        public bool? DisableHtmlValidation { get; set; }

        public bool? DisableHtmlCompatibilityCheck { get; set; }

        public long? MaxMessageSize { get; set; }

        public string DeliverToStdout { get; set; }

        public int? ExitAfterMessages { get; set; }
    }
}
