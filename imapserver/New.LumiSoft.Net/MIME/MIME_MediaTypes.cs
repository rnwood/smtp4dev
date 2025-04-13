using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.MIME
{
    /// <summary>
    /// This class holds well known Content-Type header field media types. For example: text/plain, application/octet-stream.
    /// Full IANA registered list can be found from: http://www.iana.org/assignments/media-types.
    /// </summary>
    public class MIME_MediaTypes
    {
        #region application

        /// <summary>
        /// This class holds well-known application/xxx media types.
        /// </summary>
        public class Application
        {
            /// <summary>
            /// "application/octet-stream". Defined in RFC 2045,2046.
            /// </summary>
            public static readonly string octet_stream = "application/octet-stream";

            /// <summary>
            /// "application/pdf". Defined in RFC 3778.
            /// </summary>
            public static readonly string pdf = "application/pdf";

            /// <summary>
            /// "application/sdp". Defined in RFC 4566.
            /// </summary>
            public static readonly string sdp = "application/sdp";

            /// <summary>
            /// "application/xml". Defined RFC 3023.
            /// </summary>
            public static readonly string xml = "application/xml";

            /// <summary>
            /// "application/zip". Defined in RFC 4566.
            /// </summary>
            public static readonly string zip = "application/zip";

            /// <summary>
            /// "application/x-pkcs7-signature". Defined in RFC 2311,2633.
            /// </summary>
            public static readonly string x_pkcs7_signature = "application/x-pkcs7-signature";

            /// <summary>
            /// "application/pkcs7-mime". Defined in RFC 5751.
            /// </summary>
            public static readonly string pkcs7_mime = "application/pkcs7-mime";
        }

        #endregion

        #region image

        /// <summary>
        /// This class holds well-known image/xxx media types.
        /// </summary>
        public class Image
        {
            /// <summary>
            /// "image/gif".
            /// </summary>
            public static readonly string gif = "image/gif";

            /// <summary>
            /// "image/jpeg".
            /// </summary>
            public static readonly string jpeg = "image/jpeg";

            /// <summary>
            /// "image/tiff".
            /// </summary>
            public static readonly string tiff = "image/tiff";
        }
                
        #endregion

        #region text

        /// <summary>
        /// This class holds well-known text/xxx media types.
        /// </summary>
        public class Text
        {
            /// <summary>
            /// "text/calendar". Defined in RFC 2445.
            /// </summary>
            public static readonly string calendar = "text/calendar";
        
            /// <summary>
            /// "text/css". Defined in RFC 2854
            /// </summary>
            public static readonly string css = "text/css";

            /// <summary>
            /// "text/html". Defined in RFC 2854.
            /// </summary>
            public static readonly string html = "text/html";

            /// <summary>
            /// "text/plain". Defined in RFC 2646,2046.
            /// </summary>
            public static readonly string plain = "text/plain";

            /// <summary>
            /// "text/rfc822-headers". Defined in RFC 1892.
            /// </summary>
            public static readonly string rfc822_headers = "text/rfc822-headers";

            /// <summary>
            /// "text/richtext". Defined in RFC 2045,2046.
            /// </summary>
            public static readonly string richtext = "text/richtext";

            /// <summary>
            /// "text/xml". Defined in RFC 3023.
            /// </summary>
            public static readonly string xml = "text/xml";
        }

        #endregion

        #region multipart

        /// <summary>
        /// This class holds well-known multipart/xxx media types.
        /// </summary>
        public class Multipart
        {
            /// <summary>
            /// "multipart/alternative". Defined in RFC 2045,2046.
            /// </summary>
            public static readonly string alternative = "multipart/alternative";

            /// <summary>
            /// "multipart/digest". Defined in RFC 2045,2046.
            /// </summary>
            public static readonly string digest = "multipart/digest";

            /// <summary>
            /// "multipart/digest". Defined in RFC 1847.
            /// </summary>
            public static readonly string encrypted = "multipart/digest";

            /// <summary>
            /// "multipart/form-data". Defined in RFC 2388.
            /// </summary>
            public static readonly string form_data = "multipart/form-data";

            /// <summary>
            /// "multipart/mixed". Defined in RFC 2045,2046.
            /// </summary>
            public static readonly string mixed = "multipart/mixed";

            /// <summary>
            /// "multipart/parallel". Defined in RFC 2045,2046.
            /// </summary>
            public static readonly string parallel = "multipart/parallel";

            /// <summary>
            /// "multipart/related". Defined in RFC 2387.
            /// </summary>
            public static readonly string related  = "multipart/related";

            /// <summary>
            /// "multipart/report". Defined in RFC 1892.
            /// </summary>
            public static readonly string report = "multipart/report";

            /// <summary>
            /// "multipart/signed". Defined in RFC 1847.
            /// </summary>
            public static readonly string signed = "multipart/signed";

            /// <summary>
            /// "multipart/voice-message". Defined in RFC 2421,2423.
            /// </summary>
            public static readonly string voice_message = "multipart/voice-message";
        }

        #endregion

        #region message

        /// <summary>
        /// This class holds well-known message/xxx media types.
        /// </summary>
        public class Message
        {
            /// <summary>
            /// "message/rfc822". 
            /// </summary>
            public static readonly string rfc822 = "message/rfc822";

            /// <summary>
            /// "message/disposition-notification". 
            /// </summary>
            public static readonly string disposition_notification = "message/disposition-notification";

            /// <summary>
            /// "message/delivery-status". Defined in RFC 3464.
            /// </summary>
            public static readonly string delivery_status = "message/delivery-status";
        }

        #endregion        
    }
}
