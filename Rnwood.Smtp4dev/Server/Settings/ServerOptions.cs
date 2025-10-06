using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Dynamic;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using Esprima.Ast;

namespace Rnwood.Smtp4dev.Server.Settings
{
    public record ServerOptions
    {
        private string database = "database.db";

        public string Urls { get; set; }

        public int Port { get; set; } = 25;
        public bool AllowRemoteConnections { get; set; } = true;
        public string BindAddress { get; set; }

        public string Database { get => database?.Trim('"'); set => database = value; }
        public int NumberOfMessagesToKeep { get; set; } = 100;
        public int NumberOfSessionsToKeep { get; set; } = 100;

        public string BasePath { get; set; } = "/";

        public TlsMode TlsMode { get; set; } = TlsMode.None;

        public TlsMode Pop3TlsMode { get; set; } = TlsMode.None;

        [AllowNull]
        public string SslProtocols { get; set; } = null;

        public string TlsCipherSuites { get; set; } = "";

        public string TlsCertificateStoreThumbprint { get; set; }
        public string TlsCertificate { get; set; }
        public string TlsCertificatePrivateKey { get; set; }

        public string TlsCertificatePassword { get; set; } = "";

        public string HostName { get; set; } = Dns.GetHostName();

        public int? ImapPort { get; set; } = 143;
        public int? Pop3Port { get; set; } = 110;

        public bool RecreateDb { get; set; }

        public bool LockSettings { get; set; } = false;

        public bool DisableMessageSanitisation { get; set; } = false;
        public string CredentialsValidationExpression { get; set; }
        public bool AuthenticationRequired { get; set; } = false;
        public bool SecureConnectionRequired { get; set; } = false;

        public bool SmtpAllowAnyCredentials { get; set; }
        public string RecipientValidationExpression { get; set; }

        public string MessageValidationExpression { get; set; }

        public string CommandValidationExpression { get; set; }
        public bool DisableIPv6 { get; set; } = false;

        public UserOptions[] Users { get; set; } = [];

        public bool WebAuthenticationRequired { get; set; } = false;
        public bool DeliverMessagesToUsersDefaultMailbox { get; set; } = true;

        public string SmtpEnabledAuthTypesWhenNotSecureConnection { get; set; } = "PLAIN,LOGIN,CRAM-MD5";

        public string SmtpEnabledAuthTypesWhenSecureConnection { get; set; } = "PLAIN,LOGIN,CRAM-MD5";

        public MailboxOptions[] Mailboxes { get; set; } = [];

        public string HtmlValidateConfig { get; set; }

        public bool DisableHtmlValidation { get; set; } = false;

        public bool DisableHtmlCompatibilityCheck { get; set; } = false;

        public long? MaxMessageSize { get; set; }
        
        public bool ValidateBareLineFeed { get; set; } = false;

        public bool Pop3SecureConnectionRequired { get; set; } = false;

        public bool DisableWhatsNewNotifications { get; set; } = false;

        public bool DisableUpdateNotifications { get; set; } = false;
    }

}
