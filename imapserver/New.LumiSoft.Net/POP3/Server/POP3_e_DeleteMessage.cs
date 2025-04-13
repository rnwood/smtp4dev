using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.POP3.Server
{
    /// <summary>
    /// This class provides data for <see cref="POP3_Session.DeleteMessage"/> event.
    /// </summary>
    public class POP3_e_DeleteMessage : EventArgs
    {
        private POP3_ServerMessage m_pMessage = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="message">Message to delete.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>message</b> is null reference.</exception>
        internal POP3_e_DeleteMessage(POP3_ServerMessage message)
        {
            if(message == null){
                throw new ArgumentNullException("message");
            }

            m_pMessage = message;
        }


        #region Properties implementation

        /// <summary>
        /// Gets message info.
        /// </summary>
        public POP3_ServerMessage Message
        {
            get{ return m_pMessage; }
        }

        #endregion
    }
}
