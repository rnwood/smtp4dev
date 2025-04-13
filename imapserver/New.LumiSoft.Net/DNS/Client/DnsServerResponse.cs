using System;
using System.Collections;
using System.Collections.Generic;

using LumiSoft.Net.DNS;

namespace LumiSoft.Net.DNS.Client
{
	/// <summary>
	/// This class represents dns server response.
	/// </summary>
	[Serializable]
	public class DnsServerResponse
	{
		private bool         m_Success             = true;
        private int          m_ID                  = 0;
		private DNS_RCode    m_RCODE               = DNS_RCode.NO_ERROR;
		private List<DNS_rr> m_pAnswers            = null;
		private List<DNS_rr> m_pAuthoritiveAnswers = null;
		private List<DNS_rr> m_pAdditionalAnswers  = null;
		
		internal DnsServerResponse(bool connectionOk,int id,DNS_RCode rcode,List<DNS_rr> answers,List<DNS_rr> authoritiveAnswers,List<DNS_rr> additionalAnswers)
		{
			m_Success             = connectionOk;
            m_ID                  = id;
			m_RCODE               = rcode;	
			m_pAnswers            = answers;
			m_pAuthoritiveAnswers = authoritiveAnswers;
			m_pAdditionalAnswers  = additionalAnswers;
		}


		#region method GetARecords

		/// <summary>
		/// Gets IPv4 host addess records.
		/// </summary>
		/// <returns></returns>
		public DNS_rr_A[] GetARecords()
		{
            List<DNS_rr_A> retVal = new List<DNS_rr_A>();
            foreach(DNS_rr record in m_pAnswers){
                if(record.RecordType == DNS_QType.A){
                    retVal.Add((DNS_rr_A)record);
                }
            }

			return retVal.ToArray();
		}

		#endregion

		#region method GetNSRecords

		/// <summary>
		/// Gets name server records.
		/// </summary>
		/// <returns></returns>
		public DNS_rr_NS[] GetNSRecords()
		{
            List<DNS_rr_NS> retVal = new List<DNS_rr_NS>();
            foreach(DNS_rr record in m_pAnswers){
                if(record.RecordType == DNS_QType.NS){
                    retVal.Add((DNS_rr_NS)record);
                }
            }

			return retVal.ToArray();
		}

		#endregion

		#region method GetCNAMERecords

		/// <summary>
		/// Gets CNAME records.
		/// </summary>
		/// <returns></returns>
		public DNS_rr_CNAME[] GetCNAMERecords()
		{
            List<DNS_rr_CNAME> retVal = new List<DNS_rr_CNAME>();
            foreach(DNS_rr record in m_pAnswers){
                if(record.RecordType == DNS_QType.CNAME){
                    retVal.Add((DNS_rr_CNAME)record);
                }
            }

			return retVal.ToArray();
		}

		#endregion

		#region method GetSOARecords

		/// <summary>
		/// Gets SOA records.
		/// </summary>
		/// <returns></returns>
		public DNS_rr_SOA[] GetSOARecords()
		{
            List<DNS_rr_SOA> retVal = new List<DNS_rr_SOA>();
            foreach(DNS_rr record in m_pAnswers){
                if(record.RecordType == DNS_QType.SOA){
                    retVal.Add((DNS_rr_SOA)record);
                }
            }

			return retVal.ToArray();
		}

		#endregion

		#region method GetPTRRecords

		/// <summary>
		/// Gets PTR records.
		/// </summary>
		/// <returns></returns>
		public DNS_rr_PTR[] GetPTRRecords()
		{	
            List<DNS_rr_PTR> retVal = new List<DNS_rr_PTR>();
            foreach(DNS_rr record in m_pAnswers){
                if(record.RecordType == DNS_QType.PTR){
                    retVal.Add((DNS_rr_PTR)record);
                }
            }

            return retVal.ToArray();
		}

		#endregion

		#region method GetHINFORecords

		/// <summary>
		/// Gets HINFO records.
		/// </summary>
		/// <returns></returns>
		public DNS_rr_HINFO[] GetHINFORecords()
		{	
            List<DNS_rr_HINFO> retVal = new List<DNS_rr_HINFO>();
            foreach(DNS_rr record in m_pAnswers){
                if(record.RecordType == DNS_QType.HINFO){
                    retVal.Add((DNS_rr_HINFO)record);
                }
            }

            return retVal.ToArray();
		}

		#endregion

		#region method GetMXRecords

		/// <summary>
		/// Gets MX records.(MX records are sorted by preference, lower array element is prefered)
		/// </summary>
		/// <returns></returns>
		public DNS_rr_MX[] GetMXRecords()
		{
            List<DNS_rr_MX> mx = new List<DNS_rr_MX>();
            foreach(DNS_rr record in m_pAnswers){
                if(record.RecordType == DNS_QType.MX){
                    mx.Add((DNS_rr_MX)record);
                }
            }

            // Sort MX records by preference.
            DNS_rr_MX[] retVal = mx.ToArray();
            Array.Sort(retVal);

            return retVal;
		}

		#endregion

		#region method GetTXTRecords

		/// <summary>
		/// Gets text records.
		/// </summary>
		/// <returns></returns>
		public DNS_rr_TXT[] GetTXTRecords()
		{
            List<DNS_rr_TXT> retVal = new List<DNS_rr_TXT>();
            foreach(DNS_rr record in m_pAnswers){
                if(record.RecordType == DNS_QType.TXT){
                    retVal.Add((DNS_rr_TXT)record);
                }
            }

            return retVal.ToArray();
		}

		#endregion

		#region method GetAAAARecords

		/// <summary>
		/// Gets IPv6 host addess records.
		/// </summary>
		/// <returns></returns>
		public DNS_rr_AAAA[] GetAAAARecords()
		{
            List<DNS_rr_AAAA> retVal = new List<DNS_rr_AAAA>();
            foreach(DNS_rr record in m_pAnswers){
                if(record.RecordType == DNS_QType.AAAA){
                    retVal.Add((DNS_rr_AAAA)record);
                }
            }

            return retVal.ToArray();
		}

		#endregion

        #region method GetSRVRecords

		/// <summary>
		/// Gets SRV resource records.
		/// </summary>
		/// <returns></returns>
		public DNS_rr_SRV[] GetSRVRecords()
		{
            List<DNS_rr_SRV> retVal = new List<DNS_rr_SRV>();
            foreach(DNS_rr record in m_pAnswers){
                if(record.RecordType == DNS_QType.SRV){
                    retVal.Add((DNS_rr_SRV)record);
                }
            }

            return retVal.ToArray();
		}

		#endregion

        #region method GetNAPTRRecords

		/// <summary>
		/// Gets NAPTR resource records.
		/// </summary>
		/// <returns></returns>
		public DNS_rr_NAPTR[] GetNAPTRRecords()
		{
            List<DNS_rr_NAPTR> retVal = new List<DNS_rr_NAPTR>();
            foreach(DNS_rr record in m_pAnswers){
                if(record.RecordType == DNS_QType.NAPTR){
                    retVal.Add((DNS_rr_NAPTR)record);
                }
            }

            return retVal.ToArray();
		}

		#endregion

        #region method GetSPFRecords

		/// <summary>
		/// Gets SPF resource records.
		/// </summary>
		/// <returns></returns>
		public DNS_rr_SPF[] GetSPFRecords()
		{
            List<DNS_rr_SPF> retVal = new List<DNS_rr_SPF>();
            foreach(DNS_rr record in m_pAnswers){
                if(record.RecordType == DNS_QType.SPF){
                    retVal.Add((DNS_rr_SPF)record);
                }
            }

            return retVal.ToArray();
		}

		#endregion


		#region method FilterRecords

		/// <summary>
		/// Filters out specified type of records from answer.
		/// </summary>
		/// <param name="answers"></param>
		/// <param name="type"></param>
		/// <returns></returns>
		private List<DNS_rr> FilterRecordsX(List<DNS_rr> answers,DNS_QType type)
		{
            List<DNS_rr> retVal = new List<DNS_rr>();
            foreach(DNS_rr record in answers){
                if(record.RecordType == type){
                    retVal.Add(record);
                }
            }

            return retVal;
		}

		#endregion


		#region Properties Implementation

		/// <summary>
		/// Gets if connection to dns server was successful.
		/// </summary>
		public bool ConnectionOk
		{
			get{ return m_Success; }
		}

        /// <summary>
        /// Gets DNS transaction ID.
        /// </summary>
        public int ID
        {
            get{ return m_ID; }
        }

		/// <summary>
		/// Gets dns server response code.
		/// </summary>
		public DNS_RCode ResponseCode
		{
			get{ return m_RCODE; }
		}

		
		/// <summary>
		/// Gets all resource records returned by server (answer records section + authority records section + additional records section). 
		/// NOTE: Before using this property ensure that ConnectionOk=true and ResponseCode=RCODE.NO_ERROR.
		/// </summary>
		public DNS_rr[] AllAnswers
		{
			get{
                List<DNS_rr> retVal = new List<DNS_rr>();
                retVal.AddRange(m_pAnswers.ToArray());
                retVal.AddRange(m_pAuthoritiveAnswers.ToArray());
                retVal.AddRange(m_pAdditionalAnswers.ToArray());

                return retVal.ToArray();
			}
		}

		/// <summary>
		/// Gets dns server returned answers. NOTE: Before using this property ensure that ConnectionOk=true and ResponseCode=RCODE.NO_ERROR.
		/// </summary>
		/// <code>
		/// // NOTE: DNS server may return diffrent record types even if you query MX.
		/// //       For example you query lumisoft.ee MX and server may response:	
		///	//		 1) MX - mail.lumisoft.ee
		///	//		 2) A  - lumisoft.ee
		///	// 
		///	//       Before casting to right record type, see what type record is !
		///				
		/// 
		/// foreach(DnsRecordBase record in Answers){
		///		// MX record, cast it to MX_Record
		///		if(record.RecordType == QTYPE.MX){
		///			MX_Record mx = (MX_Record)record;
		///		}
		/// }
		/// </code>
		public DNS_rr[] Answers
		{
			get{ return m_pAnswers.ToArray(); }
		}

		/// <summary>
		/// Gets name server resource records in the authority records section. NOTE: Before using this property ensure that ConnectionOk=true and ResponseCode=RCODE.NO_ERROR.
		/// </summary>
		public DNS_rr[] AuthoritiveAnswers
		{
			get{ return m_pAuthoritiveAnswers.ToArray(); }
		}

		/// <summary>
		/// Gets resource records in the additional records section. NOTE: Before using this property ensure that ConnectionOk=true and ResponseCode=RCODE.NO_ERROR.
		/// </summary>
		public DNS_rr[] AdditionalAnswers
		{
			get{ return m_pAdditionalAnswers.ToArray(); }
		}

		#endregion
	}
}
