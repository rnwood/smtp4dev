using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.AUTH
{
    /// <summary>
    /// This class represents SASL DIGEST-MD5 authentication <b>digest-challenge</b>. Defined in RFC 2831.
    /// </summary>
    public class AUTH_SASL_DigestMD5_Challenge
    {
        private string[] m_Realm      = null;
        private string   m_Nonce      = null;
        private string[] m_QopOptions = null;
        private bool     m_Stale      = false;
        private int      m_Maxbuf     = 0;
        private string   m_Charset    = null;
        private string   m_Algorithm  = null;
        private string   m_CipherOpts = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="realm">Realm value.</param>
        /// <param name="nonce">Nonce value.</param>
        /// <param name="qopOptions">Quality of protections supported. Normally this is "auth".</param>
        /// <param name="stale">Stale value.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>realm</b>,<b>nonce</b> or <b>qopOptions</b> is null reference.</exception>
        public AUTH_SASL_DigestMD5_Challenge(string[] realm,string nonce,string[] qopOptions,bool stale)
        {
            if(realm == null){
                throw new ArgumentNullException("realm");
            }
            if(nonce == null){
                throw new ArgumentNullException("nonce");
            }
            if(qopOptions == null){
                throw new ArgumentNullException("qopOptions");
            }

            m_Realm      = realm;
            m_Nonce      = nonce;
            m_QopOptions = qopOptions;
            m_Stale      = stale;
            m_Charset    = "utf-8";
            m_Algorithm  = "md5-sess";
        }

        /// <summary>
        /// Internal parse constructor.
        /// </summary>
        private AUTH_SASL_DigestMD5_Challenge()
        {
        }


        #region static method Parse

        /// <summary>
        /// Parses DIGEST-MD5 challenge from challenge-string.
        /// </summary>
        /// <param name="challenge">Challenge string.</param>
        /// <returns>Returns DIGEST-MD5 challenge.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>challenge</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when challenge parsing + validation fails.</exception>
        public static AUTH_SASL_DigestMD5_Challenge Parse(string challenge)
        {
            if(challenge == null){
                throw new ArgumentNullException("challenge");
            }

            AUTH_SASL_DigestMD5_Challenge retVal = new AUTH_SASL_DigestMD5_Challenge();

            string[] parameters = TextUtils.SplitQuotedString(challenge,',');
            foreach(string parameter in parameters){
                string[] name_value = parameter.Split(new char[]{'='},2);
                string   name       = name_value[0].Trim();

                if(name_value.Length == 2){
                    if(name.ToLower() == "realm"){
                        retVal.m_Realm = TextUtils.UnQuoteString(name_value[1]).Split(',');
                    }
                    else if(name.ToLower() == "nonce"){
                        retVal.m_Nonce = TextUtils.UnQuoteString(name_value[1]);
                    }
                    else if(name.ToLower() == "qop"){
                        retVal.m_QopOptions = TextUtils.UnQuoteString(name_value[1]).Split(',');
                    }
                    else if(name.ToLower() == "stale"){
                        retVal.m_Stale = Convert.ToBoolean(TextUtils.UnQuoteString(name_value[1]));
                    }
                    else if(name.ToLower() == "maxbuf"){
                        retVal.m_Maxbuf = Convert.ToInt32(TextUtils.UnQuoteString(name_value[1]));
                    }
                    else if(name.ToLower() == "charset"){
                        retVal.m_Charset = TextUtils.UnQuoteString(name_value[1]);
                    }
                    else if(name.ToLower() == "algorithm"){
                        retVal.m_Algorithm = TextUtils.UnQuoteString(name_value[1]);
                    }
                    else if(name.ToLower() == "cipher-opts"){
                        retVal.m_CipherOpts = TextUtils.UnQuoteString(name_value[1]);
                    }
                    //else if(name.ToLower() == "auth-param"){
                    //    retVal.m_AuthParam = TextUtils.UnQuoteString(name_value[1]);
                    //}
                }
            }

            /* Validate required fields.
                Per RFC 2831 2.1.1. Only [nonce algorithm] parameters are required.
            */
            if(string.IsNullOrEmpty(retVal.Nonce)){
                throw new ParseException("The challenge-string doesn't contain required parameter 'nonce' value.");
            }
            if(string.IsNullOrEmpty(retVal.Algorithm)){
                throw new ParseException("The challenge-string doesn't contain required parameter 'algorithm' value.");
            }

            return retVal;
        }

        #endregion


        #region method ToChallenge

        /// <summary>
        /// Returns DIGEST-MD5 "digest-challenge" string.
        /// </summary>
        /// <returns>Returns DIGEST-MD5 "digest-challenge" string.</returns>
        public string ToChallenge()
        {
            /* RFC 2831 2.1.1.
                The server starts by sending a challenge. The data encoded in the
                challenge contains a string formatted according to the rules for a
                "digest-challenge" defined as follows:

                digest-challenge  =
                                    1#( realm | nonce | qop-options | stale | maxbuf | charset
                                        algorithm | cipher-opts | auth-param )

                realm             = "realm" "=" <"> realm-value <">
                realm-value       = qdstr-val
                nonce             = "nonce" "=" <"> nonce-value <">
                nonce-value       = qdstr-val
                qop-options       = "qop" "=" <"> qop-list <">
                qop-list          = 1#qop-value
                qop-value         = "auth" | "auth-int" | "auth-conf" | token
                stale             = "stale" "=" "true"
                maxbuf            = "maxbuf" "=" maxbuf-value
                maxbuf-value      = 1*DIGIT
                charset           = "charset" "=" "utf-8"
                algorithm         = "algorithm" "=" "md5-sess"
                cipher-opts       = "cipher" "=" <"> 1#cipher-value <">
                cipher-value      = "3des" | "des" | "rc4-40" | "rc4" | "rc4-56" | token
                auth-param        = token "=" ( token | quoted-string )
            */

            StringBuilder retVal = new StringBuilder();
            retVal.Append("realm=\"" + Net_Utils.ArrayToString(this.Realm,",") + "\"");
            retVal.Append(",nonce=\"" + this.Nonce + "\"");
            if(this.QopOptions != null){
                retVal.Append(",qop=\"" + Net_Utils.ArrayToString(this.QopOptions,",") + "\"");
            }
            if(this.Stale){
                retVal.Append(",stale=true");
            }
            if(this.Maxbuf > 0){
                retVal.Append(",maxbuf=" + this.Maxbuf);
            }
            if(!string.IsNullOrEmpty(this.Charset)){
                retVal.Append(",charset=" + this.Charset);
            }
            retVal.Append(",algorithm=" + this.Algorithm);
            if(!string.IsNullOrEmpty(this.CipherOpts)){
                retVal.Append(",cipher-opts=\"" + this.CipherOpts + "\"");
            }
            //if(!string.IsNullOrEmpty(this.AuthParam)){
            //    retVal.Append("auth-param=\"" + this.AuthParam + "\"");
            //}

            return retVal.ToString();
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets realm value. For more info see RFC 2831.
        /// </summary>
        public string[] Realm
        {
            get{ return m_Realm; }
        }

        /// <summary>
        /// Gets nonce value. For more info see RFC 2831.
        /// </summary>
        public string Nonce
        {
            get{ return m_Nonce; }
        }

        /// <summary>
        /// Gets qop-options value. For more info see RFC 2831.
        /// </summary>
        public string[] QopOptions
        {
            get{ return m_QopOptions; }
        }

        /// <summary>
        /// Gets if stale value. For more info see RFC 2831.
        /// </summary>
        public bool Stale
        {
            get{ return m_Stale; }
        }

        /// <summary>
        /// Gets maxbuf value. For more info see RFC 2831.
        /// </summary>
        public int Maxbuf
        {
            get{ return m_Maxbuf; }
        }

        /// <summary>
        /// Gets charset value. For more info see RFC 2831.
        /// </summary>
        public string Charset
        {
            get{ return m_Charset; }
        }

        /// <summary>
        /// Gets algorithm value. For more info see RFC 2831.
        /// </summary>
        public string Algorithm
        {
            get{ return m_Algorithm; }
        }

        /// <summary>
        /// Gets cipher-opts value. For more info see RFC 2831.
        /// </summary>
        public string CipherOpts
        {
            get{ return m_CipherOpts; }
        }

        #endregion
    }
}
