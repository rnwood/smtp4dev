using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Text;
using System.Text.RegularExpressions;

using LumiSoft.Net.DNS;
using LumiSoft.Net.DNS.Client;

namespace LumiSoft.Net
{
	#region enum AuthType

	/// <summary>
	/// Authentication type.
	/// </summary>
	public enum AuthType
	{
		/// <summary>
		/// Plain username/password authentication.
		/// </summary>
		Plain = 0,

		/// <summary>
		/// APOP
		/// </summary>
		APOP  = 1,

		/// <summary>
		/// Not implemented.
		/// </summary>
		LOGIN = 2,	
	
		/// <summary>
		/// Cram-md5 authentication.
		/// </summary>
		CRAM_MD5 = 3,	

		/// <summary>
		/// DIGEST-md5 authentication.
		/// </summary>
		DIGEST_MD5 = 4,	
	}

	#endregion

	/// <summary>
	/// Provides net core utility methods.
	/// </summary>
    [Obsolete("")]
	public class Core
	{		
		
		#region method GetHostName

		/// <summary>
		/// Gets host name. If fails returns ip address.
		/// </summary>
		/// <param name="ip">IP address which to reverse lookup.</param>
		/// <returns>Returns host name of specified IP address.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null.</exception>
		public static string GetHostName(IPAddress ip)
		{
            if(ip == null){
                throw new ArgumentNullException("ip");
            }

            string retVal = ip.ToString();
			try{
                Dns_Client dns = new Dns_Client();
                DnsServerResponse response = dns.Query(ip.ToString(),DNS_QType.PTR);
                if(response.ResponseCode == DNS_RCode.NO_ERROR){
                    DNS_rr_PTR[] ptrs = response.GetPTRRecords();
                    if(ptrs.Length > 0){
                        retVal = ptrs[0].DomainName;
                    }                    
                }
			}
			catch{
			}

            return retVal;
		}

		#endregion


		#region method GetArgsText

		/// <summary>
		/// Gets argument part of command text.
		/// </summary>
		/// <param name="input">Input srting from where to remove value.</param>
		/// <param name="cmdTxtToRemove">Command text which to remove.</param>
		/// <returns></returns>
		public static string GetArgsText(string input,string cmdTxtToRemove)
		{
			string buff = input.Trim();
			if(buff.Length >= cmdTxtToRemove.Length){
				buff = buff.Substring(cmdTxtToRemove.Length);
			}
			buff = buff.Trim();

			return buff;
		}

		#endregion

		
		#region method IsNumber

		/// <summary>
		/// Checks if specified string is number(long).
		/// </summary>
		/// <param name="str"></param>
		/// <returns></returns>
        [Obsolete("Use Net_Utils.IsInteger instead of it")]
		public static bool IsNumber(string str)
		{
			try{
				Convert.ToInt64(str);
				return true;
			}
			catch{
				return false;
			}
		}

		#endregion


        #region static method ReverseArray

        /// <summary>
        /// Reverses the specified array elements.
        /// </summary>
        /// <param name="array">Array elements to reverse.</param>
        /// <returns>Returns array with reversed items.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>array</b> is null.</exception>
        [Obsolete("Use Net_Utils.ReverseArray instead of it")]
        public static Array ReverseArray(Array array)
        {
            if(array == null){
                throw new ArgumentNullException("array");
            }

            Array.Reverse(array);

            return array;
        }

        #endregion


        #region static method Base64Encode

        /// <summary>
		/// Encodes specified data with base64 encoding.
		/// </summary>
		/// <param name="data">Data to encode.</param>
		/// <returns></returns>
		public static byte[] Base64Encode(byte[] data)
		{
			return Base64EncodeEx(data,null,true);
		}

		/// <summary>
		/// Encodes specified data with bas64 encoding.
		/// </summary>
		/// <param name="data">Data to to encode.</param>
		/// <param name="base64Chars">Custom base64 chars (64 chars) or null if default chars used.</param>
		/// <param name="padd">Padd missing block chars. Normal base64 must be 4 bytes blocks, if not 4 bytes in block, 
		/// missing bytes must be padded with '='. Modified base64 just skips missing bytes.</param>
		/// <returns></returns>
		public static byte[] Base64EncodeEx(byte[] data,char[] base64Chars,bool padd)
		{
			/* RFC 2045 6.8.  Base64 Content-Transfer-Encoding
			
				Base64 is processed from left to right by 4 6-bit byte block, 4 6-bit byte block 
				are converted to 3 8-bit bytes.
				If base64 4 byte block doesn't have 3 8-bit bytes, missing bytes are marked with =. 
				
			
				Value Encoding  Value Encoding  Value Encoding  Value Encoding
					0 A            17 R            34 i            51 z
					1 B            18 S            35 j            52 0
					2 C            19 T            36 k            53 1
					3 D            20 U            37 l            54 2
					4 E            21 V            38 m            55 3
					5 F            22 W            39 n            56 4
					6 G            23 X            40 o            57 5
					7 H            24 Y            41 p            58 6
					8 I            25 Z            42 q            59 7
					9 J            26 a            43 r            60 8
					10 K           27 b            44 s            61 9
					11 L           28 c            45 t            62 +
					12 M           29 d            46 u            63 /
					13 N           30 e            47 v
					14 O           31 f            48 w         (pad) =
					15 P           32 g            49 x
					16 Q           33 h            50 y
					
				NOTE: 4 base64 6-bit bytes = 3 8-bit bytes				
					// |    6-bit    |    6-bit    |    6-bit    |    6-bit    |
					// | 1 2 3 4 5 6 | 1 2 3 4 5 6 | 1 2 3 4 5 6 | 1 2 3 4 5 6 |
					// |    8-bit         |    8-bit        |    8-bit         |
			*/

			if(base64Chars != null && base64Chars.Length != 64){
				throw new Exception("There must be 64 chars in base64Chars char array !");
			}

			if(base64Chars == null){
				base64Chars = new char[]{
					'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
					'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
					'0','1','2','3','4','5','6','7','8','9','+','/'
				};
			}

			// Convert chars to bytes
			byte[] base64LoockUpTable = new byte[64];
			for(int i=0;i<64;i++){
				base64LoockUpTable[i] = (byte)base64Chars[i];
			}
						
			int encodedDataLength = (int)Math.Ceiling((data.Length * 8) / (double)6);
			// Retrun value won't be interegral 4 block, but has less. Padding requested, padd missing with '='
			if(padd && (encodedDataLength / (double)4 != Math.Ceiling(encodedDataLength / (double)4))){
				encodedDataLength += (int)(Math.Ceiling(encodedDataLength / (double)4) * 4) - encodedDataLength;
			}

			// See how many line brakes we need
			int numberOfLineBreaks = 0;
			if(encodedDataLength > 76){
				numberOfLineBreaks = (int)Math.Ceiling(encodedDataLength / (double)76) - 1;
			}

			// Construc return valu buffer
			byte[] retVal = new byte[encodedDataLength + (numberOfLineBreaks * 2)];  // * 2 - CRLF

			int lineBytes = 0;
			// Loop all 3 bye blocks
			int position = 0; 
			for(int i=0;i<data.Length;i+=3){
				// Do line splitting
				if(lineBytes >= 76){
					retVal[position + 0] = (byte)'\r';
					retVal[position + 1] = (byte)'\n';					
					position += 2;
					lineBytes = 0;
				}

				// Full 3 bytes data block
				if((data.Length - i) >= 3){
					retVal[position + 0] = base64LoockUpTable[data[i + 0] >> 2];
					retVal[position + 1] = base64LoockUpTable[(data[i + 0] & 0x3) << 4 | data[i + 1] >> 4];
					retVal[position + 2] = base64LoockUpTable[(data[i + 1] & 0xF) << 2 | data[i + 2] >> 6];
					retVal[position + 3] = base64LoockUpTable[data[i + 2] & 0x3F];
					position += 4;
					lineBytes += 4;
				}
				// 2 bytes data block, left (last block)
				else if((data.Length - i) == 2){
					retVal[position + 0] = base64LoockUpTable[data[i + 0] >> 2];
					retVal[position + 1] = base64LoockUpTable[(data[i + 0] & 0x3) << 4 | data[i + 1] >> 4];
					retVal[position + 2] = base64LoockUpTable[(data[i + 1] & 0xF) << 2];					
					if(padd){
						retVal[position + 3] = (byte)'=';
					}
				}
				// 1 bytes data block, left (last block)
				else if((data.Length - i) == 1){
					retVal[position + 0] = base64LoockUpTable[data[i + 0] >> 2];
					retVal[position + 1] = base64LoockUpTable[(data[i + 0] & 0x3) << 4];					
					if(padd){
						retVal[position + 2] = (byte)'=';
						retVal[position + 3] = (byte)'=';
					}
				}
			}

			return retVal;
		}

		#endregion

		#region static method Base64Decode

		/// <summary>
		/// Decodes base64 data. Defined in RFC 2045 6.8.  Base64 Content-Transfer-Encoding.
		/// </summary>
		/// <param name="base64Data">Base64 decoded data.</param>
		/// <returns></returns>
        [Obsolete("Use Net_Utils.FromBase64 instead of it")]
		public static byte[] Base64Decode(byte[] base64Data)
		{
			return Base64DecodeEx(base64Data,null);
		}

		/// <summary>
		/// Decodes base64 data. Defined in RFC 2045 6.8.  Base64 Content-Transfer-Encoding.
		/// </summary>
		/// <param name="base64Data">Base64 decoded data.</param>
		/// <param name="base64Chars">Custom base64 chars (64 chars) or null if default chars used.</param>
		/// <returns></returns>
		public static byte[] Base64DecodeEx(byte[] base64Data,char[] base64Chars)
		{
			/* RFC 2045 6.8.  Base64 Content-Transfer-Encoding
			
				Base64 is processed from left to right by 4 6-bit byte block, 4 6-bit byte block 
				are converted to 3 8-bit bytes.
				If base64 4 byte block doesn't have 3 8-bit bytes, missing bytes are marked with =. 
				
			
				Value Encoding  Value Encoding  Value Encoding  Value Encoding
					0 A            17 R            34 i            51 z
					1 B            18 S            35 j            52 0
					2 C            19 T            36 k            53 1
					3 D            20 U            37 l            54 2
					4 E            21 V            38 m            55 3
					5 F            22 W            39 n            56 4
					6 G            23 X            40 o            57 5
					7 H            24 Y            41 p            58 6
					8 I            25 Z            42 q            59 7
					9 J            26 a            43 r            60 8
					10 K           27 b            44 s            61 9
					11 L           28 c            45 t            62 +
					12 M           29 d            46 u            63 /
					13 N           30 e            47 v
					14 O           31 f            48 w         (pad) =
					15 P           32 g            49 x
					16 Q           33 h            50 y
					
				NOTE: 4 base64 6-bit bytes = 3 8-bit bytes				
					// |    6-bit    |    6-bit    |    6-bit    |    6-bit    |
					// | 1 2 3 4 5 6 | 1 2 3 4 5 6 | 1 2 3 4 5 6 | 1 2 3 4 5 6 |
					// |    8-bit         |    8-bit        |    8-bit         |
			*/
			
			if(base64Chars != null && base64Chars.Length != 64){
				throw new Exception("There must be 64 chars in base64Chars char array !");
			}

			if(base64Chars == null){
				base64Chars = new char[]{
					'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
					'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
					'0','1','2','3','4','5','6','7','8','9','+','/'
				};
			}

			//--- Create decode table ---------------------//
			byte[] decodeTable = new byte[128];
			for(int i=0;i<128;i++){
				int mappingIndex = -1;
				for(int bc=0;bc<base64Chars.Length;bc++){
					if(i == base64Chars[bc]){
						mappingIndex = bc;
						break;
					}
				}

				if(mappingIndex > -1){
					decodeTable[i] = (byte)mappingIndex;
				}
				else{
					decodeTable[i] = 0xFF;
				}
			}
			//---------------------------------------------//

			byte[] decodedDataBuffer  = new byte[((base64Data.Length * 6) / 8) + 4];
			int    decodedBytesCount  = 0;
			int    nByteInBase64Block = 0;
			byte[] decodedBlock       = new byte[3];
			byte[] base64Block        = new byte[4];

			for(int i=0;i<base64Data.Length;i++){
				byte b = base64Data[i];

				// Read 4 byte base64 block and process it 			
				// Any characters outside of the base64 alphabet are to be ignored in base64-encoded data.

				// Padding char
				if(b == '='){
					base64Block[nByteInBase64Block] = 0xFF;
				}
				else{
					byte decodeByte = decodeTable[b & 0x7F];
					if(decodeByte != 0xFF){
						base64Block[nByteInBase64Block] = decodeByte;
						nByteInBase64Block++;
					}
				}

                /* Check if we can decode some bytes. 
                 * We must have full 4 byte base64 block or reached at the end of data.
                 */
                int encodedBytesCount = -1;
                // We have full 4 byte base64 block
                if(nByteInBase64Block == 4){
                    encodedBytesCount = 3;
                }
                // We have reached at the end of base64 data, there may be some bytes left
                else if(i == base64Data.Length - 1){
                    // Invalid value, we can't have only 6 bit, just skip 
                    if(nByteInBase64Block == 1){
                        encodedBytesCount = 0;
                    }
                    // There is 1 byte in two base64 bytes (6 + 2 bit)
                    else if(nByteInBase64Block == 2){
                        encodedBytesCount = 1;
                    }
                    // There are 2 bytes in two base64 bytes ([6 + 2],[4 + 4] bit)
                    else if(nByteInBase64Block == 3){
                        encodedBytesCount = 2;
                    }
                }

                // We have some bytes available to decode, decode them
                if(encodedBytesCount > -1){
                    decodedDataBuffer[decodedBytesCount + 0] = (byte)((int)base64Block[0] << 2         | (int)base64Block[1] >> 4);
					decodedDataBuffer[decodedBytesCount + 1] = (byte)(((int)base64Block[1] & 0xF) << 4 | (int)base64Block[2] >> 2);
					decodedDataBuffer[decodedBytesCount + 2] = (byte)(((int)base64Block[2] & 0x3) << 6 | (int)base64Block[3] >> 0);

                    // Increase decoded bytes count
					decodedBytesCount += encodedBytesCount;

                    // Reset this block, reade next if there is any
					nByteInBase64Block = 0;
                }
			}

			// There is some decoded bytes, construct return value
			if(decodedBytesCount > -1){
				byte[] retVal = new byte[decodedBytesCount];
				Array.Copy(decodedDataBuffer,0,retVal,0,decodedBytesCount);
				return retVal;
			}
			// There is no decoded bytes
			else{
				return new byte[0];
			}
		}

		#endregion

		#region method QuotedPrintableEncode

		/// <summary>
		/// Encodes data with quoted-printable encoding.
		/// </summary>
		/// <param name="data">Data to encode.</param>
		/// <returns></returns>
		public static byte[] QuotedPrintableEncode(byte[] data)
		{			
			/* Rfc 2045 6.7. Quoted-Printable Content-Transfer-Encoding
			 
			(2) (Literal representation) Octets with decimal values of 33 through 60 inclusive, 
				and 62 through 126, inclusive, MAY be represented as the US-ASCII characters which
				correspond to those octets (EXCLAMATION POINT through LESS THAN, and GREATER THAN 
				through TILDE, respectively).
			
			(3) (White Space) Octets with values of 9 and 32 MAY be represented as US-ASCII TAB (HT) and 
			    SPACE characters, respectively, but MUST NOT be so represented at the end of an encoded line. 
				You must encode it =XX.
			
			(5) Encoded lines must not be longer than 76 characters, not counting the trailing CRLF. 
				If longer lines are to be encoded with the Quoted-Printable encoding, "soft" line breaks
				must be used.  An equal sign as the last character on a encoded line indicates such 
				a non-significant ("soft") line break in the encoded text.
				
			*)  If binary data is encoded in quoted-printable, care must be taken to encode 
			    CR and LF characters as "=0D" and "=0A", respectively.	 

			*/

			int lineLength = 0;
			// Encode bytes <= 33 , >= 126 and 61 (=)
			MemoryStream retVal = new MemoryStream();
			foreach(byte b in data){
				// Suggested line length is exceeded, add soft line break
				if(lineLength > 75){
					retVal.Write(new byte[]{(byte)'=',(byte)'\r',(byte)'\n'},0,3);
					lineLength = 0;
				}

				// We need to encode that byte
				if(b <= 33 || b >= 126 || b == 61){					
					retVal.Write(new byte[]{(byte)'='},0,1);
					retVal.Write(Core.ToHex(b),0,2);
					lineLength += 3;
				}
				// We don't need to encode that byte, just write it to stream
				else{
					retVal.WriteByte(b);
					lineLength++;
				}
			}

			return retVal.ToArray();
		}

		#endregion

		#region method QuotedPrintableDecode

		/// <summary>
		/// quoted-printable decoder. Defined in RFC 2045 6.7.
		/// </summary>
		/// <param name="data">Data which to encode.</param>
		/// <returns></returns>
        [Obsolete("Use MIME_Utils.QuotedPrintableDecode instead of it")]
		public static byte[] QuotedPrintableDecode(byte[] data)
		{
			/* RFC 2045 6.7. Quoted-Printable Content-Transfer-Encoding
			 
				(1)	(General 8bit representation) Any octet, except a CR or
					LF that is part of a CRLF line break of the canonical
					(standard) form of the data being encoded, may be
					represented by an "=" followed by a two digit
					hexadecimal representation of the octet's value.  The
					digits of the hexadecimal alphabet, for this purpose,
					are "0123456789ABCDEF".  Uppercase letters must be
					used; lowercase letters are not allowed.

				(2) (Literal representation) Octets with decimal values of
					33 through 60 inclusive, and 62 through 126, inclusive,
					MAY be represented as the US-ASCII characters which
					correspond to those octets (EXCLAMATION POINT through
					LESS THAN, and GREATER THAN through TILDE, respectively).
					
				(3) (White Space) Octets with values of 9 and 32 MAY be
					represented as US-ASCII TAB (HT) and SPACE characters,
					respectively, but MUST NOT be so represented at the end
					of an encoded line.  Any TAB (HT) or SPACE characters
					on an encoded line MUST thus be followed on that line
					by a printable character.  In particular, an "=" at the
					end of an encoded line, indicating a soft line break
					(see rule #5) may follow one or more TAB (HT) or SPACE
					characters.  It follows that an octet with decimal
					value 9 or 32 appearing at the end of an encoded line
					must be represented according to Rule #1.  This rule is
					necessary because some MTAs (Message Transport Agents,
					programs which transport messages from one user to
					another, or perform a portion of such transfers) are
					known to pad lines of text with SPACEs, and others are
					known to remove "white space" characters from the end
					of a line.  Therefore, when decoding a Quoted-Printable
					body, any trailing white space on a line must be
					deleted, as it will necessarily have been added by
					intermediate transport agents.
					
				(4) (Line Breaks) A line break in a text body, represented
				    as a CRLF sequence in the text canonical form, must be
					represented by a (RFC 822) line break, which is also a
					CRLF sequence, in the Quoted-Printable encoding.  Since
					the canonical representation of media types other than
					text do not generally include the representation of
					line breaks as CRLF sequences, no hard line breaks
					(i.e. line breaks that are intended to be meaningful
					and to be displayed to the user) can occur in the
					quoted-printable encoding of such types.  Sequences
					like "=0D", "=0A", "=0A=0D" and "=0D=0A" will routinely
					appear in non-text data represented in quoted-
					printable, of course.

				(5) (Soft Line Breaks) The Quoted-Printable encoding
					REQUIRES that encoded lines be no more than 76
					characters long.  If longer lines are to be encoded
					with the Quoted-Printable encoding, "soft" line breaks
			*/

			MemoryStream msRetVal = new MemoryStream();
			MemoryStream msSourceStream = new MemoryStream(data);

			int b = msSourceStream.ReadByte();
			while(b > -1){
				// Encoded 8-bit byte(=XX) or soft line break(=CRLF)
				if(b == '='){
					byte[] buffer = new byte[2];
					int nCount = msSourceStream.Read(buffer,0,2);
					if(nCount == 2){
						// Soft line break, line splitted, just skip CRLF
						if(buffer[0] == '\r' && buffer[1] == '\n'){
						}
						// This must be encoded 8-bit byte
						else{
							try{
								msRetVal.Write(FromHex(buffer),0,1);
							}
							catch{
								// Illegal value after =, just leave it as is
								msRetVal.WriteByte((byte)'=');
								msRetVal.Write(buffer,0,2);
							}
						}
					}
					// Illegal =, just leave as it is
					else{
						msRetVal.Write(buffer,0,nCount);
					}
				}
				// Just write back all other bytes
				else{
					msRetVal.WriteByte((byte)b);
				}

				// Read next byte
				b = msSourceStream.ReadByte();
			}

			return msRetVal.ToArray();
		}

		#endregion

		#region method QDecode

		/// <summary>
		/// "Q" decoder. This is same as quoted-printable, except '_' is converted to ' '.
        /// Defined in RFC 2047 4.2.
		/// </summary>
		/// <param name="encoding">Input string encoding.</param>
		/// <param name="data">String which to encode.</param>
		/// <returns>Returns decoded string.</returns>	
        [Obsolete("Use MIME_Utils.QDecode instead of it")]	
		public static string QDecode(System.Text.Encoding encoding,string data)
		{
			return encoding.GetString(QuotedPrintableDecode(System.Text.Encoding.ASCII.GetBytes(data.Replace("_"," "))));
		}

		#endregion

		#region method CanonicalDecode

		/// <summary>
		/// Canonical decoding. Decodes all canonical encoding occurences in specified text.
		/// Usually mime message header unicode/8bit values are encoded as Canonical.
		/// Format: =?charSet?type[Q or B]?encoded_string?= .
		/// Defined in RFC 2047.
		/// </summary>
		/// <param name="text">Text to decode.</param>
		/// <returns></returns>
        [Obsolete("Use MimeUtils.DecodeWords method instead.")]
		public static string CanonicalDecode(string text)
		{
			/* RFC 2047			 
				Generally, an "encoded-word" is a sequence of printable ASCII
				characters that begins with "=?", ends with "?=", and has two "?"s in
				between.
				
				Syntax: =?charSet?type[Q or B]?encoded_string?=
				
				Examples:
					=?utf-8?q?Buy a Rolex?=
					=?iso-8859-1?B?bORs5D8=?=
			*/

			StringBuilder retVal = new StringBuilder();
			int offset = 0;
			while(offset < text.Length){
				// Search start and end of canonical entry
				int iStart = text.IndexOf("=?",offset);
				int iEnd = -1;
				if(iStart > -1){
					// End index must be over start index position
					iEnd = text.IndexOf("?=",iStart + 2);
				}
				
				if(iStart > -1 && iEnd > -1){
					// Add left side non encoded text of encoded text, if there is any
					if((iStart - offset) > 0){
						retVal.Append(text.Substring(offset,iStart - offset));
					}

					while(true){
						// Check if it is encoded entry
						string[] charset_type_text = text.Substring(iStart + 2,iEnd - iStart - 2).Split('?');
						if(charset_type_text.Length == 3){
							// Try to parse encoded text
							try{
								Encoding enc = Encoding.GetEncoding(charset_type_text[0]);
								// QEncoded text
								if(charset_type_text[1].ToLower() == "q"){
									retVal.Append(Core.QDecode(enc,charset_type_text[2]));
								}
								// Base64 encoded text
								else{
                                    retVal.Append(enc.GetString(Core.Base64Decode(Encoding.Default.GetBytes(charset_type_text[2]))));
								}
							}
							catch{
								// Parsing failed, just leave text as is.
								retVal.Append(text.Substring(iStart,iEnd - iStart + 2));
							}

							// Move current offset in string
							offset = iEnd + 2;
							break;
						}
						// This isn't right end tag, try next
						else if(charset_type_text.Length < 3){
							// Try next end tag
							iEnd = text.IndexOf("?=",iEnd + 2);
						
							// No suitable end tag for active start tag, move offset over start tag.
							if(iEnd == -1){								
								retVal.Append("=?");
								offset = iStart + 2;
								break;
							}
						}
						// Illegal start tag or start tag is just in side some text, move offset over start tag.
						else{						
							retVal.Append("=?");
							offset = iStart + 2;
							break;
						}
					}
				}
				// There are no more entries
				else{
					// Add remaining non encoded text, if there is any.
					if(text.Length > offset){
						retVal.Append(text.Substring(offset));
						offset = text.Length;
					}
				}				
			}

			return retVal.ToString();
		}

		#endregion

		#region method CanonicalEncode

		/// <summary>
		/// Canonical encoding.
		/// </summary>
		/// <param name="str">String to encode.</param>
		/// <param name="charSet">With what charset to encode string. If you aren't sure about it, utf-8 is suggested.</param>
		/// <returns>Returns encoded text.</returns>
		public static string CanonicalEncode(string str,string charSet)
		{
            /* RFC 2049 2. (9),(10)
                =?encodedWord?=
                encodedWord -> charset?encoding?encodedText
                encoding -> Q(Q encode) or B(base64)
            */

			// Contains non ascii chars, must to encode.
			if(!IsAscii(str)){
				string retVal = "=?" + charSet + "?" + "B?";
				retVal += Convert.ToBase64String(System.Text.Encoding.GetEncoding(charSet).GetBytes(str));
				retVal += "?=";

				return retVal;
			}

			return str;
		}

		#endregion

		#region static method Encode_IMAP_UTF7_String

		/// <summary>
		/// Encodes specified data with IMAP modified UTF7 encoding. Defined in RFC 3501 5.1.3.  Mailbox International Naming Convention.
		/// Example: öö is encoded to &amp;APYA9g-.
		/// </summary>
		/// <param name="text">Text to encode.</param>
		/// <returns></returns>
        [Obsolete("Use IMAP_Utils.Encode_IMAP_UTF7_String instead of it")]
		public static string Encode_IMAP_UTF7_String(string text)
		{
			/* RFC 3501 5.1.3.  Mailbox International Naming Convention
				In modified UTF-7, printable US-ASCII characters, except for "&",
				represent themselves; that is, characters with octet values 0x20-0x25
				and 0x27-0x7e.  The character "&" (0x26) is represented by the
				two-octet sequence "&-".

				All other characters (octet values 0x00-0x1f and 0x7f-0xff) are
				represented in modified BASE64, with a further modification from
				[UTF-7] that "," is used instead of "/".  Modified BASE64 MUST NOT be
				used to represent any printing US-ASCII character which can represent
				itself.
				
				"&" is used to shift to modified BASE64 and "-" to shift back to
				US-ASCII.  There is no implicit shift from BASE64 to US-ASCII, and
				null shifts ("-&" while in BASE64; note that "&-" while in US-ASCII
				means "&") are not permitted.  However, all names start in US-ASCII,
				and MUST end in US-ASCII; that is, a name that ends with a non-ASCII
				ISO-10646 character MUST end with a "-").
			*/

			// Base64 chars, except '/' is replaced with ','
			char[] base64Chars = new char[]{
				'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
				'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
				'0','1','2','3','4','5','6','7','8','9','+',','
			};

			MemoryStream retVal = new MemoryStream();
			for(int i=0;i<text.Length;i++){
				char c = text[i];

				// The character "&" (0x26) is represented by the two-octet sequence "&-".
				if(c == '&'){
					retVal.Write(new byte[]{(byte)'&',(byte)'-'},0,2);
				}
				// It is allowed char, don't need to encode
				else if(c >= 0x20 && c <= 0x25 || c >= 0x27 && c <= 0x7E){
					retVal.WriteByte((byte)c);
				}
				// Not allowed char, encode it
				else{
					// Superfluous shifts are not allowed. 
					// For example: öö may not encoded as &APY-&APY-, but must be &APYA9g-.

					// Get all continuous chars that need encoding and encode them as one block
					MemoryStream encodeBlock = new MemoryStream();
					for(int ic=i;ic<text.Length;ic++){
						char cC = text[ic];

						// Allowed char
						if(cC >= 0x20 && cC <= 0x25 || cC >= 0x27 && cC <= 0x7E){
							break;
						}
						else{
							encodeBlock.WriteByte((byte)((cC & 0xFF00) >> 8));
							encodeBlock.WriteByte((byte)(cC & 0xFF));
							i = ic;
						}
					}

					// Ecode block
					byte[] encodedData = Core.Base64EncodeEx(encodeBlock.ToArray(),base64Chars,false);
					retVal.WriteByte((byte)'&');
					retVal.Write(encodedData,0,encodedData.Length);
					retVal.WriteByte((byte)'-');
				}
			}

			return System.Text.Encoding.Default.GetString(retVal.ToArray());
		}

		#endregion

		#region static method Decode_IMAP_UTF7_String

		/// <summary>
		/// Decodes IMAP modified UTF7 encoded data. Defined in RFC 3501 5.1.3.  Mailbox International Naming Convention.
		/// Example: &amp;APYA9g- is decoded to öö.
		/// </summary>
		/// <param name="text">Text to encode.</param>
		/// <returns></returns>        
        [Obsolete("Use IMAP_Utils.Decode_IMAP_UTF7_String instead of it")]
		public static string Decode_IMAP_UTF7_String(string text)
		{
			/* RFC 3501 5.1.3.  Mailbox International Naming Convention
				In modified UTF-7, printable US-ASCII characters, except for "&",
				represent themselves; that is, characters with octet values 0x20-0x25
				and 0x27-0x7e.  The character "&" (0x26) is represented by the
				two-octet sequence "&-".

				All other characters (octet values 0x00-0x1f and 0x7f-0xff) are
				represented in modified BASE64, with a further modification from
				[UTF-7] that "," is used instead of "/".  Modified BASE64 MUST NOT be
				used to represent any printing US-ASCII character which can represent
				itself.
				
				"&" is used to shift to modified BASE64 and "-" to shift back to
				US-ASCII.  There is no implicit shift from BASE64 to US-ASCII, and
				null shifts ("-&" while in BASE64; note that "&-" while in US-ASCII
				means "&") are not permitted.  However, all names start in US-ASCII,
				and MUST end in US-ASCII; that is, a name that ends with a non-ASCII
				ISO-10646 character MUST end with a "-").
			*/

            // Base64 chars, except '/' is replaced with ','
			char[] base64Chars = new char[]{
				'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O','P','Q','R','S','T','U','V','W','X','Y','Z',
				'a','b','c','d','e','f','g','h','i','j','k','l','m','n','o','p','q','r','s','t','u','v','w','x','y','z',
				'0','1','2','3','4','5','6','7','8','9','+',','
			};

			StringBuilder retVal = new StringBuilder();
			for(int i=0;i<text.Length;i++){
				char c = text[i];

				// Encoded block or escaped &
				if(c == '&'){
					int endingPos = -1;
					// Read encoded block
					for(int b=i+1;b<text.Length;b++){
						// - marks block end
						if(text[b] == '-'){
							endingPos = b;
							break;
						}
						// Invalid & sequence, just treat it as '&' char and not like shift.
						// &....&, but must be &....-
						else if(text[b] == '&'){							
							break;
						}
					}
			
					// If no ending -, invalid encoded block. Treat it like it is
					if(endingPos == -1){
						// Just let main for to handle other chars after &
						retVal.Append(c);
					}
					// If empty block, then escaped &
					else if(endingPos - i == 1){
						retVal.Append(c);
						// Move i over '-'
						i++;
					}
					// Decode block
					else{
						// Get encoded block
						byte[] encodedBlock = System.Text.Encoding.Default.GetBytes(text.Substring(i + 1,endingPos - i - 1));
		
						// Convert to UTF-16 char						
						byte[] decodedData = Core.Base64DecodeEx(encodedBlock,base64Chars);
						char[] decodedChars = new char[decodedData.Length / 2];                        
						for(int iC=0;iC<decodedChars.Length;iC++){
							decodedChars[iC] = (char)(decodedData[iC * 2] << 8 | decodedData[(iC * 2) + 1]);
						}
                        
						// Decode data
						retVal.Append(decodedChars);

						// Move i over '-'
						i += encodedBlock.Length + 1;
					}
				}
				// Normal byte
				else{
					retVal.Append(c);
				}
			}

			return retVal.ToString();
		}

		#endregion

		#region method IsAscii

		/// <summary>
		/// Checks if specified string data is acii data.
		/// </summary>
		/// <param name="data"></param>
		/// <returns></returns>
        [Obsolete("Use Net_Utils.IsAscii instead of it")]
		public static bool IsAscii(string data)
		{			
			foreach(char c in data){
				if((int)c > 127){ 
					return false;
				}
			}

			return true;
		}

		#endregion


		#region static method GetFileNameFromPath

		/// <summary>
		/// Gets file name from path.
		/// </summary>
		/// <param name="filePath">File file path with file name. For examples: c:\fileName.xxx, aaa\fileName.xxx.</param>
		/// <returns></returns>
		public static string GetFileNameFromPath(string filePath)
		{
			return Path.GetFileName(filePath);
		}

		#endregion


        #region static method IsIP

        /// <summary>
        /// Gets if specified value is IP address.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <returns>Returns true if specified value is IP address.</returns>
        public static bool IsIP(string value)
        {
            try{
                IPAddress ip = null;
                return IPAddress.TryParse(value,out ip);
            }
            catch{
                return false;
            }
        }

        #endregion

        #region static method CompareIP

        /// <summary>
        /// Compares 2 IP addresses. Returns 0 if IPs are equal, 
        /// returns positive value if destination IP is bigger than source IP,
        /// returns negative value if destination IP is smaller than source IP.
        /// </summary>
        /// <param name="source">Source IP address.</param>
        /// <param name="destination">Destination IP address.</param>
        /// <returns></returns>
        public static int CompareIP(IPAddress source,IPAddress destination)
        {
            byte[] sourceIpBytes      = source.GetAddressBytes();
            byte[] destinationIpBytes = destination.GetAddressBytes();

            // IPv4 and IPv6
            if(sourceIpBytes.Length < destinationIpBytes.Length){
                return 1;
            }
            // IPv6 and IPv4
            else if(sourceIpBytes.Length > destinationIpBytes.Length){
                return -1;
            }
            // IPv4 and IPv4 OR IPv6 and IPv6
            else{                
                for(int i=0;i<sourceIpBytes.Length;i++){
                    if(sourceIpBytes[i] < destinationIpBytes[i]){
                        return 1;
                    }
                    else if(sourceIpBytes[i] > destinationIpBytes[i]){
                        return -1;
                    }
                }

                return 0;
            }
        }

        #endregion

        #region static method IsPrivateIP

        /// <summary>
        /// Gets if specified IP address is private LAN IP address. For example 192.168.x.x is private ip.
        /// </summary>
        /// <param name="ip">IP address to check.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null reference.</exception>
        /// <returns>Returns true if IP is private IP.</returns>        
        [Obsolete("Use Net_Utils.IsPrivateIP instead of it")]
        public static bool IsPrivateIP(string ip)
        {
            if(ip == null){
                throw new ArgumentNullException("ip");
            }

            return IsPrivateIP(IPAddress.Parse(ip));
        }

        /// <summary>
        /// Gets if specified IP address is private LAN IP address. For example 192.168.x.x is private ip.
        /// </summary>
        /// <param name="ip">IP address to check.</param>
        /// <returns>Returns true if IP is private IP.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null reference.</exception>             
        [Obsolete("Use Net_Utils.IsPrivateIP instead of it")]
        public static bool IsPrivateIP(IPAddress ip)
        {
            if(ip == null){
                throw new ArgumentNullException("ip");
            }

			if(ip.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork){
				byte[] ipBytes = ip.GetAddressBytes();

				/* Private IPs:
					First Octet = 192 AND Second Octet = 168 (Example: 192.168.X.X) 
					First Octet = 172 AND (Second Octet >= 16 AND Second Octet <= 31) (Example: 172.16.X.X - 172.31.X.X)
					First Octet = 10 (Example: 10.X.X.X)
					First Octet = 169 AND Second Octet = 254 (Example: 169.254.X.X)

				*/

				if(ipBytes[0] == 192 && ipBytes[1] == 168){
					return true;
				}
				if(ipBytes[0] == 172 && ipBytes[1] >= 16 && ipBytes[1] <= 31){
					return true;
				}
				if(ipBytes[0] == 10){
					return true;
				}
				if(ipBytes[0] == 169 && ipBytes[1] == 254){
					return true;
				}
			}

			return false;
        }

        #endregion


        #region static method CreateSocket

        /// <summary>
        /// Creates new socket for the specified end point.
        /// </summary>
        /// <param name="localEP">Local end point.</param>
        /// <param name="protocolType">Protocol type.</param>
        /// <returns>Retruns newly created socket.</returns>                   
        [Obsolete("Use Net_Utils.CreateSocket instead of it")]
        public static Socket CreateSocket(IPEndPoint localEP,ProtocolType protocolType)
        {
            SocketType socketType = SocketType.Stream;
            if(protocolType == ProtocolType.Udp){
                socketType = SocketType.Dgram;
            }
                        
            if(localEP.AddressFamily == AddressFamily.InterNetwork){
                Socket socket = new Socket(AddressFamily.InterNetwork,socketType,protocolType);
                socket.Bind(localEP);

                return socket;
            }
            else if(localEP.AddressFamily == AddressFamily.InterNetworkV6){
                Socket socket = new Socket(AddressFamily.InterNetworkV6,socketType,protocolType);
                socket.Bind(localEP);

                return socket;
            }
            else{
                throw new ArgumentException("Invalid IPEndPoint address family.");
            }
        }

        #endregion


        #region method ToHex

        /// <summary>
		/// Converts string to hex string.
		/// </summary>
		/// <param name="data">String to convert.</param>
		/// <returns>Returns data as hex string.</returns>
		public static string ToHexString(string data)
		{
            return Encoding.Default.GetString(ToHex(Encoding.Default.GetBytes(data)));
        }

        /// <summary>
		/// Converts string to hex string.
		/// </summary>
		/// <param name="data">Data to convert.</param>
		/// <returns>Returns data as hex string.</returns>  
		public static string ToHexString(byte[] data)
		{
            return Encoding.Default.GetString(ToHex(data));
        }

		/// <summary>
		/// Convert byte to hex data.
		/// </summary>
		/// <param name="byteValue">Byte to convert.</param>
		/// <returns></returns>
		public static byte[] ToHex(byte byteValue)
		{
			return ToHex(new byte[]{byteValue});
		}

		/// <summary>
		/// Converts data to hex data.
		/// </summary>
		/// <param name="data">Data to convert.</param>
		/// <returns></returns>
		public static byte[] ToHex(byte[] data)
		{
			char[] hexChars = new char[]{'0','1','2','3','4','5','6','7','8','9','A','B','C','D','E','F'};

			MemoryStream retVal = new MemoryStream(data.Length * 2);
			foreach(byte b in data){
				byte[] hexByte = new byte[2];
                
				// left 4 bit of byte
				hexByte[0] = (byte)hexChars[(b & 0xF0) >> 4];

				// right 4 bit of byte
				hexByte[1] = (byte)hexChars[b & 0x0F];

				retVal.Write(hexByte,0,2);
			}

			return retVal.ToArray();
		}
                
		#endregion

		#region method FromHex

		/// <summary>
		/// Converts hex byte data to normal byte data. Hex data must be in two bytes pairs, for example: 0F,FF,A3,... .
		/// </summary>
		/// <param name="hexData">Hex data.</param>
		/// <returns></returns>             
        [Obsolete("Use Net_Utils.FromHex instead of it")]
		public static byte[] FromHex(byte[] hexData)
		{
			if(hexData.Length < 2 || (hexData.Length / (double)2 != Math.Floor(hexData.Length / (double)2))){
				throw new Exception("Illegal hex data, hex data must be in two bytes pairs, for example: 0F,FF,A3,... .");
			}

			MemoryStream retVal = new MemoryStream(hexData.Length / 2);
			// Loop hex value pairs
			for(int i=0;i<hexData.Length;i+=2){
				byte[] hexPairInDecimal = new byte[2];
				// We need to convert hex char to decimal number, for example F = 15
				for(int h=0;h<2;h++){
					if(((char)hexData[i + h]) == '0'){
						hexPairInDecimal[h] = 0;
					}
					else if(((char)hexData[i + h]) == '1'){
						hexPairInDecimal[h] = 1;
					}
					else if(((char)hexData[i + h]) == '2'){
						hexPairInDecimal[h] = 2;
					}
					else if(((char)hexData[i + h]) == '3'){
						hexPairInDecimal[h] = 3;
					}
					else if(((char)hexData[i + h]) == '4'){
						hexPairInDecimal[h] = 4;
					}
					else if(((char)hexData[i + h]) == '5'){
						hexPairInDecimal[h] = 5;
					}
					else if(((char)hexData[i + h]) == '6'){
						hexPairInDecimal[h] = 6;
					}
					else if(((char)hexData[i + h]) == '7'){
						hexPairInDecimal[h] = 7;
					}
					else if(((char)hexData[i + h]) == '8'){
						hexPairInDecimal[h] = 8;
					}
					else if(((char)hexData[i + h]) == '9'){
						hexPairInDecimal[h] = 9;
					}
					else if(((char)hexData[i + h]) == 'A' || ((char)hexData[i + h]) == 'a'){
						hexPairInDecimal[h] = 10;
					}
					else if(((char)hexData[i + h]) == 'B' || ((char)hexData[i + h]) == 'b'){
						hexPairInDecimal[h] = 11;
					}
					else if(((char)hexData[i + h]) == 'C' || ((char)hexData[i + h]) == 'c'){
						hexPairInDecimal[h] = 12;
					}
					else if(((char)hexData[i + h]) == 'D' || ((char)hexData[i + h]) == 'd'){
						hexPairInDecimal[h] = 13;
					}
					else if(((char)hexData[i + h]) == 'E' || ((char)hexData[i + h]) == 'e'){
						hexPairInDecimal[h] = 14;
					}
					else if(((char)hexData[i + h]) == 'F' || ((char)hexData[i + h]) == 'f'){
						hexPairInDecimal[h] = 15;
					}
				}

				// Join hex 4 bit(left hex cahr) + 4bit(right hex char) in bytes 8 it
				retVal.WriteByte((byte)((hexPairInDecimal[0] << 4) | hexPairInDecimal[1]));
			}

			return retVal.ToArray();
		}

		#endregion


        #region static method ComputeMd5

        /// <summary>
        /// Computes md5 hash.
        /// </summary>
        /// <param name="text">Text to hash.</param>
        /// <param name="hex">Specifies if md5 value is returned as hex string.</param>
        /// <returns>Resturns md5 value or md5 hex value.</returns>              
        [Obsolete("Use Net_Utils.ComputeMd5 instead of it")]
        public static string ComputeMd5(string text,bool hex)
        {
            System.Security.Cryptography.MD5 md5 = new System.Security.Cryptography.MD5CryptoServiceProvider();			
			byte[] hash = md5.ComputeHash(Encoding.Default.GetBytes(text));

            if(hex){
			    return ToHexString(System.Text.Encoding.Default.GetString(hash)).ToLower();
            }
            else{
                return System.Text.Encoding.Default.GetString(hash);
            }
        }

        #endregion

    }
}
