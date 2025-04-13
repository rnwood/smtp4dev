using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This is class represents IMAP server <b>BADCHARSET</b> optional response code. Defined in RFC 3501 7.1.
    /// </summary>
    public class IMAP_t_orc_BadCharset : IMAP_t_orc
    {
        private string[] m_pCharsets = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="charsets">List of supported charsets.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>charsets</b> is null reference.</exception>
        public IMAP_t_orc_BadCharset(string[] charsets)
        {
            if(charsets == null){
                throw new ArgumentNullException("charsets");
            }

            m_pCharsets = charsets;
        }


        #region static method Parse

        /// <summary>
        /// Parses BADCHARSET optional response from string.
        /// </summary>
        /// <param name="value">BADCHARSET optional response string.</param>
        /// <returns>Returns BADCHARSET optional response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        public new static IMAP_t_orc_BadCharset Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            string[] code_value = value.Split(new char[]{' '},2);
            if(!string.Equals("BADCHARSET",code_value[0],StringComparison.InvariantCultureIgnoreCase)){
                throw new ArgumentException("Invalid BADCHARSET response value.","value");
            }

            return new IMAP_t_orc_BadCharset(code_value[1].Trim().Split(' '));
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "BADCHARSET " + Net_Utils.ArrayToString(m_pCharsets," ");
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets list of supported charsets.
        /// </summary>
        public string[] Charsets
        {
            get{ return m_pCharsets; }
        }

        #endregion
    }
}
