using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.ABNF
{
    /// <summary>
    /// This class represent ABNF "group". Defined in RFC 5234 4.
    /// </summary>
    public class ABFN_Group : ABNF_Element
    {
        private ABNF_Alternation m_pAlternation = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public ABFN_Group()
        {
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ABFN_Group Parse(System.IO.StringReader reader)
        {
            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // group = "(" *c-wsp alternation *c-wsp ")"

            if(reader.Peek() != '('){
                throw new ParseException("Invalid ABNF 'group' value '" + reader.ReadToEnd() + "'.");
            }

            // Eat "(".
            reader.Read();

            // TODO: *c-wsp

            ABFN_Group retVal = new ABFN_Group();

            // We reached end of stream, no closing ")".
            if(reader.Peek() == -1){
                throw new ParseException("Invalid ABNF 'group' value '" + reader.ReadToEnd() + "'.");
            }
         
            retVal.m_pAlternation = ABNF_Alternation.Parse(reader);

            // We don't have closing ")".
            if(reader.Peek() != ')'){
                throw new ParseException("Invalid ABNF 'group' value '" + reader.ReadToEnd() + "'."); 
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
