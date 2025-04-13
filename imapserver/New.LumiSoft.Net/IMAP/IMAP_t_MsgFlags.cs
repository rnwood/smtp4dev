using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represent IMAP message flags. Defined in RFC 3501 2.3.2.
    /// </summary>
    public class IMAP_t_MsgFlags
    {
        #region System flags definition

        /// <summary>
        /// Message flag <b>Seen</b>: Message has been read.
        /// </summary>
        public static readonly string Seen = "\\Seen";

        /// <summary>
        /// Message flag <b>Answered</b>: Message has been answered.
        /// </summary>
        public static readonly string Answered = "\\Answered";

        /// <summary>
        /// Message flag <b>Flagged</b>: Message is "flagged" for urgent/special attention.
        /// </summary>
        public static readonly string Flagged = "\\Flagged";

        /// <summary>
        /// Message flag <b>Deleted</b>: Message is "deleted" for removal by later EXPUNGE.
        /// </summary>
        public static readonly string Deleted = "\\Deleted";
            
        /// <summary>
        /// Message flag <b>Draft</b>: Message has not completed composition (marked as a draft).
        /// </summary>
        public static readonly string Draft = "\\Draft";

        /// <summary>
        /// Message flag <b>Recent</b>: Message is "recently" arrived in this mailbox.
        /// </summary>
        public static readonly string Recent = "\\Recent";

        #endregion

        private KeyValueCollection<string,string> m_pFlags = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="flags">Message flags.</param>
        public IMAP_t_MsgFlags(params string[] flags)
        {
            m_pFlags = new KeyValueCollection<string,string>();

            if(flags != null){
                foreach(string flag in flags){
                    if(!string.IsNullOrEmpty(flag)){
                        m_pFlags.Add(flag.ToLower(),flag);
                    }
                }
            }
        }


        #region static method Parse

        /// <summary>
        /// Parses message flags from flags-string.
        /// </summary>
        /// <param name="value">Message flags sttring.</param>
        /// <returns>Returns parsed flags.</returns>
        /// <exception cref="ArgumentNullException">Is riased when <b>value</b> is null reference.</exception>
        public static IMAP_t_MsgFlags Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            value = value.Trim();

            if(value.StartsWith("(") && value.EndsWith(")")){
                value = value.Substring(1,value.Length - 2);
            }

            string[] flags     = new string[0];
            if(!string.IsNullOrEmpty(value)){
                flags = value.Split(' ');
            }

            return new IMAP_t_MsgFlags(flags);
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as flags string.
        /// </summary>
        /// <returns>Returns this as flags string.</returns>
        public override string ToString()
        {
            StringBuilder retVal = new StringBuilder();

            string[] flags = ToArray();
            for(int i=0;i<flags.Length;i++){
                if(i > 0){
                    retVal.Append(" ");
                }
                retVal.Append(flags[i]);
            }

            return retVal.ToString();
        }

        #endregion

        #region method Contains

        /// <summary>
        /// Gets if flags list contains the specified flag.
        /// </summary>
        /// <param name="flag">Message flag.</param>
        /// <returns>Returns true if flags list contains the specified flag.</returns>
        public bool Contains(string flag)
        {
            if(flag == null){
                throw new ArgumentNullException("flag");
            }

            return m_pFlags.ContainsKey(flag.ToLower());
        }

        #endregion

        #region method ToArray

        /// <summary>
        /// Copies message flags to string array.
        /// </summary>
        /// <returns>Returns message flags as string array.</returns>
        public string[] ToArray()
        {
            return m_pFlags.ToArray();
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets number of flags in the collection.
        /// </summary>
        public int Count
        {
            get{ return m_pFlags.Count; }
        }

        #endregion
    }
}
