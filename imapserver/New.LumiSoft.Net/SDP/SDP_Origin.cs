using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SDP
{
    /// <summary>
    /// This class represents SDP Origin("o="). Defined in RFC 4566 5.2.
    /// </summary>
    public class SDP_Origin
    {
        private string m_UserName       = null;
        private long   m_SessionID      = 0;
        private long   m_SessionVersion = 0;
        private string m_NetType        = null;
        private string m_AddressType    = null;
        private string m_UnicastAddress = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <param name="sessionID">Session ID.</param>
        /// <param name="sessionVersion">Session version.</param>
        /// <param name="netType">Network type(IN).</param>
        /// <param name="addressType">Address type(IP4/IP6).</param>
        /// <param name="unicastAddress">Host name.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>userName</b>, <b>netType</b>, <b>addressType</b> or <b>unicastAddress</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public SDP_Origin(string userName,long sessionID,long sessionVersion,string netType,string addressType,string unicastAddress)
        {
            if(userName == null){
                throw new ArgumentNullException("userName");
            }
            if(userName == string.Empty){
                throw new ArgumentException("Argument 'userName' value must be specified.");
            }
            if(netType == null){
                throw new ArgumentNullException("netType");
            }
            if(netType == string.Empty){
                throw new ArgumentException("Argument 'netType' value must be specified.");
            }
            if(addressType == null){
                throw new ArgumentNullException("addressType");
            }
            if(addressType == string.Empty){
                throw new ArgumentException("Argument 'addressType' value must be specified.");
            }
            if(unicastAddress == null){
                throw new ArgumentNullException("unicastAddress");
            }
            if(unicastAddress == string.Empty){
                throw new ArgumentException("Argument 'unicastAddress' value must be specified.");
            }

            m_UserName       = userName;
            m_SessionID      = sessionID;
            m_SessionVersion = sessionVersion;
            m_NetType        = netType;
            m_AddressType    = addressType;
            m_UnicastAddress = unicastAddress;
        }


        #region static method Parse

        /// <summary>
        /// Parses SDP Origin("o=") from specified value.
        /// </summary>
        /// <param name="value">Origin value.</param>
        /// <returns>Returns parsed SDP Origin.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
        public static SDP_Origin Parse(string value)
        {
            if(value == null){
                throw new ArgumentNullException("value");
            }

            value = value.Trim();

            /* o=<username> <sess-id> <sess-version> <nettype> <addrtype> <unicast-address>
            */

            if(!value.ToLower().StartsWith("o=")){
                throw new ParseException("Invalid SDP Origin('o=') value '" + value + "'.");
            }
            value = value.Substring(2);

            string[] values = value.Split(' ');
            if(values.Length != 6){
                throw new ParseException("Invalid SDP Origin('o=') value '" + value + "'.");
            }

            return new SDP_Origin(
                values[0],
                Convert.ToInt64(values[1]),
                Convert.ToInt64(values[2]),
                values[3],
                values[4],
                values[5]
            );
        }

        #endregion


        #region override method ToString

        /// <summary>
        /// Returns origin as SDP string.
        /// </summary>
        /// <returns>Returns origin as SDP string.</returns>
        public override string ToString()
        {
            return "o=" + m_UserName + " " + m_SessionID +  " " + m_SessionVersion + " " + m_NetType + " " + m_AddressType + " " + m_UnicastAddress + "\r\n";
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets user name.
        /// </summary>
        public string UserName
        {
            get{ return m_UserName; }
        }

        /// <summary>
        /// Gets session ID.
        /// </summary>
        public long SessionID
        {
            get{ return m_SessionID; }
        }

        /// <summary>
        /// Gets session version.
        /// </summary>
        /// <remarks>This value should be increased each time when session data has modified.</remarks>
        public long SessionVersion
        {
            get{ return m_SessionVersion; }

            set{ m_SessionVersion = value; }
        }

        /// <summary>
        /// Gets network type. Currently "IN" is only defined value.
        /// </summary>
        public string NetType
        {
            get{ return m_NetType; }
        }

        /// <summary>
        /// Gets address type. Currently "IP4" and "IP6" are only defined values.
        /// </summary>
        public string AddressType
        {
            get{ return m_AddressType; }
        }

        /// <summary>
        /// Gets address(DNS host name or IP address). 
        /// </summary>
        public string UnicastAddress
        {
            get{ return m_UnicastAddress; }
        }

        #endregion

    }
}
