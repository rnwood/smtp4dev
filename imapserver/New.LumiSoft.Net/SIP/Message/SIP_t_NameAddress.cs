using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "name-addr" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     name-addr = [ display-name ] LAQUOT addr-spec RAQUOT
    ///     addr-spec = SIP-URI / SIPS-URI / absoluteURI
    /// </code>
    /// </remarks>
    public class SIP_t_NameAddress
    {
        private string      m_DisplayName = "";
        private AbsoluteUri m_pUri        = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_NameAddress()
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">SIP <b>name-addr</b> value.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public SIP_t_NameAddress(string value)
        {
            Parse(value);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="displayName">Display name.</param>
        /// <param name="uri">Uri.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>uri</b> is null reference.</exception>
        public SIP_t_NameAddress(string displayName,AbsoluteUri uri)
        {
            if(uri == null){
                throw new ArgumentNullException("uri");
            }

            this.DisplayName = displayName;
            this.Uri         = uri;
        }


        #region method Parse
        
        /// <summary>
        /// Parses "name-addr" or "addr-spec" from specified value.
        /// </summary>
        /// <param name="value">SIP "name-addr" or "addr-spec" value.</param>
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
        /// Parses "name-addr" or "addr-spec" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public void Parse(StringReader reader)
        {
            /* RFC 3261.
                name-addr =  [ display-name ] LAQUOT addr-spec RAQUOT
                addr-spec =  SIP-URI / SIPS-URI / absoluteURI
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            reader.ReadToFirstChar();
                        
            // LAQUOT addr-spec RAQUOT
            if(reader.StartsWith("<")){
                m_pUri = AbsoluteUri.Parse(reader.ReadParenthesized());
            }
            else{
                // Read while we get "<","," or EOF.
                StringBuilder buf = new StringBuilder();
                while(true){
                    buf.Append(reader.ReadToFirstChar());

                    string word = reader.ReadWord();
                    if(string.IsNullOrEmpty(word)){
                        break;
                    }
                    else{
                        buf.Append(word);
                    }
                }

                reader.ReadToFirstChar();

                // name-addr
                if(reader.StartsWith("<")){
                    m_DisplayName = buf.ToString().Trim();
                    m_pUri        = AbsoluteUri.Parse(reader.ReadParenthesized());
                }
                // addr-spec
                else{
                    m_pUri = AbsoluteUri.Parse(buf.ToString());
                }
            }            
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid name-addr or addr-spec string as needed.
        /// </summary>
        /// <returns>Returns name-addr or addr-spec string.</returns>
        public string ToStringValue()
        {
            /* RFC 3261.
                name-addr =  [ display-name ] LAQUOT addr-spec RAQUOT
                addr-spec =  SIP-URI / SIPS-URI / absoluteURI
            */

            // addr-spec
            if(string.IsNullOrEmpty(m_DisplayName)){
                return "<" + m_pUri.ToString() + ">";
            }
            // name-addr
            else{
                return TextUtils.QuoteString(m_DisplayName) + " <" + m_pUri.ToString() + ">";
            }            
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets display name.
        /// </summary>
        public string DisplayName
        {
            get{ return m_DisplayName; }

            set{
                if(value == null){
                    value = "";
                }

                m_DisplayName = value;
            }
        }

        /// <summary>
        /// Gets or sets URI. This can be SIP-URI / SIPS-URI / absoluteURI.
        /// Examples: sip:ivar@lumisoft.ee,sips:ivar@lumisoft.ee,mailto:ivar@lumisoft.ee, .... .
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null reference passed.</exception>
        public AbsoluteUri Uri
        {
            get{ return m_pUri; }

            set{
                if(value == null){
                    throw new ArgumentNullException("value");
                }

                m_pUri = value;
            }
        }

        /// <summary>
        /// Gets if current URI is sip or sips URI.
        /// </summary>
        public bool IsSipOrSipsUri
        {
            get{ return IsSipUri || IsSecureSipUri; }
        }

        /// <summary>
        /// Gets if current URI is SIP uri.
        /// </summary>
        public bool IsSipUri
        {
            get{ 
                if(m_pUri.Scheme == UriSchemes.sip){
                    return true; 
                }
                return false;
            }
        }

        /// <summary>
        /// Gets if current URI is SIPS uri.
        /// </summary>
        public bool IsSecureSipUri
        {
            get{ 
                if(m_pUri.Scheme == UriSchemes.sips){
                    return true; 
                }
                return false;
            }
        }

        /// <summary>
        /// Gets if current URI is MAILTO uri.
        /// </summary>
        public bool IsMailToUri
        {
            get{ 
                if(m_pUri.Scheme == UriSchemes.mailto){
                    return true; 
                }
                return false;
            }
        }

        #endregion

    }
}
