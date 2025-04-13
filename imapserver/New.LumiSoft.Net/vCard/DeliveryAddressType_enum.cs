using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Mime.vCard
{
    /// <summary>
    /// vCal delivery address type. Note this values may be flagged !
    /// </summary>
    public enum DeliveryAddressType_enum
    {
        /// <summary>
        /// Delivery address type not specified.
        /// </summary>
        NotSpecified = 0,

        /// <summary>
        /// Preferred delivery address.
        /// </summary>
        Preferred = 1,

        /// <summary>
        /// Domestic delivery address.
        /// </summary>
        Domestic = 2,

        /// <summary>
        /// International delivery address.
        /// </summary>
        Ineternational = 4,

        /// <summary>
        /// Postal delivery address.
        /// </summary>
        Postal = 8,

        /// <summary>
        /// Parcel delivery address.
        /// </summary>
        Parcel = 16,
                
        /// <summary>
        /// Delivery address for a residence.
        /// </summary>
        Home = 32,

        /// <summary>
        /// Address for a place of work.
        /// </summary>
        Work = 64,
    }
}
