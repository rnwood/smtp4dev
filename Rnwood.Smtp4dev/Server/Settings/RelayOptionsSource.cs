using Ardalis.GuardClauses;
using MailKit.Security;
using System.Text.Json.Serialization;

namespace Rnwood.Smtp4dev.Server.Settings
{
    public class RelayOptionsSource
    {
        public bool IsEnabled => SmtpServer != string.Empty;

        public string SmtpServer { get; set; }

        public int? SmtpPort
        {
            get; set;
        }

        public SecureSocketOptions? TlsMode { get; set; }

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