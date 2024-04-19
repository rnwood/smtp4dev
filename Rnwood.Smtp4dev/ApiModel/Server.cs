using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class Server
    {
        public bool SettingsAreEditable { get; internal set; }

        public bool IsRunning { get; set; }

        public string Exception { get; internal set; }

        public int PortNumber { get; set; }

        public string HostName { get; set; }

        public bool AllowRemoteConnections { get; set; }

        public int NumberOfMessagesToKeep { get; set; }

        public int NumberOfSessionsToKeep { get; set; }

        public ServerRelayOptions RelayOptions { get; set; }
        public int? ImapPortNumber { get; set; }

        public bool DisableMessageSanitisation { get; set; }
        public string TlsMode { get; set; }
        public bool AuthenticationRequired { get; set; }
        public string CredentialsValidationExpression { get; set; }
        public bool SecureConnectionRequired { get; set; }
        public string RecipientValidationExpression { get; set; }
        public string MessageValidationExpression { get; set; }
		public bool DisableIPv6 { get; set; }
    }

}
