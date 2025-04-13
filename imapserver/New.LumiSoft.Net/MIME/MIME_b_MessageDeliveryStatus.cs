using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;
using LumiSoft.Net.Mail;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class represents MIME <b>message/delivery-status</b> body. Defined in RFC 3464.
    /// </summary>
    /// <remarks>
    /// <code>
    /// delivery-status-content =  per-message-fields 1*( CRLF per-recipient-fields )
    /// 
    /// per-message-fields =
    ///            [ original-envelope-id-field CRLF ]
    ///            reporting-mta-field CRLF
    ///            [ dsn-gateway-field CRLF ]
    ///            [ received-from-mta-field CRLF ]
    ///            [ arrival-date-field CRLF ]
    ///            *( extension-field CRLF )
    ///            
    /// per-recipient-fields =
    ///          [ original-recipient-field CRLF ]
    ///          final-recipient-field CRLF
    ///          action-field CRLF
    ///          status-field CRLF
    ///          [ remote-mta-field CRLF ]
    ///          [ diagnostic-code-field CRLF ]
    ///          [ last-attempt-date-field CRLF ]
    ///          [ final-log-id-field CRLF ]
    ///          [ will-retry-until-field CRLF ]
    ///         *( extension-field CRLF )
    /// </code>
    /// </remarks>
    public class MIME_b_MessageDeliveryStatus : MIME_b
    {
        private MIME_h_Collection       m_pMessageFields   = null;
        private List<MIME_h_Collection> m_pRecipientBlocks = null;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        public MIME_b_MessageDeliveryStatus() : base(new MIME_h_ContentType("message/delivery-status"))
        {
            m_pMessageFields   = new MIME_h_Collection(new MIME_h_Provider());
            m_pRecipientBlocks = new List<MIME_h_Collection>();
        }


        #region static method Parse

        /// <summary>
        /// Parses body from the specified stream
        /// </summary>
        /// <param name="owner">Owner MIME entity.</param>
        /// <param name="defaultContentType">Default content-type for this body.</param>
        /// <param name="stream">Stream from where to read body.</param>
        /// <returns>Returns parsed body.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b>, <b>defaultContentType</b> or <b>stream</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when any parsing errors.</exception>
        protected static new MIME_b Parse(MIME_Entity owner,MIME_h_ContentType defaultContentType,SmartStream stream)
        {
            if(owner == null){
                throw new ArgumentNullException("owner");
            }
            if(defaultContentType == null){
                throw new ArgumentNullException("defaultContentType");
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            // We need to buffer all body data, otherwise we don't know if we have readed all data 
            // from stream.
            MemoryStream msBuffer = new MemoryStream();
            Net_Utils.StreamCopy(stream,msBuffer,32000);
            msBuffer.Position = 0;

            SmartStream parseStream = new SmartStream(msBuffer,true);

            MIME_b_MessageDeliveryStatus retVal = new MIME_b_MessageDeliveryStatus();
            //Pare per-message fields.
            retVal.m_pMessageFields.Parse(parseStream);

            // Parse per-recipient fields.
            while(parseStream.Position - parseStream.BytesInReadBuffer < parseStream.Length){
                MIME_h_Collection recipientFields = new MIME_h_Collection(new MIME_h_Provider());
                recipientFields.Parse(parseStream);
                retVal.m_pRecipientBlocks.Add(recipientFields);                
            }                     

            return retVal;
        }

        #endregion


        #region method ToStream

        /// <summary>
        /// Stores MIME entity body to the specified stream.
        /// </summary>
        /// <param name="stream">Stream where to store body data.</param>
        /// <param name="headerWordEncoder">Header 8-bit words ecnoder. Value null means that words are not encoded.</param>
        /// <param name="headerParmetersCharset">Charset to use to encode 8-bit header parameters. Value null means parameters not encoded.</param>
        /// <param name="headerReencode">If true always specified encoding is used for header. If false and header field value not modified, 
        /// original encoding is kept.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
        internal protected override void ToStream(Stream stream,MIME_Encoding_EncodedWord headerWordEncoder,Encoding headerParmetersCharset,bool headerReencode)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }
            
            m_pMessageFields.ToStream(stream,headerWordEncoder,headerParmetersCharset,headerReencode);
            stream.Write(new byte[]{(byte)'\r',(byte)'\n'},0,2);
            foreach(MIME_h_Collection recipientBlock in m_pRecipientBlocks){
                recipientBlock.ToStream(stream,headerWordEncoder,headerParmetersCharset,headerReencode);
                stream.Write(new byte[]{(byte)'\r',(byte)'\n'},0,2);
            }
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets if body has modified.
        /// </summary>
        public override bool IsModified
        {
            get{ 
                if(m_pMessageFields.IsModified){
                    return true;
                }
                foreach(MIME_h_Collection recipientBlock in m_pRecipientBlocks){
                    if(recipientBlock.IsModified){
                        return true;
                    }
                }

                return false; 
            }
        }

        /// <summary>
        /// Gets per-message fields collection.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is raised when this method is accessed and this body is not bounded to any entity.</exception>
        public MIME_h_Collection MessageFields
        {
            get{ return m_pMessageFields; }
        }
        
        /// <summary>
        /// Gets reciepent report blocks collection.
        /// </summary>
        /// <remarks>Each block contains per-recipient-fields.</remarks>
        public List<MIME_h_Collection> RecipientBlocks
        {
            get{ return m_pRecipientBlocks; }
        }

        #endregion
    }
}
