using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "To" value. Defined in RFC 3261.
    /// The To header field specifies the logical recipient of the request.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     To        = ( name-addr / addr-spec ) *( SEMI to-param )
    ///     to-param  = tag-param / generic-param
    ///     tag-param = "tag" EQUAL token
    /// </code>
    /// </remarks>
    public class SIP_t_To : SIP_t_ValueWithParams
    {
        private SIP_t_NameAddress m_pAddress = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">To: header field value.</param>
        public SIP_t_To(string value)
        {
            m_pAddress = new SIP_t_NameAddress();

            Parse(new StringReader(value));
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="address">To address.</param>
        public SIP_t_To(SIP_t_NameAddress address)
        {
            m_pAddress = address;
        }


        #region method Parse

        /// <summary>
        /// Parses "To" from specified value.
        /// </summary>
        /// <param name="value">SIP "To" value.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public void Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("reader");
            }

            Parse(new StringReader(value));
        }

        /// <summary>
        /// Parses "To" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /* To       =  ( name-addr / addr-spec ) *( SEMI to-param )
               to-param =  tag-param / generic-param
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // Parse address
            m_pAddress.Parse(reader);

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "To" value.
        /// </summary>
        /// <returns>Returns "To" value.</returns>
        public override string ToStringValue()
        {
            StringBuilder retVal = new StringBuilder();           
            retVal.Append(m_pAddress.ToStringValue());
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets to address.
        /// </summary>
        public SIP_t_NameAddress Address
        {
            get{ return m_pAddress; }
        }

        /// <summary>
        /// Gets or sets tag parameter value.
        /// The "tag" parameter serves as a general mechanism for dialog identification.
        /// Value null means that 'tag' paramter doesn't exist.
        /// </summary>
        public string Tag
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["tag"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                }
            }

            set{                
                if(string.IsNullOrEmpty(value)){
                    this.Parameters.Remove("tag");
                }
                else{
                    this.Parameters.Set("tag",value);
                }
            }
        }

        #endregion

    }
}
