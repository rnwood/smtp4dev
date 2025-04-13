using System;

using LumiSoft.Net.DNS.Client;

namespace LumiSoft.Net.DNS
{
	/// <summary>
	/// HINFO record.
	/// </summary>
	public class DNS_rr_HINFO : DNS_rr
	{
		private string m_CPU = "";
		private string m_OS  = "";

		/// <summary>
		/// Default constructor.
		/// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
		/// <param name="cpu">Host CPU.</param>
		/// <param name="os">Host OS.</param>
		/// <param name="ttl">TTL value.</param>
		public DNS_rr_HINFO(string name,string cpu,string os,int ttl) : base(name,DNS_QType.HINFO,ttl)
		{
			m_CPU = cpu;
			m_OS  = os;
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
        public static DNS_rr_HINFO Parse(string name,byte[] reply,ref int offset,int rdLength,int ttl)
        {
            /* RFC 1035 3.3.2. HINFO RDATA format

			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			/                      CPU                      /
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			/                       OS                      /
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			
			CPU     A <character-string> which specifies the CPU type.

			OS      A <character-string> which specifies the operating
					system type.
					
					Standard values for CPU and OS can be found in [RFC-1010].

			*/

			// CPU
			string cpu = Dns_Client.ReadCharacterString(reply,ref offset);

			// OS
			string os = Dns_Client.ReadCharacterString(reply,ref offset);

			return new DNS_rr_HINFO(name,cpu,os,ttl);
        }

        #endregion


        #region Properties Implementation

        /// <summary>
		/// Gets host's CPU.
		/// </summary>
		public string CPU
		{
			get{ return m_CPU; }
		}

		/// <summary>
		/// Gets host's OS.
		/// </summary>
		public string OS
		{
			get{ return m_OS; }
		}
        
		#endregion
	}
}
