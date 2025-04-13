using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.WebDav
{
    /// <summary>
    /// This class is base class for any WebDav property.
    /// </summary>
    public abstract class WebDav_p
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public WebDav_p()
        {
        }


        #region Properties implementation

        /// <summary>
        /// Gets property namespace.
        /// </summary>
        public abstract string Namespace
        {
            get;
        }

        /// <summary>
        /// Gets property name.
        /// </summary>
        public abstract string Name
        {
            get;
        }

        /// <summary>
        /// Gets property value.
        /// </summary>
        public abstract string Value
        {
            get;
        }

        #endregion
    }
}
