using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Message
{
    /// <summary>
    /// Implements SIP "RAck" value. Defined in RFC 3262.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 3262 Syntax:
    ///     RAck         = response-num LWS CSeq-num LWS Method
    ///     response-num = 1*DIGIT
    ///     CSeq-num     = 1*DIGIT
    /// </code>
    /// </remarks>
    public class SIP_t_RAck : SIP_t_Value
    {
        private int    m_ResponseNumber = 1;
        private int    m_CSeqNumber     = 1;
        private string m_Method         = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">RAck value.</param>
        public SIP_t_RAck(string value)
        {
            Parse(value);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="responseNo">Response number.</param>
        /// <param name="cseqNo">CSeq number.</param>
        /// <param name="method">Request method.</param>
        public SIP_t_RAck(int responseNo,int cseqNo,string method)
        {
            this.ResponseNumber = responseNo;
            this.CSeqNumber     = cseqNo;
            this.Method         = method;
        }


        #region method Parse

        /// <summary>
        /// Parses "RAck" from specified value.
        /// </summary>
        /// <param name="value">SIP "RAck" value.</param>
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
        /// Parses "RAck" from specified reader.
        /// </summary>
        /// <param name="reader">Reader from where to parse.</param>
        /// <exception cref="ArgumentNullException">Raised when <b>reader</b> is null.</exception>
        /// <exception cref="SIP_ParseException">Raised when invalid SIP message.</exception>
        public override void Parse(StringReader reader)
        {
            /*
                RAck         = response-num LWS CSeq-num LWS Method
                response-num = 1*DIGIT
                CSeq-num     = 1*DIGIT
            */

            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // response-num
            string word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("RAck response-num value is missing !");
            }
            try{
                m_ResponseNumber = Convert.ToInt32(word);
            }
            catch{
                throw new SIP_ParseException("Invalid RAck response-num value !");
            }

            // CSeq-num
            word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("RAck CSeq-num value is missing !");
            }
            try{
                m_CSeqNumber = Convert.ToInt32(word);
            }            
            catch{
                throw new SIP_ParseException("Invalid RAck CSeq-num value !");
            }

            // Get request method
            word = reader.ReadWord();
            if(word == null){
                throw new SIP_ParseException("RAck Method value is missing !");
            }
            m_Method = word;
        }

        #endregion

        #region method ToStringValue

        /// <summary>
        /// Converts this to valid "RAck" value.
        /// </summary>
        /// <returns>Returns "RAck" value.</returns>
        public override string ToStringValue()
        {
            /*
                RAck         = response-num LWS CSeq-num LWS Method
                response-num = 1*DIGIT
                CSeq-num     = 1*DIGIT
            */

            return m_ResponseNumber + " " + m_CSeqNumber + " " + m_Method;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets response number.
        /// </summary>
        public int ResponseNumber
        {
            get{ return m_ResponseNumber; }

            set{
                if(value < 1){
                    throw new ArgumentException("ResponseNumber value must be >= 1 !");
                }

                m_ResponseNumber = value;
            }
        }

        /// <summary>
        /// Gets or sets CSeq number.
        /// </summary>
        public int CSeqNumber
        {
            get{ return m_CSeqNumber; }

            set{
                if(value < 1){
                    throw new ArgumentException("CSeqNumber value must be >= 1 !");
                }

                m_CSeqNumber = value;
            }
        }

        /// <summary>
        /// Gets or sets method.
        /// </summary>
        public string Method
        {
            get{ return m_Method; }

            set{
                if(value == null){
                    throw new ArgumentNullException("Method");
                }

                m_Method = value;
            }
        }

        #endregion

    }
}
