using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.IO;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class represent MIME entity body provider.
    /// </summary>
    public class MIME_b_Provider
    {
        private Dictionary<string,Type> m_pBodyTypes = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public MIME_b_Provider()
        {
            m_pBodyTypes = new Dictionary<string,Type>(StringComparer.CurrentCultureIgnoreCase);
            m_pBodyTypes.Add("application/pkcs7-mime",typeof(MIME_b_ApplicationPkcs7Mime));
            m_pBodyTypes.Add("message/rfc822",typeof(MIME_b_MessageRfc822));
            m_pBodyTypes.Add("message/delivery-status",typeof(MIME_b_MessageDeliveryStatus));
            m_pBodyTypes.Add("multipart/alternative",typeof(MIME_b_MultipartAlternative));
            m_pBodyTypes.Add("multipart/digest",typeof(MIME_b_MultipartDigest));
            m_pBodyTypes.Add("multipart/encrypted",typeof(MIME_b_MultipartEncrypted));
            m_pBodyTypes.Add("multipart/form-data",typeof(MIME_b_MultipartFormData));
            m_pBodyTypes.Add("multipart/mixed",typeof(MIME_b_MultipartMixed));
            m_pBodyTypes.Add("multipart/parallel",typeof(MIME_b_MultipartParallel));
            m_pBodyTypes.Add("multipart/related",typeof(MIME_b_MultipartRelated));
            m_pBodyTypes.Add("multipart/report",typeof(MIME_b_MultipartReport));
            m_pBodyTypes.Add("multipart/signed",typeof(MIME_b_MultipartSigned));
        }


        #region method Parse

        /// <summary>
        /// Parses MIME entity body from specified stream.
        /// </summary>
        /// <param name="owner">Owner MIME entity.</param>
        /// <param name="stream">Stream from where to parse entity body.</param>
        /// <param name="defaultContentType">Default content type.</param>
        /// <returns>Returns parsed body.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>owner</b>, <b>strean</b> or <b>defaultContentType</b> is null reference.</exception>
        /// <exception cref="ParseException">Is raised when header field parsing errors.</exception>
        public MIME_b Parse(MIME_Entity owner,SmartStream stream,MIME_h_ContentType defaultContentType)
        {
            if(owner == null){
                throw new ArgumentNullException("owner");
            }
            if(stream == null){
                throw new ArgumentNullException("stream");
            }
            if(defaultContentType == null){
                throw new ArgumentNullException("defaultContentType");
            }

            string mediaType = defaultContentType.TypeWithSubtype;
            try{
                if(owner.ContentType != null){
                    mediaType = owner.ContentType.TypeWithSubtype;
                }
            }
            catch{
                // Do nothing, content will be MIME_b_Unknown.
                mediaType = "unknown/unknown";
            }

            Type bodyType = null;

            // We have exact body provider for specified mediaType.
            if(m_pBodyTypes.ContainsKey(mediaType)){
                bodyType = m_pBodyTypes[mediaType];                
            }
            // Use default mediaType.
            else{
                // Registered list of mediaTypes are available: http://www.iana.org/assignments/media-types/.

                string mediaRootType = mediaType.Split('/')[0].ToLowerInvariant();
                if(mediaRootType == "application"){
                    bodyType = typeof(MIME_b_Application);
                }
                else if(mediaRootType == "audio"){
                    bodyType = typeof(MIME_b_Audio);
                }
                else if(mediaRootType == "image"){
                    bodyType = typeof(MIME_b_Image);
                }
                else if(mediaRootType == "message"){
                    bodyType = typeof(MIME_b_Message);
                }
                else if(mediaRootType == "multipart"){
                    bodyType = typeof(MIME_b_Multipart);
                }
                else if(mediaRootType == "text"){
                    bodyType = typeof(MIME_b_Text);
                }
                else if(mediaRootType == "video"){
                    bodyType = typeof(MIME_b_Video);
                }
                else{
                    bodyType = typeof(MIME_b_Unknown);
                }
            }

            return (MIME_b)bodyType.GetMethod("Parse",System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.FlattenHierarchy).Invoke(null,new object[]{owner,defaultContentType,stream});
        }

        #endregion

    }
}
