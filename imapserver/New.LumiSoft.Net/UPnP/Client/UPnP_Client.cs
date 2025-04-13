using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace LumiSoft.Net.UPnP.Client
{
    /// <summary>
    /// This class implements UPnP client.
    /// </summary>
    public class UPnP_Client
    {
        /// <summary>
        /// Defaultc onstructor.
        /// </summary>
        public UPnP_Client()
        {
        }


        #region method Search

        /// <summary>
        /// Searches the network for UPnP root devices.
        /// </summary>
        /// <param name="timeout">Search wait timeout in milliseconds.</param>
        /// <returns>Returns matched UPnP devices.</returns>
        public UPnP_Device[] Search(int timeout)
        {
            return Search("upnp:rootdevice",timeout);
        }
                
        /// <summary>
        /// Searches the network for UPnP devices.
        /// </summary>
        /// <param name="deviceType">UPnP device type. For example: "urn:schemas-upnp-org:device:InternetGatewayDevice:1".</param>
        /// <param name="timeout">Search wait timeout in milliseconds.</param>
        /// <returns>Returns matched UPnP devices.</returns>
        public UPnP_Device[] Search(string deviceType,int timeout)
        {
            if(timeout < 1){
                timeout = 1;
            }

            /* www.unpnp.org. Search request with M-SEARCH.
                    When a control point desires to search the network for devices, it MUST send a multicast request with method M-SEARCH in the
                    following format. Control points that know the address of a specific device MAY also use a similar format to send unicast requests
                    with method M-SEARCH.
             
                    For multicast M-SEARCH, the message format is defined below. Values in italics are placeholders for actual values.
             
                        M-SEARCH * HTTP/1.1
                        HOST: 239.255.255.250:1900
                        MAN: "ssdp:discover"
                        MX: seconds to delay response
                        ST: search target
                        USER-AGENT: OS/version UPnP/1.1 product/version
             
                Note: No body is present in requests with method M-SEARCH, but note that the message MUST have a blank line following the
                last header field.
                Note: The TTL for the IP packet SHOULD default to 2 and SHOULD be configurable.
             
                Listed below are details for the request line and header fields appearing in the listing above. Field names are not case sensitive.
                All field values are case sensitive except where noted.
    
                Request line
                Must be “M-SEARCH * HTTP/1.1”
             
                M-SEARCH
                    Method for search requests.
                *
                    Request applies generally and not to a specific resource. MUST be *.
            
                HTTP/1.1
                    HTTP version.
             
            Header fields
             
            HOST
                REQUIRED. Field value contains the multicast address and port reserved for SSDP by Internet Assigned Numbers Authority
                (IANA). MUST be 239.255.255.250:1900.
             
            MAN
                REQUIRED by HTTP Extension Framework. Unlike the NTS and ST field values, the field value of the MAN header field is
                enclosed in double quotes; it defines the scope (namespace) of the extension. MUST be "ssdp:discover".

            MX
                REQUIRED. Field value contains maximum wait time in seconds. MUST be greater than or equal to 1 and SHOULD be less than
                5 inclusive. Device responses SHOULD be delayed a random duration between 0 and this many seconds to balance load for
                the control point when it processes responses. This value MAY be increased if a large number of devices are expected to
                respond. The MX field value SHOULD NOT be increased to accommodate network characteristics such as latency or
                propagation delay (for more details, see the explanation below). Specified by UPnP vendor. Integer.
           
            ST
                REQUIRED. Field value contains Search Target. MUST be one of the following. (See NT header field in NOTIFY with ssdp:alive
                above.) Single URI.
             
                    ssdp:all
                        Search for all devices and services.
             
                    upnp:rootdevice
                        Search for root devices only.
             
                    uuid:device-UUID
                        Search for a particular device. device-UUID specified by UPnP vendor. See section 1.1.4, “UUID format and
                        RECOMMENDED generation algorithms” for the MANDATORY UUID format.
             
                    urn:schemas-upnp-org:device:deviceType:ver
                        Search for any device of this type where deviceType and ver are defined by the UPnP Forum working committee.
                    
                    urn:schemas-upnp-org:service:serviceType:ver
                        Search for any service of this type where serviceType and ver are defined by the UPnP Forum working committee.
                    
                    urn:domain-name:device:deviceType:ver
                        Search for any device of this typewhere domain-name (a Vendor Domain Name), deviceType and ver are defined
                        by the UPnP vendor and ver specifies the highest specifies the highest supported version of the device type.
                        Period characters in the Vendor Domain Name MUST be replaced with hyphens in accordance with RFC 2141.

                    urn:domain-name:service:serviceType:ver
                        Search for any service of this type. Where domain-name (a Vendor Domain Name), serviceType and ver are
                        defined by the UPnP vendor and ver specifies the highest specifies the highest supported version of the service
                        type. Period characters in the Vendor Domain Name MUST be replaced with hyphens in accordance with RFC 2141.
            
            USER-AGENT
                OPTIONAL. Specified by UPnP vendor. String. Field value MUST begin with the following “product tokens” (defined by
                HTTP/1.1). The first product token identifes the operating system in the form OS name/OS version, the second token
                represents the UPnP version and MUST be UPnP/1.1, and the third token identifes the product using the form
                product name/product version. For example, “USER-AGENT: unix/5.1 UPnP/1.1 MyProduct/1.0”. Control points MUST be
                prepared to accept a higher minor version number of the UPnP version than the control point itself implements. For
                example, control points implementing UDA version 1.0 will be able to interoperate with devices implementing
                UDA version 1.1.
            */

            StringBuilder query = new StringBuilder();
            query.Append("M-SEARCH * HTTP/1.1\r\n");
            query.Append("HOST: 239.255.255.250:1900\r\n");
            query.Append("MAN: \"ssdp:discover\"\r\n");
            query.Append("MX: 1\r\n");
            query.Append("ST: " + deviceType + "\r\n");
            query.Append("\r\n");

            using(Socket socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp)){
                socket.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.Broadcast,1);
                socket.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.IpTimeToLive,2);

                socket.SendTo(Encoding.UTF8.GetBytes(query.ToString()),new IPEndPoint(IPAddress.Broadcast,1900));

                List<string> deviceLocations = new List<string>();    
                byte[]       buffer          = new byte[32000];
                DateTime     startTime       = DateTime.Now;
                // Receive responses while timeout reached.
                while(startTime.AddMilliseconds(timeout) > DateTime.Now){
                    // We have response, read it.
                    if(socket.Poll(1,SelectMode.SelectRead)){
                        int countReceived = socket.Receive(buffer);

                        string[] responseLines = Encoding.UTF8.GetString(buffer,0,countReceived).Split('\n');                    
                        foreach(string responseLine in responseLines){
                            string[] name_value = responseLine.Split(new char[]{':'},2);
                            if(string.Equals(name_value[0],"location",StringComparison.InvariantCultureIgnoreCase)){
                                deviceLocations.Add(name_value[1].Trim());
                            }
                        }
                    }
                }

                // Create devices.
                List<UPnP_Device> devices = new List<UPnP_Device>();
                foreach(string location in deviceLocations){
                    try{
                        devices.Add(new UPnP_Device(location));
                    }
                    catch{
                    }
                }

                return devices.ToArray();
            }
        }

        /// <summary>
        /// Searches the network for UPnP devices.
        /// </summary>
        /// <param name="ip">IP address of UPnP device.</param>
        /// <param name="deviceType">UPnP device type. For example: "urn:schemas-upnp-org:device:InternetGatewayDevice:1".</param>
        /// <param name="timeout">Search wait timeout in milliseconds.</param>
        /// <returns>Returns matched UPnP devices.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null reference.</exception>
        public UPnP_Device[] Search(IPAddress ip,string deviceType,int timeout)
        {
            if(ip == null){
                throw new ArgumentNullException("ip");
            }
            if(timeout < 1){
                timeout = 1;
            }
                        
            using(Socket socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp)){
                socket.SetSocketOption(SocketOptionLevel.Socket,SocketOptionName.IpTimeToLive,2);
               
                StringBuilder query = new StringBuilder();
                query.Append("M-SEARCH * HTTP/1.1\r\n");
                query.Append("MAN: \"ssdp:discover\"\r\n");
                query.Append("MX: 1\r\n");
                query.Append("ST: " + deviceType + "\r\n");
                query.Append("\r\n");

                socket.SendTo(Encoding.UTF8.GetBytes(query.ToString()),new IPEndPoint(ip,1900));

                List<string> deviceLocations = new List<string>();    
                byte[]       buffer          = new byte[32000];
                DateTime     startTime       = DateTime.Now;
                // Receive responses while timeout reached.
                while(startTime.AddMilliseconds(timeout) > DateTime.Now){
                    // We have response, read it.
                    if(socket.Poll(1,SelectMode.SelectRead)){
                        int countReceived = socket.Receive(buffer);

                        string[] responseLines = Encoding.UTF8.GetString(buffer,0,countReceived).Split('\n');                    
                        foreach(string responseLine in responseLines){
                            string[] name_value = responseLine.Split(new char[]{':'},2);
                            if(string.Equals(name_value[0],"location",StringComparison.InvariantCultureIgnoreCase)){
                                deviceLocations.Add(name_value[1].Trim());
                            }
                        }
                    }
                }

                // Create devices.
                List<UPnP_Device> devices = new List<UPnP_Device>();
                foreach(string location in deviceLocations){
                    try{
                        devices.Add(new UPnP_Device(location));
                    }
                    catch{
                    }
                }

                return devices.ToArray();
            }        
        }

        #endregion
    }
}
