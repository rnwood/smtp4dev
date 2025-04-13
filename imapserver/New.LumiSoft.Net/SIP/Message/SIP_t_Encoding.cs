using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements "encoding" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     encoding       = codings *(SEMI accept-param)
    ///     codings        = content-coding / "*"
    ///     content-coding = token
    ///     accept-param   = ("q" EQUAL qvalue) / generic-param
    /// </code>
    /// </remarks>
    public class SIP_t_Encoding : SIP_t_ValueWithParams
    {
        private string m_ContentEncoding = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_Encoding()
        {
        }


        #region method Parse

        /// <summary>
        /// Parses "encoding" from specified value.
        /// </summary>
        /// <param name="value">Accept-Encoding value.</param>
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
        /// Parses "encoding" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /* 
                encoding       = codings *(SEMI accept-param)
                codings        = content-coding / "*"
                content-coding = token
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }
            
            // Parse content-coding
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Invalid 'encoding' value is missing !");
            }
            m_ContentEncoding = word;

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "encoding" value.
        /// </summary>
        /// <returns>Returns "encoding" value.</returns>
        public override string ToStringValue()
        {
            /* 
                Accept-Encoding  =  "Accept-Encoding" HCOLON [ encoding *(COMMA encoding) ]
                encoding         =  codings *(SEMI accept-param)
                codings          =  content-coding / "*"
                content-coding   =  token
            */

            StringBuilder retVal = new StringBuilder();           
            retVal.Append(m_ContentEncoding);
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets content encoding. Value *(STAR) means all content encodings.
        /// </summary>
        public string ContentEncoding
        {
            get{ return m_ContentEncoding; }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property ContentEncoding value can't be null or empty !");
                }
                if(!TextUtils.IsToken(value)){
                    throw new ArgumentException("Property ContentEncoding value may be 'token' only !");
                }

                m_ContentEncoding = value;
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
