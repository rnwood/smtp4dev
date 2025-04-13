using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.FTP
{
    /// <summary>
    /// Specifies FTP data connection transfer mode.
    /// </summary>
    public enum FTP_TransferMode
    {
        /// <summary>
        /// Active transfer mode - FTP server opens data connection FTP client.
        /// </summary>
        Active,

        /// <summary>
        /// Passive transfer mode - FTP client opens data connection FTP server.
        /// </summary>
        Passive
    }
}
