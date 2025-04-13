using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This is class represents IMAP server <b>UIDNEXT</b> optional response code. Defined in RFC 3501 7.1.
    /// </summary>
    public class IMAP_t_orc_UidNext : IMAP_t_orc
    {
        private int m_UidNext = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="uidNext">Next UID value.</param>
        public IMAP_t_orc_UidNext(int uidNext)
        {
            m_UidNext = uidNext;
        }


        #region static method Parse

        /// <summary>
        /// Parses UIDNEXT optional response from string.
        /// </summary>
        /// <param name="value">UIDNEXT optional response string.</param>
        /// <returns>Returns UIDNEXT optional response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        public new static IMAP_t_orc_UidNext Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            string[] code_value = value.Split(new char[]{' '},2);
            if(!string.Equals("UIDNEXT",code_value[0],StringComparison.InvariantCultureIgnoreCase)){
                throw new ArgumentException("Invalid UIDNEXT response value.","value");
            }
            if(code_value.Length != 2){
                throw new ArgumentException("Invalid UIDNEXT response value.","value");
            }

            return new IMAP_t_orc_UidNext(Convert.ToInt32(code_value[1]));
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "UIDNEXT " + m_UidNext;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets next message predicted UID value.
        /// </summary>
        public int UidNext
        {
            get{ return m_UidNext; }
        }

        #endregion
    }
}
