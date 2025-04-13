using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "content-coding" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     content-coding = token
    /// </code>
    /// </remarks>
    public class SIP_t_ContentCoding : SIP_t_Value
    {
        private string m_Encoding = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_ContentCoding()
        {
        }


        #region method Parse

        /// <summary>
        /// Parses "content-coding" from specified value.
        /// </summary>
        /// <param name="value">SIP "content-coding" value.</param>
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
        /// Parses "content-coding" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                content-coding = token
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // Get Method
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Invalid 'content-coding' value, value is missing !");
            }
            m_Encoding = word;
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "content-coding" value.
        /// </summary>
        /// <returns>Returns "content-coding" value.</returns>
        public override string ToStringValue()
        {
            return m_Encoding;
        }

        #endregion


        #region Properties Impelementation

        /// <summary>
        /// Gets or sets content encoding.
        /// </summary>
        public string Encoding
        {
            get{ return m_Encoding; }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property Encoding value may not be null or empty !");
                }
                if(!TextUtils.IsToken(value)){
                    throw new ArgumentException("Encoding value may be 'token' only !");
                }

                m_Encoding = value;
            }
        }

        #endregion

    }
}
