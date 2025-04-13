using System;
using System.IO;
using System.Text;

namespace LumiSoft.Net.Mime
{
	/// <summary>
	/// Provides mime related utility methods.
	/// </summary>
    [Obsolete("See LumiSoft.Net.MIME or LumiSoft.Net.Mail namepaces for replacement.")]
	public class MimeUtils
	{
		#region static method ParseDate

        // TODO get rid of this method, only IMAP uses it

		/// <summary>
		/// Parses rfc 2822 datetime.
		/// </summary>
		/// <param name="date">Date string.</param>
		/// <returns></returns>
		public static DateTime ParseDate(string date)
		{
			/* Rfc 2822 3.3. Date and Time Specification.			 
				date-time       = [ day-of-week "," ] date FWS time [CFWS]
				date            = day month year
				time            = hour ":" minute [ ":" second ] FWS zone
			*/

			/* IMAP date format. 
			    date-time       = date FWS time [CFWS]
				date            = day-month-year
				time            = hour ":" minute [ ":" second ] FWS zone
			*/

            // zone = (( "+" / "-" ) 4DIGIT)

			//--- Replace timezone constants -------//
			/*
             UT  -0000            
			GMT  -0000
			EDT  -0400
			EST  -0500
			CDT  -0500
			CST  -0600
			MDT  -0600
			MST  -0700
			PDT  -0700
			PST  -0800			
			BST  +0100 British Summer Time
			*/

			date = date.ToLower();
            date = date.Replace("ut","-0000");
			date = date.Replace("gmt","-0000");
			date = date.Replace("edt","-0400");
			date = date.Replace("est","-0500");
			date = date.Replace("cdt","-0500");
			date = date.Replace("cst","-0600");
			date = date.Replace("mdt","-0600");
			date = date.Replace("mst","-0700");
			date = date.Replace("pdt","-0700");
			date = date.Replace("pst","-0800");
			date = date.Replace("bst","+0100");
			//----------------------------------------//

			//--- Replace month constants ---//
			date = date.Replace("jan","01");
			date = date.Replace("feb","02");
			date = date.Replace("mar","03");
			date = date.Replace("apr","04");
			date = date.Replace("may","05");
			date = date.Replace("jun","06");
			date = date.Replace("jul","07");
			date = date.Replace("aug","08");
			date = date.Replace("sep","09");
			date = date.Replace("oct","10");
			date = date.Replace("nov","11");
			date = date.Replace("dec","12");
			//-------------------------------//

			//  If date contains optional "day-of-week,", remove it
			if(date.IndexOf(',') > -1){
				date = date.Substring(date.IndexOf(',') + 1);
			}

			// Remove () from date. "Mon, 13 Oct 2003 20:50:57 +0300 (EEST)"
			if(date.IndexOf(" (") > -1){
				date = date.Substring(0,date.IndexOf(" ("));
			}
                        
            int year        = 1900;
            int month       = 1;
            int day         = 1;
            int hour        = -1;
            int minute      = -1;
            int second      = -1;
            int zoneMinutes = -1;

            StringReader s = new StringReader(date);

            //--- Pase date --------------------------------------------------------------------//
            try{
                day = Convert.ToInt32(s.ReadWord(true,new char[]{'.','-',' '},true));
            }
            catch{
                throw new Exception("Invalid date value '" + date + "', invalid day value !");
            }

            try{
                month = Convert.ToInt32(s.ReadWord(true,new char[]{'.','-',' '},true));
            }
            catch{
                throw new Exception("Invalid date value '" + date + "', invalid month value !");
            }

            try{
                year = Convert.ToInt32(s.ReadWord(true,new char[]{'.','-',' '},true));
            }
            catch{
                throw new Exception("Invalid date value '" + date + "', invalid year value !");
            }
            //----------------------------------------------------------------------------------//

            //--- Parse time -------------------------------------------------------------------//
            // Time is optional, so parse it if its included.
            if(s.Available > 0){
                try{
                    hour = Convert.ToInt32(s.ReadWord(true,new char[]{':'},true));                
                }
                catch{
                    throw new Exception("Invalid date value '" + date + "', invalid hour value !");
                }

                try{
                    minute = Convert.ToInt32(s.ReadWord(true,new char[]{':'},false));
                }
                catch{
                    throw new Exception("Invalid date value '" + date + "', invalid minute value !");
                }

                s.ReadToFirstChar();
                if(s.StartsWith(":")){
                    s.ReadSpecifiedLength(1);
                    try{
                        string secondString = s.ReadWord(true,new char[]{' '},true);
                        // Milli seconds specified, remove them.
                        if(secondString.IndexOf('.') > -1){
                            secondString = secondString.Substring(0,secondString.IndexOf('.'));
                        }
                        second = Convert.ToInt32(secondString);
                    }
                    catch{
                        throw new Exception("Invalid date value '" + date + "', invalid second value !");
                    }
                }

                s.ReadToFirstChar();
                if(s.Available > 3){
                    string timezone = s.SourceString.Replace(":","");
                    if(timezone.StartsWith("+") || timezone.StartsWith("-")){
                        bool utc_add_time = timezone.StartsWith("+");

                        // Remove +/- sign
                        timezone = timezone.Substring(1);
                        
                        // padd time zone to 4 symbol. For example 200, will be 0200.
                        while(timezone.Length < 4){
                            timezone = "0" + timezone;
                        }

                        try{
                            // time zone format hours|minutes
                            int h = Convert.ToInt32(timezone.Substring(0,2));
                            int m = Convert.ToInt32(timezone.Substring(2));

                            if(utc_add_time){
                                zoneMinutes = 0 - ((h * 60) + m);
                            }
                            else{
                                zoneMinutes = (h * 60) + m;
                            }
                        }
                        catch{ // Just skip time zone, if can't parse
                        }
                    }
                }
            }
            //---------------------------------------------------------------------------------//
                                    
            // Convert time to UTC
            if(hour != -1 && minute != -1 && second != -1){
                DateTime d = new DateTime(year,month,day,hour,minute,second).AddMinutes(zoneMinutes);
                return new DateTime(d.Year,d.Month,d.Day,d.Hour,d.Minute,d.Second,DateTimeKind.Utc).ToLocalTime();
            }
            else{
                return new DateTime(year,month,day);
            }
		}

		#endregion

		#region static method DateTimeToRfc2822

		/// <summary>
		/// Converts date to rfc 2822 date time string.
		/// </summary>
		/// <param name="dateTime">Date time value.</param>
		/// <returns></returns>
		public static string DateTimeToRfc2822(DateTime dateTime)
		{
			return dateTime.ToUniversalTime().ToString("r",System.Globalization.DateTimeFormatInfo.InvariantInfo);
		}

		#endregion


		#region static method ParseHeaders

		/// <summary>
		/// Parses headers from message or mime entry.
		/// </summary>
		/// <param name="entryStrm">Stream from where to read headers.</param>
		/// <returns>Returns header lines.</returns>
		public static string ParseHeaders(Stream entryStrm)
		{
			/* Rfc 2822 3.1.  GENERAL DESCRIPTION
				A message consists of header fields and, optionally, a body.
				The  body  is simply a sequence of lines containing ASCII charac-
				ters.  It is separated from the headers by a null line  (i.e.,  a
				line with nothing preceding the CRLF).
			*/

			byte[] crlf = new byte[]{(byte)'\r',(byte)'\n'};
			MemoryStream msHeaders = new MemoryStream();
			StreamLineReader r = new StreamLineReader(entryStrm);
			byte[] lineData = r.ReadLine();
			while(lineData != null){
				if(lineData.Length == 0){
					break;
				}

				msHeaders.Write(lineData,0,lineData.Length);
				msHeaders.Write(crlf,0,crlf.Length);
				lineData = r.ReadLine();
			}

			return System.Text.Encoding.Default.GetString(msHeaders.ToArray());
		}

		#endregion

		#region static method ParseHeaderField

		/// <summary>
		/// Parse header specified header field value.
		/// 
		/// Use this method only if you need to get only one header field, otherwise use
		/// MimeParser.ParseHeaderField(string fieldName,string headers).
		/// This avoid parsing headers multiple times.
		/// </summary>
		/// <param name="fieldName">Header field which to parse. Eg. Subject: .</param>
		/// <param name="entryStrm">Stream from where to read headers.</param>
		/// <returns></returns>
		public static string ParseHeaderField(string fieldName,Stream entryStrm)
		{
			return ParseHeaderField(fieldName,ParseHeaders(entryStrm));
		}

		/// <summary>
		/// Parse header specified header field value.
		/// </summary>
		/// <param name="fieldName">Header field which to parse. Eg. Subject: .</param>
		/// <param name="headers">Full headers string. Use MimeParser.ParseHeaders() to get this value.</param>
		public static string ParseHeaderField(string fieldName,string headers)
		{
			/* Rfc 2822 2.2 Header Fields
				Header fields are lines composed of a field name, followed by a colon
				(":"), followed by a field body, and terminated by CRLF.  A field
				name MUST be composed of printable US-ASCII characters (i.e.,
				characters that have values between 33 and 126, inclusive), except
				colon.  A field body may be composed of any US-ASCII characters,
				except for CR and LF.  However, a field body may contain CRLF when
				used in header "folding" and  "unfolding" as described in section
				2.2.3.  All field bodies MUST conform to the syntax described in
				sections 3 and 4 of this standard. 
				
			   Rfc 2822 2.2.3 (Multiline header fields)
				The process of moving from this folded multiple-line representation
				of a header field to its single line representation is called
				"unfolding". Unfolding is accomplished by simply removing any CRLF
				that is immediately followed by WSP.  Each header field should be
				treated in its unfolded form for further syntactic and semantic
				evaluation.
				
				Example:
					Subject: aaaaa<CRLF>
					<TAB or SP>aaaaa<CRLF>
			*/

			using(TextReader r = new StreamReader(new MemoryStream(System.Text.Encoding.Default.GetBytes(headers)))){
				string line = r.ReadLine();
				while(line != null){
					// Find line where field begins
					if(line.ToUpper().StartsWith(fieldName.ToUpper())){
						// Remove field name and start reading value
						string fieldValue = line.Substring(fieldName.Length).Trim();

						// see if multi line value. See commnt above.
						line = r.ReadLine();
						while(line != null && (line.StartsWith("\t") || line.StartsWith(" "))){
							fieldValue += line;
							line = r.ReadLine();
						}

						return fieldValue;
					}

					line = r.ReadLine();
				}
			}

			return "";
		}

		#endregion

		#region static function ParseHeaderFiledParameter

		/// <summary>
		/// Parses header field parameter value. 
		/// For example: CONTENT-TYPE: application\octet-stream; name="yourFileName.xxx",
		/// fieldName="CONTENT-TYPE:" and subFieldName="name".
		/// </summary>
		/// <param name="fieldName">Main header field name.</param>
		/// <param name="parameterName">Header field's parameter name.</param>
		/// <param name="headers">Full headrs string.</param>
		/// <returns></returns>
		public static string ParseHeaderFiledParameter(string fieldName,string parameterName,string headers)
		{
			string mainFiled = ParseHeaderField(fieldName,headers);
			// Parse sub field value
			if(mainFiled.Length > 0){
				int index = mainFiled.ToUpper().IndexOf(parameterName.ToUpper());
				if(index > -1){	
					mainFiled = mainFiled.Substring(index + parameterName.Length + 1); // Remove "subFieldName="

					// subFieldName value may be in "" and without
					if(mainFiled.StartsWith("\"")){						
						return mainFiled.Substring(1,mainFiled.IndexOf("\"",1) - 1);
					}
					// value without ""
					else{
						int endIndex = mainFiled.Length;
						if(mainFiled.IndexOf(" ") > -1){
							endIndex = mainFiled.IndexOf(" ");
						}

						return mainFiled.Substring(0,endIndex);
					}						
				}
			}
			
			return "";			
		}

		#endregion


		#region static method ParseMediaType

		/// <summary>
		/// Parses MediaType_enum from <b>Content-Type:</b> header field value.
		/// </summary>
		/// <param name="headerFieldValue"><b>Content-Type:</b> header field value. This value can be null, then MediaType_enum.NotSpecified.</param>
		/// <returns></returns>
		public static MediaType_enum ParseMediaType(string headerFieldValue)
		{
			if(headerFieldValue == null){
				return MediaType_enum.NotSpecified;
			}

			string contentType = TextUtils.SplitString(headerFieldValue,';')[0].ToLower();
			//--- Text/xxx --------------------------------//
			if(contentType.IndexOf("text/plain") > -1){
				return MediaType_enum.Text_plain;
			}
			else if(contentType.IndexOf("text/html") > -1){
				return MediaType_enum.Text_html;
			}
			else if(contentType.IndexOf("text/xml") > -1){
				return MediaType_enum.Text_xml;
			}
			else if(contentType.IndexOf("text/rtf") > -1){
				return MediaType_enum.Text_rtf;
			}
			else if(contentType.IndexOf("text") > -1){
				return MediaType_enum.Text;
			}
			//---------------------------------------------//

			//--- Image/xxx -------------------------------//
			else if(contentType.IndexOf("image/gif") > -1){
				return MediaType_enum.Image_gif;
			}
			else if(contentType.IndexOf("image/tiff") > -1){
				return MediaType_enum.Image_tiff;
			}
			else if(contentType.IndexOf("image/jpeg") > -1){
				return MediaType_enum.Image_jpeg;
			}
			else if(contentType.IndexOf("image") > -1){
				return MediaType_enum.Image;
			}
			//---------------------------------------------//

			//--- Audio/xxx -------------------------------//
			else if(contentType.IndexOf("audio") > -1){
				return MediaType_enum.Audio;
			}
			//---------------------------------------------//

			//--- Video/xxx -------------------------------//
			else if(contentType.IndexOf("video") > -1){
				return MediaType_enum.Video;
			}
			//---------------------------------------------//

			//--- Application/xxx -------------------------//
			else if(contentType.IndexOf("application/octet-stream") > -1){
				return MediaType_enum.Application_octet_stream;
			}
			else if(contentType.IndexOf("application") > -1){
				return MediaType_enum.Application;
			}
			//---------------------------------------------//

			//--- Multipart/xxx ---------------------------//
			else if(contentType.IndexOf("multipart/mixed") > -1){
				return MediaType_enum.Multipart_mixed;
			}
			else if(contentType.IndexOf("multipart/alternative") > -1){
				return MediaType_enum.Multipart_alternative;
			}
			else if(contentType.IndexOf("multipart/parallel") > -1){
				return MediaType_enum.Multipart_parallel;
			}
			else if(contentType.IndexOf("multipart/related") > -1){
				return MediaType_enum.Multipart_related;
			}
			else if(contentType.IndexOf("multipart/signed") > -1){
				return MediaType_enum.Multipart_signed;
			}
			else if(contentType.IndexOf("multipart") > -1){
				return MediaType_enum.Multipart;
			}
			//---------------------------------------------//

			//--- Message/xxx -----------------------------//
			else if(contentType.IndexOf("message/rfc822") > -1){
				return MediaType_enum.Message_rfc822;
			}
			else if(contentType.IndexOf("message") > -1){
				return MediaType_enum.Message;
			}
			//---------------------------------------------//

			else{
				return MediaType_enum.Unknown;
			}
		}

		#endregion

		#region static method MediaTypeToString

		/// <summary>
		/// Converts MediaType_enum to string. NOTE: Returns null for MediaType_enum.NotSpecified.
		/// </summary>
		/// <param name="mediaType">MediaType_enum value to convert.</param>
		/// <returns></returns>
		public static string MediaTypeToString(MediaType_enum mediaType)
		{
			//--- Text/xxx --------------------------------//
			if(mediaType == MediaType_enum.Text_plain){
				return "text/plain";
			}
			else if(mediaType == MediaType_enum.Text_html){
				return "text/html";
			}
			else if(mediaType == MediaType_enum.Text_xml){
				return "text/xml";
			}
			else if(mediaType == MediaType_enum.Text_rtf){
				return "text/rtf";
			}
			else if(mediaType == MediaType_enum.Text){
				return "text";
			}
			//---------------------------------------------//

			//--- Image/xxx -------------------------------//
			else if(mediaType == MediaType_enum.Image_gif){
				return "image/gif";
			}
			else if(mediaType == MediaType_enum.Image_tiff){
				return "image/tiff";
			}
			else if(mediaType == MediaType_enum.Image_jpeg){
				return "image/jpeg";
			}
			else if(mediaType == MediaType_enum.Image){
				return "image";
			}
			//---------------------------------------------//

			//--- Audio/xxx -------------------------------//
			else if(mediaType == MediaType_enum.Audio){
				return "audio";
			}
			//---------------------------------------------//

			//--- Video/xxx -------------------------------//
			else if(mediaType == MediaType_enum.Video){
				return "video";
			}
			//---------------------------------------------//

			//--- Application/xxx -------------------------//
			else if(mediaType == MediaType_enum.Application_octet_stream){
				return "application/octet-stream";
			}
			else if(mediaType == MediaType_enum.Application){
				return "application";
			}
			//---------------------------------------------//

			//--- Multipart/xxx ---------------------------//
			else if(mediaType == MediaType_enum.Multipart_mixed){
				return "multipart/mixed";
			}
			else if(mediaType == MediaType_enum.Multipart_alternative){
				return "multipart/alternative";
			}
			else if(mediaType == MediaType_enum.Multipart_parallel){
				return "multipart/parallel";
			}
			else if(mediaType == MediaType_enum.Multipart_related){
				return "multipart/related";
			}
			else if(mediaType == MediaType_enum.Multipart_signed){
				return "multipart/signed";
			}
			else if(mediaType == MediaType_enum.Multipart){
				return "multipart";
			}
			//---------------------------------------------//

			//--- Message/xxx -----------------------------//
			else if(mediaType == MediaType_enum.Message_rfc822){
				return "message/rfc822";
			}
			else if(mediaType == MediaType_enum.Message){
				return "message";
			}
			//---------------------------------------------//

			else if(mediaType == MediaType_enum.Unknown){
				return "unknown";
			}
			else{
				return null;
			}
		}

		#endregion

		#region static method ParseContentTransferEncoding

		/// <summary>
		/// Parses ContentTransferEncoding_enum from <b>Content-Transfer-Encoding:</b> header field value.
		/// </summary>
		/// <param name="headerFieldValue"><b>Content-Transfer-Encoding:</b> header field value. This value can be null, then ContentTransferEncoding_enum.NotSpecified.</param>
		/// <returns></returns>
		public static ContentTransferEncoding_enum ParseContentTransferEncoding(string headerFieldValue)
		{
			if(headerFieldValue == null){
				return ContentTransferEncoding_enum.NotSpecified;
			}

			string encoding = headerFieldValue.ToLower();
			if(encoding == "7bit"){
				return ContentTransferEncoding_enum._7bit;
			}
			else if(encoding == "quoted-printable"){
				return ContentTransferEncoding_enum.QuotedPrintable;
			}
			else if(encoding == "base64"){
				return ContentTransferEncoding_enum.Base64;
			}
			else if(encoding == "8bit"){
				return ContentTransferEncoding_enum._8bit;
			}
			else if(encoding == "binary"){
				return ContentTransferEncoding_enum.Binary;
			}
			else{
				return ContentTransferEncoding_enum.Unknown;
			}
		}

		#endregion

		#region static method ContentTransferEncodingToString

		/// <summary>
		/// Converts ContentTransferEncoding_enum to string. NOTE: Returns null for ContentTransferEncoding_enum.NotSpecified.
		/// </summary>
		/// <param name="encoding">ContentTransferEncoding_enum value to convert.</param>
		/// <returns></returns>
		public static string ContentTransferEncodingToString(ContentTransferEncoding_enum encoding)
		{			
			if(encoding == ContentTransferEncoding_enum._7bit){
				return "7bit";
			}
			else if(encoding == ContentTransferEncoding_enum.QuotedPrintable){
				return "quoted-printable";
			}
			else if(encoding == ContentTransferEncoding_enum.Base64){
				return "base64";
			}
			else if(encoding == ContentTransferEncoding_enum._8bit){
				return "8bit";
			}
			else if(encoding == ContentTransferEncoding_enum.Binary){
				return "binary";
			}
			else if(encoding == ContentTransferEncoding_enum.Unknown){
				return "unknown";
			}
			else{
				return null;
			}
		}

		#endregion

		#region static method ParseContentDisposition

		/// <summary>
		/// Parses ContentDisposition_enum from <b>Content-Disposition:</b> header field value.
		/// </summary>
		/// <param name="headerFieldValue"><b>Content-Disposition:</b> header field value. This value can be null, then ContentDisposition_enum.NotSpecified.</param>
		/// <returns></returns>
		public static ContentDisposition_enum ParseContentDisposition(string headerFieldValue)
		{
			if(headerFieldValue == null){
				return ContentDisposition_enum.NotSpecified;
			}

			string disposition = headerFieldValue.ToLower();
			if(disposition.IndexOf("attachment") > -1){
				return ContentDisposition_enum.Attachment;
			}
			else if(disposition.IndexOf("inline") > -1){
				return ContentDisposition_enum.Inline;
			}
			else{
				return ContentDisposition_enum.Unknown;
			}
		}

		#endregion

		#region static method ContentDispositionToString

		/// <summary>
		/// Converts ContentDisposition_enum to string. NOTE: Returns null for ContentDisposition_enum.NotSpecified.
		/// </summary>
		/// <param name="disposition">ContentDisposition_enum value to convert.</param>
		/// <returns></returns>
		public static string ContentDispositionToString(ContentDisposition_enum disposition)
		{			
			if(disposition == ContentDisposition_enum.Attachment){
				return "attachment";
			}				
			else if(disposition == ContentDisposition_enum.Inline){
				return "inline";
			}
			else if(disposition == ContentDisposition_enum.Unknown){
				return "unknown";
			}
			else{
				return null;
			}
		}

		#endregion


        #region static method EncodeWord

        /// <summary>
        /// Encodes specified text as "encoded-word" if encode is required. For more information see RFC 2047.
        /// </summary>
        /// <param name="text">Text to encode.</param>
        /// <returns>Returns encoded word.</returns>
        public static string EncodeWord(string text)
        {
            if(text == null){
                return null;
            }
            if(Core.IsAscii(text)){
                return text;
            }

            /* RFC 2047 2. Syntax of encoded-words.
                An 'encoded-word' is defined by the following ABNF grammar.  The
                notation of RFC 822 is used, with the exception that white space
                characters MUST NOT appear between components of an 'encoded-word'.

                encoded-word = "=?" charset "?" encoding "?" encoded-text "?="
                charset      = token    ; see section 3
                encoding     = token    ; see section 4
                token        = 1*<Any CHAR except SPACE, CTLs, and especials>
                especials    = "(" / ")" / "<" / ">" / "@" / "," / ";" / ":" / "
                               <"> / "/" / "[" / "]" / "?" / "." / "="
                encoded-text = 1*<Any printable ASCII character other than "?" or SPACE>
                                       ; (but see "Use of encoded-words in message headers", section 5)

                Both 'encoding' and 'charset' names are case-independent.  Thus the
                charset name "ISO-8859-1" is equivalent to "iso-8859-1", and the
                encoding named "Q" may be spelled either "Q" or "q".

                An 'encoded-word' may not be more than 75 characters long, including
                'charset', 'encoding', 'encoded-text', and delimiters.  If it is
                desirable to encode more text than will fit in an 'encoded-word' of
                75 characters, multiple 'encoded-word's (separated by CRLF SPACE) may
                be used.
              
                IMPORTANT: 'encoded-word's are designed to be recognized as 'atom's
                by an RFC 822 parser.  As a consequence, unencoded white space
                characters (such as SPACE and HTAB) are FORBIDDEN within an
                'encoded-word'.  For example, the character sequence

                =?iso-8859-1?q?this is some text?=

                would be parsed as four 'atom's, rather than as a single 'atom' (by
                an RFC 822 parser) or 'encoded-word' (by a parser which understands
                'encoded-words').  The correct way to encode the string "this is some
                text" is to encode the SPACE characters as well, e.g.

                =?iso-8859-1?q?this=20is=20some=20text?=
            */
            /*
            char[] especials = new char[]{'(',')','<','>','@',',',';',':','"','/','[',']','?','.','='};

            // See if need to enode.
            if(!Core.IsAscii(text)){
            }*/

            return Core.CanonicalEncode(text,"utf-8");
        }

        #endregion

        #region static method DecodeWords

        /// <summary>
        /// Decodes "encoded-word"'s from the specified text. For more information see RFC 2047.
        /// </summary>
        /// <param name="text">Text to decode.</param>
        /// <returns>Returns decoded text.</returns>
        public static string DecodeWords(string text)
        {
            if(text == null){
                return null;
            }

            /* RFC 2047 2. Syntax of encoded-words.
                An 'encoded-word' is defined by the following ABNF grammar.  The
                notation of RFC 822 is used, with the exception that white space
                characters MUST NOT appear between components of an 'encoded-word'.

                encoded-word = "=?" charset "?" encoding "?" encoded-text "?="
                charset      = token    ; see section 3
                encoding     = token    ; see section 4
                token        = 1*<Any CHAR except SPACE, CTLs, and especials>
                especials    = "(" / ")" / "<" / ">" / "@" / "," / ";" / ":" / "
                               <"> / "/" / "[" / "]" / "?" / "." / "="
                encoded-text = 1*<Any printable ASCII character other than "?" or SPACE>
                                       ; (but see "Use of encoded-words in message headers", section 5)

                Both 'encoding' and 'charset' names are case-independent.  Thus the
                charset name "ISO-8859-1" is equivalent to "iso-8859-1", and the
                encoding named "Q" may be spelled either "Q" or "q".

                An 'encoded-word' may not be more than 75 characters long, including
                'charset', 'encoding', 'encoded-text', and delimiters.  If it is
                desirable to encode more text than will fit in an 'encoded-word' of
                75 characters, multiple 'encoded-word's (separated by CRLF SPACE) may
                be used.
              
                IMPORTANT: 'encoded-word's are designed to be recognized as 'atom's
                by an RFC 822 parser.  As a consequence, unencoded white space
                characters (such as SPACE and HTAB) are FORBIDDEN within an
                'encoded-word'.  For example, the character sequence

                =?iso-8859-1?q?this is some text?=

                would be parsed as four 'atom's, rather than as a single 'atom' (by
                an RFC 822 parser) or 'encoded-word' (by a parser which understands
                'encoded-words').  The correct way to encode the string "this is some
                text" is to encode the SPACE characters as well, e.g.

                =?iso-8859-1?q?this=20is=20some=20text?=
            */

            StringReader  r      = new StringReader(text);
            StringBuilder retVal = new StringBuilder();

            // We need to loop all words, if encoded word, decode it, othwerwise just append to return value.
            bool lastIsEncodedWord = false;
            while(r.Available > 0){
                string whiteSpaces = r.ReadToFirstChar();

                // Probably is encoded-word, we try to parse it.
                if(r.StartsWith("=?") && r.SourceString.IndexOf("?=") > -1){
                    StringBuilder encodedWord = new StringBuilder();
                    string        decodedWord = null;

                    try{
                        // NOTE: We can't read encoded word and then split !!!, we need to read each part.
                    
                        // Remove =?
                        encodedWord.Append(r.ReadSpecifiedLength(2));

                        // Read charset
                        string charset = r.QuotedReadToDelimiter('?');
                        encodedWord.Append(charset + "?");

                        // Read encoding
                        string encoding = r.QuotedReadToDelimiter('?');
                        encodedWord.Append(encoding + "?");

                        // Read text
                        string encodedText = r.QuotedReadToDelimiter('?');
                        encodedWord.Append(encodedText + "?");

                        // We must have remaining '=' here
                        if(r.StartsWith("=")){
                            encodedWord.Append(r.ReadSpecifiedLength(1));

                            Encoding c = Encoding.GetEncoding(charset);
                            if(encoding.ToLower() == "q"){
                                decodedWord = Core.QDecode(c,encodedText);
                            }
                            else if(encoding.ToLower() == "b"){
                                decodedWord = c.GetString(Core.Base64Decode(Encoding.Default.GetBytes(encodedText)));
                            }
                        }
                    }
                    catch{
                        // Not encoded-word or contains unknwon charset/encoding, so leave
                        // encoded-word as is.
                    }

                    /* RFC 2047 6.2.
                        When displaying a particular header field that contains multiple
                        'encoded-word's, any 'linear-white-space' that separates a pair of
                        adjacent 'encoded-word's is ignored.  (This is to allow the use of
                        multiple 'encoded-word's to represent long strings of unencoded text,
                        without having to separate 'encoded-word's where spaces occur in the
                        unencoded text.)
                    */
                    if(!lastIsEncodedWord){
                        retVal.Append(whiteSpaces);
                    }

                    // Decoding failed for that encoded-word, leave encoded-word as is.
                    if(decodedWord == null){
                        retVal.Append(encodedWord.ToString());
                    }
                    // We deocded encoded-word successfully.
                    else{
                        retVal.Append(decodedWord);
                    }

                    lastIsEncodedWord = true;
                }
                // Normal word.
                else if(r.StartsWithWord()){
                    retVal.Append(whiteSpaces + r.ReadWord(false));
                    lastIsEncodedWord = false;
                }
                // We have some separator or parenthesize.
                else{
                   retVal.Append(whiteSpaces + r.ReadSpecifiedLength(1));
                }
            }

            return retVal.ToString();
        }

        #endregion

        #region static method EncodeHeaderField

        /// <summary>
		/// Encodes header field with quoted-printable encoding, if value contains ANSI or UNICODE chars.
		/// </summary>
		/// <param name="text">Text to encode.</param>
		/// <returns></returns>
		public static string EncodeHeaderField(string text)
		{
			if(Core.IsAscii(text)){
				return text;
			}

			// First try to encode quoted strings("unicode-text") only, if no
			// quoted strings, encode full text.

			if(text.IndexOf("\"") > -1){
				string retVal = text;
				int offset = 0;							
				while(offset < retVal.Length - 1){
					int quoteStartIndex = retVal.IndexOf("\"",offset);
					// There is no more qouted strings, but there are some text left
					if(quoteStartIndex == -1){
						break;
					}
					int quoteEndIndex = retVal.IndexOf("\"",quoteStartIndex + 1);
					// If there isn't closing quote, encode full text
					if(quoteEndIndex == -1){
						break;
					}

					string leftPart = retVal.Substring(0,quoteStartIndex);
					string rightPart = retVal.Substring(quoteEndIndex + 1);
					string quotedString = retVal.Substring(quoteStartIndex + 1,quoteEndIndex - quoteStartIndex - 1);

					// Encode only not ASCII text
					if(!Core.IsAscii(quotedString)){
						string quotedStringCEncoded = Core.CanonicalEncode(quotedString,"utf-8");
						retVal = leftPart + "\"" + quotedStringCEncoded + "\"" + rightPart;
						offset += quoteEndIndex + 1 + quotedStringCEncoded.Length - quotedString.Length;
					}
					else{
						offset += quoteEndIndex + 1;
					}
				}

				// See if all encoded ok, if not encode all text
				if(Core.IsAscii(retVal)){
					return retVal;
				}
				else{
                    // REMOVE ME:(12.10.2006) Fixed, return Core.CanonicalEncode(retVal,"utf-8");
					return Core.CanonicalEncode(text,"utf-8");
				}
			}
			
			return Core.CanonicalEncode(text,"utf-8");
		}

		#endregion


		#region static method CreateMessageID

		/// <summary>
		/// Creates Rfc 2822 3.6.4 message-id. Syntax: '&lt;' id-left '@' id-right '&gt;'.
		/// </summary>
		/// <returns></returns>
		public static string CreateMessageID()
		{
			return "<" + Guid.NewGuid().ToString().Replace("-","") + "@" + Guid.NewGuid().ToString().Replace("-","") + ">";
		}

		#endregion


        #region static method FoldData

        /// <summary>
        /// Folds long data line to folded lines.
        /// </summary>
        /// <param name="data">Data to fold.</param>
        /// <returns></returns>
        public static string FoldData(string data)
        {
            /* Folding rules:
                *) Line may not be bigger than 76 chars.
                *) If possible fold between TAB or SP
                *) If no fold point, just fold from char 76
            */

            // Data line too big, we need to fold data.
            if(data.Length > 76){
                int startPosition       = 0;
                int lastPossibleFoldPos = -1;
                StringBuilder retVal = new StringBuilder();
                for(int i=0;i<data.Length;i++){
                    char c = data[i];
                    // We have possible fold point
                    if(c == ' ' || c == '\t'){
                        lastPossibleFoldPos = i;
                    }

                    // End of data reached
                    if(i == (data.Length - 1)){
                        retVal.Append(data.Substring(startPosition));
                    }
                    // We need to fold
                    else if((i - startPosition) >= 76){
                        // There wasn't any good fold point(word is bigger than line), just fold from current position.
                        if(lastPossibleFoldPos == -1){
                            lastPossibleFoldPos = i;
                        }
                    
                        retVal.Append(data.Substring(startPosition,lastPossibleFoldPos - startPosition) + "\r\n\t");

                        i = lastPossibleFoldPos;
                        lastPossibleFoldPos = -1;
                        startPosition       = i;
                    }
                }

                return retVal.ToString();
            }
            else{
                return data;
            }
        }

        #endregion
	}
}
