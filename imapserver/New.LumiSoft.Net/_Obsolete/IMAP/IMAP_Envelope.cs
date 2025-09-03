using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.MIME;
using LumiSoft.Net.Mail;
using LumiSoft.Net.IMAP.Client;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// This class represents IMAP FETCH ENVELOPE data item. Defined in RFC 3501.
    /// </summary>
    [Obsolete("Use Fetch(bool uid,IMAP_t_SeqSet seqSet,IMAP_t_Fetch_i[] items,EventHandler<EventArgs<IMAP_r_u>> callback) intead.")]
    public class IMAP_Envelope
    {
        private DateTime         m_Date      = DateTime.MinValue;
        private string           m_Subject   = null;
        private Mail_t_Address[] m_pFrom     = null;
        private Mail_t_Address[] m_pSender   = null;
        private Mail_t_Address[] m_pReplyTo  = null;
        private Mail_t_Address[] m_pTo       = null;
        private Mail_t_Address[] m_pCc       = null;
        private Mail_t_Address[] m_pBcc       = null;
        private string           m_InReplyTo = null;
        private string           m_MessageID = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="date">Message <b>Date</b> header value.</param>
        /// <param name="subject">Message <b>Subject</b> header value.</param>
        /// <param name="from">Message <b>From</b> header value.</param>
        /// <param name="sender">Message <b>Sender</b> header value.</param>
        /// <param name="replyTo">Message <b>Reply-To</b> header value.</param>
        /// <param name="to">Message <b>To</b> header value.</param>
        /// <param name="cc">Message <b>Cc</b> header value.</param>
        /// <param name="bcc">Message <b>Bcc</b> header value.</param>
        /// <param name="inReplyTo">Message <b>In-Reply-To</b> header value.</param>
        /// <param name="messageID">Message <b>Message-ID</b> header value.</param>
        public IMAP_Envelope(DateTime date,string subject,Mail_t_Address[] from,Mail_t_Address[] sender,Mail_t_Address[] replyTo,Mail_t_Address[] to,Mail_t_Address[] cc,Mail_t_Address[] bcc,string inReplyTo,string messageID)
        {
            m_Date      = date;
            m_Subject   = subject;
            m_pFrom     = from;
            m_pSender   = sender;
            m_pReplyTo  = replyTo;
            m_pTo       = to;
            m_pCc       = cc;
            m_pBcc      = bcc;
            m_InReplyTo = inReplyTo;
            m_MessageID = messageID;
        }


        #region static method Parse

        /// <summary>
        /// Parses IMAP ENVELOPE from string.
        /// </summary>
        /// <param name="r">String reader.</param>
        /// <returns>Returns parsed IMAP ENVELOPE string.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>r</b> is null reference.</exception>
        public static IMAP_Envelope Parse(StringReader r)
        {
            if(r == null){
                throw new ArgumentNullException("r");
            }

            /* RFC 3501 7.4.2 ENVELOPE.
                A parenthesized list that describes the envelope structure of a
                message.  This is computed by the server by parsing the
                [RFC-2822] header into the component parts, defaulting various
                fields as necessary.

                The fields of the envelope structure are in the following
                order: date, subject, from, sender, reply-to, to, cc, bcc,
                in-reply-to, and message-id.  The date, subject, in-reply-to,
                and message-id fields are strings.  The from, sender, reply-to,
                to, cc, and bcc fields are parenthesized lists of address
                structures.

                An address structure is a parenthesized list that describes an
                electronic mail address.  The fields of an address structure
                are in the following order: personal name, [SMTP]
                at-domain-list (source route), mailbox name, and host name.

                [RFC-2822] group syntax is indicated by a special form of
                address structure in which the host name field is NIL.  If the
                mailbox name field is also NIL, this is an end of group marker
                (semi-colon in RFC 822 syntax).  If the mailbox name field is
                non-NIL, this is a start of group marker, and the mailbox name
                field holds the group name phrase.

                If the Date, Subject, In-Reply-To, and Message-ID header lines
                are absent in the [RFC-2822] header, the corresponding member
                of the envelope is NIL; if these header lines are present but
                empty the corresponding member of the envelope is the empty
                string.

                    Note: some servers may return a NIL envelope member in the
                    "present but empty" case.  Clients SHOULD treat NIL and
                    empty string as identical.

                    Note: [RFC-2822] requires that all messages have a valid
                    Date header.  Therefore, the date member in the envelope can
                    not be NIL or the empty string.

                    Note: [RFC-2822] requires that the In-Reply-To and
                    Message-ID headers, if present, have non-empty content.
                    Therefore, the in-reply-to and message-id members in the
                    envelope can not be the empty string.

                If the From, To, cc, and bcc header lines are absent in the
                [RFC-2822] header, or are present but empty, the corresponding
                member of the envelope is NIL.

                If the Sender or Reply-To lines are absent in the [RFC-2822]
                header, or are present but empty, the server sets the
                corresponding member of the envelope to be the same value as
                the from member (the client is not expected to know to do
                this).

                    Note: [RFC-2822] requires that all messages have a valid
                    From header.  Therefore, the from, sender, and reply-to
                    members in the envelope can not be NIL.
            */

            // Eat "ENVELOPE".
            r.ReadWord();             
            r.ReadToFirstChar();
            // Eat starting "(".
            r.ReadSpecifiedLength(1);

            // Read "date".
            DateTime date = DateTime.MinValue;
            string dateS = r.ReadWord();            
            if(dateS != null){
                date = MIME_Utils.ParseRfc2822DateTime(dateS);
            }

            // Read "subject".
            string subject = ReadAndDecodeWord(r.ReadWord());

            // Read "from"
            Mail_t_Address[] from = ReadAddresses(r);
            
            //Read "sender"
            Mail_t_Address[] sender = ReadAddresses(r);
            
            // Read "reply-to"
            Mail_t_Address[] replyTo = ReadAddresses(r);
            
            // Read "to"
            Mail_t_Address[] to = ReadAddresses(r);
            
            // Read "cc"
            Mail_t_Address[] cc = ReadAddresses(r);
            
            // Read "bcc"
            Mail_t_Address[] bcc = ReadAddresses(r);
            
            // Read "in-reply-to"
            string inReplyTo = r.ReadWord();
            
            // Read "message-id"
            string messageID = r.ReadWord();

            // Eat ending ")".
            r.ReadToFirstChar();
            r.ReadSpecifiedLength(1);

            return new IMAP_Envelope(date,subject,from,sender,replyTo,to,cc,bcc,inReplyTo,messageID);
        }

        /// <summary>
        /// Parses IMAP FETCH ENVELOPE data-item.
        /// </summary>
        /// <param name="fetchReader">Fetch reader.</param>
        /// <returns>Returns parsed IMAP FETCH ENVELOPE data-item.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>fetchReader</b> is null reference.</exception>
        internal static IMAP_Envelope Parse(IMAP_Client._FetchResponseReader fetchReader)
        {
            if(fetchReader == null){
                throw new ArgumentNullException("fetchReader");
            }

            /* RFC 3501 7.4.2 ENVELOPE.
                A parenthesized list that describes the envelope structure of a
                message.  This is computed by the server by parsing the
                [RFC-2822] header into the component parts, defaulting various
                fields as necessary.

                The fields of the envelope structure are in the following
                order: date, subject, from, sender, reply-to, to, cc, bcc,
                in-reply-to, and message-id.  The date, subject, in-reply-to,
                and message-id fields are strings.  The from, sender, reply-to,
                to, cc, and bcc fields are parenthesized lists of address
                structures.

                An address structure is a parenthesized list that describes an
                electronic mail address.  The fields of an address structure
                are in the following order: personal name, [SMTP]
                at-domain-list (source route), mailbox name, and host name.

                [RFC-2822] group syntax is indicated by a special form of
                address structure in which the host name field is NIL.  If the
                mailbox name field is also NIL, this is an end of group marker
                (semi-colon in RFC 822 syntax).  If the mailbox name field is
                non-NIL, this is a start of group marker, and the mailbox name
                field holds the group name phrase.

                If the Date, Subject, In-Reply-To, and Message-ID header lines
                are absent in the [RFC-2822] header, the corresponding member
                of the envelope is NIL; if these header lines are present but
                empty the corresponding member of the envelope is the empty
                string.

                    Note: some servers may return a NIL envelope member in the
                    "present but empty" case.  Clients SHOULD treat NIL and
                    empty string as identical.

                    Note: [RFC-2822] requires that all messages have a valid
                    Date header.  Therefore, the date member in the envelope can
                    not be NIL or the empty string.

                    Note: [RFC-2822] requires that the In-Reply-To and
                    Message-ID headers, if present, have non-empty content.
                    Therefore, the in-reply-to and message-id members in the
                    envelope can not be the empty string.

                If the From, To, cc, and bcc header lines are absent in the
                [RFC-2822] header, or are present but empty, the corresponding
                member of the envelope is NIL.

                If the Sender or Reply-To lines are absent in the [RFC-2822]
                header, or are present but empty, the server sets the
                corresponding member of the envelope to be the same value as
                the from member (the client is not expected to know to do
                this).

                    Note: [RFC-2822] requires that all messages have a valid
                    From header.  Therefore, the from, sender, and reply-to
                    members in the envelope can not be NIL.
            */

            // Eat "ENVELOPE".
            fetchReader.GetReader().ReadWord();             
            fetchReader.GetReader().ReadToFirstChar();
            // Eat starting "(".
            fetchReader.GetReader().ReadSpecifiedLength(1);

            // Read "date".
            DateTime date = DateTime.MinValue;
            string dateS = fetchReader.ReadString();            
            if(dateS != null){
                date = MIME_Utils.ParseRfc2822DateTime(dateS);
            }

            // Read "subject".
            string subject =  ReadAndDecodeWord(fetchReader.ReadString());

            // Read "from"
            Mail_t_Address[] from = ReadAddresses(fetchReader);
            
            //Read "sender"
            Mail_t_Address[] sender = ReadAddresses(fetchReader);
            
            // Read "reply-to"
            Mail_t_Address[] replyTo = ReadAddresses(fetchReader);
            
            // Read "to"
            Mail_t_Address[] to = ReadAddresses(fetchReader);
            
            // Read "cc"
            Mail_t_Address[] cc = ReadAddresses(fetchReader);
            
            // Read "bcc"
            Mail_t_Address[] bcc = ReadAddresses(fetchReader);
            
            // Read "in-reply-to"
            string inReplyTo = fetchReader.ReadString();
            
            // Read "message-id"
            string messageID = fetchReader.ReadString();

            // Eat ending ")".
            fetchReader.GetReader().ReadToFirstChar();
            fetchReader.GetReader().ReadSpecifiedLength(1);

            return new IMAP_Envelope(date,subject,from,sender,replyTo,to,cc,bcc,inReplyTo,messageID);
        }

        #endregion

        #region static method ConstructEnvelope

		/// <summary>
		/// Construct secified mime entity ENVELOPE string.
		/// </summary>
		/// <param name="entity">Mail message.</param>
		/// <returns></returns>
		public static string ConstructEnvelope(Mail_Message entity)
		{
			/* RFC 3501 7.4.2
				ENVELOPE
					A parenthesized list that describes the envelope structure of a
					message.  This is computed by the server by parsing the
					[RFC-2822] header into the component parts, defaulting various
					fields as necessary.

					The fields of the envelope structure are in the following
					order: date, subject, from, sender, reply-to, to, cc, bcc,
					in-reply-to, and message-id.  The date, subject, in-reply-to,
					and message-id fields are strings.  The from, sender, reply-to,
					to, cc, and bcc fields are parenthesized lists of address
					structures.

					An address structure is a parenthesized list that describes an
					electronic mail address.  The fields of an address structure
					are in the following order: personal name, [SMTP]
					at-domain-list (source route), mailbox name, and host name.

					[RFC-2822] group syntax is indicated by a special form of
					address structure in which the host name field is NIL.  If the
					mailbox name field is also NIL, this is an end of group marker
					(semi-colon in RFC 822 syntax).  If the mailbox name field is
					non-NIL, this is a start of group marker, and the mailbox name
					field holds the group name phrase.

					If the Date, Subject, In-Reply-To, and Message-ID header lines
					are absent in the [RFC-2822] header, the corresponding member
					of the envelope is NIL; if these header lines are present but
					empty the corresponding member of the envelope is the empty
					string.
					
						Note: some servers may return a NIL envelope member in the
						"present but empty" case.  Clients SHOULD treat NIL and
						empty string as identical.

						Note: [RFC-2822] requires that all messages have a valid
						Date header.  Therefore, the date member in the envelope can
						not be NIL or the empty string.

						Note: [RFC-2822] requires that the In-Reply-To and
						Message-ID headers, if present, have non-empty content.
						Therefore, the in-reply-to and message-id members in the
						envelope can not be the empty string.

					If the From, To, cc, and bcc header lines are absent in the
					[RFC-2822] header, or are present but empty, the corresponding
					member of the envelope is NIL.

					If the Sender or Reply-To lines are absent in the [RFC-2822]
					header, or are present but empty, the server sets the
					corresponding member of the envelope to be the same value as
					the from member (the client is not expected to know to do
					this).

						Note: [RFC-2822] requires that all messages have a valid
						From header.  Therefore, the from, sender, and reply-to
						members in the envelope can not be NIL.
		 
					ENVELOPE ("date" "subject" from sender reply-to to cc bcc "in-reply-to" "messageID")
			*/

			// NOTE: all header fields and parameters must in ENCODED form !!!

            MIME_Encoding_EncodedWord wordEncoder = new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8);
            wordEncoder.Split = false;

			StringBuilder retVal = new StringBuilder();
			retVal.Append("ENVELOPE (");

			// date
            try{
			    if(entity.Date != DateTime.MinValue){
				    retVal.Append(TextUtils.QuoteString(MIME_Utils.DateTimeToRfc2822(entity.Date)));
		    	}
			    else{
				    retVal.Append("NIL");
			    }
            }
            catch{
                retVal.Append("NIL");
            }

			// subject
			if(entity.Subject != null){
				//retVal.Append(" " + TextUtils.QuoteString(wordEncoder.Encode(entity.Subject)));
                string val = wordEncoder.Encode(entity.Subject);
                retVal.Append(" {" + val.Length + "}\r\n" + val);
			}
			else{
				retVal.Append(" NIL");
			}

			// from
			if(entity.From != null && entity.From.Count > 0){
				retVal.Append(" " + ConstructAddresses(entity.From.ToArray(),wordEncoder));
			}
			else{
				retVal.Append(" NIL");
			}

			// sender	
			//	NOTE: There is confusing part, according rfc 2822 Sender: is MailboxAddress and not AddressList.
			if(entity.Sender != null){
				retVal.Append(" (");

				retVal.Append(ConstructAddress(entity.Sender,wordEncoder));

				retVal.Append(")");
			}
			else{
				retVal.Append(" NIL");
			}

			// reply-to
			if(entity.ReplyTo != null){
				retVal.Append(" " + ConstructAddresses(entity.ReplyTo.Mailboxes,wordEncoder));
			}
			else{
				retVal.Append(" NIL");
			}

			// to
			if(entity.To != null && entity.To.Count > 0){
				retVal.Append(" " + ConstructAddresses(entity.To.Mailboxes,wordEncoder));
			}
			else{
				retVal.Append(" NIL");
			}

			// cc
			if(entity.Cc != null && entity.Cc.Count > 0){
				retVal.Append(" " + ConstructAddresses(entity.Cc.Mailboxes,wordEncoder));
			}
			else{
				retVal.Append(" NIL");
			}

			// bcc
			if(entity.Bcc != null && entity.Bcc.Count > 0){
				retVal.Append(" " + ConstructAddresses(entity.Bcc.Mailboxes,wordEncoder));
			}
			else{
				retVal.Append(" NIL");
			}

			// in-reply-to			
			if(entity.InReplyTo != null){
				retVal.Append(" " + TextUtils.QuoteString(wordEncoder.Encode(entity.InReplyTo)));
			}
			else{
				retVal.Append(" NIL");
			}

			// message-id
			if(entity.MessageID != null){
				retVal.Append(" " + TextUtils.QuoteString(wordEncoder.Encode(entity.MessageID)));
			}
			else{
				retVal.Append(" NIL");
			}

			retVal.Append(")");

			return retVal.ToString();			
		}

		#endregion


        #region static method ReadAddresses

        /// <summary>
        /// Reads parenthesized list of addresses.
        /// </summary>
        /// <param name="r">String reader.</param>
        /// <returns>Returns read addresses.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>r</b> is null reference.</exception>
        private static Mail_t_Address[] ReadAddresses(StringReader r)
        {
            if(r == null){
                throw new ArgumentNullException("r");
            }

            /* RFC 3501 7.4.2. 
                An address structure is a parenthesized list that describes an
                electronic mail address.  The fields of an address structure
                are in the following order: personal name, [SMTP]
                at-domain-list (source route), mailbox name, and host name.

                [RFC-2822] group syntax is indicated by a special form of
                address structure in which the host name field is NIL.  If the
                mailbox name field is also NIL, this is an end of group marker
                (semi-colon in RFC 822 syntax).  If the mailbox name field is
                non-NIL, this is a start of group marker, and the mailbox name
                field holds the group name phrase.
            */
            
            r.ReadToFirstChar();
            if(r.StartsWith("NIL",false)){
                r.ReadWord();

                return null;
            }
            else{
                List<Mail_t_Address> retVal = new List<Mail_t_Address>();
                // Eat addresses starting "(".
                r.ReadSpecifiedLength(1);

                while(r.Available > 0){
                    // We have addresses ending ")".
                    if(r.StartsWith(")")){
                        r.ReadSpecifiedLength(1);
                        break;
                    }

                    // Eat address starting "(".
                    r.ReadSpecifiedLength(1);

                    string personalName = ReadAndDecodeWord(r.ReadWord());
                    string atDomainList = r.ReadWord();
                    string mailboxName  = r.ReadWord();
                    string hostName     = r.ReadWord();

                    retVal.Add(new Mail_t_Mailbox(personalName,mailboxName + "@" + hostName));

                    // Eat address ending ")".
                    r.ReadSpecifiedLength(1);
                }

                return retVal.ToArray();
            }
        }

        /// <summary>
        /// Reads parenthesized list of addresses.
        /// </summary>
        /// <param name="fetchReader">Fetch reader.</param>
        /// <returns>Returns read addresses.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>fetchReader</b> is null reference.</exception>
        private static Mail_t_Address[] ReadAddresses(IMAP_Client._FetchResponseReader fetchReader)
        {
            if(fetchReader == null){
                throw new ArgumentNullException("fetchReader");
            }

            /* RFC 3501 7.4.2. 
                An address structure is a parenthesized list that describes an
                electronic mail address.  The fields of an address structure
                are in the following order: personal name, [SMTP]
                at-domain-list (source route), mailbox name, and host name.

                [RFC-2822] group syntax is indicated by a special form of
                address structure in which the host name field is NIL.  If the
                mailbox name field is also NIL, this is an end of group marker
                (semi-colon in RFC 822 syntax).  If the mailbox name field is
                non-NIL, this is a start of group marker, and the mailbox name
                field holds the group name phrase.
            */
            
            fetchReader.GetReader().ReadToFirstChar();
            if(fetchReader.GetReader().StartsWith("NIL",false)){
                fetchReader.GetReader().ReadWord();

                return null;
            }
            else{
                List<Mail_t_Address> retVal = new List<Mail_t_Address>();
                // Eat addresses starting "(".
                fetchReader.GetReader().ReadSpecifiedLength(1);

                while(fetchReader.GetReader().Available > 0){
                    // We have addresses ending ")".
                    if(fetchReader.GetReader().StartsWith(")")){
                        fetchReader.GetReader().ReadSpecifiedLength(1);
                        break;
                    }

                    // Eat address starting "(".
                    fetchReader.GetReader().ReadSpecifiedLength(1);

                    string personalName = ReadAndDecodeWord(fetchReader.ReadString());
                    string atDomainList = fetchReader.ReadString();
                    string mailboxName  = fetchReader.ReadString();
                    string hostName     = fetchReader.ReadString();

                    retVal.Add(new Mail_t_Mailbox(personalName,mailboxName + "@" + hostName));

                    // Eat address ending ")".
                    fetchReader.GetReader().ReadSpecifiedLength(1);
                    fetchReader.GetReader().ReadToFirstChar();
                }

                return retVal.ToArray();
            }
        }

        #endregion

        #region private static method ConstructAddresses

		/// <summary>
		/// Constructs ENVELOPE addresses structure.
		/// </summary>
		/// <param name="mailboxes">Mailboxes.</param>
        /// <param name="wordEncoder">Unicode words encoder.</param>
		/// <returns></returns>
		private static string ConstructAddresses(Mail_t_Mailbox[] mailboxes,MIME_Encoding_EncodedWord wordEncoder)
		{
			StringBuilder retVal = new StringBuilder();
			retVal.Append("(");

			foreach(Mail_t_Mailbox address in mailboxes){                
				retVal.Append(ConstructAddress(address,wordEncoder));
			}

			retVal.Append(")");

			return retVal.ToString();
		}

		#endregion

		#region private static method ConstructAddress

		/// <summary>
		/// Constructs ENVELOPE address structure.
		/// </summary>
		/// <param name="address">Mailbox address.</param>
        /// <param name="wordEncoder">Unicode words encoder.</param>
		/// <returns></returns>
		private static string ConstructAddress(Mail_t_Mailbox address,MIME_Encoding_EncodedWord wordEncoder)
		{
			/* An address structure is a parenthesized list that describes an
			   electronic mail address.  The fields of an address structure
			   are in the following order: personal name, [SMTP]
			   at-domain-list (source route), mailbox name, and host name.
			*/

			// NOTE: all header fields and parameters must in ENCODED form !!!

			StringBuilder retVal = new StringBuilder();
			retVal.Append("(");

			// personal name
            if(address.DisplayName != null){
			    retVal.Append(TextUtils.QuoteString(wordEncoder.Encode(RemoveCrlf(address.DisplayName))));
            }
            else{
                retVal.Append("NIL");
            }

			// source route, always NIL (not used nowdays)
			retVal.Append(" NIL");

			// mailbox name
			retVal.Append(" " + TextUtils.QuoteString(wordEncoder.Encode(RemoveCrlf(address.LocalPart))));

			// host name
            if(address.Domain != null){
			    retVal.Append(" " + TextUtils.QuoteString(wordEncoder.Encode(RemoveCrlf(address.Domain))));
            }
            else{
                retVal.Append(" NIL");
            }

			retVal.Append(")");

			return retVal.ToString();
		}

		#endregion

        #region static method RemoveCrlf

        /// <summary>
        /// Removes CR and LF chars from the specified string.
        /// </summary>
        /// <param name="value">String value.</param>
        /// <returns>Reurns string.</returns>
        private static string RemoveCrlf(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            return value.Replace("\r","").Replace("\n","");
        }

        #endregion

        #region static method DecodeWord

        /// <summary>
        /// Decodes word from reader.
        /// </summary>
        /// <param name="text">Text.</param>
        /// <returns>Returns decoded word.</returns>
        private static string ReadAndDecodeWord(string text)
        {            
            if(text == null){
                return null;
            }
            else if(string.Equals(text,"NIL",StringComparison.InvariantCultureIgnoreCase)){
                return "";
            }
            else{
                return MIME_Encoding_EncodedWord.DecodeTextS(text);
            }
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets message <b>Date</b> header field value. Value DateTime.Min means no <b>Date</b> header field.
        /// </summary>
        public DateTime Date
        {
            get{ return m_Date; }
        }

        /// <summary>
        /// Gets message <b>Subject</b> header field value. Value null means no <b>Subject</b> header field.
        /// </summary>
        public string Subject
        {
            get{ return m_Subject; }
        }

        /// <summary>
        /// Gets message <b>From</b> header field value. Value null means no <b>From</b> header field.
        /// </summary>
        public Mail_t_Address[] From
        {
            get{ return m_pFrom; }
        }

        /// <summary>
        /// Gets message <b>Sender</b> header field value. Value null means no <b>Sender</b> header field.
        /// </summary>
        public Mail_t_Address[] Sender
        {
            get{ return m_pSender; }
        }

        /// <summary>
        /// Gets message <b>Reply-To</b> header field value. Value null means no <b>Reply-To</b> header field.
        /// </summary>
        public Mail_t_Address[] ReplyTo
        {
            get{ return m_pReplyTo; }
        }

        /// <summary>
        /// Gets message <b>To</b> header field value. Value null means no <b>To</b> header field.
        /// </summary>
        public Mail_t_Address[] To
        {
            get{ return m_pTo; }
        }

        /// <summary>
        /// Gets message <b>Cc</b> header field value. Value null means no <b>Cc</b> header field.
        /// </summary>
        public Mail_t_Address[] Cc
        {
            get{ return m_pCc; }
        }

        /// <summary>
        /// Gets message <b>Bcc</b> header field value. Value null means no <b>Bcc</b> header field.
        /// </summary>
        public Mail_t_Address[] Bcc
        {
            get{ return m_pBcc; }
        }
        
        /// <summary>
        /// Gets message <b>In-Reply-To</b> header field value. Value null means no <b>In-Reply-To</b> header field.
        /// </summary>
        public string InReplyTo
        {
            get{ return m_InReplyTo; }
        }
        
        /// <summary>
        /// Gets message <b>Message-ID</b> header field value. Value null means no <b>Message-ID</b> header field.
        /// </summary>
        public string MessageID
        {
            get{ return m_MessageID; }
        }

        #endregion
    }
}
