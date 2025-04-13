using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP.Server
{
    /// <summary>
    ///  This class holds RCPT TO: command value.
    /// </summary>
    public class SMTP_RcptTo
    {
        private string          m_Mailbox = "";
        private SMTP_DSN_Notify m_Notify  = SMTP_DSN_Notify.NotSpecified;
        private string          m_ORCPT   = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="mailbox">Mailbox value.</param>
        /// <param name="notify">DSN NOTIFY parameter value.</param>
        /// <param name="orcpt">DSN ORCPT parameter value.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>mailbox</b> is null reference.</exception>
        public SMTP_RcptTo(string mailbox,SMTP_DSN_Notify notify,string orcpt)
        {
            if(mailbox == null){
                throw new ArgumentNullException("mailbox");
            }

            m_Mailbox = mailbox;
            m_Notify  = notify;
            m_ORCPT   = orcpt;
        }


        #region Properties implementation

        /// <summary>
        /// Gets SMTP "mailbox" value. Actually this is just email address.
        /// </summary>
        public string Mailbox
        {
            get{ return m_Mailbox; }
        }

        /// <summary>
        /// Gets DSN NOTIFY parameter value.
        /// This value specified when SMTP server should send delivery status notification.
        /// Defined in RFC 1891.
        /// </summary>
        public SMTP_DSN_Notify Notify
        {
            get{ return m_Notify; }
        }

        /// <summary>
        /// Gets DSN ORCPT parameter value. Value null means not specified.
        /// This value specifies "original" recipient address where message is sent (has point only when message forwarded).
        /// Defined in RFC 1891.
        /// </summary>
        public string ORCPT
        {
            get{ return m_ORCPT; }
        }

        #endregion
    }
}
