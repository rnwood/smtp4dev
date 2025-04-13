using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "r-value" value. Defined in RFC 4412.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 4412 Syntax:
    ///     r-value    = namespace "." r-priority
    ///     namespace  = token-nodot
    ///     r-priority = token-nodot
    /// </code>
    /// </remarks>
    public class SIP_t_RValue : SIP_t_Value
    {
        private string m_Namespace = "";
        private string m_Priority  = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_RValue()
        {
        }


        #region method Parse

        /// <summary>
        /// Parses "r-value" from specified value.
        /// </summary>
        /// <param name="value">SIP "r-value" value.</param>
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
        /// Parses "r-value" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                r-value    = namespace "." r-priority
                namespace  = token-nodot
                r-priority = token-nodot
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // namespace "." r-priority
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Invalid 'r-value' value, 'namespace \".\" r-priority' is missing !");
            }
            string[] namespace_priority = word.Split('.');
            if(namespace_priority.Length != 2){
                throw new SIP_ParseException("Invalid r-value !");
            }
            m_Namespace = namespace_priority[0];
            m_Priority  = namespace_priority[1];
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "r-value" value.
        /// </summary>
        /// <returns>Returns "r-value" value.</returns>
        public override string ToStringValue()
        {
            /*
                r-value    = namespace "." r-priority
                namespace  = token-nodot
                r-priority = token-nodot
            */

            return m_Namespace + "." + m_Priority;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets Namespace.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null value passed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid Namespace value passed.</exception>
        public string Namespace
        {
            get{ return m_Namespace; }

            set{
                if(value == null){
                    throw new ArgumentNullException("Namespace");
                }
                if(value == ""){
                    throw new ArgumentException("Property Namespace value may not be '' !");
                }
                if(!TextUtils.IsToken(value)){
                    throw new ArgumentException("Property Namespace value must be 'token' !");
                }

                m_Namespace = value;
            }
        }

        /// <summary>
        /// Gets or sets priority.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null value passed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid Priority value passed.</exception>
        public string Priority
        {
            get{ return m_Priority; }

            set{
                if(value == null){
                    throw new ArgumentNullException("Priority");
                }
                if(value == ""){
                    throw new ArgumentException("Property Priority value may not be '' !");
                }
                if(!TextUtils.IsToken(value)){
                    throw new ArgumentException("Property Priority value must be 'token' !");
                }

                m_Priority = value;
            }
        }

        #endregion

    }
}
