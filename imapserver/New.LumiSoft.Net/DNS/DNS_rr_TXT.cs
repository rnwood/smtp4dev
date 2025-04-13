using System;

using LumiSoft.Net.DNS.Client;

namespace LumiSoft.Net.DNS
{
	/// <summary>
	/// TXT record class.
	/// </summary>
	[Serializable]
	public class DNS_rr_TXT : DNS_rr
	{
		private string m_Text = "";

		/// <summary>
		/// Default constructor.
		/// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
		/// <param name="text">Text.</param>
		/// <param name="ttl">TTL value.</param>
		public DNS_rr_TXT(string name,string text,int ttl) : base(name,DNS_QType.TXT,ttl)
		{
			m_Text = text;
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
        public static DNS_rr_TXT Parse(string name,byte[] reply,ref int offset,int rdLength,int ttl)
        {
            // TXT RR

            string text = Dns_Client.ReadCharacterString(reply,ref offset);

			return new DNS_rr_TXT(name,text,ttl);
        }

        #endregion


        #region Properties Implementation

        /// <summary>
		/// Gets text.
		/// </summary>
		public string Text
		{
			get{ return m_Text; }
		}

		#endregion
	}
}
