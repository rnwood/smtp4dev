using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;
using LumiSoft.Net.Mail;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class represents MIME message/rfc822 body. Defined in RFC 2046 5.2.1.
    /// </summary>
    public class MIME_b_MessageRfc822 : MIME_b
    {
        private Mail_Message m_pMessage = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MIME_b_MessageRfc822() : base(new MIME_h_ContentType("message/rfc822"))
        {
            m_pMessage = new Mail_Message();
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

            MIME_b_MessageRfc822 retVal = new MIME_b_MessageRfc822();
            retVal.m_pMessage = Mail_Message.ParseFromStream(stream);

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

            m_pMessage.ToStream(stream,headerWordEncoder,headerParmetersCharset,headerReencode);
        }

        #endregion


        #region Properties implementation
        
        /// <summary>
        /// Gets if body has modified.
        /// </summary>
        public override bool IsModified
        {
            get{ return m_pMessage.IsModified; }
        }
    
        /// <summary>
        /// Gets embbed mail message.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null reference passed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this method is accessed and this body is not bounded to any entity.</exception>
        public Mail_Message Message
        {
            get{ return m_pMessage; }

            set{
                if(value == null){
                    throw new ArgumentNullException("value");
                }
                if(this.Entity == null){
                    throw new InvalidOperationException("Body must be bounded to some entity first.");
                }

                // Owner entity has no content-type or has different content-type, just add/overwrite it.
                if(this.Entity.ContentType == null || !string.Equals(this.Entity.ContentType.TypeWithSubtype,this.MediaType,StringComparison.InvariantCultureIgnoreCase)){
                    this.Entity.ContentType = new MIME_h_ContentType(this.MediaType);
                }

                m_pMessage = value;
            }
        }

        #endregion
    }
}
