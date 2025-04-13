using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.MIME;

namespace LumiSoft.Net.Mail
{
    /// <summary>
    /// This class represents "group" address. Defined in RFC 5322 3.4.
    /// </summary>
    public class Mail_t_Group : Mail_t_Address
    {
        private string               m_DisplayName = null;
        private List<Mail_t_Mailbox> m_pList       = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="displayName">Display name. Value null means not specified.</param>
        public Mail_t_Group(string displayName)
        {
            m_DisplayName = displayName;

            m_pList = new List<Mail_t_Mailbox>();
        }


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
            StringBuilder retVal = new StringBuilder();
            if(string.IsNullOrEmpty(m_DisplayName)){
                retVal.Append(":");
            }
            else{
                if(MIME_Encoding_EncodedWord.MustEncode(m_DisplayName)){
                    retVal.Append(wordEncoder.Encode(m_DisplayName) + ":");
                }
                else{
                    retVal.Append(TextUtils.QuoteString(m_DisplayName) + ":");
                }
            }
            for(int i=0;i<m_pList.Count;i++){
                retVal.Append(m_pList[i].ToString(wordEncoder));
                if(i < (m_pList.Count - 1)){
                    retVal.Append(",");
                }
            }
            retVal.Append(";");            

            return retVal.ToString();
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets or sets diplay name. Value null means not specified.
        /// </summary>
        public string DisplayName
        {
            get{ return m_DisplayName; }

            set{ m_DisplayName = value; }
        }

        /// <summary>
        /// Gets groiup address members collection.
        /// </summary>
        public List<Mail_t_Mailbox> Members
        {
            get{ return m_pList; }
        }

        #endregion
    }
}
