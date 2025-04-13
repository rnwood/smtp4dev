using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP.Server
{
    /// <summary>
    /// This class provided data for <b cref="SMTP_Session.Ehlo">SMTP_Session.Ehlo</b> event.
    /// </summary>
    public class SMTP_e_Ehlo : EventArgs
    {
        private SMTP_Session m_pSession = null;
        private string       m_Domain   = "";
        private SMTP_Reply   m_pReply   = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="session">Owner SMTP server session.</param>
        /// <param name="domain">Ehlo/Helo domain name.</param>
        /// <param name="reply">SMTP server reply.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>session</b>, <b>domain</b> or <b>reply</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public SMTP_e_Ehlo(SMTP_Session session,string domain,SMTP_Reply reply)
        {
            if(session == null){
                throw new ArgumentNullException("session");
            }
            if(domain == null){
                throw new ArgumentNullException("domain");
            }
            if(domain == string.Empty){
                throw new ArgumentException("Argument 'domain' value must be sepcified.","domain");
            }
            if(reply == null){
                throw new ArgumentNullException("reply");
            }

            m_pSession = session;
            m_Domain   = domain;
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
        /// Gets connected client reported domain name.
        /// </summary>
        public string Domain
        {
            get{ return m_Domain; }
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
