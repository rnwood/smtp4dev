using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace LumiSoft.Net.SIP.Stack
{
    /// <summary>
    /// Implements SIP hop(address,port,transport). Defined in RFC 3261.
    /// </summary>
    public class SIP_Hop
    {
        private IPEndPoint m_pEndPoint = null;
        private string     m_Transport = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="ep">IP end point.</param>
        /// <param name="transport">SIP transport to use.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>ep</b> or <b>transport</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public SIP_Hop(IPEndPoint ep,string transport)
        {
            if(ep == null){
                throw new ArgumentNullException("ep");
            }
            if(transport == null){
                throw new ArgumentNullException("transport");
            }
            if(transport == ""){
                throw new ArgumentException("Argument 'transport' value must be specified.");
            }

            m_pEndPoint = ep;
            m_Transport = transport;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <param name="port">Destination port.</param>
        /// <param name="transport">SIP transport to use.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> or <b>transport</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public SIP_Hop(IPAddress ip,int port,string transport)
        {
            if(ip == null){
                throw new ArgumentNullException("ip");
            }
            if(port < 1){
                throw new ArgumentException("Argument 'port' value must be >= 1.");
            }
            if(transport == null){
                throw new ArgumentNullException("transport");
            }
            if(transport == ""){
                throw new ArgumentException("Argument 'transport' value must be specified.");
            }

            m_pEndPoint = new IPEndPoint(ip,port);
            m_Transport = transport;
        }


        #region Properties implementation

        /// <summary>
        /// Gets target IP end point.
        /// </summary>
        public IPEndPoint EndPoint
        {
            get{ return m_pEndPoint; }
        }

        /// <summary>
        /// Gets target IP address.
        /// </summary>
        public IPAddress IP
        {
            get{ return m_pEndPoint.Address; }
        }

        /// <summary>
        /// Gets target port.
        /// </summary>
        public int Port
        {
            get{ return m_pEndPoint.Port; }
        }

        /// <summary>
        /// Gets target SIP transport.
        /// </summary>
        public string Transport
        {
            get{ return m_Transport; }
        }

        #endregion

    }
}
