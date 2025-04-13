using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP RECENT response. Defined in RFC 3501 7.3.2.
    /// </summary>
    public class IMAP_r_u_Recent : IMAP_r_u
    {
        private int m_MessageCount = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="messageCount">Message count with \Recent flag set.</param>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_r_u_Recent(int messageCount)
        {
            if(messageCount < 0){
                throw new ArgumentException("Arguments 'messageCount' value must be >= 0.","messageCount");
            }

            m_MessageCount = messageCount;
        }


        #region static method Parse

        /// <summary>
        /// Parses RECENT response from recent-response string.
        /// </summary>
        /// <param name="response">Recent response string.</param>
        /// <returns>Returns parsed recent response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        public static IMAP_r_u_Recent Parse(string response)
        {
            if(response == null){
                throw new ArgumentNullException("response");
            }

            /* RFC 3501 7.3.2.  RECENT Response
                Contents:   none

                  The RECENT response reports the number of messages with the
                  \Recent flag set.  This response occurs as a result of a SELECT or
                  EXAMINE command, and if the size of the mailbox changes (e.g., new
                  messages).

                       Note: It is not guaranteed that the message sequence
                       numbers of recent messages will be a contiguous range of
                       the highest n messages in the mailbox (where n is the
                       value reported by the RECENT response).  Examples of
                       situations in which this is not the case are: multiple
                       clients having the same mailbox open (the first session
                       to be notified will see it as recent, others will
                       probably see it as non-recent), and when the mailbox is
                       re-ordered by a non-IMAP agent.

                       The only reliable way to identify recent messages is to
                       look at message flags to see which have the \Recent flag
                       set, or to do a SEARCH RECENT.

                  The update from the RECENT response MUST be recorded by the
                  client.

                Example:    S: * 5 RECENT
            */
                                               
            return new IMAP_r_u_Recent(Convert.ToInt32(response.Split(' ')[1]));
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            // Example:    S: * 5 RECENT

            return "* " + m_MessageCount.ToString() + " RECENT\r\n";
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets number of messages in mailbox with \Recent flag set.
        /// </summary>
        public int MessageCount
        {
            get{ return m_MessageCount; }
        }

        #endregion
    }
}
