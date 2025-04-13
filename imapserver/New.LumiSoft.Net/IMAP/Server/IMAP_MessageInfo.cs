using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class represents IMAP message info.
    /// </summary>
    public class IMAP_MessageInfo
    {
        private string   m_ID     = null;
        private long     m_UID    = 0;
        private string[] m_pFlags = null;
        private int      m_Size   = 0;
        private DateTime m_InternalDate;
        private int      m_SeqNo  = 1;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="id">Message ID.</param>
        /// <param name="uid">Message IMAP UID value.</param>
        /// <param name="flags">Message flags.</param>
        /// <param name="size">Message size in bytes.</param>
        /// <param name="internalDate">Message IMAP internal date.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>id</b> or <b>flags</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_MessageInfo(string id,long uid,string[] flags,int size,DateTime internalDate)
        {
            if(id == null){
                throw new ArgumentNullException("id");
            }
            if(id == string.Empty){
                throw new ArgumentException("Argument 'id' value must be specified.","id");
            }
            if(uid < 1){
                throw new ArgumentException("Argument 'uid' value must be >= 1.","uid");
            }
            if(flags == null){
                throw new ArgumentNullException("flags");
            }

            m_ID           = id;
            m_UID          = uid;
            m_pFlags       = flags;
            m_Size         = size;
            m_InternalDate = internalDate;
        }


        #region method ContainsFlag

        /// <summary>
        /// Gets if this message info contains specified message flag.
        /// </summary>
        /// <param name="flag">Message flag.</param>
        /// <returns>Returns true if message info contains specified message flag.</returns>
        public bool ContainsFlag(string flag)
        {
            if(flag == null){
                throw new ArgumentNullException("flag");
            }

            foreach(string f in m_pFlags){
                if(string.Equals(f,flag,StringComparison.InvariantCultureIgnoreCase)){
                    return true;
                }
            }

            return false;
        }

        #endregion


        #region method FlagsToImapString

        /// <summary>
        /// Flags to IMAP flags string.
        /// </summary>
        /// <returns>Returns IMAP flags string.</returns>
        internal string FlagsToImapString()
        {
            StringBuilder retVal = new StringBuilder();
            retVal.Append("(");
            for(int i=0;i<m_pFlags.Length;i++){
                if(i > 0){
                    retVal.Append(" ");
                }

                retVal.Append("\\" + m_pFlags[i]);
            }
            retVal.Append(")");

            return retVal.ToString();
        }

        #endregion

        #region method UpdateFlags

        /// <summary>
        /// Updates IMAP message flags.
        /// </summary>
        /// <param name="setType">Flags set type.</param>
        /// <param name="flags">IMAP message flags.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>flags</b> is null reference.</exception>
        internal void UpdateFlags(IMAP_Flags_SetType setType,string[] flags)
        {
            if(flags == null){
                throw new ArgumentNullException("flags");
            }

            if(setType == IMAP_Flags_SetType.Add){
                m_pFlags = IMAP_Utils.MessageFlagsAdd(m_pFlags,flags);
            }
            else if(setType == IMAP_Flags_SetType.Remove){
                m_pFlags = IMAP_Utils.MessageFlagsRemove(m_pFlags,flags);
            }
            else{
                m_pFlags = flags;
            }
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets message ID value.
        /// </summary>
        public string ID
        {
            get{ return m_ID; }
        }

        /// <summary>
        /// Gets message IMAP UID value.
        /// </summary>
        public long UID
        {
            get{ return m_UID; }
        }

        /// <summary>
        /// Gets message flags.
        /// </summary>
        public string[] Flags
        {
            get{ return m_pFlags; }
        }

        /// <summary>
        /// Gets message size in bytes.
        /// </summary>
        public int Size
        {
            get{ return m_Size; }
        }

        /// <summary>
        /// Gets message IMAP internal date.
        /// </summary>
        public DateTime InternalDate
        {
            get{ return m_InternalDate; }
        }


        /// <summary>
        /// Gets or sets message one-based sequnece number.
        /// </summary>
        internal int SeqNo
        {
            get{ return m_SeqNo; }

            set{ m_SeqNo = value; }
        }
        
        #endregion
    }
}
