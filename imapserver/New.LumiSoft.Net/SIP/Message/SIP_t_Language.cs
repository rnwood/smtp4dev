using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "language" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     language       = language-range *(SEMI accept-param)
    ///     language-range = ( ( 1*8ALPHA *( "-" 1*8ALPHA ) ) / "*" )
    ///     accept-param   = ("q" EQUAL qvalue) / generic-param
    /// </code>
    /// </remarks>
    public class SIP_t_Language : SIP_t_ValueWithParams
    {
        private string m_LanguageRange = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_Language()
        {
        }


        #region method Parse

        /// <summary>
        /// Parses "language" from specified value.
        /// </summary>
        /// <param name="value">SIP "language" value.</param>
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
        /// Parses "language" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /* 
                language       = language-range *(SEMI accept-param)
                language-range = ( ( 1*8ALPHA *( "-" 1*8ALPHA ) ) / "*" )
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }
            
            // Parse content-coding
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Invalid Accept-Language value, language-range value is missing !");
            }
            m_LanguageRange = word;

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "language" value.
        /// </summary>
        /// <returns>Restuns "language" value.</returns>
        public override string ToStringValue()
        {
            /* 
                Accept-Language  =  "Accept-Language" HCOLON [ language *(COMMA language) ]
                language         =  language-range *(SEMI accept-param)
                language-range   =  ( ( 1*8ALPHA *( "-" 1*8ALPHA ) ) / "*" )
            */

            StringBuilder retVal = new StringBuilder();           
            retVal.Append(m_LanguageRange);
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets language range. Value *(STAR) means all languages.
        /// </summary>
        public string LanguageRange
        {
            get{ return m_LanguageRange; }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property LanguageRange value can't be null or empty !");
                }

                m_LanguageRange = value;
            }
        }

        /// <summary>
        /// Gets or sets qvalue parameter. Targets are processed from highest qvalue to lowest. 
        /// This value must be between 0.0 and 1.0. Value -1 means that value not specified.
        /// </summary>
        public double QValue
        {
            get{
                SIP_Parameter parameter = this.Parameters["qvalue"];
                if(parameter != null){
                    return Convert.ToDouble(parameter.Value);
                }
                else{
                    return -1;
                }
            }

            set{
                if(value < 0 || value > 1){
                    throw new ArgumentException("Property QValue value must be between 0.0 and 1.0 !");
                }

                if(value < 0){
                    this.Parameters.Remove("qvalue");
                }
                else{
                    this.Parameters.Set("qvalue",value.ToString());
                }
            }
        }

        #endregion

    }
}
