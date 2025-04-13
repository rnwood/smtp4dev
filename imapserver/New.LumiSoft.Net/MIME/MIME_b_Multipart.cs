using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class represents MIME application/xxx bodies. Defined in RFC 2046 5.1.
    /// </summary>
    /// <remarks>
    /// The "multipart" represents single MIME body containing multiple child MIME entities.
    /// The "multipart" body must contain at least 1 MIME entity.
    /// </remarks>
    public class MIME_b_Multipart : MIME_b
    {
        #region class _MultipartReader

        /// <summary>
        /// Implements  multipart "body parts" reader.
        /// </summary>
        public class _MultipartReader : Stream
        {
            #region enum State

            /// <summary>
            /// This enum specified multipart reader sate.
            /// </summary>
            internal enum State
            {
                /// <summary>
                /// First boundary must be seeked.
                /// </summary>
                SeekFirst = 0,

                /// <summary>
                /// Read next boundary. (Method Next must be called to continue next boundary reading)
                /// </summary>
                ReadNext = 1,

                /// <summary>
                /// Active boundary reading pending.
                /// </summary>
                InBoundary = 2,

                /// <summary>
                /// All boundraies readed.
                /// </summary>
                Done = 3,
            }

            #endregion

            #region class _DataLine

            /// <summary>
            /// This class holds readed data line info.
            /// </summary>
            private class _DataLine
            {
                private byte[] m_pLineBuffer   = null;
                private int    m_BytesInBuffer = 0;

                /// <summary>
                /// Default constructor.
                /// </summary>
                public _DataLine()
                {
                    m_pLineBuffer = new byte[32000];
                }


                #region method AssignFrom

                /// <summary>
                /// Assigns data line info from rea line operation.
                /// </summary>
                /// <param name="op">Read line operation.</param>
                /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
                public void AssignFrom(SmartStream.ReadLineAsyncOP op)
                {
                    if(op == null){
                        throw new ArgumentNullException();
                    }

                    m_BytesInBuffer = op.BytesInBuffer;
                    Array.Copy(op.Buffer,m_pLineBuffer,op.BytesInBuffer);
                }

                #endregion


                #region Properties implementation

                /// <summary>
                /// Gets line data buffer.
                /// </summary>
                public byte[] LineBuffer
                {
                    get{ return m_pLineBuffer; }
                }

                /// <summary>
                /// Gets number of bytes used in <b>LineBuffer</b>.
                /// </summary>
                public int BytesInBuffer
                {
                    get{ return m_BytesInBuffer; }
                }

                #endregion
            }

            #endregion

            private State                       m_State         = State.SeekFirst;
            private SmartStream                 m_pStream       = null;
            private string                      m_Boundary      = "";
            private _DataLine                   m_pPreviousLine = null;
            private SmartStream.ReadLineAsyncOP m_pReadLineOP   = null;
            private StringBuilder               m_pTextPreamble = null;
            private StringBuilder               m_pTextEpilogue = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="stream">Stream from where to read body part.</param>
            /// <param name="boundary">Boundry ID what separates body parts.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> or <b>boundary</b> is null reference.</exception>
            public _MultipartReader(SmartStream stream,string boundary)
            {
                if(stream == null){
                    throw new ArgumentNullException("stream");
                }
                if(boundary == null){
                    throw new ArgumentNullException("boundary");
                }

                m_pStream  = stream;
                m_Boundary = boundary;

                m_pReadLineOP   = new SmartStream.ReadLineAsyncOP(new byte[32000],SizeExceededAction.ThrowException);
                m_pTextPreamble = new StringBuilder();
                m_pTextEpilogue = new StringBuilder();
            }


            #region method Next

            /// <summary>
            /// Moves to next "body part". Returns true if moved to next "body part" or false if there are no more parts.
            /// </summary>
            /// <returns>Returns true if moved to next "body part" or false if there are no more body parts.</returns>
            public bool Next()
            {
                /* RFC 2046 5.1.1.
                    NOTE:  The CRLF preceding the boundary delimiter line is conceptually
                    attached to the boundary so that it is possible to have a part that
                    does not end with a CRLF (line  break).  
                */
                
                if(m_State == State.InBoundary){
                    throw new InvalidOperationException("You must read all boundary data, before calling this method.");
                }
                else if(m_State == State.Done){
                    return false;
                }
                else if(m_State == State.SeekFirst){
                    m_pPreviousLine = null;

                    while(true){
                        m_pStream.ReadLine(m_pReadLineOP,false);                                                
                        if(m_pReadLineOP.Error != null){
                            throw m_pReadLineOP.Error;
                        }
                        // We reached end of stream. Bad boundary: boundary end tag missing.
                        else if(m_pReadLineOP.BytesInBuffer == 0){
                            m_State = State.Done;

                            return false;
                        }
                        else{
                            // Check if we have boundary start/end.
                            if(m_pReadLineOP.Buffer[0] == '-'){
                                string boundary = m_pReadLineOP.LineUtf8;
                                // We have readed all MIME entity body parts.
                                if("--" + m_Boundary + "--" == boundary){
                                    m_State = State.Done;

                                    // Last CRLF is no part of preamble, but is part of boundary-tag.
                                    if(m_pTextPreamble.Length >= 2){
                                        m_pTextPreamble.Remove(m_pTextPreamble.Length - 2,2);
                                    }

                                    // Read "epilogoue",if has any.
                                    while(true){
                                        m_pStream.ReadLine(m_pReadLineOP,false);

                                        if(m_pReadLineOP.Error != null){
                                            throw m_pReadLineOP.Error;
                                        }
                                        // We reached end of stream. Epilogue reading completed.
                                        else if(m_pReadLineOP.BytesInBuffer == 0){
                                            break;
                                        }
                                        else{
                                            m_pTextEpilogue.Append(m_pReadLineOP.LineUtf8 + "\r\n");
                                        }
                                    }

                                    return false;
                                }
                                // We have next boundary.
                                else if("--" + m_Boundary == boundary){
                                    m_State = State.InBoundary;

                                    // Last CRLF is no part of preamble, but is part of boundary-tag.
                                    if(m_pTextPreamble.Length >= 2){
                                        m_pTextPreamble.Remove(m_pTextPreamble.Length - 2,2);
                                    }

                                    return true;
                                }
                                // Not boundary or not boundary we want.
                                //else{
                            }

                            m_pTextPreamble.Append(m_pReadLineOP.LineUtf8 + "\r\n");                           
                        }                        
                    }                   
                }
                else if(m_State == State.ReadNext){
                    m_pPreviousLine = null;
                    m_State = State.InBoundary;

                    return true;
                }
           
                return false;
            }

            #endregion


            #region override method Flush

            /// <summary>
            /// Clears all buffers for this stream and causes any buffered data to be written to the underlying device.
            /// </summary>
            public override void Flush()
            {            
            }

            #endregion

            #region override method Seek

            /// <summary>
            /// Sets the position within the current stream. This method is not supported and always throws a NotSupportedException.
            /// </summary>
            /// <param name="offset">A byte offset relative to the <b>origin</b> parameter.</param>
            /// <param name="origin">A value of type SeekOrigin indicating the reference point used to obtain the new position.</param>
            /// <returns>The new position within the current stream.</returns>
            /// <exception cref="NotSupportedException">Is raised when this method is accessed.</exception>
            public override long Seek(long offset,SeekOrigin origin)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region override method SetLength

            /// <summary>
            /// Sets the length of the current stream. This method is not supported and always throws a NotSupportedException.
            /// </summary>
            /// <param name="value">The desired length of the current stream in bytes.</param>
            /// <exception cref="Seek">Is raised when this method is accessed.</exception>
            public override void SetLength(long value)
            {
                throw new NotSupportedException();
            }

            #endregion

            #region override method Read

            /// <summary>
            /// Reads a sequence of bytes from the current stream and advances the position within the stream by the number of bytes read.
            /// </summary>
            /// <param name="buffer">An array of bytes. When this method returns, the buffer contains the specified byte array with the values between offset and (offset + count - 1) replaced by the bytes read from the current source.</param>
            /// <param name="offset">The zero-based byte offset in buffer at which to begin storing the data read from the current stream.</param>
            /// <param name="count">The maximum number of bytes to be read from the current stream.</param>
            /// <returns>The total number of bytes read into the buffer. This can be less than the number of bytes requested if that many bytes are not currently available, or zero (0) if the end of the stream has been reached.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
            public override int Read(byte[] buffer,int offset,int count)
            {
                if(buffer == null){
                    throw new ArgumentNullException("buffer");
                }
                if(m_State == State.SeekFirst){
                    throw new InvalidOperationException("Read method is not valid in '" + m_State + "' state.");
                }
                if(m_State == State.ReadNext || m_State == State.Done){
                    return 0;
                }

                /* RFC 2046 5.1.1.
                    NOTE:  The CRLF preceding the boundary delimiter line is conceptually
                    attached to the boundary so that it is possible to have a part that
                    does not end with a CRLF (line  break).  
                 
                   NOTE: We just need read 1 line ahead, oterwise we can't remove boundary preceeding CRLF.
                */
                                
                // Read line ahead, if none available. This is done for the boundary first line only.
                if(m_pPreviousLine == null){
                    m_pPreviousLine = new _DataLine();

                    m_pStream.ReadLine(m_pReadLineOP,false);
                    if(m_pReadLineOP.Error != null){
                        throw m_pReadLineOP.Error;
                    }
                    // We reached end of stream. Bad boundary: boundary end tag missing.
                    else if(m_pReadLineOP.BytesInBuffer == 0){
                        m_State = State.Done;

                        return 0;
                    }
                    // We have readed all MIME entity body parts.(boundary end tag reached)
                    else if(m_pReadLineOP.Buffer[0] == '-' && string.Equals("--" + m_Boundary + "--",m_pReadLineOP.LineUtf8)){
                        m_State = State.Done;

                        // Read "epilogoue",if has any.
                        while(true){
                            m_pStream.ReadLine(m_pReadLineOP,false);

                            if(m_pReadLineOP.Error != null){
                                throw m_pReadLineOP.Error;
                            }
                            // We reached end of stream. Epilogue reading completed.
                            else if(m_pReadLineOP.BytesInBuffer == 0){
                                break;
                            }
                            else{
                                m_pTextEpilogue.Append(m_pReadLineOP.LineUtf8 + "\r\n");
                            }
                        }

                        return 0;
                    }
                    // We have readed all active boundary data, next boundary start tag.
                    else if(m_pReadLineOP.Buffer[0] == '-' && string.Equals("--" + m_Boundary,m_pReadLineOP.LineUtf8)){
                        m_State = State.ReadNext;

                        return 0;
                    }
                    // Store first read-ahed line.
                    else{
                        m_pPreviousLine.AssignFrom(m_pReadLineOP);
                    }
                }

                m_pStream.ReadLine(m_pReadLineOP,false);
                if(m_pReadLineOP.Error != null){
                    throw m_pReadLineOP.Error;
                }
                // We reached end of stream. Bad boundary: boundary end tag missing.
                else if(m_pReadLineOP.BytesInBuffer == 0){
                    m_State = State.Done;

                    if(count < m_pPreviousLine.BytesInBuffer){
                        throw new ArgumentException("Argument 'buffer' is to small. This should never happen.");
                    }
                    if(m_pPreviousLine.BytesInBuffer > 0){
                        Array.Copy(m_pPreviousLine.LineBuffer,0,buffer,offset,m_pPreviousLine.BytesInBuffer);
                    }

                    return m_pPreviousLine.BytesInBuffer;
                }
                // We have readed all MIME entity body parts.(boundary end tag reached)
                else if(m_pReadLineOP.Buffer[0] == '-' && string.Equals("--" + m_Boundary + "--",m_pReadLineOP.LineUtf8)){
                    m_State = State.Done;

                    // Read "epilogoue",if has any.
                    while(true){
                        m_pStream.ReadLine(m_pReadLineOP,false);

                        if(m_pReadLineOP.Error != null){
                            throw m_pReadLineOP.Error;
                        }
                        // We reached end of stream. Epilogue reading completed.
                        else if(m_pReadLineOP.BytesInBuffer == 0){
                            break;
                        }
                        else{
                            m_pTextEpilogue.Append(m_pReadLineOP.LineUtf8 + "\r\n");
                        }
                    }
                                        
                    if(count < m_pPreviousLine.BytesInBuffer){
                        throw new ArgumentException("Argument 'buffer' is to small. This should never happen.");
                    }
                    // Return previous line data - CRLF, because CRLF if part of boundary tag.
                    if(m_pPreviousLine.BytesInBuffer > 2){
                        Array.Copy(m_pPreviousLine.LineBuffer,0,buffer,offset,m_pPreviousLine.BytesInBuffer - 2);

                        return m_pPreviousLine.BytesInBuffer - 2;
                    }
                    else{
                        return 0;
                    }
                }
                // We have readed all active boundary data, next boundary start tag.
                else if(m_pReadLineOP.Buffer[0] == '-' && string.Equals("--" + m_Boundary,m_pReadLineOP.LineUtf8)){
                    m_State = State.ReadNext;

                    // Return previous line data - CRLF, because CRLF if part of boundary tag.
                    if(count < m_pPreviousLine.BytesInBuffer){
                        throw new ArgumentException("Argument 'buffer' is to small. This should never happen.");
                    }
                    if(m_pPreviousLine.BytesInBuffer > 2){
                        Array.Copy(m_pPreviousLine.LineBuffer,0,buffer,offset,m_pPreviousLine.BytesInBuffer - 2);

                        return m_pPreviousLine.BytesInBuffer - 2;
                    }
                    else{
                        return 0;
                    }
                }
                // We have boundary data-line.
                else{
                    // Here we actually process previous line and store current.

                    if(count < m_pPreviousLine.BytesInBuffer){
                        throw new ArgumentException("Argument 'buffer' is to small. This should never happen.");
                    }                    
                    Array.Copy(m_pPreviousLine.LineBuffer,0,buffer,offset,m_pPreviousLine.BytesInBuffer);

                    int countCopied = m_pPreviousLine.BytesInBuffer;

                    // Store current line as previous.
                    m_pPreviousLine.AssignFrom(m_pReadLineOP);
                                        
                    return countCopied;
                }
            }

            #endregion

            #region override method Write

            /// <summary>
            /// Writes sequence of bytes to the current stream and advances the current position within this stream by the number of bytes written.
            /// </summary>
            /// <param name="buffer">An array of bytes. This method copies count bytes from buffer to the current stream.</param>
            /// <param name="offset">The zero-based byte offset in buffer at which to begin copying bytes to the current stream.</param>
            /// <param name="count">The number of bytes to be written to the current stream.</param>
            /// <exception cref="NotSupportedException">Is raised when this method is accessed.</exception>
            public override void Write(byte[] buffer,int offset,int count)        
            {
                throw new NotSupportedException();
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets a value indicating whether the current stream supports reading.
            /// </summary>
            public override bool CanRead
            { 
                get{
                    return true;
                } 
            }

            /// <summary>
            /// Gets a value indicating whether the current stream supports seeking.
            /// </summary>
            public override bool CanSeek
            { 
                get{
                    return false;
                } 
            }

            /// <summary>
            /// Gets a value indicating whether the current stream supports writing.
            /// </summary>
            public override bool CanWrite
            { 
                get{
                    return false;
                } 
            }

            /// <summary>
            /// Gets the length in bytes of the stream.  This method is not supported and always throws a NotSupportedException.
            /// </summary>
            /// <exception cref="NotSupportedException">Is raised when this property is accessed.</exception>
            public override long Length
            { 
                get{
                    throw new NotSupportedException();
                } 
            }

            /// <summary>
            /// Gets or sets the position within the current stream. This method is not supported and always throws a NotSupportedException.
            /// </summary>
            /// <exception cref="NotSupportedException">Is raised when this property is accessed.</exception>
            public override long Position
            { 
                get{
                    throw new NotSupportedException();
                } 

                set{
                    throw new NotSupportedException();
                }
            }
                        
            /// <summary>
            /// Gets "preamble" text. Defined in RFC 2046 5.1.1.
            /// </summary>
            /// <remarks>Preamble text is text between MIME entiy headers and first boundary.</remarks>
            public string TextPreamble
            {
                get{ return m_pTextPreamble.ToString(); }
            }

            /// <summary>
            /// Gets "epilogue" text. Defined in RFC 2046 5.1.1.
            /// </summary>
            /// <remarks>Epilogue text is text after last boundary end.</remarks>
            public string TextEpilogue
            {
                get{ return m_pTextEpilogue.ToString(); }
            }

            /// <summary>
            /// Gets reader state.
            /// </summary>
            internal State ReaderState
            {
                get{ return m_State; }
            }

            #endregion
        }

        #endregion

        private MIME_h_ContentType    m_pContentType = null;
        private MIME_EntityCollection m_pBodyParts   = null;
        private string                m_TextPreamble = "";
        private string                m_TextEpilogue = "";
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="contentType">Content type.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>contentType</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public MIME_b_Multipart(MIME_h_ContentType contentType) : base(contentType)
        {
            if(contentType == null){
                throw new ArgumentNullException("contentType");
            }
            if(string.IsNullOrEmpty(contentType.Param_Boundary)){
                throw new ArgumentException("Argument 'contentType' doesn't contain required boundary parameter.");
            }

            m_pContentType = contentType;

            m_pBodyParts = new MIME_EntityCollection();
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
            if(owner.ContentType == null || owner.ContentType.Param_Boundary == null){
                throw new ParseException("Multipart entity has not required 'boundary' paramter.");
            }
            
            MIME_b_Multipart retVal = new MIME_b_Multipart(owner.ContentType);
            ParseInternal(owner,owner.ContentType.TypeWithSubtype,stream,retVal);

            return retVal;
        }

        #endregion

        #region static method ParseInternal

        /// <summary>
        /// Internal body parsing.
        /// </summary>
        /// <param name="owner">Owner MIME entity.</param>
        /// <param name="mediaType">MIME media type. For example: text/plain.</param>
        /// <param name="stream">Stream from where to read body.</param>
        /// <param name="body">Multipart body instance.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b>, <b>mediaType</b>, <b>stream</b> or <b>body</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when any parsing errors.</exception>
        protected static void ParseInternal(MIME_Entity owner,string mediaType,SmartStream stream,MIME_b_Multipart body)
        {
            if(owner == null){
                throw new ArgumentNullException("owner");
            }
            if(mediaType == null){
                throw new ArgumentNullException("mediaType");
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }
            if(owner.ContentType == null || owner.ContentType.Param_Boundary == null){
                throw new ParseException("Multipart entity has not required 'boundary' parameter.");
            }
            if(body == null){
                throw new ArgumentNullException("body");
            }

            _MultipartReader multipartReader = new _MultipartReader(stream,owner.ContentType.Param_Boundary);       
            while(multipartReader.Next()){
                MIME_Entity entity = new MIME_Entity();
                entity.Parse(new SmartStream(multipartReader,false),Encoding.UTF8,body.DefaultBodyPartContentType);
                body.m_pBodyParts.Add(entity);
                entity.SetParent(owner);
            }

            body.m_TextPreamble = multipartReader.TextPreamble;
            body.m_TextEpilogue = multipartReader.TextEpilogue;

            body.BodyParts.SetModified(false);
        }

        #endregion


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
                this.Entity.ContentType = m_pContentType;
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

            /* RFC 2046 5.1.1.
                NOTE:  The CRLF preceding the boundary delimiter line is conceptually
                attached to the boundary so that it is possible to have a part that
                does not end with a CRLF (line  break).  
            */

            // Set "preamble" text if any.
            if(!string.IsNullOrEmpty(m_TextPreamble)){
                byte[] preableBytes = Encoding.UTF8.GetBytes(m_TextPreamble);
                stream.Write(preableBytes,0,preableBytes.Length);
            }

            for(int i=0;i<m_pBodyParts.Count;i++){
                MIME_Entity bodyPart = m_pBodyParts[i];
                // Start new body part.
                byte[] bStart = Encoding.UTF8.GetBytes("\r\n--" + this.Entity.ContentType.Param_Boundary + "\r\n");
                stream.Write(bStart,0,bStart.Length);
                
                bodyPart.ToStream(stream,headerWordEncoder,headerParmetersCharset,headerReencode);

                // Last body part, close boundary.
                if(i == (m_pBodyParts.Count - 1)){
                    byte[] bEnd = Encoding.UTF8.GetBytes("\r\n--" + this.Entity.ContentType.Param_Boundary + "--\r\n");
                    stream.Write(bEnd,0,bEnd.Length);
                }
            }

            // Set "epilogoue" text if any.
            if(!string.IsNullOrEmpty(m_TextEpilogue)){
                byte[] epilogoueBytes = Encoding.UTF8.GetBytes(m_TextEpilogue);
                stream.Write(epilogoueBytes,0,epilogoueBytes.Length);
            }
        }

        #endregion
        

        #region Properties implementation
        
        /// <summary>
        /// Gets if body has modified.
        /// </summary>
        public override bool IsModified
        {
            get{ return m_pBodyParts.IsModified; }
        }
   
        /// <summary>
        /// Gets default body part Content-Type. For more info see RFC 2046 5.1.
        /// </summary>
        public virtual MIME_h_ContentType DefaultBodyPartContentType
        {
            /* RFC 2026 5.1.
                The absence of a Content-Type header usually indicates that the corresponding body has
                a content-type of "text/plain; charset=US-ASCII".
            */

            get{ 
                MIME_h_ContentType retVal = new MIME_h_ContentType("text/plain");
                retVal.Param_Charset = "US-ASCII";

                return retVal; 
            }
        }

        /// <summary>
        /// Gets multipart body body-parts collection.
        /// </summary>
        /// <remarks>Multipart entity child entities are called "body parts" in RFC 2045.</remarks>
        public MIME_EntityCollection BodyParts
        {
            get{ return m_pBodyParts; }
        }

        /// <summary>
        /// Gets or sets "preamble" text. Defined in RFC 2046 5.1.1.
        /// </summary>
        /// <remarks>Preamble text is text between MIME entiy headers and first boundary.</remarks>
        public string TextPreamble
        {
            get{ return m_TextPreamble; }

            set{ m_TextPreamble = value; }
        }

        /// <summary>
        /// Gets or sets "epilogue" text. Defined in RFC 2046 5.1.1.
        /// </summary>
        /// <remarks>Epilogue text is text after last boundary end.</remarks>
        public string TextEpilogue
        {
            get{ return m_TextEpilogue; }

            set{ m_TextEpilogue = value; }
        }

        #endregion
    }
}
