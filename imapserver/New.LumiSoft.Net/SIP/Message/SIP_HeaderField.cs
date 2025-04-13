using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Represents SIP message header field.
    /// </summary>
    public class SIP_HeaderField
    {
        private string m_Name         = "";
        private string m_Value        = "";
        private bool   m_IsMultiValue = false;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">Header field name.</param>
        /// <param name="value">Header field value.</param>
        internal SIP_HeaderField(string name,string value)
        {
            m_Name  = name;
            m_Value = value;
        }


        #region method SetMultiValue

        /// <summary>
        /// Sets property IsMultiValue value.
        /// </summary>
        /// <param name="value">Value to set.</param>
        internal void SetMultiValue(bool value)
        {
            m_IsMultiValue = value;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets header field name.
        /// </summary>
        public string Name
        {
            get{ return m_Name; }
        }

        /// <summary>
        /// Gets or sets header field value.
        /// </summary>
        public virtual string Value
        {
            get{ return m_Value; }

            set{ m_Value = value; }
        }

        /// <summary>
        /// Gets if header field is multi value header field.
        /// </summary>
        public bool IsMultiValue
        {
            get{ return m_IsMultiValue; }
        }

        #endregion

    }
}
