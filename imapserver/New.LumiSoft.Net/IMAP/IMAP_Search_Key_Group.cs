using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using LumiSoft.Net.IMAP.Client;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents parenthesized list of IMAP SEARCH keys. Defined in RFC 3501 6.4.4.
    /// </summary>
    public class IMAP_Search_Key_Group : IMAP_Search_Key
    {
        private List<IMAP_Search_Key> m_pKeys = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public IMAP_Search_Key_Group()
        {
            m_pKeys = new List<IMAP_Search_Key>();
        }


        #region static method Parse

        /// <summary>
        /// Returns parsed IMAP SEARCH <b>AND</b> key group.
        /// </summary>
        /// <param name="r">String reader.</param>
        /// <returns>Returns parsed IMAP SEARCH <b>AND</b> key group.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>r</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when parsing fails.</exception>
        public static IMAP_Search_Key_Group Parse(StringReader r)
        {
            if(r == null){
                throw new ArgumentNullException("r");
            }

            // Remove parenthesis, if any.
            if(r.StartsWith("(")){
                r = new StringReader(r.ReadParenthesized());
            }            

            IMAP_Search_Key_Group retVal = new IMAP_Search_Key_Group();

            r.ReadToFirstChar();
            while(r.Available > 0){
                retVal.m_pKeys.Add(IMAP_Search_Key.ParseKey(r));
            }

            return retVal;
        }

        #endregion


        #region method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            StringBuilder retVal = new StringBuilder();
            retVal.Append("(");
            for(int i=0;i<m_pKeys.Count;i++){
                if(i > 0){
                    retVal.Append(" ");
                }
                retVal.Append(m_pKeys[i].ToString());
            }
            retVal.Append(")");

            return retVal.ToString();
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

            list.Add(new IMAP_Client_CmdPart(IMAP_Client_CmdPart_Type.Constant,"("));
            for(int i=0;i<m_pKeys.Count;i++){
                if(i > 0){
                    list.Add(new IMAP_Client_CmdPart(IMAP_Client_CmdPart_Type.Constant," "));
                }
                m_pKeys[i].ToCmdParts(list);
            }
            list.Add(new IMAP_Client_CmdPart(IMAP_Client_CmdPart_Type.Constant,")"));
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets AND-ded keys collection.
        /// </summary>
        public List<IMAP_Search_Key> Keys
        {
            get{ return m_pKeys; }
        }

        #endregion
    }
}
