using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.MIME;

namespace LumiSoft.Net.Mail
{    
    /// <summary>
    /// This class represents "mailbox" address. Defined in RFC 5322 3.4.
    /// </summary>
    /// <example>
    /// <code>
    /// RFC 5322 3.4.
    ///     mailbox    = name-addr / addr-spec
    ///     name-addr  = [display-name] angle-addr
    ///     angle-addr = [CFWS] "&lt;" addr-spec "&gt;" [CFWS]
    ///     addr-spec  = local-part "@" domain
    /// </code>
    /// </example>
    public class Mail_t_Mailbox : Mail_t_Address
    {
        private string m_DisplayName = null;
        private string m_Address     = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="displayName">Display name. Value null means not specified.</param>
        /// <param name="address">Email address.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>address</b> is null reference.</exception>
        public Mail_t_Mailbox(string displayName,string address)
        {
            if(address == null){
                throw new ArgumentNullException("address");
            }

            m_DisplayName = displayName;
            m_Address     = address;
        }

        #region static method Parse

        /// <summary>
        /// Parses <b>mailbox</b> from specified string value.
        /// </summary>
        /// <param name="value">The <b>mailbox</b> string value.</param>
        /// <returns>Returns parse mailbox.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when <b>value</b> is not valid <b>mailbox</b> value.</exception>
        public static Mail_t_Mailbox Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            MIME_Reader        r      = new MIME_Reader(value);
            Mail_t_MailboxList retVal = new Mail_t_MailboxList();
            while(true){
                string word = r.QuotedReadToDelimiter(new char[]{',','<'});
                // We processed all data.
                if(string.IsNullOrEmpty(word) && r.Available == 0){
                    throw new ParseException("Not valid 'mailbox' value '" + value + "'.");
                }
                // name-addr
                else if(r.Peek(true) == '<'){
                    return new Mail_t_Mailbox(word != null ? MIME_Encoding_EncodedWord.DecodeS(TextUtils.UnQuoteString(word.Trim())) : null,r.ReadParenthesized());
                }
                // addr-spec
                else{
                    return new Mail_t_Mailbox(null,word);
                }
            }

            throw new ParseException("Not valid 'mailbox' value '" + value + "'.");
        }

        #endregion


        #region method override ToString

        /// <summary>
        /// Returns mailbox as string.
        /// </summary>
        /// <returns>Returns mailbox as string.</returns>
        public override string ToString()
        {
            return ToString(null);
        }

        /// <summary>
        /// Returns address as string value.
        /// </summary>
        /// <param name="wordEncoder">8-bit words ecnoder. Value null means that words are not encoded.</param>
        /// <returns>Returns address as string value.</returns>
        public override string ToString(MIME_Encoding_EncodedWord wordEncoder)
        {
            if(string.IsNullOrEmpty(m_DisplayName)){
                return m_Address;
            }
            else{
                if(wordEncoder != null && MIME_Encoding_EncodedWord.MustEncode(m_DisplayName)){
                    return wordEncoder.Encode(m_DisplayName) + " " + "<" + m_Address + ">";
                }
                else{
                    return TextUtils.QuoteString(m_DisplayName) + " " + "<" + m_Address + ">";
                }
            }
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets display name. Value null means not specified.
        /// </summary>
        public string DisplayName
        {
            get{ return m_DisplayName; }
        }

        /// <summary>
        /// Gets address.
        /// </summary>
        public string Address
        {
            get{ return m_Address; }
        }

        /// <summary>
        /// Gets local-part of address.
        /// </summary>
        public string LocalPart
        {
            get{ 
                string[] localpart_domain = m_Address.Split('@');

                return localpart_domain[0]; 
            }
        }

        /// <summary>
        /// Gets domain part of address.
        /// </summary>
        public string Domain
        {
            get{ 
                string[] localpart_domain = m_Address.Split('@');

                if(localpart_domain.Length == 2){
                    return localpart_domain[1]; 
                }
                else{
                    return "";
                }
            }
        }

        #endregion
    }
}
