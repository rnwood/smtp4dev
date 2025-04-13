using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Principal;

namespace LumiSoft.Net.POP3.Server
{
    /// <summary>
    /// This class provides data for <see cref="POP3_Session.GetMessagesInfo"/> event.
    /// </summary>
    public class POP3_e_GetMessagesInfo : EventArgs
    {
        private List<POP3_ServerMessage> m_pMessages = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal POP3_e_GetMessagesInfo()
        {
            m_pMessages = new List<POP3_ServerMessage>();
        }


        #region Properties implementation

        /// <summary>
        /// Gets POP3 messages info collection.
        /// </summary>
        public List<POP3_ServerMessage> Messages
        {
            get{ return m_pMessages; }
        }

        #endregion
    }
}
