using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP server status(OK,NO,BAD) response. Defined in RFC 3501 7.1.
    /// </summary>
    public class IMAP_r_ServerStatus : IMAP_r
    {
        private string     m_CommandTag        = "";
        private string     m_ResponseCode      = "";
        private IMAP_t_orc m_pOptionalResponse = null;
        private string     m_ResponseText      = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="commandTag">Command tag.</param>
        /// <param name="responseCode">Response code.</param>
        /// <param name="responseText">Response text after response-code.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>commandTag</b>,<b>responseCode</b> or <b>responseText</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_r_ServerStatus(string commandTag,string responseCode,string responseText) : this(commandTag,responseCode,null,responseText)
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="commandTag">Command tag.</param>
        /// <param name="responseCode">Response code.</param>
        /// <param name="optionalResponse">Optional response. Value null means not specified.</param>
        /// <param name="responseText">Response text after response-code.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>commandTag</b>,<b>responseCode</b> or <b>responseText</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_r_ServerStatus(string commandTag,string responseCode,IMAP_t_orc optionalResponse,string responseText)
        {
            if(commandTag == null){
                throw new ArgumentNullException("commandTag");
            }
            if(commandTag == string.Empty){
                throw new ArgumentException("The argument 'commandTag' value must be specified.","commandTag");
            }
            if(responseCode == null){
                throw new ArgumentNullException("responseCode");
            }
            if(responseCode == string.Empty){
                throw new ArgumentException("The argument 'responseCode' value must be specified.","responseCode");
            }

            m_CommandTag        = commandTag;
            m_ResponseCode      = responseCode;
            m_pOptionalResponse = optionalResponse;
            m_ResponseText      = responseText;
        }

        /// <summary>
        /// Default cmdTag-less constructor.
        /// </summary>
        /// <param name="responseCode">Response code.</param>
        /// <param name="responseText">Response text after response-code.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>responseCode</b> or <b>responseText</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        internal IMAP_r_ServerStatus(string responseCode,string responseText)
        {
            m_ResponseCode = responseCode;
            m_ResponseText = responseText;
        }


        #region static method Parse

        /// <summary>
        /// Parses IMAP command completion status response from response line.
        /// </summary>
        /// <param name="responseLine">Response line.</param>
        /// <returns>Returns parsed IMAP command completion status response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>responseLine</b> is null reference value.</exception>
        public static IMAP_r_ServerStatus Parse(string responseLine)
        {
            if(responseLine == null){
                throw new ArgumentNullException("responseLine");
            }

            // We continuation "+" response.
            if(responseLine.StartsWith("+")){
                string[] parts        = responseLine.Split(new char[]{' '},2);
                string   responseText = parts.Length == 2 ? parts[1] : null;

                return new IMAP_r_ServerStatus("+","+",responseText);
            }
            // OK/BAD/NO
            else{
                string[]   parts        = responseLine.Split(new char[]{' '},3);
                string     commandTag   = parts[0];
                string     responseCode = parts[1];
                IMAP_t_orc optResponse  = null;
                string   responseText   = parts[2];

                // Optional status code.
                if(parts[2].StartsWith("[")){
                    StringReader r = new StringReader(parts[2]);
                    optResponse  = IMAP_t_orc.Parse(r.ReadParenthesized());
                    responseText = r.ReadToEnd();
                }

                return new IMAP_r_ServerStatus(commandTag,responseCode,optResponse,responseText);
            }
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            StringBuilder retVal = new StringBuilder();
            if(!string.IsNullOrEmpty(m_CommandTag)){
                retVal.Append(m_CommandTag + " ");
            }
            retVal.Append(m_ResponseCode + " ");
            if(m_pOptionalResponse != null){
                retVal.Append("[" + m_pOptionalResponse.ToString() + "] ");
            }
            retVal.Append(m_ResponseText + "\r\n");

            return retVal.ToString();
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets command tag.
        /// </summary>
        public string CommandTag
        {
            get{ return m_CommandTag; }
        }
                
        /// <summary>
        /// Gets IMAP server status response code(OK,NO,BAD).
        /// </summary>
        public string ResponseCode
        {
            get{ return m_ResponseCode; }
        }

        /// <summary>
        /// Gets IMAP server otional response-code. Value null means no optional response.
        /// </summary>
        public IMAP_t_orc OptionalResponse
        {
            get{ return m_pOptionalResponse; }
        }
                
        /// <summary>
        /// Gets response human readable text after response-code.
        /// </summary>
        public string ResponseText
        {
            get{ return m_ResponseText; }
        }

        /// <summary>
        /// Gets if this response is error response.
        /// </summary>
        public bool IsError
        {
            get{ 
                if(m_ResponseCode.Equals("NO",StringComparison.InvariantCultureIgnoreCase)){
                    return true;
                }
                else if(m_ResponseCode.Equals("BAD",StringComparison.InvariantCultureIgnoreCase)){
                    return true;
                }
                else{
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets if this response is continuation response.
        /// </summary>
        public bool IsContinue
        {
            get{ return m_ResponseCode.Equals("+",StringComparison.InvariantCultureIgnoreCase); }
        }

        #endregion


        #region Obsolete

        /// <summary>
        /// Gets IMAP server status response optiona response-code(ALERT,BADCHARSET,CAPABILITY,PARSE,PERMANENTFLAGS,
        /// READ-ONLY,READ-WRITE,TRYCREATE,UIDNEXT,UIDVALIDITY,UNSEEN).
        /// Value null means not specified. For more info see RFC 3501 7.1.
        /// </summary>
        [Obsolete("Use property OptionalResponse instead.")]
        public string OptionalResponseCode
        {
            get{ 
                if(m_pOptionalResponse == null){
                    return null;
                }
                else{
                    return m_pOptionalResponse.ToString().Split(' ')[0];
                }
            }
        }

        /// <summary>
        /// Gets optional response aruments string. Value null means not specified. For more info see RFC 3501 7.1.
        /// </summary>
        [Obsolete("Use property OptionalResponse instead.")]
        public string OptionalResponseArgs
        {
            get{ 
                if(m_pOptionalResponse == null){
                    return null;
                }
                else{
                    string[] code_args = m_pOptionalResponse.ToString().Split(new char[]{' '},2);

                    return code_args.Length == 2 ? code_args[1] : "";
                }
            }
        }

        #endregion
    }
}
