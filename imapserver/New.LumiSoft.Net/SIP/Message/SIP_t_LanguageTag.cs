using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "language-tag" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     language-tag = primary-tag *( "-" subtag )
    ///     primary-tag  = 1*8ALPHA
    ///     subtag       = 1*8ALPHA
    /// </code>
    /// </remarks>
    public class SIP_t_LanguageTag : SIP_t_ValueWithParams
    {
        private string m_LanguageTag = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_LanguageTag()
        {
        }


        #region method Parse

        /// <summary>
        /// Parses "language-tag" from specified value.
        /// </summary>
        /// <param name="value">SIP "language-tag" value.</param>
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
        /// Parses "language-tag" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /* 
                Content-Language =  "Content-Language" HCOLON language-tag *(COMMA language-tag)
                language-tag     =  primary-tag *( "-" subtag )
                primary-tag      =  1*8ALPHA
                subtag           =  1*8ALPHA
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }
            
            // Parse content-coding
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Invalid Content-Language value, language-tag value is missing !");
            }
            m_LanguageTag = word;

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "language-tag" value.
        /// </summary>
        /// <returns>Returns "language-tag" value.</returns>
        public override string ToStringValue()
        {
            /* 
                Content-Language =  "Content-Language" HCOLON language-tag *(COMMA language-tag)
                language-tag     =  primary-tag *( "-" subtag )
                primary-tag      =  1*8ALPHA
                subtag           =  1*8ALPHA
            */

            StringBuilder retVal = new StringBuilder();           
            retVal.Append(m_LanguageTag);
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets language tag.
        /// </summary>
        public string LanguageTag
        {
            get{ return m_LanguageTag; }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property LanguageTag value can't be null or empty !");
                }

                m_LanguageTag = value;
            }
        }

        #endregion

    }
}
