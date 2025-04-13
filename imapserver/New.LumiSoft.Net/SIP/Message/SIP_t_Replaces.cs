using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "Replaces" value. Defined in RFC 3891.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3891 Syntax:
    ///     Replaces        = callid *(SEMI replaces-param)
    ///     replaces-param  = to-tag / from-tag / early-flag / generic-param
    ///     to-tag          = "to-tag" EQUAL token
    ///     from-tag        = "from-tag" EQUAL token
    ///     early-flag      = "early-only"
    /// </code>
    /// </remarks>
    public class SIP_t_Replaces : SIP_t_ValueWithParams
    {
        private string m_CallID = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_Replaces()
        {
        }


        #region method Parse
        
        /// <summary>
        /// Parses "Replaces" from specified value.
        /// </summary>
        /// <param name="value">SIP "Replaces" value.</param>
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
        /// Parses "Replaces" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                Replaces        = callid *(SEMI replaces-param)
                replaces-param  = to-tag / from-tag / early-flag / generic-param
                to-tag          = "to-tag" EQUAL token
                from-tag        = "from-tag" EQUAL token
                early-flag      = "early-only"    
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // callid
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Replaces 'callid' value is missing !");
            }
            m_CallID = word;

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "Replaces" value.
        /// </summary>
        /// <returns>Returns "Replaces" value.</returns>
        public override string ToStringValue()
        {
            /*
                Replaces        = callid *(SEMI replaces-param)
                replaces-param  = to-tag / from-tag / early-flag / generic-param
                to-tag          = "to-tag" EQUAL token
                from-tag        = "from-tag" EQUAL token
                early-flag      = "early-only"    
            */

            StringBuilder retVal = new StringBuilder();
            
            // delta-seconds
            retVal.Append(m_CallID);

            // Add parameters
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets call id.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null value is passed.</exception>
        public string CallID
        {
            get{ return m_CallID; }

            set{
                if(value == null){
                    throw new ArgumentNullException("CallID");
                }

                m_CallID = value;
            }
        }

        /// <summary>
        /// Gets or sets Replaces 'to-tag' parameter. Value null means not specified.
        /// </summary>
        public string ToTag
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["to-tag"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(value == null){
                    this.Parameters.Remove("to-tag");
                }
                else{
                    this.Parameters.Set("to-tag",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets Replaces 'from-tag' parameter. Value null means not specified.
        /// </summary>
        public string FromTag
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["from-tag"];
                if(parameter != null){
                    return parameter.Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(value == null){
                    this.Parameters.Remove("from-tag");
                }
                else{
                    this.Parameters.Set("from-tag",value);
                }
            }
        }

        /// <summary>
        /// Gets or sets Replaces 'early-flag' parameter.
        /// </summary>
        public bool EarlyFlag
        {                                    
            get{ 
                if(this.Parameters.Contains("early-only")){
                    return true;
                }
                else{
                    return false;
                }
            }

            set{
                if(!value){
                    this.Parameters.Remove("early-only");
                }
                else{
                    this.Parameters.Set("early-only",null);
                }
            }
        }

        #endregion

    }
}
