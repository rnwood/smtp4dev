using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SNTP.Client
{
    /// <summary>
    /// This class implments Simple Network Time Protocol (SNTP) protocol. Defined in RFC 2030. 
    /// </summary>
    public class SNTP_Client
    {
        /// <summary>
        /// Gets UTC time from NTP server.
        /// </summary>
        /// <param name="server">NTP server.</param>
        /// <param name="port">NTP port. Default NTP port is 123.</param>
        /// <returns>Returns UTC time.</returns>
        public static DateTime GetTime(string server,int port)
        {
            /* RFC 2030 4.
                Below is a description of the NTP/SNTP Version 4 message format,
                which follows the IP and UDP headers. This format is identical to
                that described in RFC-1305, with the exception of the contents of the
                reference identifier field. The header fields are defined as follows:

                                     1                   2                   3
                 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |LI | VN  |Mode |    Stratum    |     Poll      |   Precision   |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                          Root Delay                           |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                       Root Dispersion                         |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                     Reference Identifier                      |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                                                               |
                |                   Reference Timestamp (64)                    |
                |                                                               |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                                                               |
                |                   Originate Timestamp (64)                    |
                |                                                               |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                                                               |
                |                    Receive Timestamp (64)                     |
                |                                                               |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                                                               |
                |                    Transmit Timestamp (64)                    |
                |                                                               |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                 Key Identifier (optional) (32)                |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                |                                                               |
                |                                                               |
                |                 Message Digest (optional) (128)               |
                |                                                               |
                |                                                               |
                +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             
                2030 5. For unicast request we need to fill version and mode only.
                
            */
        }
    }
}
