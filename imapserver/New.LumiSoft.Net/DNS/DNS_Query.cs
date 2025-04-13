using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.DNS
{
    /// <summary>
    /// This class represent DSN server query. Defined in RFC 1035.
    /// </summary>
    public class DNS_Query
    {
        private DNS_QClass m_QClass = DNS_QClass.IN;
        private DNS_QType  m_QType  = DNS_QType.ANY;
        private string     m_QName  = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="qtype">Query type.</param>
        /// <param name="qname">Query text. It depends on query type.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>qname</b> is null reference.</exception>
        public DNS_Query(DNS_QType qtype,string qname) : this(DNS_QClass.IN,qtype,qname)
        {
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="qclass">Query class.</param>
        /// <param name="qtype">Query type.</param>
        /// <param name="qname">Query text. It depends on query type.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>qname</b> is null reference.</exception>
        public DNS_Query(DNS_QClass qclass,DNS_QType qtype,string qname)
        {
            if(qname == null){
                throw new ArgumentNullException("qname");
            }

            m_QClass = qclass;
            m_QType  = qtype;
            m_QName  = qname;
        }


        #region Properties implementation
        
        /// <summary>
        /// Gets DNS query class.
        /// </summary>
        public DNS_QClass QueryClass
        {
            get{ return m_QClass; }
        }

        /// <summary>
        /// Gets DNS query type.
        /// </summary>
        public DNS_QType QueryType
        {
            get{ return m_QType; }
        }

        /// <summary>
        /// Gets query text.
        /// </summary>
        public string QueryName
        {
            get{ return m_QName; }
        }
                
        #endregion
    }
}
