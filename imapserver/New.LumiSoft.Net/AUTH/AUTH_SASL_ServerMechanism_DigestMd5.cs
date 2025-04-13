using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.AUTH
{
    /// <summary>
    /// Implements "DIGEST-MD5" authenticaiton. Defined in RFC 2831.
    /// </summary>
    public class AUTH_SASL_ServerMechanism_DigestMd5 : AUTH_SASL_ServerMechanism
    {
        private bool   m_IsCompleted     = false;
        private bool   m_IsAuthenticated = false;
        private bool   m_RequireSSL      = false;
        private string m_Realm           = "";
        private string m_Nonce           = "";
        private string m_UserName        = "";
        private int    m_State           = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="requireSSL">Specifies if this mechanism is available to SSL connections only.</param>
        public AUTH_SASL_ServerMechanism_DigestMd5(bool requireSSL)
        {
            m_RequireSSL = requireSSL;

            m_Nonce = Auth_HttpDigest.CreateNonce();
        }


        #region override method Reset

        /// <summary>
        /// Resets any authentication state data.
        /// </summary>
        public override void Reset()
        {
            m_IsCompleted     = false;
            m_IsAuthenticated = false;
            m_UserName        = "";
            m_State           = 0;
        }

        #endregion

        #region override method Continue

        /// <summary>
        /// Continues authentication process.
        /// </summary>
        /// <param name="clientResponse">Client sent SASL response.</param>
        /// <returns>Retunrns challange response what must be sent to client or null if authentication has completed.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>clientResponse</b> is null reference.</exception>
        public override byte[] Continue(byte[] clientResponse)
        {
            if(clientResponse == null){
                throw new ArgumentNullException("clientResponse");
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
                
                AUTH_SASL_DigestMD5_Challenge callenge = new AUTH_SASL_DigestMD5_Challenge(new string[]{m_Realm},m_Nonce,new string[]{"auth"},false);
     
                return Encoding.UTF8.GetBytes(callenge.ToChallenge());
            }
            else if(m_State == 1){
                m_State++;

                try{
                    AUTH_SASL_DigestMD5_Response response = AUTH_SASL_DigestMD5_Response.Parse(Encoding.UTF8.GetString(clientResponse));

                    // Check realm and nonce value.
                    if(m_Realm != response.Realm || m_Nonce != response.Nonce){
                        return Encoding.UTF8.GetBytes("rspauth=\"\"");
                    }

                    m_UserName = response.UserName;
                    AUTH_e_UserInfo result = OnGetUserInfo(response.UserName);
                    if(result.UserExists){            
                        if(response.Authenticate(result.UserName,result.Password)){
                            m_IsAuthenticated = true;

                            return Encoding.UTF8.GetBytes(response.ToRspauthResponse(result.UserName,result.Password));
                        }
                    }
                }
                catch{
                    // Authentication failed, just reject request.
                }

                return Encoding.UTF8.GetBytes("rspauth=\"\"");
            }
            else{
                m_IsCompleted = true;
            }

            return null;
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
        /// Gets if user has authenticated sucessfully.
        /// </summary>
        public override bool IsAuthenticated
        {
            get{ return m_IsAuthenticated; }
        }

        /// <summary>
        /// Returns always "DIGEST-MD5".
        /// </summary>
        public override string Name
        {
            get { return "DIGEST-MD5"; }
        }

        /// <summary>
        /// Gets if specified SASL mechanism is available only to SSL connection.
        /// </summary>
        public override bool RequireSSL
        {
            get{ return m_RequireSSL; }
        }

        /// <summary>
        /// Gets or sets realm value.
        /// </summary>
        /// <remarks>Normally this is host or domain name.</remarks>
        public string Realm
        {
            get{ return m_Realm; }

            set{
                if(value == null){
                    value = "";
                }
                m_Realm = value;
            }
        }

        /// <summary>
        /// Gets user login name.
        /// </summary>
        public override string UserName
        {
            get{ return m_UserName; }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// Is called when authentication mechanism needs to get user info to complete atuhentication.
        /// </summary>
        public event EventHandler<AUTH_e_UserInfo> GetUserInfo = null;

        #region method OnGetUserInfo

        /// <summary>
        /// Raises <b>GetUserInfo</b> event.
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <returns>Returns specified user info.</returns>
        private AUTH_e_UserInfo OnGetUserInfo(string userName)
        {
            AUTH_e_UserInfo retVal = new AUTH_e_UserInfo(userName);

            if(this.GetUserInfo != null){
                this.GetUserInfo(this,retVal);
            }

            return retVal;
        }

        #endregion

        #endregion
    }
}
