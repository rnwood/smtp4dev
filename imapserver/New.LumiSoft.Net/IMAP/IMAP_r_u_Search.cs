using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP SEARCH response. Defined in RFC 3501 7.2.5.
    /// </summary>
    public class IMAP_r_u_Search : IMAP_r_u
    {
        private int[] m_pValues = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="values">Search maching messages seqNo/UID(Depeneds on UID SEARCH) list.</param>
        public IMAP_r_u_Search(int[] values)
        {
            if(values == null){
                throw new ArgumentNullException("values");
            }

            m_pValues = values;
        }


        #region static method Parse

        /// <summary>
        /// Parses SEARCH response from exists-response string.
        /// </summary>
        /// <param name="response">Exists response string.</param>
        /// <returns>Returns parsed search response.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        public static IMAP_r_u_Search Parse(string response)
        {
            if(response == null){
                throw new ArgumentNullException("response");
            }

            /* RFC 3501 7.2.5.  SEARCH Response
                Contents:   zero or more numbers

                The SEARCH response occurs as a result of a SEARCH or UID SEARCH
                command.  The number(s) refer to those messages that match the
                search criteria.  For SEARCH, these are message sequence numbers;
                for UID SEARCH, these are unique identifiers.  Each number is
                delimited by a space.

                Example:    S: * SEARCH 2 3 6
            */

            List<int> values = new List<int>();
            if(response.Split(' ').Length > 2){
                foreach(string value in response.Split(new char[]{' '},3)[2].Split(' ')){
                    values.Add(Convert.ToInt32(value));
                }
            }

            return new IMAP_r_u_Search(values.ToArray());
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            // Example:    S: * SEARCH 2 3 6

            StringBuilder retVal = new StringBuilder();
            retVal.Append("* SEARCH");
            foreach(int i in m_pValues){
                retVal.Append(" " + i.ToString());
            }
            retVal.Append("\r\n");

            return retVal.ToString();
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets search matching messages seqNo/UID(Depeneds on UID SEARCH) list.
        /// </summary>
        public int[] Values
        {
            get{ return m_pValues; }
        }

        #endregion
    }
}
