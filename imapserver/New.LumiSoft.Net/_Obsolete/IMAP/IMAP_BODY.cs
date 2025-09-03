using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.Mime;
using LumiSoft.Net.MIME;
using LumiSoft.Net.Mail;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// IMAP BODYSTRUCTURE. Defined in RFC 3501 7.4.2.
    /// </summary>
    public class IMAP_BODY
    {
        private IMAP_BODY_Entity m_pMainEntity = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public IMAP_BODY()
        {
            m_pMainEntity = new IMAP_BODY_Entity();
        }


        #region static mehtod ConstructBodyStructure

		/// <summary>
		/// Constructs FETCH BODY and BODYSTRUCTURE response.
		/// </summary>
		/// <param name="message">Mail message.</param>
		/// <param name="bodystructure">Specifies if to construct BODY or BODYSTRUCTURE.</param>
		/// <returns></returns>
		public static string ConstructBodyStructure(Mail_Message message,bool bodystructure)
		{			
			if(bodystructure){
				return "BODYSTRUCTURE " + ConstructParts(message,bodystructure);
			}
			else{
				return "BODY " + ConstructParts(message,bodystructure);
			}
		}

		/// <summary>
		/// Constructs specified entity and it's childentities bodystructure string.
		/// </summary>
		/// <param name="entity">Mime entity.</param>
		/// <param name="bodystructure">Specifies if to construct BODY or BODYSTRUCTURE.</param>
		/// <returns></returns>
		private static string ConstructParts(MIME_Entity entity,bool bodystructure)
		{
			/* RFC 3501 7.4.2 BODYSTRUCTURE
							  BODY A form of BODYSTRUCTURE without extension data.
			  
				A parenthesized list that describes the [MIME-IMB] body
				structure of a message.  This is computed by the server by
				parsing the [MIME-IMB] header fields, defaulting various fields
				as necessary.

				For example, a simple text message of 48 lines and 2279 octets
				can have a body structure of: ("TEXT" "PLAIN" ("CHARSET"
				"US-ASCII") NIL NIL "7BIT" 2279 48)

				Multiple parts are indicated by parenthesis nesting.  Instead
				of a body type as the first element of the parenthesized list,
				there is a sequence of one or more nested body structures.  The
				second element of the parenthesized list is the multipart
				subtype (mixed, digest, parallel, alternative, etc.).
					
				For example, a two part message consisting of a text and a
				BASE64-encoded text attachment can have a body structure of:
				(("TEXT" "PLAIN" ("CHARSET" "US-ASCII") NIL NIL "7BIT" 1152
				23)("TEXT" "PLAIN" ("CHARSET" "US-ASCII" "NAME" "cc.diff")
				"<960723163407.20117h@cac.washington.edu>" "Compiler diff"
				"BASE64" 4554 73) "MIXED")

				Extension data follows the multipart subtype.  Extension data
				is never returned with the BODY fetch, but can be returned with
				a BODYSTRUCTURE fetch.  Extension data, if present, MUST be in
				the defined order.  The extension data of a multipart body part
				are in the following order:

				body parameter parenthesized list
					A parenthesized list of attribute/value pairs [e.g., ("foo"
					"bar" "baz" "rag") where "bar" is the value of "foo", and
					"rag" is the value of "baz"] as defined in [MIME-IMB].

				body disposition
					A parenthesized list, consisting of a disposition type
					string, followed by a parenthesized list of disposition
					attribute/value pairs as defined in [DISPOSITION].

				body language
					A string or parenthesized list giving the body language
					value as defined in [LANGUAGE-TAGS].

				body location
					A string list giving the body content URI as defined in [LOCATION].

				Any following extension data are not yet defined in this
				version of the protocol.  Such extension data can consist of
				zero or more NILs, strings, numbers, or potentially nested
				parenthesized lists of such data.  Client implementations that
				do a BODYSTRUCTURE fetch MUST be prepared to accept such
				extension data.  Server implementations MUST NOT send such
				extension data until it has been defined by a revision of this
				protocol.

				The basic fields of a non-multipart body part are in the
				following order:

				body type
					A string giving the content media type name as defined in [MIME-IMB].
				
				body subtype
					 A string giving the content subtype name as defined in [MIME-IMB].

				body parameter parenthesized list
					A parenthesized list of attribute/value pairs [e.g., ("foo"
					"bar" "baz" "rag") where "bar" is the value of "foo" and
					"rag" is the value of "baz"] as defined in [MIME-IMB].

				body id
					A string giving the content id as defined in [MIME-IMB].

				body description
					A string giving the content description as defined in [MIME-IMB].

				body encoding
					A string giving the content transfer encoding as defined in	[MIME-IMB].

				body size
					A number giving the size of the body in octets.  Note that
					this size is the size in its transfer encoding and not the
					resulting size after any decoding.

				A body type of type MESSAGE and subtype RFC822 contains,
				immediately after the basic fields, the envelope structure,
				body structure, and size in text lines of the encapsulated
				message.

				A body type of type TEXT contains, immediately after the basic
				fields, the size of the body in text lines.  Note that this
				size is the size in its content transfer encoding and not the
				resulting size after any decoding.

				Extension data follows the basic fields and the type-specific
				fields listed above.  Extension data is never returned with the
				BODY fetch, but can be returned with a BODYSTRUCTURE fetch.
				Extension data, if present, MUST be in the defined order.

				The extension data of a non-multipart body part are in the
				following order:

				body MD5
					A string giving the body MD5 value as defined in [MD5].
					
				body disposition
					A parenthesized list with the same content and function as
					the body disposition for a multipart body part.

				body language
					A string or parenthesized list giving the body language
					value as defined in [LANGUAGE-TAGS].

				body location
					A string list giving the body content URI as defined in [LOCATION].

				Any following extension data are not yet defined in this
				version of the protocol, and would be as described above under
				multipart extension data.
			
			
				// We don't construct extention fields like rfc says:
					Server implementations MUST NOT send such
					extension data until it has been defined by a revision of this
					protocol.
			
										
				contentTypeMainMediaType - Example: 'TEXT'
				contentTypeSubMediaType  - Example: 'PLAIN'
				conentTypeParameters     - Example: '("CHARSET" "iso-8859-1" ...)'
				contentID                - Content-ID: header field value.
				contentDescription       - Content-Description: header field value.
				contentEncoding          - Content-Transfer-Encoding: header field value.
				contentSize              - mimeEntity ENCODED data size
				[envelope]               - NOTE: included only if contentType = "message" !!!
				[contentLines]           - number of ENCODED data lines. NOTE: included only if contentType = "text" !!!
									   			
				// Basic fields for multipart
				(nestedMimeEntries) contentTypeSubMediaType
												
				// Basic fields for non-multipart
				contentTypeMainMediaType contentTypeSubMediaType (conentTypeParameters) contentID contentDescription contentEncoding contentSize [envelope] [contentLine]

			*/

            MIME_Encoding_EncodedWord wordEncoder = new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8);
            wordEncoder.Split = false;

			StringBuilder retVal = new StringBuilder();
			// Multipart message
			if(entity.Body is MIME_b_Multipart){
				retVal.Append("(");

				// Construct child entities.
				foreach(MIME_Entity childEntity in ((MIME_b_Multipart)entity.Body).BodyParts){
					// Construct child entity. This can be multipart or non multipart.
					retVal.Append(ConstructParts(childEntity,bodystructure));
				}
			
				// Add contentTypeSubMediaType
                if(entity.ContentType != null && entity.ContentType.SubType != null){
                    retVal.Append(" \"" + entity.ContentType.SubType + "\"");
                }
                else{
                    retVal.Append(" NIL");
                }

				retVal.Append(")");
			}
			// Single part message
			else{
				retVal.Append("(");

				// NOTE: all header fields and parameters must in ENCODED form !!!

				// Add contentTypeMainMediaType
				if(entity.ContentType != null && entity.ContentType.Type != null){
					retVal.Append("\"" + entity.ContentType.Type + "\"");
				}
				else{
					retVal.Append("NIL");
				}

                // Add contentTypeSubMediaType
                if(entity.ContentType != null && entity.ContentType.SubType != null){
                    retVal.Append(" \"" + entity.ContentType.SubType + "\"");
                }
                else{
                    retVal.Append(" NIL");
                }

				// conentTypeParameters - Syntax: {("name" SP "value" *(SP "name" SP "value"))}
				if(entity.ContentType != null){
                    if(entity.ContentType.Parameters.Count > 0){
                        retVal.Append(" (");
                        bool first = true;
                        foreach(MIME_h_Parameter parameter in entity.ContentType.Parameters){
                            // For the first item, don't add SP.
                            if(first){
                                first = false;
                            }
                            else{
                                retVal.Append(" ");
                            }

                            retVal.Append("\"" + parameter.Name + "\" \"" + wordEncoder.Encode(parameter.Value) + "\"");
                        }
                        retVal.Append(")");
                    }
                    else{
                        retVal.Append(" NIL");
                    }
				}
				else{
					retVal.Append(" NIL");
				}

				// contentID
				string contentID = entity.ContentID;
				if(contentID != null){
					retVal.Append(" \"" + wordEncoder.Encode(contentID) + "\""); 
				}
				else{
					retVal.Append(" NIL");
				}

				// contentDescription
				string contentDescription = entity.ContentDescription;
				if(contentDescription != null){
					retVal.Append(" \"" + wordEncoder.Encode(contentDescription) + "\""); 
				}
				else{
					retVal.Append(" NIL");
				}

				// contentEncoding
				if(entity.ContentTransferEncoding != null){
					retVal.Append(" \"" + wordEncoder.Encode(entity.ContentTransferEncoding) + "\""); 
				}
				else{
					// If not specified, then must be 7bit.
					retVal.Append(" \"7bit\"");
				}

				// contentSize
				if(entity.Body is MIME_b_SinglepartBase){                    
					retVal.Append(" " + ((MIME_b_SinglepartBase)entity.Body).EncodedData.Length.ToString());
				}
				else{
					retVal.Append(" 0");
				}

				// envelope ---> FOR ContentType: message/rfc822 ONLY ###
				if(entity.Body is MIME_b_MessageRfc822){                    
					retVal.Append(" " + IMAP_Envelope.ConstructEnvelope(((MIME_b_MessageRfc822)entity.Body).Message));

                    // TODO: BODYSTRUCTURE,LINES
				}

				// contentLines ---> FOR ContentType: text/xxx ONLY ###
				if(entity.Body is MIME_b_Text){                    
				    long lineCount = 0;
					StreamLineReader r = new StreamLineReader(new MemoryStream(((MIME_b_SinglepartBase)entity.Body).EncodedData));
					byte[] line = r.ReadLine();
					while(line != null){
						lineCount++;

						line = r.ReadLine();
					}
						
					retVal.Append(" " + lineCount.ToString());
				}

				retVal.Append(")");
			}

			return retVal.ToString();
		}

		#endregion


        #region method Parse

        /// <summary>
        /// Parses IMAP BODYSTRUCTURE from body structure string.
        /// </summary>
        /// <param name="bodyStructureString">Body structure string</param>
        public void Parse(string bodyStructureString)
        {
            m_pMainEntity = new IMAP_BODY_Entity();
            m_pMainEntity.Parse(bodyStructureString);
        }

        #endregion


        #region method GetEntities

		/// <summary>
		/// Gets mime entities, including nested entries. 
		/// </summary>
		/// <param name="entities"></param>
		/// <param name="allEntries"></param>
		private void GetEntities(IMAP_BODY_Entity[] entities,List<IMAP_BODY_Entity> allEntries)
		{				
			if(entities != null){
				foreach(IMAP_BODY_Entity ent in entities){
					allEntries.Add(ent);

					// Add child entities, if any
					if(ent.ChildEntities.Length > 0){
						GetEntities(ent.ChildEntities,allEntries);
					}
				}
			}
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets main entity.
        /// </summary>
        public IMAP_BODY_Entity MainEntity
        {
            get{ return m_pMainEntity; }
        }

        /// <summary>
        /// Gets all entities contained in BODYSTRUCTURE, including child entities.
        /// </summary>
        public IMAP_BODY_Entity[] Entities
        {
            get{ 
                List<IMAP_BODY_Entity> allEntities = new List<IMAP_BODY_Entity>();
				allEntities.Add(m_pMainEntity);
				GetEntities(m_pMainEntity.ChildEntities,allEntities);

				return allEntities.ToArray();
            }
        }

        /// <summary>
        /// Gets attachment entities. Entity is considered as attachmnet if:<p/>
        ///     *) Content-Type: name = "" is specified  (old RFC 822 message)<p/>
        /// </summary>
        public IMAP_BODY_Entity[] Attachmnets
        {
            // Cant use these at moment, we need to use extended BODYSTRUCTURE fo that    
        //     *) Content-Disposition: attachment (RFC 2822 message)<p/>
        //     *) Content-Disposition: filename = "" is specified  (RFC 2822 message)<p/>

            get{ 
                List<IMAP_BODY_Entity> attachments = new List<IMAP_BODY_Entity>();
				IMAP_BODY_Entity[] entities = this.Entities;
				foreach(IMAP_BODY_Entity entity in entities){
                    if(entity.ContentType != null){
                        foreach(MIME_h_Parameter parameter in entity.ContentType.Parameters){
                            if(parameter.Name.ToLower() == "name"){
                                attachments.Add(entity);
                                break;
                            }
                        }
                    }
				}

                return attachments.ToArray();
            }
        }

        #endregion

    }
}
