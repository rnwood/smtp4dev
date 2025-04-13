using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.AUTH
{
    /// <summary>
    /// Implements "DIGEST-MD5" authenticaiton.
    /// </summary>
    public class AUTH_SASL_Client_DigestMd5 : AUTH_SASL_Client
    {
        private bool                         m_IsCompleted = false;
        private int                          m_State       = 0;
        private string                       m_Protocol    = null;
        private string                       m_ServerName  = null;
        private string                       m_UserName    = null;
        private string                       m_Password    = null;
        private AUTH_SASL_DigestMD5_Response m_pResponse   = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="protocol">Protocol name. For example: SMTP.</param>
        /// <param name="server">Remote server name or IP address.</param>
        /// <param name="userName">User login name.</param>
        /// <param name="password">User password.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>protocol</b>,<b>server</b>,<b>userName</b> or <b>password</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public AUTH_SASL_Client_DigestMd5(string protocol,string server,string userName,string password)
        {
            if(protocol == null){
                throw new ArgumentNullException("protocol");
            }
            if(protocol == string.Empty){
                throw new ArgumentException("Argument 'protocol' value must be specified.","userName");
            }
            if(server == null){
                throw new ArgumentNullException("protocol");
            }
            if(server == string.Empty){
                throw new ArgumentException("Argument 'server' value must be specified.","userName");
            }
            if(userName == null){
                throw new ArgumentNullException("userName");
            }
            if(userName == string.Empty){
                throw new ArgumentException("Argument 'username' value must be specified.","userName");
            }
            if(password == null){
                throw new ArgumentNullException("password");
            }

            m_Protocol   = protocol;
            m_ServerName = server;
            m_UserName   = userName;
            m_Password   = password;
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

            /* RFC 2831.
                The base64-decoded version of the SASL exchange is:

                S: realm="elwood.innosoft.com",nonce="OA6MG9tEQGm2hh",qop="auth",
                   algorithm=md5-sess,charset=utf-8
                C: charset=utf-8,username="chris",realm="elwood.innosoft.com",
                   nonce="OA6MG9tEQGm2hh",nc=00000001,cnonce="OA6MHXh6VqTrRk",
                   digest-uri="imap/elwood.innosoft.com",
                   response=d388dad90d4bbd760a152321f2143af7,qop=auth
                S: rspauth=ea40f60335c427b5527b84dbabcdfffd
                C: 
                S: ok

                The password in this example was "secret".
            */

            if(m_State == 0){
                m_State++;

                // Parse server challenge.
                AUTH_SASL_DigestMD5_Challenge challenge = AUTH_SASL_DigestMD5_Challenge.Parse(Encoding.UTF8.GetString(serverResponse));

                // Construct our response to server challenge.
                m_pResponse = new AUTH_SASL_DigestMD5_Response(
                    challenge,
                    challenge.Realm[0],
                    m_UserName,
                    m_Password,
                    Guid.NewGuid().ToString().Replace("-",""),
                    1,
                    challenge.QopOptions[0],
                    m_Protocol + "/" + m_ServerName
                );

                return Encoding.UTF8.GetBytes(m_pResponse.ToResponse());
            }
            else if(m_State == 1){
                m_State++;
                m_IsCompleted = true;

                // Check rspauth value.
                if(!string.Equals(Encoding.UTF8.GetString(serverResponse),m_pResponse.ToRspauthResponse(m_UserName,m_Password),StringComparison.InvariantCultureIgnoreCase)){
                    throw new Exception("Server server 'rspauth' value mismatch with local 'rspauth' value.");
                }

                return new byte[0];
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
        /// Returns always "DIGEST-MD5".
        /// </summary>
        public override string Name
        {
            get { return "DIGEST-MD5"; }
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
