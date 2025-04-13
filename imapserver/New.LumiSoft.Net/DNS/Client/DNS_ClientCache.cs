using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.DNS.Client
{
    /// <summary>
    /// This class implements DNS client cache.
    /// </summary>
    public class DNS_ClientCache
    {
        #region class CacheEntry

        /// <summary>
        /// This class represents DNS cache entry.
        /// </summary>
        private class CacheEntry
        {
            private DnsServerResponse m_pResponse = null;
            private DateTime          m_Expires;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="response">DNS server response.</param>
            /// <param name="expires">Time when cache entry expires.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
            public CacheEntry(DnsServerResponse response,DateTime expires)
            {
                if(response == null){
                    throw new ArgumentNullException("response");
                }

                m_pResponse = response;
                m_Expires   = expires;
            }


            #region Properties implementation

            /// <summary>
            /// Gets DNS server response.
            /// </summary>
            public DnsServerResponse Response
            {
                get{ return m_pResponse; }
            }

            /// <summary>
            /// Gets time when cache entry expires.
            /// </summary>
            public DateTime Expires
            {
                get{ return m_Expires; }
            }

            #endregion
        }

        #endregion

        private Dictionary<string,CacheEntry> m_pCache              = null;
        private int                           m_MaxCacheTtl         = 86400;
        private int                           m_MaxNegativeCacheTtl = 900;
        private TimerEx                       m_pTimerTimeout       = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal DNS_ClientCache()
        {
            m_pCache = new Dictionary<string,CacheEntry>();

            m_pTimerTimeout = new TimerEx(60000);
            m_pTimerTimeout.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimerTimeout_Elapsed);
            m_pTimerTimeout.Start();
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        internal void Dispose()
        {
            m_pCache = null;

            m_pTimerTimeout.Dispose();
            m_pTimerTimeout = null;
        }

        #endregion


        #region Events handling

        #region method m_pTimerTimeout_Elapsed

        /// <summary>
        /// Is called when cache expired entries check timer triggers.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pTimerTimeout_Elapsed(object sender,System.Timers.ElapsedEventArgs e)
        {
            lock(m_pCache){
                // Copy entries to new array.
                List<KeyValuePair<string,CacheEntry>> values = new List<KeyValuePair<string,CacheEntry>>();
                foreach(KeyValuePair<string,CacheEntry> entry in m_pCache){
                    values.Add(entry);
                }

                // Remove expired cache entries.
                foreach(KeyValuePair<string,CacheEntry> entry in values){
                    if(DateTime.Now > entry.Value.Expires){
                        m_pCache.Remove(entry.Key);
                    }
                }
            }
        }

        #endregion

        #endregion


        #region method GetFromCache

        /// <summary>
		/// Gets DNS server cached response or null if no cached result.
		/// </summary>
		/// <param name="qname">Query name.</param>
		/// <param name="qtype">Query type.</param>
		/// <returns>Returns DNS server cached response or null if no cached result.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>qname</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
		public DnsServerResponse GetFromCache(string qname,int qtype)
		{
            if(qname == null){
                throw new ArgumentNullException("qname");
            }
            if(qname == string.Empty){
                throw new ArgumentException("Argument 'qname' value must be specified.","qname");
            }

            CacheEntry entry = null;
            if(m_pCache.TryGetValue(qname + qtype,out entry)){
                // Cache entry has expired.
                if(DateTime.Now > entry.Expires){
                    return null;
                }
                else{
                    return entry.Response;
                }
            }
            else{
                return null;
            }
		}

		#endregion

        #region method AddToCache

		/// <summary>
		/// Adds dns records to cache. If old entry exists, it is replaced.
		/// </summary>
		/// <param name="qname">Query name.</param>
		/// <param name="qtype">Query type.</param>
		/// <param name="response">DNS server response.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>qname</b> or <b>response</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
		public void AddToCache(string qname,int qtype,DnsServerResponse response)
		{
            if(qname == null){
                throw new ArgumentNullException("qname");
            }
            if(qname == string.Empty){
                throw new ArgumentException("Argument 'qname' value must be specified.","qname");
            }
			if(response == null){
				throw new ArgumentNullException("response");
			}

	        lock(m_pCache){
			    // Remove old cache entry, if any.
				if(m_pCache.ContainsKey(qname + qtype)){
				    m_pCache.Remove(qname + qtype);
			    }

                if(response.ResponseCode == DNS_RCode.NO_ERROR){
                    int ttl = m_MaxCacheTtl;
                    // Search smallest DNS record TTL and use it.
                    foreach(DNS_rr rr in response.AllAnswers){
                        if(rr.TTL < ttl){
                            ttl = rr.TTL;
                        }
                    }

                    m_pCache.Add(qname + qtype,new CacheEntry(response,DateTime.Now.AddSeconds(ttl)));
                }
                else{
                    m_pCache.Add(qname + qtype,new CacheEntry(response,DateTime.Now.AddSeconds(m_MaxNegativeCacheTtl)));
                }
			}
		}

		#endregion

        #region method ClearCache

		/// <summary>
		/// Clears DNS cache.
		/// </summary>
		public void ClearCache()
		{
			lock(m_pCache){
				m_pCache.Clear();
			}
		}

		#endregion


        #region Properties implementation

        /// <summary>
        /// Gets or sets maximum number of seconds to cache positive DNS responses.
        /// </summary>
        public int MaxCacheTtl
        {
            get{ return m_MaxCacheTtl; }

            set{ m_MaxCacheTtl = value; }
        }

        /// <summary>
        /// Gets or sets maximum number of seconds to cache negative DNS responses.
        /// </summary>
        public int MaxNegativeCacheTtl
        {
            get{ return m_MaxNegativeCacheTtl; }

            set{ m_MaxNegativeCacheTtl = value; }
        }

        /// <summary>
        /// Gets number of DNS queries cached.
        /// </summary>
        public int Count
        {
            get{ return m_pCache.Count; }
        }

        #endregion
    }
}
