using System;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Collections.Generic;
using System.Net.Security;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Text;

using LumiSoft.Net.IO;
using LumiSoft.Net.TCP;
using LumiSoft.Net.AUTH;

namespace LumiSoft.Net.POP3.Client
{
	/// <summary>
	/// POP3 Client. Defined in RFC 1939.
	/// </summary>
	/// <example>
	/// <code>
	/// 
	/// /*
	///  To make this code to work, you need to import following namespaces:
	///  using LumiSoft.Net.Mail;
	///  using LumiSoft.Net.POP3.Client; 
	///  */
	/// 
	/// using(POP3_Client c = new POP3_Client()){
	///		c.Connect("ivx",WellKnownPorts.POP3);
	///		c.Login("test","test");
    ///		// Or Auth(sasl-method);
	///				
	///		// Get first message if there is any
	///		if(c.Messages.Count > 0){
	///			// Do your suff
	///			
	///			// Parse message
	///			Mail_Message m = Mail_Message.Parse(c.Messages[0].MessageToByte());
	///			string subject = m.Subject;			
	///			// ... 
	///		}		
	///	}
	/// </code>
	/// </example>
	public class POP3_Client : TCP_Client
	{
        private string                       m_GreetingText       = "";
		private string                       m_ApopHashKey        = "";
        private List<string>                 m_pExtCapabilities   = null;
        private bool                         m_IsUidlSupported    = false;
        private POP3_ClientMessageCollection m_pMessages          = null;
        private GenericIdentity              m_pAuthdUserIdentity = null;

		/// <summary>
		/// Default constructor.
		/// </summary>
		public POP3_Client()
		{
	        m_pExtCapabilities = new List<string>();
		}

		#region override method Dispose

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		public override void Dispose()
		{
			base.Dispose();
		}

		#endregion


		#region override method Disconnect

		/// <summary>
		/// Closes connection to POP3 server.
		/// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected.</exception>
		public override void Disconnect()
		{
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("POP3 client is not connected.");
            }

			try{
                // Send QUIT command to server.                
                WriteLine("QUIT");
			}
			catch{
			}

            try{
                base.Disconnect(); 
            }
            catch{
            }


            m_GreetingText       = "";
            m_ApopHashKey        = "";
            m_pExtCapabilities   = new List<string>();
            m_IsUidlSupported    = false;
            if(m_pMessages != null){
                m_pMessages.Dispose();
                m_pMessages = null;
            } 
            m_pAuthdUserIdentity = null;
		}

		#endregion


        #region method Capa

        /// <summary>
        /// Executes CAPA command.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>
        public void Capa()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}

            using(CapaAsyncOP op = new CapaAsyncOP()){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<CapaAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.CapaAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();
                    wait.Close();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method CapaAsync

        #region class CapaAsyncOP

        /// <summary>
        /// This class represents <see cref="POP3_Client.CapaAsync"/> asynchronous operation.
        /// </summary>
        public class CapaAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock          = new object();
            private AsyncOP_State m_State          = AsyncOP_State.WaitingForStart;
            private Exception     m_pException     = null;
            private POP3_Client   m_pPop3Client    = null;
            private bool          m_RiseCompleted  = false;
            private List<string>  m_pResponseLines = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public CapaAsyncOP()
            {
                m_pResponseLines = new List<string>();
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);
                
                m_pException     = null;
                m_pPop3Client    = null;
                m_pResponseLines = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner POP3 client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(POP3_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pPop3Client = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 2449 CAPA
                        Arguments:
                            none

                        Restrictions:
                            none

                        Discussion:
                            An -ERR response indicates the capability command is not
                            implemented and the client will have to probe for
                            capabilities as before.

                            An +OK response is followed by a list of capabilities, one
                            per line.  Each capability name MAY be followed by a single
                            space and a space-separated list of parameters.  Each
                            capability line is limited to 512 octets (including the
                            CRLF).  The capability list is terminated by a line
                            containing a termination octet (".") and a CRLF pair.

                        Possible Responses:
                            +OK -ERR

                        Examples:
                            C: CAPA
                            S: +OK Capability list follows
                            S: TOP
                            S: USER
                            S: SASL CRAM-MD5 KERBEROS_V4
                            S: RESP-CODES
                            S: LOGIN-DELAY 900
                            S: PIPELINING
                            S: EXPIRE 60
                            S: UIDL
                            S: IMPLEMENTATION Shlemazle-Plotz-v302
                            S: .
                    */

                    byte[] buffer = Encoding.UTF8.GetBytes("CAPA\r\n");

                    // Log
                    m_pPop3Client.LogAddWrite(buffer.Length,"CAPA");

                    // Start command sending.
                    m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.CapaCommandSendingCompleted,null);
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
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

            #region method CapaCommandSendingCompleted

            /// <summary>
            /// Is called when CAPA command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void CapaCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pPop3Client.TcpStream.EndWrite(ar);

                    // Read POP3 server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        CapaReadResponseCompleted(op);
                    };
                    if(m_pPop3Client.TcpStream.ReadLine(op,true)){
                        CapaReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method CapaReadResponseCompleted
            
            /// <summary>
            /// Is called when POP3 server CAPA response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void CapaReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Log
                        m_pPop3Client.LogAddRead(op.BytesInBuffer,op.LineUtf8);
                                            
                        // Server returned success response.
                        if(string.Equals(op.LineUtf8.Split(new char[]{' '},2)[0],"+OK",StringComparison.InvariantCultureIgnoreCase)){
                            // Read capa-list.
                            SmartStream.ReadLineAsyncOP readLineOP = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                            readLineOP.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                                try{
                                    ReadMultiLineResponseLineCompleted(readLineOP);

                                    // Read response lines while we get terminator(.).
                                    while(this.State == AsyncOP_State.Active && m_pPop3Client.TcpStream.ReadLine(readLineOP,true)){
                                        ReadMultiLineResponseLineCompleted(readLineOP);
                                    }
                                }
                                catch(Exception x){
                                    m_pException = x;
                                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                                    SetState(AsyncOP_State.Completed);
                                }
                            };
                            // Read response lines while we get terminator(.).
                            while(this.State == AsyncOP_State.Active && m_pPop3Client.TcpStream.ReadLine(readLineOP,true)){
                                ReadMultiLineResponseLineCompleted(readLineOP);
                            }
                        }
                        // Server returned error response.
                        else{
                            m_pException = new POP3_ClientException(op.LineUtf8);
                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }

                op.Dispose();
            }

            #endregion

            #region method ReadMultiLineResponseLineCompleted
            
            /// <summary>
            /// Is called when POP3 server multiline response single line reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ReadMultiLineResponseLineCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Log
                        m_pPop3Client.LogAddRead(op.BytesInBuffer,op.LineUtf8);
                         
                        // Server closed connection.
                        if(op.BytesInBuffer == 0){
                            m_pException = new IOException("POP3 server closed connection unexpectedly.");
                            SetState(AsyncOP_State.Completed);
                        }
                        // We got respone terminator(.).
                        else if(string.Equals(op.LineUtf8,".",StringComparison.InvariantCultureIgnoreCase)){
                            m_pPop3Client.m_pExtCapabilities.Clear();
                            m_pPop3Client.m_pExtCapabilities.AddRange(m_pResponseLines);

                            SetState(AsyncOP_State.Completed);
                        }
                        // We got response line.
                        else{
                            m_pResponseLines.Add(op.LineUtf8);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
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

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<CapaAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<CapaAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending CAPA command to POP3 server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CapaAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool CapaAsync(CapaAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
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


        #region method Stls

        /// <summary>
        /// Executes STLS command.
        /// </summary>
        /// <param name="certCallback">SSL server certificate validation callback. Value null means any certificate is accepted.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected or is authenticated or is already secure connection.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>
        public void Stls(RemoteCertificateValidationCallback certCallback)
        {    
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}
			if(this.IsAuthenticated){
				throw new InvalidOperationException("The STLS command is only valid in non-authenticated state.");
			}
            if(this.IsSecureConnection){
                throw new InvalidOperationException("Connection is already secure.");
            }
                        
            using(StlsAsyncOP op = new StlsAsyncOP(certCallback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<StlsAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.StlsAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();
                    wait.Close();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method StlsAsync

        #region class StlsAsyncOP

        /// <summary>
        /// This class represents <see cref="POP3_Client.StlsAsync"/> asynchronous operation.
        /// </summary>
        public class StlsAsyncOP : IDisposable,IAsyncOP
        {
            private object                              m_pLock         = new object();
            private AsyncOP_State                       m_State         = AsyncOP_State.WaitingForStart;
            private Exception                           m_pException    = null;
            private POP3_Client                         m_pPop3Client   = null;
            private bool                                m_RiseCompleted = false;
            private RemoteCertificateValidationCallback m_pCertCallback = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="certCallback">SSL server certificate validation callback. Value null means any certificate is accepted.</param>
            public StlsAsyncOP(RemoteCertificateValidationCallback certCallback)
            {
                m_pCertCallback = certCallback;
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);
                
                m_pException  = null;
                m_pPop3Client = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner POP3 client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(POP3_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pPop3Client = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 2595 4. POP3 STARTTLS extension.
                        Arguments: none

                        Restrictions:
                            Only permitted in AUTHORIZATION state.
                     
                        Possible Responses:
                             +OK -ERR

                         Examples:
                             C: STLS
                             S: +OK Begin TLS negotiation
                             <TLS negotiation, further commands are under TLS layer>
                               ...
                             C: STLS
                             S: -ERR Command not permitted when TLS active
                    */

                    byte[] buffer = Encoding.UTF8.GetBytes("STLS\r\n");

                    // Log
                    m_pPop3Client.LogAddWrite(buffer.Length,"STLS");

                    // Start command sending.
                    m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.StlsCommandSendingCompleted,null);
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
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

            #region method StlsCommandSendingCompleted

            /// <summary>
            /// Is called when STLS command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void StlsCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pPop3Client.TcpStream.EndWrite(ar);

                    // Read POP3 server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        StlsReadResponseCompleted(op);
                    };
                    if(m_pPop3Client.TcpStream.ReadLine(op,true)){
                        StlsReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method StlsReadResponseCompleted
            
            /// <summary>
            /// Is called when POP3 server STLS response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void StlsReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Log
                        m_pPop3Client.LogAddRead(op.BytesInBuffer,op.LineUtf8);
                                            
                        // Server returned success response.
                        if(string.Equals(op.LineUtf8.Split(new char[]{' '},2)[0],"+OK",StringComparison.InvariantCultureIgnoreCase)){                        
                            // Log
                            m_pPop3Client.LogAddText("Starting TLS handshake.");

                            SwitchToSecureAsyncOP switchSecureOP = new SwitchToSecureAsyncOP(m_pCertCallback);
                            switchSecureOP.CompletedAsync += delegate(object s,EventArgs<SwitchToSecureAsyncOP> e){
                                SwitchToSecureCompleted(switchSecureOP);
                            };
                            if(!m_pPop3Client.SwitchToSecureAsync(switchSecureOP)){
                                SwitchToSecureCompleted(switchSecureOP);
                            }
                        }
                        // Server returned error response.
                        else{
                            m_pException = new POP3_ClientException(op.LineUtf8);
                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }

                op.Dispose();
            }

            #endregion

            #region method SwitchToSecureCompleted

            /// <summary>
            /// Is called when TLS handshake has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void SwitchToSecureCompleted(SwitchToSecureAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + m_pException.Message,m_pException);
                    }
                    else{
                        // Log
                        m_pPop3Client.LogAddText("TLS handshake completed successfully.");
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + m_pException.Message,m_pException);
                }

                op.Dispose();

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
            public event EventHandler<EventArgs<StlsAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<StlsAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending STLS command to POP3 server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="StlsAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool StlsAsync(StlsAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
			if(this.IsAuthenticated){
				throw new InvalidOperationException("The STLS command is only valid in non-authenticated state.");
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
                
        #region method Login

        /// <summary>
        /// Executes USER/PASS command.
        /// </summary>
        /// <param name="user">User name.</param>
        /// <param name="password">User password.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected or is already authenticated.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>user</b> or <b>password</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>
        public void Login(string user,string password)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}
			if(this.IsAuthenticated){
				throw new InvalidOperationException("Session is already authenticated.");
			}
            if(user == null){
                throw new ArgumentNullException("user");
            }
            if(user == string.Empty){
                throw new ArgumentException("Argument 'user' value must be specified.","user");
            }
            if(password == null){
                throw new ArgumentNullException("password");
            }

            using(LoginAsyncOP op = new LoginAsyncOP(user,password)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<LoginAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.LoginAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();
                    wait.Close();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method LoginAsync

        #region class LoginAsyncOP

        /// <summary>
        /// This class represents <see cref="POP3_Client.LoginAsync"/> asynchronous operation.
        /// </summary>
        public class LoginAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock         = new object();
            private AsyncOP_State m_State         = AsyncOP_State.WaitingForStart;
            private Exception     m_pException    = null;
            private POP3_Client   m_pPop3Client   = null;
            private bool          m_RiseCompleted = false;
            private string        m_User          = null;
            private string        m_Password      = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="user">User name.</param>
            /// <param name="password">User password.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>user</b> or <b>password</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public LoginAsyncOP(string user,string password)
            {
                if(user == null){
                    throw new ArgumentNullException("user");
                }
                if(user == string.Empty){
                    throw new ArgumentException("Argument 'user' value must be specified.","user");
                }
                if(password == null){
                    throw new ArgumentNullException("password");
                }

                m_User     = user;
                m_Password = password;
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);
                
                m_pException  = null;
                m_pPop3Client = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner POP3 client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(POP3_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pPop3Client = owner;

                SetState(AsyncOP_State.Active);

                try{
                    byte[] buffer = Encoding.UTF8.GetBytes("USER " + m_User + "\r\n");

                    // Log
                    m_pPop3Client.LogAddWrite(buffer.Length,"USER " + m_User);

                    // Start command sending.
                    m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.UserCommandSendingCompleted,null);
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
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

            #region method UserCommandSendingCompleted

            /// <summary>
            /// Is called when USER command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void UserCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pPop3Client.TcpStream.EndWrite(ar);

                    // Read POP3 server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        UserReadResponseCompleted(op);
                    };
                    if(m_pPop3Client.TcpStream.ReadLine(op,true)){
                        UserReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method UserReadResponseCompleted
            
            /// <summary>
            /// Is called when POP3 server USER response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void UserReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Log
                        m_pPop3Client.LogAddRead(op.BytesInBuffer,op.LineUtf8);
                                            
                        // Server returned success response.
                        if(string.Equals(op.LineUtf8.Split(new char[]{' '},2)[0],"+OK",StringComparison.InvariantCultureIgnoreCase)){                        
                            byte[] buffer = Encoding.UTF8.GetBytes("PASS " + m_Password + "\r\n");

                            // Log
                            m_pPop3Client.LogAddWrite(buffer.Length,"PASS <***REMOVED***>");

                            // Start command sending.
                            m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.PassCommandSendingCompleted,null);
                        }
                        // Server returned error response.
                        else{
                            m_pException = new POP3_ClientException(op.LineUtf8);
                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }

                op.Dispose();
            }

            #endregion

            #region method PassCommandSendingCompleted

            /// <summary>
            /// Is called when PASS command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void PassCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pPop3Client.TcpStream.EndWrite(ar);

                    // Read POP3 server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        PassReadResponseCompleted(op);
                    };
                    if(m_pPop3Client.TcpStream.ReadLine(op,true)){
                        PassReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method PassReadResponseCompleted
            
            /// <summary>
            /// Is called when POP3 server PASS response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void PassReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Log
                        m_pPop3Client.LogAddRead(op.BytesInBuffer,op.LineUtf8);
                                            
                        // Server returned success response.
                        if(string.Equals(op.LineUtf8.Split(new char[]{' '},2)[0],"+OK",StringComparison.InvariantCultureIgnoreCase)){                        
                            // Start filling messages info.
                            POP3_Client.FillMessagesAsyncOP fillOP = new FillMessagesAsyncOP();
                            fillOP.CompletedAsync += delegate(object sender,EventArgs<FillMessagesAsyncOP> e){
                                FillMessagesCompleted(fillOP);
                            };
                            if(!m_pPop3Client.FillMessagesAsync(fillOP)){
                                FillMessagesCompleted(fillOP);
                            }
                        }
                        // Server returned error response.
                        else{
                            m_pException = new POP3_ClientException(op.LineUtf8);
                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }

                op.Dispose();
            }

            #endregion

            #region method FillMessagesCompleted

            /// <summary>
            /// Is called when FillMessagesAsync method has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void FillMessagesCompleted(FillMessagesAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error ;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        m_pPop3Client.m_pAuthdUserIdentity = new GenericIdentity(m_User,"pop3-user/pass");
                        SetState(AsyncOP_State.Completed);
                    }
                }                
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
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

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<LoginAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<LoginAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts executing USER/PASS command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="LoginAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool LoginAsync(LoginAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }            
			if(this.IsAuthenticated){
				throw new InvalidOperationException("Session is already authenticated.");
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

        #region method Auth

        /// <summary>
        /// Sends AUTH command to POP3 server.
        /// </summary>
        /// <param name="sasl">SASL authentication.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected or is already authenticated.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>
        public void Auth(AUTH_SASL_Client sasl)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}
            if(this.IsAuthenticated){
                throw new InvalidOperationException("Connection is already authenticated.");
            }
            if(sasl == null){
                throw new ArgumentNullException("sasl");
            }
            
            using(AuthAsyncOP op = new AuthAsyncOP(sasl)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<AuthAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.AuthAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();
                    wait.Close();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method AuthAsync

        #region class AuthAsyncOP

        /// <summary>
        /// This class represents <see cref="POP3_Client.AuthAsync"/> asynchronous operation.
        /// </summary>
        public class AuthAsyncOP : IDisposable,IAsyncOP
        {
            private object           m_pLock         = new object();
            private AsyncOP_State    m_State         = AsyncOP_State.WaitingForStart;
            private Exception        m_pException    = null;
            private POP3_Client      m_pPop3Client   = null;
            private AUTH_SASL_Client m_pSASL         = null;
            private bool             m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="sasl">SASL authentication.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>sasl</b> is null reference.</exception>
            public AuthAsyncOP(AUTH_SASL_Client sasl)
            {
                if(sasl == null){
                    throw new ArgumentNullException("sasl");
                }

                m_pSASL = sasl;
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
                
                m_pException  = null;
                m_pPop3Client = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner POP3 client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(POP3_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pPop3Client = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 5034 4. The AUTH Command.

                        AUTH mechanism [initial-response]

                        Arguments:

                        mechanism: A string identifying a SASL authentication mechanism.
                        
                        initial-response: An optional initial client response, as
                                          defined in Section 3 of [RFC4422].  If present, this response
                                          MUST be encoded as Base64 (specified in Section 4 of
                                          [RFC4648]), or consist only of the single character "=", which
                                          represents an empty initial response.
                    */

                    if(m_pSASL.SupportsInitialResponse){
                        byte[] buffer = Encoding.UTF8.GetBytes("AUTH " + m_pSASL.Name + " " + Convert.ToBase64String(m_pSASL.Continue(null)) + "\r\n");

                        // Log
                        m_pPop3Client.LogAddWrite(buffer.Length,Encoding.UTF8.GetString(buffer).TrimEnd());

                        // Start command sending.
                        m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.AuthCommandSendingCompleted,null);
                    }
                    else{
                        byte[] buffer = Encoding.UTF8.GetBytes("AUTH " + m_pSASL.Name + "\r\n");

                        // Log
                        m_pPop3Client.LogAddWrite(buffer.Length,"AUTH " + m_pSASL.Name);

                        // Start command sending.
                        m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.AuthCommandSendingCompleted,null);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
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

            #region method AuthCommandSendingCompleted

            /// <summary>
            /// Is called when AUTH command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void AuthCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pPop3Client.TcpStream.EndWrite(ar);

                    // Read POP3 server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        AuthReadResponseCompleted(op);
                    };
                    if(m_pPop3Client.TcpStream.ReadLine(op,true)){
                        AuthReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method AuthReadResponseCompleted
            
            /// <summary>
            /// Is called when POP3 server response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void AuthReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Log
                        m_pPop3Client.LogAddRead(op.BytesInBuffer,op.LineUtf8);
                                            
                        // Authentication succeeded.
                        if(string.Equals(op.LineUtf8.Split(new char[]{' '},2)[0],"+OK",StringComparison.InvariantCultureIgnoreCase)){
                            // Start filling messages info.
                            POP3_Client.FillMessagesAsyncOP fillOP = new FillMessagesAsyncOP();
                            fillOP.CompletedAsync += delegate(object sender,EventArgs<FillMessagesAsyncOP> e){
                                FillMessagesCompleted(fillOP);
                            };
                            if(!m_pPop3Client.FillMessagesAsync(fillOP)){
                                FillMessagesCompleted(fillOP);
                            }
                        }
                        // Continue authenticating.
                        else if(op.LineUtf8.StartsWith("+")){
                            // + base64Data, we need to decode it.
                            byte[] serverResponse = Convert.FromBase64String(op.LineUtf8.Split(new char[]{' '},2)[1]);

                            byte[] clientResponse = m_pSASL.Continue(serverResponse);

                            // We need just send SASL returned auth-response as base64.
                            byte[] buffer = Encoding.UTF8.GetBytes(Convert.ToBase64String(clientResponse) + "\r\n");

                            // Log
                            m_pPop3Client.LogAddWrite(buffer.Length,Convert.ToBase64String(clientResponse));

                            // Start auth-data sending.
                            m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.AuthCommandSendingCompleted,null);
                        }
                        // Authentication rejected.
                        else{
                            m_pException = new POP3_ClientException(op.LineUtf8);
                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }

                op.Dispose();
            }

            #endregion

            #region method FillMessagesCompleted

            /// <summary>
            /// Is called when FillMessagesAsync method has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void FillMessagesCompleted(FillMessagesAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error ;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        m_pPop3Client.m_pAuthdUserIdentity = new GenericIdentity(m_pSASL.UserName,m_pSASL.Name);
                        SetState(AsyncOP_State.Completed);
                    }
                }                
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
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

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<AuthAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<AuthAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending AUTH command to POP3 server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="AuthAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected or connection is already authenticated.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool AuthAsync(AuthAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(this.IsAuthenticated){
                throw new InvalidOperationException("Connection is already authenticated.");
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
                
        #region method Noop

        /// <summary>
        /// Send NOOP command to server. This method can be used for keeping connection alive(not timing out).
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>
        public void Noop()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
			if(!this.IsAuthenticated){
				throw new InvalidOperationException("The NOOP command is only valid in TRANSACTION state.");
			}

            using(NoopAsyncOP op = new NoopAsyncOP()){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<NoopAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.NoopAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();
                    wait.Close();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method NoopAsync

        #region class NoopAsyncOP

        /// <summary>
        /// This class represents <see cref="POP3_Client.NoopAsync"/> asynchronous operation.
        /// </summary>
        public class NoopAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock         = new object();
            private AsyncOP_State m_State         = AsyncOP_State.WaitingForStart;
            private Exception     m_pException    = null;
            private POP3_Client   m_pPop3Client   = null;
            private bool          m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public NoopAsyncOP()
            {
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);
                
                m_pException  = null;
                m_pPop3Client = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner POP3 client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(POP3_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pPop3Client = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 1939 5 NOOP.
                        Arguments: none

                        Restrictions:
                            may only be given in the TRANSACTION state

                        Discussion:
                            The POP3 server does nothing, it merely replies with a
                            positive response.

                        Possible Responses:
                            +OK

                        Examples:
                            C: NOOP
                            S: +OK
                    */

                    byte[] buffer = Encoding.UTF8.GetBytes("NOOP\r\n");

                    // Log
                    m_pPop3Client.LogAddWrite(buffer.Length,"NOOP");

                    // Start command sending.
                    m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.NoopCommandSendingCompleted,null);
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
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

            #region method NoopCommandSendingCompleted

            /// <summary>
            /// Is called when NOOP command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void NoopCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pPop3Client.TcpStream.EndWrite(ar);

                    // Read POP3 server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        NoopReadResponseCompleted(op);
                    };
                    if(m_pPop3Client.TcpStream.ReadLine(op,true)){
                        NoopReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method NoopReadResponseCompleted
            
            /// <summary>
            /// Is called when POP3 server NOOP response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void NoopReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Log
                        m_pPop3Client.LogAddRead(op.BytesInBuffer,op.LineUtf8);
                                            
                        // Server returned success response.
                        if(string.Equals(op.LineUtf8.Split(new char[]{' '},2)[0],"+OK",StringComparison.InvariantCultureIgnoreCase)){                        
                            SetState(AsyncOP_State.Completed);
                        }
                        // Server returned error response.
                        else{
                            m_pException = new POP3_ClientException(op.LineUtf8);
                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
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

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<NoopAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<NoopAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending NOOP command to POP3 server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="NoopAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool NoopAsync(NoopAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
				throw new InvalidOperationException("The NOOP command is only valid in TRANSACTION(authenticated) state.");
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
                
        #region method Rset

        /// <summary>
		/// Resets session. Messages marked for deletion will be unmarked.
		/// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected and authenticated.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>
		public void Rset()
		{
			if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}
			if(!this.IsAuthenticated){
				throw new InvalidOperationException("The RSET command is only valid in TRANSACTION state.");
			}

            using(RsetAsyncOP op = new RsetAsyncOP()){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<RsetAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.RsetAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();
                    wait.Close();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
		}

		#endregion

        #region method RsetAsync

        #region class RsetAsyncOP

        /// <summary>
        /// This class represents <see cref="POP3_Client.RsetAsync"/> asynchronous operation.
        /// </summary>
        public class RsetAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock         = new object();
            private AsyncOP_State m_State         = AsyncOP_State.WaitingForStart;
            private Exception     m_pException    = null;
            private POP3_Client   m_pPop3Client   = null;
            private bool          m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public RsetAsyncOP()
            {
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);
                
                m_pException  = null;
                m_pPop3Client = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner POP3 client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(POP3_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pPop3Client = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 1939 5. RSET.
                        Arguments: none

                        Restrictions:
                            may only be given in the TRANSACTION state

                        Discussion:
                            If any messages have been marked as deleted by the POP3
                            server, they are unmarked.  The POP3 server then replies
                            with a positive response.

                        Possible Responses:
                            +OK

                        Examples:
                            C: RSET
                            S: +OK maildrop has 2 messages (320 octets)
			        */

                    byte[] buffer = Encoding.UTF8.GetBytes("RSET\r\n");

                    // Log
                    m_pPop3Client.LogAddWrite(buffer.Length,"RSET");

                    // Start command sending.
                    m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.RsetCommandSendingCompleted,null);
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
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

            #region method RsetCommandSendingCompleted

            /// <summary>
            /// Is called when RSET command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void RsetCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pPop3Client.TcpStream.EndWrite(ar);

                    // Read POP3 server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        RsetReadResponseCompleted(op);
                    };
                    if(m_pPop3Client.TcpStream.ReadLine(op,true)){
                        RsetReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method RsetReadResponseCompleted
            
            /// <summary>
            /// Is called when POP3 server RSET response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void RsetReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Log
                        m_pPop3Client.LogAddRead(op.BytesInBuffer,op.LineUtf8);
                                            
                        // Server returned success response.
                        if(string.Equals(op.LineUtf8.Split(new char[]{' '},2)[0],"+OK",StringComparison.InvariantCultureIgnoreCase)){
                            foreach(POP3_ClientMessage message in m_pPop3Client.m_pMessages){
                                message.SetMarkedForDeletion(false);
                            }

                            SetState(AsyncOP_State.Completed);
                        }
                        // Server returned error response.
                        else{
                            m_pException = new POP3_ClientException(op.LineUtf8);
                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
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

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<RsetAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<RsetAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending RSET command to POP3 server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="RsetAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool RsetAsync(RsetAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
				throw new InvalidOperationException("The RSET command is only valid in TRANSACTION(authenticated) state.");
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


        #region method FillMessagesAsync

        #region class FillMessagesAsyncOP

        /// <summary>
        /// This class represents <see cref="POP3_Client.FillMessagesAsync"/> asynchronous operation.
        /// </summary>
        private class FillMessagesAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock         = new object();
            private AsyncOP_State m_State         = AsyncOP_State.WaitingForStart;
            private Exception     m_pException    = null;
            private POP3_Client   m_pPop3Client   = null;
            private bool          m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public FillMessagesAsyncOP()
            {
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);
                
                m_pException  = null;
                m_pPop3Client = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner POP3 client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(POP3_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pPop3Client = owner;

                SetState(AsyncOP_State.Active);

                try{
                    // Start executing LIST command.
                    POP3_Client.ListAsyncOP listOP = new ListAsyncOP();
                    listOP.CompletedAsync += delegate(object sender,EventArgs<ListAsyncOP> e){
                        ListCompleted(listOP);
                    };
                    if(!m_pPop3Client.ListAsync(listOP)){
                        ListCompleted(listOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
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

            #region method ListCompleted

            /// <summary>
            /// Is called when LIST command has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ListCompleted(ListAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Fill messages info.
                        m_pPop3Client.m_pMessages = new POP3_ClientMessageCollection(m_pPop3Client);
                        foreach(string seqNo_Size in op.ResponseLines){
                            m_pPop3Client.m_pMessages.Add(Convert.ToInt32(seqNo_Size.Trim().Split(new char[]{' '})[1]));
                        }

                        // Try to UID's for messages(If server supports UIDL).
                        // Start executing LIST command.
                        POP3_Client.UidlAsyncOP uidlOP = new UidlAsyncOP();
                        uidlOP.CompletedAsync += delegate(object sender,EventArgs<UidlAsyncOP> e){
                            UidlCompleted(uidlOP);
                        };
                        if(!m_pPop3Client.UidlAsync(uidlOP)){
                            UidlCompleted(uidlOP);
                        }
                    }
                }                
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }

                op.Dispose();
            }

            #endregion

            #region method UidlCompleted

            /// <summary>
            /// Is called when LIST command has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void UidlCompleted(UidlAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        // Assume that UIDL not supported, skip error.
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        m_pPop3Client.m_IsUidlSupported = true;

                        // Fill messages UID info.
                        foreach(string responseLine in op.ResponseLines){
                            string[] seqNo_Uid = responseLine.Trim().Split(new char[]{' '});
                            m_pPop3Client.m_pMessages[Convert.ToInt32(seqNo_Uid[0]) - 1].SetUID(seqNo_Uid[1]);
                        }

                        SetState(AsyncOP_State.Completed);
                    }
                }                
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
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

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<FillMessagesAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<FillMessagesAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts filling mailbox messages info.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="FillMessagesAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        private bool FillMessagesAsync(FillMessagesAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
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

        #region method ListAsync

        #region class ListAsyncOP

        /// <summary>
        /// This class represents <see cref="POP3_Client.ListAsync"/> asynchronous operation.
        /// </summary>
        private class ListAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock          = new object();
            private AsyncOP_State m_State          = AsyncOP_State.WaitingForStart;
            private Exception     m_pException     = null;
            private POP3_Client   m_pPop3Client    = null;
            private bool          m_RiseCompleted  = false;
            private List<string>  m_pResponseLines = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public ListAsyncOP()
            {
                m_pResponseLines = new List<string>();
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);
                
                m_pException     = null;
                m_pPop3Client    = null;
                m_pResponseLines = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner POP3 client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(POP3_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pPop3Client = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 1935.5. LIST
                         Arguments:
                             a message-number (optional), which, if present, may NOT
                             refer to a message marked as deleted 

                         Restrictions:
                             may only be given in the TRANSACTION state

                         Discussion:
                             If an argument was given and the POP3 server issues a
                             positive response with a line containing information for
                             that message.  This line is called a "scan listing" for
                             that message.

                             If no argument was given and the POP3 server issues a
                             positive response, then the response given is multi-line.
                             After the initial +OK, for each message in the maildrop,
                             the POP3 server responds with a line containing
                             information for that message.  This line is also called a
                             "scan listing" for that message.  If there are no
                             messages in the maildrop, then the POP3 server responds
                             with no scan listings--it issues a positive response
                             followed by a line containing a termination octet and a
                             CRLF pair.

                             In order to simplify parsing, all POP3 servers are
                             required to use a certain format for scan listings.  A
                             scan listing consists of the message-number of the
                             message, followed by a single space and the exact size of
                             the message in octets.  Methods for calculating the exact
                             size of the message are described in the "Message Format"
                             section below.  This memo makes no requirement on what
                             follows the message size in the scan listing.  Minimal
                             implementations should just end that line of the response
                             with a CRLF pair.  More advanced implementations may
                             include other information, as parsed from the message.

                                NOTE: This memo STRONGLY discourages implementations
                                from supplying additional information in the scan
                                listing.  Other, optional, facilities are discussed
                                later on which permit the client to parse the messages
                                in the maildrop.

                             Note that messages marked as deleted are not listed.

                         Possible Responses:
                             +OK scan listing follows
                             -ERR no such message

                         Examples:
                             C: LIST
                             S: +OK 2 messages (320 octets)
                             S: 1 120
                             S: 2 200
                             S: .
                               ...
                             C: LIST 2
                             S: +OK 2 200
                               ...
                             C: LIST 3
                             S: -ERR no such message, only 2 messages in maildrop
                    */

                    byte[] buffer = Encoding.UTF8.GetBytes("LIST\r\n");

                    // Log
                    m_pPop3Client.LogAddWrite(buffer.Length,"LIST");

                    // Start command sending.
                    m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.ListCommandSendingCompleted,null);
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
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

            #region method ListCommandSendingCompleted

            /// <summary>
            /// Is called when LIST command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void ListCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pPop3Client.TcpStream.EndWrite(ar);

                    // Read POP3 server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        ListReadResponseCompleted(op);
                    };
                    if(m_pPop3Client.TcpStream.ReadLine(op,true)){
                        ListReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method ListReadResponseCompleted
            
            /// <summary>
            /// Is called when POP3 server LIST response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ListReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Log
                        m_pPop3Client.LogAddRead(op.BytesInBuffer,op.LineUtf8);
                                            
                        // Server returned success response.
                        if(string.Equals(op.LineUtf8.Split(new char[]{' '},2)[0],"+OK",StringComparison.InvariantCultureIgnoreCase)){
                            // Read capa-list.
                            SmartStream.ReadLineAsyncOP readLineOP = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                            readLineOP.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                                try{
                                    ReadMultiLineResponseLineCompleted(readLineOP);

                                    // Read response lines while we get terminator(.).
                                    while(this.State == AsyncOP_State.Active && m_pPop3Client.TcpStream.ReadLine(readLineOP,true)){
                                        ReadMultiLineResponseLineCompleted(readLineOP);
                                    }
                                }
                                catch(Exception x){
                                    m_pException = x;
                                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                                    SetState(AsyncOP_State.Completed);
                                }
                            };
                            // Read response lines while we get terminator(.).
                            while(this.State == AsyncOP_State.Active && m_pPop3Client.TcpStream.ReadLine(readLineOP,true)){
                                ReadMultiLineResponseLineCompleted(readLineOP);
                            }
                        }
                        // Server returned error response.
                        else{
                            m_pException = new POP3_ClientException(op.LineUtf8);
                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }

                op.Dispose();
            }

            #endregion

            #region method ReadMultiLineResponseLineCompleted
            
            /// <summary>
            /// Is called when POP3 server multiline response single line reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ReadMultiLineResponseLineCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Log
                        m_pPop3Client.LogAddRead(op.BytesInBuffer,op.LineUtf8);
                         
                        // Server closed connection.
                        if(op.BytesInBuffer == 0){
                            m_pException = new IOException("POP3 server closed connection unexpectedly.");
                            SetState(AsyncOP_State.Completed);
                        }
                        // We got respone terminator(.).
                        else if(string.Equals(op.LineUtf8,".",StringComparison.InvariantCultureIgnoreCase)){
                            m_pPop3Client.m_pExtCapabilities.Clear();
                            m_pPop3Client.m_pExtCapabilities.AddRange(m_pResponseLines);

                            SetState(AsyncOP_State.Completed);
                        }
                        // We got response line.
                        else{
                            m_pResponseLines.Add(op.LineUtf8);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
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
            /// Gets response lines.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public string[] ResponseLines
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Error' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pResponseLines.ToArray(); 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<ListAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<ListAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending LIST command to POP3 server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="ListAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        private bool ListAsync(ListAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
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

        #region method UidlAsync

        #region class UidlAsyncOP

        /// <summary>
        /// This class represents <see cref="POP3_Client.UidlAsync"/> asynchronous operation.
        /// </summary>
        private class UidlAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock          = new object();
            private AsyncOP_State m_State          = AsyncOP_State.WaitingForStart;
            private Exception     m_pException     = null;
            private POP3_Client   m_pPop3Client    = null;
            private bool          m_RiseCompleted  = false;
            private List<string>  m_pResponseLines = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public UidlAsyncOP()
            {
                m_pResponseLines = new List<string>();
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                if(m_State == AsyncOP_State.Disposed){
                    return;
                }
                SetState(AsyncOP_State.Disposed);
                
                m_pException     = null;
                m_pPop3Client    = null;
                m_pResponseLines = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner POP3 client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(POP3_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pPop3Client = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 1935.7. UIDL
                      Arguments:
                          a message-number (optional), which, if present, may NOT
                          refer to a message marked as deleted

                      Restrictions:
                          may only be given in the TRANSACTION state.

                      Discussion:
                          If an argument was given and the POP3 server issues a positive
                          response with a line containing information for that message.
                          This line is called a "unique-id listing" for that message.

                          If no argument was given and the POP3 server issues a positive
                          response, then the response given is multi-line.  After the
                          initial +OK, for each message in the maildrop, the POP3 server
                          responds with a line containing information for that message.
                          This line is called a "unique-id listing" for that message.

                          In order to simplify parsing, all POP3 servers are required to
                          use a certain format for unique-id listings.  A unique-id
                          listing consists of the message-number of the message,
                          followed by a single space and the unique-id of the message.
                          No information follows the unique-id in the unique-id listing.

                          The unique-id of a message is an arbitrary server-determined
                          string, consisting of one to 70 characters in the range 0x21
                          to 0x7E, which uniquely identifies a message within a
                          maildrop and which persists across sessions.  This
                          persistence is required even if a session ends without
                          entering the UPDATE state.  The server should never reuse an
                          unique-id in a given maildrop, for as long as the entity
                          using the unique-id exists.

                          Note that messages marked as deleted are not listed.

                          While it is generally preferable for server implementations
                          to store arbitrarily assigned unique-ids in the maildrop,
                          this specification is intended to permit unique-ids to be
                          calculated as a hash of the message.  Clients should be able
                          to handle a situation where two identical copies of a
                          message in a maildrop have the same unique-id.

                      Possible Responses:
                          +OK unique-id listing follows
                          -ERR no such message

                      Examples:
                          C: UIDL
                          S: +OK
                          S: 1 whqtswO00WBw418f9t5JxYwZ
                          S: 2 QhdPYR:00WBw1Ph7x7
                          S: .
                             ...
                          C: UIDL 2
                          S: +OK 2 QhdPYR:00WBw1Ph7x7
                             ...
                          C: UIDL 3
                          S: -ERR no such message, only 2 messages in maildrop
                    */

                    byte[] buffer = Encoding.UTF8.GetBytes("UIDL\r\n");

                    // Log
                    m_pPop3Client.LogAddWrite(buffer.Length,"UIDL");

                    // Start command sending.
                    m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.UidlCommandSendingCompleted,null);
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
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

            #region method UidlCommandSendingCompleted

            /// <summary>
            /// Is called when UIDL command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void UidlCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pPop3Client.TcpStream.EndWrite(ar);

                    // Read POP3 server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        UidlReadResponseCompleted(op);
                    };
                    if(m_pPop3Client.TcpStream.ReadLine(op,true)){
                        UidlReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method UidlReadResponseCompleted
            
            /// <summary>
            /// Is called when POP3 server UIDL response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void UidlReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Log
                        m_pPop3Client.LogAddRead(op.BytesInBuffer,op.LineUtf8);
                                            
                        // Server returned success response.
                        if(string.Equals(op.LineUtf8.Split(new char[]{' '},2)[0],"+OK",StringComparison.InvariantCultureIgnoreCase)){
                            // Read capa-list.
                            SmartStream.ReadLineAsyncOP readLineOP = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                            readLineOP.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                                try{
                                    ReadMultiLineResponseLineCompleted(readLineOP);

                                    // Read response lines while we get terminator(.).
                                    while(this.State == AsyncOP_State.Active && m_pPop3Client.TcpStream.ReadLine(readLineOP,true)){
                                        ReadMultiLineResponseLineCompleted(readLineOP);
                                    }
                                }
                                catch(Exception x){
                                    m_pException = x;
                                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                                    SetState(AsyncOP_State.Completed);
                                }
                            };
                            // Read response lines while we get terminator(.).
                            while(this.State == AsyncOP_State.Active && m_pPop3Client.TcpStream.ReadLine(readLineOP,true)){
                                ReadMultiLineResponseLineCompleted(readLineOP);
                            }
                        }
                        // Server returned error response.
                        else{
                            m_pException = new POP3_ClientException(op.LineUtf8);
                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }

                op.Dispose();
            }

            #endregion

            #region method ReadMultiLineResponseLineCompleted
            
            /// <summary>
            /// Is called when POP3 server multiline response single line reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ReadMultiLineResponseLineCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Operation failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pPop3Client.LogAddException("Exception: " + op.Error.Message,op.Error);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Operation succeeded.
                    else{
                        // Log
                        m_pPop3Client.LogAddRead(op.BytesInBuffer,op.LineUtf8);
                         
                        // Server closed connection.
                        if(op.BytesInBuffer == 0){
                            m_pException = new IOException("POP3 server closed connection unexpectedly.");
                            SetState(AsyncOP_State.Completed);
                        }
                        // We got respone terminator(.).
                        else if(string.Equals(op.LineUtf8,".",StringComparison.InvariantCultureIgnoreCase)){
                            m_pPop3Client.m_pExtCapabilities.Clear();
                            m_pPop3Client.m_pExtCapabilities.AddRange(m_pResponseLines);

                            SetState(AsyncOP_State.Completed);
                        }
                        // We got response line.
                        else{
                            m_pResponseLines.Add(op.LineUtf8);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
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
            /// Gets response lines.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public string[] ResponseLines
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Error' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pResponseLines.ToArray(); 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<UidlAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<UidlAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending UIDL command to POP3 server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="UidlAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        private bool UidlAsync(UidlAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
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


        #region override method OnConnected

        /// <summary>
        /// This method is called when TCP client has sucessfully connected.
        /// </summary>
        /// <param name="callback">Callback to be called to complete connect operation.</param>
        protected override void OnConnected(CompleteConnectCallback callback)
        {  
            // Read POP3 server greeting response.
            SmartStream.ReadLineAsyncOP readGreetingOP = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
            readGreetingOP.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                ReadServerGreetingCompleted(readGreetingOP,callback);
            };
            if(this.TcpStream.ReadLine(readGreetingOP,true)){
                ReadServerGreetingCompleted(readGreetingOP,callback);
            }
        }

        #endregion
        
        #region method ReadServerGreetingCompleted

        /// <summary>
        /// Is called when POP3 server greeting reading has completed.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <param name="connectCallback">Callback to be called to complete connect operation.</param>
        private void ReadServerGreetingCompleted(SmartStream.ReadLineAsyncOP op,CompleteConnectCallback connectCallback)
        {
            Exception error = null;

            try{
                // Greeting reading failed, we are done.
                if(op.Error != null){
                    error = op.Error;
                }
                // Greeting reading succeded.
                else{
                    string line = op.LineUtf8;

                    // Log.
                    LogAddRead(op.BytesInBuffer,line);

                    // POP3 server accepted connection, get greeting text.
                    if(op.LineUtf8.StartsWith("+OK",StringComparison.InvariantCultureIgnoreCase)){
                        m_GreetingText = line.Substring(3).Trim();

			            // Try to read APOP hash key, if supports APOP.
				        if(line.IndexOf("<") > -1 && line.IndexOf(">") > -1){
					        m_ApopHashKey = line.Substring(line.IndexOf("<"),line.LastIndexOf(">") - line.IndexOf("<") + 1);
				        }
                    }
                    // POP3 server rejected connection.
                    else{
                        error = new POP3_ClientException(line);
                    }
                }
            }
            catch(Exception x){
                error = x;
            }

            // Complete TCP_Client connect operation.
            connectCallback(error);
        }

        #endregion


        #region Properties Implementation
        
        /// <summary>
        /// Gets greeting text which was sent by POP3 server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and POP3 client is not connected.</exception>
        public string GreetingText
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
                    throw new InvalidOperationException("You must connect first.");
                }

                return m_GreetingText; 
            }
        }

        /// <summary>
        /// Gets POP3 exteneded capabilities supported by POP3 server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and POP3 client is not connected.</exception>
        [Obsolete("USe ExtendedCapabilities instead !")]
        public string[] ExtenededCapabilities
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
				    throw new InvalidOperationException("You must connect first.");
			    }

                return m_pExtCapabilities.ToArray(); 
            }
        }

        /// <summary>
        /// Gets POP3 exteneded capabilities supported by POP3 server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and POP3 client is not connected.</exception>
        public string[] ExtendedCapabilities
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
				    throw new InvalidOperationException("You must connect first.");
			    }

                return m_pExtCapabilities.ToArray(); 
            }
        }

        /// <summary>
        /// Gets if POP3 server supports UIDL command.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and 
        /// POP3 client is not connected and authenticated.</exception>
        public bool IsUidlSupported
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
				    throw new InvalidOperationException("You must connect first.");
			    }
                if(!this.IsAuthenticated){
				    throw new InvalidOperationException("You must authenticate first.");
			    }

                return m_IsUidlSupported; 
            }
        }

        /// <summary>
        /// Gets messages collection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and 
        /// POP3 client is not connected and authenticated.</exception>
        public POP3_ClientMessageCollection Messages
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
				    throw new InvalidOperationException("You must connect first.");
			    }
                if(!this.IsAuthenticated){
				    throw new InvalidOperationException("You must authenticate first.");
			    }

                return m_pMessages; 
            }
        }


        /// <summary>
        /// Gets session authenticated user identity, returns null if not authenticated.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and POP3 client is not connected.</exception>
        public override GenericIdentity AuthenticatedUserIdentity
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
				    throw new InvalidOperationException("You must connect first.");
			    }

                return m_pAuthdUserIdentity; 
            }
        }

		#endregion


        //--- Obsolete -------------------------------------------------------------------
                
        #region method StartTLS

        /// <summary>
        /// Switches POP3 connection to SSL.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected or is authenticated or is already secure connection.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>
        [Obsolete("Use Stls/StlsAsync method instead.")]
        public void StartTLS()
        {
            /* RFC 2595 4. POP3 STARTTLS extension.
                Arguments: none

                Restrictions:
                    Only permitted in AUTHORIZATION state.
             
                Possible Responses:
                     +OK -ERR

                 Examples:
                     C: STLS
                     S: +OK Begin TLS negotiation
                     <TLS negotiation, further commands are under TLS layer>
                       ...
                     C: STLS
                     S: -ERR Command not permitted when TLS active
            */

            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}
			if(this.IsAuthenticated){
				throw new InvalidOperationException("The STLS command is only valid in non-authenticated state.");
			}
            if(this.IsSecureConnection){
                throw new InvalidOperationException("Connection is already secure.");
            }
                        
            WriteLine("STLS");
                        
            string line = ReadLine();
			if(!line.ToUpper().StartsWith("+OK")){
				throw new POP3_ClientException(line);
			}

            this.SwitchToSecure();
        }

        #endregion

        #region method BeginStartTLS

        /// <summary>
        /// Internal helper method for asynchronous StartTLS method.
        /// </summary>
        private delegate void StartTLSDelegate();

        /// <summary>
        /// Starts switching to SSL.
        /// </summary>
        /// <returns>An IAsyncResult that references the asynchronous operation.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected or is authenticated or is already secure connection.</exception>
        [Obsolete("Use Stls/StlsAsync method instead.")]
        public IAsyncResult BeginStartTLS(AsyncCallback callback,object state)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}
			if(this.IsAuthenticated){
				throw new InvalidOperationException("The STLS command is only valid in non-authenticated state.");
			}
            if(this.IsSecureConnection){
                throw new InvalidOperationException("Connection is already secure.");
            }

            StartTLSDelegate asyncMethod = new StartTLSDelegate(this.StartTLS);
            AsyncResultState asyncState = new AsyncResultState(this,asyncMethod,callback,state);
            asyncState.SetAsyncResult(asyncMethod.BeginInvoke(new AsyncCallback(asyncState.CompletedCallback),null));

            return asyncState;
        }

        #endregion

        #region method EndStartTLS

        /// <summary>
        /// Ends a pending asynchronous StartTLS request.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that stores state information and any user defined data for this asynchronous operation.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid <b>asyncResult</b> passed to this method.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>
        [Obsolete("Use Stls/StlsAsync method instead.")]
        public void EndStartTLS(IAsyncResult asyncResult)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }

            AsyncResultState castedAsyncResult = asyncResult as AsyncResultState;
            if(castedAsyncResult == null || castedAsyncResult.AsyncObject != this){
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginReset method.");
            }
            if(castedAsyncResult.IsEndCalled){
                throw new InvalidOperationException("BeginReset was previously called for the asynchronous connection.");
            }
             
            castedAsyncResult.IsEndCalled = true;
            if(castedAsyncResult.AsyncDelegate is StartTLSDelegate){
                ((StartTLSDelegate)castedAsyncResult.AsyncDelegate).EndInvoke(castedAsyncResult.AsyncResult);
            }
            else{
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginReset method.");
            }
        }

        #endregion

        #region method Authenticate

        /// <summary>
		/// Authenticates user.
		/// </summary>
		/// <param name="userName">User login name.</param>
		/// <param name="password">Password.</param>
		/// <param name="tryApop"> If true and POP3 server supports APOP, then APOP is used, otherwise normal login used.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected or is already authenticated.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>
        [Obsolete("Use Login/LoginAsync method instead.")]
		public void Authenticate(string userName,string password,bool tryApop)
		{
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}
			if(this.IsAuthenticated){
				throw new InvalidOperationException("Session is already authenticated.");
			}
            
			// Supports APOP, use it.
			if(tryApop && m_ApopHashKey.Length > 0){
                string hexHash = Net_Utils.ComputeMd5(m_ApopHashKey + password,true);
                                
				int countWritten = this.TcpStream.WriteLine("APOP " + userName + " " + hexHash);
                LogAddWrite(countWritten,"APOP " + userName + " " + hexHash);

                string line = this.ReadLine();
				if(line.StartsWith("+OK")){
					m_pAuthdUserIdentity = new GenericIdentity(userName,"apop");
				}
				else{
					throw new POP3_ClientException(line);
				}
			}
            // Use normal LOGIN, don't support APOP.
			else{                 
				int countWritten = this.TcpStream.WriteLine("USER " + userName);
                LogAddWrite(countWritten,"USER " + userName);

                string line = this.ReadLine();
				if(line.StartsWith("+OK")){                    
					countWritten = this.TcpStream.WriteLine("PASS " + password);
                    LogAddWrite(countWritten,"PASS <***REMOVED***>");

					line = this.ReadLine();
					if(line.StartsWith("+OK")){
						m_pAuthdUserIdentity = new GenericIdentity(userName,"pop3-user/pass");
					}
					else{
						throw new POP3_ClientException(line);
					}
				}
				else{
					throw new POP3_ClientException(line);
				}				
			}

            if(this.IsAuthenticated){
                FillMessages();
            }
		}

		#endregion

        #region method BeginAuthenticate

        /// <summary>
        /// Internal helper method for asynchronous Authenticate method.
        /// </summary>
        private delegate void AuthenticateDelegate(string userName,string password,bool tryApop);

        /// <summary>
        /// Starts authentication.
        /// </summary>
		/// <param name="userName">User login name.</param>
		/// <param name="password">Password.</param>
		/// <param name="tryApop"> If true and POP3 server supports APOP, then APOP is used, otherwise normal login used.</param>
        /// <param name="callback">Callback to call when the asynchronous operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous operation.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected or is already authenticated.</exception>
        [Obsolete("Use Login/LoginAsync method instead.")]
        public IAsyncResult BeginAuthenticate(string userName,string password,bool tryApop,AsyncCallback callback,object state)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}
			if(this.IsAuthenticated){
				throw new InvalidOperationException("Session is already authenticated.");
			}

            AuthenticateDelegate asyncMethod = new AuthenticateDelegate(this.Authenticate);
            AsyncResultState asyncState = new AsyncResultState(this,asyncMethod,callback,state);
            asyncState.SetAsyncResult(asyncMethod.BeginInvoke(userName,password,tryApop,new AsyncCallback(asyncState.CompletedCallback),null));

            return asyncState;
        }

        #endregion

        #region method EndAuthenticate

        /// <summary>
        /// Ends a pending asynchronous authentication request.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that stores state information and any user defined data for this asynchronous operation.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid <b>asyncResult</b> passed to this method.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>
        [Obsolete("Use Login/LoginAsync method instead.")]
        public void EndAuthenticate(IAsyncResult asyncResult)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }

            AsyncResultState castedAsyncResult = asyncResult as AsyncResultState;
            if(castedAsyncResult == null || castedAsyncResult.AsyncObject != this){
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginAuthenticate method.");
            }
            if(castedAsyncResult.IsEndCalled){
                throw new InvalidOperationException("BeginAuthenticate was previously called for the asynchronous connection.");
            }
             
            castedAsyncResult.IsEndCalled = true;
            if(castedAsyncResult.AsyncDelegate is AuthenticateDelegate){
                ((AuthenticateDelegate)castedAsyncResult.AsyncDelegate).EndInvoke(castedAsyncResult.AsyncResult);
            }
            else{
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginAuthenticate method.");
            }
        }

        #endregion

        #region method BeginNoop

        /// <summary>
        /// Internal helper method for asynchronous Noop method.
        /// </summary>
        private delegate void NoopDelegate();

        /// <summary>
        /// Starts sending NOOP command to server. This method can be used for keeping connection alive(not timing out).
        /// </summary>
        /// <param name="callback">Callback to call when the asynchronous operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous operation.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected.</exception>
        [Obsolete("Use Noop/NoopAsync method instead.")]
        public IAsyncResult BeginNoop(AsyncCallback callback,object state)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}

            NoopDelegate asyncMethod = new NoopDelegate(this.Noop);
            AsyncResultState asyncState = new AsyncResultState(this,asyncMethod,callback,state);
            asyncState.SetAsyncResult(asyncMethod.BeginInvoke(new AsyncCallback(asyncState.CompletedCallback),null));

            return asyncState;
        }

        #endregion

        #region method EndNoop

        /// <summary>
        /// Ends a pending asynchronous Noop request.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that stores state information and any user defined data for this asynchronous operation.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid <b>asyncResult</b> passed to this method.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>
        [Obsolete("Use Noop/NoopAsync method instead.")]
        public void EndNoop(IAsyncResult asyncResult)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }

            AsyncResultState castedAsyncResult = asyncResult as AsyncResultState;
            if(castedAsyncResult == null || castedAsyncResult.AsyncObject != this){
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginNoop method.");
            }
            if(castedAsyncResult.IsEndCalled){
                throw new InvalidOperationException("BeginNoop was previously called for the asynchronous connection.");
            }
             
            castedAsyncResult.IsEndCalled = true;
            if(castedAsyncResult.AsyncDelegate is NoopDelegate){
                ((NoopDelegate)castedAsyncResult.AsyncDelegate).EndInvoke(castedAsyncResult.AsyncResult);
            }
            else{
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginNoop method.");
            }
        }

        #endregion

        #region method BeginReset

        /// <summary>
        /// Internal helper method for asynchronous Reset method.
        /// </summary>
        private delegate void ResetDelegate();

        /// <summary>
        /// Starts resetting session. Messages marked for deletion will be unmarked.
        /// </summary>
        /// <param name="callback">Callback to call when the asynchronous operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous operation.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected and authenticated.</exception>
        [Obsolete("Use Rset/RsetAsync method instead.")]
        public IAsyncResult BeginReset(AsyncCallback callback,object state)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}
			if(!this.IsAuthenticated){
				throw new InvalidOperationException("The RSET command is only valid in authenticated state.");
			}

            ResetDelegate asyncMethod = new ResetDelegate(this.Reset);
            AsyncResultState asyncState = new AsyncResultState(this,asyncMethod,callback,state);
            asyncState.SetAsyncResult(asyncMethod.BeginInvoke(new AsyncCallback(asyncState.CompletedCallback),null));

            return asyncState;
        }

        #endregion

        #region method EndReset

        /// <summary>
        /// Ends a pending asynchronous reset request.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that stores state information and any user defined data for this asynchronous operation.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid <b>asyncResult</b> passed to this method.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>
        [Obsolete("Use Rset/RsetAsync method instead.")]
        public void EndReset(IAsyncResult asyncResult)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }

            AsyncResultState castedAsyncResult = asyncResult as AsyncResultState;
            if(castedAsyncResult == null || castedAsyncResult.AsyncObject != this){
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginReset method.");
            }
            if(castedAsyncResult.IsEndCalled){
                throw new InvalidOperationException("BeginReset was previously called for the asynchronous connection.");
            }
             
            castedAsyncResult.IsEndCalled = true;
            if(castedAsyncResult.AsyncDelegate is ResetDelegate){
                ((ResetDelegate)castedAsyncResult.AsyncDelegate).EndInvoke(castedAsyncResult.AsyncResult);
            }
            else{
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginReset method.");
            }
        }

        #endregion

        #region method Reset

        /// <summary>
		/// Resets session. Messages marked for deletion will be unmarked.
		/// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not connected and authenticated.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 server returns error.</exception>        
        [Obsolete("Use Rset/RsetAsync method instead.")]
		public void Reset()
		{
			if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}
			if(!this.IsAuthenticated){
				throw new InvalidOperationException("The RSET command is only valid in TRANSACTION state.");
			}

            using(RsetAsyncOP op = new RsetAsyncOP()){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<RsetAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.RsetAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();
                    wait.Close();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
		}

		#endregion

        #region method FillMessages

        /// <summary>
        /// Fills messages info.
        /// </summary>
        [Obsolete("deprecated")]
        private void FillMessages()
        {
            m_pMessages = new POP3_ClientMessageCollection(this);

            /*
                First make messages info, then try to add UIDL if server supports.
            */
                                   
			/* NOTE: If reply is +OK, this is multiline respone and is terminated with '.'.
			Examples:
				C: LIST
				S: +OK 2 messages (320 octets)
				S: 1 120				
				S: 2 200
				S: .
				...
				C: LIST 3
				S: -ERR no such message, only 2 messages in maildrop
			*/
                        
            WriteLine("LIST");

			// Read first line of reply, check if it's ok.
			string line = ReadLine();
			if(line.StartsWith("+OK")){
				// Read lines while get only '.' on line itshelf.
				while(true){
					line = ReadLine();

					// End of data
					if(line.Trim() == "."){
						break;
					}
					else{
                        string[] no_size = line.Trim().Split(new char[]{' '});
                        m_pMessages.Add(Convert.ToInt32(no_size[1]));
					}
				}
			}
			else{
				throw new POP3_ClientException(line);
			}

            // Try to fill messages UIDs.
            /* NOTE: If reply is +OK, this is multiline respone and is terminated with '.'.
			Examples:
				C: UIDL
				S: +OK
				S: 1 whqtswO00WBw418f9t5JxYwZ
				S: 2 QhdPYR:00WBw1Ph7x7
				S: .
				...
				C: UIDL 3
				S: -ERR no such message
			*/

            WriteLine("UIDL");

			// Read first line of reply, check if it's ok
			line = ReadLine();
			if(line.StartsWith("+OK")){
                m_IsUidlSupported = true;

				// Read lines while get only '.' on line itshelf.
				while(true){
					line = ReadLine();

					// End of data
					if(line.Trim() == "."){
						break;
					}
					else{
                        string[] no_uid = line.Trim().Split(new char[]{' '});                        
                        m_pMessages[Convert.ToInt32(no_uid[0]) - 1].SetUID(no_uid[1]);
					}
				}
			}
			else{
				m_IsUidlSupported = false;
			}
        }

        #endregion
	}
}
