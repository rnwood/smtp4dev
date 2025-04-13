using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.ABNF
{
    /// <summary>
    /// This class represent ABNF "option". Defined in RFC 5234 4.
    /// </summary>
    public class ABNF_Option : ABNF_Element
    {
        private ABNF_Alternation m_pAlternation = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ABNF_Option()
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ABNF_Option Parse(System.IO.StringReader reader)
        {
            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // option = "[" *c-wsp alternation *c-wsp "]"

            if(reader.Peek() != '['){
                throw new ParseException("Invalid ABNF 'option' value '" + reader.ReadToEnd() + "'.");
            }

            // Eat "[".
            reader.Read();

            // TODO: *c-wsp

            ABNF_Option retVal = new ABNF_Option();

            // We reached end of stream, no closing "]".
            if(reader.Peek() == -1){
                throw new ParseException("Invalid ABNF 'option' value '" + reader.ReadToEnd() + "'.");
            }
         
            retVal.m_pAlternation = ABNF_Alternation.Parse(reader);

            // We don't have closing ")".
            if(reader.Peek() != ']'){
                throw new ParseException("Invalid ABNF 'option' value '" + reader.ReadToEnd() + "'."); 
            }
            else{
                reader.Read();
            }

            return retVal;
        }


        #region Properties implementation

        /// <summary>
        /// Gets option alternation elements.
        /// </summary>
        public ABNF_Alternation Alternation
        {
            get{ return m_pAlternation; }
        }

        #endregion
    }
}
