using System.Net;

namespace Rnwood.Smtp4dev.Server
{
    public class ServerOptions
    {
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
    }
}
