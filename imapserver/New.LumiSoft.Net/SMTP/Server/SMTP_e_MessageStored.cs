using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP.Server
{
    /// <summary>
    /// This class provided data for <b cref="SMTP_Session.MessageStoringCompleted">SMTP_Session.MessageStoringCompleted</b> event.
    /// </summary>
    public class SMTP_e_MessageStored : EventArgs
    {
        private SMTP_Session m_pSession = null;
        private Stream       m_pStream  = null;
        private SMTP_Reply   m_pReply   = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="session">Owner SMTP server session.</param>
        /// <param name="stream">Message stream.</param>
        /// <param name="reply">SMTP server reply.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>session</b>, <b>stream</b> or <b>reply</b> is null reference.</exception>
        public SMTP_e_MessageStored(SMTP_Session session,Stream stream,SMTP_Reply reply)
        {
            if(session == null){
                throw new ArgumentNullException("session");
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }
            if(reply == null){
                throw new ArgumentNullException("reply");
            }

            m_pSession = session;
            m_pStream  = stream;
            m_pReply   = reply;
        }


        #region Properties implementation

        /// <summary>
        /// Gets owner SMTP session.
        /// </summary>
        public SMTP_Session Session
        {
            get{ return m_pSession; }
        }

        /// <summary>
        /// Gets message stream where message has stored.
        /// </summary>
        public Stream Stream
        {
            get{ return m_pStream; }
        }

        /// <summary>
        /// Gets or sets SMTP server reply.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null reference passed.</exception>
        public SMTP_Reply Reply
        {
            get{ return m_pReply; }

            set{
                if(value == null){
                    throw new ArgumentNullException("Reply");
                }

                m_pReply = value;
            }
        }

        #endregion
    }
}
