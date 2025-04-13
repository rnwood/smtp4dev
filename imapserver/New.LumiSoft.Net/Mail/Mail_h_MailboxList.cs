using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.MIME;

namespace LumiSoft.Net.Mail
{
    /// <summary>
    /// This class represent generic <b>mailbox-list</b> header fields. For example: From header.
    /// </summary>
    /// <example>
    /// <code>
    /// RFC 5322.
    ///     header       = "FiledName:" mailbox-list CRLF
    ///     mailbox-list =  (mailbox *("," mailbox)) / obs-mbox-list
    /// </code>
    /// </example>
    public class Mail_h_MailboxList : MIME_h
    {
        private string             m_ParseValue = null;
        private string             m_Name       = null;
        private Mail_t_MailboxList m_pAddresses = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="filedName">Header field name. For example: "To".</param>
        /// <param name="values">Addresses collection.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>filedName</b> or <b>values</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public Mail_h_MailboxList(string filedName,Mail_t_MailboxList values)
        {
            if(filedName == null){
                throw new ArgumentNullException("filedName");
            }
            if(filedName == string.Empty){
                throw new ArgumentException("Argument 'filedName' value must be specified.");
            }
            if(values == null){
                throw new ArgumentNullException("values");
            }

            m_Name       = filedName;
            m_pAddresses = values;
        }


        #region static method Parse

        /// <summary>
        /// Parses header field from the specified value.
        /// </summary>
        /// <param name="value">Header field value. Header field name must be included. For example: 'Content-Type: text/plain'.</param>
        /// <returns>Returns parsed header field.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public static Mail_h_MailboxList Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            string[] name_value = value.Split(new char[]{':'},2);
            if(name_value.Length != 2){
                throw new ParseException("Invalid header field value '" + value + "'.");
            }

            /* RFC 5322 3.4.
                mailbox       =   name-addr / addr-spec
                name-addr     =   [display-name] angle-addr
                angle-addr    =   [CFWS] "<" addr-spec ">" [CFWS] / obs-angle-addr
                display-name  =   phrase
                mailbox-list  =   (mailbox *("," mailbox)) / obs-mbox-list
            */

            Mail_h_MailboxList retVal = new Mail_h_MailboxList(name_value[0],Mail_t_MailboxList.Parse(name_value[1].Trim()));
            retVal.m_ParseValue = value;
            retVal.m_pAddresses.AcceptChanges();

            return retVal;
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns header field as string.
        /// </summary>
        /// <param name="wordEncoder">8-bit words ecnoder. Value null means that words are not encoded.</param>
        /// <param name="parmetersCharset">Charset to use to encode 8-bit characters. Value null means parameters not encoded.</param>
        /// <param name="reEncode">If true always specified encoding is used. If false and header field value not modified, original encoding is kept.</param>
        /// <returns>Returns header field as string.</returns>
        public override string ToString(MIME_Encoding_EncodedWord wordEncoder,Encoding parmetersCharset,bool reEncode)
        {
            if(reEncode || this.IsModified){
                StringBuilder retVal = new StringBuilder();
                retVal.Append(this.Name + ": ");
                for(int i=0;i<m_pAddresses.Count;i++){
                    if(i > 0){
                        retVal.Append("\t");
                    }
 
                    // Don't add ',' for last item.
                    if(i == (m_pAddresses.Count - 1)){
                        retVal.Append(m_pAddresses[i].ToString(wordEncoder) + "\r\n");
                    }
                    else{
                        retVal.Append(m_pAddresses[i].ToString(wordEncoder) + ",\r\n");
                    }
                }
                // No items, we need to add ending CRLF.
                if(m_pAddresses.Count == 0){
                    retVal.Append("\r\n");
                }

                return retVal.ToString();
            }
            else{
                return m_ParseValue;
            }            
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets if this header field is modified since it has loaded.
        /// </summary>
        /// <remarks>All new added header fields has <b>IsModified = true</b>.</remarks>
        /// <exception cref="ObjectDisposedException">Is riased when this class is disposed and this property is accessed.</exception>
        public override bool IsModified
        {
            get{ return m_pAddresses.IsModified; }
        }

        /// <summary>
        /// Gets header field name. For example "From".
        /// </summary>
        public override string Name
        {
            get{ return m_Name; }
        }

        /// <summary>
        /// Gets addresses collection.
        /// </summary>
        public Mail_t_MailboxList Addresses
        {
            get{ return m_pAddresses; }
        }

        #endregion
    }
}
