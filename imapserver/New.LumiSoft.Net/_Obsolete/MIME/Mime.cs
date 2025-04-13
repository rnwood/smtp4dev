using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using LumiSoft.Net.IO;

namespace LumiSoft.Net.Mime
{
	/// <summary>
	/// Class for creating,parsing,modifing rfc 2822 mime messages.
	/// </summary>
	/// <remarks>
	/// <code>
	/// 
	/// Message examples:
	/// 
	/// <B>Simple message:</B>
	/// 
	/// //--- Beginning of message
	/// From: sender@domain.com
	/// To: recipient@domain.com
	/// Subject: Message subject.
	/// Content-Type: text/plain
	/// 
	/// Message body text. Bla blaa
	/// blaa,blaa.
	/// //--- End of message
	/// 
	/// 
	/// In simple message MainEntity is whole message.
	/// 
	/// <B>Message with attachments:</B>
	/// 
	/// //--- Beginning of message
	/// From: sender@domain.com
	/// To: recipient@domain.com
	/// Subject: Message subject.
	/// Content-Type: multipart/mixed; boundary="multipart_mixed"
	/// 
	/// --multipart_mixed	/* text entity */
	///	Content-Type: text/plain
	///	
	///	Message body text. Bla blaa
	///	blaa,blaa.	
	///	--multipart_mixed	/* attachment entity */
	///	Content-Type: application/octet-stream
	///	
	///	attachment_data
	///	--multipart_mixed--
	///	//--- End of message
	///	
	///	MainEntity is multipart_mixed entity and text and attachment entities are child entities of MainEntity.
	/// </code>
	/// </remarks>
	/// <example>
	/// <code>
	/// // Parsing example:
	/// Mime m = Mime.Parse("message.eml");
	/// // Do your stuff with mime
	/// </code>
	/// <code>
	/// // Create simple message with simple way:
	/// AddressList from = new AddressList();
	/// from.Add(new MailboxAddress("dispaly name","user@domain.com"));
	///	AddressList to = new AddressList();
	///	to.Add(new MailboxAddress("dispaly name","user@domain.com"));
	///	
	///	Mime m = Mime.CreateSimple(from,to,"test subject","test body text","");
	/// </code>
	/// <code>
	/// // Creating a new simple message
	/// Mime m = new Mime();
	/// MimeEntity mainEntity = m.MainEntity;
	/// // Force to create From: header field
	/// mainEntity.From = new AddressList();
	/// mainEntity.From.Add(new MailboxAddress("dispaly name","user@domain.com"));
	/// // Force to create To: header field
	/// mainEntity.To = new AddressList();
	/// mainEntity.To.Add(new MailboxAddress("dispaly name","user@domain.com"));
	/// mainEntity.Subject = "subject";
	/// mainEntity.ContentType = MediaType_enum.Text_plain;
	/// mainEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
	/// mainEntity.DataText = "Message body text.";
	/// 
	/// m.ToFile("message.eml");
	/// </code>
	/// <code>
	/// // Creating message with text and attachments
	/// Mime m = new Mime();
	/// MimeEntity mainEntity = m.MainEntity;
	/// // Force to create From: header field
	/// mainEntity.From = new AddressList();
	/// mainEntity.From.Add(new MailboxAddress("dispaly name","user@domain.com"));
	/// // Force to create To: header field
	/// mainEntity.To = new AddressList();
	/// mainEntity.To.Add(new MailboxAddress("dispaly name","user@domain.com"));
	/// mainEntity.Subject = "subject";
	/// mainEntity.ContentType = MediaType_enum.Multipart_mixed;
	/// 
	/// MimeEntity textEntity = mainEntity.ChildEntities.Add();
	/// textEntity.ContentType = MediaType_enum.Text_plain;
	/// textEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
	/// textEntity.DataText = "Message body text.";
	/// 
	/// MimeEntity attachmentEntity = mainEntity.ChildEntities.Add();
	/// attachmentEntity.ContentType = MediaType_enum.Application_octet_stream;
	/// attachmentEntity.ContentDisposition = ContentDisposition_enum.Attachment;
	/// attachmentEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
	/// attachmentEntity.ContentDisposition_FileName = "yourfile.xxx";
	/// attachmentEntity.DataFromFile("yourfile.xxx");
	/// // or
	/// attachmentEntity.Data = your_attachment_data;
	/// </code>
	/// </example>
    [Obsolete("See LumiSoft.Net.MIME or LumiSoft.Net.Mail namepaces for replacement.")]
	public class Mime
	{
		private MimeEntity m_pMainEntity = null;

		/// <summary>
		/// Default constructor.
		/// </summary>
		public Mime()
		{
			m_pMainEntity = new MimeEntity();

			// Add default header fields
			m_pMainEntity.MessageID = MimeUtils.CreateMessageID();
			m_pMainEntity.Date = DateTime.Now;
			m_pMainEntity.MimeVersion = "1.0";
		}


		#region static method Parse

		/// <summary>
		/// Parses mime message from byte[] data.
		/// </summary>
		/// <param name="data">Mime message data.</param>
		/// <returns></returns>
		public static Mime Parse(byte[] data)
		{
			using(MemoryStream ms = new MemoryStream(data)){
				return Parse(ms);
			}
		}

		/// <summary>
		/// Parses mime message from file.
		/// </summary>
		/// <param name="fileName">Mime message file.</param>
		/// <returns></returns>
		public static Mime Parse(string fileName)
		{
			using(FileStream fs = File.OpenRead(fileName)){
				return Parse(fs);
			}
		}

		/// <summary>
		/// Parses mime message from stream.
		/// </summary>
		/// <param name="stream">Mime message stream.</param>
		/// <returns></returns>
		public static Mime Parse(Stream stream)
		{
			Mime mime = new Mime();
			mime.MainEntity.Parse(new SmartStream(stream,false),null);

			return mime;
		}

		#endregion


		#region static method CreateSimple

		/// <summary>
		/// Creates simple mime message.
		/// </summary>
		/// <param name="from">Header field From: value.</param>
		/// <param name="to">Header field To: value.</param>
		/// <param name="subject">Header field Subject: value.</param>
		/// <param name="bodyText">Body text of message. NOTE: Pass null is body text isn't wanted.</param>
		/// <param name="bodyHtml">Body HTML text of message. NOTE: Pass null is body HTML text isn't wanted.</param>
		/// <returns></returns>
		public static Mime CreateSimple(AddressList from,AddressList to,string subject,string bodyText,string bodyHtml)
		{
			return CreateSimple(from,to,subject,bodyText,bodyHtml,null);
		}

		/// <summary>
		/// Creates simple mime message with attachments.
		/// </summary>
		/// <param name="from">Header field From: value.</param>
		/// <param name="to">Header field To: value.</param>
		/// <param name="subject">Header field Subject: value.</param>
		/// <param name="bodyText">Body text of message. NOTE: Pass null is body text isn't wanted.</param>
		/// <param name="bodyHtml">Body HTML text of message. NOTE: Pass null is body HTML text isn't wanted.</param>
		/// <param name="attachmentFileNames">Attachment file names. Pass null if no attachments. NOTE: File name must contain full path to file, for example: c:\test.pdf.</param>
		/// <returns></returns>
		public static Mime CreateSimple(AddressList from,AddressList to,string subject,string bodyText,string bodyHtml,string[] attachmentFileNames)
		{
			Mime m = new Mime();

			MimeEntity mainEntity = m.MainEntity;
			mainEntity.From = from;
			mainEntity.To = to;
			mainEntity.Subject = subject;

			// There are no atachments
			if(attachmentFileNames == null || attachmentFileNames.Length == 0){
				// If bodyText and bodyHtml both specified
				if(bodyText != null && bodyHtml != null){
					mainEntity.ContentType = MediaType_enum.Multipart_alternative;

					MimeEntity textEntity = mainEntity.ChildEntities.Add();
					textEntity.ContentType = MediaType_enum.Text_plain;
					textEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
					textEntity.DataText = bodyText;

					MimeEntity textHtmlEntity = mainEntity.ChildEntities.Add();
					textHtmlEntity.ContentType = MediaType_enum.Text_html;
					textHtmlEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
					textHtmlEntity.DataText = bodyHtml;
				}
				// There is only body text
				else if(bodyText != null){
					MimeEntity textEntity = mainEntity;
					textEntity.ContentType = MediaType_enum.Text_plain;
					textEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
					textEntity.DataText = bodyText;
				}
				// There is only body html text
				else if(bodyHtml != null){
					MimeEntity textHtmlEntity = mainEntity;
					textHtmlEntity.ContentType = MediaType_enum.Text_html;
					textHtmlEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
					textHtmlEntity.DataText = bodyHtml;
				}
			}
			// There are attachments
			else{				
				mainEntity.ContentType = MediaType_enum.Multipart_mixed;

				// If bodyText and bodyHtml both specified
				if(bodyText != null && bodyHtml != null){
					MimeEntity multiPartAlternativeEntity = mainEntity.ChildEntities.Add();
					multiPartAlternativeEntity.ContentType = MediaType_enum.Multipart_alternative;

					MimeEntity textEntity = multiPartAlternativeEntity.ChildEntities.Add();
					textEntity.ContentType = MediaType_enum.Text_plain;
					textEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
					textEntity.DataText = bodyText;

					MimeEntity textHtmlEntity = multiPartAlternativeEntity.ChildEntities.Add();
					textHtmlEntity.ContentType = MediaType_enum.Text_html;
					textHtmlEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
					textHtmlEntity.DataText = bodyHtml;
				}
				// There is only body text
				else if(bodyText != null){
					MimeEntity textEntity = mainEntity.ChildEntities.Add();
					textEntity.ContentType = MediaType_enum.Text_plain;
					textEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
					textEntity.DataText = bodyText;
				}
				// There is only body html text
				else if(bodyHtml != null){
					MimeEntity textHtmlEntity = mainEntity.ChildEntities.Add();
					textHtmlEntity.ContentType = MediaType_enum.Text_html;
					textHtmlEntity.ContentTransferEncoding = ContentTransferEncoding_enum.QuotedPrintable;
					textHtmlEntity.DataText = bodyHtml;
				}

				foreach(string fileName in attachmentFileNames){
					MimeEntity attachmentEntity = mainEntity.ChildEntities.Add();
					attachmentEntity.ContentType = MediaType_enum.Application_octet_stream;
					attachmentEntity.ContentDisposition = ContentDisposition_enum.Attachment;
					attachmentEntity.ContentTransferEncoding = ContentTransferEncoding_enum.Base64;
					attachmentEntity.ContentDisposition_FileName = Core.GetFileNameFromPath(fileName);
					attachmentEntity.DataFromFile(fileName);
				}
			}

			return m;
		}

		#endregion


		#region method ToStringData

		/// <summary>
		/// Stores mime message to string.
		/// </summary>
		/// <returns></returns>
		public string ToStringData()
		{
			return System.Text.Encoding.Default.GetString(this.ToByteData());
		}

		#endregion

		#region method ToByteData

		/// <summary>
		/// Stores mime message to byte[].
		/// </summary>
		/// <returns></returns>
		public byte[] ToByteData()
		{
			using(MemoryStream ms = new MemoryStream()){
				ToStream(ms);

				return ms.ToArray();
			}
		}

		#endregion

		#region method ToFile

		/// <summary>
		/// Stores mime message to specified file.
		/// </summary>
		/// <param name="fileName">File name.</param>
		public void ToFile(string fileName)
		{
			using(FileStream fs = File.Create(fileName)){
				ToStream(fs);
			}
		}

		#endregion

		#region method ToStream

		/// <summary>
		/// Stores mime message to specified stream. Stream position stays where mime writing ends.
		/// </summary>
		/// <param name="storeStream">Stream where to store mime message.</param>
		public void ToStream(Stream storeStream)
		{
			m_pMainEntity.ToStream(storeStream);
		}

		#endregion


		#region method GetEntities

		/// <summary>
		/// Gets mime entities, including nested entries. 
		/// </summary>
		/// <param name="entities"></param>
		/// <param name="allEntries"></param>
		private void GetEntities(MimeEntityCollection entities,List<MimeEntity> allEntries)
		{				
			if(entities != null){
				foreach(MimeEntity ent in entities){
					allEntries.Add(ent);

					// Add child entities, if any
					if(ent.ChildEntities.Count > 0){
						GetEntities(ent.ChildEntities,allEntries);
					}
				}
			}
		}

		#endregion


		#region Properties Implementaion

		/// <summary>
		/// Message main(top-level) entity.
		/// </summary>
		public MimeEntity MainEntity
		{
			get{ return m_pMainEntity; }
		}

		/// <summary>
		/// Gets all mime entities contained in message, including child entities.
		/// </summary>
		public MimeEntity[] MimeEntities
		{
			get{ 
				List<MimeEntity> allEntities = new List<MimeEntity>();
				allEntities.Add(m_pMainEntity);
				GetEntities(m_pMainEntity.ChildEntities,allEntities);

				return allEntities.ToArray(); 
			}
		}
		
		/// <summary>
		/// Gets attachment entities. Entity is considered as attachmnet if:<p/>
        ///     *) Content-Disposition: attachment (RFC 2822 message)<p/>
        ///     *) Content-Disposition: filename = "" is specified  (RFC 2822 message)<p/>
        ///     *) Content-Type: name = "" is specified  (old RFC 822 message)<p/>
		/// </summary>
		public MimeEntity[] Attachments
		{
			get{                
                List<MimeEntity> attachments = new List<MimeEntity>();
				MimeEntity[] entities = this.MimeEntities;
				foreach(MimeEntity entity in entities){
                    if(entity.ContentDisposition == ContentDisposition_enum.Attachment){
                        attachments.Add(entity);
                    }
                    else if(entity.ContentType_Name != null){
                        attachments.Add(entity);
                    }
                    else if(entity.ContentDisposition_FileName != null){
                        attachments.Add(entity);
                    }
				}

                return attachments.ToArray();
			}
		}
			
		/// <summary>
		/// Gets message body text. Returns null if no body text specified.
		/// </summary>
		public string BodyText
		{
			get{
                /* RFC 2045 5.2 
                    If content Content-Type: header field is missing, then it defaults to:
                        Content-type: text/plain; charset=us-ascii
                */

				if(this.MainEntity.ContentType == MediaType_enum.NotSpecified){
					if(this.MainEntity.DataEncoded != null){
						return System.Text.Encoding.ASCII.GetString(this.MainEntity.Data);
					}
				}
				else{
					MimeEntity[] entities = this.MimeEntities;
					foreach(MimeEntity entity in entities){
						if(entity.ContentType == MediaType_enum.Text_plain){
							return entity.DataText;
						}
					}
				}

				return null;
			}
		}
/*
		/// <summary>
		/// Gets body text mime entity. Returns null if no body body text entity.
		/// </summary>
		public MimeEntity BodyTextEntity
		{
			get{
				if(this.MainEntity.ContentType == MediaType_enum.NotSpecified){
					if(this.MainEntity.DataEncoded != null){
						return this.MainEntity;
					}
				}
				else{
					MimeEntity[] entities = this.MimeEntities;
					foreach(MimeEntity entity in entities){
						if(entity.ContentType == MediaType_enum.Text_plain){
							return entity;
						}
					}
				}

				return null;
			}
		}
*/
		/// <summary>
		/// Gets message body html. Returns null if no body html text specified.
		/// </summary>
		public string BodyHtml
		{
			get{
				MimeEntity[] entities = this.MimeEntities;
				foreach(MimeEntity entity in entities){
					if(entity.ContentType == MediaType_enum.Text_html){
						return entity.DataText;
					}
				}

				return null;
			}
		}
		
		#endregion

	}
}
