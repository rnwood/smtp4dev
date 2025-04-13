using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "refer-sub" value. Defined in RFC 4488.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 4488 Syntax:
    ///     Refer-Sub       = refer-sub-value *(SEMI exten)
    ///     refer-sub-value = "true" / "false"
    ///     exten           = generic-param
    /// </code>
    /// </remarks>
    public class SIP_t_ReferSub : SIP_t_ValueWithParams
    {
        private bool m_Value = false;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_ReferSub()
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">Refer-Sub value.</param>
        public SIP_t_ReferSub(string value)
        {
            Parse(value);
        }


        #region method Parse
        
        /// <summary>
        /// Parses "Refer-Sub" from specified value.
        /// </summary>
        /// <param name="value">SIP "Refer-Sub" value.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>value</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public void Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            Parse(new StringReader(value));
        }

        /// <summary>
        /// Parses "Refer-Sub" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                Refer-Sub       = refer-sub-value *(SEMI exten)
                refer-sub-value = "true" / "false"
                exten           = generic-param        
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // refer-sub-value
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Refer-Sub refer-sub-value value is missing !");
            }
            try{
                m_Value = Convert.ToBoolean(word);
            }
            catch{
                throw new SIP_ParseException("Invalid Refer-Sub refer-sub-value value !");
            }

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "contact-param" value.
        /// </summary>
        /// <returns>Returns "contact-param" value.</returns>
        public override string ToStringValue()
        {
            /*
                Refer-Sub       = refer-sub-value *(SEMI exten)
                refer-sub-value = "true" / "false"
                exten           = generic-param        
            */

            StringBuilder retVal = new StringBuilder();
            
            // refer-sub-value
            retVal.Append(m_Value.ToString());

            // Add parameters
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets refer-sub-value value.
        /// </summary>
        public bool Value
        {
            get{ return m_Value; }

            set{ m_Value = value; }
        }

        #endregion

    }
}
