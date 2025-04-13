using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "rc-value" value. Defined in RFC 3841.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3841 Syntax:
    ///     rc-value  =  "*" *(SEMI rc-params)
    ///     rc-params =  feature-param / generic-param
    /// </code>
    /// </remarks>
    public class SIP_t_RCValue : SIP_t_ValueWithParams
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_RCValue()
        {
        }


        #region method Parse
        
        /// <summary>
        /// Parses "rc-value" from specified value.
        /// </summary>
        /// <param name="value">SIP "rc-value" value.</param>
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
        /// Parses "rc-value" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                rc-value  =  "*" *(SEMI rc-params)
                rc-params =  feature-param / generic-param
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Invalid 'rc-value', '*' is missing !");
            }

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "rc-value" value.
        /// </summary>
        /// <returns>Returns "rc-value" value.</returns>
        public override string ToStringValue()
        {
            /*
                rc-value  =  "*" *(SEMI rc-params)
                rc-params =  feature-param / generic-param
            */

            StringBuilder retVal = new StringBuilder();
            
            // *
            retVal.Append("*");

            // Add parameters
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion

    }
}
