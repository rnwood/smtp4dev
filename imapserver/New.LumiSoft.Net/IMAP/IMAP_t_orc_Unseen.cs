using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This is class represents IMAP server <b>UNSEEN</b> optional response code. Defined in RFC 3501 7.1.
    /// </summary>
    public class IMAP_t_orc_Unseen : IMAP_t_orc
    {
        private int m_FirstUnseen = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="firstUnseen">First unseen message sequence number.</param>
        public IMAP_t_orc_Unseen(int firstUnseen)
        {
            m_FirstUnseen = firstUnseen;
        }


        #region static method Parse

        /// <summary>
        /// Parses UNSEEN optional response from string.
        /// </summary>
        /// <param name="value">UNSEEN optional response string.</param>
        /// <returns>Returns UNSEEN optional response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        public new static IMAP_t_orc_Unseen Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            string[] code_value = value.Split(new char[]{' '},2);
            if(!string.Equals("UNSEEN",code_value[0],StringComparison.InvariantCultureIgnoreCase)){
                throw new ArgumentException("Invalid UNSEEN response value.","value");
            }
            if(code_value.Length != 2){
                throw new ArgumentException("Invalid UNSEEN response value.","value");
            }

            return new IMAP_t_orc_Unseen(Convert.ToInt32(code_value[1]));
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "UNSEEN " + m_FirstUnseen;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets first unseen message sequence number.
        /// </summary>
        public int SeqNo
        {
            get{ return m_FirstUnseen; }
        }

        #endregion
    }
}
