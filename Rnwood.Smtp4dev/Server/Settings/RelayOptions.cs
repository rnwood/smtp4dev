using Ardalis.GuardClauses;
using MailKit.Security;
using System.Text.Json.Serialization;

namespace Rnwood.Smtp4dev.Server.Settings
{
    public record RelayOptions
    {
        private int smtpPort = 25;

        [JsonIgnore]
        public bool IsEnabled => SmtpServer != string.Empty;

        public string SmtpServer { get; set; } = string.Empty;

        public int SmtpPort
        {
            get => smtpPort;
            set
            {
                Guard.Against.OutOfRange(value, nameof(SmtpPort), 1, 65535);
                smtpPort = value;
            }
        }

        public SecureSocketOptions TlsMode { get; set; } = SecureSocketOptions.Auto;
      
        public string[] SslCipherSuitesPolicy { get; set; } = System.Array.Empty<string>();

        public bool CheckCertificateRevocation { get; set; } = true;
        
        public string[] AutomaticEmails { get; set; } = System.Array.Empty<string>();

        public string AutomaticRelayExpression { get; set; }

        public string SenderAddress { get; set; } = "";

        public string Login { get; set; } = "";

        public string Password { get; set; } = "";

        [JsonIgnore]
        public string AutomaticEmailsString
        {
            get => string.Join(",", AutomaticEmails);
            set => AutomaticEmails = value.Split(',');
        }
    }
}