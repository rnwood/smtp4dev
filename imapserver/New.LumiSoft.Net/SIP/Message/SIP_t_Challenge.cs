using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "challenge" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     challenge = ("Digest" LWS digest-cln *(COMMA digest-cln)) / other-challenge
    /// </code>
    /// </remarks>
    public class SIP_t_Challenge : SIP_t_Value
    {
        private string m_Method   = "";
        private string m_AuthData = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">SIP challenge value.</param>
        public SIP_t_Challenge(string value)
        {
            Parse(new StringReader(value));
        }


        #region method Parse

        /// <summary>
        /// Parses "challenge" from specified value.
        /// </summary>
        /// <param name="value">SIP "challenge" value.</param>
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
        /// Parses "challenge" from specified reader.
        /// </summary>
        /// <param name="reader">Reader what contains challenge value.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            // challenge = ("Digest" LWS digest-cln *(COMMA digest-cln)) / other-challenge

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // Get authentication method
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Invalid WWW-Authenticate: value, authentication method is missing !");
            }
            m_Method = word;

            // Get authentication data
            word = reader.ReadToEnd();
            if(word == null){
                throw new SIP_ParseException("Invalid WWW-Authenticate: value, authentication parameters are missing !");
            }
            m_AuthData = word.Trim();
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "challenge" value.
        /// </summary>
        /// <returns>Returns "challenge" value.</returns>
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
