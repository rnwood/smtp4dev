using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.POP3.Server
{
    /// <summary>
    /// This class provides data for <see cref="POP3_Session.GetTopOfMessage"/> event.
    /// </summary>
    public class POP3_e_GetTopOfMessage : EventArgs
    {
        private POP3_ServerMessage m_pMessage  = null;
        private int                m_LineCount = 0;
        private byte[]             m_pData     = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="message">Message which top data to get.</param>
        /// <param name="lines">Number of message-body lines to get.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>message</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        internal POP3_e_GetTopOfMessage(POP3_ServerMessage message,int lines)
        {
            if(message == null){
                throw new ArgumentNullException("message");
            }
            if(lines < 0){
                throw new ArgumentException("Argument 'lines' value must be >= 0.","lines");
            }

            m_pMessage  = message;
            m_LineCount = lines;
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
        /// Gets number message body lines should be included.
        /// </summary>
        public int LineCount
        {
            get{ return m_LineCount; }
        }

        /// <summary>
        /// Gets or sets top of message data.
        /// </summary>
        /// <remarks>This value should contain message header + number of <b>lineCount</b> body lines.</remarks>
        public byte[] Data
        {
            get{ return m_pData; }

            set{ m_pData = value; }
        }

        #endregion
    }
}
