using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class represents MIME video/xxx bodies. Defined in RFC 2046 4.4.
    /// </summary>
    /// <remarks>
    /// A media type of "video" indicates that the body contains a time-
    /// varying-picture image, possibly with color and coordinated sound.
    /// </remarks>
    public class MIME_b_Video : MIME_b_SinglepartBase
    {
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="mediaType">MIME media type.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>mediaType</b> is null reference.</exception>
        public MIME_b_Video(string mediaType) : base(new MIME_h_ContentType(mediaType))
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

            MIME_b_Video retVal = null;
            if(owner.ContentType != null){
                retVal = new MIME_b_Video(owner.ContentType.TypeWithSubtype);
            }
            else{
                retVal = new MIME_b_Video(defaultContentType.TypeWithSubtype);
            }

            Net_Utils.StreamCopy(stream,retVal.EncodedStream,32000);

            return retVal;
        }

        #endregion
        

        #region Properties implementation
                
        #endregion
    }
}
