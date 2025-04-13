using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP.Relay
{
    /// <summary>
    /// Thsi class holds Relay_Queue queued item.
    /// </summary>
    public class Relay_QueueItem
    {
        private Relay_Queue     m_pQueue            = null;
        private string          m_From              = "";
        private string          m_EnvelopeID        = null;
        private SMTP_DSN_Ret    m_DSN_Ret           = SMTP_DSN_Ret.NotSpecified;
        private string          m_To                = "";
        private string          m_OriginalRecipient = null;
        private SMTP_DSN_Notify m_DSN_Notify        = SMTP_DSN_Notify.NotSpecified;
        private string          m_MessageID         = "";
        private Stream          m_pMessageStream    = null;
        private object          m_pTag              = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="queue">Item owner queue.</param>
        /// <param name="from">Sender address.</param>
        /// <param name="envelopeID">Envelope ID_(MAIL FROM: ENVID).</param>
        /// <param name="ret">Specifies what parts of message are returned in DSN report.</param>
        /// <param name="to">Target recipient address.</param>
        /// <param name="originalRecipient">Original recipient(RCPT TO: ORCPT).</param>
        /// <param name="notify">DSN notify condition.</param>
        /// <param name="messageID">Message ID.</param>
        /// <param name="message">Raw mime message. Message reading starts from current position.</param>
        /// <param name="tag">User data.</param>
        internal Relay_QueueItem(Relay_Queue queue,string from,string envelopeID,SMTP_DSN_Ret ret,string to,string originalRecipient,SMTP_DSN_Notify notify,string messageID,Stream message,object tag)
        {
            m_pQueue            = queue;
            m_From              = from;
            m_EnvelopeID        = envelopeID;
            m_DSN_Ret           = ret;
            m_To                = to;
            m_OriginalRecipient = originalRecipient;
            m_DSN_Notify        = notify;
            m_MessageID         = messageID;
            m_pMessageStream    = message;
            m_pTag              = tag;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets this relay item owner queue.
        /// </summary>
        public Relay_Queue Queue
        {
            get{ return m_pQueue; }
        }

        /// <summary>
        /// Gets from address.
        /// </summary>
        public string From
        {
            get{ return m_From; }
        }

        /// <summary>
        /// Gets DSN ENVID value.
        /// </summary>
        public string EnvelopeID
        {
            get{ return m_EnvelopeID; }
        }

        /// <summary>
        /// Gets DSN RET value.
        /// </summary>
        public SMTP_DSN_Ret DSN_Ret
        {
            get{ return m_DSN_Ret; }
        }

        /// <summary>
        /// Gets target recipient.
        /// </summary>
        public string To
        {
            get{ return m_To; }
        }

        /// <summary>
        /// Gets DSN ORCPT value.
        /// </summary>
        public string OriginalRecipient
        {
            get{ return m_OriginalRecipient; }
        }

        /// <summary>
        /// Gets DSN Notify value.
        /// </summary>
        public SMTP_DSN_Notify DSN_Notify
        {
            get{ return m_DSN_Notify; }
        }

        /// <summary>
        /// Gets message ID which is being relayed now.
        /// </summary>
        public string MessageID
        {
            get{ return m_MessageID; }
        }

        /// <summary>
        /// Gets raw mime message which must be relayed.
        /// </summary>
        public Stream MessageStream
        {
            get{ return m_pMessageStream; }
        }

        /// <summary>
        /// Gets or sets user data.
        /// </summary>
        public object Tag
        {
            get{ return m_pTag; }

            set{ m_pTag = value; }
        }

        #endregion

    }
}
