using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP CAPABILITY response. Defined in RFC 3501 7.2.1.
    /// </summary>
    public class IMAP_r_u_Capability : IMAP_r_u
    {
        private string[] m_pCapabilities = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="capabilities">Capabilities list.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>capabilities</b> is null reference.</exception>
        public IMAP_r_u_Capability(string[] capabilities)
        {
            if(capabilities == null){
                throw new ArgumentNullException("capabilities");
            }

            m_pCapabilities = capabilities;
        }


        #region static method Parse

        /// <summary>
        /// Parses CAPABILITY response from capability-response string.
        /// </summary>
        /// <param name="response">Capability response string.</param>
        /// <returns>Returns parsed CAPABILITY response.</returns>
        /// <exception cref="ArgumentNullException">Is riased when <b>response</b> is null reference.</exception>
        public static IMAP_r_u_Capability Parse(string response)
        {
            if(response == null){
                throw new ArgumentNullException("response");
            }

            /* RFC 3501 7.2.1. CAPABILITY Response.
                Contents:   capability listing

                The CAPABILITY response occurs as a result of a CAPABILITY
                command.  The capability listing contains a space-separated
                listing of capability names that the server supports.  The
                capability listing MUST include the atom "IMAP4rev1".

                In addition, client and server implementations MUST implement the
                STARTTLS, LOGINDISABLED, and AUTH=PLAIN (described in [IMAP-TLS])
                capabilities.  See the Security Considerations section for
                important information.

                A capability name which begins with "AUTH=" indicates that the
                server supports that particular authentication mechanism.

                The LOGINDISABLED capability indicates that the LOGIN command is
                disabled, and that the server will respond with a tagged NO
                response to any attempt to use the LOGIN command even if the user
                name and password are valid.  An IMAP client MUST NOT issue the
                LOGIN command if the server advertises the LOGINDISABLED
                capability.

                Other capability names indicate that the server supports an
                extension, revision, or amendment to the IMAP4rev1 protocol.
                Server responses MUST conform to this document until the client
                issues a command that uses the associated capability.

                Capability names MUST either begin with "X" or be standard or
                standards-track IMAP4rev1 extensions, revisions, or amendments
                registered with IANA.  A server MUST NOT offer unregistered or
                non-standard capability names, unless such names are prefixed with
                an "X".

                Client implementations SHOULD NOT require any capability name
                other than "IMAP4rev1", and MUST ignore any unknown capability
                names.

                A server MAY send capabilities automatically, by using the
                CAPABILITY response code in the initial PREAUTH or OK responses,
                and by sending an updated CAPABILITY response code in the tagged
                OK response as part of a successful authentication.  It is
                unnecessary for a client to send a separate CAPABILITY command if
                it recognizes these automatic capabilities.

                Example:    S: * CAPABILITY IMAP4rev1 STARTTLS AUTH=GSSAPI XPIG-LATIN
            */

            StringReader r = new StringReader(response);
            // Eat "*"
            r.ReadWord();
            // Eat "CAPABILITY"
            r.ReadWord();

            string[] capabilities = r.ReadToEnd().Split(' ');

            return new IMAP_r_u_Capability(capabilities);
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            // Example:    S: * CAPABILITY IMAP4rev1 STARTTLS AUTH=GSSAPI XPIG-LATIN

            StringBuilder retVal = new StringBuilder();
            retVal.Append("* CAPABILITY");
            foreach(string capability in m_pCapabilities){
                retVal.Append(" " + capability);
            }
            retVal.Append("\r\n");

            return retVal.ToString();
        }

        #endregion


        #region Properties impelementation

        /// <summary>
        /// Gets capabilities list.
        /// </summary>
        public string[] Capabilities
        {
            get{ return m_pCapabilities; }
        }

        #endregion
    }
}
