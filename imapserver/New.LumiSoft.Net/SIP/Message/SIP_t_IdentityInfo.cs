using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "Identity-Info" value. Defined in RFC 4474.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 4474 Syntax:
    ///     Identity-Info        = ident-info *( SEMI ident-info-params )
    ///     ident-info           = LAQUOT absoluteURI RAQUOT
    ///     ident-info-params    = ident-info-alg / ident-info-extension
    ///     ident-info-alg       = "alg" EQUAL token
    ///     ident-info-extension = generic-param
    /// </code>
    /// </remarks>
    public class SIP_t_IdentityInfo : SIP_t_ValueWithParams
    {
        private string m_Uri = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">SIP 'Identity-Info' value.</param>
        public SIP_t_IdentityInfo(string value)
        {
            Parse(value);
        }


        #region method Parse
        
        /// <summary>
        /// Parses "Identity-Info" from specified value.
        /// </summary>
        /// <param name="value">SIP "Identity-Info" value.</param>
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
        /// Parses "Identity-Info" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                Identity-Info        = ident-info *( SEMI ident-info-params )
                ident-info           = LAQUOT absoluteURI RAQUOT
                ident-info-params    = ident-info-alg / ident-info-extension
                ident-info-alg       = "alg" EQUAL token
                ident-info-extension = generic-param
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // absoluteURI
            try{
                string word = reader.ReadParenthesized();
                if(word == null){
                    throw new SIP_ParseException("Invalid Identity-Info 'absoluteURI' value !");
                }
                m_Uri = word;
            }
            catch{
                throw new SIP_ParseException("Invalid Identity-Info 'absoluteURI' value !");
            }
            
            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "Identity-Info" value.
        /// </summary>
        /// <returns>Returns "Identity-Info" value.</returns>
        public override string ToStringValue()
        {
            /*
                Identity-Info        = ident-info *( SEMI ident-info-params )
                ident-info           = LAQUOT absoluteURI RAQUOT
                ident-info-params    = ident-info-alg / ident-info-extension
                ident-info-alg       = "alg" EQUAL token
                ident-info-extension = generic-param
            */

            StringBuilder retVal = new StringBuilder();
            
            // absoluteURI
            retVal.Append("<" + m_Uri + ">");

            // Add parameters
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets URI value.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null value is passed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid 'absoluteURI' value is passed.</exception>
        public string Uri
        {
            get{ return m_Uri; }

            set{
                if(value == null){
                    throw new ArgumentNullException("Uri");
                }
                if(value == ""){
                    throw new ArgumentException("Invalid Identity-Info 'absoluteURI' value !");
                }

                m_Uri = value;
            }
        }

        /// <summary>
        /// Gets or sets 'alg' parameter value. Value null means not specified.
        /// </summary>
        public string Alg
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["alg"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                }
            }

            set{                
                if(string.IsNullOrEmpty(value)){
                    this.Parameters.Remove("alg");
                }
                else{
                    this.Parameters.Set("alg",value);
                }
            }
        }

        #endregion

    }
}
