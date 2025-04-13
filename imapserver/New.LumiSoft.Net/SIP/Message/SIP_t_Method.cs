using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "Method" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     Method           = INVITEm / ACKm / OPTIONSm / BYEm / CANCELm / REGISTERm / extension-method
    ///     extension-method = token
    /// </code>
    /// </remarks>
    public class SIP_t_Method : SIP_t_Value
    {
        private string m_Method = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_Method()
        {
        }

        
        #region method Parse

        /// <summary>
        /// Parses "Method" from specified value.
        /// </summary>
        /// <param name="value">SIP "Method" value.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public void Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("reader");
            }

            Parse(new StringReader(value));
        }

        /// <summary>
        /// Parses "Method" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /* Allow            = "Allow" HCOLON [Method *(COMMA Method)]
               Method           = INVITEm / ACKm / OPTIONSm / BYEm / CANCELm / REGISTERm / extension-method
               extension-method = token
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // Get Method
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Invalid 'Method' value, value is missing !");
            }
            m_Method = word;
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "Method" value.
        /// </summary>
        /// <returns></returns>
        public override string ToStringValue()
        {
            return m_Method;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets SIP method what is allowed.
        /// </summary>
        public string Method
        {
            get{ return m_Method; }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property Method value can't be null or empty !");
                }
                if(TextUtils.IsToken(value)){
                    throw new ArgumentException("Property Method value must be 'token' !");
                }

                m_Method = value;
            }
        }

        #endregion

    }
}
