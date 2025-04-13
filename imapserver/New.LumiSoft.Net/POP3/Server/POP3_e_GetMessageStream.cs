using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.POP3.Server
{
    /// <summary>
    /// This class provides data for <see cref="POP3_Session.GetMessageStream"/> event.
    /// </summary>
    public class POP3_e_GetMessageStream : EventArgs
    {
        private POP3_ServerMessage m_pMessage    = null;
        private bool               m_CloseStream = true;
        private Stream             m_pStream     = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="message">Message which top data to get.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>message</b> is null reference.</exception>
        internal POP3_e_GetMessageStream(POP3_ServerMessage message)
        {
            if(message == null){
                throw new ArgumentNullException("message");
            }

            m_pMessage  = message;
        }


        #region Properties implementation

        /// <summary>
        /// Gets message info.
        /// </summary>
        public POP3_ServerMessage Message
        {
            get{ return m_pMessage; }
        }

        /// <summary>
        /// Gets or sets if message stream is closed after message sending has completed.
        /// </summary>
        public bool CloseMessageStream
        {
            get{ return m_CloseStream; }

            set{ m_CloseStream = value; }
        }

        /// <summary>
        /// Gets or sets message stream.
        /// </summary>
        /// <remarks>POP3 server starts reading message from stream current position and reads while end of stream reached.</remarks>
        public Stream MessageStream
        {
            get{ return m_pStream; }

            set{ m_pStream = value; }
        }

        #endregion
    }
}
