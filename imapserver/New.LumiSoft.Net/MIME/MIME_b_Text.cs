using System;
using System.IO;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class represents MIME text/xxx bodies. Defined in RFC 2045.
    /// </summary>
    /// <remarks>
    /// The "text" media type is intended for sending material which is principally textual in form.
    /// </remarks>
    public class MIME_b_Text : MIME_b_SinglepartBase
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="mediaType">MIME media type.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>mediaSubType</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public MIME_b_Text(string mediaType) : base(new MIME_h_ContentType(mediaType))
        {            
        }

        #region static method Parse

        /// <summary>
        /// Parses body from the specified stream
        /// </summary>
        /// <param name="owner">Owner MIME entity.</param>
        /// <param name="defaultContentType">Default content-type for this body.</param>
        /// <param name="stream">Stream from where to read body.</param>
        /// <returns>Returns parsed body.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b>, <b>mediaType</b> or <b>stream</b> is null reference.</exception>
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

            MIME_b_Text retVal = null;
            if(owner.ContentType != null){
                retVal = new MIME_b_Text(owner.ContentType.TypeWithSubtype);
            }
            else{
                retVal = new MIME_b_Text(defaultContentType.TypeWithSubtype);
            }

            Net_Utils.StreamCopy(stream,retVal.EncodedStream,32000);
            retVal.SetModified(false);

            return retVal;
        }

        #endregion


        #region method SetText

        /// <summary>
        /// Sets text.
        /// </summary>
        /// <param name="transferEncoding">Content transfer encoding.</param>
        /// <param name="charset">Charset to use to encode text. If not sure, utf-8 is recommended.</param>
        /// <param name="text">Text.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>transferEncoding</b>, <b>charset</b> or <b>text</b> is null reference.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this method is accessed and this body is not bounded to any entity.</exception>
        /// <exception cref="NotSupportedException">Is raised when body contains not supported Content-Transfer-Encoding.</exception>
        public void SetText(string transferEncoding,Encoding charset,string text)
        {
            if(transferEncoding == null){
                throw new ArgumentNullException("transferEncoding");
            }
            if(charset == null){
                throw new ArgumentNullException("charset");
            }
            if(text == null){
                throw new ArgumentNullException("text");
            }
            if(this.Entity == null){
                throw new InvalidOperationException("Body must be bounded to some entity first.");
            }

            SetData(new MemoryStream(charset.GetBytes(text)),transferEncoding);
            this.Entity.ContentType.Param_Charset = charset.WebName;            
        }

        #endregion


        #region method GetCharset

        /// <summary>
        /// Gets charset from Content-Type. If char set isn't specified, "ascii" is defined as default and it will be returned.
        /// </summary>
        /// <returns>Returns content charset.</returns>
        /// <exception cref="ArgumentException">Is raised when Content-Type has not supported charset parameter value.</exception>
        private Encoding GetCharset()
        {
            // RFC 2046 4.1.2. The default character set, US-ASCII.
            
            if(this.Entity.ContentType == null || string.IsNullOrEmpty(this.Entity.ContentType.Param_Charset)){
                return Encoding.ASCII;
            }
            else{
                // Handle custome/extended charsets, just remove "x-" from start.
                if(this.Entity.ContentType.Param_Charset.ToLower().StartsWith("x-")){
                    return Encoding.GetEncoding(this.Entity.ContentType.Param_Charset.Substring(2));
                }
                // Cp1252 is not IANA reggistered, some mail clients send it, it equal to windows-1252.
                else if(string.Equals(this.Entity.ContentType.Param_Charset,"cp1252",StringComparison.InvariantCultureIgnoreCase)){
                    return Encoding.GetEncoding("windows-1252");
                }
                else{
                    return Encoding.GetEncoding(this.Entity.ContentType.Param_Charset);
                }
            }
        }

        #endregion
                

        #region Properties implementation
                
        /// <summary>
        /// Gets body decoded text.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when not supported content-type charset or not supported content-transfer-encoding value.</exception>
        /// <exception cref="NotSupportedException">Is raised when body contains not supported Content-Transfer-Encoding.</exception>
        public string Text
        {
            get{ return GetCharset().GetString(this.Data); }
        }

        #endregion
    }
}
