using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class represents MIME audio/xxx bodies. Defined in RFC 2046 4.3.
    /// </summary>
    /// <remarks>A media type of "audio" indicates that the body contains audio data.</remarks>
    public class MIME_b_Audio : MIME_b_SinglepartBase
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="mediaType">MIME media type.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>mediaType</b> is null reference.</exception>
        public MIME_b_Audio(string mediaType) : base(new MIME_h_ContentType(mediaType))
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

            MIME_b_Audio retVal = null;
            if(owner.ContentType != null){
                retVal = new MIME_b_Audio(owner.ContentType.TypeWithSubtype);
            }
            else{
                retVal = new MIME_b_Audio(defaultContentType.TypeWithSubtype);
            }

            Net_Utils.StreamCopy(stream,retVal.EncodedStream,32000);

            return retVal;
        }

        #endregion
        

        #region Properties implementation

        #endregion
    }
}
