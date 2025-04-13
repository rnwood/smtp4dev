using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class is base class for MIME entity bodies.
    /// </summary>
    public abstract class MIME_b
    {
        private MIME_Entity        m_pEntity      = null;
        private MIME_h_ContentType m_pContentType = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="contentType">Content type.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>contentType</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public MIME_b(MIME_h_ContentType contentType)
        {
            if(contentType == null){
                throw new ArgumentNullException("contentType");
            }

            m_pContentType = contentType;
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
        protected static MIME_b Parse(MIME_Entity owner,MIME_h_ContentType defaultContentType,SmartStream stream)
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

            throw new NotImplementedException("Body provider class does not implement required Parse method.");
        }

        #endregion


        #region method SetParent

        /// <summary>
        /// Sets body parent.
        /// </summary>
        /// <param name="entity">Owner entity.</param>
        /// <param name="setContentType">If true sets entity.ContentType header value.</param>
        internal virtual void SetParent(MIME_Entity entity,bool setContentType)
        {
            m_pEntity = entity;

            // Owner entity has no content-type or has different content-type, just add/overwrite it.
            if(setContentType &&(entity.ContentType == null || !string.Equals(entity.ContentType.TypeWithSubtype,this.MediaType,StringComparison.InvariantCultureIgnoreCase))){
                entity.ContentType = m_pContentType;
            }
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
        internal protected abstract void ToStream(Stream stream,MIME_Encoding_EncodedWord headerWordEncoder,Encoding headerParmetersCharset,bool headerReencode);

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets if body has modified.
        /// </summary>
        public abstract bool IsModified
        {
            get;
        }

        /// <summary>
        /// Gets body owner entity. Returns null if body not bounded to any entity yet.
        /// </summary>
        public MIME_Entity Entity
        {
            get{ return m_pEntity; }
        }

        /// <summary>
        /// Gets body media type. For example: 'text/plain'.
        /// </summary>
        public string MediaType
        {
            get{ return m_pContentType.TypeWithSubtype; }
        }

        #endregion
    }
}
