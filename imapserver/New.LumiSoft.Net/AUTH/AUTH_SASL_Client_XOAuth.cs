using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.AUTH
{
    /// <summary>
    /// This class implements <b>XOAUTH</b> authentication.
    /// </summary>
    public class AUTH_SASL_Client_XOAuth : AUTH_SASL_Client
    {
        private bool                              m_IsCompleted           = false;
        private int                               m_State                 = 0;
        private string                            m_UserName              = null;
        private string                            m_RequestUri            = null;
        private KeyValueCollection<string,string> m_pRequestUriParameters = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="userName">User login name.</param>
        /// <param name="requestUri">OAuth request URI.</param>
        /// <param name="requestUriParameters">OAuth request URI parameters.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>userName</b>,<b>requestUri</b> or <b>requestUriParameters</b> is null reference.</exception>
        public AUTH_SASL_Client_XOAuth(string userName,string requestUri,KeyValueCollection<string,string> requestUriParameters)
        {
            if(userName == null){
                throw new ArgumentNullException("userName");
            }
            if(requestUri == null){
                throw new ArgumentNullException("requestUri");
            }
            if(requestUriParameters == null){
                throw new ArgumentNullException("requestUriParameters");
            }

            m_UserName              = userName;
            m_RequestUri            = requestUri;
            m_pRequestUriParameters = requestUriParameters;
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
            if(m_IsCompleted){
                throw new InvalidOperationException("Authentication is completed.");
            }

            if(m_State == 0){
                m_IsCompleted = true;

                StringBuilder retVal = new StringBuilder();
                retVal.Append("GET " + m_RequestUri + " ");
                bool first = true;
                foreach(KeyValuePair<string,string> p in m_pRequestUriParameters){
                    if(first){
                        first = false;

                        retVal.Append(EncodeParamter(p.Key) + "=\"" + EncodeParamter(p.Value) + "\"");
                    }
                    else{
                        retVal.Append("," + EncodeParamter(p.Key) + "=\"" + EncodeParamter(p.Value) + "\"");
                    }
                }

                return Encoding.ASCII.GetBytes(retVal.ToString());
            }

            return null;
        }

        #endregion


        #region method EncodeParamter

        /// <summary>
        /// Encodes paramter name or value.
        /// </summary>
        /// <param name="value">Parameter name or value.</param>
        /// <returns>Returns encoded value.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference value.</exception>
        private string EncodeParamter(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            /* All parameter names and values are escaped using the [RFC3986] percent-encoding (%xx) mechanism. 
               Characters not in the unreserved character set ([RFC3986] section 2.3) MUST be encoded. 
               Characters in the unreserved character set MUST NOT be encoded. Hexadecimal characters in encodings MUST be upper case. 
               Text names and values MUST be encoded as UTF-8 octets before percent-encoding them per [RFC3629]. 
               unreserved = ALPHA, DIGIT, '-', '.', '_', '~'
            */

            byte[] valueUtf8 = Encoding.UTF8.GetBytes(value);

            StringBuilder retVal = new StringBuilder();
            foreach(byte b in valueUtf8){
                // unreserverd
                if((b >= 65 && b <= 90) || (b >= 97 && b <= 122)|| (b >= 48 && b <= 57) || b == '-' || b == '.' || b == '_' || b == '~'){
                    retVal.Append((char)b);
                }
                // Encoding needed.
                else{
                    retVal.Append(b.ToString("X2"));
                }
            }

            return retVal.ToString();
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
            get { return "XOAUTH"; }
        }

        /// <summary>
        /// Gets user login name.
        /// </summary>
        public override string UserName
        {
            get{ return m_UserName; }
        }

        /// <summary>
        /// Returns always true, because XOAUTH authentication method supports SASL client "inital response".
        /// </summary>
        public override bool SupportsInitialResponse
        {
            get{ return true; }
        }

        #endregion
    }
}
