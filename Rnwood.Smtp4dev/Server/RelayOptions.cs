using MailKit.Security;

namespace Rnwood.Smtp4dev.Server
{
    public class RelayOptions
    {
        public bool IsEnabled => SmtpServer != string.Empty;

        public string SmtpServer { get; set; } = string.Empty;

        public int SmtpPort { get; set; } = 25;

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