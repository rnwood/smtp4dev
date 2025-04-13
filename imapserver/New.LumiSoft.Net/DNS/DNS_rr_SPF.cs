using System;
using System.Collections.Generic;
using System.Text;

using LumiSoft.Net.DNS.Client;

namespace LumiSoft.Net.DNS
{
    /// <summary>
    /// This class represent SPF resource record. Defined in RFC 4408.
    /// </summary>
	[Serializable]
    public class DNS_rr_SPF : DNS_rr
    {
        private string m_Text = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
        /// <param name="text">SPF text.</param>
		/// <param name="ttl">TTL (time to live) value in seconds.</param>
        public DNS_rr_SPF(string name,string text,int ttl) : base(name,DNS_QType.SPF,ttl)
        {
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
        public static DNS_rr_SPF Parse(string name,byte[] reply,ref int offset,int rdLength,int ttl)
        {
            // SPF RR

            string text = Dns_Client.ReadCharacterString(reply,ref offset);

			return new DNS_rr_SPF(name,text,ttl);
        }

        #endregion


        #region Properties implementation

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
