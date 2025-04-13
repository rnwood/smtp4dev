using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SDP
{
    /// <summary>
    /// A SDP_ConnectionData represents an <B>c=</B> SDP message field. Defined in RFC 4566 5.7. Connection Data.
    /// </summary>
    public class SDP_Connection
    {
        private string m_NetType     = "IN";
        private string m_AddressType = "";
        private string m_Address     = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="netType">Network type(IN).</param>
        /// <param name="addressType">Address type(IP4/IP6).</param>
        /// <param name="address">Host name or IP address.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>netType</b>, <b>addressType</b> or <b>address</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public SDP_Connection(string netType,string addressType,string address)
        {
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
            if(address == null){
                throw new ArgumentNullException("address");
            }
            if(address == string.Empty){
                throw new ArgumentException("Argument 'address' value must be specified.");
            }

            m_NetType     = netType;
            m_AddressType = addressType;
            m_Address     = address;
        }


        #region method static Parse

        /// <summary>
        /// Parses media from "c" SDP message field.
        /// </summary>
        /// <param name="cValue">"m" SDP message field.</param>
        /// <returns></returns>
        public static SDP_Connection Parse(string cValue)
        {
            // c=<nettype> <addrtype> <connection-address>

            string netType          = "";
            string addrType         = "";
            string connectionAddress = "";

            // Remove c=
            StringReader r = new StringReader(cValue);
            r.QuotedReadToDelimiter('=');

            //--- <nettype> ------------------------------------------------------------
            string word = r.ReadWord();
            if(word == null){
                throw new Exception("SDP message \"c\" field <nettype> value is missing !");
            }
            netType = word;

            //--- <addrtype> -----------------------------------------------------------
            word = r.ReadWord();
            if(word == null){
                throw new Exception("SDP message \"c\" field <addrtype> value is missing !");
            }
            addrType = word;

            //--- <connection-address> -------------------------------------------------
            word = r.ReadWord();
            if(word == null){
                throw new Exception("SDP message \"c\" field <connection-address> value is missing !");
            }
            connectionAddress = word;

            return new SDP_Connection(netType,addrType,connectionAddress);
        }

        #endregion

        #region method ToValue

        /// <summary>
        /// Converts this to valid connection data stirng. 
        /// </summary>
        /// <returns></returns>
        public string ToValue()
        {
            // c=<nettype> <addrtype> <connection-address>

            return "c=" + NetType + " " + AddressType + " " + Address + "\r\n";
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets net type. Currently it's always IN(Internet).
        /// </summary>
        public string NetType
        {
            get{ return m_NetType; }
        }

        /// <summary>
        /// Gets or sets address type. Currently defined values IP4 or IP6.
        /// </summary>
        public string AddressType
        {
            get{ return m_AddressType; }

            set{
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property AddressType can't be null or empty !");
                }

                m_AddressType = value; 
            }
        }

        /// <summary>
        /// Gets or sets connection address.
        /// </summary>
        public string Address
        {
            get{ return m_Address; }

            set{ 
                if(string.IsNullOrEmpty(value)){
                    throw new ArgumentException("Property Address can't be null or empty !");
                }

                m_Address = value;
            }
        }

        #endregion

    }
}
