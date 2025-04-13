using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace LumiSoft.Net.Mail
{
    /// <summary>
    /// Represents Received: header "TCP-info" value. Defined in RFC 5321. 4.4.
    /// </summary>
    /// <remarks>
    /// <code>
    /// RFC 5321 4.4.
    ///     TCP-info        = address-literal / ( Domain FWS address-literal )
    ///     address-literal = "[" ( IPv4-address-literal / IPv6-address-literal / General-address-literal ) "]"
    /// </code>
    /// </remarks>
    public class Mail_t_TcpInfo
    {
        private IPAddress m_pIP      = null;
        private string    m_HostName = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <param name="hostName">Host name.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null reference.</exception>
        public Mail_t_TcpInfo(IPAddress ip,string hostName)
        {
            if(ip == null){
                throw new ArgumentNullException("ip");
            }

            m_pIP      = ip;
            m_HostName = hostName;
        }


        #region method ToString

        /// <summary>
        /// Returns this as string.
        /// </summary>
        /// <returns>Returns this as string.</returns>
        public override string ToString()
        {
            if(string.IsNullOrEmpty(m_HostName)){
                return "["  + m_pIP.ToString() + "]";
            }
            else{
                return m_HostName + " [" + m_pIP.ToString() + "]";
            }
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets IP address.
        /// </summary>
        public IPAddress IP
        {
            get{ return m_pIP; }
        }

        /// <summary>
        /// Gets host value. Value null means not specified.
        /// </summary>
        public string HostName
        {
            get{ return m_HostName; }
        }

        #endregion
    }
}
