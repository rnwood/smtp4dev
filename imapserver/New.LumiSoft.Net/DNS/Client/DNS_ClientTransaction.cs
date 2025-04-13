using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

namespace LumiSoft.Net.DNS.Client
{
    /// <summary>
    /// This class represents DNS client transaction.
    /// </summary>
    public class DNS_ClientTransaction
    {
        private object                     m_pLock         = new object();
        private DNS_ClientTransactionState m_State         = DNS_ClientTransactionState.WaitingForStart;
        private DateTime                   m_CreateTime;
        private Dns_Client                 m_pOwner        = null;
        private int                        m_ID            = 1;
        private string                     m_QName         = "";
        private DNS_QType                  m_QType         = 0;
        private TimerEx                    m_pTimeoutTimer = null;
        private DnsServerResponse          m_pResponse     = null;
        private int                        m_ResponseCount = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="owner">Owner DNS client.</param>
        /// <param name="id">Transaction ID.</param>
        /// <param name="qtype">QTYPE value.</param>
        /// <param name="qname">QNAME value.</param>
        /// <param name="timeout">Timeout in milliseconds.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> or <b>qname</b> is null reference.</exception>
        internal DNS_ClientTransaction(Dns_Client owner,int id,DNS_QType qtype,string qname,int timeout)
        {
            if(owner == null){
                throw new ArgumentNullException("owner");
            }
            if(qname == null){
                throw new ArgumentNullException("qname");
            }

            m_pOwner = owner;
            m_ID     = id;
            m_QName  = qname;
            m_QType  = qtype;
                        
            m_CreateTime    = DateTime.Now;
            m_pTimeoutTimer = new TimerEx(timeout);
            m_pTimeoutTimer.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimeoutTimer_Elapsed);
        }
                        
        #region method Dispose

        /// <summary>
        /// Cleans up any resource being used.
        /// </summary>
        public void Dispose()
        {
            lock(m_pLock){
                if(this.State == DNS_ClientTransactionState.Disposed){
                    return;
                }

                SetState(DNS_ClientTransactionState.Disposed);

                m_pTimeoutTimer.Dispose();
                m_pTimeoutTimer = null;

                m_pOwner = null;

                m_pResponse = null;

                this.StateChanged = null;
                this.Timeout = null;
            }
        }

        #endregion


        #region Events handling

        #region method m_pTimeoutTimer_Elapsed

        /// <summary>
        /// Is called when DNS transaction timeout timer triggers.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pTimeoutTimer_Elapsed(object sender,System.Timers.ElapsedEventArgs e)
        {
            try{
                OnTimeout();
            }
            catch{
                // We don't care about errors here.
            }
            finally{
                Dispose();
            }
        }

        #endregion

        #endregion


        #region method Start

        /// <summary>
        /// Starts DNS transaction processing.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is raised when this method is called in invalid transaction state.</exception>
        public void Start()
        {   
            if(this.State != DNS_ClientTransactionState.WaitingForStart){
                throw new InvalidOperationException("DNS_ClientTransaction.Start may be called only in 'WaitingForStart' transaction state.");
            }

            SetState(DNS_ClientTransactionState.Active);

            // Move processing to thread pool.
            ThreadPool.QueueUserWorkItem(delegate(object state){
                try{
                    // Use DNS cache if allowed.
			        if(Dns_Client.UseDnsCache){ 
	                    DnsServerResponse response = m_pOwner.Cache.GetFromCache(m_QName,(int)m_QType);
				        if(response != null){
					        m_pResponse = response;

                            SetState(DNS_ClientTransactionState.Completed);
                            Dispose();

                            return;
				        }
			        }   

                    byte[] buffer = new byte[1400];
                    int count = CreateQuery(buffer,m_ID,m_QName,m_QType,1);
  
                    // Send parallel query to DNS server(s).
                    foreach(string server in Dns_Client.DnsServers){
                        if(Net_Utils.IsIPAddress(server)){
                            IPAddress ip = IPAddress.Parse(server);
                            m_pOwner.Send(ip,buffer,count);
                        }
                    }

                    m_pTimeoutTimer.Start();
                }
                catch{
                    Dispose();
                }
            });
        }

        #endregion


        #region method ProcessResponse

        /// <summary>
        /// Processes DNS server response through this transaction.
        /// </summary>
        /// <param name="response">DNS server response.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
        internal void ProcessResponse(DnsServerResponse response)
        {
            if(response == null){
                throw new ArgumentNullException("response");
            }
                        
            try{
                lock(m_pLock){
                    if(this.State != DNS_ClientTransactionState.Active){
                        return;
                    }
                    m_ResponseCount++;

                    // Late arriving response or retransmitted response, just skip it.
                    if(m_pResponse != null){
                        return;
                    }
                    // If server refused to complete query and we more active queries to other servers, skip that response.
                    if(response.ResponseCode == DNS_RCode.REFUSED && m_ResponseCount < Dns_Client.DnsServers.Length){
                        return;
                    }

                    m_pResponse = response;

                    SetState(DNS_ClientTransactionState.Completed);
                } 
            }
            finally{
                if(this.State == DNS_ClientTransactionState.Completed){
                    Dispose();
                }                
            }
        }

        #endregion


        #region method SetState

        /// <summary>
        /// Sets transaction state.
        /// </summary>
        /// <param name="state">New transaction state.</param>
        private void SetState(DNS_ClientTransactionState state)
        {
            m_State = state;

            OnStateChanged();
        }

        #endregion

        #region method CreateQuery

		/// <summary>
		/// Creates binary query.
		/// </summary>
        /// <param name="buffer">Buffer where to store query.</param>
		/// <param name="ID">Query ID.</param>
		/// <param name="qname">Query text.</param>
		/// <param name="qtype">Query type.</param>
		/// <param name="qclass">Query class.</param>
		/// <returns>Returns number of bytes stored to <b>buffer</b>.</returns>
		private int CreateQuery(byte[] buffer,int ID,string qname,DNS_QType qtype,int qclass)
		{
			//---- Create header --------------------------------------------//
			// Header is first 12 bytes of query

			/* 4.1.1. Header section format
										  1  1  1  1  1  1
			0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                      ID                       |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|QR|   Opcode  |AA|TC|RD|RA|   Z    |   RCODE   |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                    QDCOUNT                    |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                    ANCOUNT                    |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                    NSCOUNT                    |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                    ARCOUNT                    |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			
			QR  A one bit field that specifies whether this message is a
                query (0), or a response (1).
				
			OPCODE          A four bit field that specifies kind of query in this
                message.  This value is set by the originator of a query
                and copied into the response.  The values are:

                0               a standard query (QUERY)

                1               an inverse query (IQUERY)

                2               a server status request (STATUS)
				
			*/

			//--------- Header part -----------------------------------//
			buffer[0]  = (byte) (ID >> 8); buffer[1]  = (byte) (ID & 0xFF);
			buffer[2]  = (byte) 1;         buffer[3]  = (byte) 0;
			buffer[4]  = (byte) 0;         buffer[5]  = (byte) 1;
			buffer[6]  = (byte) 0;         buffer[7]  = (byte) 0;
			buffer[8]  = (byte) 0;         buffer[9]  = (byte) 0;
			buffer[10] = (byte) 0;         buffer[11] = (byte) 0;
			//---------------------------------------------------------//

			//---- End of header --------------------------------------------//


			//----Create query ------------------------------------//

			/* 	Rfc 1035 4.1.2. Question section format
											  1  1  1  1  1  1
			0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                                               |
			/                     QNAME                     /
			/                                               /
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                     QTYPE                     |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                     QCLASS                    |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			
			QNAME
				a domain name represented as a sequence of labels, where
				each label consists of a length octet followed by that
				number of octets.  The domain name terminates with the
				zero length octet for the null label of the root.  Note
				that this field may be an odd number of octets; no
				padding is used.
			*/

            // Convert unicode domain name. For more info see RFC 5890.
            System.Globalization.IdnMapping ldn = new System.Globalization.IdnMapping();
            qname = ldn.GetAscii(qname);

			string[] labels = qname.Split(new char[] {'.'});
			int position = 12;
					
			// Copy all domain parts(labels) to query
			// eg. lumisoft.ee = 2 labels, lumisoft and ee.
			// format = label.length + label(bytes)
			foreach(string label in labels){
                // convert label string to byte array
                byte[] b = Encoding.ASCII.GetBytes(label);

				// add label lenght to query
				buffer[position++] = (byte)(b.Length);
                b.CopyTo(buffer,position);

				// Move position by label length
				position += b.Length;
			}

			// Terminate domain (see note above)
			buffer[position++] = (byte) 0; 
			
			// Set QTYPE 
			buffer[position++] = (byte) 0;
			buffer[position++] = (byte)qtype;
				
			// Set QCLASS
			buffer[position++] = (byte) 0;
			buffer[position++] = (byte)qclass;
			//-------------------------------------------------------//
			
			return position;
		}

		#endregion


        #region Properties implementaion

        /// <summary>
        /// Get DNS transaction state.
        /// </summary>
        public DNS_ClientTransactionState State
        {
            get{ return m_State; }
        }

        /// <summary>
        /// Gets transaction create time.
        /// </summary>
        public DateTime CreateTime
        {
            get{ return m_CreateTime; }
        }

        /// <summary>
        /// Gets DNS transaction ID.
        /// </summary>
        public int ID
        {
            get{ return m_ID; }
        }

        /// <summary>
        /// Gets QNAME value.
        /// </summary>
        public string QName
        {
            get{ return m_QName; }
        }

        /// <summary>
        /// Gets QTYPE value.
        /// </summary>
        public DNS_QType QType
        {
            get{ return m_QType; }
        }

        /// <summary>
        /// Gets DNS server response. Value null means no response received yet.
        /// </summary>
        public DnsServerResponse Response
        {
            get{ return m_pResponse; }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// This event is raised when DNS transaction state has changed.
        /// </summary>
        public event EventHandler<EventArgs<DNS_ClientTransaction>> StateChanged = null;

        #region method OnStateChanged

        /// <summary>
        /// Raises <b>StateChanged</b> event.
        /// </summary>
        private void OnStateChanged()
        {
            if(this.StateChanged != null){
                this.StateChanged(this,new EventArgs<DNS_ClientTransaction>(this));
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when DNS transaction times out.
        /// </summary>
        public event EventHandler Timeout = null;

        #region method OnTimeout

        /// <summary>
        /// Raises <b>Timeout</b> event.
        /// </summary>
        private void OnTimeout()
        {
           if(this.Timeout != null){
                this.Timeout(this,new EventArgs());
           }
        }

        #endregion

        #endregion
    }
}
