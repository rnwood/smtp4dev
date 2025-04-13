using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Security.Principal;

using LumiSoft.Net;
using LumiSoft.Net.IO;
using LumiSoft.Net.TCP;
using LumiSoft.Net.SMTP;
using LumiSoft.Net.SMTP.Client;
using LumiSoft.Net.DNS;
using LumiSoft.Net.DNS.Client;
using LumiSoft.Net.Log;

namespace LumiSoft.Net.SMTP.Relay
{
    /// <summary>
    /// This class implements SMTP relay server session.
    /// </summary>
    public class Relay_Session : TCP_Session
    {
        #region class Relay_Target

        /// <summary>
        /// This class holds relay target information.
        /// </summary>
        private class Relay_Target
        {
            private string     m_HostName = "";
            private IPEndPoint m_pTarget  = null;
            private SslMode    m_SslMode  = SslMode.None;
            private string     m_UserName = null;
            private string     m_Password = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="hostName">Target host name.</param>
            /// <param name="target">Target host IP end point.</param>
            public Relay_Target(string hostName,IPEndPoint target)
            {
                m_HostName = hostName;
                m_pTarget  = target;
            }

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="hostName">Target host name.</param>
            /// <param name="target">Target host IP end point.</param>
            /// <param name="sslMode">SSL mode.</param>
            /// <param name="userName">Target host user name.</param>
            /// <param name="password">Target host password.</param>
            public Relay_Target(string hostName,IPEndPoint target,SslMode sslMode,string userName,string password)
            {
                m_HostName = hostName;
                m_pTarget  = target;
                m_SslMode  = sslMode;
                m_UserName = userName;
                m_Password = password;
            }


            #region Properties Implementation

            /// <summary>
            /// Gets target host name.
            /// </summary>
            public string HostName
            {
                get{ return m_HostName; }
            }

            /// <summary>
            /// Gets specified target IP end point.
            /// </summary>
            public IPEndPoint Target
            {
                get{ return m_pTarget; }
            }

            /// <summary>
            /// Gets target SSL mode.
            /// </summary>
            public SslMode SslMode
            {
                get{ return m_SslMode; }
            }

            /// <summary>
            /// Gets target server user name.
            /// </summary>
            public string UserName
            {
                get{ return m_UserName; }
            }

            /// <summary>
            /// Gets target server password.
            /// </summary>
            public string Password
            {
                get{ return m_Password; }
            }

            #endregion

        }

        #endregion

        private bool               m_IsDisposed     = false;
        private Relay_Server       m_pServer        = null;
        private IPBindInfo         m_pLocalBindInfo = null;
        private Relay_QueueItem    m_pRelayItem     = null;
        private Relay_SmartHost[]  m_pSmartHosts    = null;
        private Relay_Mode         m_RelayMode      = Relay_Mode.Dns;
        private string             m_SessionID      = "";
        private DateTime           m_SessionCreateTime;
        private SMTP_Client        m_pSmtpClient    = null;
        private List<Relay_Target> m_pTargets       = null;
        private Relay_Target       m_pActiveTarget  = null;
        
        /// <summary>
        /// Dns relay session constructor.
        /// </summary>
        /// <param name="server">Owner relay server.</param>
        /// <param name="realyItem">Relay item.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>server</b> or <b>realyItem</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        internal Relay_Session(Relay_Server server,Relay_QueueItem realyItem)
        {
            if(server == null){
                throw new ArgumentNullException("server");
            }
            if(realyItem == null){
                throw new ArgumentNullException("realyItem");
            }

            m_pServer    = server;
            m_pRelayItem = realyItem;

            m_SessionID         = Guid.NewGuid().ToString();
            m_SessionCreateTime = DateTime.Now;
            m_pTargets          = new List<Relay_Target>();
            m_pSmtpClient       = new SMTP_Client();
        }

        /// <summary>
        /// Smart host relay session constructor.
        /// </summary>
        /// <param name="server">Owner relay server.</param>
        /// <param name="realyItem">Relay item.</param>
        /// <param name="smartHosts">Smart hosts.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>server</b>,<b>realyItem</b> or <b>smartHosts</b>is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        internal Relay_Session(Relay_Server server,Relay_QueueItem realyItem,Relay_SmartHost[] smartHosts)
        {
            if(server == null){
                throw new ArgumentNullException("server");
            }
            if(realyItem == null){
                throw new ArgumentNullException("realyItem");
            }
            if(smartHosts == null){
                throw new ArgumentNullException("smartHosts");
            }

            m_pServer     = server;
            m_pRelayItem  = realyItem;
            m_pSmartHosts = smartHosts;
                        
            m_RelayMode         = Relay_Mode.SmartHost;
            m_SessionID         = Guid.NewGuid().ToString();
            m_SessionCreateTime = DateTime.Now;
            m_pTargets          = new List<Relay_Target>();
            m_pSmtpClient       = new SMTP_Client();
        }

        #region override method Dispose

        /// <summary>
        /// Completes relay session and does clean up. This method is thread-safe.
        /// </summary>
        public override void Dispose()
        {
            Dispose(new ObjectDisposedException(this.GetType().Name));
        }

        /// <summary>
        /// Completes relay session and does clean up. This method is thread-safe.
        /// </summary>
        /// <param name="exception">Exception happened or null if relay completed successfully.</param>
        public void Dispose(Exception exception)
        {
            try{
                lock(this){
                    if(m_IsDisposed){
                        return;
                    }
                    try{
                        m_pServer.OnSessionCompleted(this,exception);
                    }
                    catch{
                    }
                    m_pServer.Sessions.Remove(this);
                    m_IsDisposed = true;
                        
                    m_pLocalBindInfo = null;
                    m_pRelayItem = null;
                    m_pSmartHosts = null;
                    if(m_pSmtpClient != null){
                        m_pSmtpClient.Dispose();
                        m_pSmtpClient = null;
                    }
                    m_pTargets = null;
                    if(m_pActiveTarget != null){
                        m_pServer.RemoveIpUsage(m_pActiveTarget.Target.Address);
                        m_pActiveTarget = null;
                    }
                    m_pServer = null;
                }
            }
            catch(Exception x){
                if(m_pServer != null){
                    m_pServer.OnError(x);
                }
            }
        }

        #endregion


        #region method Start

        /// <summary>
        /// Start processing relay message.
        /// </summary>
        /// <param name="state">User data.</param>
        internal void Start(object state)
        {
            try{ 
                if(m_pServer.Logger != null){
                    m_pSmtpClient.Logger = new Logger();
                    m_pSmtpClient.Logger.WriteLog += new EventHandler<WriteLogEventArgs>(SmtpClient_WriteLog);
                }

                LogText("Starting to relay message '" + m_pRelayItem.MessageID + "' from '" + m_pRelayItem.From + "' to '" + m_pRelayItem.To + "'.");

                // Resolve email target hosts.               
                if(m_RelayMode == Relay_Mode.Dns){
                    Dns_Client.GetEmailHostsAsyncOP op = new Dns_Client.GetEmailHostsAsyncOP(m_pRelayItem.To);
                    op.CompletedAsync += delegate(object s1,EventArgs<Dns_Client.GetEmailHostsAsyncOP> e1){
                        EmailHostsResolveCompleted(m_pRelayItem.To,op);
                    };
                    if(!m_pServer.DnsClient.GetEmailHostsAsync(op)){
                        EmailHostsResolveCompleted(m_pRelayItem.To,op);
                    }
                }
                // Resolve smart hosts IP addresses.
                else if(m_RelayMode == Relay_Mode.SmartHost){
                    string[] smartHosts = new string[m_pSmartHosts.Length];
                    for(int i=0;i<m_pSmartHosts.Length;i++){
                        smartHosts[i] = m_pSmartHosts[i].Host;
                    }

                    Dns_Client.GetHostsAddressesAsyncOP op = new Dns_Client.GetHostsAddressesAsyncOP(smartHosts);
                    op.CompletedAsync += delegate(object s1,EventArgs<Dns_Client.GetHostsAddressesAsyncOP> e1){
                        SmartHostsResolveCompleted(op);
                    };
                    if(!m_pServer.DnsClient.GetHostsAddressesAsync(op)){
                        SmartHostsResolveCompleted(op);
                    }
                } 
            }
            catch(Exception x){
                Dispose(x);
            }
        }
                
        #endregion


        #region override method Disconnect

        /// <summary>
        /// Closes relay connection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public override void Disconnect()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                return;
            }

            m_pSmtpClient.Disconnect();
        }

        /// <summary>
        /// Closes relay connection.
        /// </summary>
        /// <param name="text">Text to send to the connected host.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        public void Disconnect(string text)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                return;
            }

            m_pSmtpClient.TcpStream.WriteLine(text);
            Disconnect();
        }

        #endregion


        #region method EmailHostsResolveCompleted

        /// <summary>
        /// Is called when email domain target servers resolve operation has completed.
        /// </summary>
        /// <param name="to">RCPT TO: address.</param>
        /// <param name="op">Asynchronous operation.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>to</b> or <b>op</b> is null reference.</exception>
        private void EmailHostsResolveCompleted(string to,Dns_Client.GetEmailHostsAsyncOP op)
        {
            if(to == null){
                throw new ArgumentNullException("to");
            }
            if(op == null){
                throw new ArgumentNullException("op");
            }
            
            if(op.Error != null){
                LogText("Failed to resolve email domain for email address '" + to + "' with error: " + op.Error.Message + ".");

                Dispose(op.Error);
            }
            else{
                StringBuilder buf = new StringBuilder();
                foreach(HostEntry host in op.Hosts){
                    foreach(IPAddress ip in host.Addresses){
                        m_pTargets.Add(new Relay_Target(host.HostName,new IPEndPoint(ip,25)));
                    }
                    buf.Append(host.HostName + " ");
                }
                LogText("Resolved to following email hosts: (" + buf.ToString().TrimEnd() + ").");

                BeginConnect();
            }

            op.Dispose();
        }

        #endregion

        #region method SmartHostsResolveCompleted

        /// <summary>
        /// Is called when smart hosts ip addresses resolve operation has completed.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        private void SmartHostsResolveCompleted(Dns_Client.GetHostsAddressesAsyncOP op)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }

            if(op.Error != null){
                LogText("Failed to resolve relay smart host(s) ip addresses with error: " + op.Error.Message + ".");

                Dispose(op.Error);
            }
            else{
                for(int i=0;i<op.HostEntries.Length;i++){
                    Relay_SmartHost smartHost = m_pSmartHosts[i];

                    foreach(IPAddress ip in op.HostEntries[i].Addresses){
                        m_pTargets.Add(new Relay_Target(smartHost.Host,new IPEndPoint(ip,smartHost.Port),smartHost.SslMode,smartHost.UserName,smartHost.Password));
                    }
                }                

                BeginConnect();
            }

            op.Dispose();
        }

        #endregion

        #region method BeginConnect

        /// <summary>
        /// Starts connecting to best target. 
        /// </summary>
        private void BeginConnect()
        {
            // No tagets, abort relay.
            if(m_pTargets.Count == 0){
                LogText("No relay target(s) for '" + m_pRelayItem.To + "', aborting.");
                Dispose(new Exception("No relay target(s) for '" + m_pRelayItem.To + "', aborting."));

                return;
            }

            // Maximum connections per IP limited.           
            if(m_pServer.MaxConnectionsPerIP > 0){
                // For DNS or load-balnced smart host relay, search free target if any.
                if(m_pServer.RelayMode == Relay_Mode.Dns || m_pServer.SmartHostsBalanceMode == BalanceMode.LoadBalance){
                    foreach(Relay_Target t in m_pTargets){
                        // Get local IP binding for remote IP.
                        m_pLocalBindInfo = m_pServer.GetLocalBinding(t.Target.Address);

                        // We have suitable local IP binding for the target.
                        if(m_pLocalBindInfo != null){
                            // We found free target, stop searching.
                            if(m_pServer.TryAddIpUsage(t.Target.Address)){
                                m_pActiveTarget = t;
                                m_pTargets.Remove(t);

                                break;
                            }
                            // Connection per IP limit reached.
                            else{
                                LogText("Skipping relay target (" + t.HostName + "->" + t.Target.Address + "), maximum connections to the specified IP has reached.");
                            }
                        }
                        // No suitable local IP binding, try next target.
                        else{
                            LogText("Skipping relay target (" + t.HostName + "->" + t.Target.Address + "), no suitable local IPv4/IPv6 binding.");
                        }
                    }
                }
                // Smart host fail-over mode, just check if it's free.
                else{
                    // Get local IP binding for remote IP.
                    m_pLocalBindInfo = m_pServer.GetLocalBinding(m_pTargets[0].Target.Address);

                    // We have suitable local IP binding for the target.
                    if(m_pLocalBindInfo != null){
                        // Smart host IP limit not reached.
                        if(m_pServer.TryAddIpUsage(m_pTargets[0].Target.Address)){
                            m_pActiveTarget = m_pTargets[0];
                            m_pTargets.RemoveAt(0);
                        }
                        // Connection per IP limit reached.
                        else{
                            LogText("Skipping relay target (" + m_pTargets[0].HostName + "->" + m_pTargets[0].Target.Address + "), maximum connections to the specified IP has reached.");
                        }
                    }
                    // No suitable local IP binding, try next target.
                    else{
                        LogText("Skipping relay target (" + m_pTargets[0].HostName + "->" + m_pTargets[0].Target.Address + "), no suitable local IPv4/IPv6 binding.");
                    }
                }                
            }
            // Just get first target.
            else{
                 // Get local IP binding for remote IP.
                 m_pLocalBindInfo = m_pServer.GetLocalBinding(m_pTargets[0].Target.Address);

                 // We have suitable local IP binding for the target.
                 if(m_pLocalBindInfo != null){
                    m_pActiveTarget = m_pTargets[0];
                    m_pTargets.RemoveAt(0);
                 }
                 // No suitable local IP binding, try next target.
                 else{
                    LogText("Skipping relay target (" + m_pTargets[0].HostName + "->" + m_pTargets[0].Target.Address + "), no suitable local IPv4/IPv6 binding.");
                 }
            }

            // We don't have suitable local IP end point for relay target.
            // This may heppen for example: if remote server supports only IPv6 and we don't have local IPv6 local end point.            
            if(m_pLocalBindInfo == null){
                LogText("No suitable IPv4/IPv6 local IP endpoint for relay target.");
                Dispose(new Exception("No suitable IPv4/IPv6 local IP endpoint for relay target."));

                return;
            }

            // If all targets has exeeded maximum allowed connection per IP address, end relay session, 
            // next relay cycle will try to relay again.
            if(m_pActiveTarget == null){
                LogText("All targets has exeeded maximum allowed connection per IP address, skip relay.");
                Dispose(new Exception("All targets has exeeded maximum allowed connection per IP address, skip relay."));

                return;
            }

            // Set SMTP host name.
            m_pSmtpClient.LocalHostName = m_pLocalBindInfo.HostName;

            // Start connecting to remote end point.
            TCP_Client.ConnectAsyncOP connectOP = new TCP_Client.ConnectAsyncOP(new IPEndPoint(m_pLocalBindInfo.IP,0),m_pActiveTarget.Target,false,null);
            connectOP.CompletedAsync += delegate(object s,EventArgs<TCP_Client.ConnectAsyncOP> e){
                ConnectCompleted(connectOP);
            };
            if(!m_pSmtpClient.ConnectAsync(connectOP)){
                ConnectCompleted(connectOP);
            }
        }

        #endregion

        #region method ConnectCompleted

        /// <summary>
        /// Is called when EHLO/HELO command has completed.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        private void ConnectCompleted(TCP_Client.ConnectAsyncOP op)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }

            try{
                // Connect failed.
                if(op.Error != null){
                    try{
                        // Release IP usage.
                        m_pServer.RemoveIpUsage(m_pActiveTarget.Target.Address);
                        m_pActiveTarget = null;

                        // Connect failed, if there are more target IPs, try next one.
                        if(!this.IsDisposed && !this.IsConnected && m_pTargets.Count > 0){
                            BeginConnect();
                        }
                        else{
                            Dispose(op.Error);
                        }
                    }
                    catch(Exception x1){
                        Dispose(x1);
                    }
                }
                // Connect suceeded.
                else{
                    // Do EHLO/HELO.
                    string hostName = string.IsNullOrEmpty(m_pLocalBindInfo.HostName) ? Dns.GetHostName() : m_pLocalBindInfo.HostName;
                    SMTP_Client.EhloHeloAsyncOP ehloOP = new SMTP_Client.EhloHeloAsyncOP(hostName);
                    ehloOP.CompletedAsync += delegate(object s,EventArgs<SMTP_Client.EhloHeloAsyncOP> e){
                        EhloCommandCompleted(ehloOP);
                    };
                    if(!m_pSmtpClient.EhloHeloAsync(ehloOP)){
                        EhloCommandCompleted(ehloOP);
                    }
                }
            }
            catch(Exception x){
                Dispose(x);
            }
        }

        #endregion

        #region method EhloCommandCompleted

        /// <summary>
        /// Is called when EHLO/HELO command has completed.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        private void EhloCommandCompleted(SMTP_Client.EhloHeloAsyncOP op)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }

            try{
                if(op.Error != null){
                    Dispose(op.Error);
                }
                else{
                    // Start TLS requested, start switching to secure.
                    if(!m_pSmtpClient.IsSecureConnection && m_pActiveTarget.SslMode == SslMode.TLS){
                        SMTP_Client.StartTlsAsyncOP startTlsOP = new SMTP_Client.StartTlsAsyncOP(null);
                        startTlsOP.CompletedAsync += delegate(object s,EventArgs<SMTP_Client.StartTlsAsyncOP> e){
                            StartTlsCommandCompleted(startTlsOP);
                        };
                        if(!m_pSmtpClient.StartTlsAsync(startTlsOP)){
                            StartTlsCommandCompleted(startTlsOP);
                        }
                    }
                    // Authentication requested, start authenticating.
                    else if(!string.IsNullOrEmpty(m_pActiveTarget.UserName)){
                        SMTP_Client.AuthAsyncOP authOP = new SMTP_Client.AuthAsyncOP(m_pSmtpClient.AuthGetStrongestMethod(m_pActiveTarget.UserName,m_pActiveTarget.Password));                        
                        authOP.CompletedAsync += delegate(object s,EventArgs<SMTP_Client.AuthAsyncOP> e){
                            AuthCommandCompleted(authOP);
                        };
                        if(!m_pSmtpClient.AuthAsync(authOP)){
                            AuthCommandCompleted(authOP);
                        }
                    }
                    // Start MAIL command.
                    else{
                        long messageSize = -1;
                        try{
                            messageSize = m_pRelayItem.MessageStream.Length - m_pRelayItem.MessageStream.Position;
                        }
                        catch{
                            // Stream doesn't support seeking.
                        }

                        SMTP_Client.MailFromAsyncOP mailOP = new SMTP_Client.MailFromAsyncOP(
                            this.From,
                            messageSize,
                            IsDsnSupported() ? m_pRelayItem.DSN_Ret : SMTP_DSN_Ret.NotSpecified,
                            IsDsnSupported() ? m_pRelayItem.EnvelopeID : null
                        );
                        mailOP.CompletedAsync += delegate(object s,EventArgs<SMTP_Client.MailFromAsyncOP> e){
                            MailCommandCompleted(mailOP);
                        };
                        if(!m_pSmtpClient.MailFromAsync(mailOP)){
                            MailCommandCompleted(mailOP);
                        }
                    }
                }                                
            }
            catch(Exception x){
                Dispose(x);
            }
        }

        #endregion

        #region method StartTlsCommandCompleted

        /// <summary>
        /// Is called when STARTTLS command has completed.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        private void StartTlsCommandCompleted(SMTP_Client.StartTlsAsyncOP op)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }

            try{
                if(op.Error != null){
                    Dispose(op.Error);
                }
                else{
                    // Do EHLO/HELO.
                    SMTP_Client.EhloHeloAsyncOP ehloOP = new SMTP_Client.EhloHeloAsyncOP(null);
                    ehloOP.CompletedAsync += delegate(object s,EventArgs<SMTP_Client.EhloHeloAsyncOP> e){
                        EhloCommandCompleted(ehloOP);
                    };
                    if(!m_pSmtpClient.EhloHeloAsync(ehloOP)){
                        EhloCommandCompleted(ehloOP);
                    }                    
                }                                
            }
            catch(Exception x){
                Dispose(x);
            }
        }

        #endregion

        #region method AuthCommandCompleted

        /// <summary>
        /// Is called when AUTH command has completed.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        private void AuthCommandCompleted(SMTP_Client.AuthAsyncOP op)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }

            try{
                if(op.Error != null){
                    Dispose(op.Error);
                }
                else{
                    long messageSize = -1;
                    try{
                        messageSize = m_pRelayItem.MessageStream.Length - m_pRelayItem.MessageStream.Position;
                    }
                    catch{
                        // Stream doesn't support seeking.
                    }

                    SMTP_Client.MailFromAsyncOP mailOP = new SMTP_Client.MailFromAsyncOP(
                        this.From,
                        messageSize,
                        IsDsnSupported() ? m_pRelayItem.DSN_Ret : SMTP_DSN_Ret.NotSpecified,
                        IsDsnSupported() ? m_pRelayItem.EnvelopeID : null
                    );
                    mailOP.CompletedAsync += delegate(object s,EventArgs<SMTP_Client.MailFromAsyncOP> e){
                        MailCommandCompleted(mailOP);
                    };
                    if(!m_pSmtpClient.MailFromAsync(mailOP)){
                        MailCommandCompleted(mailOP);
                    }
                }                                
            }
            catch(Exception x){
                Dispose(x);
            }
        }

        #endregion

        #region method MailCommandCompleted

        /// <summary>
        /// Is called when MAIL command has completed.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        private void MailCommandCompleted(SMTP_Client.MailFromAsyncOP op)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }

            try{
                if(op.Error != null){
                    Dispose(op.Error);
                }
                else{
                    SMTP_Client.RcptToAsyncOP rcptOP = new SMTP_Client.RcptToAsyncOP(
                        this.To,
                        IsDsnSupported() ? m_pRelayItem.DSN_Notify : SMTP_DSN_Notify.NotSpecified,
                        IsDsnSupported() ? m_pRelayItem.OriginalRecipient : null
                    );
                    rcptOP.CompletedAsync += delegate(object s,EventArgs<SMTP_Client.RcptToAsyncOP> e){
                        RcptCommandCompleted(rcptOP);
                    };
                    if(!m_pSmtpClient.RcptToAsync(rcptOP)){
                        RcptCommandCompleted(rcptOP);
                    }
                }                                
            }
            catch(Exception x){
                Dispose(x);
            }
        }

        #endregion

        #region method RcptCommandCompleted

        /// <summary>
        /// Is called when RCPT command has completed.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        private void RcptCommandCompleted(SMTP_Client.RcptToAsyncOP op)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }

            try{
                if(op.Error != null){
                    Dispose(op.Error);
                }
                else{
                    // Start sending message.
                    SMTP_Client.SendMessageAsyncOP sendMsgOP = new SMTP_Client.SendMessageAsyncOP(m_pRelayItem.MessageStream,false);
                    sendMsgOP.CompletedAsync += delegate(object s,EventArgs<SMTP_Client.SendMessageAsyncOP> e){
                        MessageSendingCompleted(sendMsgOP);
                    };
                    if(!m_pSmtpClient.SendMessageAsync(sendMsgOP)){
                        MessageSendingCompleted(sendMsgOP);
                    }
                }                                
            }
            catch(Exception x){
                Dispose(x);
            }
        }

        #endregion

        #region method MessageSendingCompleted

        /// <summary>
        /// Is called when message sending has completed.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        private void MessageSendingCompleted(SMTP_Client.SendMessageAsyncOP op)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }

            try{
                if(op.Error != null){
                    Dispose(op.Error);
                }
                // Message sent sucessfully.
                else{
                    Dispose(null);
                }
            }
            catch(Exception x){
                Dispose(x);
            }

            op.Dispose();
        }

        #endregion


        #region method SmtpClient_WriteLog

        /// <summary>
        /// Thsi method is called when SMTP client has new log entry available.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void SmtpClient_WriteLog(object sender,WriteLogEventArgs e)
        {
            try{
                if(m_pServer.Logger == null){
                }
                else if(e.LogEntry.EntryType == LogEntryType.Read){
                    m_pServer.Logger.AddRead(m_SessionID,e.LogEntry.UserIdentity,e.LogEntry.Size,e.LogEntry.Text,e.LogEntry.LocalEndPoint,e.LogEntry.RemoteEndPoint);
                }
                else if(e.LogEntry.EntryType == LogEntryType.Text){
                    m_pServer.Logger.AddText(m_SessionID,e.LogEntry.UserIdentity,e.LogEntry.Text,e.LogEntry.LocalEndPoint,e.LogEntry.RemoteEndPoint);
                }
                else if(e.LogEntry.EntryType == LogEntryType.Write){
                    m_pServer.Logger.AddWrite(m_SessionID,e.LogEntry.UserIdentity,e.LogEntry.Size,e.LogEntry.Text,e.LogEntry.LocalEndPoint,e.LogEntry.RemoteEndPoint);
                }
                else if(e.LogEntry.EntryType == LogEntryType.Exception){
                    m_pServer.Logger.AddException(m_SessionID,e.LogEntry.UserIdentity,e.LogEntry.Text,e.LogEntry.LocalEndPoint,e.LogEntry.RemoteEndPoint,e.LogEntry.Exception);
                }
            }
            catch{
            }
        }

        #endregion

        #region method LogText

        /// <summary>
        /// Logs specified text if logging enabled.
        /// </summary>
        /// <param name="text">Text to log.</param>
        private void LogText(string text)
        {
            if(m_pServer.Logger != null){
                GenericIdentity identity = null;
                try{
                    identity = this.AuthenticatedUserIdentity;
                }
                catch{
                }
                IPEndPoint localEP  = null;
                IPEndPoint remoteEP = null;
                try{
                    localEP  = m_pSmtpClient.LocalEndPoint;
                    remoteEP = m_pSmtpClient.RemoteEndPoint;
                }
                catch{
                }
                m_pServer.Logger.AddText(m_SessionID,identity,text,localEP,remoteEP);
            }
        }

        #endregion

        #region method IsDsnSupported

        /// <summary>
        /// Gets if DSN extention is supported by remote server.
        /// </summary>
        /// <returns></returns>
        private bool IsDsnSupported()
        {
            foreach(string feature in m_pSmtpClient.EsmtpFeatures){
                if(string.Equals(feature,SMTP_ServiceExtensions.DSN,StringComparison.InvariantCultureIgnoreCase)){
                    return true;
                }
            }

            return false;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets if this object is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get{ return m_IsDisposed; }
        }

        /// <summary>
        /// Gets local host name for LoaclEP.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string LocalHostName
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return (m_pLocalBindInfo == null ? "" : m_pLocalBindInfo.HostName); 
            }
        }

        /// <summary>
        /// Gets time when relay session created.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public DateTime SessionCreateTime
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_SessionCreateTime; 
            }
        }

        /// <summary>
        /// Gets how many seconds has left before timout is triggered.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public int ExpectedTimeout
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return (int)(m_pServer.SessionIdleTimeout - ((DateTime.Now.Ticks - this.TcpStream.LastActivity.Ticks) / 10000));
            }
        }

        /// <summary>
        /// Gets from address.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string From
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pRelayItem.From; 
            }
        }

        /// <summary>
        /// Gets target recipient.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string To
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_pRelayItem.To; 
            }
        }

        /// <summary>
        /// Gets message ID which is being relayed now.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string MessageID
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pRelayItem.MessageID; 
            }
        }

        /// <summary>
        /// Gets message what is being relayed now.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public Stream MessageStream
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pRelayItem.MessageStream; 
            }
        }

        /// <summary>
        /// Gets current remote host name. Returns null if not connected to any target.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public string RemoteHostName
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                if(m_pActiveTarget != null){
                    return m_pActiveTarget.HostName;
                }

                return null;
            }
        }

        /// <summary>
        /// Gets relay queue which session it is.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public Relay_Queue Queue
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pRelayItem.Queue; 
            }
        }

        /// <summary>
        /// Gets user data what was procided to Relay_Queue.QueueMessage method.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public object QueueTag
        {
            get{               
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pRelayItem.Tag; 
            }
        }


        /// <summary>
        /// Gets session authenticated user identity, returns null if not authenticated.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and relay session is not connected.</exception>
        public override GenericIdentity AuthenticatedUserIdentity
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!m_pSmtpClient.IsConnected){
				    throw new InvalidOperationException("You must connect first.");
			    }

                return m_pSmtpClient.AuthenticatedUserIdentity; 
            }
        }

        /// <summary>
        /// Gets if session is connected.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override bool IsConnected
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSmtpClient.IsConnected; 
            }
        }

        /// <summary>
        /// Gets session ID.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override string ID
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_SessionID; 
            }
        }

        /// <summary>
        /// Gets the time when session was connected.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override DateTime ConnectTime
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSmtpClient.ConnectTime; 
            }
        }

        /// <summary>
        /// Gets the last time when data was sent or received.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override DateTime LastActivity
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSmtpClient.LastActivity; 
            }
        }

        /// <summary>
        /// Gets session local IP end point.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override IPEndPoint LocalEndPoint
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSmtpClient.LocalEndPoint; 
            }
        }

        /// <summary>
        /// Gets session remote IP end point.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override IPEndPoint RemoteEndPoint
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSmtpClient.RemoteEndPoint; 
            }
        }

        /// <summary>
        /// Gets TCP stream which must be used to send/receive data through this session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override SmartStream TcpStream
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pSmtpClient.TcpStream; 
            }
        }

        #endregion

    }
}
