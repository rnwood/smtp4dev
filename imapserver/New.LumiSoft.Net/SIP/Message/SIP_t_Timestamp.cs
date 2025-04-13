using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "Timestamp" value. Defined in RFC 3261.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3261 Syntax:
    ///     Timestamp = 1*(DIGIT) [ "." *(DIGIT) ] [ LWS delay ]
    ///         delay = *(DIGIT) [ "." *(DIGIT) ]
    /// </code>
    /// </remarks>
    public class SIP_t_Timestamp : SIP_t_Value
    {
        private decimal m_Time  = 0;
        private decimal m_Delay = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">Timestamp: header field value.</param>
        public SIP_t_Timestamp(string value)
        {
            Parse(new StringReader(value));
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="time">Time in seconds when request was sent.</param>
        /// <param name="delay">Delay time in seconds.</param>
        public SIP_t_Timestamp(decimal time,decimal delay)
        {
            m_Time  = time;
            m_Delay = delay;
        }
        
        
        #region method Parse

        /// <summary>
        /// Parses "Timestamp" from specified value.
        /// </summary>
        /// <param name="value">SIP "Timestamp" value.</param>
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
        /// Parses "Timestamp" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /* RFC 3261.
                Timestamp =  "Timestamp" HCOLON 1*(DIGIT) [ "." *(DIGIT) ] [ LWS delay ]
                    delay =  *(DIGIT) [ "." *(DIGIT) ]
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // Get time
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Invalid 'Timestamp' value, time is missing !");
            }
            m_Time = Convert.ToDecimal(word);

            // Get optional delay
            word = reader.ReadWord();
            if(word != null){
                m_Delay = Convert.ToDecimal(word);
            }
            else{
                m_Delay = 0;
            }
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "Timestamp" value.
        /// </summary>
        /// <returns>Returns "accept-range" value.</returns>
        public override string ToStringValue()
        {
            /* RFC 3261.
                Timestamp =  "Timestamp" HCOLON 1*(DIGIT) [ "." *(DIGIT) ] [ LWS delay ]
                    delay =  *(DIGIT) [ "." *(DIGIT) ]
            */

            if(m_Delay > 0){
                return m_Time.ToString() + " " + m_Delay.ToString();
            }
            else{
                return m_Time.ToString();
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets time in seconds when request was sent.
        /// </summary>
        public decimal Time
        {
            get{ return m_Time; }

            set{ m_Time = value; }
        }

        /// <summary>
        /// Gets or sets delay time in seconds. Delay specifies the time between the UAS received 
        /// the request and generated response.
        /// </summary>
        public decimal Delay
        {
            get{ return m_Delay; }

            set{ m_Delay = value; }
        }

        #endregion

    }
}
