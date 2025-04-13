using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.ABNF
{
    /// <summary>
    /// This class represent ABNF "char-val". Defined in RFC 5234 4.
    /// </summary>
    public class ABNF_CharVal : ABNF_Element
    {
        private string m_Value = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">The prose-val value.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public ABNF_CharVal(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }
            if(!Validate(value)){
                //throw new ArgumentException("Invalid argument 'value' value. Value must be: 'DQUOTE *(%x20-21 / %x23-7E) DQUOTE'.");
            }
            
            m_Value = value;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ABNF_CharVal Parse(System.IO.StringReader reader)
        {
            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            /*
                char-val       =  DQUOTE *(%x20-21 / %x23-7E) DQUOTE
                                ; quoted string of SP and VCHAR
                                ;  without DQUOTE
            */

            if(reader.Peek() != '\"'){
                throw new ParseException("Invalid ABNF 'char-val' value '" + reader.ReadToEnd() + "'.");
            }

            // Eat DQUOTE
            reader.Read();

            // TODO: *c-wsp

            StringBuilder value = new StringBuilder();

            while(true){
                // We reached end of stream, no closing DQUOTE.
                if(reader.Peek() == -1){
                    throw new ParseException("Invalid ABNF 'char-val' value '" + reader.ReadToEnd() + "'.");
                }
                // We have closing DQUOTE.
                else if(reader.Peek() == '\"'){
                    reader.Read();
                    break;
                }
                // Allowed char.
                else if((reader.Peek() >= 0x20 && reader.Peek() <= 0x21) || (reader.Peek() >= 0x23 && reader.Peek() <= 0x7E)){
                    value.Append((char)reader.Read());
                }
                // Invalid value.
                else{
                    throw new ParseException("Invalid ABNF 'char-val' value '" + reader.ReadToEnd() + "'.");
                }
            }

            return new ABNF_CharVal(value.ToString());
        }


        #region method Validate

        /// <summary>
        /// Validates "prose-val" value.
        /// </summary>
        /// <param name="value">The "prose-val" value.</param>
        /// <returns>Returns if value is "prose-val" value, otherwise false.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        private bool Validate(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            // RFC 5234 4.
            //  char-val =  DQUOTE *(%x20-21 / %x23-7E) DQUOTE

            if(value.Length < 2){
                return false;
            }

            for(int i=0;i<value.Length;i++){
                char c = value[i];

                if(i == 0 && c != '\"'){
                    return false;
                }
                else if(i == (value.Length - 1) && c != '\"'){
                    return false;
                }
                else if(!((c >= 0x20 && c <= 0x21) || (c >= 0x23 && c <= 0x7E))){
                    return false;
                }
            }

            return true;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets value.
        /// </summary>
        public string Value
        {
            get{ return m_Value; }
        }

        #endregion
    }
}
