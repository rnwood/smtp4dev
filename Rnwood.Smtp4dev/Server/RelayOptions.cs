using Ardalis.GuardClauses;
using MailKit.Security;

namespace Rnwood.Smtp4dev.Server
{
    public class RelayOptions
    {
        private int smtpPort = 25;
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

        public string[] AutomaticEmails { get; set; } = System.Array.Empty<string>();

        /// <summary>
        /// Use the Rules engine for Relay decisions or false -> use AutomaticEmails for Relay match
        /// </summary>
        public bool UseRulesEngine { get; set; } = false;

        public string SenderAddress { get; set; } = "";

        public bool UseAuthentication => !string.IsNullOrEmpty(Login);

        public string Login { get; set; } = "";

        public string Password { get; set; } = "";

        [Newtonsoft.Json.JsonIgnore]
        [System.Text.Json.Serialization.JsonIgnore]
        public string AutomaticEmailsString
        {
            get => string.Join(",", AutomaticEmails);
            set => this.AutomaticEmails = value.Split(',');
        }
    }
}