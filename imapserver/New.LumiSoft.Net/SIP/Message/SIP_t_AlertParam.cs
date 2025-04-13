using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "alert-param" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     alert-param = LAQUOT absoluteURI RAQUOT *( SEMI generic-param )
    /// </code>
    /// </remarks>
    public class SIP_t_AlertParam : SIP_t_ValueWithParams
    {
        private string m_Uri = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_AlertParam()
        {
        }


        #region method Parse

        /// <summary>
        /// Parses "alert-param" from specified value.
        /// </summary>
        /// <param name="value">SIP "alert-param" value.</param>
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
        /// Parses "alert-param" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /* 
                alert-param = LAQUOT absoluteURI RAQUOT *( SEMI generic-param )
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
            m_Uri = reader.ReadParenthesized();

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "alert-param" value.
        /// </summary>
        /// <returns>Returns "alert-param" value.</returns>
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
        /// Gets or sets uri value.
        /// </summary>
        public string Uri
        {
            get{ return m_Uri; }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property Uri value can't be null or empty !");
                }

                m_Uri = value;
            }
        }

        #endregion

    }
}
