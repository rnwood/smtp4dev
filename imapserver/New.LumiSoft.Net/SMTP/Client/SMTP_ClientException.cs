using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SMTP.Client
{
    /// <summary>
    /// SMTP client exception.
    /// </summary>
    public class SMTP_ClientException : Exception
    {
        private SMTP_t_ReplyLine[] m_pReplyLines  = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="responseLine">SMTP server response line.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>responseLine</b> is null.</exception>
        public SMTP_ClientException(string responseLine) : base(responseLine.TrimEnd())
        {
            if(responseLine == null){
                throw new ArgumentNullException("responseLine");
            }

            m_pReplyLines = new SMTP_t_ReplyLine[]{SMTP_t_ReplyLine.Parse(responseLine)};
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="replyLines">SMTP server error reply lines.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>replyLines</b> is null reference.</exception>
        public SMTP_ClientException(SMTP_t_ReplyLine[] replyLines) : base(replyLines[0].ToString().TrimEnd())
        {
            if(replyLines == null){
                throw new ArgumentNullException("replyLines");
            }

            m_pReplyLines = replyLines;            
        }


        #region Properties Implementation

        /// <summary>
        /// Gets SMTP status code.
        /// </summary>
        [Obsolete("Use property 'ReplyLines' insead.")]
        public int StatusCode
        {
            get{ return m_pReplyLines[0].ReplyCode; }
        }

        /// <summary>
        /// Gets SMTP server response text after status code.
        /// </summary>
        [Obsolete("Use property 'ReplyLines' insead.")]
        public string ResponseText
        {
            get{ return m_pReplyLines[0].Text; }
        }

        /// <summary>
        /// Gets SMTP server error reply lines.
        /// </summary>
        public SMTP_t_ReplyLine[] ReplyLines
        {
            get{ return m_pReplyLines; }
        }

        /// <summary>
        /// Gets if it is permanent SMTP(5xx) error.
        /// </summary>
        public bool IsPermanentError
        {
            get{
                if(m_pReplyLines[0].ReplyCode >= 500 && m_pReplyLines[0].ReplyCode <= 599){
                    return true;
                }
                else{
                    return false;
                }
            }
        }

        #endregion

    }
}
