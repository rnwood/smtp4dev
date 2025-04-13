using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP ENABLE response. Defined in RFC 5161 4.
    /// </summary>
    public class IMAP_r_u_Enable : IMAP_r_u
    {
        private string[] m_Capabilities = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="capabilities">IMAP capabilities.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>capabilities</b> is null reference.</exception>
        public IMAP_r_u_Enable(string[] capabilities)
        {
            if(capabilities == null){
                throw new ArgumentNullException("capabilities");
            }

            m_Capabilities = capabilities;
        }
        

        #region static method Parse

        /// <summary>
        /// Parses ENABLE response from enable-response string.
        /// </summary>
        /// <param name="enableResponse">Enable response string.</param>
        /// <returns>Returns parsed ENABLE response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>enableResponse</b> is null reference.</exception>
        public static IMAP_r_u_Enable Parse(string enableResponse)
        {
            if(enableResponse == null){
                throw new ArgumentNullException("enableResponse");
            }

            /* RFC 5161 4.  Formal Syntax
                The following syntax specification uses the Augmented Backus-Naur
                Form (ABNF) notation as specified in [RFC5234] including the core
                rules in Appendix B.1.  [RFC3501] defines the non-terminals
                "capability" and "command-any".

                Except as noted otherwise, all alphabetic characters are
                case-insensitive.  The use of upper or lower case characters to
                define token strings is for editorial clarity only.  Implementations
                MUST accept these strings in a case-insensitive fashion.

                    capability    =/ "ENABLE"
                    command-any   =/ "ENABLE" 1*(SP capability)
                    response-data =/ "*" SP enable-data CRLF
                    enable-data   = "ENABLED" *(SP capability)
            */

            StringReader r = new StringReader(enableResponse);
            // Eat "*"
            r.ReadWord();
            // Eat "ENABLED"
            r.ReadWord();

            return new IMAP_r_u_Enable(r.ReadToEnd().Split(' '));
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            // Example: S: * ENABLED X-GOOD-IDEA

            StringBuilder retVal = new StringBuilder();
            retVal.Append("* ENABLED");
            foreach(string capability in m_Capabilities){
                retVal.Append(" " + capability);
            }
            retVal.Append("\r\n");

            return retVal.ToString();
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets IMAP capabilities.
        /// </summary>
        public string[] Capabilities
        {
            get{ return m_Capabilities; }
        }

        #endregion
    }
}
