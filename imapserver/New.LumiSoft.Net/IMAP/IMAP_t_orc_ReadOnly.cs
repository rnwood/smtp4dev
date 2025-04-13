using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This is class represents IMAP server <b>READ-ONLY</b> optional response code. Defined in RFC 3501 7.1.
    /// </summary>
    public class IMAP_t_orc_ReadOnly : IMAP_t_orc
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public IMAP_t_orc_ReadOnly()
        {
        }


        #region static method Parse

        /// <summary>
        /// Parses READ-ONLY optional response from string.
        /// </summary>
        /// <param name="value">READ-ONLY optional response string.</param>
        /// <returns>Returns READ-ONLY optional response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        public new static IMAP_t_orc_ReadOnly Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            string[] code_value = value.Split(new char[]{' '},2);
            if(!string.Equals("READ-ONLY",code_value[0],StringComparison.InvariantCultureIgnoreCase)){
                throw new ArgumentException("Invalid READ-ONLY response value.","value");
            }

            return new IMAP_t_orc_ReadOnly();
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "READ-ONLY";
        }

        #endregion
    }
}
