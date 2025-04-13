using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net
{
    /// <summary>
    /// Implements absolute-URI. Defined in RFC 3986.4.3.
    /// </summary>
    public class AbsoluteUri
    {
        private string m_Scheme = "";
        private string m_Value  = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal AbsoluteUri()
        {
        }
        

        #region static method Parse

        /// <summary>
        /// Parse URI from string value.
        /// </summary>
        /// <param name="value">String URI value.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when <b>value</b> has invalid URI value.</exception>
        public static AbsoluteUri Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }
            if(value == ""){
                throw new ArgumentException("Argument 'value' value must be specified.");
            }
            
            string[] scheme_value = value.Split(new char[]{':'},2);
            if(scheme_value[0].ToLower() == UriSchemes.sip || scheme_value[0].ToLower() == UriSchemes.sips){
                SIP_Uri uri = new SIP_Uri();
                uri.ParseInternal(value);

                return uri;
            }
            else{
                AbsoluteUri uri = new AbsoluteUri();
                uri.ParseInternal(value);

                return uri;
            }
        }

        #endregion


        #region virtual ParseInternal

        /// <summary>
        /// Parses URI from the specified string.
        /// </summary>
        /// <param name="value">URI string.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        protected virtual void ParseInternal(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            string[] scheme_value = value.Split(new char[]{':'},1);
            m_Scheme = scheme_value[0].ToLower();
            if(scheme_value.Length == 2){
                m_Value = scheme_value[1];
            }
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Converts URI to string.
        /// </summary>
        /// <returns>Returns URI as string.</returns>
        public override string ToString()
        {
            return m_Scheme + ":" + m_Value;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets URI scheme.
        /// </summary>
        public virtual string Scheme
        {
            get{ return m_Scheme; }
        }

        /// <summary>
        /// Gets URI value after scheme.
        /// </summary>
        public string Value
        {
            get{ return ToString().Split(new char[]{':'},2)[1]; }
        }

        #endregion

    }
}
