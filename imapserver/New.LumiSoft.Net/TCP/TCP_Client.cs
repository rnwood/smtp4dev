using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Net.Security;
using System.Security.Cryptography.X509Certificates;
//using System.Runtime.Remoting.Messaging;
using System.Threading;

using LumiSoft.Net.IO;
using LumiSoft.Net.Log;

namespace LumiSoft.Net.TCP
{    
    /// <summary>
    /// This class implements generic TCP client.
    /// </summary>
    public class TCP_Client : TCP_Session
    {
        private bool                                m_IsDisposed           = false;
        private bool                                m_IsConnected          = false;
        private string                              m_ID                   = "";
        private DateTime                            m_ConnectTime;
        private IPEndPoint                          m_pLocalEP             = null;
        private IPEndPoint                          m_pRemoteEP            = null;
        private bool                                m_IsSecure             = false;
        private SmartStream                         m_pTcpStream           = null;
        private Logger                              m_pLogger              = null;
        private RemoteCertificateValidationCallback m_pCertificateCallback = null;
        private int                                 m_Timeout              = 61000;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public TCP_Client()
        {
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used. This method is thread-safe.
        /// </summary>
        public override void Dispose()
        {
            lock(this){
                if(m_IsDisposed){
                    return;
                }
                try{
                    Disconnect();
                }
                catch{
                }
                m_IsDisposed = true;
            }
        }

        #endregion

                
        #region method Connect

        /// <summary>
        /// Connects to the specified host. If the hostname resolves to more than one IP address, 
        /// all IP addresses will be tried for connection, until one of them connects.
        /// </summary>
        /// <param name="host">Host name or IP address.</param>
        /// <param name="port">Port to connect.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is already connected.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public void Connect(string host,int port)
        {
            Connect(host,port,false);
        }

        /// <summary>
        /// Connects to the specified host. If the hostname resolves to more than one IP address, 
        /// all IP addresses will be tried for connection, until one of them connects.
        /// </summary>
        /// <param name="host">Host name or IP address.</param>
        /// <param name="port">Port to connect.</param>
        /// <param name="ssl">Specifies if connects to SSL end point.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is already connected.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public void Connect(string host,int port,bool ssl)
        {            
            if(m_IsDisposed){
                throw new ObjectDisposedException("TCP_Client");
            }
            if(m_IsConnected){
                throw new InvalidOperationException("TCP client is already connected.");
            }
            if(string.IsNullOrEmpty(host)){
                throw new ArgumentException("Argument 'host' value may not be null or empty.");
            }
            if(port < 1){
                throw new ArgumentException("Argument 'port' value must be >= 1.");
            }

            IPAddress[] ips = System.Net.Dns.GetHostAddresses(host);
            for(int i=0;i<ips.Length;i++){
                try{
                    Connect(null,new IPEndPoint(ips[i],port),ssl);
                    break;
                }
                catch(Exception x){
                    if(this.IsConnected){
                        throw x;
                    }
                    // Connect failed for specified IP address, if there are some more IPs left, try next, otherwise forward exception.
                    else if(i == (ips.Length - 1)){
                        throw x;
                    }
                }
            }
        }

        /// <summary>
        /// Connects to the specified remote end point.
        /// </summary>
        /// <param name="remoteEP">Remote IP end point where to connect.</param>
        /// <param name="ssl">Specifies if connects to SSL end point.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is already connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>remoteEP</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public void Connect(IPEndPoint remoteEP,bool ssl)
        {
            Connect(null,remoteEP,ssl);
        }

        /// <summary>
        /// Connects to the specified remote end point.
        /// </summary>
        /// <param name="localEP">Local IP end point to use. Value null means that system will allocate it.</param>
        /// <param name="remoteEP">Remote IP end point to connect.</param>
        /// <param name="ssl">Specifies if connection switches to SSL affter connect.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is already connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>remoteEP</b> is null reference.</exception>
        public void Connect(IPEndPoint localEP,IPEndPoint remoteEP,bool ssl)
        {
            Connect(localEP,remoteEP,ssl,null);
        }

        /// <summary>
        /// Connects to the specified remote end point.
        /// </summary>
        /// <param name="localEP">Local IP end point to use. Value null means that system will allocate it.</param>
        /// <param name="remoteEP">Remote IP end point to connect.</param>
        /// <param name="ssl">Specifies if connection switches to SSL affter connect.</param>
        /// <param name="certCallback">SSL server certificate validation callback. Value null means any certificate is accepted.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is already connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>remoteEP</b> is null reference.</exception>
        public void Connect(IPEndPoint localEP,IPEndPoint remoteEP,bool ssl,RemoteCertificateValidationCallback certCallback)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(m_IsConnected){
                throw new InvalidOperationException("TCP client is already connected.");
            }
            if(remoteEP == null){
                throw new ArgumentNullException("remoteEP");
            }

            ManualResetEvent wait = new ManualResetEvent(false);
            using(ConnectAsyncOP op = new ConnectAsyncOP(localEP,remoteEP,ssl,certCallback)){
                op.CompletedAsync += delegate(object s1,EventArgs<ConnectAsyncOP> e1){
                    wait.Set();
                };
                if(!this.ConnectAsync(op)){
                    wait.Set();
                }
                wait.WaitOne();
                wait.Close();

                if(op.Error != null){
                    throw op.Error;
                }
            }
        }

        #endregion

        #region method ConnectAsync

        #region class ConnectAsyncOP

        /// <summary>
        /// This class represents <see cref="TCP_Client.ConnectAsync"/> asynchronous operation.
        /// </summary>
        public class ConnectAsyncOP : IDisposable,IAsyncOP
        {
            private object                              m_pLock         = new object();
            private AsyncOP_State                       m_State         = AsyncOP_State.WaitingForStart;
            private Exception                           m_pException    = null;
            private IPEndPoint                          m_pLocalEP      = null;
            private IPEndPoint                          m_pRemoteEP     = null;
            private bool                                m_SSL           = false;
            private RemoteCertificateValidationCallback m_pCertCallback = null;
            private TCP_Client                          m_pTcpClient    = null;
            private Socket                              m_pSocket       = null;
            private Stream                              m_pStream       = null;
            private bool                                m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="localEP">Local IP end point to use. Value null means that system will allocate it.</param>
            /// <param name="remoteEP">Remote IP end point to connect.</param>
            /// <param name="ssl">Specifies if connection switches to SSL affter connect.</param>
            /// <param name="certCallback">SSL server certificate validation callback. Value null means any certificate is accepted.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>remoteEP</b> is null reference.</exception>
            public ConnectAsyncOP(IPEndPoint localEP,IPEndPoint remoteEP,bool ssl,RemoteCertificateValidationCallback certCallback)
            {
                if(remoteEP == null){
                    throw new ArgumentNullException("localEP");
                }

                m_pLocalEP      = localEP;
                m_pRemoteEP     = remoteEP;
                m_SSL           = ssl;
                m_pCertCallback = certCallback;
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

                m_pException    = null;
                m_pLocalEP      = null;
                m_pRemoteEP     = null;
                m_SSL           = false;
                m_pCertCallback = null;
                m_pTcpClient    = null;
                m_pSocket       = null;
                m_pStream       = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner TCP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(TCP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pTcpClient = owner;

                SetState(AsyncOP_State.Active);

                try{
                    // Create socket.
                    if(m_pRemoteEP.AddressFamily == AddressFamily.InterNetwork){
                        m_pSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);
                        m_pSocket.ReceiveTimeout = m_pTcpClient.m_Timeout;
                        m_pSocket.SendTimeout = m_pTcpClient.m_Timeout;
                    }
                    else if(m_pRemoteEP.AddressFamily == AddressFamily.InterNetworkV6){
                        m_pSocket = new Socket(AddressFamily.InterNetworkV6,SocketType.Stream,ProtocolType.Tcp);
                        m_pSocket.ReceiveTimeout = m_pTcpClient.m_Timeout;
                        m_pSocket.SendTimeout = m_pTcpClient.m_Timeout;
                    }
                    // Bind socket to the specified end point.
                    if(m_pLocalEP != null){
                        m_pSocket.Bind(m_pLocalEP);
                    }

                    m_pTcpClient.LogAddText("Connecting to " + m_pRemoteEP.ToString() + ".");

                    // Start connecting.
                    m_pSocket.BeginConnect(m_pRemoteEP,this.BeginConnectCompleted,null);
                }
                catch(Exception x){
                    m_pException = x;
                    CleanupSocketRelated();
                    m_pTcpClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);

                    return false;
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

            #region method BeginConnectCompleted

            /// <summary>
            /// This method is called when "BeginConnect" has completed.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void BeginConnectCompleted(IAsyncResult ar)
            {
                try{
                    m_pSocket.EndConnect(ar);

                    m_pTcpClient.LogAddText("Connected, localEP='" + m_pSocket.LocalEndPoint.ToString() + "'; remoteEP='" + m_pSocket.RemoteEndPoint.ToString() + "'.");

                    // Start SSL handshake.
                    if(m_SSL){
                        m_pTcpClient.LogAddText("Starting SSL handshake.");

                        m_pStream = new SslStream(new NetworkStream(m_pSocket,true),false,this.RemoteCertificateValidationCallback);
                        ((SslStream)m_pStream).BeginAuthenticateAsClient("dummy",this.BeginAuthenticateAsClientCompleted,null);
                    }
                    // We are done.
                    else{
                        m_pStream = new NetworkStream(m_pSocket,true);

                        InternalConnectCompleted();
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    CleanupSocketRelated();
                    m_pTcpClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method BeginAuthenticateAsClientCompleted

            /// <summary>
            /// This method is called when "BeginAuthenticateAsClient" has completed.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void BeginAuthenticateAsClientCompleted(IAsyncResult ar)
            {
                try{
                    ((SslStream)m_pStream).EndAuthenticateAsClient(ar);

                    m_pTcpClient.LogAddText("SSL handshake completed sucessfully.");

                    InternalConnectCompleted();
                }
                catch(Exception x){
                    m_pException = x;
                    CleanupSocketRelated();
                    m_pTcpClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method RemoteCertificateValidationCallback

            /// <summary>
            /// This method is called when we need to validate remote server certificate.
            /// </summary>
            /// <param name="sender">Sender.</param>
            /// <param name="certificate">Certificate.</param>
            /// <param name="chain">Certificate chain.</param>
            /// <param name="sslPolicyErrors">SSL policy errors.</param>
            /// <returns>Returns true if certificate validated, otherwise false.</returns>
            private bool RemoteCertificateValidationCallback(object sender,X509Certificate certificate,X509Chain chain,SslPolicyErrors sslPolicyErrors)
            {
                // User will handle it.
                if(m_pCertCallback != null){
                    return m_pCertCallback(sender,certificate,chain,sslPolicyErrors);
                }
                else{
                    if(sslPolicyErrors == SslPolicyErrors.None || ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) > 0)){
                        return true;
                    }

                    // Do not allow this client to communicate with unauthenticated servers.
                    return false;
                }
            }

            #endregion

            #region method CleanupSocketRelated

            /// <summary>
            /// Cleans up any socket related resources.
            /// </summary>
            private void CleanupSocketRelated()
            {
                try{                    
                    if(m_pStream != null){
                        m_pStream.Dispose();
                    }
                    if(m_pSocket != null){
                        m_pSocket.Close();
                    }
                }
                catch{
                }
            }

            #endregion

            #region method InternalConnectCompleted

            /// <summary>
            /// Is called when when connecting has finished.
            /// </summary>
            private void InternalConnectCompleted()
            {
                m_pTcpClient.m_IsConnected = true;
                m_pTcpClient.m_ID          = Guid.NewGuid().ToString();
                m_pTcpClient.m_ConnectTime = DateTime.Now;
                m_pTcpClient.m_pLocalEP    = (IPEndPoint)m_pSocket.LocalEndPoint;
                m_pTcpClient.m_pRemoteEP   = (IPEndPoint)m_pSocket.RemoteEndPoint;
                m_pTcpClient.m_pTcpStream  = new SmartStream(m_pStream,true);
                m_pTcpClient.m_pTcpStream.Encoding = Encoding.UTF8;

                m_pTcpClient.OnConnected(this.CompleteConnectCallback);
            }

            #endregion

            #region method CompleteConnectCallback

            /// <summary>
            /// This method is called when this derrived class OnConnected processing has completed.
            /// </summary>
            /// <param name="error">Exception happened or null if no errors.</param>
            private void CompleteConnectCallback(Exception error)
            {
                m_pException = error;
                                
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
            /// Gets connected socket.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public Socket Socket
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Socket' is accessible only in 'AsyncOP_State.Completed' state.");
                    }
                    if(m_pException != null){
                        throw m_pException;
                    }

                    return m_pSocket; 
                }
            }

            /// <summary>
            /// Gets connected TCP stream.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public Stream Stream
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Stream' is accessible only in 'AsyncOP_State.Completed' state.");
                    }
                    if(m_pException != null){
                        throw m_pException;
                    }

                    return m_pStream; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<ConnectAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<ConnectAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts connecting to remote end point.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="ConnectAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool ConnectAsync(ConnectAsyncOP op)
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

        #region method Disconnect

        /// <summary>
        /// Disconnects connection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is not connected.</exception>
        public override void Disconnect()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("TCP_Client");
            }
            if(!m_IsConnected){
                throw new InvalidOperationException("TCP client is not connected.");
            }
            m_IsConnected = false;

            m_pLocalEP = null;
            m_pRemoteEP = null;
            m_pTcpStream.Dispose();
            m_IsSecure = false;
            m_pTcpStream = null;

            LogAddText("Disconnected.");
        }

        #endregion

        #region method BeginDisconnect

        /// <summary>
        /// Internal helper method for asynchronous Disconnect method.
        /// </summary>
        private delegate void DisconnectDelegate();

        /// <summary>
        /// Starts disconnecting connection.
        /// </summary>
        /// <param name="callback">Callback to call when the asynchronous operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous disconnect.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is not connected.</exception>
        public IAsyncResult BeginDisconnect(AsyncCallback callback,object state)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!m_IsConnected){
                throw new InvalidOperationException("TCP client is not connected.");
            }

            DisconnectDelegate asyncMethod = new DisconnectDelegate(this.Disconnect);
            AsyncResultState asyncState = new AsyncResultState(this,asyncMethod,callback,state);
            asyncState.SetAsyncResult(asyncMethod.BeginInvoke(new AsyncCallback(asyncState.CompletedCallback),null));

            return asyncState;
        }

        #endregion

        #region method EndDisconnect

        /// <summary>
        /// Ends a pending asynchronous disconnect request.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that stores state information and any user defined data for this asynchronous operation.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when argument <b>asyncResult</b> was not returned by a call to the <b>BeginDisconnect</b> method.</exception>
        /// <exception cref="InvalidOperationException">Is raised when <b>EndDisconnect</b> was previously called for the asynchronous connection.</exception>
        public void EndDisconnect(IAsyncResult asyncResult)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }
            
            AsyncResultState castedAsyncResult = asyncResult as AsyncResultState;
            if(castedAsyncResult == null || castedAsyncResult.AsyncObject != this){
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginDisconnect method.");
            }
            if(castedAsyncResult.IsEndCalled){
                throw new InvalidOperationException("EndDisconnect was previously called for the asynchronous connection.");
            }
             
            castedAsyncResult.IsEndCalled = true;
            if(castedAsyncResult.AsyncDelegate is DisconnectDelegate){
                ((DisconnectDelegate)castedAsyncResult.AsyncDelegate).EndInvoke(castedAsyncResult.AsyncResult);
            }
            else{
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginDisconnect method.");
            }
        }

        #endregion


        #region method SwitchToSecure

        /// <summary>
        /// Switches session to secure connection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is not connected or is already secure.</exception>
        protected void SwitchToSecure()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException("TCP_Client");
            }
            if(!m_IsConnected){
                throw new InvalidOperationException("TCP client is not connected.");
            }
            if(m_IsSecure){
                throw new InvalidOperationException("TCP client is already secure.");
            }

            LogAddText("Switching to SSL.");

            // FIX ME: if ssl switching fails, it closes source stream or otherwise if ssl successful, source stream leaks.

            SslStream sslStream = new SslStream(m_pTcpStream.SourceStream,true,this.RemoteCertificateValidationCallback);
            sslStream.AuthenticateAsClient("dummy");

            // Close old stream, but leave source stream open.
            m_pTcpStream.IsOwner = false;
            m_pTcpStream.Dispose();

            m_IsSecure = true;
            m_pTcpStream = new SmartStream(sslStream,true);
        }

        #region method RemoteCertificateValidationCallback

        private bool RemoteCertificateValidationCallback(object sender,X509Certificate certificate,X509Chain chain,SslPolicyErrors sslPolicyErrors)
        {
            // User will handle it.
            if(m_pCertificateCallback != null){
                return m_pCertificateCallback(sender,certificate,chain,sslPolicyErrors);
            }
            else{
                if(sslPolicyErrors == SslPolicyErrors.None || ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) > 0)){
                    return true;
                }

                // Do not allow this client to communicate with unauthenticated servers.
                return false;
            }
        }

        #endregion

        #endregion

        #region method SwitchToSecureAsync

        #region class SwitchToSecureAsyncOP

        /// <summary>
        /// This class represents <see cref="TCP_Client.SwitchToSecureAsync"/> asynchronous operation.
        /// </summary>
        protected class SwitchToSecureAsyncOP : IDisposable,IAsyncOP
        {
            private object                              m_pLock         = new object();
            private bool                                m_RiseCompleted = false;
            private AsyncOP_State                       m_State         = AsyncOP_State.WaitingForStart;
            private Exception                           m_pException    = null;
            private RemoteCertificateValidationCallback m_pCertCallback = null;
            private TCP_Client                          m_pTcpClient    = null;
            private SslStream                           m_pSslStream    = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="certCallback">SSL server certificate validation callback. Value null means any certificate is accepted.</param>
            public SwitchToSecureAsyncOP(RemoteCertificateValidationCallback certCallback)
            {
                m_pCertCallback = certCallback;
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
                
                m_pException    = null;
                m_pCertCallback = null;
                m_pSslStream    = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner TCP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(TCP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pTcpClient = owner;

                SetState(AsyncOP_State.Active);

                try{
                    m_pSslStream = new SslStream(m_pTcpClient.m_pTcpStream.SourceStream,false,this.RemoteCertificateValidationCallback);
                    m_pSslStream.BeginAuthenticateAsClient("dummy",this.BeginAuthenticateAsClientCompleted,null);                  
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

            #region method RemoteCertificateValidationCallback

            /// <summary>
            /// This method is called when we need to validate remote server certificate.
            /// </summary>
            /// <param name="sender">Sender.</param>
            /// <param name="certificate">Certificate.</param>
            /// <param name="chain">Certificate chain.</param>
            /// <param name="sslPolicyErrors">SSL policy errors.</param>
            /// <returns>Returns true if certificate validated, otherwise false.</returns>
            private bool RemoteCertificateValidationCallback(object sender,X509Certificate certificate,X509Chain chain,SslPolicyErrors sslPolicyErrors)
            {
                // User will handle it.
                if(m_pCertCallback != null){
                    return m_pCertCallback(sender,certificate,chain,sslPolicyErrors);
                }
                else{
                    if(sslPolicyErrors == SslPolicyErrors.None || ((sslPolicyErrors & SslPolicyErrors.RemoteCertificateNameMismatch) > 0)){
                        return true;
                    }

                    // Do not allow this client to communicate with unauthenticated servers.
                    return false;
                }
            }

            #endregion

            #region method BeginAuthenticateAsClientCompleted

            /// <summary>
            /// This method is called when "BeginAuthenticateAsClient" has completed.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void BeginAuthenticateAsClientCompleted(IAsyncResult ar)
            {
                try{
                    m_pSslStream.EndAuthenticateAsClient(ar);

                    // Close old stream, but leave source stream open.
                    m_pTcpClient.m_pTcpStream.IsOwner = false;
                    m_pTcpClient.m_pTcpStream.Dispose();

                    m_pTcpClient.m_IsSecure = true;
                    m_pTcpClient.m_pTcpStream = new SmartStream(m_pSslStream,true);
                }
                catch(Exception x){
                    m_pException = x;                    
                }

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

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<SwitchToSecureAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<SwitchToSecureAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts switching connection to secure.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="SwitchToSecureAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is not connected or connection is already secure.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        protected bool SwitchToSecureAsync(SwitchToSecureAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(this.IsSecureConnection){
                throw new InvalidOperationException("Connection is already secure.");
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

        
        #region virtual method OnConnected

        /// <summary>
        /// This method is called after TCP client has sucessfully connected.
        /// </summary>
        protected virtual void OnConnected()
        {
        }

        /// <summary>
        /// Represents callback to be called when to complete connect operation.
        /// </summary>
        /// <param name="error">Exception happened or null if no errors.</param>
        protected delegate void CompleteConnectCallback(Exception error);

        /// <summary>
        /// This method is called when TCP client has sucessfully connected.
        /// </summary>
        /// <param name="callback">Callback to be called to complete connect operation.</param>
        protected virtual void OnConnected(CompleteConnectCallback callback)
        {
            try{
                OnConnected();

                callback(null);
            }
            catch(Exception x){
                callback(x);
            }
        }

        #endregion


        #region method ReadLine

        /// <summary>
        /// Reads and logs specified line from connected host.
        /// </summary>
        /// <returns>Returns readed line.</returns>
        protected string ReadLine()
        {
            SmartStream.ReadLineAsyncOP args = new SmartStream.ReadLineAsyncOP(new byte[32000],SizeExceededAction.JunkAndThrowException);
            this.TcpStream.ReadLine(args,false);
            if(args.Error != null){
                throw args.Error;
            }
            string line = args.LineUtf8;
            if(args.BytesInBuffer > 0){
                LogAddRead(args.BytesInBuffer,line);
            }
            else{
                LogAddText("Remote host closed connection.");
            }

            return line;
        }

        #endregion

        #region method WriteLine

        /// <summary>
        /// Sends and logs specified line to connected host.
        /// </summary>
        /// <param name="line">Line to send.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>line</b> is null reference.</exception>
        protected void WriteLine(string line)
        {
            if(line == null){
                throw new ArgumentNullException("line");
            }

            int countWritten = this.TcpStream.WriteLine(line);
            LogAddWrite(countWritten,line);
        }

        #endregion


        #region mehtod LogAddRead

        /// <summary>
        /// Logs read operation.
        /// </summary>
        /// <param name="size">Number of bytes readed.</param>
        /// <param name="text">Log text.</param>
        internal protected void LogAddRead(long size,string text)
        {
            try{
                if(m_pLogger != null){
                    m_pLogger.AddRead(
                        this.ID,
                        this.AuthenticatedUserIdentity,
                        size,
                        text,                        
                        this.LocalEndPoint,
                        this.RemoteEndPoint
                    );
                }
            }
            catch{
                // We skip all logging errors, normally there shouldn't be any.
            }
        }

        #endregion

        #region method LogAddWrite

        /// <summary>
        /// Logs write operation.
        /// </summary>
        /// <param name="size">Number of bytes written.</param>
        /// <param name="text">Log text.</param>
        internal protected void LogAddWrite(long size,string text)
        {
            try{
                if(m_pLogger != null){
                    m_pLogger.AddWrite(
                        this.ID,
                        this.AuthenticatedUserIdentity,
                        size,
                        text,                        
                        this.LocalEndPoint,
                        this.RemoteEndPoint
                    );
                }
            }
            catch{
                // We skip all logging errors, normally there shouldn't be any.
            }
        }

        #endregion

        #region method LogAddText

        /// <summary>
        /// Logs free text entry.
        /// </summary>
        /// <param name="text">Log text.</param>
        internal protected void LogAddText(string text)
        {
            try{
                if(m_pLogger != null){
                    m_pLogger.AddText(
                        this.IsConnected ? this.ID : "",
                        this.IsConnected ? this.AuthenticatedUserIdentity : null,
                        text,                        
                        this.IsConnected ? this.LocalEndPoint : null,
                        this.IsConnected ? this.RemoteEndPoint : null
                    );
                }
            }
            catch{
                // We skip all logging errors, normally there shouldn't be any.
            }
        }

        #endregion

        #region method LogAddException

        /// <summary>
        /// Logs exception.
        /// </summary>
        /// <param name="text">Log text.</param>
        /// <param name="x">Exception happened.</param>
        internal protected void LogAddException(string text,Exception x)
        {
            try{
                if(m_pLogger != null){
                    m_pLogger.AddException(
                        this.IsConnected ? this.ID : "",
                        this.IsConnected ? this.AuthenticatedUserIdentity : null,
                        text,                        
                        this.IsConnected ? this.LocalEndPoint : null,
                        this.IsConnected ? this.RemoteEndPoint : null,
                        x
                    );
                }
            }
            catch{
                // We skip all logging errors, normally there shouldn't be any.
            }
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
        /// Gets or sets TCP client logger. Value null means no logging.
        /// </summary>
        public Logger Logger
        {
            get{ return m_pLogger; }

            set{ m_pLogger = value; }
        }


        /// <summary>
        /// Gets if TCP client is connected.
        /// </summary>
        public override bool IsConnected
        {
            get{ return m_IsConnected; }
        }

        /// <summary>
        /// Gets session ID.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is not connected.</exception>
        public override string ID
        {
            get{                
                if(m_IsDisposed){
                    throw new ObjectDisposedException("TCP_Client");
                }
                if(!m_IsConnected){
                    throw new InvalidOperationException("TCP client is not connected.");
                }

                return m_ID; 
            }
        }

        /// <summary>
        /// Gets the time when session was connected.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is not connected.</exception>
        public override DateTime ConnectTime
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("TCP_Client");
                }
                if(!m_IsConnected){
                    throw new InvalidOperationException("TCP client is not connected.");
                }

                return m_ConnectTime; 
            }
        }

        /// <summary>
        /// Gets the last time when data was sent or received.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is not connected.</exception>
        public override DateTime LastActivity
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("TCP_Client");
                }
                if(!m_IsConnected){
                    throw new InvalidOperationException("TCP client is not connected.");
                }

                return m_pTcpStream.LastActivity; 
            }
        }

        /// <summary>
        /// Gets session local IP end point.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is not connected.</exception>
        public override IPEndPoint LocalEndPoint
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("TCP_Client");
                }
                if(!m_IsConnected){
                    throw new InvalidOperationException("TCP client is not connected.");
                }

                return m_pLocalEP; 
            }
        }

        /// <summary>
        /// Gets session remote IP end point.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is not connected.</exception>
        public override IPEndPoint RemoteEndPoint
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("TCP_Client");
                }
                if(!m_IsConnected){
                    throw new InvalidOperationException("TCP client is not connected.");
                }

                return m_pRemoteEP; 
            }
        }
        
        /// <summary>
        /// Gets if this session TCP connection is secure connection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is not connected.</exception>
        public override bool IsSecureConnection
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("TCP_Client");
                }
                if(!m_IsConnected){
                    throw new InvalidOperationException("TCP client is not connected.");
                }

                return m_IsSecure; 
            }
        }

        /// <summary>
        /// Gets TCP stream which must be used to send/receive data through this session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is not connected.</exception>
        public override SmartStream TcpStream
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("TCP_Client");
                }
                if(!m_IsConnected){
                    throw new InvalidOperationException("TCP client is not connected.");
                }

                return m_pTcpStream; 
            }
        }

        /// <summary>
        /// Gets or stes remote callback which is called when remote server certificate needs to be validated.
        /// Value null means not sepcified.
        /// </summary>
        public RemoteCertificateValidationCallback ValidateCertificateCallback
        {
            get{ return m_pCertificateCallback; }

            set{ m_pCertificateCallback = value; }
        }

        /// <summary>
        /// Gets or sets default TCP read/write timeout.
        /// </summary>
        /// <remarks>This timeout applies only synchronous TCP read/write operations.</remarks>
        public int Timeout
        {
            get{ return m_Timeout; }

            set{ m_Timeout = value; }
        }

        #endregion


        // OBSOLETE

        #region method BeginConnect
        
        /// <summary>
        /// Internal helper method for asynchronous Connect method.
        /// </summary>
        /// <param name="host">Host name or IP address.</param>
        /// <param name="port">Port to connect.</param>
        /// <param name="ssl">Specifies if connects to SSL end point.</param>
        private delegate void BeginConnectHostDelegate(string host,int port,bool ssl);

        /// <summary>
        /// Internal helper method for asynchronous Connect method.
        /// </summary>
        /// <param name="localEP">Local IP end point to use for connect.</param>
        /// <param name="remoteEP">Remote IP end point where to connect.</param>
        /// <param name="ssl">Specifies if connects to SSL end point.</param>
        private delegate void BeginConnectEPDelegate(IPEndPoint localEP,IPEndPoint remoteEP,bool ssl);

        /// <summary>
        /// Starts connection to the specified host.
        /// </summary>
        /// <param name="host">Host name or IP address.</param>
        /// <param name="port">Port to connect.</param>
        /// <param name="callback">Callback to call when the connect operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous connection.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is already connected.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        [Obsolete("Use method ConnectAsync instead.")]
        public IAsyncResult BeginConnect(string host,int port,AsyncCallback callback,object state)
        {            
            return BeginConnect(host,port,false,callback,state);
        }
                
        /// <summary>
        /// Starts connection to the specified host.
        /// </summary>
        /// <param name="host">Host name or IP address.</param>
        /// <param name="port">Port to connect.</param>
        /// <param name="ssl">Specifies if connects to SSL end point.</param>
        /// <param name="callback">Callback to call when the connect operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous connection.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is already connected.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        [Obsolete("Use method ConnectAsync instead.")]
        public IAsyncResult BeginConnect(string host,int port,bool ssl,AsyncCallback callback,object state)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(m_IsConnected){
                throw new InvalidOperationException("TCP client is already connected.");
            }
            if(string.IsNullOrEmpty(host)){
                throw new ArgumentException("Argument 'host' value may not be null or empty.");
            }
            if(port < 1){
                throw new ArgumentException("Argument 'port' value must be >= 1.");
            }

            BeginConnectHostDelegate asyncMethod = new BeginConnectHostDelegate(this.Connect);
            AsyncResultState asyncState = new AsyncResultState(this,asyncMethod,callback,state);
            asyncState.SetAsyncResult(asyncMethod.BeginInvoke(host,port,ssl,new AsyncCallback(asyncState.CompletedCallback),null));

            return asyncState;
        }

        /// <summary>
        /// Starts connection to the specified remote end point.
        /// </summary>
        /// <param name="remoteEP">Remote IP end point where to connect.</param>
        /// <param name="ssl">Specifies if connects to SSL end point.</param>
        /// <param name="callback">Callback to call when the connect operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous connection.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is already connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>remoteEP</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        [Obsolete("Use method ConnectAsync instead.")]
        public IAsyncResult BeginConnect(IPEndPoint remoteEP,bool ssl,AsyncCallback callback,object state)
        {
            return BeginConnect(null,remoteEP,ssl,callback,state);
        }

        /// <summary>
        /// Starts connection to the specified remote end point.
        /// </summary>
        /// <param name="localEP">Local IP end point to use for connect.</param>
        /// <param name="remoteEP">Remote IP end point where to connect.</param>
        /// <param name="ssl">Specifies if connects to SSL end point.</param>
        /// <param name="callback">Callback to call when the connect operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous connection.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when TCP client is already connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>remoteEP</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        [Obsolete("Use method ConnectAsync instead.")]
        public IAsyncResult BeginConnect(IPEndPoint localEP,IPEndPoint remoteEP,bool ssl,AsyncCallback callback,object state)
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(m_IsConnected){
                throw new InvalidOperationException("TCP client is already connected.");
            }
            if(remoteEP == null){
                throw new ArgumentNullException("remoteEP");
            }
            
            BeginConnectEPDelegate asyncMethod = new BeginConnectEPDelegate(this.Connect);
            AsyncResultState asyncState = new AsyncResultState(this,asyncMethod,callback,state);
            asyncState.SetAsyncResult(asyncMethod.BeginInvoke(localEP,remoteEP,ssl,new AsyncCallback(asyncState.CompletedCallback),null));

            return asyncState;
        }

        #endregion

        #region method EndConnect

        /// <summary>
        /// Ends a pending asynchronous connection request.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that stores state information and any user defined data for this asynchronous operation.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when argument <b>asyncResult</b> was not returned by a call to the <b>BeginConnect</b> method.</exception>
        /// <exception cref="InvalidOperationException">Is raised when <b>EndConnect</b> was previously called for the asynchronous connection.</exception>
        [Obsolete("Use method ConnectAsync instead.")]
        public void EndConnect(IAsyncResult asyncResult)
        {   
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }
                        
            AsyncResultState castedAsyncResult = asyncResult as AsyncResultState;
            if(castedAsyncResult == null || castedAsyncResult.AsyncObject != this){                
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginConnect method.");
            }
            if(castedAsyncResult.IsEndCalled){
                throw new InvalidOperationException("EndConnect was previously called for the asynchronous operation.");
            }
             
            castedAsyncResult.IsEndCalled = true;
            if(castedAsyncResult.AsyncDelegate is BeginConnectHostDelegate){
                ((BeginConnectHostDelegate)castedAsyncResult.AsyncDelegate).EndInvoke(castedAsyncResult.AsyncResult);
            }
            else if(castedAsyncResult.AsyncDelegate is BeginConnectEPDelegate){
                ((BeginConnectEPDelegate)castedAsyncResult.AsyncDelegate).EndInvoke(castedAsyncResult.AsyncResult);
            }
            else{
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginConnect method.");
            }
        }

        #endregion

        #region method OnError

        /// <summary>
        /// This must be called when unexpected error happens. When inheriting <b>TCP_Client</b> class, be sure that you call <b>OnError</b>
        /// method for each unexpected error.
        /// </summary>
        /// <param name="x">Exception happened.</param>
        [Obsolete("Don't use this method.")]
        protected void OnError(Exception x)
        {
            try{
                if(m_pLogger != null){
                    //m_pLogger.AddException(x);
                }
            }
            catch{
            }
        }

        #endregion

    }
}
