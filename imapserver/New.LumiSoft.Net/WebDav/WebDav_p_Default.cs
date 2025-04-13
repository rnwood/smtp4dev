using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.WebDav
{
    /// <summary>
    /// This class represents WebDav default property.
    /// </summary>
    public class WebDav_p_Default : WebDav_p
    {
        private string m_Namespace = "";
        private string m_Name      = null;
        private string m_Value     = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="nameSpace">Property namespace.</param>
        /// <param name="name">Property name.</param>
        /// <param name="value">Property value.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>name</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public WebDav_p_Default(string nameSpace,string name,string value)
        {
            if(name == null){
                throw new ArgumentNullException("name");
            }
            if(name == string.Empty){
                throw new ArgumentException("Argument 'name' value must be specified.");
            }

            m_Namespace = nameSpace;
            m_Name      = name;
            m_Value     = value;
        }


        #region Properties implementation

        /// <summary>
        /// Gets property namespace.
        /// </summary>
        public override string Namespace
        {
            get{ return m_Namespace; }
        }

        /// <summary>
        /// Gets property name.
        /// </summary>
        public override string Name
        {
            get{ return m_Name; }
        }

        /// <summary>
        /// Gets property value.
        /// </summary>
        public override string Value
        {
            get{ return m_Value; }
        }
        
        #endregion
    }
}
