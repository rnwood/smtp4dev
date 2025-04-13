using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP BYE response. Defined in RFC 3501 7.1.5.
    /// </summary>
    public class IMAP_r_u_Bye : IMAP_r_u
    {
        private string m_Text = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="text">Bye reason text.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>text</b> is null reference.</exception>
        public IMAP_r_u_Bye(string text)
        {
            if(text == null){
                throw new ArgumentNullException("text");
            }

            m_Text = text;
        }
        

        #region static method Parse

        /// <summary>
        /// Parses BYE response from bye-response string.
        /// </summary>
        /// <param name="byeResponse">Bye response string.</param>
        /// <returns>Returns parsed BYE response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>byeResponse</b> is null reference.</exception>
        public static IMAP_r_u_Bye Parse(string byeResponse)
        {
            if(byeResponse == null){
                throw new ArgumentNullException("byeResponse");
            }

            /* RFC 3501 7.1.5. BYE Response.
               Contents:   OPTIONAL response code
                           human-readable text

                  The BYE response is always untagged, and indicates that the server
                  is about to close the connection.  The human-readable text MAY be
                  displayed to the user in a status report by the client.  The BYE
                  response is sent under one of four conditions:

                     1) as part of a normal logout sequence.  The server will close
                        the connection after sending the tagged OK response to the
                        LOGOUT command.

                     2) as a panic shutdown announcement.  The server closes the
                        connection immediately.

                     3) as an announcement of an inactivity autologout.  The server
                        closes the connection immediately.

                     4) as one of three possible greetings at connection startup,
                        indicating that the server is not willing to accept a
                        connection from this client.  The server closes the
                        connection immediately.

                  The difference between a BYE that occurs as part of a normal
                  LOGOUT sequence (the first case) and a BYE that occurs because of
                  a failure (the other three cases) is that the connection closes
                  immediately in the failure case.  In all cases the client SHOULD
                  continue to read response data from the server until the
                  connection is closed; this will ensure that any pending untagged
                  or completion responses are read and processed.

               Example:    S: * BYE Autologout; idle for too long
            */

            StringReader r = new StringReader(byeResponse);
            // Eat "*"
            r.ReadWord();
            // Eat "BYE"
            r.ReadWord();

            return new IMAP_r_u_Bye(r.ReadToEnd());
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            // Example:  S: * BYE Autologout; idle for too long

            StringBuilder retVal = new StringBuilder();
            retVal.Append("* BYE " + m_Text + "\r\n");

            return retVal.ToString();
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets BYE reason text.
        /// </summary>
        public string Text
        {
            get{ return m_Text; }
        }

        #endregion
    }
}
