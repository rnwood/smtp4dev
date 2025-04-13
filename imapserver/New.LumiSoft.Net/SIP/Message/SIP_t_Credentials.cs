using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "credentials" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     credentials    = ("Digest" LWS digest-response) / other-response
    ///     other-response = auth-scheme LWS auth-param *(COMMA auth-param)
    ///     auth-scheme    = token
    /// </code>
    /// </remarks>
    public class SIP_t_Credentials : SIP_t_Value
    {
        private string m_Method   = "";
        private string m_AuthData = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">SIP credentials value.</param>
        public SIP_t_Credentials(string value)
        {
            Parse(new StringReader(value));
        }


        #region method Parse

        /// <summary>
        /// Parses "credentials" from specified value.
        /// </summary>
        /// <param name="value">SIP "credentials" value.</param>
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
        /// Parses "credentials" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                credentials = ("Digest" LWS digest-response) / other-response
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // Get authentication method
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Invalid 'credentials' value, authentication method is missing !");
            }
            m_Method = word;

            // Get authentication data
            word = reader.ReadToEnd();
            if(word == null){
                throw new SIP_ParseException("Invalid 'credentials' value, authentication parameters are missing !");
            }
            m_AuthData = word.Trim();
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "credentials" value.
        /// </summary>
        /// <returns>Returns "credentials" value.</returns>
        public override string ToStringValue()
        {
            return m_Method + " " + m_AuthData;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets authentication method. Normally this value is always 'Digest'.
        /// </summary>
        public string Method
        {
            get{ return m_Method; }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property Method value cant be null or mepty !");
                }

                m_Method = value;
            }
        }

        /// <summary>
        /// Gets or sets authentication data. That value depends on authentication type.
        /// </summary>
        public string AuthData
        {
            get{ return m_AuthData; }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property AuthData value cant be null or mepty !");
                }

                m_AuthData = value;
            }
        }

        #endregion
    }
}
