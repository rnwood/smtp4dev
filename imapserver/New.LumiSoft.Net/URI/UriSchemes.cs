using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net
{
    /// <summary>
    /// This class represents well known URI schemes.
    /// </summary>
    public class UriSchemes
    {   
        /// <summary>
        /// HTTP Extensions for Distributed Authoring (WebDAV).
        /// </summary>
        public const string dav = "dav";

        /// <summary>
        /// Addressing files on local or network file systems.
        /// </summary>
        public const string file = "file";

        /// <summary>
        /// FTP resources.
        /// </summary>
        public const string ftp = "ftp";

        /// <summary>
        /// HTTP resources.
        /// </summary>
        public const string http = "http";

        /// <summary>
        /// HTTP connections secured using SSL/TLS.
        /// </summary>
        public const string https = "https";

        /// <summary>
        /// SMTP e-mail addresses and default content.
        /// </summary>
        public const string mailto = "mailto";

        /// <summary>
        /// Session Initiation Protocol (SIP).
        /// </summary>
        public const string sip = "sip";
                
        /// <summary>
        /// Session Initiation Protocol (SIP) using TLS.
        /// </summary>
        public const string sips = "sips";

        /// <summary>
        /// Telephone numbers.
        /// </summary>
        public const string tel = "tel";
    }
}
