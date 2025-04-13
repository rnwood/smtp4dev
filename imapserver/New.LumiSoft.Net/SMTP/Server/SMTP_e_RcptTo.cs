using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP.Server
{
    /// <summary>
    /// This class provided data for <b cref="SMTP_Session.RcptTo">SMTP_Session.RcptTo</b> event.
    /// </summary>
    public class SMTP_e_RcptTo : EventArgs
    {
        private SMTP_Session m_pSession  = null;
        private SMTP_RcptTo  m_pRcptTo   = null;
        private SMTP_Reply   m_pReply    = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="session">Owner SMTP server session.</param>
        /// <param name="to">RCPT TO: value.</param>
        /// <param name="reply">SMTP server reply.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>session</b>, <b>to</b> or <b>reply</b> is null reference.</exception>
        public SMTP_e_RcptTo(SMTP_Session session,SMTP_RcptTo to,SMTP_Reply reply)
        {
            if(session == null){
                throw new ArgumentNullException("session");
            }
            if(to == null){
                throw new ArgumentNullException("from");
            }
            if(reply == null){
                throw new ArgumentNullException("reply");
            }

            m_pSession = session;
            m_pRcptTo  = to;
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
        /// Gets RCPT TO: value.
        /// </summary>
        public SMTP_RcptTo RcptTo
        {
            get{ return m_pRcptTo; }
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
