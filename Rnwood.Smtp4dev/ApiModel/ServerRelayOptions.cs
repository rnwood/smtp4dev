using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.ApiModel
{
    public class ServerRelayOptions
    {
        public string SmtpServer { get; set; }

        public int SmtpPort { get; set; }

        public string[] AutomaticEmails { get; set; }

        public string SenderAddress { get; set; }

        public string Login { get; set; }

        public string Password { get; set; }
    }
}
