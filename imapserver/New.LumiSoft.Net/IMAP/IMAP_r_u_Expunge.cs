using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP EXPUNGE response. Defined in RFC 3501 7.4.1.
    /// </summary>
    public class IMAP_r_u_Expunge : IMAP_r_u
    {
        private int m_SeqNo = 1;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="seqNo">Message sequence number.</param>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_r_u_Expunge(int seqNo)
        {
            if(seqNo < 1){
                throw new ArgumentException("Arguments 'seqNo' value must be >= 1.","seqNo");
            }

            m_SeqNo = seqNo;
        }


        #region static method Parse

        /// <summary>
        /// Parses EXPUNGE response from expunge-response string.
        /// </summary>
        /// <param name="response">Expunge response string.</param>
        /// <returns>Returns parsed expunge response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        public static IMAP_r_u_Expunge Parse(string response)
        {
            if(response == null){
                throw new ArgumentNullException("response");
            }

            /* RFC 3501 7.4.1. 7.4.1. EXPUNGE Response.
                Contents:   none

                The EXPUNGE response reports that the specified message sequence
                number has been permanently removed from the mailbox.  The message
                sequence number for each successive message in the mailbox is
                immediately decremented by 1, and this decrement is reflected in
                message sequence numbers in subsequent responses (including other
                untagged EXPUNGE responses).

                The EXPUNGE response also decrements the number of messages in the
                mailbox; it is not necessary to send an EXISTS response with the
                new value.
    
                As a result of the immediate decrement rule, message sequence
                numbers that appear in a set of successive EXPUNGE responses
                depend upon whether the messages are removed starting from lower
                numbers to higher numbers, or from higher numbers to lower
                numbers.  For example, if the last 5 messages in a 9-message
                mailbox are expunged, a "lower to higher" server will send five
                untagged EXPUNGE responses for message sequence number 5, whereas
                a "higher to lower server" will send successive untagged EXPUNGE
                responses for message sequence numbers 9, 8, 7, 6, and 5.

                An EXPUNGE response MUST NOT be sent when no command is in
                progress, nor while responding to a FETCH, STORE, or SEARCH
                command.  This rule is necessary to prevent a loss of
                synchronization of message sequence numbers between client and
                server.  A command is not "in progress" until the complete command
                has been received; in particular, a command is not "in progress"
                during the negotiation of command continuation.

                    Note: UID FETCH, UID STORE, and UID SEARCH are different
                    commands from FETCH, STORE, and SEARCH.  An EXPUNGE
                    response MAY be sent during a UID command.

                The update from the EXPUNGE response MUST be recorded by the
                client.

                Example:    S: * 44 EXPUNGE
            */
                                               
            return new IMAP_r_u_Expunge(Convert.ToInt32(response.Split(' ')[1]));
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            // Example:    S: * 44 EXPUNGE

            return "* " + m_SeqNo.ToString() + " EXPUNGE\r\n";
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets message sequence number.
        /// </summary>
        public int SeqNo
        {
            get{ return m_SeqNo; }
        }

        #endregion
    }
}
