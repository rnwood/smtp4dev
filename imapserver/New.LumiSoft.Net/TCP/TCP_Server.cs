using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using LumiSoft.Net.Log;

namespace LumiSoft.Net.TCP
{
    /// <summary>
    /// This class implements generic TCP session based server.
    /// </summary>
    public class TCP_Server<T> : IDisposable where T : TCP_ServerSession, new()
    {
        #region class ListeningPoint

        /// <summary>
        /// This class holds listening point info.
        /// </summary>
        public class ListeningPoint
        {
            private Socket m_pSocket = null;
            private IPBindInfo m_pBindInfo = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="socket">Listening socket.</param>
            /// <param name="bind">Bind info what acceped socket.</param>
            public ListeningPoint(Socket socket, IPBindInfo bind)
            {
                m_pSocket = socket;
                m_pBindInfo = bind;
            }


            #region Properties Implementation

            /// <summary>
            /// Gets socket.
            /// </summary>
            public Socket Socket
            {
                get { return m_pSocket; }
            }

            /// <summary>
            /// Gets bind info.
            /// </summary>
            public IPBindInfo BindInfo
            {
                get { return m_pBindInfo; }
            }

            #endregion

        }

        #endregion

        #region class TCP_Acceptor

        /// <summary>
        /// Implements single TCP connection acceptor.
        /// </summary>
        /// <remarks>For higher performance, mutiple acceptors per socket must be created.</remarks>
        private class TCP_Acceptor : IDisposable
        {
            private bool m_IsDisposed = false;
            private bool m_IsRunning = false;
            private Socket m_pSocket = null;
            private SocketAsyncEventArgs m_pSocketArgs = null;
            private Dictionary<string, object> m_pTags = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="socket">Socket.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>socket</b> is null reference.</exception>
            public TCP_Acceptor(Socket socket)
            {
                if (socket == null)
                {
                    throw new ArgumentNullException("socket");
                }

                m_pSocket = socket;

                m_pTags = new Dictionary<string, object>();
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if (m_IsDisposed)
                {
                    return;
                }
                m_IsDisposed = true;

                m_pSocket = null;
                m_pSocketArgs = null;
                m_pTags = null;

                this.ConnectionAccepted = null;
                this.Error = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts accpeting connections.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this calss is disposed and this method is accessed.</exception>
            public void Start()
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if (m_IsRunning)
                {
                    return;
                }
                m_IsRunning = true;

                // Move processing to thread pool.
                ThreadPool.QueueUserWorkItem(delegate (object state)
                {
                    try
                    {
                        #region IO completion ports

                        if (Net_Utils.IsSocketAsyncSupported())
                        {
                            m_pSocketArgs = new SocketAsyncEventArgs();
                            m_pSocketArgs.Completed += delegate (object s1, SocketAsyncEventArgs e1)
                            {
                                if (m_IsDisposed)
                                {
                                    return;
                                }

                                try
                                {
                                    if (m_pSocketArgs.SocketError == SocketError.Success)
                                    {
                                        OnConnectionAccepted(m_pSocketArgs.AcceptSocket);
                                    }
                                    else
                                    {
                                        OnError(new Exception("Socket error '" + m_pSocketArgs.SocketError + "'."));
                                    }

                                    IOCompletionAccept();
                                }
                                catch (Exception x)
                                {
                                    OnError(x);
                                }
                            };

                            IOCompletionAccept();
                        }

                        #endregion

                        #region Async sockets

                        else
                        {
                            m_pSocket.BeginAccept(new AsyncCallback(this.AsyncSocketAccept), null);
                        }

                        #endregion
                    }
                    catch (Exception x)
                    {
                        OnError(x);
                    }
                });
            }

            #endregion


            #region method IOCompletionAccept

            /// <summary>
            /// Accpets connection synchornously(if connection(s) available now) or starts waiting TCP connection asynchronously if no connections at moment.
            /// </summary>
            private void IOCompletionAccept()
            {
                try
                {
                    // We need to clear it, before reuse.
                    m_pSocketArgs.AcceptSocket = null;

                    // Use active worker thread as long as ReceiveFromAsync completes synchronously.
                    // (With this approach we don't have thread context switches while ReceiveFromAsync completes synchronously)
                    while (!m_IsDisposed && !m_pSocket.AcceptAsync(m_pSocketArgs))
                    {
                        if (m_pSocketArgs.SocketError == SocketError.Success)
                        {
                            try
                            {
                                OnConnectionAccepted(m_pSocketArgs.AcceptSocket);

                                // We need to clear it, before reuse.
                                m_pSocketArgs.AcceptSocket = null;
                            }
                            catch (Exception x)
                            {
                                OnError(x);
                            }
                        }
                        else
                        {
                            OnError(new Exception("Socket error '" + m_pSocketArgs.SocketError + "'."));
                        }
                    }
                }
                catch (Exception x)
                {
                    OnError(x);
                }
            }

            #endregion

            #region method AsyncSocketAccept

            /// <summary>
            /// Is called BeginAccept has completed.
            /// </summary>
            /// <param name="ar">The result of the asynchronous operation.</param>
            private void AsyncSocketAccept(IAsyncResult ar)
            {
                if (m_IsDisposed)
                {
                    return;
                }

                try
                {
                    OnConnectionAccepted(m_pSocket.EndAccept(ar));
                }
                catch (Exception x)
                {
                    OnError(x);
                }

                try
                {
                    m_pSocket.BeginAccept(new AsyncCallback(this.AsyncSocketAccept), null);
                }
                catch (Exception x)
                {
                    OnError(x);
                }
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets user data items.
            /// </summary>
            public Dictionary<string, object> Tags
            {
                get { return m_pTags; }
            }

            #endregion

            #region Events handling

            /// <summary>
            /// Is raised when new TCP connection was accepted.
            /// </summary>
            public event EventHandler<EventArgs<Socket>> ConnectionAccepted = null;

            #region method OnConnectionAccepted

            /// <summary>
            /// Raises <b>ConnectionAccepted</b> event.
            /// </summary>
            /// <param name="socket">Accepted socket.</param>
            private void OnConnectionAccepted(Socket socket)
            {
                if (this.ConnectionAccepted != null)
                {
                    this.ConnectionAccepted(this, new EventArgs<Socket>(socket));
                }
            }

            #endregion

            /// <summary>
            /// Is raised when unhandled error happens.
            /// </summary>
            public event EventHandler<ExceptionEventArgs> Error = null;

            #region method OnError

            /// <summary>
            /// Raises <b>Error</b> event.
            /// </summary>
            /// <param name="x">Exception happened.</param>
            private void OnError(Exception x)
            {
                if (this.Error != null)
                {
                    this.Error(this, new ExceptionEventArgs(x));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        private bool m_IsDisposed = false;
        private bool m_IsRunning = false;
        private IPBindInfo[] m_pBindings = new IPBindInfo[0];
        private long m_MaxConnections = 0;
        private long m_MaxConnectionsPerIP = 0;
        private int m_SessionIdleTimeout = 100;
        private Logger m_pLogger = null;
        private DateTime m_StartTime;
        private long m_ConnectionsProcessed = 0;
        private List<TCP_Acceptor> m_pConnectionAcceptors = null;
        private List<ListeningPoint> m_pListeningPoints = null;
        private TCP_SessionCollection<TCP_ServerSession> m_pSessions = null;
        private TimerEx m_pTimer_IdleTimeout = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TCP_Server()
        {
            m_pConnectionAcceptors = new List<TCP_Server<T>.TCP_Acceptor>();
            m_pListeningPoints = new List<TCP_Server<T>.ListeningPoint>();
            m_pSessions = new TCP_SessionCollection<TCP_ServerSession>();
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public void Dispose()
        {
            if (m_IsDisposed)
            {
                return;
            }
            if (m_IsRunning)
            {
                try
                {
                    Stop();
                }
                catch
                {
                }
            }
            m_IsDisposed = true;

            // We must call disposed event before we release events.
            try
            {
                OnDisposed();
            }
            catch
            {
                // We never should get exception here, user should handle it, just skip it.
            }

            m_pSessions = null;

            // Release all events.
            this.Started = null;
            this.Stopped = null;
            this.Disposed = null;
            this.Error = null;
        }

        #endregion


        #region Events handling

        #region method m_pTimer_IdleTimeout_Elapsed

        /// <summary>
        /// Is called when session idle check timer triggered.
        /// </summary>
        /// <param name="sender">Sender.</param>
        /// <param name="e">Event data.</param>
        private void m_pTimer_IdleTimeout_Elapsed(object sender, System.Timers.ElapsedEventArgs e)
        {
            try
            {
                foreach (T session in this.Sessions.ToArray())
                {
                    try
                    {
                        if (DateTime.Now > session.TcpStream.LastActivity.AddSeconds(m_SessionIdleTimeout))
                        {
                            ;
                            session.OnTimeoutI();
                            // Session didn't dispose itself, so dispose it.
                            if (!session.IsDisposed)
                            {
                                session.Disconnect();
                                session.Dispose();
                            }
                        }
                    }
                    catch
                    {
                    }
                }
            }
            catch (Exception x)
            {
                OnError(x);
            }
        }

        #endregion

        #endregion


        #region method Start

        /// <summary>
        /// Starts TCP server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public void Start()
        {
            if (m_IsDisposed)
            {
                throw new ObjectDisposedException("TCP_Server");
            }
            if (m_IsRunning)
            {
                return;
            }
            m_IsRunning = true;

            m_StartTime = DateTime.Now;
            m_ConnectionsProcessed = 0;

            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate (object state)
            {
                StartListen();
            }));

            m_pTimer_IdleTimeout = new TimerEx(30000, true);
            m_pTimer_IdleTimeout.Elapsed += new System.Timers.ElapsedEventHandler(m_pTimer_IdleTimeout_Elapsed);
            m_pTimer_IdleTimeout.Enabled = true;
        }

        #endregion

        #region method Stop

        /// <summary>
        /// Stops TCP server, all active connections will be terminated.
        /// </summary>
        public void Stop()
        {
            if (!m_IsRunning)
            {
                return;
            }
            m_IsRunning = false;

            // Dispose all old binds.
            foreach (ListeningPoint listeningPoint in m_pListeningPoints.ToArray())
            {
                try
                {
                    listeningPoint.Socket.Close();
                }
                catch (Exception x)
                {
                    OnError(x);
                }
            }
            m_pListeningPoints.Clear();

            m_pTimer_IdleTimeout.Dispose();
            m_pTimer_IdleTimeout = null;

            OnStopped();
        }

        #endregion

        #region method Restart

        /// <summary>
        /// Restarts TCP server.
        /// </summary>
        public void Restart()
        {
            Stop();
            Start();
        }

        #endregion


        #region virtual method OnMaxConnectionsExceeded

        /// <summary>
        /// Is called when new incoming session and server maximum allowed connections exceeded.
        /// </summary>
        /// <param name="session">Incoming session.</param>
        /// <remarks>This method allows inhereted classes to report error message to connected client.
        /// Session will be disconnected after this method completes.
        /// </remarks>
        protected virtual void OnMaxConnectionsExceeded(T session)
        {
        }

        #endregion

        #region virtual method OnMaxConnectionsPerIPExceeded

        /// <summary>
        /// Is called when new incoming session and server maximum allowed connections per connected IP exceeded.
        /// </summary>
        /// <param name="session">Incoming session.</param>
        /// <remarks>This method allows inhereted classes to report error message to connected client.
        /// Session will be disconnected after this method completes.
        /// </remarks>
        protected virtual void OnMaxConnectionsPerIPExceeded(T session)
        {
        }

        #endregion


        #region method StartListen

        /// <summary>
        /// Starts listening incoming connections. NOTE: All active listening points will be disposed.
        /// </summary>
        private void StartListen()
        {
            try
            {
                // Dispose all old binds.
                foreach (ListeningPoint listeningPoint in m_pListeningPoints.ToArray())
                {
                    try
                    {
                        listeningPoint.Socket.Close();
                    }
                    catch (Exception x)
                    {
                        OnError(x);
                    }
                }
                m_pListeningPoints.Clear();

                // Create new listening points and start accepting connections.
                foreach (IPBindInfo bind in m_pBindings)
                {
                    try
                    {
                        Socket socket = null;
                        if (bind.IP.AddressFamily == AddressFamily.InterNetwork)
                        {
                            socket = new Socket(AddressFamily.InterNetwork, SocketType.Stream, ProtocolType.Tcp);
                        }
                        else if (bind.IP.AddressFamily == AddressFamily.InterNetworkV6)
                        {
                            socket = new Socket(AddressFamily.InterNetworkV6, SocketType.Stream, ProtocolType.Tcp);
                            socket.DualMode = true;
                        }
                        else
                        {
                            // Invalid address family, just skip it.
                            continue;
                        }
                        socket.Bind(new IPEndPoint(bind.IP, bind.Port));
                        socket.Listen(100);

                        ListeningPoint listeningPoint = new ListeningPoint(socket, bind);
                        m_pListeningPoints.Add(listeningPoint);

                        // Create TCP connection acceptors.
                        for (int i = 0; i < 10; i++)
                        {
                            TCP_Acceptor acceptor = new TCP_Server<T>.TCP_Acceptor(socket);
                            acceptor.Tags["bind"] = bind;
                            acceptor.ConnectionAccepted += delegate (object s1, EventArgs<Socket> e1)
                            {
                                // NOTE: We may not use 'bind' variable here, foreach changes it's value before we reach here.
                                ProcessConnection(e1.Value, (IPBindInfo)acceptor.Tags["bind"]);
                            };
                            acceptor.Error += delegate (object s1, ExceptionEventArgs e1)
                            {
                                OnError(e1.Exception);
                            };
                            m_pConnectionAcceptors.Add(acceptor);
                            acceptor.Start();
                        }

                        OnStarted();
                    }
                    catch (Exception x)
                    {
                        // The only exception what we should get there is if socket is in use.
                        OnError(x);
                    }
                }
            }
            catch (Exception x)
            {
                OnError(x);
            }
        }

        #endregion

        #region method ProcessConnection

        /// <summary>
        /// Processes specified connection.
        /// </summary>
        /// <param name="socket">Accpeted socket.</param>
        /// <param name="bindInfo">Local bind info what accpeted connection.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>socket</b> or <b>bindInfo</b> is null reference.</exception>
        private void ProcessConnection(Socket socket, IPBindInfo bindInfo)
        {
            if (socket == null)
            {
                throw new ArgumentNullException("socket");
            }
            if (bindInfo == null)
            {
                throw new ArgumentNullException("bindInfo");
            }

            m_ConnectionsProcessed++;

            try
            {
                T session = new T();
                session.Init(this, socket, bindInfo.HostName, bindInfo.SslMode == SslMode.SSL, bindInfo.Certificate);

                // Maximum allowed connections exceeded, reject connection.
                if (m_MaxConnections != 0 && m_pSessions.Count > m_MaxConnections)
                {
                    OnMaxConnectionsExceeded(session);
                    session.Dispose();
                }
                // Maximum allowed connections per IP exceeded, reject connection.
                else if (m_MaxConnectionsPerIP != 0 && m_pSessions.GetConnectionsPerIP(session.RemoteEndPoint.Address) > m_MaxConnectionsPerIP)
                {
                    OnMaxConnectionsPerIPExceeded(session);
                    session.Dispose();
                }
                // Start processing new session.
                else
                {
                    session.Disonnected += new EventHandler(delegate (object sender, EventArgs e)
                    {
                        m_pSessions.Remove((TCP_ServerSession)sender);
                    });
                    m_pSessions.Add(session);

                    OnSessionCreated(session);

                    session.StartI();
                }
            }
            catch (Exception x)
            {
                OnError(x);
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets if server is disposed.
        /// </summary>
        public bool IsDisposed
        {
            get { return m_IsDisposed; }
        }

        /// <summary>
        /// Gets if server is running.
        /// </summary>
        public bool IsRunning
        {
            get { return m_IsRunning; }
        }

        /// <summary>
        /// Gets or sets TCP server IP bindings.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public IPBindInfo[] Bindings
        {
            get
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pBindings;
            }

            set
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if (value == null)
                {
                    value = new IPBindInfo[0];
                }

                //--- See binds has changed --------------
                bool changed = false;
                if (m_pBindings.Length != value.Length)
                {
                    changed = true;
                }
                else
                {
                    for (int i = 0; i < m_pBindings.Length; i++)
                    {
                        if (!m_pBindings[i].Equals(value[i]))
                        {
                            changed = true;
                            break;
                        }
                    }
                }

                if (changed)
                {
                    m_pBindings = value;

                    if (m_IsRunning)
                    {
                        StartListen();
                    }
                }
            }
        }

        public ListeningPoint[] ListeningPoints
        {
            get
            {
                return this.m_pListeningPoints.ToArray();
            }
        }

        /// <summary>
        /// Gets local listening IP end points.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public IPEndPoint[] LocalEndPoints
        {
            get
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                List<IPEndPoint> retVal = new List<IPEndPoint>();
                foreach (IPBindInfo bind in this.Bindings)
                {
                    if (bind.IP.Equals(IPAddress.Any))
                    {
                        foreach (IPAddress ip in System.Net.Dns.GetHostAddresses(""))
                        {
                            if (ip.AddressFamily == AddressFamily.InterNetwork && !retVal.Contains(new IPEndPoint(ip, bind.Port)))
                            {
                                retVal.Add(new IPEndPoint(ip, bind.Port));
                            }
                        }
                    }
                    else if (bind.IP.Equals(IPAddress.IPv6Any))
                    {
                        foreach (IPAddress ip in System.Net.Dns.GetHostAddresses(""))
                        {
                            if (ip.AddressFamily == AddressFamily.InterNetworkV6 && !retVal.Contains(new IPEndPoint(ip, bind.Port)))
                            {
                                retVal.Add(new IPEndPoint(ip, bind.Port));
                            }
                        }
                    }
                    else
                    {
                        if (!retVal.Contains(bind.EndPoint))
                        {
                            retVal.Add(bind.EndPoint);
                        }
                    }
                }

                return retVal.ToArray();
            }
        }

        /// <summary>
        /// Gets or sets maximum allowed concurent connections. Value 0 means unlimited.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when negative value is passed.</exception>
        public long MaxConnections
        {
            get
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException("TCP_Server");
                }

                return m_MaxConnections;
            }

            set
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException("TCP_Server");
                }
                if (value < 0)
                {
                    throw new ArgumentException("Property 'MaxConnections' value must be >= 0.");
                }

                m_MaxConnections = value;
            }
        }

        /// <summary>
        /// Gets or sets maximum allowed connections for 1 IP address. Value 0 means unlimited.
        /// </summary>
        public long MaxConnectionsPerIP
        {
            get
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException("TCP_Server");
                }

                return m_MaxConnectionsPerIP;
            }

            set
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException("TCP_Server");
                }
                if (m_MaxConnectionsPerIP < 0)
                {
                    throw new ArgumentException("Property 'MaxConnectionsPerIP' value must be >= 0.");
                }

                m_MaxConnectionsPerIP = value;
            }
        }

        /// <summary>
        /// Gets or sets maximum allowed session idle time in seconds, after what session will be terminated. Value 0 means unlimited,
        /// but this is strongly not recommened.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when negative value is passed.</exception>
        public int SessionIdleTimeout
        {
            get
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException("TCP_Server");
                }

                return m_SessionIdleTimeout;
            }

            set
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException("TCP_Server");
                }
                if (value < 0)
                {
                    throw new ArgumentException("Property 'SessionIdleTimeout' value must be >= 0.");
                }

                m_SessionIdleTimeout = value;
            }
        }

        /// <summary>
        /// Gets or sets logger. Value null means no logging.
        /// </summary>
        public Logger Logger
        {
            get
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pLogger;
            }

            set
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                m_pLogger = value;
            }
        }

        /// <summary>
        /// Gets the time when server was started.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP server is not running and this property is accesed.</exception>
        public DateTime StartTime
        {
            get
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException("TCP_Server");
                }
                if (!m_IsRunning)
                {
                    throw new InvalidOperationException("TCP server is not running.");
                }

                return m_StartTime;
            }
        }

        /// <summary>
        /// Gets how many connections this TCP server has processed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP server is not running and this property is accesed.</exception>
        public long ConnectionsProcessed
        {
            get
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException("TCP_Server");
                }
                if (!m_IsRunning)
                {
                    throw new InvalidOperationException("TCP server is not running.");
                }

                return m_ConnectionsProcessed;
            }
        }

        /// <summary>
        /// Gets TCP server active sessions.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP server is not running and this property is accesed.</exception>
        public TCP_SessionCollection<TCP_ServerSession> Sessions
        {
            get
            {
                if (m_IsDisposed)
                {
                    throw new ObjectDisposedException("TCP_Server");
                }
                if (!m_IsRunning)
                {
                    throw new InvalidOperationException("TCP server is not running.");
                }

                return m_pSessions;
            }
        }


        #endregion

        #region Events Implementation

        /// <summary>
        /// This event is raised when TCP server has started.
        /// </summary>
        public event EventHandler Started = null;

        #region method OnStarted

        /// <summary>
        /// Raises <b>Started</b> event.
        /// </summary>
        protected void OnStarted()
        {
            if (this.Started != null)
            {
                this.Started(this, new EventArgs());
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when TCP server has stopped.
        /// </summary>
        public event EventHandler Stopped = null;

        #region method OnStopped

        /// <summary>
        /// Raises <b>Stopped</b> event.
        /// </summary>
        protected void OnStopped()
        {
            if (this.Stopped != null)
            {
                this.Stopped(this, new EventArgs());
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when TCP server has disposed.
        /// </summary>
        public event EventHandler Disposed = null;

        #region method OnDisposed

        /// <summary>
        /// Raises <b>Disposed</b> event.
        /// </summary>
        protected void OnDisposed()
        {
            if (this.Disposed != null)
            {
                this.Disposed(this, new EventArgs());
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when TCP server creates new session.
        /// </summary>
        public event EventHandler<TCP_ServerSessionEventArgs<T>> SessionCreated = null;

        #region method OnSessionCreated

        /// <summary>
        /// Raises <b>SessionCreated</b> event.
        /// </summary>
        /// <param name="session">TCP server session that was created.</param>
        private void OnSessionCreated(T session)
        {
            if (this.SessionCreated != null)
            {
                this.SessionCreated(this, new TCP_ServerSessionEventArgs<T>(this, session));
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when TCP server has unknown unhandled error.
        /// </summary>
        public event ErrorEventHandler Error = null;

        #region method OnError

        /// <summary>
        /// Raises <b>Error</b> event.
        /// </summary>
        /// <param name="x">Exception happened.</param>
        private void OnError(Exception x)
        {
            if (this.Error != null)
            {
                this.Error(this, new Error_EventArgs(x, new System.Diagnostics.StackTrace()));
            }
        }

        #endregion

        #endregion

    }
}
