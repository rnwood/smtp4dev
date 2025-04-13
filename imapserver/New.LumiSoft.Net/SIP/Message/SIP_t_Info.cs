using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "info" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     info       = LAQUOT absoluteURI RAQUOT *( SEMI info-param)
    ///     info-param = ( "purpose" EQUAL ( "icon" / "info" / "card" / token ) ) / generic-param
    /// </code>
    /// </remarks>
    public class SIP_t_Info : SIP_t_ValueWithParams
    {
        private string m_Uri = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_Info()
        {
        }


        #region method Parse

        /// <summary>
        /// Parses "info" from specified value.
        /// </summary>
        /// <param name="value">SIP "info" value.</param>
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
        /// Parses "info" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                Call-Info  = "Call-Info" HCOLON info *(COMMA info)
                info       = LAQUOT absoluteURI RAQUOT *( SEMI info-param)
                info-param = ( "purpose" EQUAL ( "icon" / "info" / "card" / token ) ) / generic-param
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }
            
            // Parse uri
            // Read to LAQUOT
            reader.QuotedReadToDelimiter('<');
            if(!reader.StartsWith("<")){
                throw new SIP_ParseException("Invalid Alert-Info value, Uri not between <> !");
            }

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "info" value.
        /// </summary>
        /// <returns>Returns "info" value.</returns>
        public override string ToStringValue()
        {
            StringBuilder retVal = new StringBuilder();           
            retVal.Append("<" + m_Uri + ">");
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion
                

        #region Properties Implementation

        /// <summary>
        /// Gets or sets 'purpose' parameter value. Value null means not specified. 
        /// Known values: "icon","info","card".
        /// </summary>
        public string Purpose
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["purpose"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                } 
            }

            set{                
                if(string.IsNullOrEmpty(value)){
                    this.Parameters.Remove("purpose");
                }
                else{
                    this.Parameters.Set("purpose",value);
                }
            }
        }

        #endregion

    }
}
