using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.ABNF
{
    /// <summary>
    /// This class represent ABNF "rulename". Defined in RFC 5234 4.
    /// </summary>
    public class ABNF_RuleName : ABNF_Element
    {
        private string m_RuleName = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="ruleName">Rule name.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>ruleName</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public ABNF_RuleName(string ruleName)
        {
            if(ruleName == null){
                throw new ArgumentNullException("ruleName");
            }
            if(!ValidateName(ruleName)){
                throw new ArgumentException("Invalid argument 'ruleName' value. Value must be 'rulename =  ALPHA *(ALPHA / DIGIT / \"-\")'.");
            }

            m_RuleName = ruleName;
        }


        /// <summary>
        /// 
        /// </summary>
        /// <param name="reader"></param>
        /// <returns></returns>
        public static ABNF_RuleName Parse(System.IO.StringReader reader)
        {
            if(reader == null){
                throw new ArgumentNullException("reader");
            }

            // RFC 5234 4.
            //  rulename =  ALPHA *(ALPHA / DIGIT / "-")

            if(!char.IsLetter((char)reader.Peek())){
                throw new ParseException("Invalid ABNF 'rulename' value '" + reader.ReadToEnd() + "'.");
            }

            StringBuilder ruleName = new StringBuilder();

            while(true){
                // We reached end of string.
                if(reader.Peek() == -1){
                    break;
                }
                // We have valid rule name char.
                else if(char.IsLetter((char)reader.Peek()) | char.IsDigit((char)reader.Peek()) | (char)reader.Peek() == '-'){
                    ruleName.Append((char)reader.Read());
                }
                // Not rule name char, probably readed name.
                else{
                    break;
                }
            }

            return new ABNF_RuleName(ruleName.ToString());
        }


        #region method ValidateName

        /// <summary>
        /// Validates 'rulename' value.
        /// </summary>
        /// <param name="name">Rule name.</param>
        /// <returns>Returns true if rule name is valid, otherwise false.</returns>
        private bool ValidateName(string name)
        {
            if(name == null){
                return false;
            }
            if(name == string.Empty){
                return false;
            }

            // RFC 5234 4.
            //  rulename =  ALPHA *(ALPHA / DIGIT / "-")

            if(!char.IsLetter(name[0])){
                return false;
            }
            for(int i=1;i<name.Length;i++){
                char c = name[i];
                if(!(char.IsLetter(c) | char.IsDigit(c) | c == '-')){
                    return false;
                }
            }

            return true;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets rule name.
        /// </summary>
        public string RuleName
        {
            get{ return m_RuleName; }
        }

        #endregion
    }
}
