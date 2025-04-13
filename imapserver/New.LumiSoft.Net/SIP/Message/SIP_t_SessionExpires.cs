using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "Session-Expires" value. Defined in RFC 4028.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 4028 Syntax:
    ///     Session-Expires  = delta-seconds *(SEMI se-params)
    ///     se-params        = refresher-param / generic-param
    ///     refresher-param  = "refresher" EQUAL  ("uas" / "uac")
    /// </code>
    /// </remarks>
    public class SIP_t_SessionExpires : SIP_t_ValueWithParams
    {
        private int m_Expires = 90;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">Session-Expires value.</param>
        public SIP_t_SessionExpires(string value)
        {
            Parse(value);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="expires">Specifies after many seconds session expires.</param>
        /// <param name="refresher">Specifies session refresher(uac/uas/null). Value null means not specified.</param>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public SIP_t_SessionExpires(int expires,string refresher)
        {
            if(m_Expires < 90){
                throw new ArgumentException("Argument 'expires' value must be >= 90 !");
            }

            m_Expires = expires;
            this.Refresher = refresher;
        }
        

        #region method Parse
        
        /// <summary>
        /// Parses "Session-Expires" from specified value.
        /// </summary>
        /// <param name="value">SIP "Session-Expires" value.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>value</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public void Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            Parse(new StringReader(value));
        }

        /// <summary>
        /// Parses "Session-Expires" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                Session-Expires  = delta-seconds *(SEMI se-params)
                se-params        = refresher-param / generic-param
                refresher-param  = "refresher" EQUAL  ("uas" / "uac")      
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // delta-seconds
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Session-Expires delta-seconds value is missing !");
            }
            try{
                m_Expires = Convert.ToInt32(word);
            }
            catch{
                throw new SIP_ParseException("Invalid Session-Expires delta-seconds value !");
            }

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "Session-Expires" value.
        /// </summary>
        /// <returns>Returns "Session-Expires" value.</returns>
        public override string ToStringValue()
        {
            /*
                Session-Expires  = delta-seconds *(SEMI se-params)
                se-params        = refresher-param / generic-param
                refresher-param  = "refresher" EQUAL  ("uas" / "uac")      
            */

            StringBuilder retVal = new StringBuilder();
            
            // delta-seconds
            retVal.Append(m_Expires.ToString());

            // Add parameters
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets after how many seconds session expires.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when value less than 90 is passed.</exception>
        public int Expires
        {
            get{ return m_Expires; }

            set{
                if(m_Expires < 90){
                    throw new ArgumentException("Property Expires value must be >= 90 !");
                }

                m_Expires = value;
            }
        }

        /// <summary>
        /// Gets or sets Session-Expires 'refresher' parameter. Normally this value is 'ua' or 'uas'.
        /// Value null means not specified.
        /// </summary>
        public string Refresher
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["refresher"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(value == null){
                    this.Parameters.Remove("refresher");
                }
                else{
                    this.Parameters.Set("refresher",value);
                }
            }
        }

        #endregion

    }
}
