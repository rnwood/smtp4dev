using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class is base class for singlepart media bodies like: text,video,audio,image.
    /// </summary>
    public abstract class MIME_b_SinglepartBase : MIME_b
    {
        private bool   m_IsModified         = false;
        private Stream m_pEncodedDataStream = null;
                
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="contentType">Content type.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>contentType</b> is null reference.</exception>
        public MIME_b_SinglepartBase(MIME_h_ContentType contentType) : base(contentType)
        {
            if(contentType == null){
                throw new ArgumentNullException("contentType");
            }

            m_pEncodedDataStream = new MemoryStreamEx(32000);
        }

        /// <summary>
        /// Destructor - Just incase user won't call dispose.
        /// </summary>
        ~MIME_b_SinglepartBase()
        {
            if(m_pEncodedDataStream != null){
                m_pEncodedDataStream.Close();
            }
        }
        
  
        #region override SetParent

        /// <summary>
        /// Sets body parent.
        /// </summary>
        /// <param name="entity">Owner entity.</param>
        /// <param name="setContentType">If true sets entity.ContentType header value.</param>
        internal override void SetParent(MIME_Entity entity,bool setContentType)
        {
            base.SetParent(entity,setContentType);

            // Owner entity has no content-type or has different content-type, just add/overwrite it.
            if(setContentType && (this.Entity.ContentType == null || !string.Equals(this.Entity.ContentType.TypeWithSubtype,this.MediaType,StringComparison.InvariantCultureIgnoreCase))){
                this.Entity.ContentType = new MIME_h_ContentType(MediaType);
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
        internal protected override void ToStream(Stream stream,MIME_Encoding_EncodedWord headerWordEncoder,Encoding headerParmetersCharset,bool headerReencode)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            Net_Utils.StreamCopy(GetEncodedDataStream(),stream,32000);
        }

        #endregion

        #region method SetModified

        /// <summary>
        /// Sets IsModified property value.
        /// </summary>
        /// <param name="isModified">Modified flag.</param>
        protected void SetModified(bool isModified)
        {
            m_IsModified = isModified;
        }

        #endregion


        #region method GetEncodedDataStream

        /// <summary>
        /// Gets body encoded data stream.
        /// </summary>
        /// <returns>Returns body encoded data stream.</returns>
        /// <exception cref="InvalidOperationException">Is raised when this method is accessed and this body is not bounded to any entity.</exception>
        public Stream GetEncodedDataStream()
        {
            if(this.Entity == null){
                throw new InvalidOperationException("Body must be bounded to some entity first.");
            }

            m_pEncodedDataStream.Position = 0;

            return m_pEncodedDataStream;
        }

        #endregion

        #region method SetEncodedData

        /// <summary>
        /// Sets body encoded data from specified stream.
        /// </summary>
        /// <param name="contentTransferEncoding">Content-Transfer-Encoding in what encoding <b>stream</b> data is.</param>
        /// <param name="stream">Stream data to add.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>contentTransferEncoding</b> or <b>stream</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the argumennts has invalid value.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this method is accessed and this body is not bounded to any entity.</exception>
        public void SetEncodedData(string contentTransferEncoding,Stream stream)
        {
            if(contentTransferEncoding == null){
                throw new ArgumentNullException("contentTransferEncoding");
            }
            if(contentTransferEncoding == string.Empty){
                throw new ArgumentException("Argument 'contentTransferEncoding' value must be specified.");
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }
            if(this.Entity == null){
                throw new InvalidOperationException("Body must be bounded to some entity first.");
            }

            // Owner entity has no content-type or has different content-type, just add/overwrite it.
            if(this.Entity.ContentType == null || !string.Equals(this.Entity.ContentType.TypeWithSubtype,this.MediaType,StringComparison.InvariantCultureIgnoreCase)){
                this.Entity.ContentType = new MIME_h_ContentType(this.MediaType);
            }
            this.Entity.ContentTransferEncoding = contentTransferEncoding;

            m_pEncodedDataStream.SetLength(0);
            Net_Utils.StreamCopy(stream,m_pEncodedDataStream,32000);
       
            m_IsModified = true;
        }

        #endregion

        #region method GetDataStream

        /// <summary>
        /// Gets body decoded data stream.
        /// </summary>
        /// <returns>Returns body decoded data stream.</returns>
        /// <exception cref="InvalidOperationException">Is raised when this method is accessed and this body is not bounded to any entity.</exception>
        /// <exception cref="NotSupportedException">Is raised when body contains not supported Content-Transfer-Encoding.</exception>
        /// <remarks>The returned stream should be closed/disposed as soon as it's not needed any more.</remarks>
        public Stream GetDataStream()
        {             
            if(this.Entity == null){
                throw new InvalidOperationException("Body must be bounded to some entity first.");
            }

            /* RFC 2045 6.1.
                This is the default value -- that is, "Content-Transfer-Encoding: 7BIT" is assumed if the
                Content-Transfer-Encoding header field is not present.
            */
            string transferEncoding = MIME_TransferEncodings.SevenBit;
            if(this.Entity.ContentTransferEncoding != null){
                transferEncoding = this.Entity.ContentTransferEncoding.ToLowerInvariant();
            }

            m_pEncodedDataStream.Position = 0;            
            if(transferEncoding == MIME_TransferEncodings.QuotedPrintable){                
                return new QuotedPrintableStream(new SmartStream(m_pEncodedDataStream,false),FileAccess.Read);
            }
            else if(transferEncoding == MIME_TransferEncodings.Base64){
                return new Base64Stream(m_pEncodedDataStream,false,true,FileAccess.Read);
            }            
            else if(transferEncoding == MIME_TransferEncodings.Binary){
                return new ReadWriteControlledStream(m_pEncodedDataStream,FileAccess.Read);
            }
            else if(transferEncoding == MIME_TransferEncodings.EightBit){
                return new ReadWriteControlledStream(m_pEncodedDataStream,FileAccess.Read);
            }
            else if(transferEncoding == MIME_TransferEncodings.SevenBit){
                return new ReadWriteControlledStream(m_pEncodedDataStream,FileAccess.Read);
            }
            else{
                throw new NotSupportedException("Not supported Content-Transfer-Encoding '" + this.Entity.ContentTransferEncoding + "'.");
            }
        }

        #endregion

        #region method SetData

        /// <summary>
        /// Sets body data from the specified stream.
        /// </summary>
        /// <param name="stream">Source stream.</param>
        /// <param name="transferEncoding">Specifies content-transfer-encoding to use to encode data.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> or <b>transferEncoding</b> is null reference.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this method is accessed and this body is not bounded to any entity.</exception>
        public void SetData(Stream stream,string transferEncoding)
        {            
            if(stream == null){
                throw new ArgumentNullException("stream");
            }
            if(transferEncoding == null){
                throw new ArgumentNullException("transferEncoding");
            }

            if(string.Equals(transferEncoding,MIME_TransferEncodings.QuotedPrintable,StringComparison.InvariantCultureIgnoreCase)){
                using(MemoryStreamEx fs = new MemoryStreamEx(32000)){
                    QuotedPrintableStream encoder = new QuotedPrintableStream(new SmartStream(fs,false),FileAccess.ReadWrite);
                    Net_Utils.StreamCopy(stream,encoder,32000);
                    encoder.Flush();
                    fs.Position = 0;
                    SetEncodedData(transferEncoding,fs);
                }
            }
            else if(string.Equals(transferEncoding,MIME_TransferEncodings.Base64,StringComparison.InvariantCultureIgnoreCase)){
                using(MemoryStreamEx fs = new MemoryStreamEx(32000)){
                    Base64Stream encoder = new Base64Stream(fs,false,true,FileAccess.ReadWrite);                                     
                    Net_Utils.StreamCopy(stream,encoder,32000);
                    encoder.Finish();
                    fs.Position = 0;
                    SetEncodedData(transferEncoding,fs);
                }
            }            
            else if(string.Equals(transferEncoding,MIME_TransferEncodings.Binary,StringComparison.InvariantCultureIgnoreCase)){
                SetEncodedData(transferEncoding,stream);
            }
            else if(string.Equals(transferEncoding,MIME_TransferEncodings.EightBit,StringComparison.InvariantCultureIgnoreCase)){
                SetEncodedData(transferEncoding,stream);
            }
            else if(string.Equals(transferEncoding,MIME_TransferEncodings.SevenBit,StringComparison.InvariantCultureIgnoreCase)){
                SetEncodedData(transferEncoding,stream);
            }
            else{
                throw new NotSupportedException("Not supported Content-Transfer-Encoding '" + transferEncoding + "'.");
            }
        }

        #endregion

        #region method SetDataFromFile

        /// <summary>
        /// Sets body data from the specified file.
        /// </summary>
        /// <param name="file">File name with optional path.</param>
        /// <param name="transferEncoding">Specifies content-transfer-encoding to use to encode data.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>file</b> is null reference.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this method is accessed and this body is not bounded to any entity.</exception>
        public void SetDataFromFile(string file,string transferEncoding)
        {
            if(file == null){
                throw new ArgumentNullException("file");
            }
            
            using(FileStream fs = File.OpenRead(file)){
                SetData(fs,transferEncoding);
            }            
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets if body has modified.
        /// </summary>
        public override bool IsModified
        {
            get{ return m_IsModified; }
        }
                
        /// <summary>
        /// Gets encoded body data size in bytes.
        /// </summary>
        public int EncodedDataSize
        {
            get{ return (int)m_pEncodedDataStream.Length; }
        }

        /// <summary>
        /// Gets body encoded data. 
        /// </summary>
        /// <remarks>NOTE: Use this property with care, because body data may be very big and you may run out of memory.
        /// For bigger data use <see cref="GetEncodedDataStream"/> method instead.</remarks>
        public byte[] EncodedData
        {
            get{ 
                MemoryStream ms = new MemoryStream();
                Net_Utils.StreamCopy(this.GetEncodedDataStream(),ms,32000);

                return ms.ToArray();
            }
        }

        /// <summary>
        /// Gets body decoded data.
        /// </summary>
        /// <remarks>NOTE: Use this property with care, because body data may be very big and you may run out of memory.
        /// For bigger data use <see cref="GetDataStream"/> method instead.</remarks>
        /// <exception cref="NotSupportedException">Is raised when body contains not supported Content-Transfer-Encoding.</exception>
        public byte[] Data
        {
            get{
                MemoryStream ms = new MemoryStream();
                Net_Utils.StreamCopy(this.GetDataStream(),ms,32000);

                return ms.ToArray(); 
            }
        }


        /// <summary>
        /// Gets encoded data stream.
        /// </summary>
        protected Stream EncodedStream
        {
            get{ return m_pEncodedDataStream; }
        }

        #endregion
    }
}
