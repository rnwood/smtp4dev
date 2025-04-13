using System;
using System.Collections.Generic;
using System.Text;
using System.Security.Cryptography;

namespace LumiSoft.Net.AUTH
{
    /// <summary>
    /// Implements "CRAM-MD5" authenticaiton.
    /// </summary>
    public class AUTH_SASL_Client_CramMd5 : AUTH_SASL_Client
    {
        private bool   m_IsCompleted = false;
        private int    m_State       = 0;
        private string m_UserName    = null;
        private string m_Password    = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="userName">User login name.</param>
        /// <param name="password">User password.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>userName</b> or <b>password</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public AUTH_SASL_Client_CramMd5(string userName,string password)
        {
            if(userName == null){
                throw new ArgumentNullException("userName");
            }
            if(userName == string.Empty){
                throw new ArgumentException("Argument 'username' value must be specified.","userName");
            }
            if(password == null){
                throw new ArgumentNullException("password");
            }

            m_UserName = userName;
            m_Password = password;
        }


        #region method Continue

        /// <summary>
        /// Continues authentication process.
        /// </summary>
        /// <param name="serverResponse">Server sent SASL response.</param>
        /// <returns>Returns challange request what must be sent to server or null if authentication has completed.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>serverResponse</b> is null reference.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this method is called when authentication is completed.</exception>
        public override byte[] Continue(byte[] serverResponse)
        {
            if(serverResponse == null){
                throw new ArgumentNullException("serverResponse");
            }
            if(m_IsCompleted){
                throw new InvalidOperationException("Authentication is completed.");
            }

            /* RFC 2195 2. Challenge-Response Authentication Mechanism.
                The authentication type associated with CRAM is "CRAM-MD5".

                The data encoded in the first ready response contains an
                presumptively arbitrary string of random digits, a timestamp, and the
                fully-qualified primary host name of the server.  The syntax of the
                unencoded form must correspond to that of an RFC 822 'msg-id'
                [RFC822] as described in [POP3].

                The client makes note of the data and then responds with a string
                consisting of the user name, a space, and a 'digest'.  The latter is
                computed by applying the keyed MD5 algorithm from [KEYED-MD5] where
                the key is a shared secret and the digested text is the timestamp
                (including angle-brackets).

                This shared secret is a string known only to the client and server.
                The `digest' parameter itself is a 16-octet value which is sent in
                hexadecimal format, using lower-case ASCII characters.

                When the server receives this client response, it verifies the digest
                provided.  If the digest is correct, the server should consider the
                client authenticated and respond appropriately.
              
                Example:
                    The examples in this document show the use of the CRAM mechanism with
                    the IMAP4 AUTHENTICATE command [IMAP-AUTH].  The base64 encoding of
                    the challenges and responses is part of the IMAP4 AUTHENTICATE
                    command, not part of the CRAM specification itself.

                    S: * OK IMAP4 Server
                    C: A0001 AUTHENTICATE CRAM-MD5
                    S: + PDE4OTYuNjk3MTcwOTUyQHBvc3RvZmZpY2UucmVzdG9uLm1jaS5uZXQ+
                    C: dGltIGI5MTNhNjAyYzdlZGE3YTQ5NWI0ZTZlNzMzNGQzODkw
                    S: A0001 OK CRAM authentication successful

                    In this example, the shared secret is the string
                    'tanstaaftanstaaf'.  Hence, the Keyed MD5 digest is produced by
                    calculating

                    MD5((tanstaaftanstaaf XOR opad),
                        MD5((tanstaaftanstaaf XOR ipad),
                        <1896.697170952@postoffice.reston.mci.net>))

                    where ipad and opad are as defined in the keyed-MD5 Work in
                    Progress [KEYED-MD5] and the string shown in the challenge is the
                    base64 encoding of <1896.697170952@postoffice.reston.mci.net>. The
                    shared secret is null-padded to a length of 64 bytes. If the
                    shared secret is longer than 64 bytes, the MD5 digest of the
                    shared secret is used as a 16 byte input to the keyed MD5
                    calculation.

                    This produces a digest value (in hexadecimal) of

                        b913a602c7eda7a495b4e6e7334d3890

                    The user name is then prepended to it, forming

                        tim b913a602c7eda7a495b4e6e7334d3890

                    Which is then base64 encoded to meet the requirements of the IMAP4
                    AUTHENTICATE command (or the similar POP3 AUTH command), yielding

                    dGltIGI5MTNhNjAyYzdlZGE3YTQ5NWI0ZTZlNzMzNGQzODkw
            */

            if(m_State == 0){
                m_State++;
                m_IsCompleted = true;

                HMACMD5 kMd5         = new HMACMD5(Encoding.UTF8.GetBytes(m_Password));
				string  passwordHash = Net_Utils.ToHex(kMd5.ComputeHash(serverResponse)).ToLower();
				
                return Encoding.UTF8.GetBytes(m_UserName + " " + passwordHash);
            }
            else{
                throw new InvalidOperationException("Authentication is completed.");
            }
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets if the authentication exchange has completed.
        /// </summary>
        public override bool IsCompleted
        {
            get{ return m_IsCompleted; }
        }

        /// <summary>
        /// Returns always "LOGIN".
        /// </summary>
        public override string Name
        {
            get { return "CRAM-MD5"; }
        }

        /// <summary>
        /// Gets user login name.
        /// </summary>
        public override string UserName
        {
            get{ return m_UserName; }
        }

        #endregion
    }
}
