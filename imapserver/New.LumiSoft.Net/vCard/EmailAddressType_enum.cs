using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Mime.vCard
{
    /// <summary>
    /// vCal email address type. Note this values may be flagged !
    /// </summary>
    public enum EmailAddressType_enum
    {
        /// <summary>
        /// Email address type not specified.
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// Preferred email address.
        /// </summary>
        Preferred = 1,

        /// <summary>
        /// Internet addressing type.
        /// </summary>
        Internet = 2,

        /// <summary>
        /// X.400 addressing type.
        /// </summary>
        X400 = 4,
    }
}
