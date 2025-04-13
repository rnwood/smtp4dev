using System;

namespace LumiSoft.Net.DNS
{
	/// <summary>
	/// This is base class for DNS records.
	/// </summary>
	public abstract class DNS_rr
	{
        private string    m_Name = "";
		private DNS_QType m_Type = DNS_QType.A;
		private int       m_TTL  = -1;

		/// <summary>
		/// Default constructor.
		/// </summary>
        /// <param name="name">DNS domain name that owns a resource record.</param>
		/// <param name="recordType">Record type (A,MX, ...).</param>
		/// <param name="ttl">TTL (time to live) value in seconds.</param>
		public DNS_rr(string name,DNS_QType recordType,int ttl)
		{
            m_Name = name;
			m_Type = recordType;
			m_TTL  = ttl;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets DNS domain name that owns a resource record.
        /// </summary>
        public string Name
        {
            get{ return m_Name; }
        }

        /// <summary>
		/// Gets record type (A,MX,...).
		/// </summary>
		public DNS_QType RecordType
		{
			get{ return m_Type; }
		}

		/// <summary>
		/// Gets TTL (time to live) value in seconds.
		/// </summary>
		public int TTL
		{
			get{ return m_TTL; }
		}

		#endregion
	}
}
