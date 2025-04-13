using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP.Client
{
    /// <summary>
    /// IMAP client exception.
    /// </summary>
    public class IMAP_ClientException : Exception
    {
        private IMAP_r_ServerStatus m_pResponse = null;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="response">IMAP server response.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        public IMAP_ClientException(IMAP_r_ServerStatus response) : base(response.ToString())
        {
            if(response == null){
                throw new ArgumentNullException("response");
            }

            m_pResponse = response;            
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="responseLine">IMAP server response line.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>responseLine</b> is null.</exception>
        public IMAP_ClientException(string responseLine) : base(responseLine)
        {
            if(responseLine == null){
                throw new ArgumentNullException("responseLine");
            }

            m_pResponse = IMAP_r_ServerStatus.Parse(responseLine);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="responseCode">IMAP response code(BAD,NO).</param>
        /// <param name="responseText">Response text.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>responseCode</b> or <b>responseText</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_ClientException(string responseCode,string responseText) : base(responseCode + " " + responseText)
        {
            if(responseCode == null){
                throw new ArgumentNullException("responseCode");
            }
            if(responseCode == string .Empty){
                throw new ArgumentException("Argument 'responseCode' value must be specified.","responseCode");
            }
            if(responseText == null){
                throw new ArgumentNullException("responseText");
            }
            if(responseText == string .Empty){
                throw new ArgumentException("Argument 'responseText' value must be specified.","responseText");
            }

            m_pResponse = IMAP_r_ServerStatus.Parse(responseCode + " " + responseText);
        }


        #region Properties Implementation

        /// <summary>
        /// Gets IMAP server response.
        /// </summary>
        public IMAP_r_ServerStatus Response
        {
            get{ return m_pResponse; }
        }

        /// <summary>
        /// Gets IMAP server error status code.
        /// </summary>
        public string StatusCode
        {
            get{ return m_pResponse.ResponseCode; }
        }

        /// <summary>
        /// Gets IMAP server response text after status code.
        /// </summary>
        public string ResponseText
        {
            get{ return m_pResponse.ResponseText; }
        }

        #endregion

    }
}
