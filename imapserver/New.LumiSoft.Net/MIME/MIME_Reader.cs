using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// MIME lexical tokens parser.
    /// </summary>
    public class MIME_Reader
    {
        private string m_Source = "";
        private int    m_Offset = 0;

        #region constants

        private static readonly char[] atextChars = new char[]{'!','#','$','%','&','\'','*','+','-','/','=','?','^','_','`','{','|','}','~'};

        private static readonly char[] specials = new char[]{'(',')','<','>','[',']',':',';','@','\\',',','.','"'};

        private static readonly char[] tspecials = new char[]{'(',')','<','>','@',',',';',':','\\','"','/','[',']','?','='};

        private static readonly Regex encodedword_regex = new Regex(@"=\?(?<charset>.*?)\?(?<encoding>[qQbB])\?(?<value>.*?)\?=",RegexOptions.IgnoreCase);

        #endregion

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="value">Value to read.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null.</exception>
        public MIME_Reader(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            m_Source = value;
        }


        #region method Atom

        /// <summary>
        /// Reads RFC 2822 'atom' from source stream.
        /// </summary>
        /// <returns>Returns RFC 2822 'atom' or null if end of stream reached.</returns>
        public string Atom()
        {
            /* RFC 2822 3.2.4.
             *  atom  = [CFWS] 1*atext [CFWS]
            */

            ToFirstChar();

            StringBuilder retVal = new StringBuilder();
            while(true){
                int peekChar = Peek(false);
                // We reached end of string.
                if(peekChar == -1){
                    break;
                }
                else{
                    char c = (char)peekChar;
                    if(IsAText(c)){
                        retVal.Append((char)Char(false));
                    }
                    // Char is not part of 'atom', break.
                    else{
                        break;
                    }
                }
            }

            if(retVal.Length > 0){
                return retVal.ToString();
            }
            else{
                return null;
            }
        }

        #endregion

        #region method DotAtom

        /// <summary>
        /// Reads RFC 2822 'dot-atom' from source stream.
        /// </summary>
        /// <returns>Returns RFC 2822 'dot-atom' or null if end of stream reached.</returns>
        public string DotAtom()
        {
            /* RFC 2822 3.2.4.
             *  dot-atom      = [CFWS] dot-atom-text [CFWS]
             *  dot-atom-text = 1*atext *("." 1*atext)
            */

            ToFirstChar();

            StringBuilder retVal = new StringBuilder();
            while(true){
                string atom = Atom();
                // We reached end of string.
                if(atom == null){
                    break;
                }                
                else{
                    retVal.Append(atom);
                                      
                    // dot-atom-text continues.                    
                    if(Peek(false) == '.'){
                        retVal.Append((char)Char(false));
                    }
                    else{
                        break;
                    }
                }
            }

            if(retVal.Length > 0){
                return retVal.ToString();
            }
            else{
                return null;
            }
        }

        #endregion

        #region method Token

        /// <summary>
        /// Reads RFC 2045 (section 5) 'token' from source stream.
        /// </summary>
        /// <returns>Returns RFC 2045 (section 5) 'token' or null if end of stream reached.</returns>
        public string Token()
        {
            /* RFC 2045 5.
             *  token := 1*<any (US-ASCII) CHAR except SPACE, CTLs, or tspecials>
            */

            ToFirstChar();

            StringBuilder retVal = new StringBuilder();
            while(true){
                int peekChar = Peek(false);
                // We reached end of string.
                if(peekChar == -1){
                    break;
                }
                else{
                    char c = (char)peekChar;
                    if(IsToken(c)){
                        retVal.Append((char)Char(false));
                    }
                    // Char is not part of 'token', break.
                    else{
                        break;
                    }
                }
            }

            if(retVal.Length > 0){
                return retVal.ToString();
            }
            else{
                return null;
            }
        }

        #endregion

        #region method Comment

        /// <summary>
        /// Reads RFC 822 'comment' from source stream.
        /// </summary>
        /// <returns>Returns RFC 822 'comment' or null if end of stream reached.</returns>
        public string Comment()
        {
            /* RFC 822 3.3.
             *  comment     = "(" *(ctext / quoted-pair / comment) ")"
             *  ctext       = <any CHAR excluding "(", ")", "\" & CR, & including linear-white-space>
             *  quoted-pair = "\" CHAR  
             */

            ToFirstChar();

            if(Peek(false) != '('){
                throw new InvalidOperationException("No 'comment' value available.");
            }

            StringBuilder retVal = new StringBuilder();

            // Remove '('.
            Char(false);

            int nestedParenthesis = 0;
            while(true){
                int intC = Char(false);
                // End of stream reached, invalid 'comment' value.
                if(intC == -1){
                    throw new ArgumentException("Invalid 'comment' value, no closing ')'.");
                }
                else if(intC == '('){
                    nestedParenthesis++;
                }
                else if(intC == ')'){
                    // We readed whole 'comment' ok.
                    if(nestedParenthesis == 0){
                        break;
                    }
                    else{
                        nestedParenthesis--;
                    }
                }
                else{
                    retVal.Append((char)intC);
                }
            }

            return retVal.ToString();
        }

        #endregion

        #region method Word

        /// <summary>
        /// Reads RFC 2822 (section 3.2.6) 'word' from source stream.
        /// </summary>
        /// <returns>Returns RFC 2822 (section 3.2.6) 'word' or null if end of stream reached.</returns>
        public string Word()
        {
            /* RFC 2822 3.2.6.
                word = atom / quoted-string
            
                Consider dot-atom as word too.
            */

            if(Peek(true) == '"'){
                return QuotedString();
            }
            else{
                return DotAtom();
            }
        }

        #endregion

        #region method EncodedWord

        /// <summary>
        /// Reads RFC 2047 'encoded-word' from source stream.
        /// </summary>
        /// <returns>Returns RFC 2047 'encoded-word' or null if end of stream reached.</returns>
        /// <exception cref="InvalidOperationException">Is raised when source stream has no encoded-word at current position.</exception>
        public string EncodedWord()
        {
            /* RFC 2047 2.
                encoded-word = "=?" charset "?" encoding "?" encoded-text "?="
             
                encoded-text = 1*<Any printable ASCII character other than "?" or SPACE>
                               ; (but see "Use of encoded-words in message
                               ; headers", section 5)
            
                An 'encoded-word' may not be more than 75 characters long, including
                'charset', 'encoding', 'encoded-text', and delimiters.  If it is
                desirable to encode more text than will fit in an 'encoded-word' of
                75 characters, multiple 'encoded-word's (separated by CRLF SPACE) may
                be used.
            */

            ToFirstChar();

            if(Peek(false) != '='){
                throw new InvalidOperationException("No encoded-word available.");
            }

            StringBuilder retVal = new StringBuilder();
            while(true){
                Match match = encodedword_regex.Match(m_Source,m_Offset);
                if(match.Success && match.Index == m_Offset){
                    string encodedWord = m_Source.Substring(m_Offset,match.Length);
                    // Move index over encoded-word.
                    m_Offset += match.Length;

                    try{
                        if(string.Equals(match.Groups["encoding"].Value,"Q",StringComparison.InvariantCultureIgnoreCase)){
                            retVal.Append(MIME_Utils.QDecode(Encoding.GetEncoding(match.Groups["charset"].Value),match.Groups["value"].Value));
                        }
                        else if(string.Equals(match.Groups["encoding"].Value,"B",StringComparison.InvariantCultureIgnoreCase)){
                            retVal.Append(Encoding.GetEncoding(match.Groups["charset"].Value).GetString(Net_Utils.FromBase64(Encoding.Default.GetBytes(match.Groups["value"].Value))));
                        }
                        // Failed to parse encoded-word, leave it as is. RFC 2047 6.3.
                        else{
                            retVal.Append(encodedWord);
                        }
                    }
                    catch{
                        // Failed to parse encoded-word, leave it as is. RFC 2047 6.3.
                        retVal.Append(encodedWord);
                    }
                }
                else{
                    retVal.Append(Atom());
                }

                // We have continuos encoded-word.
                match = encodedword_regex.Match(m_Source,m_Offset);
                if(match.Success && match.Index == m_Offset){
                    ToFirstChar();
                }
                // encoded-word does not continue.
                else{
                    break;
                }
            }

            return retVal.ToString();
        }

        #endregion
                
        #region method QuotedString

        /// <summary>
        /// Reads RFC 822 'quoted-string' from source stream.
        /// </summary>
        /// <returns>Returns RFC 822 'quoted-string' or null if end of stream reached.</returns>
        /// <exception cref="InvalidOperationException">Is raised when source stream has no quoted-string at current position.</exception>
        /// <exception cref="ArgumentException">Is raised when not valid 'quoted-string'.</exception>
        public string QuotedString()
        {
            /* RFC 2822 3.2.5.
             *  qtext         = NO-WS-CTL /     ; Non white space controls
                                %d33 /          ; The rest of the US-ASCII
                                %d35-91 /       ;  characters not including "\"
                                %d93-126        ;  or the quote character
                qcontent      = qtext / quoted-pair
                quoted-string = [CFWS] DQUOTE *([FWS] qcontent) [FWS] DQUOTE [CFWS]
            */

            ToFirstChar();

            if(Peek(false) != '"'){
                throw new InvalidOperationException("No quoted-string available.");
            }

            // Read start DQUOTE.
            Char(false);

            StringBuilder retVal = new StringBuilder();
            bool escape = false;
            while(true){
                int intC = Char(false);
                // We reached end of stream, invalid quoted string, end quote is missing.
                if(intC == -1){
                    throw new ArgumentException("Invalid quoted-string, end quote is missing.");
                }
                // This char is escaped.
                else if(escape){
                    escape = false;

                    retVal.Append((char)intC);
                }
                // Closing DQUOTE.
                else if(intC == '"'){
                    break;
                }
                // Next char is escaped.
                else if(intC == '\\'){
                    escape = true;
                }
                // Skip folding chars.
                else if(intC == '\r' || intC == '\n'){
                }                   
                // Normal char in quoted-string.
                else{
                    retVal.Append((char)intC);
                }
            }

            return retVal.ToString();
        }

        #endregion

        #region method Value

        /// <summary>
        /// Reads RFC 2045 (section 5) 'token' from source stream.
        /// </summary>
        /// <returns>Returns 2045 (section 5) 'token' or null if end of stream reached.</returns>
        public string Value()
        {
            // value := token / quoted-string

            if(Peek(true) == '"'){
                return QuotedString();
            }
            else{
                return Token();
            }
        }

        #endregion

        #region method Phrase

        /// <summary>
        /// Reads RFC 2047 (section 5) 'phrase' from source stream.
        /// </summary>
        /// <returns>Returns RFC 2047 (section 5) 'phrase' or null if end of stream reached.</returns>
        public string Phrase()
        {
            /* RFC 2047 5.
             *  phrase = 1*( encoded-word / word )        
             *  word   = atom / quoted-string
            */
                        
            int peek = Peek(true);
            if(peek == -1){
                return null;
            }
            else if(peek == '"'){
                return "\"" + QuotedString() + "\"";
            }
            else if(peek == '='){
                return EncodedWord();
            }
            else{
                string word = Atom();
                if(word == null){
                    return null;
                }
                
                // Try to encode invalid encoded-words if any mixed in text.
                word = encodedword_regex.Replace(word,delegate(Match m){
                    string encodedWord = m.Value;
                    try{
                        if(string.Equals(m.Groups["encoding"].Value,"Q",StringComparison.InvariantCultureIgnoreCase)){
                            return MIME_Utils.QDecode(Encoding.GetEncoding(m.Groups["charset"].Value),m.Groups["value"].Value);
                        }
                        else if(string.Equals(m.Groups["encoding"].Value,"B",StringComparison.InvariantCultureIgnoreCase)){
                            return Encoding.GetEncoding(m.Groups["charset"].Value).GetString(Net_Utils.FromBase64(Encoding.Default.GetBytes(m.Groups["value"].Value)));
                        }
                        // Failed to parse encoded-word, leave it as is. RFC 2047 6.3.
                        else{
                            return encodedWord;
                        }
                    }
                    catch{
                        // Failed to parse encoded-word, leave it as is. RFC 2047 6.3.
                        return encodedWord;
                    }
                });        

                return word;
            }
        }

        #endregion

        #region method Text

        /// <summary>
        /// Reads RFC 822 '*text' from source stream.
        /// </summary>
        /// <returns>Returns RFC 822 '*text' or null if end of stream reached.</returns>
        public string Text()
        {
            throw new NotImplementedException();
        }

        #endregion

        #region method ToFirstChar

        /// <summary>
        /// Reads all white-space chars + CR and LF.
        /// </summary>
        /// <returns>Returns readed chars.</returns>
        public string ToFirstChar()
        {
            // NOTE: Never call Peek or Char method here or stack overflow !

            StringBuilder retVal = new StringBuilder();
            while(true){
                int peekChar = -1;
                if(m_Offset > m_Source.Length - 1){
                    peekChar = -1;
                }
                else{
                    peekChar = m_Source[m_Offset];
                }
                // We reached end of string.
                if(peekChar == -1){
                    break;
                }
                else if(peekChar == ' ' || peekChar == '\t' || peekChar == '\r' || peekChar == '\n'){
                    retVal.Append(m_Source[m_Offset++]);
                }
                else{
                    break;
                }
            }

            return retVal.ToString();
        }

        #endregion

        #region method Char

        /// <summary>
        /// Reads 1 char from source stream.
        /// </summary>
        /// <param name="readToFirstChar">Specifies if postion is moved to char(skips white spaces).</param>
        /// <returns>Returns readed char or -1 if end of stream reached.</returns>
        public int Char(bool readToFirstChar)
        {
            if(readToFirstChar){
                ToFirstChar();
            }

            if(m_Offset > m_Source.Length - 1){
                return -1;
            }
            else{
                return m_Source[m_Offset++];
            }
        }

        #endregion

        #region method Peek

        /// <summary>
        /// Shows next char in source stream, this method won't consume that char.
        /// </summary>
        /// <param name="readToFirstChar">Specifies if postion is moved to char(skips white spaces).</param>
        /// <returns>Returns next char in source stream, returns -1 if end of stream.</returns>
        public int Peek(bool readToFirstChar)
        {
            if(readToFirstChar){
                ToFirstChar();
            }

            if(m_Offset > m_Source.Length - 1){
                return -1;
            }
            else{
                return m_Source[m_Offset];
            }
        }

        #endregion

        #region method StartsWith

        /// <summary>
        /// Gets if source stream valu starts with the specified value. Compare is case-insensitive.
        /// </summary>
        /// <param name="value">Value to check.</param>
        /// <returns>Returns true if source steam satrs with specified string.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null.</exception>
        public bool StartsWith(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            return m_Source.Substring(m_Offset).StartsWith(value,StringComparison.InvariantCultureIgnoreCase);
        }

        #endregion

        #region method ToEnd

        /// <summary>
        /// Reads all data from current postion to the end.
        /// </summary>
        /// <returns>Retruns readed data. Returns null if end of string is reached.</returns>
        public string ToEnd()
        {
            if(m_Offset >= m_Source.Length){
                return null;
            }

            string retVal = m_Source.Substring(m_Offset);
            m_Offset = m_Source.Length;

            return retVal;
        }

        #endregion


        #region static method IsAlpha

        /// <summary>
        /// Gets if the specified char is RFC 822 'ALPHA'.
        /// </summary>
        /// <param name="c">Char to check.</param>
        /// <returns>Returns true if specified char is RFC 822 'ALPHA'.</returns>
        public static bool IsAlpha(char c)
        {
            /* RFC 822 3.3.
                ALPHA = <any ASCII alphabetic character>; (65.- 90.); (97.-122.)
            */

            if((c >= 65 && c <= 90) || (c >= 97 && c <= 122)){
                return true;
            }
            else{
                return false;
            }
        }

        #endregion

        #region static method IsAText

        /// <summary>
        /// Gets if the specified char is RFC 2822 'atext'.
        /// </summary>
        /// <param name="c">Char to check.</param>
        /// <returns>Returns true if specified char is RFC 2822 'atext'.</returns>
        public static bool IsAText(char c)
        {
            /* RFC 2822 3.2.4.
             *  atext = ALPHA / DIGIT / 
             *          "!" / "#" / "$" / "%" / "&" / "'" / "*" / "+" /
             *          "-" / "/" / "=" / "?" / "^" / "_" / "`" / "{" /
             *          "|" / "}" / "~"
            */
                        
            if(IsAlpha(c) || char.IsDigit(c)){
                return true;
            }
            else{
                foreach(char aC in atextChars){
                    if(c == aC){
                        return true;
                    }
                }                
            }

            return false;
        }

        #endregion

        #region static method IsDotAtom

        /// <summary>
        /// Gets if the specified value can be represented as "dot-atom".
        /// </summary>
        /// <param name="value">Value to check.</param>
        /// <returns>Returns true if the specified value can be represented as "dot-atom".</returns>
        public static bool IsDotAtom(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            /* RFC 2822 3.2.4.
             *  dot-atom      = [CFWS] dot-atom-text [CFWS]
             *  dot-atom-text = 1*atext *("." 1*atext)
            */

            foreach(char c in value){
                if(c != '.' && !IsAText(c)){
                    return false;
                }
            }

            return true;
        }

        #endregion

        #region static method IsToken

        /// <summary>
        /// Gets if specified valu is RFC 2045 (section 5) 'token'.
        /// </summary>
        /// <param name="text">Text to check.</param>
        /// <returns>Returns true if specified char is RFC 2045 (section 5) 'token'.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>text</b> is null.</exception>
        public static bool IsToken(string text)
        {
            if(text == null){
                throw new ArgumentNullException("text");
            }

            if(text == ""){
                return false;
            }

            foreach(char c in text){
                if(!IsToken(c)){
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Gets if the specified char is RFC 2045 (section 5) 'token'.
        /// </summary>
        /// <param name="c">Char to check.</param>
        /// <returns>Returns true if specified char is RFC 2045 (section 5) 'token'.</returns>
        public static bool IsToken(char c)
        {
            /* RFC 2045 5.
             *  token := 1*<any (US-ASCII) CHAR except SPACE, CTLs, or tspecials>
             * 
             * RFC 822 3.3.
             *  CTL =  <any ASCII control; (0.- 31.); (127.)
            */
            
            if(c <= 31 || c == 127){
                return false;
            }
            else if(c == ' '){
                return false;
            }
            else{
                foreach(char tsC in tspecials){
                    if(tsC == c){
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion

        #region static method IsAttributeChar
                
        /// <summary>
        /// Gets if the specified char is RFC 2231 (section 7) 'attribute-char'.
        /// </summary>
        /// <param name="c">Char to check.</param>
        /// <returns>Returns true if specified char is RFC 2231 (section 7) 'attribute-char'.</returns>
        public static bool IsAttributeChar(char c)
        {
            /* RFC 2231 7.
             * attribute-char := <any (US-ASCII) CHAR except SPACE, CTLs, "*", "'", "%", or tspecials>
             * 
             * RFC 822 3.3.
             *  CTL =  <any ASCII control; (0.- 31.); (127.)
            */

            if(c <= 31 || c > 127){
                return false;
            }
            else if(c == ' ' || c == '*' || c == '\'' || c == '%'){
                return false;
            }
            else{
                foreach(char cS in tspecials){
                    if(c == cS){
                        return false;
                    }
                }
            }

            return true;
        }

        #endregion


        #region method ReadParenthesized

		/// <summary>
		/// Reads parenthesized value. Supports {},(),[],&lt;&gt; parenthesis. 
		/// Throws exception if there isn't parenthesized value or closing parenthesize is missing.
		/// </summary>
		/// <returns>Returns value between parenthesized.</returns>
		public string ReadParenthesized()
		{
            ToFirstChar();

			char startingChar = ' ';
			char closingChar  = ' ';

			if(m_Source[m_Offset] == '{'){
				startingChar = '{';
				closingChar = '}';
			}
			else if(m_Source[m_Offset] == '('){
				startingChar = '(';
				closingChar = ')';
			}			
			else if(m_Source[m_Offset] == '['){
				startingChar = '[';
				closingChar = ']';
			}						
			else if(m_Source[m_Offset] == '<'){
				startingChar = '<';
				closingChar = '>';
			}
			else{
				throw new Exception("No parenthesized value '" + m_Source.Substring(m_Offset) + "' !");
			}
            m_Offset++;

			bool inQuotedString            = false; // Holds flag if position is quoted string or not
			char lastChar                  = (char)0;
			int  nestedStartingCharCounter = 0;
			for(int i=m_Offset;i<m_Source.Length;i++){
				// Skip escaped(\) "
				if(lastChar != '\\' && m_Source[i] == '\"'){
					// Start/end quoted string area
					inQuotedString = !inQuotedString;
				}
				// We need to skip parenthesis in quoted string
				else if(!inQuotedString){
					// There is nested parenthesis
					if(m_Source[i] == startingChar){
						nestedStartingCharCounter++;
					}
					// Closing char
					else if(m_Source[i] == closingChar){
						// There isn't nested parenthesis closing chars left, this is closing char what we want.
						if(nestedStartingCharCounter == 0){
                            string retVal = m_Source.Substring(m_Offset,i - m_Offset);
                            m_Offset = i + 1;

				            return retVal;
						}
						// This is nested parenthesis closing char
						else{
							nestedStartingCharCounter--;
						}
					}
				}

				lastChar = m_Source[i];
			}

			throw new ArgumentException("There is no closing parenthesize for '" + m_Source.Substring(m_Offset) + "' !");
		}

		#endregion

        #region method QuotedReadToDelimiter

        /// <summary>
		/// Reads string to specified delimiter or to end of underlying string. Notes: Delimiters in quoted string is skipped. 
		/// For example: delimiter = ',', text = '"aaaa,eee",qqqq' - then result is '"aaaa,eee"'.
		/// </summary>
		/// <param name="delimiters">Data delimiters.</param>
		/// <returns>Returns readed string or null if end of string reached.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>delimiters</b> is null reference.</exception>
		public string QuotedReadToDelimiter(char[] delimiters)
		{
            if(delimiters == null){
                throw new ArgumentNullException("delimiters");
            }

            if(this.Available == 0){
                return null;
            }

            ToFirstChar();

			StringBuilder currentSplitBuffer = new StringBuilder(); // Holds active
			bool          inQuotedString     = false;               // Holds flag if position is quoted string or not
			char          lastChar           = (char)0;

			for(int i=m_Offset;i<m_Source.Length;i++){
				char c = (char)Peek(false);

				// Skip escaped(\) "
				if(lastChar != '\\' && c == '\"'){
					// Start/end quoted string area
					inQuotedString = !inQuotedString;
				}
			
                // See if char is delimiter
                bool isDelimiter = false;
                foreach(char delimiter in delimiters){
                    if(c == delimiter){
                        isDelimiter = true;
                        break;
                    }
                }

				// Current char is split char and it isn't in quoted string, do split
				if(!inQuotedString && isDelimiter){
					return currentSplitBuffer.ToString();
				}
				else{
					currentSplitBuffer.Append(c);
                    m_Offset++;
				}

				lastChar = c;
			}
            
			// If we reached so far then we are end of string, return it.
			return currentSplitBuffer.ToString();
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets number of chars has left for processing.
        /// </summary>
        public int Available
        {
            get{ return m_Source.Length - m_Offset; }
        }

        /// <summary>
        /// Gets position in string.
        /// </summary>
        public int Position
        {
            get{ return m_Offset; }
        }

        #endregion

    }
}
