// -----------------------------------------------------------------------
//
//   Copyright (C) 2003-2006 Angel Marin
// 
//   This file is part of SharpMimeTools
//
//   SharpMimeTools is free software; you can redistribute it and/or
//   modify it under the terms of the GNU Lesser General Public
//   License as published by the Free Software Foundation; either
//   version 2.1 of the License, or (at your option) any later version.
//
//   SharpMimeTools is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//   Lesser General Public License for more details.
//
//   You should have received a copy of the GNU Lesser General Public
//   License along with SharpMimeTools; if not, write to the Free Software
//   Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA  02110-1301  USA
//
// -----------------------------------------------------------------------

using System;

namespace anmar.SharpMimeTools
{
	/// <summary>
	/// 
	/// </summary>
	public class SharpMimeTools {
#if LOG
		private static log4net.ILog log  = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);
#endif
		private static System.String[] _date_formats = new System.String[] {
																@"dddd, d MMM yyyy H:m:s zzz", @"ddd, d MMM yyyy H:m:s zzz", @"d MMM yyyy H:m:s zzz",
																@"dddd, d MMM yy H:m:s zzz", @"ddd, d MMM yy H:m:s zzz", @"d MMM yy H:m:s zzz",
																@"dddd, d MMM yyyy H:m zzz", @"ddd, d MMM yyyy H:m zzz", @"d MMM yyyy H:m zzz",
																@"dddd, d MMM yy H:m zzz", @"ddd, d MMM yy H:m zzz", @"d MMM yy H:m zzz"
			};
		
		internal static System.String GetFileName ( System.String name ) {
			if ( name==null || name.Length==0 )
				return name;
			name = name.Replace("\t", "");
			try {
				name = System.IO.Path.GetFileName(name);
			} catch ( System.ArgumentException ) {
				// Remove invalid chars
				foreach ( char ichar in System.IO.Path.GetInvalidPathChars() ) {
					name = name.Replace ( ichar.ToString(), System.String.Empty );
				}
				name = System.IO.Path.GetFileName(name);
			}
			try {
				System.IO.FileInfo fi = new System.IO.FileInfo(name);
				if ( fi!=null )
					fi = null;
			} catch ( System.ArgumentException ) {
				name = null;
#if LOG
				if ( log.IsErrorEnabled ) {
					log.Error(System.String.Concat("Filename [", name, "] is not allowed by the filesystem"));
				}
#endif
			}
			return name;
		}
		private static bool IsValidHexChar ( System.Char ch ) {
			return ( (ch>0x2F && ch<0x3A) || (ch>0x40 && ch<0x47) || (ch>0x60 && ch<0x67) );
		}
		/// <summary>
		/// Parses a <see cref="System.Text.Encoding" /> from a charset name
		/// </summary>
		/// <param name="charset">charset to parse</param>
		/// <returns>A <see cref="System.Text.Encoding" /> that represents the given <c>charset</c></returns>
		public static System.Text.Encoding parseCharSet ( System.String charset ) {
			if ( charset==null || charset.Length==0 )
				return null;
			try {
				return System.Text.Encoding.GetEncoding (charset);
#if LOG
			} catch ( System.Exception e ) {
				if ( log.IsErrorEnabled )
					log.Error(System.String.Concat("Error parsing charset: [", charset, "]"), e);
#else
			} catch ( System.Exception ) {
#endif
				return null;
			}
		}
		internal static System.Enum ParseEnum ( System.Type t, System.Object s, System.Enum defaultvalue ) {
			if ( s==null )
				return defaultvalue;
			System.Enum value = defaultvalue;
			if ( System.Enum.IsDefined(t, s) ) {
				value = (System.Enum)System.Enum.Parse(t, s.ToString());
			}
			return value;
		}
		/// <summary>
		/// Parse a rfc 2822 address specification. rfc 2822 section 3.4
		/// </summary>
		/// <param name="from">field body to parse</param>
		/// <returns>A <see cref="System.Collections.IEnumerable" /> collection of <see cref="anmar.SharpMimeTools.SharpMimeAddress" /></returns>
		public static System.Collections.IEnumerable parseFrom ( System.String from ) {
			return anmar.SharpMimeTools.SharpMimeAddressCollection.Parse (from);
		}
		/// <summary>
		/// Parse a rfc 2822 name-address specification. rfc 2822 section 3.4
		/// </summary>
		/// <param name="from">address</param>
		/// <param name="part">1 is display-name; 2 is addr-spec</param>
		/// <returns>the requested <see cref="System.String" /></returns>
		public static System.String parseFrom ( System.String from, int part ) {
			int pos;
			if ( from==null || from.Length<1) {
				return System.String.Empty;
			}
			switch (part) {
				case 1:
					pos = from.LastIndexOf('<');
					pos = (pos<0)?from.Length:pos;
					from = from.Substring (0, pos).Trim();
					from = anmar.SharpMimeTools.SharpMimeTools.parserfc2047Header ( from );
					return from;
				case 2:
					pos = from.LastIndexOf('<')+1;
					return from.Substring(pos, from.Length-pos).Trim(new char[]{'<','>',' '});
			}
			return from;
		}
		/// <summary>
		/// Parse a rfc 2822 date and time specification. rfc 2822 section 3.3
		/// </summary>
		/// <param name="date">rfc 2822 date-time</param>
		/// <returns>A <see cref="System.DateTime" /> from the parsed header body</returns>
		public static System.DateTime parseDate ( System.String date ) {
			if ( date==null || date.Equals(System.String.Empty) )
				return System.DateTime.MinValue;
			System.DateTime msgDateTime;
			date = anmar.SharpMimeTools.SharpMimeTools.uncommentString (date);
			msgDateTime = new System.DateTime (0);
			try {
				// TODO: Complete the list
				date = date.Replace("UT", "+0000");
				date = date.Replace("GMT", "+0000");
				date = date.Replace("EDT", "-0400");
				date = date.Replace("EST", "-0500");
				date = date.Replace("CDT", "-0500");
				date = date.Replace("MDT", "-0600");
				date = date.Replace("MST", "-0600");
				date = date.Replace("EST", "-0700");
				date = date.Replace("PDT", "-0700");
				date = date.Replace("PST", "-0800");

				date = date.Replace("AM", System.String.Empty);
				date = date.Replace("PM", System.String.Empty);
				int rpos = date.LastIndexOfAny(new Char[]{' ', '\t'});
				if (rpos>0 && rpos != date.Length - 6)
					date = date.Substring(0, rpos + 1) + "-0000";
				date = date.Insert(date.Length-2, ":");
				msgDateTime = DateTime.ParseExact(date, 
					_date_formats,
					System.Globalization.CultureInfo.CreateSpecificCulture("en-us"),
					System.Globalization.DateTimeStyles.AllowInnerWhite);
#if LOG
			} catch ( System.Exception e ) {
				if ( log.IsErrorEnabled )
					log.Error(System.String.Concat("Error parsing date: [", date, "]"), e);
#else
			} catch ( System.Exception ) {
#endif
				msgDateTime = new System.DateTime (0);
			}
			return msgDateTime;
		}
		/// <summary>
		/// Parse a rfc 2822 header field with parameters
		/// </summary>
		/// <param name="field">field name</param>
		/// <param name="fieldbody">field body to parse</param>
		/// <returns>A <see cref="System.Collections.Specialized.StringDictionary" /> from the parsed field body</returns>
		public static System.Collections.Specialized.StringDictionary parseHeaderFieldBody ( System.String field, System.String fieldbody ) {
			if ( fieldbody==null )
				return null;
			// FIXME: rewrite parseHeaderFieldBody to being regexp based.
			fieldbody = anmar.SharpMimeTools.SharpMimeTools.uncommentString (fieldbody);
			System.Collections.Specialized.StringDictionary fieldbodycol = new System.Collections.Specialized.StringDictionary ();
			System.String[] words = fieldbody.Split(new Char[]{';'});
			if ( words.Length>0 ) {
				fieldbodycol.Add (field.ToLower(), words[0].ToLower().Trim());
				for (int i=1; i<words.Length; i++ ) {
					System.String[] param = words[i].Trim(new Char[]{' ', '\t'}).Split(new Char[]{'='}, 2);
					if ( param.Length==2 ) {
						param[0] = param[0].Trim(new Char[]{' ', '\t'});
						param[1] = param[1].Trim(new Char[]{' ', '\t'});
						if ( param[1].StartsWith("\"") && !param[1].EndsWith("\"")) {
							do {
								param[1] += ";" + words[++i];
							} while  ( !words[i].EndsWith("\"") && i<words.Length);
						}
						fieldbodycol.Add ( param[0], anmar.SharpMimeTools.SharpMimeTools.parserfc2047Header (param[1].TrimEnd(';').Trim('\"', ' ')) );
					}
				}
			}
			return fieldbodycol;
		}
		/// <summary>
		/// Parse and decode rfc 2047 header body
		/// </summary>
		/// <param name="header">header body to parse</param>
		/// <returns>parsed <see cref="System.String" /></returns>
		public static System.String parserfc2047Header ( System.String header ) {
			header = header.Replace ("\"", System.String.Empty);
			header = anmar.SharpMimeTools.SharpMimeTools.rfc2047decode(header);
			return header;
		}
		/// <summary>
		/// Decode rfc 2047 definition of quoted-printable
		/// </summary>
		/// <param name="charset">charset to use when decoding</param>
		/// <param name="orig"><see cref="System.String" /> to decode</param>
		/// <returns>the decoded <see cref="System.String" /></returns>
		public static System.String QuotedPrintable2Unicode ( System.String charset, System.String orig ) {
			System.Text.Encoding enc = anmar.SharpMimeTools.SharpMimeTools.parseCharSet (charset);
			return anmar.SharpMimeTools.SharpMimeTools.QuotedPrintable2Unicode ( enc, orig );
		}
		/// <summary>
		/// Decode rfc 2047 definition of quoted-printable
		/// </summary>
		/// <param name="enc"><see cref="System.Text.Encoding" /> to use</param>
		/// <param name="orig"><see cref="System.String" /> to decode</param>
		/// <returns>the decoded <see cref="System.String" /></returns>
		public static System.String QuotedPrintable2Unicode ( System.Text.Encoding enc, System.String orig ) {
			if ( enc==null || orig==null )
				return orig;

			System.Text.StringBuilder decoded = new System.Text.StringBuilder(orig);
			int bytecount=0, offset=0;
			System.Byte[] ch = new System.Byte[24];
			for ( int i=0, total=decoded.Length; i<total; ) {
				// Possible encoded byte
				if ( decoded[i] == '=' && (total-i)>2 ) {
					System.String hex = decoded.ToString(i+1, 2);
					// encoded byte
					if ( IsValidHexChar(hex[0]) && IsValidHexChar(hex[1]) ) {
						ch[bytecount++] = System.Convert.ToByte(hex, 16);
						offset+=3;
						i+=3;
					// soft line break
					} else if ( hex==ABNF.CRLF ) {
						offset+=3;
						i+=3;
					// there shouldn't be a '=' without being encoded, so we remove it
					} else {
						offset++;
						i++;
					}
					// Replace chars with decoded bytes if we have finished the series of encoded bytes
					// or have filled the buffer.
					if ( offset>0 && (bytecount==24 || i==total || (total-i)<3 || decoded[i]!='=') ) {
						i-=offset;
						total-=offset;
						decoded.Remove(i, offset);
						if ( bytecount>0 ) {
							System.String decodedItem = enc.GetString(ch, 0, bytecount);
							decoded.Insert(i, decodedItem);
							i+=decodedItem.Length;
							total+=decodedItem.Length;
						}
						bytecount=0;
						offset=0;
					}
				} else {
					i++;
				}
			}
			return decoded.ToString();
		}
		/// <summary>
		/// rfc 2047 header body decoding
		/// </summary>
		/// <param name="word"><c>string</c> to decode</param>
		/// <returns>the decoded <see cref="System.String" /></returns>
		public static System.String rfc2047decode ( System.String word ) {
			System.String[] words;
			System.String[] wordetails;

			System.Text.RegularExpressions.Regex rfc2047format = new System.Text.RegularExpressions.Regex (@"(=\?[\-a-zA-Z0-9]+\?[qQbB]\?[a-zA-Z0-9=_\-\.$%&/\'\\!:;{}\+\*\|@#~`^\(\)]+\?=)\s*", System.Text.RegularExpressions.RegexOptions.ECMAScript);
			// No rfc2047 format
			if ( !rfc2047format.IsMatch (word) ){
#if LOG
				if ( log.IsDebugEnabled )
					log.Debug ("Not a RFC 2047 string: " + word);
#endif
				return word;
			}
#if LOG
			if ( log.IsDebugEnabled )
				log.Debug ("Decoding 2047 string: " + word);
#endif
			words = rfc2047format.Split ( word );
			word = System.String.Empty;
			rfc2047format = new System.Text.RegularExpressions.Regex (@"=\?([\-a-zA-Z0-9]+)\?([qQbB])\?([a-zA-Z0-9=_\-\.$%&/\'\\!:;{}\+\*\|@#~`^\(\)]+)\?=", System.Text.RegularExpressions.RegexOptions.ECMAScript);
			for ( int i=0; i<words.GetLength (0); i++ ) {
				if ( !rfc2047format.IsMatch (words[i]) ){
					word += words[i];
					continue;
				}
				wordetails = rfc2047format.Split ( words[i] );

				switch (wordetails[2]) {
					case "q":
					case "Q":
						word += anmar.SharpMimeTools.SharpMimeTools.QuotedPrintable2Unicode ( wordetails[1], wordetails[3] ).Replace ('_', ' ');;
						break;
					case "b":
					case "B":
						try {
							System.Text.Encoding enc = System.Text.Encoding.GetEncoding (wordetails[1]);
							System.Byte[] ch = System.Convert.FromBase64String(wordetails[3]);
							word += enc.GetString (ch);
						} catch ( System.Exception ) {
						}
						break;
				}
			}
#if LOG
			if ( log.IsDebugEnabled )
				log.Debug ("Decoded 2047 string: " + word);
#endif
			return word;
		}
		/// <summary>
		/// Remove rfc 2822 comments
		/// </summary>
		/// <param name="fieldValue"><c>string</c> to uncomment</param>
		/// <returns></returns>
		// TODO: refactorize this
		public static System.String uncommentString ( System.String fieldValue ) {
			if ( fieldValue==null || fieldValue.Equals(System.String.Empty) )
				return fieldValue;
			if ( fieldValue.IndexOf('(')==-1 )
				return fieldValue.Trim();
			const int a = 0;
			const int b = 1;
			const int c = 2;

			System.Text.StringBuilder buf = new System.Text.StringBuilder();
			int leftSqureCount = 0;
			bool isQuotedPair = false;
			int state = a;

			for (int i = 0; i < fieldValue.Length; i ++) {
				switch (state) {
					case a:
						if (fieldValue[i] == '"') {
							state = c;
							System.Diagnostics.Debug.Assert(!isQuotedPair, "quoted-pair");
						}
						else if (fieldValue[i] == '(') {
							state = b;
							leftSqureCount ++;
							System.Diagnostics.Debug.Assert(!isQuotedPair, "quoted-pair");
						}
						break;
					case b:
						if (!isQuotedPair) {
							if (fieldValue[i] == '(')
								leftSqureCount ++;
							else if (fieldValue[i] == ')') {
								leftSqureCount --;
								if (leftSqureCount == 0) {
									buf.Append(' ');
									state = a;
									continue;
								}
							}
						}
						break;
					case c:
						if (!isQuotedPair) {
							if (fieldValue[i] == '"')
								state = a;
						}
						break;
					default:
						break;
				}

				if (state != a) { //quoted-pair
					if (isQuotedPair)
						isQuotedPair = false;
					else
						isQuotedPair = fieldValue[i] == '\\';
				}
				if (state != b)
					buf.Append(fieldValue[i]);
			}
      
			return buf.ToString().Trim();

		}
		/// <summary>
		/// Encodes a Message-ID or Content-ID following RFC 2392 rules. 
		/// </summary>
		/// <param name="input"><see cref="System.String" /> with the Message-ID or Content-ID.</param>
		/// <returns><see cref="System.String" /> with the value encoded as RFC 2392 dictates.</returns> 
		public static System.String Rfc2392Url ( System.String input) {
			if ( input==null || input.Length<4 )
				return input;
			if ( input.Length>2 && input[0]=='<' && input[input.Length-1]=='>' )
				input = input.Substring(1, input.Length-2);
			if ( input.IndexOf('/')!=-1 ) {
				input = input.Replace("/", "%2f");
			}
			return input;
		}
		/// <summary>
		/// Decodes the provided uuencoded string. 
		/// </summary>
		/// <param name="input"><see cref="System.String" /> with the uuendoced content.</param>
		/// <returns>A <see cref="System.Byte" /> <see cref="System.Array" /> with the uudecoded content. Or the <b>null</b> reference if content can't be decoded.</returns>
		/// <remarks>The input string must contain the <b>begin</b> and <b>end</b> delimiters.</remarks>
		public static System.Byte[] UuDecode ( System.String input ) {
			if ( input==null || input.Length==0 )
				return null;
			System.IO.StringReader reader = new System.IO.StringReader(input);
			System.IO.MemoryStream stream = null;
			System.Byte[] output = null;
			for ( System.String line=reader.ReadLine(); line!=null; line=reader.ReadLine() ) {
				// Found the start point of uuencoded content
				if ( line.Length>10 && line[0]=='b' && line[1]=='e' && line[2]=='g' && line[3]=='i' && line[4]=='n' && line[5]==' ' && line[9]==' ' ) {
					stream = new System.IO.MemoryStream();
					continue;
				}
				// Not within uuencoded content
				if ( stream==null )
					continue;
				// Content finished
				if ( line.Length==3 && line=="end" ) {
					stream.Flush();
					output = stream.ToArray();
					stream.Close();
					stream = null;
					break;
				}
				// Decode and write uuencoded line
				UuDecodeLine(line, stream);
			}
			reader.Close();
			reader = null;
			if ( stream!=null ) {
				stream.Close();
				stream = null;
			}
			return output;
		}
		/// <summary>
		/// Decodes the provided uuencoded line. 
		/// </summary>
		/// <param name="s"><see cref="System.String" /> with the uuendoced line.</param>
		/// <param name="stream"><see cref="System.IO.Stream" /> where decoded <see cref="System.Byte" /> should be written.</param>
		/// <returns><b>true</b> if content has been decoded and <b>false</b> otherwise.</returns>
		public static bool UuDecodeLine ( System.String s, System.IO.Stream stream ) {
			if ( s==null || s.Length==0 || stream==null || !stream.CanWrite )
				return false;
			System.Byte[] input = System.Text.Encoding.ASCII.GetBytes(s);
			int length = input.Length;
			int length_output = 0;
			// Full line, so it has length info in the first byte
			if ( (length%4)==1 ) {
				length_output = ((input[0]-0x20) & 0x3f);
			}
			// Wrong input
			if ( length==0 || length_output<=0 )
				return false;
			// Decode each four characters of input to three octets of output
			for ( int i=1, pos=0; i<length; i+=4 ) {
				System.Byte b = (byte)((input[i+1]-0x20) & 0x3f);
				System.Byte c = (byte)((input[i+2]-0x20) & 0x3f);
				stream.WriteByte((byte)(((input[i]-0x20) & 0x3f)<<2|b>>4));
				pos++;
				if ( pos<length_output ) {
					stream.WriteByte((byte)(b<<4|c>>2));
					pos++;
				}
				if ( pos<length_output ) {
					stream.WriteByte((byte)(c<<6|((input[i+3]-0x20) & 0x3f)));
					pos++;
				}
			}
			return true;
		}
	}
}
