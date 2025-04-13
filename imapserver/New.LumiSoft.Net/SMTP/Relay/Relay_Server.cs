using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using LumiSoft.Net;
using LumiSoft.Net.Log;
using LumiSoft.Net.DNS.Client;
using LumiSoft.Net.TCP;

namespace LumiSoft.Net.SMTP.Relay
{
    #region Delegates

    /// <summary>
    /// Represents the method that will handle the <b>Relay_Server.SessionCompleted</b> event.
    /// </summary>
    /// <param name="e">Event data.</param>
    public delegate void Relay_SessionCompletedEventHandler(Relay_SessionCompletedEventArgs e);

    #endregion

    /// <summary>
    /// This class implements SMTP relay server. Defined in RFC 2821.
    /// </summary>
    public class Relay_Server : IDisposable
    {
        private bool                                 m_IsDisposed            = false;
        private bool                                 m_IsRunning             = false;
        private IPBindInfo[]                         m_pBindings             = new IPBindInfo[0];
        private bool                                 m_HasBindingsChanged    = false;
        private Relay_Mode                           m_RelayMode             = Relay_Mode.Dns;
        private List<Relay_Queue>                    m_pQueues               = null;
        private BalanceMode                          m_SmartHostsBalanceMode = BalanceMode.LoadBalance;
        private CircleCollection<Relay_SmartHost>    m_pSmartHosts           = null;
        private CircleCollection<IPBindInfo>         m_pLocalEndPointIPv4    = null;
        private CircleCollection<IPBindInfo>         m_pLocalEndPointIPv6    = null;
        private long                                 m_MaxConnections        = 0;
        private long                                 m_MaxConnectionsPerIP   = 0;
        private Dns_Client                           m_pDsnClient            = null;
        private TCP_SessionCollection<Relay_Session> m_pSessions             = null;
        private Dictionary<IPAddress,long>           m_pConnectionsPerIP     = null;
        private int                                  m_SessionIdleTimeout    = 30;
        private TimerEx                              m_pTimerTimeout         = null;
        private Logger                               m_pLogger               = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public Relay_Server()
        {
            m_pQueues     = new List<Relay_Queue>();
            m_pSmartHosts = new CircleCollection<Relay_SmartHost>();
            m_pDsnClient  = new Dns_Client();
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
            try{
                if(m_IsRunning){
                    Stop();
                }
            }
            catch{
            }
            m_IsDisposed = true;

            // Release events.
            this.Error = null;
            this.SessionCompleted = null;

            m_pQueues     = null;
            m_pSmartHosts = null;

            m_pDsnClient.Dispose();
            m_pDsnClient = null;
        }

        #endregion


        #region Events handling

        #region method m_pTimerTimeout_Elapsed

        /// <summary>
        /// Is called when we need to check timed out relay sessions.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pTimerTimeout_Elapsed(object sender,System.Timers.ElapsedEventArgs e)
        {
            try{
                foreach(Relay_Session session in this.Sessions.ToArray()){
                    try{
                        if(session.LastActivity.AddSeconds(m_SessionIdleTimeout) < DateTime.Now){
                            session.Dispose(new Exception("Session idle timeout."));
                        }
                    }
                    catch{
                    }
                }
            }
            catch(Exception x){
                OnError(x);
            }
        }

        #endregion

        #endregion


        #region method Start

        /// <summary>
        /// Starts SMTP relay server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public virtual void Start()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(m_IsRunning){
                return;
            }
            m_IsRunning = true;

            m_pLocalEndPointIPv4 = new CircleCollection<IPBindInfo>();
            m_pLocalEndPointIPv6 = new CircleCollection<IPBindInfo>();
            m_pSessions          = new TCP_SessionCollection<Relay_Session>();
            m_pConnectionsPerIP  = new Dictionary<IPAddress,long>();

            Thread tr1 = new Thread(new ThreadStart(this.Run));
            tr1.Start();

            m_pTimerTimeout = new TimerEx(30000);
            m_pTimerTimeout.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimerTimeout_Elapsed);
            m_pTimerTimeout.Start();
        }
                
        #endregion

        #region method Stop

        /// <summary>
        /// Stops SMTP relay server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public virtual void Stop()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!m_IsRunning){
                return;
            }
            m_IsRunning = false;

            // TODO: We need to send notify to all not processed messages, then they can be Disposed as needed.
                        
            // Clean up.            
            m_pLocalEndPointIPv4 = null;
            m_pLocalEndPointIPv6 = null;
            //m_pSessions.Dispose();
            m_pSessions = null;
            m_pConnectionsPerIP = null;
            m_pTimerTimeout.Dispose();
            m_pTimerTimeout = null;
        }

        #endregion
                              

        #region method Run

        /// <summary>
        /// Processes relay queue.
        /// </summary>
        private void Run()
        {
            while(m_IsRunning){
                try{
                    // Bind info has changed, create new local end points.
                    if(m_HasBindingsChanged){
                        m_pLocalEndPointIPv4.Clear();
                        m_pLocalEndPointIPv6.Clear();

                        foreach(IPBindInfo binding in m_pBindings){
                            if(binding.IP == IPAddress.Any){
                                foreach(IPAddress ip in System.Net.Dns.GetHostAddresses("")){
                                    if(ip.AddressFamily == AddressFamily.InterNetwork){
                                        IPBindInfo b = new IPBindInfo(binding.HostName,binding.Protocol,ip,25);
                                        if(!m_pLocalEndPointIPv4.Contains(b)){
                                            m_pLocalEndPointIPv4.Add(b);
                                        }
                                    }
                                }
                            }
                            else if(binding.IP == IPAddress.IPv6Any){
                                foreach(IPAddress ip in System.Net.Dns.GetHostAddresses("")){
                                    if(ip.AddressFamily == AddressFamily.InterNetworkV6){
                                        IPBindInfo b = new IPBindInfo(binding.HostName,binding.Protocol,ip,25);
                                        if(!m_pLocalEndPointIPv6.Contains(b)){
                                            m_pLocalEndPointIPv6.Add(b);
                                        }
                                    }
                                }
                            }
                            else{
                                IPBindInfo b = new IPBindInfo(binding.HostName,binding.Protocol,binding.IP,25);
                                if(binding.IP.AddressFamily == AddressFamily.InterNetwork){
                                    if(!m_pLocalEndPointIPv4.Contains(b)){
                                        m_pLocalEndPointIPv4.Add(b);
                                    }
                                }
                                else{
                                    if(!m_pLocalEndPointIPv6.Contains(b)){
                                        m_pLocalEndPointIPv6.Add(b);
                                    }
                                }
                            }
                        }

                        m_HasBindingsChanged = false;
                    }

                    // There are no local end points specified.
                    if(m_pLocalEndPointIPv4.Count == 0 && m_pLocalEndPointIPv6.Count == 0){
                        Thread.Sleep(10);
                    }
                    // Maximum allowed relay sessions exceeded, skip adding new ones.
                    else if(m_MaxConnections != 0 && m_pSessions.Count >= m_MaxConnections){
                        Thread.Sleep(10);
                    }
                    else{
                        Relay_QueueItem item = null;

                        // Get next queued message from highest possible priority queue.
                        foreach(Relay_Queue queue in m_pQueues){
                            item = queue.DequeueMessage();
                            // There is queued message.
                            if(item != null){
                                break;
                            }
                            // No messages in this queue, see next lower priority queue.
                        }

                        // There are no messages in any queue.
                        if(item == null){
                            Thread.Sleep(10);
                        }
                        // Create new session for queued relay item.
                        else{
                            if(m_RelayMode == Relay_Mode.Dns){
                                Relay_Session session = new Relay_Session(this,item);
                                m_pSessions.Add(session);
                                ThreadPool.QueueUserWorkItem(new WaitCallback(session.Start));
                            }
                            else if(m_RelayMode == Relay_Mode.SmartHost){
                                // Get smart hosts in balance mode order.
                                Relay_SmartHost[] smartHosts = null;
                                if(m_SmartHostsBalanceMode == BalanceMode.FailOver){
                                    smartHosts = m_pSmartHosts.ToArray();
                                }
                                else{
                                    smartHosts = m_pSmartHosts.ToCurrentOrderArray();
                                }

                                Relay_Session session = new Relay_Session(this,item,smartHosts);
                                m_pSessions.Add(session);
                                ThreadPool.QueueUserWorkItem(new WaitCallback(session.Start));
                            }                            
                        }
                    }                    
                }
                catch(Exception x){
                    OnError(x);
                }
            }
        }

        #endregion


        #region method GetLocalBinding

        /// <summary>
        /// Gets local IP binding for specified remote IP.
        /// </summary>
        /// <param name="remoteIP">Remote SMTP target IP address.</param>
        /// <returns>Returns local IP binding or null if no suitable IP binding available.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>remoteIP</b> is null reference.</exception>
        internal IPBindInfo GetLocalBinding(IPAddress remoteIP)
        {
            if(remoteIP == null){
                throw new ArgumentNullException("remoteIP");
            }

            // Get round-robin local end point for that remote IP.
            // This ensures if multiple network connections, all will be load balanced.

            // IPv6
            if(remoteIP.AddressFamily == AddressFamily.InterNetworkV6){
                if(m_pLocalEndPointIPv6.Count == 0){
                    return null;
                }
                else{
                    return m_pLocalEndPointIPv6.Next();
                }
            }
            // IPv4
            else{
                if(m_pLocalEndPointIPv4.Count == 0){
                    return null;
                }
                else{
                    return m_pLocalEndPointIPv4.Next();
                }
            }
        }

        #endregion

        #region method TryAddIpUsage

        /// <summary>
        /// Increases specified IP address connactions count if maximum allowed connections to 
        /// the specified IP address isn't exceeded.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null.</exception>
        /// <returns>Returns true if specified IP usage increased, false if maximum allowed connections to the specified IP address is exceeded.</returns>
        internal bool TryAddIpUsage(IPAddress ip)
        {
            if(ip == null){
                throw new ArgumentNullException("ip");
            }

            lock(m_pConnectionsPerIP){
                long count = 0;
                // Specified IP entry exists, increase usage.
                if(m_pConnectionsPerIP.TryGetValue(ip,out count)){
                    // Maximum allowed connections to the specified IP address is exceeded.
                    if(m_MaxConnectionsPerIP > 0 && count >= m_MaxConnectionsPerIP){
                        return false;
                    }

                    m_pConnectionsPerIP[ip] = count + 1;
                }
                // Specified IP entry doesn't exist, create new entry and increase usage.
                else{
                    m_pConnectionsPerIP.Add(ip,1);
                }

                return true;
            }
        }

        #endregion

        #region method RemoveIpUsage

        /// <summary>
        /// Decreases specified IP address connactions count.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null.</exception>
        internal void RemoveIpUsage(IPAddress ip)
        {
            if(ip == null){
                throw new ArgumentNullException("ip");
            }

            lock(m_pConnectionsPerIP){
                long count = 0;
                // Specified IP entry exists, increase usage.
                if(m_pConnectionsPerIP.TryGetValue(ip,out count)){
                    // This is last usage to that IP, remove that IP entry.
                    if(count == 1){
                        m_pConnectionsPerIP.Remove(ip);
                    }
                    // Decrease Ip usage.
                    else{
                        m_pConnectionsPerIP[ip] = count - 1;
                    }
                }
                else{
                    // No such entry, just skip it.
                }
            }
        }

        #endregion

        #region method GetIpUsage

        /// <summary>
        /// Gets how many connections to the specified IP address.
        /// </summary>
        /// <param name="ip">IP address.</param>
        /// <returns>Returns number of connections to the specified IP address.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>ip</b> is null.</exception>
        internal long GetIpUsage(IPAddress ip)
        {
            if(ip == null){
                throw new ArgumentNullException("ip");
            }

            lock(m_pConnectionsPerIP){
                long count = 0;
                // Specified IP entry exists, return usage.
                if(m_pConnectionsPerIP.TryGetValue(ip,out count)){
                    return count;
                }
                // No usage to specified IP.
                else{
                    return 0;
                }
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets if server is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get{ return m_IsDisposed; }
        }

        /// <summary>
        /// Gets if server is running.
        /// </summary>
        public bool IsRunning
        {
            get{ return m_IsRunning; }
        }

        /// <summary>
        /// Gets or sets relay server IP bindings.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public IPBindInfo[] Bindings
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pBindings; 
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value == null){
                    value = new IPBindInfo[0];
                }

                //--- See binds has changed --------------
                bool changed = false;
                if(m_pBindings.Length != value.Length){
                    changed = true;
                }
                else{
                    for(int i=0;i<m_pBindings.Length;i++){
                        if(!m_pBindings[i].Equals(value[i])){
                            changed = true;
                            break;
                        }
                    }
                }

                if(changed){
                    m_pBindings = value;
                    m_HasBindingsChanged = true;
                }
            }
        }

        /// <summary>
        /// Gets or sets relay mode.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public Relay_Mode RelayMode
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_RelayMode; 
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                m_RelayMode = value;
            }
        }

        /// <summary>
        /// Gets relay queues. Queue with lower index number has higher priority.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public List<Relay_Queue> Queues
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pQueues; 
            }
        }

        /// <summary>
        /// Gets or sets how smart hosts will be balanced.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public BalanceMode SmartHostsBalanceMode
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_SmartHostsBalanceMode; 
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                m_SmartHostsBalanceMode = value;
            }
        }

        /// <summary>
        /// Gets or sets smart hosts. Smart hosts must be in priority order, lower index number means higher priority.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when null value is passed.</exception>
        public Relay_SmartHost[] SmartHosts
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSmartHosts.ToArray();
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value == null){
                    throw new ArgumentNullException("SmartHosts");
                }

                m_pSmartHosts.Add(value);
            }
        }
                
        /// <summary>
        /// Gets or sets maximum allowed concurent connections. Value 0 means unlimited.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when negative value is passed.</exception>
        public long MaxConnections
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_MaxConnections; 
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value < 0){
                    throw new ArgumentException("Property 'MaxConnections' value must be >= 0.");
                }

                m_MaxConnections = value;
            }
        }

        /// <summary>
        /// Gets or sets maximum allowed connections to 1 IP address. Value 0 means unlimited.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public long MaxConnectionsPerIP
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_MaxConnectionsPerIP; 
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(m_MaxConnectionsPerIP < 0){
                    throw new ArgumentException("Property 'MaxConnectionsPerIP' value must be >= 0.");
                }

                m_MaxConnectionsPerIP = value;
            }
        }

        /// <summary>
        /// Gets or sets session idle time in seconds when it will be timed out.  Value 0 means unlimited (strongly not recomended).
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public int SessionIdleTimeout
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_SessionIdleTimeout;
            }

            set{if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(m_SessionIdleTimeout < 0){
                    throw new ArgumentException("Property 'SessionIdleTimeout' value must be >= 0.");
                }

                m_SessionIdleTimeout = value;
            }
        }

        /// <summary>
        /// Gets active relay sessions.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and relay server is not running.</exception>
        public TCP_SessionCollection<Relay_Session> Sessions
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!m_IsRunning){
                    throw new InvalidOperationException("Relay server not running.");
                }

                return m_pSessions; 
            }
        }        

        /// <summary>
        /// Gets or sets relay logger. Value null means no logging.
        /// </summary>
        public Logger Logger
        {
            get{ return m_pLogger; }

            set{ m_pLogger = value; }
        }

        /// <summary>
        /// Gets or stes DNS client.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when null value is passed.</exception>
        public Dns_Client DnsClient
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pDsnClient;
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value == null){
                    throw new ArgumentNullException("DnsClient");
                }

                m_pDsnClient = value;
            }
        }

        #endregion

        #region Events Implementation

        /// <summary>
        /// This event is raised when relay session processing completes.
        /// </summary>
        public event Relay_SessionCompletedEventHandler SessionCompleted = null;

        #region method OnSessionCompleted

        /// <summary>
        /// Raises <b>SessionCompleted</b> event.
        /// </summary>
        /// <param name="session">Session what completed processing.</param>
        /// <param name="exception">Exception happened or null if relay completed successfully.</param>
        internal protected virtual void OnSessionCompleted(Relay_Session session,Exception exception)
        {
            if(this.SessionCompleted != null){
                this.SessionCompleted(new Relay_SessionCompletedEventArgs(session,exception));
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when unhandled exception happens.
        /// </summary>
        public event ErrorEventHandler Error = null;

        #region method OnError

        /// <summary>
        /// Raises <b>Error</b> event.
        /// </summary>
        /// <param name="x">Exception happned.</param>
        internal protected virtual void OnError(Exception x)
        {
            if(this.Error != null){
                this.Error(this,new Error_EventArgs(x,new System.Diagnostics.StackTrace()));
            }
        }

        #endregion

        #endregion

    }
}
