using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using LumiSoft.Net.IMAP.Client;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP SEARCH <b>OLDER (interval)</b> key. Defined in RFC 5032.
    /// </summary>
    /// <remarks>Messages whose internal date (disregarding time and timezone) is more than the specified interval of seconds from the current time.</remarks>
    public class IMAP_Search_Key_Older : IMAP_Search_Key
    {
        private uint m_Interval;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="interval">Interval value in seconds.</param>
        public IMAP_Search_Key_Older(uint interval)
        {
            m_Interval = interval;
        }


        #region static method Parse

        /// <summary>
        /// Returns parsed IMAP SEARCH <b>OLDER (interval)</b> key.
        /// </summary>
        /// <param name="r">String reader.</param>
        /// <returns>Returns parsed IMAP SEARCH <b>OLDER (interval)</b> key.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>r</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when parsing fails.</exception>
        internal static IMAP_Search_Key_Older Parse(StringReader r)
        {
            if(r == null){
                throw new ArgumentNullException("r");
            }

            string word = r.ReadWord();
            if(!string.Equals(word,"OLDER",StringComparison.InvariantCultureIgnoreCase)){
                throw new ParseException("Parse error: Not a SEARCH 'OLDER' key.");
            }
            string value = r.ReadWord();
            if(value == null){
                throw new ParseException("Parse error: Invalid 'OLDER' value.");
            }
            uint interval;
            if(!uint.TryParse(value, out interval)){
                throw new ParseException("Parse error: Invalid 'OLDER' value.");
            }

            return new IMAP_Search_Key_Older(interval);
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            return "OLDER " + m_Interval.ToString();
        }

        #endregion


        #region internal override method ToCmdParts

        /// <summary>
        /// Stores IMAP search-key command parts to the specified array.
        /// </summary>
        /// <param name="list">Array where to store command parts.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>list</b> is null reference.</exception>
        internal override void ToCmdParts(List<IMAP_Client_CmdPart> list)
        {
            if(list == null){
                throw new ArgumentNullException("list");
            }

            list.Add(new IMAP_Client_CmdPart(IMAP_Client_CmdPart_Type.Constant,ToString()));
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets interval value in seconds.
        /// </summary>
        public uint Interval
        {
            get{ return m_Interval; }
        }

        #endregion
    }
}