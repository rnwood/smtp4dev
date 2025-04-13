using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.DNS.Client;

namespace LumiSoft.Net.DNS
{
    /// <summary>
    /// DNS SRV record. SRV record specifies the location of services. Defined in RFC 2782.
    /// </summary>
    [Serializable]
    public class DNS_rr_SRV : DNS_rr
    {
        private int    m_Priority = 1;
        private int    m_Weight   = 1;
        private int    m_Port     = 0;
        private string m_Target   = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="priority">Service priority.</param>
        /// <param name="weight">Weight value.</param>
        /// <param name="port">Service port.</param>
        /// <param name="target">Service provider host name or IP address.</param>
        /// <param name="ttl">Time to live value in seconds.</param>
        public DNS_rr_SRV(string name,int priority,int weight,int port,string target,int ttl) : base(name,DNS_QType.SRV,ttl)
        {
            m_Priority = priority;
            m_Weight   = weight;
            m_Port     = port;
            m_Target   = target;
        }


        #region static method Parse

        /// <summary>
        /// Parses resource record from reply data.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="reply">DNS server reply data.</param>
        /// <param name="offset">Current offset in reply data.</param>
        /// <param name="rdLength">Resource record data length.</param>
        /// <param name="ttl">Time to live in seconds.</param>
        public static DNS_rr_SRV Parse(string name,byte[] reply,ref int offset,int rdLength,int ttl)
        {
            // Priority Weight Port Target
            
            // Priority
            int priority  = reply[offset++] << 8 | reply[offset++];

            // Weight
            int weight  = reply[offset++] << 8 | reply[offset++];

            // Port
            int port  = reply[offset++] << 8 | reply[offset++];

            // Target
            string target = "";
            Dns_Client.GetQName(reply,ref offset,ref target);

            return new DNS_rr_SRV(name,priority,weight,port,target,ttl);
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets service priority. Lowest value means greater priority.
        /// </summary>
        public int Priority
        {
            get{ return m_Priority; }
        }

        /// <summary>
        /// Gets weight. The weight field specifies a relative weight for entries with the same priority. 
        /// Larger weights SHOULD be given a proportionately higher probability of being selected.
        /// </summary>
        public int Weight
        {
            get{ return m_Weight; }
        }

        /// <summary>
        /// Port where service runs.
        /// </summary>
        public int Port
        {
            get{ return m_Port; }
        }

        /// <summary>
        /// Service provider host name or IP address.
        /// </summary>
        public string Target
        {
            get{ return m_Target; }
        }

        #endregion

    }
}
