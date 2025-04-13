using System;

using LumiSoft.Net.DNS.Client;

namespace LumiSoft.Net.DNS
{
	/// <summary>
	/// NS record class.
	/// </summary>
	[Serializable]
	public class DNS_rr_NS : DNS_rr
	{
		private string m_NameServer = "";

		/// <summary>
		/// Default constructor.
		/// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
		/// <param name="nameServer">Name server name.</param>
		/// <param name="ttl">TTL value.</param>
		public DNS_rr_NS(string name,string nameServer,int ttl) : base(name,DNS_QType.NS,ttl)
		{
			m_NameServer = nameServer;
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
        public static DNS_rr_NS Parse(string name,byte[] reply,ref int offset,int rdLength,int ttl)
        {
            // Name server name

			string server = "";			
			if(Dns_Client.GetQName(reply,ref offset,ref server)){			
				return new DNS_rr_NS(name,server,ttl);
			}
            else{
                throw new ArgumentException("Invalid NS resource record data !");
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
		/// Gets name server name.
		/// </summary>
		public string NameServer
		{
			get{ return m_NameServer; }
		}

		#endregion

	}
}
