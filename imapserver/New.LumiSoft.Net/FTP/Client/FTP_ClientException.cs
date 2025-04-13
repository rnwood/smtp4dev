using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.FTP.Client
{
    /// <summary>
    /// FTP client exception.
    /// </summary>
    public class FTP_ClientException : Exception
    {
        private int    m_StatusCode   = 500;
        private string m_ResponseText = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="responseLine">FTP server response line.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>responseLine</b> is null.</exception>
        public FTP_ClientException(string responseLine) : base(responseLine)
        {
            if(responseLine == null){
                throw new ArgumentNullException("responseLine");
            }

            string[] code_text = responseLine.Split(new char[]{' '},2);
            try{
                m_StatusCode = Convert.ToInt32(code_text[0]);
            }
            catch{
            }
            if(code_text.Length == 2){
                m_ResponseText =  code_text[1];                
            }
        }


        #region Properties Implementation

        /// <summary>
        /// Gets FTP status code.
        /// </summary>
        public int StatusCode
        {
            get{ return m_StatusCode; }
        }

        /// <summary>
        /// Gets FTP server response text after status code.
        /// </summary>
        public string ResponseText
        {
            get{ return m_ResponseText; }
        }

        /// <summary>
        /// Gets if it is permanent FTP(5xx) error.
        /// </summary>
        public bool IsPermanentError
        {
            get{
                if(m_StatusCode >= 500 && m_StatusCode <= 599){
                    return true;
                }
                else{
                    return false;
                }
            }
        }

        #endregion

    }
}
