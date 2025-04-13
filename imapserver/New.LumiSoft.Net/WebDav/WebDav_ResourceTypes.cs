using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.WebDav
{
    /// <summary>
    /// This class holds well-known WebDav resource types.
    /// </summary>
    public class WebDav_ResourceTypes
    {
        /// <summary>
        /// This class represents 'DAV:collection' resurce type.
        /// </summary>
        public static readonly string collection = "DAV:collection";

        /// <summary>
        /// This class represents 'DAV:version-history' resurce type.
        /// </summary>
        public static readonly string version_history = "DAV:version-history";

        /// <summary>
        /// This class represents 'DAV:activity' resurce type.
        /// </summary>
        public static readonly string activity = "DAV:activity";

        /// <summary>
        /// This class represents 'DAV:baseline' resurce type.
        /// </summary>
        public static readonly string baseline = "DAV:baseline";
    }
}
