using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This is class represents IMAP server <b>PARSE</b> optional response code. Defined in RFC 3501 7.1.
    /// </summary>
    public class IMAP_t_orc_Parse : IMAP_t_orc
    {
        private string m_ErrorText = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="text">Parse error text.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>text</b> is null reference.</exception>
        public IMAP_t_orc_Parse(string text)
        {
            if(text == null){
                throw new ArgumentNullException("text");
            }

            m_ErrorText = text;
        }


        #region static method Parse

        /// <summary>
        /// Parses PARSE optional response from string.
        /// </summary>
        /// <param name="value">PARSE optional response string.</param>
        /// <returns>Returns PARSE optional response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        public new static IMAP_t_orc_Parse Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            string[] code_value = value.Split(new char[]{' '},2);
            if(!string.Equals("PARSE",code_value[0],StringComparison.InvariantCultureIgnoreCase)){
                throw new ArgumentException("Invalid PARSE response value.","value");
            }

            return new IMAP_t_orc_Parse(code_value.Length == 2 ? code_value[1] : "");
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "PARSE " + m_ErrorText;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets parse error text.
        /// </summary>
        public string ErrorText
        {
            get{ return m_ErrorText; }
        }

        #endregion
    }
}
