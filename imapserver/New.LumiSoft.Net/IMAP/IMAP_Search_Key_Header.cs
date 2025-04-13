using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using LumiSoft.Net.IMAP.Client;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP SEARCH <b>HEADER (field-name) (string)</b> key. Defined in RFC 3501 6.4.4.
    /// </summary>
    /// <remarks>Messages that have a header with the specified field-name (as
    /// defined in [RFC-2822]) and that contains the specified string
    /// in the text of the header (what comes after the colon).  If the
    /// string to search is zero-length, this matches all messages that
    /// have a header line with the specified field-name regardless of
    /// the contents.
    /// </remarks>
    public class IMAP_Search_Key_Header : IMAP_Search_Key
    {
        private string m_FieldName = "";
        private string m_Value     = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="fieldName">Header field name. For example: 'Subject'.</param>
        /// <param name="value">String value.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>fieldName</b> or <b>value</b> is null reference.</exception>
        public IMAP_Search_Key_Header(string fieldName,string value)
        {
            if(fieldName == null){
                throw new ArgumentNullException("fieldName");
            }
            if(value == null){
                throw new ArgumentNullException("value");
            }

            m_FieldName = fieldName;
            m_Value     = value;
        }


        #region static method Parse

        /// <summary>
        /// Returns parsed IMAP SEARCH <b>HEADER (field-name) (string)</b> key.
        /// </summary>
        /// <param name="r">String reader.</param>
        /// <returns>Returns parsed IMAP SEARCH <b>HEADER (field-name) (string)</b> key.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>r</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when parsing fails.</exception>
        internal static IMAP_Search_Key_Header Parse(StringReader r)
        {
            if(r == null){
                throw new ArgumentNullException("r");
            }

            string word = r.ReadWord();
            if(!string.Equals(word,"HEADER",StringComparison.InvariantCultureIgnoreCase)){
                throw new ParseException("Parse error: Not a SEARCH 'HEADER' key.");
            }
            string fieldName = IMAP_Utils.ReadString(r);
            if(fieldName == null){
                throw new ParseException("Parse error: Invalid 'HEADER' field-name value.");
            }
            string value = IMAP_Utils.ReadString(r);
            if(value == null){
                throw new ParseException("Parse error: Invalid 'HEADER' string value.");
            }

            return new IMAP_Search_Key_Header(fieldName,value);
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            return "HEADER " + TextUtils.QuoteString(m_FieldName) + " " + TextUtils.QuoteString(m_Value);
        }

        #endregion


        #region internal override method ToCmdParts

        /// <summary>
        /// Stores IMAP search-key command parts to the specified array.
        /// </summary>
        /// <param name="list">Array where to store command parts.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>list</b> is null reference.</exception>
        internal override void ToCmdParts(List<IMAP_Client_CmdPart> list)
        {
            if(list == null){
                throw new ArgumentNullException("list");
            }

            list.Add(new IMAP_Client_CmdPart(IMAP_Client_CmdPart_Type.Constant,"HEADER "));
            list.Add(new IMAP_Client_CmdPart(IMAP_Client_CmdPart_Type.String,m_FieldName));
            list.Add(new IMAP_Client_CmdPart(IMAP_Client_CmdPart_Type.Constant," "));
            list.Add(new IMAP_Client_CmdPart(IMAP_Client_CmdPart_Type.String,m_Value));
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets header field name.
        /// </summary>
        public string FieldName
        {
            get{ return m_FieldName; }
        }

        /// <summary>
        /// Gets filter value.
        /// </summary>
        public string Value
        {
            get{ return m_Value; }
        }

        #endregion
    }
}
