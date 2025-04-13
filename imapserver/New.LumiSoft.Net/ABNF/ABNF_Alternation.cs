using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.ABNF
{
    /// <summary>
    /// This class represent ABNF "alternation". Defined in RFC 5234 4.
    /// </summary>
    public class ABNF_Alternation
    {
        private List<ABNF_Concatenation> m_pItems = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ABNF_Alternation()
        {
            m_pItems = new List<ABNF_Concatenation>();
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ABNF_Alternation Parse(System.IO.StringReader reader)
        {
            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // alternation = concatenation *(*c-wsp "/" *c-wsp concatenation)

            ABNF_Alternation retVal = new ABNF_Alternation();

            while(true){
                ABNF_Concatenation item = ABNF_Concatenation.Parse(reader);
                if(item != null){
                    retVal.m_pItems.Add(item);
                }

                // We reached end of string.
                if(reader.Peek() == -1){
                    break;
                }
                // We have next alternation item.
                else if(reader.Peek() == '/'){
                    reader.Read();
                }
                // We have unexpected value, probably alternation ends.
                else{
                    break;
                }
            }                      

            return retVal;
        }


        #region Properties implementation

        /// <summary>
        /// Gets alternation items.
        /// </summary>
        public List<ABNF_Concatenation> Items
        {
            get{ return m_pItems; }
        }

        #endregion
    }
}
