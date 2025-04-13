using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Proxy
{
    /// <summary>
    /// Specifies SIP proxy mode.
    /// <example>
    /// All flags may be combined, except Stateless,Statefull,B2BUA.
    /// For example: (Stateless | Statefull) not allowed, but (Registrar | Presence | Statefull) is allowed.
    /// </example>
    /// </summary>
    [Flags]
    public enum SIP_ProxyMode
    {
        /// <summary>
        /// Proxy implements SIP registrar.
        /// </summary>
        Registrar = 1,

        /// <summary>
        /// Proxy implements SIP presence server.
        /// </summary>
        Presence = 2,

        /// <summary>
        /// Proxy runs in stateless mode.
        /// </summary>
        Stateless = 4,

        /// <summary>
        /// Proxy runs in statefull mode.
        /// </summary>
        Statefull = 8,
        
        /// <summary>
        /// Proxy runs in B2BUA(back to back user agent) mode.
        /// </summary>
        B2BUA = 16,
    }
}
