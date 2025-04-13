using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class provides data for <b cref="IMAP_Session.GetMessagesInfo">IMAP_Session.GetMessagesInfo</b> event.
    /// </summary>
    public class IMAP_e_MessagesInfo : EventArgs
    {
        private string                 m_Folder    = null;
        private List<IMAP_MessageInfo> m_pMessages = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        internal IMAP_e_MessagesInfo(string folder)
        {
            if(folder == null){
                throw new ArgumentNullException("folder");
            }

            m_Folder = folder;

            m_pMessages = new List<IMAP_MessageInfo>();
        }


        #region Properties implementation
                
        /// <summary>
        /// Gets folder name with optional path.
        /// </summary>
        public string Folder
        {
            get{ return m_Folder; }
        }

        /// <summary>
        /// Gets messages info collection.
        /// </summary>
        public List<IMAP_MessageInfo> MessagesInfo
        {
            get{ return m_pMessages; }
        }


        /// <summary>
        /// Gets messages count.
        /// </summary>
        internal int Exists
        {
            get{ return m_pMessages.Count; }
        }

        /// <summary>
        /// Gets messages count with recent flag set.
        /// </summary>
        internal int Recent
        {
            get{ 
                int count = 0;
                foreach(IMAP_MessageInfo m in m_pMessages){
                    foreach(string flag in m.Flags){
                        if(string.Equals(flag,"Recent",StringComparison.InvariantCultureIgnoreCase)){
                            count++;
                            break;
                        }
                    }
                }

                return count; 
            }
        }

        /// <summary>
        /// Get messages first unseen message 1-based sequnece number. Returns -1 if no umseen messages.
        /// </summary>
        internal int FirstUnseen
        {
            get{
                for(int i=0;i<m_pMessages.Count;i++){
                    if(!m_pMessages[i].ContainsFlag("Seen")){
                        return i + 1;
                    }
                }

                return -1; 
            }
        }

        /// <summary>
        /// Gets messages count with seen flag not set.
        /// </summary>
        internal int Unseen
        {
            get{ 
                int count = m_pMessages.Count;
                foreach(IMAP_MessageInfo m in m_pMessages){
                    foreach(string flag in m.Flags){
                        if(string.Equals(flag,"Seen",StringComparison.InvariantCultureIgnoreCase)){
                            count--;
                            break;
                        }
                    }
                }

                return count; 
            }
        }

        /// <summary>
        /// Gets next message predicted UID value.
        /// </summary>
        internal long UidNext
        {
            get{ 
                long maxUID = 0;
                foreach(IMAP_MessageInfo m in m_pMessages){
                    if(m.UID > maxUID){
                        maxUID = m.UID;
                    }
                }
                maxUID++;
                
                return maxUID; 
            }
        }

        #endregion
    }
}
