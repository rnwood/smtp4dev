using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.AUTH
{
    /// <summary>
    /// This class represents SASL DIGEST-MD5 authentication <b>digest-response</b>. Defined in RFC 2831.
    /// </summary>
    public class AUTH_SASL_DigestMD5_Response
    {
        private AUTH_SASL_DigestMD5_Challenge m_pChallenge = null;
        private string                        m_UserName   = null;
        private string                        m_Password   = null;
        private string                        m_Realm      = null;
        private string                        m_Nonce      = null;
        private string                        m_Cnonce     = null;
        private int                           m_NonceCount = 0;
        private string                        m_Qop        = null;
        private string                        m_DigestUri  = null;
        private string                        m_Response   = null;
        private string                        m_Charset    = null;
        private string                        m_Cipher     = null;
        private string                        m_Authzid    = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="challenge">Client challenge.</param>
        /// <param name="realm">Realm value. This must be one value of the challenge Realm.</param>
        /// <param name="userName">User name.</param>
        /// <param name="password">User password.</param>
        /// <param name="cnonce">Client nonce value.</param>
        /// <param name="nonceCount">Nonce count. One-based client authentication attempt number. Normally this value is 1.</param>
        /// <param name="qop">Indicates what "quality of protection" the client accepted. This must be one value of the challenge QopOptions.</param>
        /// <param name="digestUri">Digest URI.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>challenge</b>,<b>realm</b>,<b>password</b>,<b>nonce</b>,<b>qop</b> or <b>digestUri</b> is null reference.</exception>
        public AUTH_SASL_DigestMD5_Response(AUTH_SASL_DigestMD5_Challenge challenge,string realm,string userName,string password,string cnonce,int nonceCount,string qop,string digestUri)
        {    
            if(challenge == null){
                throw new ArgumentNullException("challenge");
            }
            if(realm == null){
                throw new ArgumentNullException("realm");
            }
            if(userName == null){
                throw new ArgumentNullException("userName");
            }
            if(password == null){
                throw new ArgumentNullException("password");
            }
            if(cnonce == null){
                throw new ArgumentNullException("cnonce");
            }
            if(qop == null){
                throw new ArgumentNullException("qop");
            }
            if(digestUri == null){
                throw new ArgumentNullException("digestUri");
            }

            m_pChallenge = challenge;
            m_Realm      = realm;
            m_UserName   = userName;
            m_Password   = password;
            m_Nonce      = m_pChallenge.Nonce;
            m_Cnonce     = cnonce;
            m_NonceCount = nonceCount;
            m_Qop        = qop;
            m_DigestUri  = digestUri;                        
            m_Response   = CalculateResponse(userName,password);
            m_Charset    = challenge.Charset;
        }

        /// <summary>
        /// Internal parse constructor.
        /// </summary>
        private AUTH_SASL_DigestMD5_Response()
        {
        }


        #region static method Parse

        /// <summary>
        /// Parses DIGEST-MD5 response from response-string.
        /// </summary>
        /// <param name="digestResponse">Response string.</param>
        /// <returns>Returns DIGEST-MD5 response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>digestResponse</b> isnull reference.</exception>
        /// <exception cref="ParseException">Is raised when response parsing + validation fails.</exception>
        public static AUTH_SASL_DigestMD5_Response Parse(string digestResponse)
        {
            if(digestResponse == null){
                throw new ArgumentNullException(digestResponse);
            }

            /* RFC 2831 2.1.2.
                The client makes note of the "digest-challenge" and then responds
                with a string formatted and computed according to the rules for a
                "digest-response" defined as follows:

                digest-response  = 1#( username | realm | nonce | cnonce |
                                       nonce-count | qop | digest-uri | response |
                                       maxbuf | charset | cipher | authzid |
                                       auth-param )

                username         = "username" "=" <"> username-value <">
                username-value   = qdstr-val
                cnonce           = "cnonce" "=" <"> cnonce-value <">
                cnonce-value     = qdstr-val
                nonce-count      = "nc" "=" nc-value
                nc-value         = 8LHEX
                qop              = "qop" "=" qop-value
                digest-uri       = "digest-uri" "=" <"> digest-uri-value <">
                digest-uri-value  = serv-type "/" host [ "/" serv-name ]
                serv-type        = 1*ALPHA
                host             = 1*( ALPHA | DIGIT | "-" | "." )
                serv-name        = host
                response         = "response" "=" response-value
                response-value   = 32LHEX
                LHEX             = "0" | "1" | "2" | "3" |
                                   "4" | "5" | "6" | "7" |
                                   "8" | "9" | "a" | "b" |
                                   "c" | "d" | "e" | "f"
                cipher           = "cipher" "=" cipher-value
                authzid          = "authzid" "=" <"> authzid-value <">
                authzid-value    = qdstr-val
            */    
            
            AUTH_SASL_DigestMD5_Response retVal = new AUTH_SASL_DigestMD5_Response();

            // Set default values.
            retVal.m_Realm = "";

            string[] parameters = TextUtils.SplitQuotedString(digestResponse,',');
            foreach(string parameter in parameters){
                string[] name_value = parameter.Split(new char[]{'='},2);
                string   name       = name_value[0].Trim();

                if(name_value.Length == 2){
                    if(name.ToLower() == "username"){
                        retVal.m_UserName = TextUtils.UnQuoteString(name_value[1]);
                    }
                    else if(name.ToLower() == "realm"){
                        retVal.m_Realm = TextUtils.UnQuoteString(name_value[1]);
                    }
                    else if(name.ToLower() == "nonce"){            
                        retVal.m_Nonce = TextUtils.UnQuoteString(name_value[1]);
                    }
                    else if(name.ToLower() == "cnonce"){
                        retVal.m_Cnonce = TextUtils.UnQuoteString(name_value[1]);
                    }
                    else if(name.ToLower() == "nc"){
                        retVal.m_NonceCount = Int32.Parse(TextUtils.UnQuoteString(name_value[1]),System.Globalization.NumberStyles.HexNumber);
                    }
                    else if(name.ToLower() == "qop"){
                        retVal.m_Qop = TextUtils.UnQuoteString(name_value[1]);
                    }
                    else if(name.ToLower() == "digest-uri"){
                        retVal.m_DigestUri = TextUtils.UnQuoteString(name_value[1]);
                    }
                    else if(name.ToLower() == "response"){
                        retVal.m_Response = TextUtils.UnQuoteString(name_value[1]);
                    }
                    else if(name.ToLower() == "charset"){
                        retVal.m_Charset = TextUtils.UnQuoteString(name_value[1]);
                    }
                    else if(name.ToLower() == "cipher"){
                        retVal.m_Cipher = TextUtils.UnQuoteString(name_value[1]);
                    }
                    else if(name.ToLower() == "authzid"){
                        retVal.m_Authzid = TextUtils.UnQuoteString(name_value[1]);
                    }
                }
            }

            /* Validate required fields.
                Per RFC 2831 2.1.2. Only [username nonce cnonce nc response] parameters are required.
            */
            if(string.IsNullOrEmpty(retVal.UserName)){
                throw new ParseException("The response-string doesn't contain required parameter 'username' value.");
            }
            if(string.IsNullOrEmpty(retVal.Nonce)){
                throw new ParseException("The response-string doesn't contain required parameter 'nonce' value.");
            }
            if(string.IsNullOrEmpty(retVal.Cnonce)){
                throw new ParseException("The response-string doesn't contain required parameter 'cnonce' value.");
            }            
            if(retVal.NonceCount < 1){
                throw new ParseException("The response-string doesn't contain required parameter 'nc' value.");
            }
            if(string.IsNullOrEmpty(retVal.Response)){
                throw new ParseException("The response-string doesn't contain required parameter 'response' value.");
            }

            return retVal;
        }

        #endregion


        #region method Authenticate

        /// <summary>
        /// Authenticates user.
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>Returns true if user authenticated, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>userName</b> or <b>password</b> is null reference.</exception>
        public bool Authenticate(string userName,string password)
        {
            if(userName == null){
                throw new ArgumentNullException("userName");
            }
            if(password == null){
                throw new ArgumentNullException("password");
            }

            if(this.Response == CalculateResponse(userName,password)){
                return true;
            }

            return false;
        }

        #endregion


        #region method ToResponse

        /// <summary>
        /// Creates digest response for challenge.
        /// </summary>
        /// <returns>Returns digest response.</returns>
        public string ToResponse()
        {
            /* RFC 2831 2.1.2.
                The client makes note of the "digest-challenge" and then responds
                with a string formatted and computed according to the rules for a
                "digest-response" defined as follows:

                digest-response  = 1#( username | realm | nonce | cnonce |
                                       nonce-count | qop | digest-uri | response |
                                       maxbuf | charset | cipher | authzid |
                                       auth-param )

                username         = "username" "=" <"> username-value <">
                username-value   = qdstr-val
                cnonce           = "cnonce" "=" <"> cnonce-value <">
                cnonce-value     = qdstr-val
                nonce-count      = "nc" "=" nc-value
                nc-value         = 8LHEX
                qop              = "qop" "=" qop-value
                digest-uri       = "digest-uri" "=" <"> digest-uri-value <">
                digest-uri-value  = serv-type "/" host [ "/" serv-name ]
                serv-type        = 1*ALPHA
                host             = 1*( ALPHA | DIGIT | "-" | "." )
                serv-name        = host
                response         = "response" "=" response-value
                response-value   = 32LHEX
                LHEX             = "0" | "1" | "2" | "3" |
                                   "4" | "5" | "6" | "7" |
                                   "8" | "9" | "a" | "b" |
                                   "c" | "d" | "e" | "f"
                cipher           = "cipher" "=" cipher-value
                authzid          = "authzid" "=" <"> authzid-value <">
                authzid-value    = qdstr-val
            */

            StringBuilder retVal = new StringBuilder();
            retVal.Append("username=\"" + this.UserName + "\"");
            retVal.Append(",realm=\"" + this.Realm + "\"");
            retVal.Append(",nonce=\"" + this.Nonce + "\"");
            retVal.Append(",cnonce=\"" + this.Cnonce + "\"");
            retVal.Append(",nc=" + this.NonceCount.ToString("x8"));
            retVal.Append(",qop=" + this.Qop);
            retVal.Append(",digest-uri=\"" + this.DigestUri + "\"");
            retVal.Append(",response=" + this.Response);
            if(!string.IsNullOrEmpty(this.Charset)){
                retVal.Append(",charset=" + this.Charset);
            }
            if(!string.IsNullOrEmpty(this.Cipher)){
                retVal.Append(",cipher=\"" + this.Cipher + "\"");
            }
            if(!string.IsNullOrEmpty(this.Authzid)){
                retVal.Append(",authzid=\"" + this.Authzid + "\"");
            }
            // auth-param

            return retVal.ToString();
        }

        #endregion

        #region method ToRspauthResponse

        /// <summary>
        /// Creates <b>response-auth</b> response for client.
        /// </summary>
        /// <returns>Returns <b>response-auth</b> response.</returns>
        public string ToRspauthResponse(string userName,string password)
        {
            /* RFC 2831 2.1.3.
                The server receives and validates the "digest-response". The server
                checks that the nonce-count is "00000001". If it supports subsequent
                authentication (see section 2.2), it saves the value of the nonce and
                the nonce-count. It sends a message formatted as follows:

                    response-auth = "rspauth" "=" response-value

                where response-value is calculated as above, using the values sent in
                step two, except that if qop is "auth", then A2 is

                    A2 = { ":", digest-uri-value }

                And if qop is "auth-int" or "auth-conf" then A2 is

                    A2 = { ":", digest-uri-value, ":00000000000000000000000000000000" }

                Compared to its use in HTTP, the following Digest directives in the
                "digest-response" are unused:

                    nextnonce
                    qop
                    cnonce
                    nonce-count
             
                response-value  =
                    HEX( KD ( HEX(H(A1)),
                        { nonce-value, ":" nc-value, ":", cnonce-value, ":", qop-value, ":", HEX(H(A2)) }))
            */

            byte[] a2 = null;
            if(string.IsNullOrEmpty(this.Qop) || this.Qop.ToLower() == "auth"){
                a2 = Encoding.UTF8.GetBytes(":" + this.DigestUri);
            }
            else if(this.Qop.ToLower() == "auth-int" || this.Qop.ToLower() == "auth-conf"){
                a2 = Encoding.UTF8.GetBytes(":" + this.DigestUri + ":00000000000000000000000000000000");
            }            

            if(this.Qop.ToLower() == "auth"){
                // RFC 2831 2.1.2.1.
                // response-value = HEX(KD(HEX(H(A1)),{nonce-value,":" nc-value,":",cnonce-value,":",qop-value,":",HEX(H(A2))}))

                return "rspauth=" + hex(kd(hex(h(a1(userName,password))),m_Nonce + ":" + this.NonceCount.ToString("x8") + ":" + this.Cnonce + ":" + this.Qop + ":" + hex(h(a2))));
            }
            else{
                throw new ArgumentException("Invalid 'qop' value '" + this.Qop + "'.");
            }            
        }

        #endregion


        #region method CalculateResponse

        /// <summary>
        /// Calculates digest response.
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>Returns digest response.</returns>
        private string CalculateResponse(string userName,string password)
        {
            /* RFC 2831.2.1.2.1.
                The definition of "response-value" above indicates the encoding for
                its value -- 32 lower case hex characters. The following definitions
                show how the value is computed.

                Although qop-value and components of digest-uri-value may be
                case-insensitive, the case which the client supplies in step two is
                preserved for the purpose of computing and verifying the
                response-value.

                response-value  =
                    HEX( KD ( HEX(H(A1)),
                        { nonce-value, ":" nc-value, ":", cnonce-value, ":", qop-value, ":", HEX(H(A2)) }))

                If authzid is specified, then A1 is

                    A1 = { H( { username-value, ":", realm-value, ":", passwd } ),
                        ":", nonce-value, ":", cnonce-value, ":", authzid-value }

                If authzid is not specified, then A1 is

                    A1 = { H( { username-value, ":", realm-value, ":", passwd } ),
                        ":", nonce-value, ":", cnonce-value }

                The "username-value", "realm-value" and "passwd" are encoded
                according to the value of the "charset" directive. If "charset=UTF-8"
                is present, and all the characters of either "username-value" or
                "passwd" are in the ISO 8859-1 character set, then it must be
                converted to ISO 8859-1 before being hashed. This is so that
                authentication databases that store the hashed username, realm and
                password (which is common) can be shared compatibly with HTTP, which
                specifies ISO 8859-1. A sample implementation of this conversion is
                in section 8.

                If the "qop" directive's value is "auth", then A2 is:

                    A2       = { "AUTHENTICATE:", digest-uri-value }

                If the "qop" value is "auth-int" or "auth-conf" then A2 is:

                    A2       = { "AUTHENTICATE:", digest-uri-value,
                                ":00000000000000000000000000000000" }

                Note that "AUTHENTICATE:" must be in upper case, and the second
                string constant is a string with a colon followed by 32 zeros.

                These apparently strange values of A2 are for compatibility with
                HTTP; they were arrived at by setting "Method" to "AUTHENTICATE" and
                the hash of the entity body to zero in the HTTP digest calculation of
                A2.

                Also, in the HTTP usage of Digest, several directives in the

                "digest-challenge" sent by the server have to be returned by the
                client in the "digest-response". These are:

                    opaque
                    algorithm

                These directives are not needed when Digest is used as a SASL
                mechanism (i.e., MUST NOT be sent, and MUST be ignored if received).
            */
                        
            if(string.IsNullOrEmpty(this.Qop) || this.Qop.ToLower() == "auth"){
                // RFC 2831 2.1.2.1.
                // response-value = HEX(KD(HEX(H(A1)),{nonce-value,":" nc-value,":",cnonce-value,":",qop-value,":",HEX(H(A2))}))

                return hex(kd(hex(h(a1(userName,password))),m_Nonce + ":" + this.NonceCount.ToString("x8") + ":" + this.Cnonce + ":" + this.Qop + ":" + hex(h(a2()))));
            }
            else{
                throw new ArgumentException("Invalid 'qop' value '" + this.Qop + "'.");
            }
        }

        #endregion

        #region method a1

        /// <summary>
        /// Calculates A1 value.
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>Returns A1 value.</returns>
        private byte[] a1(string userName,string password)
        {   
            /* RFC 2831 2.1.2.1.
                If authzid is specified, then A1 is

                A1 = { H( { username-value, ":", realm-value, ":", passwd } ),
                      ":", nonce-value, ":", cnonce-value, ":", authzid-value }

                If authzid is not specified, then A1 is

                A1 = { H( { username-value, ":", realm-value, ":", passwd } ),
                      ":", nonce-value, ":", cnonce-value
             
                NOTE: HTTP MD5 RFC 2617 supports more algorithms. SASL requires md5-sess.
            */
  
            if(string.IsNullOrEmpty(this.Authzid)){
                byte[] user_realm_pwd = h(Encoding.UTF8.GetBytes(userName + ":" + this.Realm + ":" + password));
                byte[] nonce_cnonce   = Encoding.UTF8.GetBytes(":" + m_Nonce + ":" + this.Cnonce);

                byte[] retVal = new byte[user_realm_pwd.Length + nonce_cnonce.Length];
                Array.Copy(user_realm_pwd,0,retVal,0,user_realm_pwd.Length);
                Array.Copy(nonce_cnonce,0,retVal,user_realm_pwd.Length,nonce_cnonce.Length);

                return retVal;
            }
            else{
                byte[] user_realm_pwd       = h(Encoding.UTF8.GetBytes(userName + ":" + this.Realm + ":" + password));
                byte[] nonce_cnonce_authzid = Encoding.UTF8.GetBytes(":" + m_Nonce + ":" + this.Cnonce + ":" + this.Authzid);

                byte[] retVal = new byte[user_realm_pwd.Length + nonce_cnonce_authzid.Length];
                Array.Copy(user_realm_pwd,0,retVal,0,user_realm_pwd.Length);
                Array.Copy(nonce_cnonce_authzid,0,retVal,user_realm_pwd.Length,nonce_cnonce_authzid.Length);

                return retVal;
            }
        }

        #endregion

        #region method a2

        /// <summary>
        /// Calculates A2 value.
        /// </summary>
        /// <returns>Returns A2 value.</returns>
        private byte[] a2()
        {
            /* RFC 2831 2.1.2.1.
                If the "qop" directive's value is "auth", then A2 is:

                    A2       = { "AUTHENTICATE:", digest-uri-value }

                If the "qop" value is "auth-int" or "auth-conf" then A2 is:

                    A2       = { "AUTHENTICATE:", digest-uri-value, ":00000000000000000000000000000000" }

                Note that "AUTHENTICATE:" must be in upper case, and the second
                string constant is a string with a colon followed by 32 zeros.
             
                RFC 2617(HTTP MD5) 3.2.2.3.
                    A2       = Method ":" digest-uri-value ":" H(entity-body)

                NOTE: In SASL entity-body hash always "00000000000000000000000000000000".
            */

            if(string.IsNullOrEmpty(this.Qop) || this.Qop.ToLower() == "auth"){
                return Encoding.UTF8.GetBytes("AUTHENTICATE:" + this.DigestUri);
            }
            else if(this.Qop.ToLower() == "auth-int" || this.Qop.ToLower() == "auth-conf"){
                return Encoding.UTF8.GetBytes("AUTHENTICATE:" + this.DigestUri + ":00000000000000000000000000000000");
            }
            else{
                throw new ArgumentException("Invalid 'qop' value '" + this.Qop + "'.");
            }
        }

        #endregion

        #region method h

        /// <summary>
        /// Computes MD5 hash.
        /// </summary>
        /// <param name="value">Value to process.</param>
        /// <returns>Return MD5 hash.</returns>
        private byte[] h(byte[] value)
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();			
			
            return md5.ComputeHash(value);
        }

        #endregion

        #region method kd

        private byte[] kd(string secret,string data)
        {
            // KD(secret, data) = H(concat(secret, ":", data))

            return h(Encoding.UTF8.GetBytes(secret + ":" + data));
        }

        #endregion

        #region method hex

        /// <summary>
        /// Converts value to hex string.
        /// </summary>
        /// <param name="value">Value to convert.</param>
        /// <returns>Returns hex string.</returns>
        private string hex(byte[] value)
        {
            return Net_Utils.ToHex(value);
        }

        #endregion


        #region Properties implementation
                
        /// <summary>
        /// Gets user name.
        /// </summary>
        public string UserName
        {
            get{ return m_UserName; }
        }

        /// <summary>
        /// Gets realm(domain) name.
        /// </summary>
        public string Realm
        {
            get{ return m_Realm; }
        }

        /// <summary>
        /// Gets nonce value.
        /// </summary>
        public string Nonce
        {
            get{ return m_Nonce; }
        }

        /// <summary>
        /// Gets cnonce value.
        /// </summary>
        public string Cnonce
        {
            get{ return m_Cnonce; }
        }

        /// <summary>
        /// Gets nonce count.
        /// </summary>
        public int NonceCount
        {
            get{ return m_NonceCount; }
        }

        /// <summary>
        /// Gets "quality of protection" value.
        /// </summary>
        public string Qop
        {
            get{ return m_Qop; }
        }

        /// <summary>
        /// Gets digest URI value.
        /// </summary>
        public string DigestUri
        {
            get{ return m_DigestUri; }
        }

        /// <summary>
        /// Gets response value.
        /// </summary>
        public string Response
        {
            get{ return m_Response; }
        }

        /// <summary>
        /// Gets charset value.
        /// </summary>
        public string Charset
        {
            get{ return m_Charset; }
        }

        /// <summary>
        /// Gets cipher value.
        /// </summary>
        public string Cipher
        {
            get{ return m_Cipher; }
        }

        /// <summary>
        /// Gets authorization ID.
        /// </summary>
        public string Authzid
        {
            get{ return m_Authzid; }
        }

        #endregion
    }
}
