using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;
using LumiSoft.Net.MIME;

using System.Threading;

namespace LumiSoft.Net.Mail
{
    /// <summary>
    /// This class represent electronic mail message. Defined in RFC 5322.
    /// </summary>
    public class Mail_Message : MIME_Message
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Mail_Message()
        {
            this.Header.FieldsProvider.HeaderFields.Add("From",typeof(Mail_h_MailboxList));
            this.Header.FieldsProvider.HeaderFields.Add("Sender",typeof(Mail_h_Mailbox));
            this.Header.FieldsProvider.HeaderFields.Add("Reply-To",typeof(Mail_h_AddressList));
            this.Header.FieldsProvider.HeaderFields.Add("To",typeof(Mail_h_AddressList));
            this.Header.FieldsProvider.HeaderFields.Add("Cc",typeof(Mail_h_AddressList));
            this.Header.FieldsProvider.HeaderFields.Add("Bcc",typeof(Mail_h_AddressList));
            this.Header.FieldsProvider.HeaderFields.Add("Resent-From",typeof(Mail_h_MailboxList));
            this.Header.FieldsProvider.HeaderFields.Add("Resent-Sender",typeof(Mail_h_Mailbox));
            this.Header.FieldsProvider.HeaderFields.Add("Resent-To",typeof(Mail_h_AddressList));
            this.Header.FieldsProvider.HeaderFields.Add("Resent-Cc",typeof(Mail_h_AddressList));
            this.Header.FieldsProvider.HeaderFields.Add("Resent-Bcc",typeof(Mail_h_AddressList));
            this.Header.FieldsProvider.HeaderFields.Add("Resent-Reply-To",typeof(Mail_h_AddressList));
            this.Header.FieldsProvider.HeaderFields.Add("Return-Path",typeof(Mail_h_ReturnPath));
            this.Header.FieldsProvider.HeaderFields.Add("Received",typeof(Mail_h_Received));
            this.Header.FieldsProvider.HeaderFields.Add("Disposition-Notification-To",typeof(Mail_h_MailboxList));
            this.Header.FieldsProvider.HeaderFields.Add("Disposition-Notification-Options",typeof(Mail_h_DispositionNotificationOptions));
        }


        #region static method ParseFromByte

        /// <summary>
        /// Parses mail message from the specified byte array.
        /// </summary>
        /// <param name="data">Mail message data.</param>
        /// <returns>Returns parsed mail message.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>data</b> is null reference.</exception>
        public static Mail_Message ParseFromByte(byte[] data)
        {
            if(data == null){
                throw new ArgumentNullException("data");
            }

            return ParseFromStream(new MemoryStream(data));
        }

        /// <summary>
        /// Parses mail message from the specified byte array.
        /// </summary>
        /// <param name="data">Mail message data.</param>
        /// <param name="headerEncoding">Header reading encoding. If not sure UTF-8 is recommended.</param>
        /// <returns>Returns parsed mail message.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>data</b> or <b>headerEncoding</b> is null reference.</exception>
        public static Mail_Message ParseFromByte(byte[] data,Encoding headerEncoding)
        {
            if(data == null){
                throw new ArgumentNullException("data");
            }
            if(headerEncoding == null){
                throw new ArgumentNullException("headerEncoding");
            }

            return ParseFromStream(new MemoryStream(data),headerEncoding);
        }

        #endregion

        #region static method ParseFromFile

        /// <summary>
        /// Parses mail message from the specified file.
        /// </summary>
        /// <param name="file">File name with path from where to parse mail message.</param>
        /// <returns>Returns parsed mail message.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>file</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public static new Mail_Message ParseFromFile(string file)
        {
            if(file == null){
                throw new ArgumentNullException("file");
            }
            if(file == ""){
                throw new ArgumentException("Argument 'file' value must be specified.");
            }

            using(FileStream fs = File.OpenRead(file)){
                return ParseFromStream(fs);
            }
        }

        /// <summary>
        /// Parses mail message from the specified file.
        /// </summary>
        /// <param name="file">File name with path from where to parse mail message.</param>
        /// <param name="headerEncoding">Header reading encoding. If not sure UTF-8 is recommended.</param>
        /// <returns>Returns parsed mail message.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>file</b> or <b>headerEncoding</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public static new Mail_Message ParseFromFile(string file,Encoding headerEncoding)
        {
            if(file == null){
                throw new ArgumentNullException("file");
            }
            if(file == ""){
                throw new ArgumentException("Argument 'file' value must be specified.");
            }
            if(headerEncoding == null){
                throw new ArgumentNullException("headerEncoding");
            }

            using(FileStream fs = File.OpenRead(file)){
                return ParseFromStream(fs,headerEncoding);
            }
        }

        #endregion

        #region static method ParseFromStream

        /// <summary>
        /// Parses mail message from the specified stream.
        /// </summary>
        /// <param name="stream">Stream from where to parse mail message. Parsing starts from current stream position.</param>
        /// <returns>Returns parsed mail message.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null.</exception>
        public static new Mail_Message ParseFromStream(Stream stream)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            return ParseFromStream(stream,Encoding.UTF8);
        }
        
        /// <summary>
        /// Parses mail message from the specified stream.
        /// </summary>
        /// <param name="stream">Stream from where to parse mail message. Parsing starts from current stream position.</param>
        /// <param name="headerEncoding">Header reading encoding. If not sure UTF-8 is recommended.</param>
        /// <returns>Returns parsed mail message.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> or <b>headerEncoding</b> is null.</exception>
        public static new Mail_Message ParseFromStream(Stream stream,Encoding headerEncoding)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }
            if(headerEncoding == null){
                throw new ArgumentNullException("headerEncoding");
            }
                        
            Mail_Message retVal = new Mail_Message();
            retVal.Parse(new SmartStream(stream,false),headerEncoding,new MIME_h_ContentType("text/plain"));

            return retVal;
        }

        #endregion


        #region method GetAttachments

        /// <summary>
        /// Gets this message attachments.
        /// </summary>
        /// <param name="includeInline">Specifies if 'inline' entities are included.</param>
        /// <returns>Returns this message attachments.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public MIME_Entity[] GetAttachments(bool includeInline)
        {
            return GetAttachments(includeInline,true);
        }

        /// <summary>
        /// Gets this message attachments.
        /// </summary>
        /// <param name="includeInline">Specifies if 'inline' entities are included.</param>
        /// <param name="includeEmbbedMessage">Specifies if embbed RFC822 message child entities are included.</param>
        /// <returns>Returns this message attachments.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public MIME_Entity[] GetAttachments(bool includeInline,bool includeEmbbedMessage)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }

            List<MIME_Entity> retVal = new List<MIME_Entity>();
            foreach(MIME_Entity entity in GetAllEntities(includeEmbbedMessage)){
                MIME_h_ContentType contentType= null;
                try{
                    contentType = entity.ContentType;
                }
                catch{
                    // ContentType parsing failed.
                }
                MIME_h_ContentDisposition disposition = null;
                try{
                    disposition = entity.ContentDisposition;
                }
                catch{
                    // ContentDisposition parsing failed.
                }

                if(disposition != null && string.Equals(disposition.DispositionType,"attachment",StringComparison.InvariantCultureIgnoreCase)){
                    retVal.Add(entity);
                }
                else if(!includeInline && disposition != null && string.Equals(disposition.DispositionType,"inline",StringComparison.InvariantCultureIgnoreCase)){
                }
                else if(contentType != null && contentType.Type.ToLower() == "application"){
                    retVal.Add(entity);
                }
                else if(contentType != null && contentType.Type.ToLower() == "image"){
                    retVal.Add(entity);
                }
                else if(contentType != null && contentType.Type.ToLower() == "video"){
                    retVal.Add(entity);
                }
                else if(contentType != null && contentType.Type.ToLower() == "audio"){
                    retVal.Add(entity);
                }
                else if(contentType != null && contentType.Type.ToLower() == "message"){
                    retVal.Add(entity);
                }
            }

            return retVal.ToArray();
        }

        #endregion


        #region Properties Implementation

        // Permanent headerds list: http://www.rfc-editor.org/rfc/rfc4021.txt

        /// <summary>
        /// Gets or sets message date and time. Value <b>DateTime.MinValue</b> means not specified.
        /// </summary>
        /// <remarks>Specifies the date and time at which the creator of the message indicated that the 
        /// message was complete and ready to enter the mail delivery system.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public DateTime Date
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Date");
                if(h != null){
                    try{
                        return MIME_Utils.ParseRfc2822DateTime(((MIME_h_Unstructured)h).Value);
                    }
                    catch{
                        throw new ParseException("Header field 'Date' parsing failed.");
                    }
                }
                else{
                    return DateTime.MinValue;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == DateTime.MinValue){
                    this.Header.RemoveAll("Date");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Date");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("Date",MIME_Utils.DateTimeToRfc2822(value)));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("Date",MIME_Utils.DateTimeToRfc2822(value)));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets message author(s). Value null means not specified.
        /// </summary>
        /// <remarks>Specifies the author(s) of the message; that is, the mailbox(es) of the person(s) or 
        /// system(s) responsible for the writing of the message.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_MailboxList From
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("From");
                if(h != null){
                    if(!(h is Mail_h_MailboxList)){
                        throw new ParseException("Header field 'From' parsing failed.");
                    }

                    return ((Mail_h_MailboxList)h).Addresses;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("From");
                }
                else{
                    MIME_h h = this.Header.GetFirst("From");
                    if(h == null){
                        this.Header.Add(new Mail_h_MailboxList("From",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_MailboxList("From",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets message sender. Value null means not specified.
        /// </summary>
        /// <remarks>Specifies the mailbox of the agent responsible for the actual transmission of the message.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_Mailbox Sender
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Sender");
                if(h != null){
                    if(!(h is Mail_h_Mailbox)){
                        throw new ParseException("Header field 'Sender' parsing failed.");
                    }

                    return ((Mail_h_Mailbox)h).Address;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Sender");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Sender");
                    if(h == null){
                        this.Header.Add(new Mail_h_Mailbox("Sender",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_Mailbox("Sender",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets mailbox for replies to message. Value null means not specified.
        /// </summary>
        /// <remarks>When the "Reply-To:" field is present, it indicates the mailbox(es) to which the author of 
        /// the message suggests that replies be sent.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_AddressList ReplyTo
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Reply-To");
                if(h != null){
                    if(!(h is Mail_h_AddressList)){
                        throw new ParseException("Header field 'Reply-To' parsing failed.");
                    }

                    return ((Mail_h_AddressList)h).Addresses;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Reply-To");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Reply-To");
                    if(h == null){
                        this.Header.Add(new Mail_h_AddressList("Reply-To",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_AddressList("Reply-To",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets message primary recipient(s). Value null means not specified.
        /// </summary>
        /// <remarks>Contains the address(es) of the primary recipient(s) of the message.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_AddressList To
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("To");
                if(h != null){
                    if(!(h is Mail_h_AddressList)){
                        throw new ParseException("Header field 'To' parsing failed.");
                    }

                    return ((Mail_h_AddressList)h).Addresses;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("To");
                }
                else{
                    MIME_h h = this.Header.GetFirst("To");
                    if(h == null){
                        this.Header.Add(new Mail_h_AddressList("To",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_AddressList("To",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets carbon-copy recipient mailbox. Value null means not specified.
        /// </summary>
        /// <remarks>Contains the addresses of others who are to receive the message, though the content of the message may not be directed at them.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_AddressList Cc
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Cc");
                if(h != null){
                    if(!(h is Mail_h_AddressList)){
                        throw new ParseException("Header field 'Cc' parsing failed.");
                    }

                    return ((Mail_h_AddressList)h).Addresses;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Cc");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Cc");
                    if(h == null){
                        this.Header.Add(new Mail_h_AddressList("Cc",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_AddressList("Cc",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets blind-carbon-copy recipient mailbox. Value null means not specified.
        /// </summary>
        /// <remarks>Contains addresses of recipients of the message whose addresses are not to be revealed to other recipients of the message.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_AddressList Bcc
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Bcc");
                if(h != null){
                    if(!(h is Mail_h_AddressList)){
                        throw new ParseException("Header field 'Bcc' parsing failed.");
                    }

                    return ((Mail_h_AddressList)h).Addresses;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Bcc");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Bcc");
                    if(h == null){
                        this.Header.Add(new Mail_h_AddressList("Bcc",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_AddressList("Bcc",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets message identifier. Value null means not specified.
        /// </summary>
        /// <remarks>Contains a single unique message identifier that refers to a particular version of a particular message. 
        /// If the message is resent without changes, the original Message-ID is retained.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string MessageID 
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Message-ID");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Message-ID");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Message-ID");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("Message-ID",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("Message-ID",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets identify replied-to message(s). Value null means not specified.
        /// </summary>
        /// <remarks>The message identifier(s) of the original message(s) to which the current message is a reply.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string InReplyTo
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("In-Reply-To");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("In-Reply-To");
                }
                else{
                    MIME_h h = this.Header.GetFirst("In-Reply-To");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("In-Reply-To",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("In-Reply-To",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets related message identifier(s). Value null means not specified.
        /// </summary>
        /// <remarks>The message identifier(s) of other message(s) to which the current message may be related. 
        /// In RFC 2822, the definition was changed to say that this header field contains a list of all Message-IDs
        /// of messages in the preceding reply chain.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string References
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("References");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("References");
                }
                else{
                    MIME_h h = this.Header.GetFirst("References");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("References",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("References",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets topic of message. Value null means not specified.
        /// </summary>
        /// <remarks>Contains a short string identifying the topic of the message.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string Subject
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Subject");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Subject");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Subject");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("Subject",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("Subject",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets additional comments about the message. Value null means not specified.
        /// </summary>
        /// <remarks>Contains any additional comments on the text of the body of the message. 
        /// Warning: Some mailers will not show this field to recipients.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string Comments
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Comments");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Comments");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Comments");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("Comments",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("Comments",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets message key words and/or phrases. Value null means not specified.
        /// </summary>
        /// <remarks>Contains a comma-separated list of important words and phrases that might be useful for the recipient.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string Keywords
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Keywords");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Keywords");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Keywords");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("Keywords",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("Keywords",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets date and time message is resent. Value <b>DateTime.MinValue</b> means not specified.
        /// </summary>
        /// <remarks>Contains the date and time that a message is reintroduced into the message transfer system.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public DateTime ResentDate
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Resent-Date");
                if(h != null){
                    try{
                        return MIME_Utils.ParseRfc2822DateTime(((MIME_h_Unstructured)h).Value);
                    }
                    catch{
                        throw new ParseException("Header field 'Resent-Date' parsing failed.");
                    }
                }
                else{
                    return DateTime.MinValue;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == DateTime.MinValue){
                    this.Header.RemoveAll("Resent-Date");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Resent-Date");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("Resent-Date",MIME_Utils.DateTimeToRfc2822(value)));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("Resent-Date",MIME_Utils.DateTimeToRfc2822(value)));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets mailbox of person for whom message is resent. Value null means not specified.
        /// </summary>
        /// <remarks>Contains the mailbox of the agent who has reintroduced the message into 
        /// the message transfer system, or on whose behalf the message has been resent.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_MailboxList ResentFrom
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Resent-From");
                if(h != null){
                    if(!(h is Mail_h_MailboxList)){
                        throw new ParseException("Header field 'Resent-From' parsing failed.");
                    }

                    return ((Mail_h_MailboxList)h).Addresses;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Resent-From");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Resent-From");
                    if(h == null){
                        this.Header.Add(new Mail_h_MailboxList("Resent-From",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_MailboxList("Resent-From",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets mailbox of person who actually resends the message. Value null means not specified.
        /// </summary>
        /// <remarks>Contains the mailbox of the agent who has reintroduced the message into 
        /// the message transfer system, if this is different from the Resent-From value.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_Mailbox ResentSender
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Resent-Sender");
                if(h != null){
                    if(!(h is Mail_h_Mailbox)){
                        throw new ParseException("Header field 'Resent-Sender' parsing failed.");
                    }

                    return ((Mail_h_Mailbox)h).Address;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Resent-Sender");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Resent-Sender");
                    if(h == null){
                        this.Header.Add(new Mail_h_Mailbox("Resent-Sender",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_Mailbox("Resent-Sender",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets mailbox to which message is resent. Value null means not specified.
        /// </summary>
        /// <remarks>Contains the mailbox(es) to which the message has been resent.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_AddressList ResentTo
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Resent-To");
                if(h != null){
                    if(!(h is Mail_h_AddressList)){
                        throw new ParseException("Header field 'Resent-To' parsing failed.");
                    }

                    return ((Mail_h_AddressList)h).Addresses;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Resent-To");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Resent-To");
                    if(h == null){
                        this.Header.Add(new Mail_h_AddressList("Resent-To",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_AddressList("Resent-To",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets mailbox(es) to which message is cc'ed on resend. Value null means not specified.
        /// </summary>
        /// <remarks>Contains the mailbox(es) to which message is cc'ed on resend.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_AddressList ResentCc
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Resent-Cc");
                if(h != null){
                    if(!(h is Mail_h_AddressList)){
                        throw new ParseException("Header field 'Resent-Cc' parsing failed.");
                    }

                    return ((Mail_h_AddressList)h).Addresses;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Resent-Cc");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Resent-Cc");
                    if(h == null){
                        this.Header.Add(new Mail_h_AddressList("Resent-Cc",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_AddressList("Resent-Cc",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets mailbox(es) to which message is bcc'ed on resend. Value null means not specified.
        /// </summary>
        /// <remarks>Contains the mailbox(es) to which message is bcc'ed on resend.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_AddressList ResentBcc
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Resent-Bcc");
                if(h != null){
                    if(!(h is Mail_h_AddressList)){
                        throw new ParseException("Header field 'Resent-Bcc' parsing failed.");
                    }

                    return ((Mail_h_AddressList)h).Addresses;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Resent-Bcc");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Resent-Bcc");
                    if(h == null){
                        this.Header.Add(new Mail_h_AddressList("Resent-Bcc",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_AddressList("Resent-Bcc",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets resent reply-to. Value null means not specified.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_AddressList ResentReplyTo
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Resent-Reply-To");
                if(h != null){
                    if(!(h is Mail_h_AddressList)){
                        throw new ParseException("Header field 'Resent-Reply-To' parsing failed.");
                    }

                    return ((Mail_h_AddressList)h).Addresses;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Resent-Reply-To");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Resent-Reply-To");
                    if(h == null){
                        this.Header.Add(new Mail_h_AddressList("Resent-Reply-To",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_AddressList("Resent-Reply-To",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets message identifier for resent message. Value null means not specified.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string ResentMessageID
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Resent-Message-ID");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Resent-Message-ID");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Resent-Message-ID");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("Resent-Message-ID",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("Resent-Message-ID",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets message return path. Value null means not specified.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_h_ReturnPath ReturnPath
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Return-Path");
                if(h != null){
                    if(!(h is Mail_h_ReturnPath)){
                        throw new ParseException("Header field 'Return-Path' parsing failed.");
                    }

                    return (Mail_h_ReturnPath)h;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Return-Path");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Return-Path");
                    if(h == null){
                        this.Header.Add(value);
                    }
                    else{
                        this.Header.ReplaceFirst(value);
                    }
                }
            }
        }

        /// <summary>
        /// Gets mail transfer trace information. Value null means not specified.
        /// </summary>
        /// <remarks>Contains information about receipt of the current message by a mail transfer agent on the transfer path.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_h_Received[] Received
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h[] h = this.Header["Received"];
                if(h != null){
                    List<Mail_h_Received> retVal = new List<Mail_h_Received>();
                    for(int i=0;i<h.Length;i++){
                        if(!(h[i] is Mail_h_Received)){
                            throw new ParseException("Header field 'Received' parsing failed.");
                        }

                        retVal.Add((Mail_h_Received)h[i]);
                    }

                    return retVal.ToArray();
                }
                else{
                    return null;
                }
            }
        }

        // Obsoleted by RFC 2822.
        // public string Encypted

        /// <summary>
        /// Gets or sets mailboxes where to send disposition notification message. Value null means not specified.
        /// </summary>
        /// <remarks>Indicates that the sender wants a disposition notification when this message 
        /// is received (read, processed, etc.) by its recipients.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_t_MailboxList DispositionNotificationTo
        {
            // Defined RFC 3798 2.1.

            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Disposition-Notification-To");
                if(h != null){
                    if(!(h is Mail_h_MailboxList)){
                        throw new ParseException("Header field 'From' parsing failed.");
                    }

                    return ((Mail_h_MailboxList)h).Addresses;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Disposition-Notification-To");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Disposition-Notification-To");
                    if(h == null){
                        this.Header.Add(new Mail_h_MailboxList("Disposition-Notification-To",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new Mail_h_MailboxList("Disposition-Notification-To",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets disposition notification options. Value null means not specified.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public Mail_h_DispositionNotificationOptions DispositionNotificationOptions
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Disposition-Notification-Options");
                if(h != null){
                    if(!(h is Mail_h_DispositionNotificationOptions)){
                        throw new ParseException("Header field 'Disposition-Notification-Options' parsing failed.");
                    }

                    return (Mail_h_DispositionNotificationOptions)h;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Disposition-Notification-Options");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Disposition-Notification-Options");
                    if(h == null){
                        this.Header.Add(value);
                    }
                    else{
                        this.Header.ReplaceFirst(value);
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets language that the message sender requests to be used for responses. Value null means not specified.
        /// </summary>
        /// <remarks>
        /// Indicates a language that the message sender requests to be used for responses.
        /// </remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string AcceptLanguage
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Accept-Language");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Accept-Language");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Accept-Language");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("Accept-Language",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("Accept-Language",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets original message identifier. Value null means not specified.
        /// </summary>
        /// <remarks>Original message identifier used with resend of message with alternative content format; 
        /// identifies the original message data to which it corresponds.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string OriginalMessageID
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Original-Message-ID");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Original-Message-ID");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Original-Message-ID");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("Original-Message-ID",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("Original-Message-ID",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets PICS rating label. Value null means not specified.
        /// </summary>
        /// <remarks>Ratings label to control selection (filtering) of messages according to the PICS protocol.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string PICSLabel
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("PICS-Label");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("PICS-LabelD");
                }
                else{
                    MIME_h h = this.Header.GetFirst("PICS-Label");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("PICS-Label",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("PICS-Label",value));
                    }
                }
            }
        }

        
        // Not widely used
        // public string Encoding

        /// <summary>
        /// Gets or sets URL of mailing list archive. Value null means not specified.
        /// </summary>
        /// <remarks>Contains the URL to use to browse the archives of the mailing list from which this message was relayed.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string ListArchive
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("List-Archive");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("List-Archive");
                }
                else{
                    MIME_h h = this.Header.GetFirst("List-Archive");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("List-Archive",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("List-Archive",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets URL for mailing list information. Value null means not specified.
        /// </summary>
        /// <remarks>Contains the URL to use to get information about the mailing list from which this message was relayed.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string ListHelp
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("List-Help");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("List-Help");
                }
                else{
                    MIME_h h = this.Header.GetFirst("List-Help");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("List-Help",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("List-Help",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets mailing list identifier. Value null means not specified.
        /// </summary>
        /// <remarks>Stores an identification of the mailing list through which this message was distributed.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string ListID
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("List-ID");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("List-ID");
                }
                else{
                    MIME_h h = this.Header.GetFirst("List-ID");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("List-ID",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("List-ID",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets URL for mailing list owner's mailbox. Value null means not specified.
        /// </summary>
        /// <remarks>Contains the URL to send e-mail to the owner of the mailing list from which this message was relayed.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string ListOwner
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("List-Owner");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("List-Owner");
                }
                else{
                    MIME_h h = this.Header.GetFirst("List-Owner");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("List-Owner",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("List-Owner",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets URL for mailing list posting. Value null means not specified.
        /// </summary>
        /// <remarks>Contains the URL to use to send contributions to the mailing list from which this message was relayed.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string ListPost
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("List-Post");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("List-Post");
                }
                else{
                    MIME_h h = this.Header.GetFirst("List-Post");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("List-Post",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("List-Post",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets URL for mailing list subscription. Value null means not specified.
        /// </summary>
        /// <remarks>Contains the URL to use to get a subscription to the mailing list from which this message was relayed.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string ListSubscribe
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("List-Subscribe");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("List-Subscribe");
                }
                else{
                    MIME_h h = this.Header.GetFirst("List-Subscribe");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("List-Subscribe",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("List-Subscribe",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets URL for mailing list unsubscription. Value null means not specified.
        /// </summary>
        /// <remarks>Contains the URL to use to unsubscribe the mailing list from which this message was relayed.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string ListUnsubscribe
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("List-Unsubscribe");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("List-Unsubscribe");
                }
                else{
                    MIME_h h = this.Header.GetFirst("List-Unsubscribe");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("List-Unsubscribe",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("List-Unsubscribe",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets type or context of message. Value null means not specified.
        /// </summary>
        /// <remarks>Provides information about the context and presentation characteristics of a message. 
        /// Can have the values 'voice-message', 'fax-message', 'pager-message', 'multimedia-message', 'text-message', or 'none'.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string MessageContext
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Message-Context");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Message-Context");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Message-Context");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("Message-Context",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("Message-Context",value));
                    }
                }
            }
        }

        // Not for general use
        //public string AlternateRecipient

        // Obsolete
        //public string ContentReturn

        // Not for general use
        // public string GenerateDeliveryReport

        // Not for general use
        // public string PreventNonDeliveryReport

        // Obsolete
        // public string ContentIdentifier

        // Not for general use
        // public string DeliveryDate

        // Obsolete
        // public string ExpiryDate

        // Not for general use
        // public string Expires

        // Not for general use
        // public string ReplyBy

        /// <summary>
        /// Gets or sets message importance. Value null means not specified.
        /// </summary>
        /// <remarks>A hint from the originator to the recipients about how important a message is. 
        /// Values: High, normal, or low.  Not used to control transmission speed.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string Importance
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Importance");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Importance");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Importance");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("Importance",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("Importance",value));
                    }
                }
            }
        }

        /// <summary>
        /// Gets or sets message priority. Value null means not specified.
        /// </summary>
        /// <remarks>Can be 'normal', 'urgent', or 'non-urgent' and can influence transmission speed and delivery.</remarks>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string Priority
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                MIME_h h = this.Header.GetFirst("Priority");
                if(h != null){
                    return ((MIME_h_Unstructured)h).Value;
                }
                else{
                    return null;
                }
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                if(value == null){
                    this.Header.RemoveAll("Priority");
                }
                else{
                    MIME_h h = this.Header.GetFirst("Priority");
                    if(h == null){
                        this.Header.Add(new MIME_h_Unstructured("Priority",value));
                    }
                    else{
                        this.Header.ReplaceFirst(new MIME_h_Unstructured("Priority",value));
                    }
                }
            }
        }

        // Not for general use
        // public string Sensitivity

        // Not for general use
        // public string Language

        // Not for general use
        // public string MessageType

        // Not for general use
        // public string Autosubmitted

        // Not for general use
        // public string Autoforwarded

        // Not for general use
        // public string DiscloseRecipients

        // Not for general use
        // public string DeferredDelivery

        // Not for general use
        // public string LatestDeliveryTime

        // Not for general use
        // public string OriginatorReturnAddress


        /// <summary>
        /// Gets this message attachments.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public MIME_Entity[] Attachments
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return GetAttachments(false);
            }
        }

        /// <summary>
        /// Gets message body text. Returns null if no body text available.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string BodyText
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                foreach(MIME_Entity e in GetAllEntities(false)){
                    if(e.Body.MediaType.ToLower() == MIME_MediaTypes.Text.plain){
                        return ((MIME_b_Text)e.Body).Text;
                    }
                }

                return null; 
            }
        }

        /// <summary>
        /// Gets message body html text. Returns null if no body html text available.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string BodyHtmlText
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                foreach(MIME_Entity e in GetAllEntities(false)){
                    if(e.Body.MediaType.ToLower() == MIME_MediaTypes.Text.html){
                        return ((MIME_b_Text)e.Body).Text;
                    }
                }

                return null; 
            }
        }

        #endregion

    }
}
