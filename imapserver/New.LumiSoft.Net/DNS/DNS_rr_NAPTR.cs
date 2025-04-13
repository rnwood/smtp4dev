using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.DNS.Client;

namespace LumiSoft.Net.DNS
{
    /// <summary>
    /// NAPRT(Naming Authority Pointer) resource record. Defined in RFC 3403.
    /// </summary>
    [Serializable]
    public class DNS_rr_NAPTR : DNS_rr
    {
        private int    m_Order       = 0;
        private int    m_Preference  = 0;
        private string m_Flags       = "";
        private string m_Services    = "";
        private string m_Regexp      = "";
        private string m_Replacement = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="order">Oorder in which the NAPTR records MUST be processed.</param>
        /// <param name="preference">Order in which NAPTR records with equal Order values SHOULD be processed.</param>
        /// <param name="flags">Flags which control the rewriting and interpretation of the fields in the record.</param>
        /// <param name="services">Services related to this record.</param>
        /// <param name="regexp">Regular expression that is applied to the original string.</param>
        /// <param name="replacement">Regular expressions replacement value.</param>
        /// <param name="ttl">Time to live value in seconds.</param>
        public DNS_rr_NAPTR(string name,int order,int preference,string flags,string services,string regexp,string replacement,int ttl) : base(name,DNS_QType.NAPTR,ttl)
        {
            m_Order       = order;
            m_Preference  = preference;
            m_Flags       = flags;
            m_Services    = services;
            m_Regexp      = regexp;
            m_Replacement = replacement;
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
        public static DNS_rr_NAPTR Parse(string name,byte[] reply,ref int offset,int rdLength,int ttl)
        {
            /* RFC 3403.
                The packet format for the NAPTR record is as follows
                                               1  1  1  1  1  1
                 0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                |                     ORDER                     |
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                |                   PREFERENCE                  |
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                /                     FLAGS                     /
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                /                   SERVICES                    /
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                /                    REGEXP                     /
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
                /                  REPLACEMENT                  /
                /                                               /
                +--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
            */

            int order = reply[offset++] << 8 | reply[offset++];

            int preference = reply[offset++] << 8 | reply[offset++];

            string flags = Dns_Client.ReadCharacterString(reply,ref offset);

            string services = Dns_Client.ReadCharacterString(reply,ref offset);

            string regexp = Dns_Client.ReadCharacterString(reply,ref offset);
            
            string replacement = "";
            Dns_Client.GetQName(reply,ref offset,ref replacement);

            return new DNS_rr_NAPTR(name,order,preference,flags,services,regexp,replacement,ttl);
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets order in which the NAPTR records MUST be processed in order to accurately 
        /// represent the ordered list of Rules.
        /// </summary>
        public int Order
        {
            get{ return m_Order; }
        }

        /// <summary>
        /// Gets the order in which NAPTR records with equal Order values SHOULD be processed, 
        /// low numbers being processed before high numbers.
        /// </summary>
        public int Preference
        {
            get{ return m_Preference; }
        }

        /// <summary>
        /// Gets flags which control the rewriting and interpretation of the fields in the record.
        /// </summary>
        public string Flags
        {
            get{ return m_Flags; }
        }

        /// <summary>
        /// Gets services related to this record. Known values can be get from: http://www.iana.org/assignments/enum-services.
        /// </summary>
        public string Services
        {
            get{ return m_Services; }
        }

        /// <summary>
        /// Gets regular expression that is applied to the original string held by the client in order to 
        /// construct the next domain name to lookup.
        /// </summary>
        public string Regexp
        {
            get{ return m_Regexp; }
        }

        /// <summary>
        /// Gets regular expressions replacement value.
        /// </summary>
        public string Replacement
        {
            get{ return m_Replacement; }
        }

        #endregion

    }
}
