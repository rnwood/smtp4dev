using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// Implements SIP Status-Line. Defined in RFC 3261.
    /// </summary>
    public class SIP_StatusLine
    {
        private string m_Version    = "";
        private int    m_StatusCode = 0;
        private string m_Reason     = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="statusCode">Status code.</param>
        /// <param name="reason">Reason text.</param>
        /// <exception cref="ArgumentException">Is raised when </exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>reason</b> is null reference.</exception>
        public SIP_StatusLine(int statusCode,string reason)
        {
            if(statusCode < 100 || statusCode > 699){
                throw new ArgumentException("Argument 'statusCode' value must be >= 100 and <= 699.");
            }
            if(reason == null){
                throw new ArgumentNullException("reason");
            }

            m_Version    = "SIP/2.0";
            m_StatusCode = statusCode;
            m_Reason     = reason;
        }


        #region override method ToString

        /// <summary>
        /// Returns Status-Line string.
        /// </summary>
        /// <returns>Returns Status-Line string.</returns>
        public override string ToString()
        {
            // RFC 3261 25. 
            //  Status-Line = SIP-Version SP Status-Code SP Reason-Phrase CRLF

            return m_Version + " " + m_StatusCode + " " + m_Reason + "\r\n";
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets or sets SIP version.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when <b>value</b> has invalid value.</exception>
        public string Version
        {
            get{ return m_Version; }

            set{
                if(value == null){
                    throw new ArgumentNullException("Version");
                }
                if(value == ""){
                    throw new ArgumentException("Property 'Version' value must be specified.");
                }

                m_Version = value;
            }
        }

        /// <summary>
        /// Gets or sets status code.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when <b>value</b> has invalid value.</exception>
        public int StatusCode
        {
            get{ return m_StatusCode; }

            set{
                if(value < 100 || value > 699){
                    throw new ArgumentException("Argument 'statusCode' value must be >= 100 and <= 699.");
                }

                m_StatusCode = value;
            }
        }

        /// <summary>
        /// Gets or sets reason phrase.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        public string Reason
        {
            get{ return m_Reason; }

            set{
                if(Reason == null){
                    throw new ArgumentNullException("Reason");
                }

                m_Reason = value;
            }
        }

        #endregion

    }
}
