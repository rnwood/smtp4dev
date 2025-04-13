using System;
using System.Collections.Generic;
using System.Text;
using System.Net;

namespace LumiSoft.Net.UPnP.NAT
{
    /// <summary>
    /// This class represents NAT port mapping entry.
    /// </summary>
    public class UPnP_NAT_Map
    {
        private bool   m_Enabled       = false;
        private string m_Protocol      = "";
        private string m_RemoteHost    = "";
        private string m_ExternalPort  = "";
        private string m_InternalHost  = "";
        private int    m_InternalPort  = 0;
        private string m_Description   = "";
        private int    m_LeaseDuration = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="enabled">Specifies if NAT port map is enabled.</param>
        /// <param name="protocol">Port mapping protocol. Nomrally this value TCP or UDP.</param>
        /// <param name="remoteHost">Remote host IP address. NOTE: Some implementations may use wilcard(*,?) values.</param>
        /// <param name="externalPort">NAT external port number. NOTE: Some implementations may use wilcard(*,?) values.</param>
        /// <param name="internalHost">Internal host IP address.</param>
        /// <param name="internalPort">Internal host port number.</param>
        /// <param name="description">NAT port mapping description.</param>
        /// <param name="leaseDuration">Lease duration in in seconds. Value null means "never expires".</param>
        public UPnP_NAT_Map(bool enabled,string protocol,string remoteHost,string externalPort,string internalHost,int internalPort,string description,int leaseDuration)
        {
            m_Enabled       = enabled;
            m_Protocol      = protocol;
            m_RemoteHost    = remoteHost;
            m_ExternalPort  = externalPort;
            m_InternalHost  = internalHost;
            m_InternalPort  = internalPort;
            m_Description   = description;
            m_LeaseDuration = leaseDuration;
        }


        #region Properties implementation

        /// <summary>
        /// Gets if NAT port map is enabled.
        /// </summary>
        public bool Enabled
        {
            get{ return m_Enabled; }
        }

        /// <summary>
        /// Gets port mapping protocol. Nomrally this value TCP or UDP.
        /// </summary>
        public string Protocol
        {
            get{ return m_Protocol; }
        }

        /// <summary>
        /// Gets remote host IP address. NOTE: Some implementations may use wilcard(*,?) values.
        /// </summary>
        public string RemoteHost
        {
            get{ return m_RemoteHost; }
        }

        /// <summary>
        /// Gets NAT external port number. NOTE: Some implementations may use wilcard(*,?) values.
        /// </summary>
        public string ExternalPort
        {
            get{ return m_ExternalPort; }
        }

        /// <summary>
        /// Gets internal host IP address.
        /// </summary>
        public string InternalHost
        {
            get{ return m_InternalHost; }
        }

        /// <summary>
        /// Gets internal host port number.
        /// </summary>
        public int InternalPort
        {
            get{ return m_InternalPort; }
        }

        /// <summary>
        /// Gets NAT port mapping description.
        /// </summary>
        public string Description
        {
            get{ return m_Description; }
        }

        /// <summary>
        /// Gets lease duration in in seconds. Value null means "never expires".
        /// </summary>
        public int LeaseDuration
        {
            get{ return m_LeaseDuration; }
        }

        #endregion
    }
}
