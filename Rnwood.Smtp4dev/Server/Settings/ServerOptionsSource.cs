using System.Net;
using Esprima.Ast;

namespace Rnwood.Smtp4dev.Server.Settings
{
    public class ServerOptionsSource
    {
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
        public bool? SecureConnectionRequired { get; set; }
        public string RecipientValidationExpression { get; set; }

        public string MessageValidationExpression { get; set; }
        public bool? DisableIPv6 { get; set; }

        public User[] Users { get; set; }
    }
}
