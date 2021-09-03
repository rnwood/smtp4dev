﻿using MailKit.Security;

namespace Rnwood.Smtp4dev.Server
{
    public class RelayOptions
    {
        public bool IsEnabled => SmtpServer != string.Empty;

        public string SmtpServer { get; set; } = string.Empty;

        public int SmtpPort { get; set; } = 25;

        public SecureSocketOptions TlsMode { get; set; } = SecureSocketOptions.Auto;

        public string[] AutomaticEmails { get; set; } = System.Array.Empty<string>();

        public string SenderAddress { get; set; } = "";

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