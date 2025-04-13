using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This is class represents IMAP server <b>READ-WRITE</b> optional response code. Defined in RFC 3501 7.1.
    /// </summary>
    public class IMAP_t_orc_ReadWrite : IMAP_t_orc
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public IMAP_t_orc_ReadWrite()
        {
        }


        #region static method Parse

        /// <summary>
        /// Parses READ-WRITE optional response from string.
        /// </summary>
        /// <param name="value">READ-WRITE optional response string.</param>
        /// <returns>Returns READ-WRITE optional response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        public new static IMAP_t_orc_ReadWrite Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            string[] code_value = value.Split(new char[]{' '},2);
            if(!string.Equals("READ-WRITE",code_value[0],StringComparison.InvariantCultureIgnoreCase)){
                throw new ArgumentException("Invalid READ-WRITE response value.","value");
            }

            return new IMAP_t_orc_ReadWrite();
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "READ-WRITE";
        }

        #endregion
    }
}
