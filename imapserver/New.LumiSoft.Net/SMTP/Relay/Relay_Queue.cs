using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP.Relay
{
    /// <summary>
    /// This class implements SMTP relay queue.
    /// </summary>
    public class Relay_Queue : IDisposable
    {
        private string                 m_Name   = "";
        private Queue<Relay_QueueItem> m_pQueue = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">Relay queue name.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>name</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public Relay_Queue(string name)
        {
            if(name == null){
                throw new ArgumentNullException("name");
            }
            if(name == ""){
                throw new ArgumentException("Argument 'name' value may not be empty.");
            }

            m_Name   = name;
            m_pQueue = new Queue<Relay_QueueItem>();
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public void Dispose()
        {
        }

        #endregion


        #region method QueueMessage

        /// <summary>
        /// Queues message for relay.
        /// </summary>
        /// <param name="from">Sender address.</param>
        /// <param name="to">Target recipient address.</param>
        /// <param name="messageID">Message ID.</param>
        /// <param name="message">Raw mime message. Message reading starts from current position.</param>
        /// <param name="tag">User data.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>to</b>,<b>to</b>,<b>messageID</b> or <b>message</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public void QueueMessage(string from,string to,string messageID,Stream message,object tag)
        {
            QueueMessage(from,null,SMTP_DSN_Ret.NotSpecified,to,null,SMTP_DSN_Notify.NotSpecified,messageID,message,tag);
        }

        /// <summary>
        /// Queues message for relay.
        /// </summary>
        /// <param name="from">Sender address.</param>
        /// <param name="envelopeID">Envelope ID_(MAIL FROM: ENVID).</param>
        /// <param name="ret">Specifies what parts of message are returned in DSN report.</param>
        /// <param name="to">Target recipient address.</param>
        /// <param name="originalRecipient">Original recipient(RCPT TO: ORCPT).</param>
        /// <param name="notify">DSN notify condition.</param>
        /// <param name="messageID">Message ID.</param>
        /// <param name="message">Raw mime message. Message reading starts from current position.</param>
        /// <param name="tag">User data.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>to</b>,<b>to</b>,<b>messageID</b> or <b>message</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public void QueueMessage(string from,string envelopeID,SMTP_DSN_Ret ret,string to,string originalRecipient,SMTP_DSN_Notify notify,string messageID,Stream message,object tag)
        {
            if(messageID == null){
                throw new ArgumentNullException("messageID");
            }
            if(messageID == ""){
                throw new ArgumentException("Argument 'messageID' value must be specified.");
            }
            if(message == null){
                throw new ArgumentNullException("message");
            }

            lock(m_pQueue){
                m_pQueue.Enqueue(new Relay_QueueItem(this,from,envelopeID,ret,to,originalRecipient,notify,messageID,message,tag));
            }
        }

        #endregion

        #region method DequeueMessage

        /// <summary>
        /// Dequeues message from queue. If there are no messages, this method returns null.
        /// </summary>
        /// <returns>Returns queued relay message or null if no messages.</returns>
        public Relay_QueueItem DequeueMessage()
        {
            lock(m_pQueue){
                if(m_pQueue.Count > 0){
                    return m_pQueue.Dequeue();
                }
                else{
                    return null;
                }
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets queue name.
        /// </summary>
        public string Name
        {
            get{ return m_Name; }
        }

        /// <summary>
        /// Gets number of queued items in queue.
        /// </summary>
        public int Count
        {
            get{ return m_pQueue.Count; }
        }

        #endregion

    }
}
