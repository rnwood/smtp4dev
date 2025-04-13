using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This is class represents IMAP server <b>CAPABILITY</b> optional response code. Defined in RFC 3501 7.1.
    /// </summary>
    public class IMAP_t_orc_Capability : IMAP_t_orc
    {
        private string[] m_pCapabilities = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="capabilities">List of supported capabilities.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>capabilities</b> is null reference.</exception>
        public IMAP_t_orc_Capability(string[] capabilities)
        {
            if(capabilities == null){
                throw new ArgumentNullException("capabilities");
            }

            m_pCapabilities = capabilities;
        }


        #region static method Parse

        /// <summary>
        /// Parses CAPABILITY optional response from string.
        /// </summary>
        /// <param name="value">CAPABILITY optional response string.</param>
        /// <returns>Returns CAPABILITY optional response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        public new static IMAP_t_orc_Capability Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            string[] code_value = value.Split(new char[]{' '},2);
            if(!string.Equals("CAPABILITY",code_value[0],StringComparison.InvariantCultureIgnoreCase)){
                throw new ArgumentException("Invalid CAPABILITY response value.","value");
            }
            if(code_value.Length != 2){
                throw new ArgumentException("Invalid CAPABILITY response value.","value");
            }

            return new IMAP_t_orc_Capability(code_value[1].Split(' '));
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns></returns>
        public override string ToString()
        {
            return "CAPABILITY (" + Net_Utils.ArrayToString(m_pCapabilities," ") + ")";
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets list of supported capabilities.
        /// </summary>
        public string[] Capabilities
        {
            get{ return m_pCapabilities; }
        }

        #endregion
    }
}
