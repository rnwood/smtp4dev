using System.Net;
using Esprima.Ast;

namespace Rnwood.Smtp4dev.Server.Settings
{
    public record ServerOptions
    {
        public string Urls { get; set; }

        public int Port { get; set; } = 25;
        public bool AllowRemoteConnections { get; set; } = true;

        public string Database { get; set; } = "database.db";

        public int NumberOfMessagesToKeep { get; set; } = 100;
        public int NumberOfSessionsToKeep { get; set; } = 100;

        public string BasePath { get; set; } = "/";

        public TlsMode TlsMode { get; set; } = TlsMode.None;

        public string TlsCertificate { get; set; }
        public string TlsCertificatePrivateKey { get; set; }

        public string TlsCertificatePassword { get; set; } = "";

        public string HostName { get; set; } = Dns.GetHostName();

        public int? ImapPort { get; set; } = 143;

        public bool RecreateDb { get; set; }

        public bool LockSettings { get; set; } = false;

        public bool DisableMessageSanitisation { get; set; } = false;
        public string CredentialsValidationExpression { get; set; }
        public bool AuthenticationRequired { get; set; } = false;
        public bool SecureConnectionRequired { get; set; } = false;

        public bool SmtpAllowAnyCredentials { get; set; }
        public string RecipientValidationExpression { get; set; }

        public string MessageValidationExpression { get; set; }
        public bool DisableIPv6 { get; set; } = false;

        public UserOptions[] Users { get; set; } = [];

        public bool WebAuthenticationRequired { get; set; } = false;

        public string SmtpEnabledAuthTypesWhenNotSecureConnection { get; set; } = "PLAIN,LOGIN,CRAM-MD5";

        public string SmtpEnabledAuthTypesWhenSecureConnection { get; set; } = "PLAIN,LOGIN,CRAM-MD5";

        public MailboxOptions[] Mailboxes { get; set; } = [];
    }
}
