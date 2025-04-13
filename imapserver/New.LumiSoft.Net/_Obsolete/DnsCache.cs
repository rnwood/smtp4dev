using System;
using System.IO;
using System.Collections;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Formatters.Binary;

namespace LumiSoft.Net.DNS.Client
{
	#region struct CacheEntry

	/// <summary>
	/// Dns cache entry.
	/// </summary>
	[Serializable]
	internal struct DnsCacheEntry
	{
		private DnsServerResponse m_pResponse;
		private DateTime          m_Time;

		/// <summary>
		/// Default constructor.
		/// </summary>
		/// <param name="answers">Dns answers.</param>
		/// <param name="addTime">Entry add time.</param>
		public DnsCacheEntry(DnsServerResponse answers,DateTime addTime)
		{
			m_pResponse = answers;
			m_Time      = addTime;
		}

		/// <summary>
		/// Gets dns answers.
		/// </summary>
		public DnsServerResponse Answers
		{
			get{ return m_pResponse; }
		}

		/// <summary>
		/// Gets entry add time.
		/// </summary>
		public DateTime Time
		{
			get{ return m_Time; }
		}
	}

	#endregion

	/// <summary>
	/// This class implements dns query cache.
	/// </summary>
    [Obsolete("Use DNS_Client.Cache instead.")]
	public class DnsCache
	{
		private static Hashtable m_pCache    = null;
		private static long      m_CacheTime = 10000;

		/// <summary>
		/// Default constructor.
		/// </summary>
		static DnsCache()
		{
			m_pCache = new Hashtable();
		}


		#region method GetFromCache

		/// <summary>
		/// Tries to get dns records from cache, if any.
		/// </summary>
		/// <param name="qname"></param>
		/// <param name="qtype"></param>
		/// <returns>Returns null if not in cache.</returns>
		public static DnsServerResponse GetFromCache(string qname,int qtype)
		{
			try{
				if(m_pCache.Contains(qname + qtype)){
					DnsCacheEntry entry = (DnsCacheEntry)m_pCache[qname + qtype];

					// If cache object isn't expired
					if(entry.Time.AddSeconds(m_CacheTime) > DateTime.Now){
						return entry.Answers;
					}
				}
			}
			catch{
			}
			
			return null;
		}

		#endregion

		#region method AddToCache

		/// <summary>
		/// Adds dns records to cache. If old entry exists, it is replaced.
		/// </summary>
		/// <param name="qname"></param>
		/// <param name="qtype"></param>
		/// <param name="answers"></param>
		public static void AddToCache(string qname,int qtype,DnsServerResponse answers)
		{
			if(answers == null){
				return;
			}

			try{
				lock(m_pCache){
					// Remove old cache entry, if any.
					if(m_pCache.Contains(qname + qtype)){
						m_pCache.Remove(qname + qtype);
					}
					m_pCache.Add(qname + qtype,new DnsCacheEntry(answers,DateTime.Now));
				}
			}
			catch{
			}
		}

		#endregion

		#region method ClearCache

		/// <summary>
		/// Clears DNS cache.
		/// </summary>
		public static void ClearCache()
		{
			lock(m_pCache){
				m_pCache.Clear();
			}
		}

		#endregion


		#region method SerializeCache

		/// <summary>
		/// Serializes current cache.
		/// </summary>
		/// <returns>Return serialized cache.</returns>
		public static byte[] SerializeCache()
		{
            lock(m_pCache){
			    MemoryStream retVal = new MemoryStream();

			    BinaryFormatter b = new BinaryFormatter();
			    b.Serialize(retVal,m_pCache);

			    return retVal.ToArray();
            }
		}

		#endregion

		#region method DeSerializeCache

		/// <summary>
		/// DeSerializes stored cache.
		/// </summary>
		/// <param name="cacheData">This value must be DnsCache.SerializeCache() method value.</param>
		public static void DeSerializeCache(byte[] cacheData)
		{
            lock(m_pCache){
			    MemoryStream retVal = new MemoryStream(cacheData);

			    BinaryFormatter b = new BinaryFormatter();
			    m_pCache = (Hashtable)b.Deserialize(retVal);
            }
		}

		#endregion


		#region Properties Implementation

		/// <summary>
		/// Gets or sets how long(seconds) to cache dns query.
		/// </summary>
		public static long CacheTime
		{
			get{ return m_CacheTime; }

			set{ m_CacheTime = value; }
		}

		#endregion

	}
}
