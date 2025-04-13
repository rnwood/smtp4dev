using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP.Server
{
    /// <summary>
    /// This class provided data for <b cref="SMTP_Session.MailFrom">SMTP_Session.MailFrom</b> event.
    /// </summary>
    public class SMTP_e_MailFrom : EventArgs
    {
        private SMTP_Session  m_pSession  = null;
        private SMTP_MailFrom m_pMailFrom = null;
        private SMTP_Reply    m_pReply    = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="session">Owner SMTP server session.</param>
        /// <param name="from">MAIL FROM: value.</param>
        /// <param name="reply">SMTP server reply.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>session</b>, <b>from</b> or <b>reply</b> is null reference.</exception>
        public SMTP_e_MailFrom(SMTP_Session session,SMTP_MailFrom from,SMTP_Reply reply)
        {
            if(session == null){
                throw new ArgumentNullException("session");
            }
            if(from == null){
                throw new ArgumentNullException("from");
            }
            if(reply == null){
                throw new ArgumentNullException("reply");
            }

            m_pSession  = session;
            m_pMailFrom = from;
            m_pReply    = reply;
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
        /// Gets MAIL FROM: value.
        /// </summary>
        public SMTP_MailFrom MailFrom
        {
            get{ return m_pMailFrom; }
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
