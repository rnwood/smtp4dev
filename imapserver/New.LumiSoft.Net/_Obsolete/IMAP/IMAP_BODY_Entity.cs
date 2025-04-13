using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.MIME;

namespace LumiSoft.Net.IMAP
{
    /// <summary>
    /// IMAP BODY mime entity info.
    /// </summary>
    public class IMAP_BODY_Entity
    {
        private IMAP_BODY_Entity             m_pParentEntity      = null;
        private List<IMAP_BODY_Entity>       m_pChildEntities     = null;
        private MIME_h_ContentType           m_pContentType       = null;
        private string                       m_ContentID          = null;
        private string                       m_ContentDescription = null;
        private string                       m_ContentEncoding    = MIME_TransferEncodings.SevenBit;
        private int                          m_ContentSize        = 0;
        private IMAP_Envelope                m_pEnvelope          = null;
        private int                          m_ContentLines       = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal IMAP_BODY_Entity()
        {
            m_pChildEntities = new List<IMAP_BODY_Entity>();
        }


        #region method Parse

        /// <summary>
        /// Parses entity and it's child entities.
        /// </summary>
        internal void Parse(string text)
        {
            throw new NotImplementedException();
            /*
            StringReader r = new StringReader(text);
            r.ReadToFirstChar();

            // If starts with ( then multipart/xxx, otherwise normal single part entity
            if(r.StartsWith("(")){
                // Entities are (entity1)(entity2)(...) <SP> ContentTypeSubType
                while(r.StartsWith("(")){
                    IMAP_BODY_Entity entity = new IMAP_BODY_Entity();
                    entity.Parse(r.ReadParenthesized());
                    entity.m_pParentEntity = this;
                    m_pChildEntities.Add(entity);

                    r.ReadToFirstChar();
                }
                
                // Read multipart values. (nestedMimeEntries) contentTypeSubMediaType
                string contentTypeSubMediaType = r.ReadWord();

                m_pContentType = new MIME_h_ContentType("multipart/" + contentTypeSubMediaType);
            }
            else{
                // Basic fields for non-multipart
				// contentTypeMainMediaType contentTypeSubMediaType (conentTypeParameters) contentID contentDescription contentEncoding contentSize [envelope] [contentLine]

                // Content-Type
                string contentTypeMainMediaType = r.ReadWord();
                string contentTypeSubMediaType  = r.ReadWord();
                if(contentTypeMainMediaType.ToUpper() != "NIL" && contentTypeSubMediaType.ToUpper() != "NIL"){
                    m_pContentType = new MIME_h_ContentType(contentTypeMainMediaType + "/" + contentTypeSubMediaType);
                }

                // Content-Type header field parameters
                // Parameters syntax: "name" <SP> "value" <SP> "name" <SP> "value" <SP> ... .
                r.ReadToFirstChar();
                string conentTypeParameters = "NIL";
                if(r.StartsWith("(")){
                    conentTypeParameters = r.ReadParenthesized();

                    StringReader contentTypeParamReader = new StringReader(conentTypeParameters);
                    while(contentTypeParamReader.Available > 0){
                        string parameterName  = contentTypeParamReader.ReadWord();
                        string parameterValue = MIME_Encoding_EncodedWord.DecodeS(contentTypeParamReader.ReadWord());

                        m_pContentType.Parameters[parameterName] = parameterValue;
                    }
                }
                else{
                    // Skip NIL
                    r.ReadWord();
                }

                // Content-ID:
                string contentID = r.ReadWord();
                if(contentID.ToUpper() != "NIL"){
                    m_ContentID = contentID;
                }

                // Content-Description:
                string contentDescription = r.ReadWord();
                if(contentDescription.ToUpper() != "NIL"){
                    m_ContentDescription = contentDescription;
                }

                // Content-Transfer-Encoding:
                string contentEncoding = r.ReadWord();
                if(contentEncoding.ToUpper() != "NIL"){                   
                    m_ContentEncoding = contentEncoding;
                }

                // Content Encoded data size in bytes
                string contentSize = r.ReadWord();
                if(contentSize.ToUpper() != "NIL"){
                    m_ContentSize = Convert.ToInt32(contentSize);
                }

                // Only for ContentType message/rfc822
                if(string.Equals(this.ContentType.TypeWithSubype,MIME_MediaTypes.Message.rfc822,StringComparison.InvariantCultureIgnoreCase)){
                    r.ReadToFirstChar();
 
                    // envelope
                    if(r.StartsWith("(")){
                        m_pEnvelope = new IMAP_Envelope();
                        m_pEnvelope.Parse(r.ReadParenthesized());
                    }
                    else{
                        // Skip NIL, ENVELOPE wasn't included
                        r.ReadWord();
                    }

                    // TODO:
                    // BODYSTRUCTURE

                    // contentLine
                }

                // Only for ContentType text/xxx
                if(contentTypeMainMediaType.ToLower() == "text"){
                    // contentLine
                    string contentLines = r.ReadWord();
                    if(contentLines.ToUpper() != "NIL"){
                        m_ContentLines = Convert.ToInt32(contentLines);
                    }
                }                
            }   */         
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets parent entity of this entity. If this entity is top level, then this property returns null.
        /// </summary>
        public IMAP_BODY_Entity ParentEntity
        {
            get{ return m_pParentEntity; }
        }

        /// <summary>
        /// Gets child entities. This property is available only if ContentType = multipart/... .
        /// </summary>
        public IMAP_BODY_Entity[] ChildEntities
        {
            get{ 
            //  if((this.ContentType & MediaType_enum.Multipart) == 0){
            //      throw new Exception("NOTE: ChildEntities property is available only for non-multipart contentype !");
            //  }

                return m_pChildEntities.ToArray(); 
            }
        }

        /// <summary>
        /// Gets header field "<b>Content-Type:</b>" value.
        /// </summary>
        public MIME_h_ContentType ContentType
        {
            get{ return m_pContentType; }
        }

        /// <summary>
        /// Gets header field "<b>Content-ID:</b>" value. Returns null if value isn't set.
        /// </summary>
        public string ContentID
        {
            get{ return m_ContentID; }
        }

        /// <summary>
        /// Gets header field "<b>Content-Description:</b>" value. Returns null if value isn't set.
        /// </summary>
        public string ContentDescription
        {
            get{ return m_ContentDescription; }
        }

        /// <summary>
        /// Gets header field "<b>Content-Transfer-Encoding:</b>" value.
        /// </summary>
        public string ContentTransferEncoding
        {
            get{ return m_ContentEncoding; }
        }

        /// <summary>
        /// Gets content encoded data size. NOTE: This property is available only for non-multipart contentype !
        /// </summary>
        public int ContentSize
        {
            get{
                if(string.Equals(this.ContentType.Type,"multipart",StringComparison.InvariantCultureIgnoreCase)){
                    throw new Exception("NOTE: ContentSize property is available only for non-multipart contentype !");
                }

                return m_ContentSize; 
            }
        }
        /*
        /// <summary>
        /// Gets content envelope. NOTE: This property is available only for message/xxx content type !
        /// Yhis value can be also null if no ENVELOPE provided by server.
        /// </summary>
        public IMAP_Envelope Envelope
        {
            get{ 
                if(!string.Equals(this.ContentType.Type,"message",StringComparison.InvariantCultureIgnoreCase)){
                    throw new Exception("NOTE: Envelope property is available only for message/rfc2822 contentype !");
                }

                return null; 
            }
        }*/

        /// <summary>
        /// Gets content encoded data lines. NOTE: This property is available only for text/xxx content type !
        /// </summary>
        public int ContentLines
        {
            get{ 
                if(!string.Equals(this.ContentType.Type,"text",StringComparison.InvariantCultureIgnoreCase)){
                    throw new Exception("NOTE: ContentLines property is available only for text/xxx content type !");
                }

                return m_ContentSize; 
            }
        }

        #endregion

    }
}
