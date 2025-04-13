using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP.Relay
{
    /// <summary>
    /// Specifies relay mode.
    /// </summary>
    public enum Relay_Mode
    {
        /// <summary>
        /// Dns is used to resolve email message target.
        /// </summary>
        Dns = 0,

        /// <summary>
        /// All messages sent to the specified host.
        /// </summary>
        SmartHost = 1,
    }
}
