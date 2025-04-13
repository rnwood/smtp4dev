using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// This class specifies SIP UA registration state.
    /// </summary>
    public enum SIP_UA_RegistrationState
    {
        /// <summary>
        /// Registration is currently registering.
        /// </summary>
        Registering,

        /// <summary>
        /// Registration is active.
        /// </summary>
        Registered,

        /// <summary>
        /// Registration is not registered to registrar server.
        /// </summary>
        Unregistered,

        /// <summary>
        /// Registering has failed.
        /// </summary>
        Error,

        /// <summary>
        /// Registration has disposed.
        /// </summary>
        Disposed
    }
}
