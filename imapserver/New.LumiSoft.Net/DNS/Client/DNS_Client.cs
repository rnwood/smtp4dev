using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Net.NetworkInformation;
using System.Threading;

using LumiSoft.Net;
using LumiSoft.Net.UDP;

namespace LumiSoft.Net.DNS.Client
{
	/// <summary>
	/// DNS client.
	/// </summary>
	/// <example>
	/// <code>
	/// // Optionally set dns servers, by default DNS client uses defaultt NIC DNS servers.
	/// Dns_Client.DnsServers = new string[]{"194.126.115.18"};
	/// 
	/// Dns_Client dns = Dns_Client.Static;
	/// 
	/// // Get MX records.
	/// DnsServerResponse resp = dns.Query("lumisoft.ee",QTYPE.MX);
	/// if(resp.ConnectionOk &amp;&amp; resp.ResponseCode == RCODE.NO_ERROR){
	///		MX_Record[] mxRecords = resp.GetMXRecords();
	///		
	///		// Do your stuff
	///	}
	///	else{
	///		// Handle error there, for more exact error info see RCODE 
	///	}	 
	/// 
	/// </code>
	/// </example>
	public class Dns_Client : IDisposable
    {        
        private static Dns_Client  m_pDnsClient  = null;
        private static IPAddress[] m_DnsServers  = null;
		private static bool        m_UseDnsCache = true;
        // 
        private bool                                  m_IsDisposed    = false;
        private Dictionary<int,DNS_ClientTransaction> m_pTransactions = null;
        private Socket                                m_pIPv4Socket   = null;
        private Socket                                m_pIPv6Socket   = null;
        private List<UDP_DataReceiver>                m_pReceivers    = null;
        private Random                                m_pRandom       = null;
        private DNS_ClientCache                       m_pCache        = null;

		/// <summary>
		/// Static constructor.
		/// </summary>
		static Dns_Client()
		{
			// Try to get default NIC dns servers.
			try{
				List<IPAddress> dnsServers = new List<IPAddress>();
                foreach(NetworkInterface nic in NetworkInterface.GetAllNetworkInterfaces()){
                    if(nic.OperationalStatus == OperationalStatus.Up){
                        foreach(IPAddress ip in nic.GetIPProperties().DnsAddresses){
                            if(ip.AddressFamily == AddressFamily.InterNetwork){
                                if(!dnsServers.Contains(ip)){
                                    dnsServers.Add(ip);
                                }
                            }
                        }

                        break;
                    }
                }

                m_DnsServers = dnsServers.ToArray();
			}
			catch{
			}
		}

		/// <summary>
		/// Default constructor.
		/// </summary>
		public Dns_Client()
		{
            m_pTransactions = new Dictionary<int,DNS_ClientTransaction>();

            m_pIPv4Socket = new Socket(AddressFamily.InterNetwork,SocketType.Dgram,ProtocolType.Udp);
            m_pIPv4Socket.Bind(new IPEndPoint(IPAddress.Any,0));

            if(Socket.OSSupportsIPv6){
                m_pIPv6Socket = new Socket(AddressFamily.InterNetworkV6,SocketType.Dgram,ProtocolType.Udp);
                m_pIPv6Socket.Bind(new IPEndPoint(IPAddress.IPv6Any,0));
            }

            m_pReceivers = new List<UDP_DataReceiver>();
            m_pRandom = new Random();
            m_pCache = new DNS_ClientCache();

            // Create UDP data receivers.
            for(int i=0;i<5;i++){
                UDP_DataReceiver ipv4Receiver = new UDP_DataReceiver(m_pIPv4Socket);
                ipv4Receiver.PacketReceived += delegate(object s1,UDP_e_PacketReceived e1){
                    ProcessUdpPacket(e1);
                };
                m_pReceivers.Add(ipv4Receiver);
                ipv4Receiver.Start();

                if(m_pIPv6Socket != null){
                    UDP_DataReceiver ipv6Receiver = new UDP_DataReceiver(m_pIPv6Socket);
                    ipv6Receiver.PacketReceived += delegate(object s1,UDP_e_PacketReceived e1){
                        ProcessUdpPacket(e1);
                    };
                    m_pReceivers.Add(ipv6Receiver);
                    ipv6Receiver.Start();
                }
            }
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public void Dispose()
        {
            if(m_IsDisposed){
                return;
            }
            m_IsDisposed = true;

            if(m_pReceivers != null){
                foreach(UDP_DataReceiver receiver in m_pReceivers){
                    receiver.Dispose();
                }
                m_pReceivers = null;
            }

            m_pIPv4Socket.Close();
            m_pIPv4Socket = null;

            if(m_pIPv6Socket != null){
                m_pIPv6Socket.Close();
                m_pIPv6Socket = null;
            }

            m_pTransactions = null;
            
            m_pRandom = null;

            m_pCache.Dispose();
            m_pCache = null;
        }

        #endregion


        #region method CreateTransaction

        /// <summary>
        /// Creates new DNS client transaction.
        /// </summary>
        /// <param name="queryType">Query type.</param>
        /// <param name="queryText">Query text. It depends on queryType.</param>
        /// <param name="timeout">Transaction timeout in milliseconds. DNS default value is 2000, value 0 means no timeout - this is not suggested.</param>
        /// <returns>Returns DNS client transaction.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>queryText</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <remarks>Creates asynchronous(non-blocking) DNS transaction. Call <see cref="DNS_ClientTransaction.Start"/> to start transaction.
        /// It is allowd to create multiple conccurent transactions.</remarks>
        public DNS_ClientTransaction CreateTransaction(DNS_QType queryType,string queryText,int timeout)
        {   
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(queryText == null){
                throw new ArgumentNullException("queryText");
            }
            if(queryText == string.Empty){
                throw new ArgumentException("Argument 'queryText' value may not be \"\".","queryText");
            }
            if(queryType == DNS_QType.PTR){
                IPAddress ip = null;
                if(!IPAddress.TryParse(queryText,out ip)){
                    throw new ArgumentException("Argument 'queryText' value must be IP address if queryType == DNS_QType.PTR.","queryText");
                }
            }

            if(queryType == DNS_QType.PTR){
				string ip = queryText;

				// See if IP is ok.
				IPAddress ipA = IPAddress.Parse(ip);		
				queryText = "";

				// IPv6
				if(ipA.AddressFamily == AddressFamily.InterNetworkV6){
					// 4321:0:1:2:3:4:567:89ab
					// would be
					// b.a.9.8.7.6.5.0.4.0.0.0.3.0.0.0.2.0.0.0.1.0.0.0.0.0.0.0.1.2.3.4.IP6.ARPA
					
					char[] ipChars = ip.Replace(":","").ToCharArray();
					for(int i=ipChars.Length - 1;i>-1;i--){
						queryText += ipChars[i] + ".";
					}
					queryText += "IP6.ARPA";
				}
				// IPv4
				else{
					// 213.35.221.186
					// would be
					// 186.221.35.213.in-addr.arpa

					string[] ipParts = ip.Split('.');
					//--- Reverse IP ----------
					for(int i=3;i>-1;i--){
						queryText += ipParts[i] + ".";
					}
					queryText += "in-addr.arpa";
				}
			}

            // Create transaction ID.
            int transactionID = 0;
            lock(m_pTransactions){
                while(true){
                    transactionID = m_pRandom.Next(0xFFFF);

                    // We got not used transaction ID.
                    if(!m_pTransactions.ContainsKey(transactionID)){
                        break;
                    }
                }
            }

            DNS_ClientTransaction retVal = new DNS_ClientTransaction(this,transactionID,queryType,queryText,timeout);
            retVal.StateChanged += delegate(object s1,EventArgs<DNS_ClientTransaction> e1){
                if(retVal.State == DNS_ClientTransactionState.Disposed){
                    lock(m_pTransactions){
                        m_pTransactions.Remove(e1.Value.ID);
                    }
                }
            };
            lock(m_pTransactions){
                m_pTransactions.Add(retVal.ID,retVal);
            }

            return retVal;
        }

        #endregion

        #region method Query

        /// <summary>
		/// Queries server with specified query.
		/// </summary>
		/// <param name="queryText">Query text. It depends on queryType.</param>
		/// <param name="queryType">Query type.</param>
		/// <returns>Returns DSN server response.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>queryText</b> is null.</exception>
		public DnsServerResponse Query(string queryText,DNS_QType queryType)
		{
            return Query(queryText,queryType,2000);
        }

		/// <summary>
		/// Queries server with specified query.
		/// </summary>
		/// <param name="queryText">Query text. It depends on queryType.</param>
		/// <param name="queryType">Query type.</param>
        /// <param name="timeout">Query timeout in milli seconds.</param>
		/// <returns>Returns DSN server response.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>queryText</b> is null.</exception>
		public DnsServerResponse Query(string queryText,DNS_QType queryType,int timeout)
		{
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(queryText == null){
                throw new ArgumentNullException("queryText");
            }

            DnsServerResponse retVal = null;
            ManualResetEvent  wait   = new ManualResetEvent(false);

            DNS_ClientTransaction transaction = CreateTransaction(queryType,queryText,timeout);            
            transaction.Timeout += delegate(object s,EventArgs e){
                wait.Set();
            };
            transaction.StateChanged += delegate(object s1,EventArgs<DNS_ClientTransaction> e1){
                if(transaction.State == DNS_ClientTransactionState.Completed || transaction.State == DNS_ClientTransactionState.Disposed){ 
                    retVal = transaction.Response;

                    wait.Set();
                }
            };
            transaction.Start();

            // Wait transaction to complete.
            wait.WaitOne();
            wait.Close();

            return retVal;
		}

		#endregion
        
        #region method GetHostAddresses

        /// <summary>
        /// Gets host IPv4 and IPv6 addresses.
        /// </summary>
        /// <param name="hostNameOrIP">Host name or IP address.</param>
        /// <returns>Returns host IPv4 and IPv6 addresses.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>hostNameOrIP</b> is null reference.</exception>
        /// <exception cref="DNS_ClientException">Is raised when DNS server returns error.</exception>
        /// <exception cref="IOException">Is raised when IO reletaed error happens.</exception>
        public IPAddress[] GetHostAddresses(string hostNameOrIP)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(hostNameOrIP == null){
                throw new ArgumentNullException("hostNameOrIP");
            }

            ManualResetEvent wait = new ManualResetEvent(false);
            using(Dns_Client.GetHostAddressesAsyncOP op = new Dns_Client.GetHostAddressesAsyncOP(hostNameOrIP)){
                op.CompletedAsync += delegate(object s1,EventArgs<Dns_Client.GetHostAddressesAsyncOP> e1){
                    wait.Set();
                };
                if(!this.GetHostAddressesAsync(op)){
                    wait.Set();
                }
                wait.WaitOne();
                wait.Close();

                if(op.Error != null){
                    throw op.Error;
                }
                else{
                    return op.Addresses;
                }
            }
        }

        #endregion

        #region method GetHostAddressesAsync

        #region class GetHostAddressesAsyncOP

        /// <summary>
        /// This class represents <see cref="Dns_Client.GetHostAddressesAsync"/> asynchronous operation.
        /// </summary>
        public class GetHostAddressesAsyncOP : IDisposable,IAsyncOP
        {
            private object          m_pLock          = new object();
            private AsyncOP_State   m_State          = AsyncOP_State.WaitingForStart;
            private Exception       m_pException     = null;
            private string          m_HostNameOrIP   = null;
            private List<IPAddress> m_pIPv4Addresses = null;
            private List<IPAddress> m_pIPv6Addresses = null;
            private int             m_Counter        = 0;
            private bool            m_RiseCompleted  = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="hostNameOrIP">Host name or IP address.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>hostNameOrIP</b> is null reference.</exception>
            public GetHostAddressesAsyncOP(string hostNameOrIP)
            {
                if(hostNameOrIP == null){
                    throw new ArgumentNullException("hostNameOrIP");
                }

                m_HostNameOrIP = hostNameOrIP;

                m_pIPv4Addresses = new List<IPAddress>();
                m_pIPv6Addresses = new List<IPAddress>();
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resource being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);

                m_pException     = null;
                m_HostNameOrIP   = null;
                m_pIPv4Addresses = null;
                m_pIPv6Addresses = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="dnsClient">DNS client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>dnsClient</b> is null reference.</exception>
            internal bool Start(Dns_Client dnsClient)
            {                
                if(dnsClient == null){
                    throw new ArgumentNullException("dnsClient");
                }

                SetState(AsyncOP_State.Active);

                // Argument 'hostNameOrIP' is IP address.
                if(Net_Utils.IsIPAddress(m_HostNameOrIP)){
                    m_pIPv4Addresses.Add(IPAddress.Parse(m_HostNameOrIP));

                    SetState(AsyncOP_State.Completed);
                }
                // This is probably NetBios name.
			    if(m_HostNameOrIP.IndexOf(".") == -1){
                    try{
                        // This callback is called when BeginGetHostAddresses method has completed.
                        AsyncCallback callback = delegate(IAsyncResult ar){
                            try{
                                foreach(IPAddress ip in  System.Net.Dns.EndGetHostAddresses(ar)){
                                    if(ip.AddressFamily == AddressFamily.InterNetwork){
                                        m_pIPv4Addresses.Add(ip);
                                    }
                                    else{
                                        m_pIPv6Addresses.Add(ip);
                                    }
                                }
                            }
                            catch(Exception x){
                                m_pException = x;
                            }

                            SetState(AsyncOP_State.Completed);
                        };

                        // Start resolving host ip addresses.
                        System.Net.Dns.BeginGetHostAddresses(m_HostNameOrIP,callback,null); 
                    }
                    catch(Exception x){
                        m_pException = x;
                    }
			    }
                // Query A/AAAA records.
                else{
                    #region A records transaction

                    DNS_ClientTransaction transaction_A = dnsClient.CreateTransaction(DNS_QType.A,m_HostNameOrIP,2000);
                    transaction_A.StateChanged += delegate(object s1,EventArgs<DNS_ClientTransaction> e1){ 
                        if(e1.Value.State == DNS_ClientTransactionState.Completed){
                            lock(m_pLock){ 
                                if(e1.Value.Response.ResponseCode != DNS_RCode.NO_ERROR){
                                    m_pException = new DNS_ClientException(e1.Value.Response.ResponseCode);
                                }
                                else{
                                    foreach(DNS_rr_A record in e1.Value.Response.GetARecords()){
                                        m_pIPv4Addresses.Add(record.IP);
                                    }
                                }

                                m_Counter++;

                                // Both A and AAAA transactions are completed, we are done.
                                if(m_Counter == 2){
                                    SetState(AsyncOP_State.Completed);
                                }
                            }
                        }
                    };
                    transaction_A.Timeout += delegate(object s1,EventArgs e1){
                        lock(m_pLock){
                            m_pException = new IOException("DNS transaction timeout, no response from DNS server.");
                            m_Counter++;

                            // Both A and AAAA transactions are completed, we are done.
                            if(m_Counter == 2){
                                SetState(AsyncOP_State.Completed);
                            }
                        }
                    };
                    transaction_A.Start();
                    
                    #endregion

                    #region AAAA records transaction

                    DNS_ClientTransaction transaction_AAAA = dnsClient.CreateTransaction(DNS_QType.AAAA,m_HostNameOrIP,2000);
                    transaction_AAAA.StateChanged += delegate(object s1,EventArgs<DNS_ClientTransaction> e1){
                        if(e1.Value.State == DNS_ClientTransactionState.Completed){
                            lock(m_pLock){
                                if(e1.Value.Response.ResponseCode != DNS_RCode.NO_ERROR){
                                    m_pException = new DNS_ClientException(e1.Value.Response.ResponseCode);
                                }
                                else{
                                    foreach(DNS_rr_AAAA record in e1.Value.Response.GetAAAARecords()){
                                        m_pIPv6Addresses.Add(record.IP);
                                    }
                                }

                                m_Counter++;

                                // Both A and AAAA transactions are completed, we are done.
                                if(m_Counter == 2){
                                    SetState(AsyncOP_State.Completed);
                                }
                            }
                        }
                    };
                    transaction_AAAA.Timeout += delegate(object s1,EventArgs e1){
                        lock(m_pLock){
                            m_pException = new IOException("DNS transaction timeout, no response from DNS server.");
                            m_Counter++;

                            // Both A and AAAA transactions are completed, we are done.
                            if(m_Counter == 2){
                                SetState(AsyncOP_State.Completed);
                            }
                        }
                    };
                    transaction_AAAA.Start();

                    #endregion
                }

                // Set flag rise CompletedAsync event flag. The event is raised when async op completes.
                // If already completed sync, that flag has no effect.
                lock(m_pLock){
                    m_RiseCompleted = true;

                    return m_State == AsyncOP_State.Active;
                }
            }

            #endregion


            #region method SetState

            /// <summary>
            /// Sets operation state.
            /// </summary>
            /// <param name="state">New state.</param>
            private void SetState(AsyncOP_State state)
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }

                lock(m_pLock){
                    m_State = state;

                    if(m_State == AsyncOP_State.Completed && m_RiseCompleted){
                        OnCompletedAsync();
                    }
                }
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets asynchronous operation state.
            /// </summary>
            public AsyncOP_State State
            {
                get{ return m_State; }
            }

            /// <summary>
            /// Gets error happened during operation. Returns null if no error.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public Exception Error
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Error' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pException; 
                }
            }

            /// <summary>
            /// Gets argument <b>hostNameOrIP</b> value.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            public string HostNameOrIP
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_HostNameOrIP; 
                }
            }

            /// <summary>
            /// Gets host IP addresses.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IPAddress[] Addresses
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Addresses' is accessible only in 'AsyncOP_State.Completed' state.");
                    }
                    if(m_pException != null){
                        throw m_pException;
                    }

                    // We list IPv4 addresses before IPv6.
                    List<IPAddress> retVal = new List<IPAddress>();
                    retVal.AddRange(m_pIPv4Addresses);
                    retVal.AddRange(m_pIPv6Addresses);

                    return retVal.ToArray(); 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<GetHostAddressesAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<GetHostAddressesAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts resolving host IPv4 and IPv6 addresses.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="GetHostAddressesAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool GetHostAddressesAsync(GetHostAddressesAsyncOP op)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(op == null){
                throw new ArgumentNullException("op");
            }
            if(op.State != AsyncOP_State.WaitingForStart){
                throw new ArgumentException("Invalid argument 'op' state, 'op' must be in 'AsyncOP_State.WaitingForStart' state.","op");
            }

            return op.Start(this);
        }

        #endregion

        #region method GetHostsAddresses

        /// <summary>
        /// Resolving multiple host IPv4 and IPv6 addresses.
        /// </summary>
        /// <param name="hostNames">Host names to resolve.</param>
        /// <returns>Returns host entries.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>hostNames</b> is null reference.</exception>
        /// <exception cref="DNS_ClientException">Is raised when DNS server returns error.</exception>
        /// <exception cref="IOException">Is raised when IO reletaed error happens.</exception>
        public HostEntry[] GetHostsAddresses(string[] hostNames)
        {
            return GetHostsAddresses(hostNames,false);
        }

        /// <summary>
        /// Resolving multiple host IPv4 and IPv6 addresses.
        /// </summary>
        /// <param name="hostNames">Host names to resolve.</param>
        /// <param name="resolveAny">If true, as long as one host name is resolved, no error returned.</param>
        /// <returns>Returns host entries.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>hostNames</b> is null reference.</exception>
        /// <exception cref="DNS_ClientException">Is raised when DNS server returns error.</exception>
        /// <exception cref="IOException">Is raised when IO reletaed error happens.</exception>
        public HostEntry[] GetHostsAddresses(string[] hostNames,bool resolveAny)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(hostNames == null){
                throw new ArgumentNullException("hostNames");
            }

            ManualResetEvent wait = new ManualResetEvent(false);
            using(Dns_Client.GetHostsAddressesAsyncOP op = new Dns_Client.GetHostsAddressesAsyncOP(hostNames,resolveAny)){
                op.CompletedAsync += delegate(object s1,EventArgs<Dns_Client.GetHostsAddressesAsyncOP> e1){
                    wait.Set();
                };
                if(!this.GetHostsAddressesAsync(op)){
                    wait.Set();
                }
                wait.WaitOne();
                wait.Close();

                if(op.Error != null){
                    throw op.Error;
                }
                else{
                    return op.HostEntries;
                }
            }
        }

        #endregion

        #region method GetHostsAddressesAsync

        #region class GetHostsAddressesAsyncOP

        /// <summary>
        /// This class represents <see cref="Dns_Client.GetHostsAddressesAsync"/> asynchronous operation.
        /// </summary>
        public class GetHostsAddressesAsyncOP : IDisposable,IAsyncOP
        {
            private object                                  m_pLock          = new object();
            private AsyncOP_State                           m_State          = AsyncOP_State.WaitingForStart;
            private Exception                               m_pException     = null;
            private string[]                                m_pHostNames     = null;
            private bool                                    m_ResolveAny     = false;
            private Dictionary<int,GetHostAddressesAsyncOP> m_pIpLookupQueue = null;
            private HostEntry[]                             m_pHostEntries   = null;
            private bool                                    m_RiseCompleted  = false;
            private int                                     m_ResolvedCount  = 0;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="hostNames">Host names to resolve.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>hostNames</b> is null reference.</exception>
            public GetHostsAddressesAsyncOP(string[] hostNames) : this(hostNames,false)
            {
            }

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="hostNames">Host names to resolve.</param>
            /// <param name="resolveAny">If true, as long as one host name is resolved, no error returned.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>hostNames</b> is null reference.</exception>
            public GetHostsAddressesAsyncOP(string[] hostNames,bool resolveAny)
            {
                if(hostNames == null){
                    throw new ArgumentNullException("hostNames");
                }

                m_pHostNames = hostNames;
                m_ResolveAny = resolveAny;

                m_pIpLookupQueue = new Dictionary<int,GetHostAddressesAsyncOP>();
            }


            #region method Dispose

            /// <summary>
            /// Cleans up any resource being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);

                m_pException     = null;
                m_pHostNames     = null;
                m_pIpLookupQueue = null;
                m_pHostEntries   = null;
                
                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="dnsClient">DNS client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>dnsClient</b> is null reference.</exception>
            internal bool Start(Dns_Client dnsClient)
            {   
                if(dnsClient == null){
                    throw new ArgumentNullException("dnsClient");
                }

                SetState(AsyncOP_State.Active);

                m_pHostEntries = new HostEntry[m_pHostNames.Length];

                // Create look up operations for hosts. The "opList" copy array is needed because
                // when we start asyn OP, m_pIpLookupQueue may be altered when OP completes.
                Dictionary<int,GetHostAddressesAsyncOP> opList = new Dictionary<int,GetHostAddressesAsyncOP>();
                for(int i=0;i<m_pHostNames.Length;i++){
                    GetHostAddressesAsyncOP op = new GetHostAddressesAsyncOP(m_pHostNames[i]);
                    m_pIpLookupQueue.Add(i,op);
                    opList.Add(i,op);
                }

                // Start operations.
                foreach(KeyValuePair<int,GetHostAddressesAsyncOP> entry in opList){
                    // NOTE: We may not access "entry" in CompletedAsync, because next for loop reassigns this value.
                    int index = entry.Key;

                    // This event is raised when GetHostAddressesAsync completes asynchronously.
                    entry.Value.CompletedAsync += delegate(object s1,EventArgs<GetHostAddressesAsyncOP> e1){                        
                        GetHostAddressesCompleted(e1.Value,index);
                    };                    
                    // GetHostAddressesAsync completes synchronously.
                    if(!dnsClient.GetHostAddressesAsync(entry.Value)){
                        GetHostAddressesCompleted(entry.Value,index);
                    }
                }

                // Set flag rise CompletedAsync event flag. The event is raised when async op completes.
                // If already completed sync, that flag has no effect.
                lock(m_pLock){
                    m_RiseCompleted = true;

                    return m_State == AsyncOP_State.Active;
                }
            }

            #endregion


            #region method SetState

            /// <summary>
            /// Sets operation state.
            /// </summary>
            /// <param name="state">New state.</param>
            private void SetState(AsyncOP_State state)
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }

                lock(m_pLock){
                    m_State = state;

                    if(m_State == AsyncOP_State.Completed && m_RiseCompleted){
                        OnCompletedAsync();
                    }
                }
            }

            #endregion

            #region method GetHostAddressesCompleted

            /// <summary>
            /// This method is called when GetHostAddresses operation has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <param name="index">Index in 'm_pHostEntries' where to store lookup result.</param>
            private void GetHostAddressesCompleted(GetHostAddressesAsyncOP op,int index)
            {
                lock(m_pLock){
                    try{
                        if(op.Error != null){
                            // We wanted any of the host names to resolve:
                            //  *) We have already one resolved host name.
                            //  *) We have more names to resolve, so next may succeed.
                            if(m_ResolveAny && (m_ResolvedCount > 0 || m_pIpLookupQueue.Count > 1)){
                            }
                            else{
                                m_pException = op.Error;
                            }
                        }
                        else{
                            m_pHostEntries[index] = new HostEntry(op.HostNameOrIP,op.Addresses,null);
                            m_ResolvedCount++;
                        }

                        m_pIpLookupQueue.Remove(index);
                        if(m_pIpLookupQueue.Count == 0){
                            // We wanted resolve any, so some host names may not be resolved and are null, remove them from response.
                            if(m_ResolveAny){
                                List<HostEntry> retVal = new List<HostEntry>();
                                foreach(HostEntry host in m_pHostEntries){
                                    if(host != null){
                                        retVal.Add(host);
                                    }
                                }

                                m_pHostEntries = retVal.ToArray();
                            }

                            SetState(AsyncOP_State.Completed);
                        }
                    }
                    catch(Exception x){
                        m_pException = x;

                        SetState(AsyncOP_State.Completed);
                    }
                }

                op.Dispose();
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets asynchronous operation state.
            /// </summary>
            public AsyncOP_State State
            {
                get{ return m_State; }
            }

            /// <summary>
            /// Gets error happened during operation. Returns null if no error.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public Exception Error
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Error' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pException; 
                }
            }
                        
            /// <summary>
            /// Gets argument <b>hostNames</b> value.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            public string[] HostNames
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_pHostNames; 
                }
            }

            /// <summary>
            /// Gets host entries.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public HostEntry[] HostEntries
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'HostEntries' is accessible only in 'AsyncOP_State.Completed' state.");
                    }
                    if(m_pException != null){
                        throw m_pException;
                    }

                    return m_pHostEntries;                    
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<GetHostsAddressesAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<GetHostsAddressesAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion
        
        /// <summary>
        /// Starts resolving multiple host IPv4 and IPv6 addresses.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="GetHostsAddressesAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool GetHostsAddressesAsync(GetHostsAddressesAsyncOP op)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(op == null){
                throw new ArgumentNullException("op");
            }
            if(op.State != AsyncOP_State.WaitingForStart){
                throw new ArgumentException("Invalid argument 'op' state, 'op' must be in 'AsyncOP_State.WaitingForStart' state.","op");
            }

            return op.Start(this);
        }

        #endregion

        #region method GetEmailHosts

        /// <summary>
        /// Gets email hosts.
        /// </summary>
        /// <param name="domain">Email domain. For example: 'domain.com'.</param>
        /// <returns>Returns email hosts in priority order.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>domain</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="DNS_ClientException">Is raised when DNS server returns error.</exception>
        /// <exception cref="IOException">Is raised when IO reletaed error happens.</exception>
        public HostEntry[] GetEmailHosts(string domain)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(domain == null){
                throw new ArgumentNullException("domain");
            }
            if(domain == string.Empty){
                throw new ArgumentException("Argument 'domain' value must be specified.","domain");
            }

            ManualResetEvent wait = new ManualResetEvent(false);
            using(Dns_Client.GetEmailHostsAsyncOP op = new Dns_Client.GetEmailHostsAsyncOP(domain)){
                op.CompletedAsync += delegate(object s1,EventArgs<Dns_Client.GetEmailHostsAsyncOP> e1){
                    wait.Set();
                };
                if(!this.GetEmailHostsAsync(op)){
                    wait.Set();
                }
                wait.WaitOne();
                wait.Close();

                if(op.Error != null){
                    throw op.Error;
                }
                else{
                    return op.Hosts;
                }
            }
        }

        #endregion

        #region method GetEmailHostsAsync

        #region class GetEmailHostsAsyncOP

        /// <summary>
        /// This class represents <see cref="Dns_Client.GetEmailHostsAsync"/> asynchronous operation.
        /// </summary>
        public class GetEmailHostsAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock         = new object();
            private AsyncOP_State m_State         = AsyncOP_State.WaitingForStart;
            private Exception     m_pException    = null;
            private string        m_Domain        = null;
            private HostEntry[]   m_pHosts        = null;
            private bool          m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="domain">Email domain. For example: 'domain.com'.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>domain</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public GetEmailHostsAsyncOP(string domain)
            {
                if(domain == null){
                    throw new ArgumentNullException("domain");
                }
                if(domain == string.Empty){
                    throw new ArgumentException("Argument 'domain' value must be specified.","domain");
                }

                m_Domain = domain;

                // We have email address, parse domain.
                if(domain.IndexOf("@") > -1){
                    m_Domain = domain.Split(new char[]{'@'},2)[1];
                }
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resource being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);

                m_pException = null;
                m_Domain     = null;
                m_pHosts     = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="dnsClient">DNS client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>dnsClient</b> is null reference.</exception>
            internal bool Start(Dns_Client dnsClient)
            {
                if(dnsClient == null){
                    throw new ArgumentNullException("dnsClient");
                }

                SetState(AsyncOP_State.Active);

                /* RFC 5321 5.
                    The lookup first attempts to locate an MX record associated with the
                    name.  If a CNAME record is found, the resulting name is processed as
                    if it were the initial name.
                 
			        If no MX records are found, but an A RR is found, the A RR is treated as if it 
                    was associated with an implicit MX RR, with a preference of 0, pointing to that host.
			    */

                try{
                    LookupMX(dnsClient,m_Domain,false);
                }
                catch(Exception x){
                    m_pException = x;
                    SetState(AsyncOP_State.Completed);
                }

                // Set flag rise CompletedAsync event flag. The event is raised when async op completes.
                // If already completed sync, that flag has no effect.
                lock(m_pLock){
                    m_RiseCompleted = true;

                    return m_State == AsyncOP_State.Active;
                }
            }

            #endregion


            #region method SetState

            /// <summary>
            /// Sets operation state.
            /// </summary>
            /// <param name="state">New state.</param>
            private void SetState(AsyncOP_State state)
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }

                lock(m_pLock){
                    m_State = state;

                    if(m_State == AsyncOP_State.Completed && m_RiseCompleted){
                        OnCompletedAsync();
                    }
                }
            }

            #endregion

            #region method LookupMX

            /// <summary>
            /// Starts looking up MX records for specified domain.
            /// </summary>
            /// <param name="dnsClient">DNS client.</param>
            /// <param name="domain">Domain name.</param>
            /// <param name="domainIsCName">If true domain name is CNAME(alias).</param>
            /// <exception cref="ArgumentNullException">Is riased when <b>dnsClient</b> or <b>domain</b> is null reference.</exception>
            private void LookupMX(Dns_Client dnsClient,string domain,bool domainIsCName)
            {
                if(dnsClient == null){
                    throw new ArgumentNullException("dnsClient");
                }
                if(domain == null){
                    throw new ArgumentNullException("domain");
                }

                // Try to get MX records.
                DNS_ClientTransaction transaction_MX = dnsClient.CreateTransaction(DNS_QType.MX,domain,2000);
                transaction_MX.StateChanged += delegate(object s1,EventArgs<DNS_ClientTransaction> e1){
                    try{
                        if(e1.Value.State == DNS_ClientTransactionState.Completed){
                            // No errors.
                            if(e1.Value.Response.ResponseCode == DNS_RCode.NO_ERROR){
                                List<DNS_rr_MX> mxRecords = new List<DNS_rr_MX>();
                                foreach(DNS_rr_MX mx in e1.Value.Response.GetMXRecords()){
                                    // Skip invalid MX records.
                                    if(string.IsNullOrEmpty(mx.Host)){
                                    }
                                    else{
                                        mxRecords.Add(mx);
                                    }
                                }

                                // Use MX records.
                                if(mxRecords.Count > 0){
                                    m_pHosts = new HostEntry[mxRecords.Count];
                                    
                                    // Create name to index map, so we can map asynchronous A/AAAA lookup results back to MX priority index.
                                    Dictionary<string,int> name_to_index_map = new Dictionary<string,int>();
                                    List<string>           lookupQueue       = new List<string>();

                                    // Process MX records.
                                    for(int i=0;i<m_pHosts.Length;i++){
                                        DNS_rr_MX mx = mxRecords[i];
                                    
                                        IPAddress[] ips = Get_A_or_AAAA_FromResponse(mx.Host,e1.Value.Response);
                                        // No A or AAAA records in addtional answers section for MX, we need todo new query for that.
                                        if(ips.Length == 0){
                                            name_to_index_map[mx.Host] = i;
                                            lookupQueue.Add(mx.Host);
                                        }
                                        else{
                                            m_pHosts[i] = new HostEntry(mx.Host,ips,null);
                                        }                                        
                                    }

                                    // We have MX records which A or AAAA records not provided in DNS response, lookup them.
                                    if(lookupQueue.Count > 0){
                                        GetHostsAddressesAsyncOP op = new GetHostsAddressesAsyncOP(lookupQueue.ToArray(),true);
                                        // This event is raised when lookup completes asynchronously.
                                        op.CompletedAsync += delegate(object s2,EventArgs<GetHostsAddressesAsyncOP> e2){
                                            LookupCompleted(op,name_to_index_map);
                                        };
                                        // Lookup completed synchronously.
                                        if(!dnsClient.GetHostsAddressesAsync(op)){
                                            LookupCompleted(op,name_to_index_map);
                                        }
                                    }
                                    // All MX records resolved.
                                    else{
                                        SetState(AsyncOP_State.Completed);
                                    }
                                }
                                // Use CNAME as initial domain name.
                                else if(e1.Value.Response.GetCNAMERecords().Length > 0){
                                    if(domainIsCName){
                                        m_pException = new Exception("CNAME to CNAME loop dedected.");
                                        SetState(AsyncOP_State.Completed);
                                    }
                                    else{
                                        LookupMX(dnsClient,e1.Value.Response.GetCNAMERecords()[0].Alias,true);
                                    }
                                }
                                // Use domain name as MX.
                                else{
                                    m_pHosts = new HostEntry[1];

                                    // Create name to index map, so we can map asynchronous A/AAAA lookup results back to MX priority index.
                                    Dictionary<string,int> name_to_index_map = new Dictionary<string,int>();
                                    name_to_index_map.Add(domain,0);

                                    GetHostsAddressesAsyncOP op = new GetHostsAddressesAsyncOP(new string[]{domain});
                                    // This event is raised when lookup completes asynchronously.
                                    op.CompletedAsync += delegate(object s2,EventArgs<GetHostsAddressesAsyncOP> e2){
                                        LookupCompleted(op,name_to_index_map);
                                    };
                                    // Lookup completed synchronously.
                                    if(!dnsClient.GetHostsAddressesAsync(op)){
                                        LookupCompleted(op,name_to_index_map);
                                    }
                                }
                            }
                            // DNS server returned error, just return error.
                            else{
                                m_pException = new DNS_ClientException(e1.Value.Response.ResponseCode);
                                SetState(AsyncOP_State.Completed);
                            }
                        }
                        transaction_MX.Timeout += delegate(object s2,EventArgs e2){
                            m_pException = new IOException("DNS transaction timeout, no response from DNS server.");
                            SetState(AsyncOP_State.Completed);
                        };
                    }
                    catch(Exception x){
                        m_pException = x;
                        SetState(AsyncOP_State.Completed);
                    }
                };
                transaction_MX.Start();
            }

            #endregion

            #region method Get_A_or_AAAA_FromResponse

            /// <summary>
            /// Gets A and AAAA records from DNS server additional responses section.
            /// </summary>
            /// <param name="name">Host name.</param>
            /// <param name="response">DNS server response.</param>
            /// <returns>Returns A and AAAA records from DNS server additional responses section.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>name</b> or <b>response</b> is null reference.</exception>
            private IPAddress[] Get_A_or_AAAA_FromResponse(string name,DnsServerResponse response)
            {
                if(name == null){
                    throw new ArgumentNullException("name");
                }
                if(response == null){
                    throw new ArgumentNullException("response");
                }

                List<IPAddress> aList = new List<IPAddress>();
                List<IPAddress> aaaaList = new List<IPAddress>();

                foreach(DNS_rr rr in response.AdditionalAnswers){
                    if(string.Equals(name,rr.Name,StringComparison.InvariantCultureIgnoreCase)){
                        if(rr is DNS_rr_A){
                            aList.Add(((DNS_rr_A)rr).IP);
                        }
                        else if(rr is DNS_rr_AAAA){
                            aaaaList.Add(((DNS_rr_AAAA)rr).IP);
                        }
                    }
                }

                // We list IPv4 first and then IPv6 addresses.
                aList.AddRange(aaaaList);

                return aList.ToArray();
            }

            #endregion

            #region method LookupCompleted

            /// <summary>
            /// This method is called when A/AAAA lookup has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <param name="name_to_index">Dns name to index lookup table.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> or <b>name_to_index</b> is null reference value.</exception>
            private void LookupCompleted(GetHostsAddressesAsyncOP op,Dictionary<string,int> name_to_index)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }
                                
                if(op.Error != null){
                    // If we have any resolved DNS, we don't return error if any.
                    bool anyResolved = false;
                    foreach(HostEntry host in m_pHosts){
                        if(host != null){
                            anyResolved = true;

                            break;
                        }
                    }
                    if(!anyResolved){
                        m_pException = op.Error;
                    }
                }
                else{
                    foreach(HostEntry host in op.HostEntries){
                        m_pHosts[name_to_index[host.HostName]] = host;
                    }                   
                }                

                op.Dispose();

                // Remove unresolved DNS entries from response.
                List<HostEntry> retVal = new List<HostEntry>();
                foreach(HostEntry host in m_pHosts){
                    if(host != null){
                        retVal.Add(host);
                    }
                }
                m_pHosts = retVal.ToArray();

                SetState(AsyncOP_State.Completed);
            }

            #endregion
                                    

            #region Properties implementation

            /// <summary>
            /// Gets asynchronous operation state.
            /// </summary>
            public AsyncOP_State State
            {
                get{ return m_State; }
            }

            /// <summary>
            /// Gets error happened during operation. Returns null if no error.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public Exception Error
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Error' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pException; 
                }
            }

            /// <summary>
            /// Gets email domain.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            public string EmailDomain
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }

                    return m_Domain; 
                }
            }

            /// <summary>
            /// Gets email hosts. Hosts are in priority order.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public HostEntry[] Hosts
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Error' is accessible only in 'AsyncOP_State.Completed' state.");
                    }
                    if(m_pException != null){
                        throw m_pException;
                    }

                    return m_pHosts; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<GetEmailHostsAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<GetEmailHostsAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts getting email hosts.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="GetEmailHostsAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool GetEmailHostsAsync(GetEmailHostsAsyncOP op)
        {         
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(op == null){
                throw new ArgumentNullException("op");
            }
            if(op.State != AsyncOP_State.WaitingForStart){
                throw new ArgumentException("Invalid argument 'op' state, 'op' must be in 'AsyncOP_State.WaitingForStart' state.","op");
            }
   
            return op.Start(this);
        }

        #endregion
                        

        #region method Send

        /// <summary>
        /// Sends specified packet to the specified target IP end point.
        /// </summary>
        /// <param name="target">Target end point.</param>
        /// <param name="packet">Packet to send.</param>
        /// <param name="count">Number of bytes to send from <b>packet</b>.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>target</b> or <b>packet</b> is null reference.</exception>
        internal void Send(IPAddress target,byte[] packet,int count)
        {
            if(target == null){
                throw new ArgumentNullException("target");
            }
            if(packet == null){
                throw new ArgumentNullException("packet");
            }

            try{
                if(target.AddressFamily == AddressFamily.InterNetwork){
                    m_pIPv4Socket.SendTo(packet,count,SocketFlags.None,new IPEndPoint(target,53));
                }
                else if(target.AddressFamily == AddressFamily.InterNetworkV6){
                    m_pIPv6Socket.SendTo(packet,count,SocketFlags.None,new IPEndPoint(target,53));
                }                
            }
            catch{
            }
        }

        #endregion


        #region method ProcessUdpPacket

        /// <summary>
        /// Processes received UDP packet.
        /// </summary>
        /// <param name="e">UDP packet.</param>
        private void ProcessUdpPacket(UDP_e_PacketReceived e)
        {
            try{
                if(m_IsDisposed){
                    return;
                }
                                
                DnsServerResponse serverResponse = ParseQuery(e.Buffer);
                DNS_ClientTransaction transaction = null;
                // Pass response to transaction.
                if(m_pTransactions.TryGetValue(serverResponse.ID,out transaction)){
                    if(transaction.State == DNS_ClientTransactionState.Active){
                        // Cache query.
                        if(m_UseDnsCache && serverResponse.ResponseCode == DNS_RCode.NO_ERROR){
	                        m_pCache.AddToCache(transaction.QName,(int)transaction.QType,serverResponse);
		                }
                        
                        transaction.ProcessResponse(serverResponse);
                    }
                }
                // No such transaction or transaction has timed out before answer received.
                //else{
                //}
            }
            catch{
                // We don't care about receiving errors here, skip them.
            }
        }

        #endregion


        #region method GetQName

        internal static bool GetQName(byte[] reply,ref int offset,ref string name)
		{
            bool retVal = GetQNameI(reply,ref offset,ref name);

            // Convert domain name to unicode. For more info see RFC 5890.
            System.Globalization.IdnMapping ldn = new System.Globalization.IdnMapping();
            name = ldn.GetUnicode(name);

            return retVal;
        }

        private static bool GetQNameI(byte[] reply,ref int offset,ref string name)
		{				
			try{				
				while(true){
                    // Invalid DNS packet, offset goes beyound reply size, probably terminator missing.
                    if(offset >= reply.Length){
                        return false;
                    }
                    // We have label terminator "0".
                    if(reply[offset] == 0){
                        break;
                    }

					// Check if it's pointer(In pointer first two bits always 1)
					bool isPointer = ((reply[offset] & 0xC0) == 0xC0);
					
					// If pointer
					if(isPointer){
						/* Pointer location number is 2 bytes long
						    0 | 1 | 2 | 3 | 4 | 5 | 6 | 7  # byte 2 # 0 | 1 | 2 | | 3 | 4 | 5 | 6 | 7
						    empty | < ---- pointer location number --------------------------------->
                        */
						int pStart = ((reply[offset] & 0x3F) << 8) | (reply[++offset]);
						offset++;
			
						return GetQNameI(reply,ref pStart,ref name);
					}
					else{
						/* Label length (length = 8Bit and first 2 bits always 0)
						    0 | 1 | 2 | 3 | 4 | 5 | 6 | 7
						    empty | lablel length in bytes 
                        */
						int labelLength = (reply[offset] & 0x3F);
						offset++;
				
						// Copy label into name 
						name += Encoding.UTF8.GetString(reply,offset,labelLength);
						offset += labelLength;
					}
									
					// If the next char isn't terminator, label continues - add dot between two labels.
					if (reply[offset] != 0){
						name += ".";
					}					
				}

				// Move offset by terminator length.
				offset++;

				return true;
			}
			catch{
				return false;
			}
		}

		#endregion

		#region method ParseQuery

		/// <summary>
		/// Parses query.
		/// </summary>
		/// <param name="reply">Dns server reply.</param>
		/// <returns></returns>
		private DnsServerResponse ParseQuery(byte[] reply)
		{	
			//--- Parse headers ------------------------------------//

			/* RFC 1035 4.1.1. Header section format
			 
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
			 
			QDCOUNT
				an unsigned 16 bit integer specifying the number of
				entries in the question section.

			ANCOUNT
				an unsigned 16 bit integer specifying the number of
				resource records in the answer section.
				
			NSCOUNT
			    an unsigned 16 bit integer specifying the number of name
                server resource records in the authority records section.

			ARCOUNT
			    an unsigned 16 bit integer specifying the number of
                resource records in the additional records section.
				
			*/
		
			// Get reply code
			int       id                     = (reply[0]  << 8 | reply[1]);
			OPCODE    opcode                 = (OPCODE)((reply[2] >> 3) & 15);
			DNS_RCode replyCode              = (DNS_RCode)(reply[3]  & 15);	
			int       queryCount             = (reply[4]  << 8 | reply[5]);
			int       answerCount            = (reply[6]  << 8 | reply[7]);
			int       authoritiveAnswerCount = (reply[8]  << 8 | reply[9]);
			int       additionalAnswerCount  = (reply[10] << 8 | reply[11]);
			//---- End of headers ---------------------------------//
		
			int pos = 12;

			//----- Parse question part ------------//
			for(int q=0;q<queryCount;q++){
				string dummy = "";
				GetQName(reply,ref pos,ref dummy);
				//qtype + qclass
				pos += 4;
			}
			//--------------------------------------//

			// 1) parse answers
			// 2) parse authoritive answers
			// 3) parse additional answers
			List<DNS_rr> answers = ParseAnswers(reply,answerCount,ref pos);
			List<DNS_rr> authoritiveAnswers = ParseAnswers(reply,authoritiveAnswerCount,ref pos);
			List<DNS_rr> additionalAnswers = ParseAnswers(reply,additionalAnswerCount,ref pos);

			return new DnsServerResponse(true,id,replyCode,answers,authoritiveAnswers,additionalAnswers);
		}

		#endregion

		#region method ParseAnswers

		/// <summary>
		/// Parses specified count of answers from query.
		/// </summary>
		/// <param name="reply">Server returned query.</param>
		/// <param name="answerCount">Number of answers to parse.</param>
		/// <param name="offset">Position from where to start parsing answers.</param>
		/// <returns></returns>
		private List<DNS_rr> ParseAnswers(byte[] reply,int answerCount,ref int offset)
		{
			/* RFC 1035 4.1.3. Resource record format
			 
										   1  1  1  1  1  1
			 0  1  2  3  4  5  6  7  8  9  0  1  2  3  4  5
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                                               |
			/                                               /
			/                      NAME                     /
			|                                               |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                      TYPE                     |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                     CLASS                     |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                      TTL                      |
			|                                               |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			|                   RDLENGTH                    |
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--|
			/                     RDATA                     /
			/                                               /
			+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+--+
			*/

			List<DNS_rr> answers = new List<DNS_rr>();
			//---- Start parsing answers ------------------------------------------------------------------//
			for(int i=0;i<answerCount;i++){        
				string name = "";
				if(!GetQName(reply,ref offset,ref name)){
					break;
				}
                                
				int type     = reply[offset++] << 8  | reply[offset++];
				int rdClass  = reply[offset++] << 8  | reply[offset++];
				int ttl      = reply[offset++] << 24 | reply[offset++] << 16 | reply[offset++] << 8  | reply[offset++];
				int rdLength = reply[offset++] << 8  | reply[offset++];
                				
                if((DNS_QType)type == DNS_QType.A){
                    answers.Add(DNS_rr_A.Parse(name,reply,ref offset,rdLength,ttl));
                }
                else if((DNS_QType)type == DNS_QType.NS){
                    answers.Add(DNS_rr_NS.Parse(name,reply,ref offset,rdLength,ttl));
                }
                else if((DNS_QType)type == DNS_QType.CNAME){
                    answers.Add(DNS_rr_CNAME.Parse(name,reply,ref offset,rdLength,ttl));
                }
                else if((DNS_QType)type == DNS_QType.SOA){
                    answers.Add(DNS_rr_SOA.Parse(name,reply,ref offset,rdLength,ttl));
                }
                else if((DNS_QType)type == DNS_QType.PTR){
                    answers.Add(DNS_rr_PTR.Parse(name,reply,ref offset,rdLength,ttl));
                }
                else if((DNS_QType)type == DNS_QType.HINFO){
                    answers.Add(DNS_rr_HINFO.Parse(name,reply,ref offset,rdLength,ttl));
                }
                else if((DNS_QType)type == DNS_QType.MX){
                    answers.Add(DNS_rr_MX.Parse(name,reply,ref offset,rdLength,ttl));
                }
                else if((DNS_QType)type == DNS_QType.TXT){
                    answers.Add(DNS_rr_TXT.Parse(name,reply,ref offset,rdLength,ttl));
                }
                else if((DNS_QType)type == DNS_QType.AAAA){
                    answers.Add(DNS_rr_AAAA.Parse(name,reply,ref offset,rdLength,ttl));
                }
                else if((DNS_QType)type == DNS_QType.SRV){
                    answers.Add(DNS_rr_SRV.Parse(name,reply,ref offset,rdLength,ttl));
                }
                else if((DNS_QType)type == DNS_QType.NAPTR){
                    answers.Add(DNS_rr_NAPTR.Parse(name,reply,ref offset,rdLength,ttl));
                }
                else if((DNS_QType)type == DNS_QType.SPF){
                    answers.Add(DNS_rr_SPF.Parse(name,reply,ref offset,rdLength,ttl));
                }
                else{
                    // Unknown record, skip it.
                    offset += rdLength;
                }
			}

			return answers;
		}

		#endregion

        #region method ReadCharacterString

        /// <summary>
        /// Reads character-string from spefcified data and offset.
        /// </summary>
        /// <param name="data">Data from where to read.</param>
        /// <param name="offset">Offset from where to start reading.</param>
        /// <returns>Returns readed string.</returns>
        internal static string ReadCharacterString(byte[] data,ref int offset)
        {
            /* RFC 1035 3.3.
                <character-string> is a single length octet followed by that number of characters. 
                <character-string> is treated as binary information, and can be up to 256 characters 
                in length (including the length octet).
            */

            int dataLength = (int)data[offset++];
            string retVal = Encoding.Default.GetString(data,offset,dataLength);
            offset += dataLength;

            return retVal;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets static DNS client.
        /// </summary>
        public static Dns_Client Static
        {
            get{ 
                if(m_pDnsClient == null){
                    m_pDnsClient = new Dns_Client();
                }

                return m_pDnsClient; 
            }
        }

        /// <summary>
		/// Gets or sets dns servers.
		/// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null value is passed.</exception>
		public static string[] DnsServers
		{
			get{
                string[] retVal = new string[m_DnsServers.Length];
                for(int i=0;i<m_DnsServers.Length;i++){
                    retVal[i] = m_DnsServers[i].ToString();
                }

                return retVal; 
            }

			set{
                if(value == null){
                    throw new ArgumentNullException();
                }

                IPAddress[] retVal = new IPAddress[value.Length];
                for(int i=0;i<value.Length;i++){
                    retVal[i] = IPAddress.Parse(value[i]);
                }

                m_DnsServers = retVal; 
            }
		}

		/// <summary>
		/// Gets or sets if to use dns caching.
		/// </summary>
		public static bool UseDnsCache
		{
			get{ return m_UseDnsCache; }

			set{ m_UseDnsCache = value; }
		}

        /// <summary>
        /// Gets DNS cache.
        /// </summary>
        public DNS_ClientCache Cache
        {
            get{ return m_pCache; }
        }

		#endregion


        //--- OBSOLETE --------------------

        #region [obsolete] static method Resolve

        /// <summary>
        /// Resolves host names to IP addresses.
        /// </summary>
        /// <param name="hosts">Host names to resolve.</param>
        /// <returns>Returns specified hosts IP addresses.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>hosts</b> is null.</exception>
        [Obsolete("Use Dns_Client.GetHostAddresses instead.")]
        public static IPAddress[] Resolve(string[] hosts)
        {
            if(hosts == null){
                throw new ArgumentNullException("hosts");
            }

            List<IPAddress> retVal = new List<IPAddress>();
            foreach(string host in hosts){
                IPAddress[] addresses = Resolve(host);
                foreach(IPAddress ip in addresses){
                    if(!retVal.Contains(ip)){
                        retVal.Add(ip);
                    }
                }
            }

            return retVal.ToArray();
        }

		/// <summary>
		/// Resolves host name to IP addresses.
		/// </summary>
		/// <param name="host">Host name or IP address.</param>
		/// <returns>Return specified host IP addresses.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>host</b> is null.</exception>
        [Obsolete("Use Dns_Client.GetHostAddresses instead.")]
		public static IPAddress[] Resolve(string host)
		{
            if(host == null){
                throw new ArgumentNullException("host");
            }

			// If hostName_IP is IP
			try{
				return new IPAddress[]{IPAddress.Parse(host)};
			}
			catch{
			}

			// This is probably NetBios name
			if(host.IndexOf(".") == -1){
				return System.Net.Dns.GetHostEntry(host).AddressList;
			}
			else{
				// hostName_IP must be host name, try to resolve it's IP
				using(Dns_Client dns = new Dns_Client()){
				    DnsServerResponse resp = dns.Query(host,DNS_QType.A);
				    if(resp.ResponseCode == DNS_RCode.NO_ERROR){
					    DNS_rr_A[] records = resp.GetARecords();
					    IPAddress[] retVal = new IPAddress[records.Length];
					    for(int i=0;i<records.Length;i++){
						    retVal[i] = records[i].IP;
					    }

					    return retVal;
				    }
				    else{
					    throw new Exception(resp.ResponseCode.ToString());
				    }
                }
			}
		}

		#endregion

	}
}
