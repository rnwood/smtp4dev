using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP EXISTS response. Defined in RFC 3501 7.3.1.
    /// </summary>
    public class IMAP_r_u_Exists : IMAP_r_u
    {
        private int m_MessageCount = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="messageCount">Message count.</param>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_r_u_Exists(int messageCount)
        {
            if(messageCount < 0){
                throw new ArgumentException("Arguments 'messageCount' value must be >= 0.","messageCount");
            }

            m_MessageCount = messageCount;
        }


        #region static method Parse

        /// <summary>
        /// Parses EXISTS response from exists-response string.
        /// </summary>
        /// <param name="response">Exists response string.</param>
        /// <returns>Returns parsed exists response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        public static IMAP_r_u_Exists Parse(string response)
        {
            if(response == null){
                throw new ArgumentNullException("response");
            }

            /* RFC 3501 7.3.1.  EXISTS Response
                Contents:   none

                  The EXISTS response reports the number of messages in the mailbox.
                  This response occurs as a result of a SELECT or EXAMINE command,
                  and if the size of the mailbox changes (e.g., new messages).

                  The update from the EXISTS response MUST be recorded by the
                  client.

                Example:    S: * 23 EXISTS
            */
                                               
            return new IMAP_r_u_Exists(Convert.ToInt32(response.Split(' ')[1]));
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            // Example:    S: * 23 EXISTS

            return "* " + m_MessageCount.ToString() + " EXISTS\r\n";
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets number of messages in mailbox.
        /// </summary>
        public int MessageCount
        {
            get{ return m_MessageCount; }
        }

        #endregion
    }
}
