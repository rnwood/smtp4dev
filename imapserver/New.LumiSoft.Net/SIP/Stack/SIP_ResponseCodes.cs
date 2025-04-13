using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// This class holds SIP respnse codes.
    /// </summary>
    public class SIP_ResponseCodes
    {
        /// <summary>
        /// This response indicates that the request has been received by the
        /// next-hop server and that some unspecified action is being taken on
        /// behalf of this call (for example, a database is being consulted).
        /// </summary>
        public static readonly string x100_Trying = "100 Trying";

        /// <summary>
        /// The UA receiving the INVITE is trying to alert the user.
        /// </summary>
        public static readonly string x180_Ringing = "180 Ringing";

        /// <summary>
        /// A server MAY use this status code to indicate that the call is being
        /// forwarded to a different set of destinations.
        /// </summary>
        public static readonly string x181_Call_Forwarded = "181 Call Is Being Forwarded";

        /// <summary>
        /// The called party is temporarily unavailable, but the server has
        /// decided to queue the call rather than reject it.  When the callee
        /// becomes available, it will return the appropriate final status response.
        /// </summary>
        public static readonly string x182_Queued = "182 Queued";

        /// <summary>
        /// The 183 (Session Progress) response is used to convey information
        /// about the progress of the call that is not otherwise classified.
        /// </summary>
        public static readonly string x183_Session_Progress = "183 Session Progress";

        /// <summary>
        /// The request has succeeded.
        /// </summary>
        public static readonly string x200_Ok = "200 OK";

        /// <summary>
        /// The request has accepted. Defined in rfc 3265.
        /// </summary>
        public static readonly string x202_Ok = "202 Accepted";

        /// <summary>
        /// The user can no longer be found at the address in the Request-URI,
        /// and the requesting client SHOULD retry at the new address given by
        /// the Contact header field (Section 20.10). Defined in rfc 3265.
        /// </summary>
        public static readonly string x301_Ok = "301 Moved Permanently";

        /// <summary>
        /// The requesting client SHOULD retry the request at the new address(es)
        /// given by the Contact header field (Section 20.10). Defined in rfc 3265.
        /// </summary>
        public static readonly string x302_Ok = "302 Moved Temporarily";

        /// <summary>
        /// The request could not be understood due to malformed syntax.  The
        /// Reason-Phrase SHOULD identify the syntax problem in more detail, for
        /// example, "Missing Call-ID header field".
        /// </summary>
        public static readonly string x400_Bad_Request = "400 Bad Request";

        /// <summary>
        /// The request requires user authentication.
        /// </summary>
        public static readonly string x401_Unauthorized = "401 Unauthorized";

        /// <summary>
        /// The server understood the request, but is refusing to fulfill it.
        /// Authorization will not help, and the request SHOULD NOT be repeated.
        /// </summary>
        public static readonly string x403_Forbidden = "403 Forbidden";

        /// <summary>
        /// The server has definitive information that the user does not exist at
        /// the domain specified in the Request-URI.
        /// </summary>
        public static readonly string x404_Not_Found = "404 Not Found";

        /// <summary>
        /// The method specified in the Request-Line is understood, but not
        /// allowed for the address identified by the Request-URI.
        /// </summary>
        public static readonly string x405_Method_Not_Allowed = "405 Method Not Allowed";

        /// <summary>
        /// The resource identified by the request is only capable of generating
        /// response entities that have content characteristics not acceptable
        /// according to the Accept header field sent in the request.
        /// </summary>
        public static readonly string x406_Not_Acceptable = "406 Not Acceptable";

        /// <summary>
        /// This code is similar to 401 (Unauthorized), but indicates that the
        /// client MUST first authenticate itself with the proxy.
        /// </summary>
        public static readonly string x407_Proxy_Authentication_Required = "407 Proxy Authentication Required";

        /// <summary>
        /// The server could not produce a response within a suitable amount of
        /// time, for example, if it could not determine the location of the user in time.
        /// </summary>
        public static readonly string x408_Request_Timeout = "408 Request Timeout";

        /// <summary>
        /// The requested resource is no longer available at the server and no
        /// forwarding address is known.  This condition is expected to be
        /// considered permanent.
        /// </summary>
        public static readonly string x410_Gone = "410 Gone";

        /// <summary>
        /// Is used to indicate that the precondition given for the request has failed. Defined in rfc 3903.
        /// </summary>
        public static readonly string x412_Conditional_Request_Failed = "412 Conditional Request Failed";

        /// <summary>
        /// The server is refusing to process a request because the request
        /// entity-body is larger than the server is willing or able to process.
        /// The server MAY close the connection to prevent the client from
        /// continuing the request.
        /// </summary>
        public static readonly string x413_Request_Entity_Too_Large = "413 Request Entity Too Large";

        /// <summary>
        /// The server is refusing to service the request because the Request-URI
        /// is longer than the server is willing to interpret.
        /// </summary>
        public static readonly string x414_RequestURI_Too_Long = "414 Request-URI Too Long";

        /// <summary>
        /// The server is refusing to service the request because the message
        /// body of the request is in a format not supported by the server for
        /// the requested method.  The server MUST return a list of acceptable
        /// formats using the Accept, Accept-Encoding, or Accept-Language header
        /// field, depending on the specific problem with the content.
        /// </summary>
        public static readonly string x415_Unsupported_Media_Type = "415 Unsupported Media Type";

        /// <summary>
        /// The server cannot process the request because the scheme of the URI
        /// in the Request-URI is unknown to the server.
        /// </summary>
        public static readonly string x416_Unsupported_URI_Scheme = "416 Unsupported URI Scheme";

        /// <summary>
        /// TODO: add description. Defined in rfc 4412.
        /// </summary>
        public static readonly string x417_Unknown_Resource_Priority = "417 Unknown Resource-Priority";

        /// <summary>
        /// The server did not understand the protocol extension specified in a
        /// Proxy-Require or Require header field.
        /// </summary>
        public static readonly string x420_Bad_Extension = "420 Bad Extension";

        /// <summary>
        /// The UAS needs a particular extension to process the request, but this
        /// extension is not listed in a Supported header field in the request.
        /// Responses with this status code MUST contain a Require header field
        /// listing the required extensions.
        /// </summary>
        public static readonly string x421_Extension_Required = "421 Extension Required";

        /// <summary>
        /// It is generated by a UAS or proxy when a request contains a Session-Expires header field 
        /// with a duration below the minimum timer for the server. The 422 response MUST contain a Min-SE
        /// header field with the minimum timer for that server.
        /// </summary>
        public static readonly string x422_Session_Interval_Too_Small = "422 Session Interval Too Small";

        /// <summary>
        /// The server is rejecting the request because the expiration time of
        /// the resource refreshed by the request is too short.  This response
        /// can be used by a registrar to reject a registration whose Contact
        /// header field expiration time was too small.
        /// </summary>
        public static readonly string x423_Interval_Too_Brief = "423 Interval Too Brief";

        /// <summary>
        /// It is used when the verifier receives a message with an Identity signature that does not 
        /// correspond to the digest-string calculated by the verifier. Defined in rfc 4474.
        /// </summary>
        public static readonly string x428_Use_Identity_Header = "428 Use Identity Header";

        /// <summary>
        /// TODO: add description. Defined in rfc 3892.
        /// </summary>
        public static readonly string x429_Provide_Referrer_Identity = "429 Provide Referrer Identity";

        /// <summary>
        /// It is used when the Identity-Info header contains a URI that cannot be dereferenced by the 
        /// verifier (either the URI scheme is unsupported by the verifier, or the resource designated by
        /// the URI is otherwise unavailable). Defined in rfc 4474.
        /// </summary>
        public static readonly string x436_Bad_Identity_Info = "436 Bad Identity-Info";

        /// <summary>
        /// It is used when the verifier cannot validate the certificate referenced by the URI of the 
        /// Identity-Info header, because, for example, the certificate is self-signed, or signed by a
        /// root certificate authority for whom the verifier does not possess a root certificate. 
        /// Defined in rfc 4474.
        /// </summary>
        public static readonly string x437_Unsupported_Certificate = "437 Unsupported Certificate";

        /// <summary>
        /// It is used when the verifier receives a message with an Identity signature that does not 
        /// correspond to the digest-string calculated by the verifier. Defined in rfc 4474.
        /// </summary>
        public static readonly string x438_Invalid_Identity_Header = "438 Invalid Identity Header";

        /// <summary>
        /// The callee's end system was contacted successfully but the callee is
        /// currently unavailable (for example, is not logged in, logged in but
        /// in a state that precludes communication with the callee, or has
        /// activated the "do not disturb" feature).
        /// </summary>
        public static readonly string x480_Temporarily_Unavailable = "480 Temporarily Unavailable";

        /// <summary>
        /// This status indicates that the UAS received a request that does not
        /// match any existing dialog or transaction.
        /// </summary>
        public static readonly string x481_Call_Transaction_Does_Not_Exist = "481 Call/Transaction Does Not Exist";

        /// <summary>
        /// The server has detected a loop.
        /// </summary>
        public static readonly string x482_Loop_Detected = "482 Loop Detected";

        /// <summary>
        /// The server received a request that contains a Max-Forwards.
        /// </summary>
        public static readonly string x483_Too_Many_Hops = "483 Too Many Hops";

        /// <summary>
        /// The server received a request with a Request-URI that was incomplete.
        /// Additional information SHOULD be provided in the reason phrase.
        /// </summary>
        public static readonly string x484_Address_Incomplete = "484 Address Incomplete";

        /// <summary>
        /// The Request-URI was ambiguous.
        /// </summary>
        public static readonly string x485_Ambiguous = "485 Ambiguous";

        /// <summary>
        /// The callee's end system was contacted successfully, but the callee is
        /// currently not willing or able to take additional calls at this end
        /// system. The response MAY indicate a better time to call in the
        /// Retry-After header field.
        /// </summary>
        public static readonly string x486_Busy_Here = "486 Busy Here";

        /// <summary>
        /// The request was terminated by a BYE or CANCEL request. This response
        /// is never returned for a CANCEL request itself.
        /// </summary>
        public static readonly string x487_Request_Terminated = "487 Request Terminated";

        /// <summary>
        /// The response has the same meaning as 606 (Not Acceptable), but only
        /// applies to the specific resource addressed by the Request-URI and the
        /// request may succeed elsewhere.
        /// </summary>
        public static readonly string x488_Not_Acceptable_Here = "488 Not Acceptable Here";

        /// <summary>
        /// Is used to indicate that the server did not understand the event package specified 
        /// in a "Event" header field. Defined in rfc 3265.
        /// </summary>
        public static readonly string x489_Bad_Event = "489 Bad Event";

        /// <summary>
        /// The request was received by a UAS that had a pending request within
        /// the same dialog.
        /// </summary>
        public static readonly string x491_Request_Pending = "491 Request Pending";

        /// <summary>
        /// The request was received by a UAS that contained an encrypted MIME
        /// body for which the recipient does not possess or will not provide an
        /// appropriate decryption key.
        /// </summary>
        public static readonly string x493_Undecipherable = "493 Undecipherable";

        /// <summary>
        /// TODO: add description. Defined in rfc 3329.
        /// </summary>
        public static readonly string x494_Security_Agreement_Required = "494 Security Agreement Required";

        /// <summary>
        /// The server encountered an unexpected condition that prevented it from
        /// fulfilling the request.
        /// </summary>
        public static readonly string x500_Server_Internal_Error = "500 Server Internal Error";

        /// <summary>
        /// The server does not support the functionality required to fulfill the request.
        /// </summary>
        public static readonly string x501_Not_Implemented = "501 Not Implemented";

        /// <summary>
        /// The server, while acting as a gateway or proxy, received an invalid
        /// response from the downstream server it accessed in attempting to
        /// fulfill the request.
        /// </summary>
        public static readonly string x502_Bad_Gateway = "502 Bad Gateway";

        /// <summary>
        /// The server is temporarily unable to process the request due to a
        /// temporary overloading or maintenance of the server.
        /// </summary>
        public static readonly string x503_Service_Unavailable = "503 Service Unavailable";

        /// <summary>
        /// The server did not receive a timely response from an external server
        /// it accessed in attempting to process the request.
        /// </summary>
        public static readonly string x504_Timeout = "504 Server Time-out";

        /// <summary>
        /// The server does not support, or refuses to support, the SIP protocol
        /// version that was used in the request.
        /// </summary>
        public static readonly string x504_Version_Not_Supported = "505 Version Not Supported";

        /// <summary>
        /// The server was unable to process the request since the message length
        /// exceeded its capabilities.
        /// </summary>
        public static readonly string x513_Message_Too_Large = "513 Message Too Large";

        /// <summary>
        /// When a UAS, acting as an answerer, cannot or is not willing to meet the preconditions 
        /// in the offer. Defined in rfc 3312.
        /// </summary>
        public static readonly string x580_Precondition_Failure = "580 Precondition Failure";

        /// <summary>
        /// The callee's end system was contacted successfully but the callee is
        /// busy and does not wish to take the call at this time.
        /// </summary>
        public static readonly string x600_Busy_Everywhere = "600 Busy Everywhere";

        /// <summary>
        /// The callee's machine was successfully contacted but the user
        /// explicitly does not wish to or cannot participate.
        /// </summary>
        public static readonly string x603_Decline = "603 Decline";

        /// <summary>
        /// The server has authoritative information that the user indicated in
        /// the Request-URI does not exist anywhere
        /// </summary>
        public static readonly string x604_Does_Not_Exist_Anywhere = "604 Does Not Exist Anywhere";

        /// <summary>
        /// The user's agent was contacted successfully but some aspects of the
        /// session description such as the requested media, bandwidth, or
        /// addressing style were not acceptable.
        /// </summary>
        public static readonly string x606_Not_Acceptable = "606 Not Acceptable";
    }
}
