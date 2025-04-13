using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "event-type" value. Defined in RFC 3265.
    /// </summary>
    public class SIP_t_EventType : SIP_t_Value
    {
        private string m_EventType = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        public SIP_t_EventType()
        {
        }


        #region method Parse

        /// <summary>
        /// Parses "event-type" from specified value.
        /// </summary>
        /// <param name="value">SIP "event-type" value.</param>
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
        /// Parses "event-type" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // Get Method
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("Invalid 'event-type' value, event-type is missing !");
            }
            m_EventType = word;
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "event-type" value.
        /// </summary>
        /// <returns>Returns "event-type" value.</returns>
        public override string ToStringValue()
        {
            return m_EventType;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets event type.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null value passed as value.</exception>
        public string EventType
        {
            get{ return m_EventType; }

            set{
                if(value == null){
                    throw new ArgumentNullException("EventType");
                }

                m_EventType = value;
            }
        }

        #endregion

    }
}
