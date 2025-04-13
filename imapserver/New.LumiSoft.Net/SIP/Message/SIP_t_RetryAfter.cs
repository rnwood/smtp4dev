using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "Retry-After" value. Defined in rfc 3261.
    /// Retry after specifies how many seconds the service is expected to be unavailable to the requesting client.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     Retry-After = delta-seconds [ comment ] *( SEMI retry-param )
    ///     retry-param = ("duration" EQUAL delta-seconds) / generic-param
    /// </code>
    /// </remarks>
    public class SIP_t_RetryAfter : SIP_t_ValueWithParams
    {
        private int m_Time = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">SIP Retry-After value.</param>
        public SIP_t_RetryAfter(string value)
        {
            Parse(value);
        }


        #region method Parse
        
        /// <summary>
        /// Parses "Retry-After" from specified value.
        /// </summary>
        /// <param name="value">SIP "Retry-After" value.</param>
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
        /// Parses "Retry-After" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                Retry-After = delta-seconds [ comment ] *( SEMI retry-param )
                retry-param = ("duration" EQUAL delta-seconds) / generic-param
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // delta-seconds
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("SIP Retry-After 'delta-seconds' value is missing !");
            }
            try{
                m_Time = Convert.ToInt32(word);
            }
            catch{
                throw new SIP_ParseException("Invalid SIP Retry-After 'delta-seconds' value !");
            }

            // Parse parameters
            ParseParameters(reader);
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "Retry-After" value.
        /// </summary>
        /// <returns>Returns "Retry-After" value.</returns>
        public override string ToStringValue()
        {
            /*
                Retry-After = delta-seconds [ comment ] *( SEMI retry-param )
                retry-param = ("duration" EQUAL delta-seconds) / generic-param
            */

            StringBuilder retVal = new StringBuilder();
            
            // delta-seconds
            retVal.Append(m_Time);

            // Add parameters
            retVal.Append(ParametersToString());

            return retVal.ToString();
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets how many seconds the service is expected to be unavailable to the requesting client.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when when value less than 1 is passed.</exception>
        public int Time
        {
            get{ return m_Time; }

            set{
                if(value < 1){
                    throw new ArgumentException("Property Time value must be >= 1 !");
                }

                m_Time = value;
            }
        }

        /// <summary>
        /// Gets or sets 'duration' parameter value. The 'duration' parameter indicates how long the 
        /// called party will be reachable starting at the initial time of availability. If no duration 
        /// parameter is given, the service is assumed to be available indefinitely. Value -1 means not specified.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when when value less than 1 is passed.</exception>
        public int Duration
        {
            get{ 
                SIP_Parameter parameter = this.Parameters["duration"];
                if(parameter != null){
                    return Convert.ToInt32(parameter.Value);
                }
                else{
                    return -1;
                }
            }

            set{                
                if(value == -1){
                    this.Parameters.Remove("duration");
                }
                else{
                    if(value < 1){
                        throw new ArgumentException("Property Duration value must be >= 1 !");
                    }

                    this.Parameters.Set("duration",value.ToString());
                }
            }
        }

        #endregion

    }
}
