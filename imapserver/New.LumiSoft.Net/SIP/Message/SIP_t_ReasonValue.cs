using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "reason-value" value. Defined in rfc 3326.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3326 Syntax:
    ///     reason-value      =  protocol *(SEMI reason-params)
    ///     protocol          =  "SIP" / "Q.850" / token
    ///     reason-params     =  protocol-cause / reason-text / reason-extension
    ///     protocol-cause    =  "cause" EQUAL cause
    ///     cause             =  1*DIGIT
    ///     reason-text       =  "text" EQUAL quoted-string
    ///     reason-extension  =  generic-param
    /// </code>
    /// </remarks>
    public class SIP_t_ReasonValue : SIP_t_ValueWithParams
    {
        private string m_Protocol = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_ReasonValue()
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">SIP reason-value value.</param>
        public SIP_t_ReasonValue(string value)
        {
            Parse(value);
        }


        #region method Parse
        
        /// <summary>
        /// Parses "reason-value" from specified value.
        /// </summary>
        /// <param name="value">SIP "reason-value" value.</param>
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
        /// Parses "reason-value" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                reason-value      =  protocol *(SEMI reason-params)
                protocol          =  "SIP" / "Q.850" / token
                reason-params     =  protocol-cause / reason-text / reason-extension
                protocol-cause    =  "cause" EQUAL cause
                cause             =  1*DIGIT
                reason-text       =  "text" EQUAL quoted-string
                reason-extension  =  generic-param
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // protocol
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("SIP reason-value 'protocol' value is missing !");
            }
            m_Protocol = word;

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "reason-value" value.
        /// </summary>
        /// <returns>Returns "reason-value" value.</returns>
        public override string ToStringValue()
        {
            /*
                reason-value      =  protocol *(SEMI reason-params)
                protocol          =  "SIP" / "Q.850" / token
                reason-params     =  protocol-cause / reason-text / reason-extension
                protocol-cause    =  "cause" EQUAL cause
                cause             =  1*DIGIT
                reason-text       =  "text" EQUAL quoted-string
                reason-extension  =  generic-param
            */

            StringBuilder retVal = new StringBuilder();
            
            // Add protocol
            retVal.Append(m_Protocol);

            // Add parameters
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets protocol.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null value is passed.</exception>
        public string Protocol
        {
            get{ return m_Protocol; }

            set{
                if(value == null){
                    throw new ArgumentNullException("Protocol");
                }

                m_Protocol = value;
            }
        }

        /// <summary>
        /// Gets or sets 'cause' parameter value. The cause parameter contains a SIP status code. 
        /// Value -1 means not specified.
        /// </summary>
        public int Cause
        {
            get{
                if(this.Parameters["cause"] == null){
                    return -1;
                }
                else{
                    return Convert.ToInt32(this.Parameters["cause"].Value); 
                }
            }

            set{
                if(value < 0){
                    this.Parameters.Remove("cause");
                }
                else{
                    this.Parameters.Set("cause",value.ToString());
                }
            }
        }

        /// <summary>
        /// Gets or sets 'text' parameter value. Value null means not specified.
        /// </summary>
        public string Text
        {
            get{
                SIP_Parameter parameter = this.Parameters["text"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(value == null){
                    this.Parameters.Remove("text");
                }
                else{
                    this.Parameters.Set("text",value);
                }
            }
        }

        #endregion

    }
}
