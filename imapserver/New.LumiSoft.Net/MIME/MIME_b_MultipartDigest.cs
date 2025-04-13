using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class represents MIME multipart/digest body. Defined in RFC 2046 5.1.5.
    /// </summary>
    /// <remarks>
    /// The "multipart/digest" Content-Type is intended to be used to send collections of messages.
    /// </remarks>
    public class MIME_b_MultipartDigest : MIME_b_Multipart
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="contentType">Content type.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>contentType</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public MIME_b_MultipartDigest(MIME_h_ContentType contentType) : base(contentType)
        {
            if(!string.Equals(contentType.TypeWithSubtype,"multipart/digest",StringComparison.CurrentCultureIgnoreCase)){
                throw new ArgumentException("Argument 'contentType.TypeWithSubype' value must be 'multipart/digest'.");
            }
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
            
            MIME_b_MultipartDigest retVal = new MIME_b_MultipartDigest(owner.ContentType);
            ParseInternal(owner,owner.ContentType.TypeWithSubtype,stream,retVal);

            return retVal;
        }

        #endregion
        

        #region Properties implementation

        /// <summary>
        /// Gets default body part Content-Type. For more info see RFC 2046 5.1.
        /// </summary>
        public override MIME_h_ContentType DefaultBodyPartContentType
        {
            /* RFC 2046 5.1.6.
                The absence of a Content-Type header usually indicates that the corresponding body has
                a content-type of "message/rfc822".
            */

            get{ 
                MIME_h_ContentType retVal = new MIME_h_ContentType("message/rfc822");

                return retVal; 
            }
        }

        #endregion
    }
}
