using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.MIME;

namespace LumiSoft.Net.Mail
{
    /// <summary>
    /// Represents "Return-Path:" header. Defined in RFC 5322 3.6.7.
    /// </summary>
    /// <example>
    /// <code>
    /// RFC 5322 3.6.7.
    ///     return     = "Return-Path:" path CRLF
    ///     path       = angle-addr / ([CFWS] "&lt;" [CFWS] "&gt;" [CFWS])
    ///     angle-addr = [CFWS] "&lt;" addr-spec "&gt;" [CFWS]
    /// </code>
    /// </example>
    public class Mail_h_ReturnPath : MIME_h
    {
        private bool   m_IsModified = false;
        private string m_Address    = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="address">Address. Value null means null-path.</param>
        public Mail_h_ReturnPath(string address)
        {
            m_Address = address;
        }


        #region static method Parse

        /// <summary>
        /// Parses header field from the specified value.
        /// </summary>
        /// <param name="value">Header field value. Header field name must be included. For example: 'Return-Path: &lt;jhon.doe@domain.com&gt;'.</param>
        /// <returns>Returns parsed header field.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public static Mail_h_ReturnPath Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            string[] name_value = value.Split(new char[]{':'},2);
            if(name_value.Length != 2){
                throw new ParseException("Invalid header field value '" + value + "'.");
            }

            Mail_h_ReturnPath retVal = new Mail_h_ReturnPath(null);

            MIME_Reader r = new MIME_Reader(name_value[1].Trim());
            r.ToFirstChar();
            // Return-Path missing <>, some server won't be honor RFC.
            if(!r.StartsWith("<")){
                retVal.m_Address = r.ToEnd();
            }
            else{
                retVal.m_Address = r.ReadParenthesized();
            }

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
            if(string.IsNullOrEmpty(m_Address)){
                return "Return-Path: <>\r\n";
            }
            else{
                return "Return-Path: <" + m_Address + ">\r\n";
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
            get{ return m_IsModified; }
        }

        /// <summary>
        /// Gets header field name. For example "Sender".
        /// </summary>
        public override string Name
        {
            get{ return "Return-Path"; }
        }

        /// <summary>
        /// Gets mailbox address. Value null means null-path.
        /// </summary>
        public string Address
        {
            get{ return m_Address; }
        }

        #endregion
    }
}
