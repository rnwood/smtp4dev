using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.DNS
{
    /// <summary>
	/// This enum holds DNS query type. Defined in RFC 1035.
	/// </summary>
	public enum DNS_QType
	{
		/// <summary>
		/// IPv4 host address
		/// </summary>
		A = 1,

		/// <summary>
		/// An authoritative name server.
		/// </summary>
		NS    = 2,  

	//	MD    = 3,  Obsolete
	//	MF    = 4,  Obsolete

		/// <summary>
		/// The canonical name for an alias.
		/// </summary>
		CNAME = 5,  

		/// <summary>
		/// Marks the start of a zone of authority.
		/// </summary>
		SOA   = 6,  

	//	MB    = 7,  EXPERIMENTAL
	//	MG    = 8,  EXPERIMENTAL
	//  MR    = 9,  EXPERIMENTAL
	//	NULL  = 10, EXPERIMENTAL

	/*	/// <summary>
		/// A well known service description.
		/// </summary>
		WKS   = 11, */

		/// <summary>
		/// A domain name pointer.
		/// </summary>
		PTR   = 12, 

		/// <summary>
		/// Host information.
		/// </summary>
		HINFO = 13, 
/*
		/// <summary>
		/// Mailbox or mail list information.
		/// </summary>
		MINFO = 14, */

		/// <summary>
		/// Mail exchange.
		/// </summary>
		MX    = 15, 

		/// <summary>
		/// Text strings.
		/// </summary>
		TXT   = 16, 

		/// <summary>
		/// IPv6 host address.
		/// </summary>
		AAAA = 28,

        /// <summary>
        /// SRV record specifies the location of services.
        /// </summary>
        SRV = 33,

        /// <summary>
        /// NAPTR(Naming Authority Pointer) record.
        /// </summary>
        NAPTR = 35,

        /// <summary>
        /// SPF(Sender Policy Framework) record.
        /// </summary>
        SPF = 99,

        /// <summary>
        /// All records what server returns.
        /// </summary>
        ANY = 255,
	}
}
