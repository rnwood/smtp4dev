using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Security.Principal;
using System.Threading;
using System.Net.Security;

using LumiSoft.Net.IO;
using LumiSoft.Net.TCP;
using LumiSoft.Net.AUTH;
using LumiSoft.Net.IMAP;
using LumiSoft.Net.MIME;

namespace LumiSoft.Net.IMAP.Client
{
    /// <summary>
    /// IMAP v4 Client. Defined in RFC 3501.
    /// </summary>
	/// <example>
	/// <code>
	/// /*
	///  To make this code to work, you need to import following namespaces:
	///  using LumiSoft.Net.IMAP.Client; 
	/// */
	/// 
	/// using(IMAP_Client imap = new IMAP_Client()){
    ///     imap.Connect("host",143);
    ///     // Call Capability even if you don't care about capabilities, it also controls IMAP client features.
    ///     imap.Capability();
    ///     
    ///     imap.Authenticate(... choose auth method ...);
    /// 
    ///     // Do do your stuff ...
    /// }
	/// </code>
	/// </example>
    public class IMAP_Client : TCP_Client
    {
        #region class CmdLine

        /// <summary>
        /// This class represent IMAP single command line.
        /// </summary>
        internal class CmdLine
        {
            private byte[] m_pData   = null;
            private string m_LogText = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="data">Command line data.</param>
            /// <param name="logText">Command line log text.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>data</b> or <b>logText</b> is null reference.</exception>
            public CmdLine(byte[] data,string logText)
            {
                if(data == null){
                    throw new ArgumentNullException("data");
                }
                if(logText == null){
                    throw new ArgumentNullException("logText");
                }

                m_pData   = data;
                m_LogText = logText;
            }


            #region Properties implementation

            /// <summary>
            /// Gets command line data.
            /// </summary>
            public byte[] Data
            {
                get{ return m_pData; }
            }

            /// <summary>
            /// Gets command line data.
            /// </summary>
            public string LogText
            {
                get{ return m_LogText; }
            }

            #endregion
        }

        #endregion

        #region class CmdAsyncOP

        /// <summary>
        /// This class is base class for simple(request -> response) IMAP commands.
        /// </summary>
        public abstract class CmdAsyncOP<T> : IDisposable,IAsyncOP where T:IAsyncOP
        {
            private object                            m_pLock          = new object();
            private AsyncOP_State                     m_State          = AsyncOP_State.WaitingForStart;
            private Exception                         m_pException     = null;
            private IMAP_r_ServerStatus               m_pFinalResponse = null;
            private IMAP_Client                       m_pImapClient    = null;
            private bool                              m_RiseCompleted  = false;
            private List<CmdLine>                     m_pCmdLines      = null;
            private EventHandler<EventArgs<IMAP_r_u>> m_pCallback      = null;
                        
            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public CmdAsyncOP(EventHandler<EventArgs<IMAP_r_u>> callback)
            {
                m_pCallback = callback;

                m_pCmdLines = new List<CmdLine>();
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
                m_pImapClient    = null;
                m_pFinalResponse = null;
                m_pCallback      = null;
                m_pCmdLines      = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                                
                m_pImapClient = owner;
                        
                SetState(AsyncOP_State.Active);

                try{
                    // Force inhereted class to fill command line info.
                    OnInitCmdLine(owner);

                    SendCmdAndReadRespAsyncOP op = new SendCmdAndReadRespAsyncOP(m_pCmdLines.ToArray(),m_pCallback);
                    op.CompletedAsync += delegate(object sender,EventArgs<SendCmdAndReadRespAsyncOP> e){
                        try{
                            // Command send/receive failed.
                            if(op.Error != null){
                                m_pException = e.Value.Error;
                                m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                            }
                            // Command send/receive succeeded.
                            else{
                                m_pFinalResponse = op.FinalResponse;

                                // IMAP server returned error response.
                                if(op.FinalResponse.IsError){
                                    m_pException = new IMAP_ClientException(op.FinalResponse);
                                }
                            }

                            SetState(AsyncOP_State.Completed);
                        }
                        finally{
                            op.Dispose();
                        }
                    };
                    // Operation completed synchronously.
                    if(!m_pImapClient.SendCmdAndReadRespAsync(op)){
                        try{
                            // Command send/receive failed.
                            if(op.Error != null){
                                m_pException = op.Error;
                                m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                            }
                            // Command send/receive succeeded.
                            else{
                                m_pFinalResponse = op.FinalResponse;

                                // IMAP server returned error response.
                                if(op.FinalResponse.IsError){
                                    m_pException = new IMAP_ClientException(op.FinalResponse);
                                }
                            }

                            SetState(AsyncOP_State.Completed);
                        }
                        finally{
                            op.Dispose();
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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


            #region abstract method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected abstract void OnInitCmdLine(IMAP_Client imap);

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
            /// Returns IMAP server final response.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_r_ServerStatus FinalResponse
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pFinalResponse; 
                }
            }

                        
            /// <summary>
            /// Gets command lines.
            /// </summary>
            internal List<CmdLine> CmdLines
            {
                get{ return m_pCmdLines; }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<T>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<T>((T)((object)this)));
                }
            }

            #endregion

            #endregion
        }

        #endregion
   
        private GenericIdentity            m_pAuthenticatedUser = null;
        private string                     m_GreetingText       = "";
        private int                        m_CommandIndex       = 1;
        private List<string>               m_pCapabilities      = null;
        private IMAP_Client_SelectedFolder m_pSelectedFolder    = null;
        private IMAP_Mailbox_Encoding      m_MailboxEncoding    = IMAP_Mailbox_Encoding.ImapUtf7;
        private IdleAsyncOP                m_pIdle              = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public IMAP_Client()
        {
        }


        #region override method Disconnect

		/// <summary>
		/// Closes connection to IMAP server.
		/// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not connected.</exception>
		public override void Disconnect()
		{
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("IMAP client is not connected.");
            }

			try{
                // Send LOGOUT command to server.                
                WriteLine((m_CommandIndex++).ToString("d5") + " LOGOUT");
			}
			catch{
			}

            try{
                base.Disconnect(); 
            }
            catch{
            }

            // Reset state varibles.
            m_pAuthenticatedUser = null;
            m_GreetingText       = "";
            m_CommandIndex       = 1;
            m_pCapabilities      = null;
            m_pSelectedFolder    = null;
            m_MailboxEncoding    = IMAP_Mailbox_Encoding.ImapUtf7;
		}

		#endregion


        #region method StartTls

        /// <summary>
        /// Switches connection to secure connection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void StartTls()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(this.IsSecureConnection){
                throw new InvalidOperationException("Connection is already secure.");
            }
            if(this.IsAuthenticated){
                throw new InvalidOperationException("STARTTLS is only valid in not-authenticated state.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }

            using(StartTlsAsyncOP op = new StartTlsAsyncOP(null,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<StartTlsAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.StartTlsAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }            
        }

        #endregion

        #region method StartTlsAsync

        #region class StartTlsAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.StartTlsAsync"/> asynchronous operation.
        /// </summary>
        public class StartTlsAsyncOP : IDisposable,IAsyncOP
        {
            private object                              m_pLock          = new object();
            private AsyncOP_State                       m_State          = AsyncOP_State.WaitingForStart;
            private Exception                           m_pException     = null;
            private RemoteCertificateValidationCallback m_pCertCallback  = null;
            private IMAP_r_ServerStatus                 m_pFinalResponse = null;
            private IMAP_Client                         m_pImapClient    = null;
            private bool                                m_RiseCompleted  = false;
            private EventHandler<EventArgs<IMAP_r_u>>   m_pCallback      = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="certCallback">SSL server certificate validation callback. Value null means any certificate is accepted.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public StartTlsAsyncOP(RemoteCertificateValidationCallback certCallback,EventHandler<EventArgs<IMAP_r_u>> callback)
            {                
                m_pCertCallback = certCallback;
                m_pCallback     = callback;
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
                m_pImapClient    = null;
                m_pFinalResponse = null;
                m_pCallback      = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                                
                m_pImapClient = owner;
                        
                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 3501 6.2.1. STARTTLS Command.
                        Arguments:  none

                        Responses:  no specific response for this command

                        Result:     OK - starttls completed, begin TLS negotiation
                                    BAD - command unknown or arguments invalid

                        A [TLS] negotiation begins immediately after the CRLF at the end
                        of the tagged OK response from the server.  Once a client issues a
                        STARTTLS command, it MUST NOT issue further commands until a
                        server response is seen and the [TLS] negotiation is complete.

                        The server remains in the non-authenticated state, even if client
                        credentials are supplied during the [TLS] negotiation.  This does
                        not preclude an authentication mechanism such as EXTERNAL (defined
                        in [SASL]) from using client identity determined by the [TLS]
                        negotiation.

                        Once [TLS] has been started, the client MUST discard cached
                        information about server capabilities and SHOULD re-issue the
                        CAPABILITY command.  This is necessary to protect against man-in-
                        the-middle attacks which alter the capabilities list prior to
                        STARTTLS. 
                    */

                    byte[] cmdLine    = Encoding.UTF8.GetBytes((m_pImapClient.m_CommandIndex++).ToString("d5") + " STARTTLS\r\n");
                    string cmdLineLog = Encoding.UTF8.GetString(cmdLine).TrimEnd();

                    SendCmdAndReadRespAsyncOP args = new SendCmdAndReadRespAsyncOP(cmdLine,cmdLineLog,m_pCallback);
                    args.CompletedAsync += delegate(object sender,EventArgs<SendCmdAndReadRespAsyncOP> e){
                        ProcessCmdResult(e.Value);
                    };
                    // Operation completed synchronously.
                    if(!m_pImapClient.SendCmdAndReadRespAsync(args)){
                        ProcessCmdResult(args);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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

            #region method ProcessCmdResult

            /// <summary>
            /// Processes STARTTLS command result.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ProcessCmdResult(SendCmdAndReadRespAsyncOP op)
            {
                try{
                    // Command send/receive failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                    }
                    // Command send/receive succeeded.
                    else{
                        m_pFinalResponse = op.FinalResponse;

                        // IMAP server returned error response.
                        if(op.FinalResponse.IsError){
                            m_pException = new IMAP_ClientException(op.FinalResponse);
                        }
                        // IMAP server returned success response.
                        else{
                            // Start TLS/SSl handshake.
                            TCP_Client.SwitchToSecureAsyncOP tlsOP = new SwitchToSecureAsyncOP(m_pCertCallback);
                            tlsOP.CompletedAsync += delegate(object sender,EventArgs<SwitchToSecureAsyncOP> e){
                                if(e.Value.Error != null){
                                    m_pException = e.Value.Error;
                                }
                                
                                SetState(AsyncOP_State.Completed);
                            };
                            // Operation completed synchronously.
                            if(!m_pImapClient.SwitchToSecureAsync(tlsOP)){
                                if(tlsOP.Error != null){
                                    m_pException = tlsOP.Error;
                                }

                                SetState(AsyncOP_State.Completed);
                            }
                        }
                    }

                    SetState(AsyncOP_State.Completed);
                }
                finally{
                    op.Dispose();
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
            /// Returns IMAP server final response.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_r_ServerStatus FinalResponse
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pFinalResponse; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<StartTlsAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<StartTlsAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes STARTTLS command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="StartTlsAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool StartTlsAsync(StartTlsAsyncOP op)
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
            if(this.IsAuthenticated){
                throw new InvalidOperationException("STARTTLS is only valid in not-authenticated state.");
            }          
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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
        /// Authenticates user using IMAP-LOGIN method.
        /// </summary>
        /// <param name="user">User name.</param>
        /// <param name="password">Password.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>user</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void Login(string user,string password)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(this.IsAuthenticated){
                throw new InvalidOperationException("Re-authentication error, you are already authenticated.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(user == null){
                throw new ArgumentNullException("user");
            }
            if(user == string.Empty){
                throw new ArgumentException("Argument 'user' value must be specified.");
            }
                        
            using(LoginAsyncOP op = new LoginAsyncOP(user,password,null)){
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
        /// This class represents <see cref="IMAP_Client.LoginAsync"/> asynchronous operation.
        /// </summary>
        public class LoginAsyncOP : IDisposable,IAsyncOP
        {
            private object                            m_pLock          = new object();
            private AsyncOP_State                     m_State          = AsyncOP_State.WaitingForStart;
            private Exception                         m_pException     = null;
            private IMAP_r_ServerStatus               m_pFinalResponse = null;
            private IMAP_Client                       m_pImapClient    = null;
            private bool                              m_RiseCompleted  = false;
            private string                            m_User           = null;
            private string                            m_Password       = null;
            private EventHandler<EventArgs<IMAP_r_u>> m_pCallback      = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="user">User login name.</param>
            /// <param name="password">User password.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>user</b> or <b>password</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public LoginAsyncOP(string user,string password,EventHandler<EventArgs<IMAP_r_u>> callback)
            {
                if(user == null){
                    throw new ArgumentNullException("user");
                }
                if(string.IsNullOrEmpty(user)){
                    throw new ArgumentException("Argument 'user' value must be specified.","user");
                }
                if(password == null){
                    throw new ArgumentNullException("password");
                }

                m_User      = user;
                m_Password  = password;
                m_pCallback = callback;
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
                m_pImapClient    = null;
                m_pFinalResponse = null;
                m_pCallback      = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                                
                m_pImapClient = owner;
                        
                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 3501 6.2.3.  LOGIN Command
                        Arguments:  user name
                                    password

                        Responses:  no specific responses for this command

                        Result:     OK - login completed, now in authenticated state
                                    NO - login failure: user name or password rejected
                                    BAD - command unknown or arguments invalid

                        The LOGIN command identifies the client to the server and carries
                        the plaintext password authenticating this user.

                        A server MAY include a CAPABILITY response code in the tagged OK
                        response to a successful LOGIN command in order to send
                        capabilities automatically.  It is unnecessary for a client to
                        send a separate CAPABILITY command if it recognizes these
                        automatic capabilities.
                    */

                    byte[] cmdLine    = Encoding.UTF8.GetBytes((m_pImapClient.m_CommandIndex++).ToString("d5") + " LOGIN " + TextUtils.QuoteString(m_User) + " " + TextUtils.QuoteString(m_Password) + "\r\n");
                    string cmdLineLog = (m_pImapClient.m_CommandIndex - 1).ToString("d5") + " LOGIN " + TextUtils.QuoteString(m_User) + " <PASSWORD-REMOVED>";

                    SendCmdAndReadRespAsyncOP args = new SendCmdAndReadRespAsyncOP(cmdLine,cmdLineLog,m_pCallback);
                    args.CompletedAsync += delegate(object sender,EventArgs<SendCmdAndReadRespAsyncOP> e){
                        try{
                            // Command send/receive failed.
                            if(args.Error != null){
                                m_pException = e.Value.Error;
                            }
                            // Command send/receive succeeded.
                            else{
                                m_pFinalResponse = args.FinalResponse;

                                // IMAP server returned error response.
                                if(args.FinalResponse.IsError){
                                    m_pException = new IMAP_ClientException(args.FinalResponse);
                                }
                                // IMAP server returned success response.
                                else{
                                    m_pImapClient.m_pAuthenticatedUser = new GenericIdentity(m_User,"IMAP-LOGIN");
                                }
                            }

                            SetState(AsyncOP_State.Completed);
                        }
                        finally{
                            args.Dispose();
                        }
                    };
                    // Operation completed synchronously.
                    if(!m_pImapClient.SendCmdAndReadRespAsync(args)){
                        try{
                            // Command send/receive failed.
                            if(args.Error != null){
                                m_pException = args.Error;
                            }
                            // Command send/receive succeeded.
                            else{
                                m_pFinalResponse = args.FinalResponse;

                                // IMAP server returned error response.
                                if(args.FinalResponse.IsError){
                                    m_pException = new IMAP_ClientException(args.FinalResponse);
                                }
                                // IMAP server returned success response.
                                else{
                                    m_pImapClient.m_pAuthenticatedUser = new GenericIdentity(m_User,"IMAP-LOGIN");
                                }
                            }

                            SetState(AsyncOP_State.Completed);
                        }
                        finally{
                            args.Dispose();
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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
            /// Returns IMAP server final response.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_r_ServerStatus FinalResponse
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pFinalResponse; 
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
        /// Executes LOGIN command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="LoginAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool LoginAsync(LoginAsyncOP op)
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
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method Authenticate

        /// <summary>
        /// Sends AUTHENTICATE command to IMAP server.
        /// </summary>
        /// <param name="sasl">SASL authentication.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>sasl</b> is null reference.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when IMAP server returns error.</exception>
        public void Authenticate(AUTH_SASL_Client sasl)
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
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(sasl == null){
                throw new ArgumentNullException("sasl");
            }
                        
            using(AuthenticateAsyncOP op = new AuthenticateAsyncOP(sasl)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<AuthenticateAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.AuthenticateAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();
                    
                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method AuthenticateAsync

        #region class AuthenticateAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.AuthenticateAsync"/> asynchronous operation.
        /// </summary>
        public class AuthenticateAsyncOP : IDisposable,IAsyncOP
        {
            private object           m_pLock         = new object();
            private AsyncOP_State    m_State         = AsyncOP_State.WaitingForStart;
            private Exception        m_pException    = null;
            private IMAP_Client      m_pImapClient   = null;
            private AUTH_SASL_Client m_pSASL         = null;
            private bool             m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="sasl">SASL authentication.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>sasl</b> is null reference.</exception>
            public AuthenticateAsyncOP(AUTH_SASL_Client sasl)
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
                m_pImapClient = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pImapClient = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 3501 6.2.2.  AUTHENTICATE Command.

                        Arguments:  authentication mechanism name

                        Responses:  continuation data can be requested

                        Result:     OK - authenticate completed, now in authenticated state
                                    NO - authenticate failure: unsupported authentication
                                         mechanism, credentials rejected
                                    BAD - command unknown or arguments invalid,
                                          authentication exchange cancelled
                    */

                    if(m_pSASL.SupportsInitialResponse && m_pImapClient.SupportsCapability("SASL-IR")){
                        byte[] buffer = Encoding.UTF8.GetBytes((m_pImapClient.m_CommandIndex++).ToString("d5") + " AUTHENTICATE " + m_pSASL.Name + " " + Convert.ToBase64String(m_pSASL.Continue(null)) + "\r\n");
                            
                        // Log
                        m_pImapClient.LogAddWrite(buffer.Length,Encoding.UTF8.GetString(buffer).TrimEnd());

                        // Start command sending.
                        m_pImapClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.AuthenticateCommandSendingCompleted,null);
                    }
                    else{
                        byte[] buffer = Encoding.UTF8.GetBytes((m_pImapClient.m_CommandIndex++).ToString("d5") + " AUTHENTICATE " + m_pSASL.Name + "\r\n");

                        // Log
                        m_pImapClient.LogAddWrite(buffer.Length,(m_pImapClient.m_CommandIndex++).ToString("d5") + " AUTHENTICATE " + m_pSASL.Name);

                        // Start command sending.
                        m_pImapClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.AuthenticateCommandSendingCompleted,null);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + x.Message,x);
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

            #region method AuthenticateCommandSendingCompleted

            /// <summary>
            /// Is called when AUTHENTICATE command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void AuthenticateCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pImapClient.TcpStream.EndWrite(ar);

                    // Read IMAP server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        AuthenticateReadResponseCompleted(op);
                    };
                    if(m_pImapClient.TcpStream.ReadLine(op,true)){
                        AuthenticateReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method AuthenticateReadResponseCompleted
            
            /// <summary>
            /// Is called when IMAP server response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void AuthenticateReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
            {
                try{
                    // Log
                    m_pImapClient.LogAddRead(op.BytesInBuffer,op.LineUtf8);

                    // Continue authenticating.
                    if(op.LineUtf8.StartsWith("+")){
                        // + base64Data, we need to decode it.
                        byte[] serverResponse = Convert.FromBase64String(op.LineUtf8.Split(new char[]{' '},2)[1]);

                        byte[] clientResponse = m_pSASL.Continue(serverResponse);

                        // We need just send SASL returned auth-response as base64.
                        byte[] buffer = Encoding.UTF8.GetBytes(Convert.ToBase64String(clientResponse) + "\r\n");

                        // Log
                        m_pImapClient.LogAddWrite(buffer.Length,Convert.ToBase64String(clientResponse));

                        // Start auth-data sending.
                        m_pImapClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.AuthenticateCommandSendingCompleted,null);
                    }
                    // Authentication suceeded.
                    else if(string.Equals(op.LineUtf8.Split(new char[]{' '},3)[1],"OK",StringComparison.InvariantCultureIgnoreCase)){
                        m_pImapClient.m_pAuthenticatedUser = new GenericIdentity(m_pSASL.UserName,m_pSASL.Name);

                        SetState(AsyncOP_State.Completed);
                    }
                    // Authentication rejected.
                    else{
                        m_pException = new IMAP_ClientException(op.LineUtf8);
                        SetState(AsyncOP_State.Completed);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + x.Message,x);
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
            public event EventHandler<EventArgs<AuthenticateAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<AuthenticateAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending AUTHENTICATE command to IMAP server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="AuthenticateAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not connected or connection is already authenticated.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool AuthenticateAsync(AuthenticateAsyncOP op)
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
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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


        #region method GetNamespaces

        /// <summary>
        /// Gets IMAP server namespaces.
        /// </summary>
        /// <returns>Returns namespaces responses.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public IMAP_r_u_Namespace[] GetNamespaces()
        {   
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }

            List<IMAP_r_u_Namespace> retVal = new List<IMAP_r_u_Namespace>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_Namespace){
                    retVal.Add((IMAP_r_u_Namespace)e.Value);
                }
            };

            using(GetNamespacesAsyncOP op = new GetNamespacesAsyncOP(callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<GetNamespacesAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.GetNamespacesAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetNamespacesAsync

        #region class GetNamespacesAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.GetNamespacesAsync"/> asynchronous operation.
        /// </summary>
        public class GetNamespacesAsyncOP : CmdAsyncOP<GetNamespacesAsyncOP>
        {
            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public GetNamespacesAsyncOP(EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {   
                /* RFC 2342 5. NAMESPACE Command.
                    Arguments: none

                    Response:  an untagged NAMESPACE response that contains the prefix
                               and hierarchy delimiter to the server's Personal
                               Namespace(s), Other Users' Namespace(s), and Shared
                               Namespace(s) that the server wishes to expose. The
                               response will contain a NIL for any namespace class
                               that is not available. Namespace_Response_Extensions
                               MAY be included in the response.
                               Namespace_Response_Extensions which are not on the IETF
                               standards track, MUST be prefixed with an "X-".

                    Result:    OK - Command completed
                               NO - Error: Can't complete command
                               BAD - argument invalid
                        
                    Example:
                        < A server that contains a Personal Namespace and a single Shared Namespace. >

                        C: A001 NAMESPACE
                        S: * NAMESPACE (("" "/")) NIL (("Public Folders/" "/"))
                        S: A001 OK NAMESPACE command completed
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " NAMESPACE" + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes NAMESPACE command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool GetNamespacesAsync(GetNamespacesAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method GetFolders

        /// <summary>
        /// Gets folders list.
        /// </summary>
        /// <param name="filter">Folders filter. If this value is null, all folders are returned.</param>
        /// <returns>Returns folders list.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        /// <remarks>
        /// The character "*" is a wildcard, and matches zero or more
        /// characters at this position.  The character "%" is similar to "*",
        /// but it does not match a hierarchy delimiter.  If the "%" wildcard
        /// is the last character of a mailbox name argument, matching levels
        /// of hierarchy are also returned.
        /// </remarks>
        public IMAP_r_u_List[] GetFolders(string filter)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }

            List<IMAP_r_u_List> retVal = new List<IMAP_r_u_List>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_List){
                    retVal.Add((IMAP_r_u_List)e.Value);
                }
            };

            using(GetFoldersAsyncOP op = new GetFoldersAsyncOP(filter,callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<GetFoldersAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.GetFoldersAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetFoldersAsync

        #region class GetFoldersAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.GetFoldersAsync"/> asynchronous operation.
        /// </summary>
        public class GetFoldersAsyncOP : CmdAsyncOP<GetFoldersAsyncOP>
        {
            private string m_Filter = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="filter">Folders filter. If this value is null, all folders are returned.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            /// <remarks>
            /// The character "*" is a wildcard, and matches zero or more
            /// characters at this position.  The character "%" is similar to "*",
            /// but it does not match a hierarchy delimiter.  If the "%" wildcard
            /// is the last character of a mailbox name argument, matching levels
            /// of hierarchy are also returned.
            /// </remarks>
            public GetFoldersAsyncOP(string filter,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                m_Filter = filter;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {           
                /* RFC 3501 6.3.8. LIST Command.
                    Arguments:  reference name
                                mailbox name with possible wildcards

                    Responses:  untagged responses: LIST

                    Result:     OK - list completed
                                NO - list failure: can't list that reference or name
                                BAD - command unknown or arguments invalid

                    The LIST command returns a subset of names from the complete set
                    of all names available to the client.  Zero or more untagged LIST
                    replies are returned, containing the name attributes, hierarchy
                    delimiter, and name; see the description of the LIST reply for
                    more detail.

                    An empty ("" string) reference name argument indicates that the
                    mailbox name is interpreted as by SELECT.  The returned mailbox
                    names MUST match the supplied mailbox name pattern.  A non-empty
                    reference name argument is the name of a mailbox or a level of
                    mailbox hierarchy, and indicates the context in which the mailbox
                    name is interpreted.

                    An empty ("" string) mailbox name argument is a special request to
                    return the hierarchy delimiter and the root name of the name given
                    in the reference.  The value returned as the root MAY be the empty
                    string if the reference is non-rooted or is an empty string.  In
                    all cases, a hierarchy delimiter (or NIL if there is no hierarchy)
                    is returned.  This permits a client to get the hierarchy delimiter
                    (or find out that the mailbox names are flat) even when no
                    mailboxes by that name currently exist.

                    The reference and mailbox name arguments are interpreted into a
                    canonical form that represents an unambiguous left-to-right
                    hierarchy.  The returned mailbox names will be in the interpreted
                    form.

                    Note: The interpretation of the reference argument is
                    implementation-defined.  It depends upon whether the
                    server implementation has a concept of the "current
                    working directory" and leading "break out characters",
                    which override the current working directory.

                    For example, on a server which exports a UNIX or NT
                    filesystem, the reference argument contains the current
                    working directory, and the mailbox name argument would
                    contain the name as interpreted in the current working
                    directory.

                    If a server implementation has no concept of break out
                    characters, the canonical form is normally the reference
                    name appended with the mailbox name.  Note that if the
                    server implements the namespace convention (section
                    5.1.2), "#" is a break out character and must be treated
                    as such.

                    If the reference argument is not a level of mailbox
                    hierarchy (that is, it is a \NoInferiors name), and/or
                    the reference argument does not end with the hierarchy
                    delimiter, it is implementation-dependent how this is
                    interpreted.  For example, a reference of "foo/bar" and
                    mailbox name of "rag/baz" could be interpreted as
                    "foo/bar/rag/baz", "foo/barrag/baz", or "foo/rag/baz".
                    A client SHOULD NOT use such a reference argument except
                    at the explicit request of the user.  A hierarchical
                    browser MUST NOT make any assumptions about server
                    interpretation of the reference unless the reference is
                    a level of mailbox hierarchy AND ends with the hierarchy
                    delimiter.

                    Any part of the reference argument that is included in the
                    interpreted form SHOULD prefix the interpreted form.  It SHOULD
                    also be in the same form as the reference name argument.  This
                    rule permits the client to determine if the returned mailbox name
                    is in the context of the reference argument, or if something about
                    the mailbox argument overrode the reference argument.  Without
                    this rule, the client would have to have knowledge of the server's
                    naming semantics including what characters are "breakouts" that
                    override a naming context.  

                        For example, here are some examples of how references
                        and mailbox names might be interpreted on a UNIX-based
                        server:

                            Reference     Mailbox Name  Interpretation
                            ------------  ------------  --------------
                            ~smith/Mail/  foo.*         ~smith/Mail/foo.*
                            archive/      %             archive/%
                            #news.        comp.mail.*   #news.comp.mail.*
                            ~smith/Mail/  /usr/doc/foo  /usr/doc/foo
                            archive/      ~fred/Mail/*  ~fred/Mail/*

                        The first three examples demonstrate interpretations in
                        the context of the reference argument.  Note that
                        "~smith/Mail" SHOULD NOT be transformed into something
                        like "/u2/users/smith/Mail", or it would be impossible
                        for the client to determine that the interpretation was
                        in the context of the reference.

                The character "*" is a wildcard, and matches zero or more
                characters at this position.  The character "%" is similar to "*",
                but it does not match a hierarchy delimiter.  If the "%" wildcard
                is the last character of a mailbox name argument, matching levels
                of hierarchy are also returned.  If these levels of hierarchy are
                not also selectable mailboxes, they are returned with the
                \Noselect mailbox name attribute (see the description of the LIST
                response for more details).

                The special name INBOX is included in the output from LIST, if
                INBOX is supported by this server for this user and if the
                uppercase string "INBOX" matches the interpreted reference and
                mailbox name arguments with wildcards as described above.  The
                criteria for omitting INBOX is whether SELECT INBOX will return
                failure; it is not relevant whether the user's real INBOX resides
                on this or some other server.

                Example:    C: A101 LIST "" ""
                            S: * LIST (\Noselect) "/" ""
                            S: A101 OK LIST Completed
                            C: A102 LIST #news.comp.mail.misc ""
                            S: * LIST (\Noselect) "." #news.
                            S: A102 OK LIST Completed
                            C: A103 LIST /usr/staff/jones ""
                            S: * LIST (\Noselect) "/" /
                            S: A103 OK LIST Completed
                            C: A202 LIST ~/Mail/ %
                            S: * LIST (\Noselect) "/" ~/Mail/foo
                            S: * LIST () "/" ~/Mail/meetings
                            S: A202 OK LIST completed
                */

                if(m_Filter != null){
                    byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " LIST \"\" " + IMAP_Utils.EncodeMailbox(m_Filter,imap.m_MailboxEncoding) + "\r\n");
                    this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
                }
                else{
                    byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " LIST \"\" \"*\"\r\n");
                    this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
                }
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes LIST command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool GetFoldersAsync(GetFoldersAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method CreateFolder

        /// <summary>
        /// Creates new folder.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void CreateFolder(string folder)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            using(CreateFolderAsyncOP op = new CreateFolderAsyncOP(folder,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<CreateFolderAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.CreateFolderAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method CreateFolderAsync

        #region class CreateFolderAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.CreateFolderAsync"/> asynchronous operation.
        /// </summary>
        public class CreateFolderAsyncOP : CmdAsyncOP<CreateFolderAsyncOP>
        {
            private string m_Folder = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="folder">Folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public CreateFolderAsyncOP(string folder,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }

                m_Folder = folder;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {        
                /* RFC 3501 6.3.3. CREATE Command.
                    Arguments:  mailbox name

                    Responses:  no specific responses for this command

                    Result:     OK - create completed
                                NO - create failure: can't create mailbox with that name
                                BAD - command unknown or arguments invalid

                    The CREATE command creates a mailbox with the given name.  An OK
                    response is returned only if a new mailbox with that name has been
                    created.  It is an error to attempt to create INBOX or a mailbox
                    with a name that refers to an extant mailbox.  Any error in
                    creation will return a tagged NO response.

                    If the mailbox name is suffixed with the server's hierarchy
                    separator character (as returned from the server by a LIST
                    command), this is a declaration that the client intends to create
                    mailbox names under this name in the hierarchy.  Server
                    implementations that do not require this declaration MUST ignore
                    the declaration.  In any case, the name created is without the
                    trailing hierarchy delimiter.

                    If the server's hierarchy separator character appears elsewhere in
                    the name, the server SHOULD create any superior hierarchical names
                    that are needed for the CREATE command to be successfully
                    completed.  In other words, an attempt to create "foo/bar/zap" on
                    a server in which "/" is the hierarchy separator character SHOULD
                    create foo/ and foo/bar/ if they do not already exist.

                    If a new mailbox is created with the same name as a mailbox which
                    was deleted, its unique identifiers MUST be greater than any
                    unique identifiers used in the previous incarnation of the mailbox
                    UNLESS the new incarnation has a different unique identifier
                    validity value.  See the description of the UID command for more
                    detail.

                    Example:    C: A003 CREATE owatagusiam/
                                S: A003 OK CREATE completed
                                C: A004 CREATE owatagusiam/blurdybloop
                                S: A004 OK CREATE completed

                        Note: The interpretation of this example depends on whether
                        "/" was returned as the hierarchy separator from LIST.  If
                        "/" is the hierarchy separator, a new level of hierarchy
                        named "owatagusiam" with a member called "blurdybloop" is
                        created.  Otherwise, two mailboxes at the same hierarchy
                        level are created.
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " CREATE " + IMAP_Utils.EncodeMailbox(m_Folder,imap.m_MailboxEncoding) + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes CREATE command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool CreateFolderAsync(CreateFolderAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method DeleteFolder

        /// <summary>
        /// Deletes specified folder.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void DeleteFolder(string folder)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            using(DeleteFolderAsyncOP op = new DeleteFolderAsyncOP(folder,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<DeleteFolderAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.DeleteFolderAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method DeleteFolderAsync

        #region class DeleteFolderAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.DeleteFolderAsync"/> asynchronous operation.
        /// </summary>
        public class DeleteFolderAsyncOP : CmdAsyncOP<DeleteFolderAsyncOP>
        {
            private string m_Folder = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="folder">Folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public DeleteFolderAsyncOP(string folder,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }

                m_Folder = folder;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.3.4. DELETE Command.
                    Arguments:  mailbox name

                    Responses:  no specific responses for this command

                    Result:     OK - delete completed
                                NO - delete failure: can't delete mailbox with that name
                                BAD - command unknown or arguments invalid

                    The DELETE command permanently removes the mailbox with the given
                    name.  A tagged OK response is returned only if the mailbox has
                    been deleted.  It is an error to attempt to delete INBOX or a
                    mailbox name that does not exist.

                    The DELETE command MUST NOT remove inferior hierarchical names.
                    For example, if a mailbox "foo" has an inferior "foo.bar"
                    (assuming "." is the hierarchy delimiter character), removing
                    "foo" MUST NOT remove "foo.bar".  It is an error to attempt to
                    delete a name that has inferior hierarchical names and also has
                    the \Noselect mailbox name attribute (see the description of the
                    LIST response for more details).

                    It is permitted to delete a name that has inferior hierarchical
                    names and does not have the \Noselect mailbox name attribute.  In
                    this case, all messages in that mailbox are removed, and the name
                    will acquire the \Noselect mailbox name attribute.

                    The value of the highest-used unique identifier of the deleted
                    mailbox MUST be preserved so that a new mailbox created with the
                    same name will not reuse the identifiers of the former
                    incarnation, UNLESS the new incarnation has a different unique
                    identifier validity value.  See the description of the UID command
                    for more detail.

                    Examples:   C: A682 LIST "" *
                                S: * LIST () "/" blurdybloop
                                S: * LIST (\Noselect) "/" foo
                                S: * LIST () "/" foo/bar
                                S: A682 OK LIST completed
                                C: A683 DELETE blurdybloop
                                S: A683 OK DELETE completed
                                C: A684 DELETE foo
                                S: A684 NO Name "foo" has inferior hierarchical names
                                C: A685 DELETE foo/bar
                                S: A685 OK DELETE Completed
                                C: A686 LIST "" *
                                S: * LIST (\Noselect) "/" foo
                                S: A686 OK LIST completed
                                C: A687 DELETE foo
                                S: A687 OK DELETE Completed
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " DELETE " + IMAP_Utils.EncodeMailbox(m_Folder,imap.m_MailboxEncoding) + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes DELETE command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool DeleteFolderAsync(DeleteFolderAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method RenameFolder

        /// <summary>
        /// Renames exisiting folder name.
        /// </summary>
        /// <param name="folder">Folder name with path to rename.</param>
        /// <param name="newFolder">New folder name with path.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> or <b>newFolder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void RenameFolder(string folder,string newFolder)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' name must be specified.","folder");
            }
            if(newFolder == null){
                throw new ArgumentNullException("newFolder");
            }
            if(newFolder == string.Empty){
                throw new ArgumentException("Argument 'newFolder' name must be specified.","newFolder");
            }

            using(RenameFolderAsyncOP op = new RenameFolderAsyncOP(folder,newFolder,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<RenameFolderAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.RenameFolderAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method RenameFolderAsync

        #region class RenameFolderAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.RenameFolderAsync"/> asynchronous operation.
        /// </summary>
        public class RenameFolderAsyncOP : CmdAsyncOP<RenameFolderAsyncOP>
        {
            private string m_Folder    = null;
            private string m_NewFolder = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="folder">Folder name with path.</param>
            /// <param name="newFolder">New folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> or <b>newFolder</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public RenameFolderAsyncOP(string folder,string newFolder,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }
                if(newFolder == null){
                    throw new ArgumentNullException("newFolder");
                }
                if(string.IsNullOrEmpty(newFolder)){
                    throw new ArgumentException("Argument 'newFolder' value must be specified.","newFolder");
                }                

                m_Folder    = folder;
                m_NewFolder = newFolder;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.3.5. RENAME Command.
                    Arguments:  existing mailbox name
                                new mailbox name

                    Responses:  no specific responses for this command

                    Result:     OK - rename completed
                                NO - rename failure: can't rename mailbox with that name,
                                     can't rename to mailbox with that name
                                BAD - command unknown or arguments invalid

                    The RENAME command changes the name of a mailbox.  A tagged OK
                    response is returned only if the mailbox has been renamed.  It is
                    an error to attempt to rename from a mailbox name that does not
                    exist or to a mailbox name that already exists.  Any error in
                    renaming will return a tagged NO response.

                    If the name has inferior hierarchical names, then the inferior
                    hierarchical names MUST also be renamed.  For example, a rename of
                    "foo" to "zap" will rename "foo/bar" (assuming "/" is the
                    hierarchy delimiter character) to "zap/bar".

                    If the server's hierarchy separator character appears in the name,
                    the server SHOULD create any superior hierarchical names that are
                    needed for the RENAME command to complete successfully.  In other
                    words, an attempt to rename "foo/bar/zap" to baz/rag/zowie on a
                    server in which "/" is the hierarchy separator character SHOULD
                    create baz/ and baz/rag/ if they do not already exist.

                    The value of the highest-used unique identifier of the old mailbox
                    name MUST be preserved so that a new mailbox created with the same
                    name will not reuse the identifiers of the former incarnation,
                    UNLESS the new incarnation has a different unique identifier
                    validity value.  See the description of the UID command for more
                    detail.

                    Renaming INBOX is permitted, and has special behavior.  It moves
                    all messages in INBOX to a new mailbox with the given name,
                    leaving INBOX empty.  If the server implementation supports
                    inferior hierarchical names of INBOX, these are unaffected by a
                    rename of INBOX.

                    Examples:   C: A682 LIST "" *
                                S: * LIST () "/" blurdybloop
                                S: * LIST (\Noselect) "/" foo
                                S: * LIST () "/" foo/bar
                                S: A682 OK LIST completed
                                C: A683 RENAME blurdybloop sarasoop
                                S: A683 OK RENAME completed
                                C: A684 RENAME foo zowie
                                S: A684 OK RENAME Completed
                                C: A685 LIST "" *
                                S: * LIST () "/" sarasoop
                                S: * LIST (\Noselect) "/" zowie
                                S: * LIST () "/" zowie/bar
                                S: A685 OK LIST completed
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " RENAME " + IMAP_Utils.EncodeMailbox(m_Folder,imap.m_MailboxEncoding) + " " + IMAP_Utils.EncodeMailbox(m_NewFolder,imap.m_MailboxEncoding) + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes RENAME command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool RenameFolderAsync(RenameFolderAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method GetSubscribedFolders

        /// <summary>
        /// Get user subscribed folders list.
        /// </summary>
        /// <param name="filter">Folders filter. If this value is null, all folders are returned.</param>
        /// <returns>Returns subscribed folders list.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        /// <remarks>
        /// The character "*" is a wildcard, and matches zero or more
        /// characters at this position.  The character "%" is similar to "*",
        /// but it does not match a hierarchy delimiter.  If the "%" wildcard
        /// is the last character of a mailbox name argument, matching levels
        /// of hierarchy are also returned.
        /// </remarks>
        public IMAP_r_u_LSub[] GetSubscribedFolders(string filter)
        {      
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            
            List<IMAP_r_u_LSub> retVal = new List<IMAP_r_u_LSub>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_LSub){
                    retVal.Add((IMAP_r_u_LSub)e.Value);
                }
            };

            using(GetSubscribedFoldersAsyncOP op = new GetSubscribedFoldersAsyncOP(filter,callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<GetSubscribedFoldersAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.GetSubscribedFoldersAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetSubscribedFoldersAsync

        #region class GetSubscribedFoldersAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.GetSubscribedFoldersAsync"/> asynchronous operation.
        /// </summary>
        public class GetSubscribedFoldersAsyncOP : CmdAsyncOP<GetSubscribedFoldersAsyncOP>
        {
            private string m_Filter = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="filter">Folders filter. If this value is null, all folders are returned.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            /// <remarks>
            /// The character "*" is a wildcard, and matches zero or more
            /// characters at this position.  The character "%" is similar to "*",
            /// but it does not match a hierarchy delimiter.  If the "%" wildcard
            /// is the last character of a mailbox name argument, matching levels
            /// of hierarchy are also returned.
            /// </remarks>
            public GetSubscribedFoldersAsyncOP(string filter,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                m_Filter = filter;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.3.9. LSUB Command.
                    Arguments:  reference name
                                mailbox name with possible wildcards

                    Responses:  untagged responses: LSUB

                    Result:     OK - lsub completed
                                NO - lsub failure: can't list that reference or name
                                BAD - command unknown or arguments invalid

                    The LSUB command returns a subset of names from the set of names
                    that the user has declared as being "active" or "subscribed".
                    Zero or more untagged LSUB replies are returned.  The arguments to
                    LSUB are in the same form as those for LIST.

                    The returned untagged LSUB response MAY contain different mailbox
                    flags from a LIST untagged response.  If this should happen, the
                    flags in the untagged LIST are considered more authoritative.

                    A special situation occurs when using LSUB with the % wildcard.
                    Consider what happens if "foo/bar" (with a hierarchy delimiter of
                    "/") is subscribed but "foo" is not.  A "%" wildcard to LSUB must
                    return foo, not foo/bar, in the LSUB response, and it MUST be
                    flagged with the \Noselect attribute.

                    The server MUST NOT unilaterally remove an existing mailbox name
                    from the subscription list even if a mailbox by that name no
                    longer exists.

                    Example:    C: A002 LSUB "#news." "comp.mail.*"
                                S: * LSUB () "." #news.comp.mail.mime
                                S: * LSUB () "." #news.comp.mail.misc
                                S: A002 OK LSUB completed
                                C: A003 LSUB "#news." "comp.%"
                                S: * LSUB (\NoSelect) "." #news.comp.mail
                                S: A003 OK LSUB completed
                */

                if(m_Filter != null){
                    byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " LSUB \"\" " + IMAP_Utils.EncodeMailbox(m_Filter,imap.m_MailboxEncoding) + "\r\n");
                    this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
                }
                else{
                    byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " LSUB \"\" \"*\"\r\n");
                    this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
                }
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes LSUB command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool GetSubscribedFoldersAsync(GetSubscribedFoldersAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method SubscribeFolder

        /// <summary>
        /// Subscribes specified folder.
        /// </summary>
        /// <param name="folder">Foler name with path.</param>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void SubscribeFolder(string folder)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }
                        
            using(SubscribeFolderAsyncOP op = new SubscribeFolderAsyncOP(folder,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<SubscribeFolderAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.SubscribeFolderAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method SubscribeFolderAsync

        #region class SubscribeFolderAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.SubscribeFolderAsync"/> asynchronous operation.
        /// </summary>
        public class SubscribeFolderAsyncOP : CmdAsyncOP<SubscribeFolderAsyncOP>
        {
            private string m_Folder = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="folder">Folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public SubscribeFolderAsyncOP(string folder,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }

                m_Folder = folder;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.3.6. SUBSCRIBE Command.
                    Arguments:  mailbox

                    Responses:  no specific responses for this command

                    Result:     OK - subscribe completed
                                NO - subscribe failure: can't subscribe to that name
                                BAD - command unknown or arguments invalid

                    The SUBSCRIBE command adds the specified mailbox name to the
                    server's set of "active" or "subscribed" mailboxes as returned by
                    the LSUB command.  This command returns a tagged OK response only
                    if the subscription is successful.

                    A server MAY validate the mailbox argument to SUBSCRIBE to verify
                    that it exists.  However, it MUST NOT unilaterally remove an
                    existing mailbox name from the subscription list even if a mailbox
                    by that name no longer exists.

                        Note: This requirement is because a server site can
                        choose to routinely remove a mailbox with a well-known
                        name (e.g., "system-alerts") after its contents expire,
                        with the intention of recreating it when new contents
                        are appropriate.


                    Example:    C: A002 SUBSCRIBE #news.comp.mail.mime
                                S: A002 OK SUBSCRIBE completed
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " SUBSCRIBE " + IMAP_Utils.EncodeMailbox(m_Folder,imap.m_MailboxEncoding) + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes SUBSCRIBE command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool SubscribeFolderAsync(SubscribeFolderAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method UnsubscribeFolder

        /// <summary>
        /// Unsubscribes specified folder.
        /// </summary>
        /// <param name="folder">Foler name with path.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void UnsubscribeFolder(string folder)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            using(UnsubscribeFolderAsyncOP op = new UnsubscribeFolderAsyncOP(folder,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<UnsubscribeFolderAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.UnsubscribeFolderAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method UnsubscribeFolderAsync

        #region class UnsubscribeFolderAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.UnsubscribeFolderAsync"/> asynchronous operation.
        /// </summary>
        public class UnsubscribeFolderAsyncOP : CmdAsyncOP<UnsubscribeFolderAsyncOP>
        {
            private string m_Folder = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="folder">Folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public UnsubscribeFolderAsyncOP(string folder,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }

                m_Folder = folder;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.3.7. UNSUBSCRIBE Command.
                    Arguments:  mailbox name

                    Responses:  no specific responses for this command

                    Result:     OK - unsubscribe completed
                                NO - unsubscribe failure: can't unsubscribe that name
                                BAD - command unknown or arguments invalid

                    The UNSUBSCRIBE command removes the specified mailbox name from
                    the server's set of "active" or "subscribed" mailboxes as returned
                    by the LSUB command.  This command returns a tagged OK response
                    only if the unsubscription is successful.

                    Example:    C: A002 UNSUBSCRIBE #news.comp.mail.mime
                                S: A002 OK UNSUBSCRIBE completed
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " UNSUBSCRIBE " + IMAP_Utils.EncodeMailbox(m_Folder,imap.m_MailboxEncoding) + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes UNSUBSCRIBE command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool UnsubscribeFolderAsync(UnsubscribeFolderAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method FolderStatus

        /// <summary>
        /// Gets the specified folder status.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <returns>Returns STATUS responses.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public IMAP_r_u_Status[] FolderStatus(string folder)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            List<IMAP_r_u_Status> retVal = new List<IMAP_r_u_Status>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_Status){
                    retVal.Add((IMAP_r_u_Status)e.Value);
                }
            };

            using(FolderStatusAsyncOP op = new FolderStatusAsyncOP(folder,callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<FolderStatusAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.FolderStatusAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }

        #endregion
        
        #region method FolderStatusAsync

        #region class FolderStatusAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.FolderStatusAsync"/> asynchronous operation.
        /// </summary>
        public class FolderStatusAsyncOP : CmdAsyncOP<FolderStatusAsyncOP>
        {
            private string m_Folder = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="folder">Folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public FolderStatusAsyncOP(string folder,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }

                m_Folder = folder;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.3.10. STATUS Command.
                    Arguments:  mailbox name
                                status data item names

                    Responses:  untagged responses: STATUS

                    Result:     OK - status completed
                                NO - status failure: no status for that name
                                BAD - command unknown or arguments invalid

                    The STATUS command requests the status of the indicated mailbox.
                    It does not change the currently selected mailbox, nor does it
                    affect the state of any messages in the queried mailbox (in
                    particular, STATUS MUST NOT cause messages to lose the \Recent
                    flag).

                    The STATUS command provides an alternative to opening a second
                    IMAP4rev1 connection and doing an EXAMINE command on a mailbox to
                    query that mailbox's status without deselecting the current
                    mailbox in the first IMAP4rev1 connection.

                    Unlike the LIST command, the STATUS command is not guaranteed to
                    be fast in its response.  Under certain circumstances, it can be
                    quite slow.  In some implementations, the server is obliged to
                    open the mailbox read-only internally to obtain certain status
                    information.  Also unlike the LIST command, the STATUS command
                    does not accept wildcards.

                    Note: The STATUS command is intended to access the
                    status of mailboxes other than the currently selected
                    mailbox.  Because the STATUS command can cause the
                    mailbox to be opened internally, and because this
                    information is available by other means on the selected
                    mailbox, the STATUS command SHOULD NOT be used on the
                    currently selected mailbox.

                    The STATUS command MUST NOT be used as a "check for new
                    messages in the selected mailbox" operation (refer to
                    sections 7, 7.3.1, and 7.3.2 for more information about
                    the proper method for new message checking).

                    Because the STATUS command is not guaranteed to be fast
                    in its results, clients SHOULD NOT expect to be able to
                    issue many consecutive STATUS commands and obtain
                    reasonable performance.

                    The currently defined status data items that can be requested are:

                    MESSAGES
                        The number of messages in the mailbox.

                    RECENT
                        The number of messages with the \Recent flag set.

                    UIDNEXT
                        The next unique identifier value of the mailbox.  Refer to
                        section 2.3.1.1 for more information.

                    UIDVALIDITY
                        The unique identifier validity value of the mailbox.  Refer to
                        section 2.3.1.1 for more information.

                    UNSEEN
                        The number of messages which do not have the \Seen flag set.


                    Example:    C: A042 STATUS blurdybloop (UIDNEXT MESSAGES)
                                S: * STATUS blurdybloop (MESSAGES 231 UIDNEXT 44292)
                                S: A042 OK STATUS completed
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " STATUS " + IMAP_Utils.EncodeMailbox(m_Folder,imap.m_MailboxEncoding) + " (MESSAGES RECENT UIDNEXT UIDVALIDITY UNSEEN)\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes STATUS command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool FolderStatusAsync(FolderStatusAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method SelectFolder

        /// <summary>
        /// Selects specified folder.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void SelectFolder(string folder)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            using(SelectFolderAsyncOP op = new SelectFolderAsyncOP(folder,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<SelectFolderAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.SelectFolderAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method SelectFolderAsync

        #region class SelectFolderAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.SelectFolderAsync"/> asynchronous operation.
        /// </summary>
        public class SelectFolderAsyncOP : IDisposable,IAsyncOP
        {
            private object                            m_pLock          = new object();
            private AsyncOP_State                     m_State          = AsyncOP_State.WaitingForStart;
            private Exception                         m_pException     = null;
            private IMAP_r_ServerStatus               m_pFinalResponse = null;
            private IMAP_Client                       m_pImapClient    = null;
            private bool                              m_RiseCompleted  = false;
            private string                            m_Folder         = null;
            private EventHandler<EventArgs<IMAP_r_u>> m_pCallback      = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="folder">Folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public SelectFolderAsyncOP(string folder,EventHandler<EventArgs<IMAP_r_u>> callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }

                m_Folder    = folder;
                m_pCallback = callback;
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
                m_pImapClient    = null;
                m_pFinalResponse = null;
                m_pCallback      = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                                
                m_pImapClient = owner;
                        
                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 3501 6.3.1.  SELECT Command.
                        Arguments:  mailbox name

                        Responses:  REQUIRED untagged responses: FLAGS, EXISTS, RECENT
                                    REQUIRED OK untagged responses:  UNSEEN,  PERMANENTFLAGS,
                                    UIDNEXT, UIDVALIDITY

                        Result:     OK - select completed, now in selected state
                                    NO - select failure, now in authenticated state: no
                                         such mailbox, can't access mailbox
                                    BAD - command unknown or arguments invalid

                        The SELECT command selects a mailbox so that messages in the
                        mailbox can be accessed.  Before returning an OK to the client,
                        the server MUST send the following untagged data to the client.
                        Note that earlier versions of this protocol only required the
                        FLAGS, EXISTS, and RECENT untagged data; consequently, client
                        implementations SHOULD implement default behavior for missing data
                        as discussed with the individual item.

                            FLAGS       Defined flags in the mailbox.  See the description
                                        of the FLAGS response for more detail.

                            <n> EXISTS  The number of messages in the mailbox.  See the
                                        description of the EXISTS response for more detail.

                            <n> RECENT  The number of messages with the \Recent flag set.
                                        See the description of the RECENT response for more
                                        detail.

                            OK [UNSEEN <n>]
                                        The message sequence number of the first unseen
                                        message in the mailbox.  If this is missing, the
                                        client can not make any assumptions about the first
                                        unseen message in the mailbox, and needs to issue a
                                        SEARCH command if it wants to find it.

                            OK [PERMANENTFLAGS (<list of flags>)]
                                        A list of message flags that the client can change
                                        permanently.  If this is missing, the client should
                                        assume that all flags can be changed permanently.

                            OK [UIDNEXT <n>]
                                        The next unique identifier value.  Refer to section
                                        2.3.1.1 for more information.  If this is missing,
                                        the client can not make any assumptions about the
                                        next unique identifier value.

                            OK [UIDVALIDITY <n>]
                                    The unique identifier validity value.  Refer to
                                    section 2.3.1.1 for more information.  If this is
                                    missing, the server does not support unique
                                    identifiers.

                        Only one mailbox can be selected at a time in a connection;
                        simultaneous access to multiple mailboxes requires multiple
                        connections.  The SELECT command automatically deselects any
                        currently selected mailbox before attempting the new selection.
                        Consequently, if a mailbox is selected and a SELECT command that
                        fails is attempted, no mailbox is selected.
                     
                        If the client is permitted to modify the mailbox, the server
                        SHOULD prefix the text of the tagged OK response with the
                        "[READ-WRITE]" response code.
                    */

                    // Set new folder as selected folder.
                    m_pImapClient.m_pSelectedFolder = new IMAP_Client_SelectedFolder(m_Folder);
                    
                    byte[] cmdLine    = Encoding.UTF8.GetBytes((m_pImapClient.m_CommandIndex++).ToString("d5") + " SELECT " + IMAP_Utils.EncodeMailbox(m_Folder,m_pImapClient.m_MailboxEncoding) + "\r\n");
                    string cmdLineLog = Encoding.UTF8.GetString(cmdLine).TrimEnd();

                    SendCmdAndReadRespAsyncOP args = new SendCmdAndReadRespAsyncOP(cmdLine,cmdLineLog,m_pCallback);
                    args.CompletedAsync += delegate(object sender,EventArgs<SendCmdAndReadRespAsyncOP> e){
                        ProecessCmdResult(e.Value);
                    };
                    // Operation completed synchronously.
                    if(!m_pImapClient.SendCmdAndReadRespAsync(args)){
                        ProecessCmdResult(args);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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

            #region method ProecessCmdResult

            /// <summary>
            /// Processes command result.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ProecessCmdResult(SendCmdAndReadRespAsyncOP op)
            {
                try{
                    // Command send/receive failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    }
                    // Command send/receive succeeded.
                    else{
                        m_pFinalResponse = op.FinalResponse;

                        // IMAP server returned error response.
                        if(op.FinalResponse.IsError){
                            m_pException = new IMAP_ClientException(op.FinalResponse);
                            
                            // If a mailbox is selected and a SELECT command that fails is attempted, no mailbox is selected.
                            m_pImapClient.m_pSelectedFolder = null;
                        }
                        // IMAP server returned success response.
                        else{
                            // Mark folder as read-only if optional response code "READ-ONLY" specified.
                            if(m_pFinalResponse.OptionalResponse != null && m_pFinalResponse.OptionalResponse is IMAP_t_orc_ReadOnly){
                               m_pImapClient.m_pSelectedFolder.SetReadOnly(true);
                            }
                        }
                    }

                    SetState(AsyncOP_State.Completed);
                }
                finally{
                    op.Dispose();
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
            /// Returns IMAP server final response.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_r_ServerStatus FinalResponse
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pFinalResponse; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<SelectFolderAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<SelectFolderAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes SELECT command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="SelectFolderAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool SelectFolderAsync(SelectFolderAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method ExamineFolder

        /// <summary>
        /// Selects folder as read-only, no changes to messages or flags not possible.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void ExamineFolder(string folder)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            using(ExamineFolderAsyncOP op = new ExamineFolderAsyncOP(folder,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<ExamineFolderAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.ExamineFolderAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method ExamineFolderAsync

        #region class ExamineFolderAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.ExamineFolderAsync"/> asynchronous operation.
        /// </summary>
        public class ExamineFolderAsyncOP : IDisposable,IAsyncOP
        {
            private object                            m_pLock          = new object();
            private AsyncOP_State                     m_State          = AsyncOP_State.WaitingForStart;
            private Exception                         m_pException     = null;
            private IMAP_r_ServerStatus               m_pFinalResponse = null;
            private IMAP_Client                       m_pImapClient    = null;
            private bool                              m_RiseCompleted  = false;
            private string                            m_Folder         = null;
            private EventHandler<EventArgs<IMAP_r_u>> m_pCallback      = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="folder">Folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public ExamineFolderAsyncOP(string folder,EventHandler<EventArgs<IMAP_r_u>> callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }

                m_Folder    = folder;
                m_pCallback = callback;
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
                m_pImapClient    = null;
                m_pFinalResponse = null;
                m_pCallback      = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                                
                m_pImapClient = owner;
                        
                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 3501 6.3.2.  EXAMINE Command.
                        Arguments:  mailbox name

                        Responses:  REQUIRED untagged responses: FLAGS, EXISTS, RECENT
                                    REQUIRED OK untagged responses:  UNSEEN,  PERMANENTFLAGS,
                                    UIDNEXT, UIDVALIDITY

                        Result:     OK - examine completed, now in selected state
                                    NO - examine failure, now in authenticated state: no
                                         such mailbox, can't access mailbox
                                    BAD - command unknown or arguments invalid

                        The EXAMINE command is identical to SELECT and returns the same
                        output; however, the selected mailbox is identified as read-only.
                        No changes to the permanent state of the mailbox, including
                        per-user state, are permitted; in particular, EXAMINE MUST NOT
                        cause messages to lose the \Recent flag.

                        The text of the tagged OK response to the EXAMINE command MUST
                        begin with the "[READ-ONLY]" response code.
                    */

                    // Set new folder as selected folder.
                    m_pImapClient.m_pSelectedFolder = new IMAP_Client_SelectedFolder(m_Folder);
                    
                    byte[] cmdLine    = Encoding.UTF8.GetBytes((m_pImapClient.m_CommandIndex++).ToString("d5") + " EXAMINE " + IMAP_Utils.EncodeMailbox(m_Folder,m_pImapClient.m_MailboxEncoding) + "\r\n");
                    string cmdLineLog = Encoding.UTF8.GetString(cmdLine).TrimEnd();

                    SendCmdAndReadRespAsyncOP args = new SendCmdAndReadRespAsyncOP(cmdLine,cmdLineLog,m_pCallback);
                    args.CompletedAsync += delegate(object sender,EventArgs<SendCmdAndReadRespAsyncOP> e){
                        ProecessCmdResult(e.Value);
                    };
                    // Operation completed synchronously.
                    if(!m_pImapClient.SendCmdAndReadRespAsync(args)){
                        ProecessCmdResult(args);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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

            #region method ProecessCmdResult

            /// <summary>
            /// Processes command result.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ProecessCmdResult(SendCmdAndReadRespAsyncOP op)
            {
                try{
                    // Command send/receive failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    }
                    // Command send/receive succeeded.
                    else{
                        m_pFinalResponse = op.FinalResponse;

                        // IMAP server returned error response.
                        if(op.FinalResponse.IsError){
                            m_pException = new IMAP_ClientException(op.FinalResponse);
                            
                            // If a mailbox is selected and a SELECT command that fails is attempted, no mailbox is selected.
                            m_pImapClient.m_pSelectedFolder = null;
                        }
                        // IMAP server returned success response.
                        else{
                            // Mark folder as read-only if optional response code "READ-ONLY" specified.
                            if(m_pFinalResponse.OptionalResponse != null && m_pFinalResponse.OptionalResponse is IMAP_t_orc_ReadOnly){
                               m_pImapClient.m_pSelectedFolder.SetReadOnly(true);
                            }
                        }
                    }

                    SetState(AsyncOP_State.Completed);
                }
                finally{
                    op.Dispose();
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
            /// Returns IMAP server final response.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_r_ServerStatus FinalResponse
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pFinalResponse; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<ExamineFolderAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<ExamineFolderAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes EXAMINE command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="ExamineFolderAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool ExamineFolderAsync(ExamineFolderAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method GetFolderQuotaRoots

        /// <summary>
        /// Gets specified folder quota roots and their quota resource usage.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <returns>Returns quota-roots and their resource limit entries.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public IMAP_r[] GetFolderQuotaRoots(string folder)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            List<IMAP_r> retVal = new List<IMAP_r>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_Quota){
                    retVal.Add((IMAP_r_u_Quota)e.Value);
                }
                else if(e.Value is IMAP_r_u_QuotaRoot){
                    retVal.Add((IMAP_r_u_QuotaRoot)e.Value);
                }
            };

            using(GetFolderQuotaRootsAsyncOP op = new GetFolderQuotaRootsAsyncOP(folder,callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<GetFolderQuotaRootsAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.GetFolderQuotaRootsAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetFolderQuotaRootsAsync

        #region class GetFolderQuotaRootsAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.GetFolderQuotaRootsAsync"/> asynchronous operation.
        /// </summary>
        public class GetFolderQuotaRootsAsyncOP : CmdAsyncOP<GetFolderQuotaRootsAsyncOP>
        {
            private string m_Folder = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="folder">Folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public GetFolderQuotaRootsAsyncOP(string folder,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }

                m_Folder = folder;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 2087 4.3. GETQUOTAROOT Command.
                    Arguments:  mailbox name

                    Data:       untagged responses: QUOTAROOT, QUOTA

                    Result:     OK - getquota completed
                                NO - getquota error: no such mailbox, permission denied
                                BAD - command unknown or arguments invalid

                    The GETQUOTAROOT command takes the name of a mailbox and returns the
                    list of quota roots for the mailbox in an untagged QUOTAROOT
                    response.  For each listed quota root, it also returns the quota
                    root's resource usage and limits in an untagged QUOTA response.

                    Example:    C: A003 GETQUOTAROOT INBOX
                                S: * QUOTAROOT INBOX ""
                                S: * QUOTA "" (STORAGE 10 512)
                                S: A003 OK Getquota completed
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " GETQUOTAROOT " + IMAP_Utils.EncodeMailbox(m_Folder,imap.m_MailboxEncoding) + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes STATUS command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool GetFolderQuotaRootsAsync(GetFolderQuotaRootsAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method GetQuota

        /// <summary>
        /// Gets the specified folder quota-root resource limit entries.
        /// </summary>
        /// <param name="quotaRootName">Quota root name.</param>
        /// <returns>Returns quota-root resource limit entries.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>quotaRootName</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public IMAP_r_u_Quota[] GetQuota(string quotaRootName)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(quotaRootName == null){
                throw new ArgumentNullException("quotaRootName");
            }            

            List<IMAP_r_u_Quota> retVal = new List<IMAP_r_u_Quota>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_Quota){
                    retVal.Add((IMAP_r_u_Quota)e.Value);
                }
            };

            using(GetQuotaAsyncOP op = new GetQuotaAsyncOP(quotaRootName,callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<GetQuotaAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.GetQuotaAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetQuotaAsync

        #region class GetQuotaAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.GetQuotaAsync"/> asynchronous operation.
        /// </summary>
        public class GetQuotaAsyncOP : CmdAsyncOP<GetQuotaAsyncOP>
        {
            private string m_QuotaRootName = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="quotaRootName">Quota root name.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is riased when <b>quotaRootName</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public GetQuotaAsyncOP(string quotaRootName,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(quotaRootName == null){
                    throw new ArgumentNullException("quotaRootName");
                }

                m_QuotaRootName = quotaRootName;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 2087 4.2. GETQUOTA Command.
                    Arguments:  quota root

                    Data:       untagged responses: QUOTA
        
                    Result:     OK - getquota completed
                                NO - getquota  error:  no  such  quota  root,  permission denied
                                BAD - command unknown or arguments invalid
                    
                    The GETQUOTA command takes the name of a quota root and returns the
                    quota root's resource usage and limits in an untagged QUOTA response.

                    Example:    C: A003 GETQUOTA ""
                                S: * QUOTA "" (STORAGE 10 512)
                                S: A003 OK Getquota completed
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " GETQUOTA " + IMAP_Utils.EncodeMailbox(m_QuotaRootName,imap.m_MailboxEncoding) + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes GETQUOTA command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool GetQuotaAsync(GetQuotaAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method SetQuota

        private void SetQuota()
        {
            /* RFC 2087 4.1. SETQUOTA Command.
                Arguments:  quota root
                            list of resource limits

                Data:       untagged responses: QUOTA

                Result:     OK - setquota completed
                            NO - setquota error: can't set that data
                            BAD - command unknown or arguments invalid

                The SETQUOTA command takes the name of a mailbox quota root and a
                list of resource limits. The resource limits for the named quota root
                are changed to be the specified limits.  Any previous resource limits
                for the named quota root are discarded.

                If the named quota root did not previously exist, an implementation
                may optionally create it and change the quota roots for any number of
                existing mailboxes in an implementation-defined manner.

                Example:    C: A001 SETQUOTA "" (STORAGE 512)
                            S: * QUOTA "" (STORAGE 10 512)
                            S: A001 OK Setquota completed
            */
        }

        #endregion

        #region method GetFolderAcl

        /// <summary>
        /// Gets the specified folder ACL entries.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <returns>Returns folder ACL entries.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public IMAP_r_u_Acl[] GetFolderAcl(string folder)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            List<IMAP_r_u_Acl> retVal = new List<IMAP_r_u_Acl>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_Acl){
                    retVal.Add((IMAP_r_u_Acl)e.Value);
                }
            };

            using(GetFolderAclAsyncOP op = new GetFolderAclAsyncOP(folder,callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<GetFolderAclAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.GetFolderAclAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetFolderAclAsync

        #region class GetFolderAclAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.GetFolderAclAsync"/> asynchronous operation.
        /// </summary>
        public class GetFolderAclAsyncOP : CmdAsyncOP<GetFolderAclAsyncOP>
        {
            private string m_Folder = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="folder">Folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is riased when <b>folder</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public GetFolderAclAsyncOP(string folder,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }

                m_Folder = folder;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 4314 3.3. GETACL Command.
                    Arguments:  mailbox name

                    Data:       untagged responses: ACL

                    Result:     OK - getacl completed
                                NO - getacl failure: can't get acl
                                BAD - arguments invalid

                    The GETACL command returns the access control list for mailbox in an
                    untagged ACL response.

                    Some implementations MAY permit multiple forms of an identifier to
                    reference the same IMAP account.  Usually, such implementations will
                    have a canonical form that is stored internally.  An ACL response
                    caused by a GETACL command MAY include a canonicalized form of the
                    identifier that might be different from the one used in the
                    corresponding SETACL command.

                    Example:    C: A002 GETACL INBOX
                                S: * ACL INBOX Fred rwipsldexta
                                S: A002 OK Getacl complete                
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " GETACL " + IMAP_Utils.EncodeMailbox(m_Folder,imap.m_MailboxEncoding) + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes GETACL command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool GetFolderAclAsync(GetFolderAclAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method SetFolderAcl

        /// <summary>
        /// Sets the specified folder ACL.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <param name="user">User name.</param>
        /// <param name="setType">Specifies how flags are set.</param>
        /// <param name="permissions">ACL permissions.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> or <b>user</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void SetFolderAcl(string folder,string user,IMAP_Flags_SetType setType,IMAP_ACL_Flags permissions)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }
            if(user == null){
                throw new ArgumentNullException("user");
            }
            if(user == string.Empty){
                throw new ArgumentException("Argument 'user' value must be specified.","user");
            }

            using(SetFolderAclAsyncOP op = new SetFolderAclAsyncOP(folder,user,setType,permissions,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<SetFolderAclAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.SetFolderAclAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method SetFolderAclAsync

        #region class SetFolderAclAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.SetFolderAclAsync"/> asynchronous operation.
        /// </summary>
        public class SetFolderAclAsyncOP : CmdAsyncOP<SetFolderAclAsyncOP>
        {
            private string             m_Folder       = null;
            private string             m_Identifier   = null;
            private IMAP_Flags_SetType m_FlagsSetType = IMAP_Flags_SetType.Replace;
            private IMAP_ACL_Flags     m_Permissions  = IMAP_ACL_Flags.None;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="folder">Folder name with path.</param>
            /// <param name="identifier">ACL entry identifier. Normally this is user or group name.</param>
            /// <param name="setType">Specifies how flags are set.</param>
            /// <param name="permissions">ACL permissions.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is riased when <b>folder</b> or <b>identifier</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public SetFolderAclAsyncOP(string folder,string identifier,IMAP_Flags_SetType setType,IMAP_ACL_Flags permissions,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }
                if(identifier == null){
                    throw new ArgumentNullException("identifier");
                }
                if(string.IsNullOrEmpty(identifier)){
                    throw new ArgumentException("Argument 'identifier' value must be specified.","identifier");
                }

                m_Folder       = folder;
                m_Identifier   = identifier;
                m_FlagsSetType = setType;
                m_Permissions  = permissions;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 4314 3.1. SETACL Command.
                    Arguments:  mailbox name
                                identifier
                                access right modification

                    Data:       no specific data for this command

                    Result:     OK - setacl completed
                                NO - setacl failure: can't set acl
                                BAD - arguments invalid

                    The SETACL command changes the access control list on the specified
                    mailbox so that the specified identifier is granted permissions as
                    specified in the third argument.

                    The third argument is a string containing an optional plus ("+") or
                    minus ("-") prefix, followed by zero or more rights characters.  If
                    the string starts with a plus, the following rights are added to any
                    existing rights for the identifier.  If the string starts with a
                    minus, the following rights are removed from any existing rights for
                    the identifier.  If the string does not start with a plus or minus,
                    the rights replace any existing rights for the identifier.

                    Note that an unrecognized right MUST cause the command to return the
                    BAD response.  In particular, the server MUST NOT silently ignore
                    unrecognized rights.

                    Example:    C: A035 SETACL INBOX/Drafts John lrQswicda
                                S: A035 BAD Uppercase rights are not allowed
                
                                C: A036 SETACL INBOX/Drafts John lrqswicda
                                S: A036 BAD The q right is not supported
                */

                StringBuilder command = new StringBuilder();
                command.Append((imap.m_CommandIndex++).ToString("d5"));            
                command.Append(" SETACL");
                command.Append(" " + IMAP_Utils.EncodeMailbox(m_Folder,imap.m_MailboxEncoding));
                command.Append(" " + TextUtils.QuoteString(m_Identifier));
                if(m_FlagsSetType == IMAP_Flags_SetType.Add){
                    command.Append(" +" + IMAP_Utils.ACL_to_String(m_Permissions));
                }
                else if(m_FlagsSetType == IMAP_Flags_SetType.Remove){
                    command.Append(" -" + IMAP_Utils.ACL_to_String(m_Permissions));
                }
                else if(m_FlagsSetType == IMAP_Flags_SetType.Replace){
                    command.Append(" " + IMAP_Utils.ACL_to_String(m_Permissions));
                }
                else{
                    throw new NotSupportedException("Not supported argument 'setType' value '" + m_FlagsSetType.ToString() + "'.");
                }

                byte[] cmdLine = Encoding.UTF8.GetBytes(command.ToString());
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes SETACL command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool SetFolderAclAsync(SetFolderAclAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method DeleteFolderAcl

        /// <summary>
        /// Deletes the specified folder user ACL entry.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <param name="user">User name.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> or <b>user</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void DeleteFolderAcl(string folder,string user)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }
            if(user == null){
                throw new ArgumentNullException("user");
            }
            if(user == string.Empty){
                throw new ArgumentException("Argument 'user' value must be specified.","user");
            }

            using(DeleteFolderAclAsyncOP op = new DeleteFolderAclAsyncOP(folder,user,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<DeleteFolderAclAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.DeleteFolderAclAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method DeleteFolderAclAsync

        #region class DeleteFolderAclAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.DeleteFolderAclAsync"/> asynchronous operation.
        /// </summary>
        public class DeleteFolderAclAsyncOP : CmdAsyncOP<DeleteFolderAclAsyncOP>
        {
            private string m_Folder     = null;
            private string m_Identifier = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="folder">Folder name with path.</param>
            /// <param name="identifier">ACL entry identifier. Normally this is user or group name.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is riased when <b>folder</b> or <b>identifier</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public DeleteFolderAclAsyncOP(string folder,string identifier,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }
                if(identifier == null){
                    throw new ArgumentNullException("identifier");
                }

                m_Folder     = folder;
                m_Identifier = identifier;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 4314 3.2. DELETEACL Command.
                    Arguments:  mailbox name
                                identifier

                    Data:       no specific data for this command

                    Result:     OK - deleteacl completed
                                NO - deleteacl failure: can't delete acl
                                BAD - arguments invalid

                    The DELETEACL command removes any <identifier,rights> pair for the
                    specified identifier from the access control list for the specified
                    mailbox.

                    Example:    C: B001 getacl INBOX
                                S: * ACL INBOX Fred rwipslxetad -Fred wetd $team w
                                S: B001 OK Getacl complete
                                C: B002 DeleteAcl INBOX Fred
                                S: B002 OK Deleteacl complete
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " DELETEACL " + IMAP_Utils.EncodeMailbox(m_Folder,imap.m_MailboxEncoding) + " " + TextUtils.QuoteString(m_Identifier) + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes DELETEACL command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool DeleteFolderAclAsync(DeleteFolderAclAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method GetFolderRights

        /// <summary>
        /// Gets rights which can be set for the specified identifier.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <param name="identifier">ACL entry identifier. Normally this is user or group name.</param>
        /// <returns>Returns LISTRIGHTS responses.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when<b>folder</b> or <b>identifier</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public IMAP_r_u_ListRights[] GetFolderRights(string folder,string identifier)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }
            if(identifier == null){
                throw new ArgumentNullException("identifier");
            }
            if(identifier == string.Empty){
                throw new ArgumentException("Argument 'identifier' value must be specified.","identifier");
            }

            List<IMAP_r_u_ListRights> retVal = new List<IMAP_r_u_ListRights>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_ListRights){
                    retVal.Add((IMAP_r_u_ListRights)e.Value);
                }
            };

            using(GetFolderRightsAsyncOP op = new GetFolderRightsAsyncOP(folder,identifier,callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<GetFolderRightsAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.GetFolderRightsAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetFolderRightsAsync

        #region class GetFolderRightsAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.GetFolderRightsAsync"/> asynchronous operation.
        /// </summary>
        public class GetFolderRightsAsyncOP : CmdAsyncOP<GetFolderRightsAsyncOP>
        {
            private string m_Folder     = null;
            private string m_Identifier = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="folder">Folder name with path.</param>
            /// <param name="identifier">ACL entry identifier. Normally this is user or group name.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is riased when <b>folder</b> or <b>identifier</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public GetFolderRightsAsyncOP(string folder,string identifier,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }
                if(identifier == null){
                    throw new ArgumentNullException("identifier");
                }

                m_Folder     = folder;
                m_Identifier = identifier;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 4314 3.4. LISTRIGHTS Command.
                    Arguments:  mailbox name
                                identifier

                    Data:       untagged responses: LISTRIGHTS

                    Result:     OK - listrights completed
                                NO - listrights failure: can't get rights list
                                BAD - arguments invalid

                    The LISTRIGHTS command takes a mailbox name and an identifier and
                    returns information about what rights can be granted to the
                    identifier in the ACL for the mailbox.

                    Some implementations MAY permit multiple forms of an identifier to
                    reference the same IMAP account.  Usually, such implementations will
                    have a canonical form that is stored internally.  A LISTRIGHTS
                    response caused by a LISTRIGHTS command MUST always return the same
                    form of an identifier as specified by the client.  This is to allow
                    the client to correlate the response with the command.

                    Example:    C: a001 LISTRIGHTS ~/Mail/saved smith
                                S: * LISTRIGHTS ~/Mail/saved smith la r swicdkxte
                                S: a001 OK Listrights completed

                    Example:    C: a005 listrights archive/imap anyone
                                S: * LISTRIGHTS archive.imap anyone ""
                                   l r s w i p k x t e c d a 0 1 2 3 4 5 6 7 8 9
                                S: a005 Listrights successful
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " LISTRIGHTS " + IMAP_Utils.EncodeMailbox(m_Folder,imap.m_MailboxEncoding) + " " + TextUtils.QuoteString(m_Identifier) + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes LISTRIGHTS command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool GetFolderRightsAsync(GetFolderRightsAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method GetFolderMyRights

        /// <summary>
        /// Gets myrights to the specified folder.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <returns>Returns MYRIGHTS responses.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public IMAP_r_u_MyRights[] GetFolderMyRights(string folder)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            List<IMAP_r_u_MyRights> retVal = new List<IMAP_r_u_MyRights>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_MyRights){
                    retVal.Add((IMAP_r_u_MyRights)e.Value);
                }
            };

            using(GetFolderMyRightsAsyncOP op = new GetFolderMyRightsAsyncOP(folder,callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<GetFolderMyRightsAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.GetFolderMyRightsAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method GetFolderMyRightsAsync

        #region class GetFolderMyRightsAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.GetFolderMyRightsAsyncOP"/> asynchronous operation.
        /// </summary>
        public class GetFolderMyRightsAsyncOP : CmdAsyncOP<GetFolderMyRightsAsyncOP>
        {
            private string m_Folder = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="folder">Folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is riased when <b>folder</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public GetFolderMyRightsAsyncOP(string folder,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }

                m_Folder = folder;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 4314 3.5. MYRIGHTS Command.
                    Arguments:  mailbox name

                    Data:       untagged responses: MYRIGHTS

                    Result:     OK - myrights completed
                                NO - myrights failure: can't get rights
                                BAD - arguments invalid

                    The MYRIGHTS command returns the set of rights that the user has to
                    mailbox in an untagged MYRIGHTS reply.

                    Example:    C: A003 MYRIGHTS INBOX
                                S: * MYRIGHTS INBOX rwiptsldaex
                                S: A003 OK Myrights complete
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " MYRIGHTS " + IMAP_Utils.EncodeMailbox(m_Folder,imap.m_MailboxEncoding) + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes MYRIGHTS command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool GetFolderMyRightsAsync(GetFolderMyRightsAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method StoreMessage
                
        /// <summary>
        /// Stores specified message to the specified folder.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <param name="flags">Message flags. Value null means no flags. For example: new string[]{"\Seen","\Answered"}.</param>
        /// <param name="internalDate">Message internal data. DateTime.MinValue means server will allocate it.</param>
        /// <param name="message">Message stream.</param>
        /// <param name="count">Number of bytes send from <b>message</b> stream.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> or <b>stream</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception> 
        public void StoreMessage(string folder,string[] flags,DateTime internalDate,Stream message,int count)
        {
            StoreMessage(folder,flags != null ? new IMAP_t_MsgFlags(flags) : new IMAP_t_MsgFlags(new string[0]),internalDate,message,count);
        }

        /// <summary>
        /// Stores specified message to the specified folder.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <param name="flags">Message flags.</param>
        /// <param name="internalDate">Message internal data. DateTime.MinValue means server will allocate it.</param>
        /// <param name="message">Message stream.</param>
        /// <param name="count">Number of bytes send from <b>message</b> stream.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b>,<b>flags</b> or <b>stream</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void StoreMessage(string folder,IMAP_t_MsgFlags flags,DateTime internalDate,Stream message,int count)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(folder == null){
                throw new ArgumentNullException("folder");
            }
            if(folder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }
            if(flags == null){
                throw new ArgumentNullException("flags");
            }
            if(message == null){
                throw new ArgumentNullException("message");
            }
            if(count < 1){
                throw new ArgumentException("Argument 'count' value must be >= 1.","count");
            }

            using(StoreMessageAsyncOP op = new StoreMessageAsyncOP(folder,flags,internalDate,message,count,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<StoreMessageAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.StoreMessageAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        /// <summary>
        /// Stores specified message to the specified folder.
        /// </summary>
        /// <param name="op">Store message operation.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public void StoreMessage(StoreMessageAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(op == null){
                throw new ArgumentNullException("op");
            }

            using(ManualResetEvent wait = new ManualResetEvent(false)){
                op.CompletedAsync += delegate(object s1,EventArgs<StoreMessageAsyncOP> e1){
                    wait.Set();
                };
                if(!this.StoreMessageAsync(op)){
                    wait.Set();
                }
                wait.WaitOne();

                if(op.Error != null){
                    throw op.Error;
                }
            }
        }

        #endregion

        #region method StoreMessageAsync

        #region class StoreMessageAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.StoreMessageAsync"/> asynchronous operation.
        /// </summary>
        public class StoreMessageAsyncOP : IDisposable,IAsyncOP
        {
            private object                            m_pLock          = new object();
            private AsyncOP_State                     m_State          = AsyncOP_State.WaitingForStart;
            private Exception                         m_pException     = null;
            private IMAP_r_ServerStatus               m_pFinalResponse = null;
            private IMAP_Client                       m_pImapClient    = null;
            private bool                              m_RiseCompleted  = false;
            private string                            m_Folder         = null;
            private IMAP_t_MsgFlags                   m_pFlags         = null;
            private DateTime                          m_InternalDate;
            private Stream                            m_pStream        = null;
            private long                              m_Count          = 0;
            private EventHandler<EventArgs<IMAP_r_u>> m_pCallback      = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="folder">Folder name with path.</param>
            /// <param name="flags">Message flags. Value null means no flags.</param>
            /// <param name="internalDate">Message internal data. DateTime.MinValue means server will allocate it.</param>
            /// <param name="message">Message stream.</param>
            /// <param name="count">Number of bytes send from <b>message</b> stream.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is riased when <b>folder</b> or <b>message</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public StoreMessageAsyncOP(string folder,IMAP_t_MsgFlags flags,DateTime internalDate,Stream message,long count,EventHandler<EventArgs<IMAP_r_u>> callback)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(string.IsNullOrEmpty(folder)){
                    throw new ArgumentException("Argument 'folder' value must be specified.","folder");
                }
                if(message == null){
                    throw new ArgumentNullException("message");
                }
                if(count < 1){
                    throw new ArgumentException("Argument 'count' value must be >= 1.","count");
                }

                m_Folder       = folder;
                m_pFlags       = flags;
                m_InternalDate = internalDate;
                m_pStream      = message;
                m_Count        = count;
                m_pCallback    = callback;
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
                m_pImapClient    = null;
                m_pFinalResponse = null;
                m_pCallback      = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                                
                m_pImapClient = owner;
                        
                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 3501 6.3.11. APPEND Command.
                        Arguments:  mailbox name
                                    OPTIONAL flag parenthesized list
                                    OPTIONAL date/time string
                                    message literal

                        Responses:  no specific responses for this command

                        Result:     OK - append completed
                                    NO - append error: can't append to that mailbox, error
                                         in flags or date/time or message text
                                    BAD - command unknown or arguments invalid

                        The APPEND command appends the literal argument as a new message
                        to the end of the specified destination mailbox.  This argument
                        SHOULD be in the format of an [RFC-2822] message.  8-bit
                        characters are permitted in the message.  A server implementation
                        that is unable to preserve 8-bit data properly MUST be able to
                        reversibly convert 8-bit APPEND data to 7-bit using a [MIME-IMB]
                        content transfer encoding.

                        Note: There MAY be exceptions, e.g., draft messages, in
                        which required [RFC-2822] header lines are omitted in
                        the message literal argument to APPEND.  The full
                        implications of doing so MUST be understood and
                        carefully weighed.

                        If a flag parenthesized list is specified, the flags SHOULD be set
                        in the resulting message; otherwise, the flag list of the
                        resulting message is set to empty by default.  In either case, the
                        Recent flag is also set.

                        If a date-time is specified, the internal date SHOULD be set in
                        the resulting message; otherwise, the internal date of the
                        resulting message is set to the current date and time by default.

                        If the append is unsuccessful for any reason, the mailbox MUST be
                        restored to its state before the APPEND attempt; no partial
                        appending is permitted.

                        If the destination mailbox does not exist, a server MUST return an
                        error, and MUST NOT automatically create the mailbox.  Unless it
                        is certain that the destination mailbox can not be created, the
                        server MUST send the response code "[TRYCREATE]" as the prefix of
                        the text of the tagged NO response.  This gives a hint to the
                        client that it can attempt a CREATE command and retry the APPEND
                        if the CREATE is successful.

                        If the mailbox is currently selected, the normal new message
                        actions SHOULD occur.  Specifically, the server SHOULD notify the
                        client immediately via an untagged EXISTS response.  If the server
                        does not do so, the client MAY issue a NOOP command (or failing
                        that, a CHECK command) after one or more APPEND commands.

                        Example:    C: A003 APPEND saved-messages (\Seen) {310}
                                    S: + Ready for literal data
                                    C: Date: Mon, 7 Feb 1994 21:52:25 -0800 (PST)
                                    C: From: Fred Foobar <foobar@Blurdybloop.COM>
                                    C: Subject: afternoon meeting
                                    C: To: mooch@owatagu.siam.edu
                                    C: Message-Id: <B27397-0100000@Blurdybloop.COM>
                                    C: MIME-Version: 1.0
                                    C: Content-Type: TEXT/PLAIN; CHARSET=US-ASCII
                                    C:
                                    C: Hello Joe, do you think we can meet at 3:30 tomorrow?
                                    C:
                                    S: A003 OK APPEND completed

                        Note: The APPEND command is not used for message delivery,
                        because it does not provide a mechanism to transfer [SMTP]
                        envelope information.
                    */

                    StringBuilder command = new StringBuilder();
                    command.Append((m_pImapClient.m_CommandIndex++).ToString("d5"));            
                    command.Append(" APPEND");
                    command.Append(" " + IMAP_Utils.EncodeMailbox(m_Folder,m_pImapClient.m_MailboxEncoding));
                    if(m_pFlags != null){
                        command.Append(" (");
                        string[] flags = m_pFlags.ToArray();
                        for(int i=0;i<flags.Length;i++){
                            if(i > 0){
                                command.Append(" ");
                            }
                            command.Append(flags[i]);
                        }                
                        command.Append(")");
                    }
                    if(m_InternalDate != DateTime.MinValue){
                        command.Append(" " + TextUtils.QuoteString(IMAP_Utils.DateTimeToString(m_InternalDate)));
                    }
                    command.Append(" {" + m_Count + "}\r\n");

                    byte[] cmdLine    = Encoding.UTF8.GetBytes(command.ToString());
                    string cmdLineLog = Encoding.UTF8.GetString(cmdLine).TrimEnd();

                    SendCmdAndReadRespAsyncOP args = new SendCmdAndReadRespAsyncOP(cmdLine,cmdLineLog,m_pCallback);
                    args.CompletedAsync += delegate(object sender,EventArgs<SendCmdAndReadRespAsyncOP> e){
                        ProcessCmdSendingResult(args);
                    };
                    // Operation completed synchronously.
                    if(!m_pImapClient.SendCmdAndReadRespAsync(args)){
                        ProcessCmdSendingResult(args);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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

            #region method ProcessCmdSendingResult

            /// <summary>
            /// Processes intial command line sending result.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ProcessCmdSendingResult(SendCmdAndReadRespAsyncOP op)
            {
                try{
                    // Command send/receive failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                    }
                    // Command send/receive succeeded.
                    else{ 
                        // IMAP server returned continue response.
                        if(op.FinalResponse.IsContinue){
                            // Send message literal.
                            SmartStream.WriteStreamAsyncOP writeOP = new SmartStream.WriteStreamAsyncOP(m_pStream,m_Count);
                            writeOP.CompletedAsync += delegate(object sender,EventArgs<SmartStream.WriteStreamAsyncOP> e){
                                ProcessMsgSendingResult(writeOP);
                            };
                            // Operation completed synchronously.
                            if(!m_pImapClient.TcpStream.WriteStreamAsync(writeOP)){
                                ProcessMsgSendingResult(writeOP);
                            }
                        }
                        // IMAP server returned error response.
                        else{
                            m_pFinalResponse = op.FinalResponse;
                            m_pException = new IMAP_ClientException(op.FinalResponse);
                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    SetState(AsyncOP_State.Completed);
                }
                finally{
                    op.Dispose();
                }
            }

            #endregion

            #region method ProcessMsgSendingResult

            /// <summary>
            /// Processes message literal sending result.
            /// </summary>
            /// <param name="writeOP">Asynchronous operation.</param>
            private void ProcessMsgSendingResult(SmartStream.WriteStreamAsyncOP writeOP)
            {
                try{
                    // Message literal sending failed.
                    if(writeOP.Error != null){
                        m_pException = writeOP.Error;
                        m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                        SetState(AsyncOP_State.Completed);
                    }
                    // Message literal sending succeeded.
                    else{
                        // Log
                        m_pImapClient.LogAddWrite(m_Count,"Wrote " + m_Count + " bytes.");

                        // Send remaining command line(which is CRLF) and read response.
                        SendCmdAndReadRespAsyncOP args = new SendCmdAndReadRespAsyncOP(new byte[]{(int)'\r',(int)'\n'},"",m_pCallback);
                        args.CompletedAsync += delegate(object sender,EventArgs<SendCmdAndReadRespAsyncOP> e){
                            if(args.Error != null){
                                m_pException = args.Error;
                            }
                            else{                                
                                if(args.FinalResponse.IsError){
                                    m_pException = new IMAP_ClientException(args.FinalResponse);
                                }
                                m_pFinalResponse = args.FinalResponse;
                            }

                            SetState(AsyncOP_State.Completed);
                        };
                        // Operation completed synchronously.
                        if(!m_pImapClient.SendCmdAndReadRespAsync(args)){
                            if(args.Error != null){
                                m_pException = args.Error;
                            }
                            else{
                                if(args.FinalResponse.IsError){
                                    m_pException = new IMAP_ClientException(args.FinalResponse);
                                }
                                m_pFinalResponse = args.FinalResponse;
                            }

                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    SetState(AsyncOP_State.Completed);
                }
                finally{
                    writeOP.Dispose();
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
            /// Returns IMAP server final response.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_r_ServerStatus FinalResponse
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pFinalResponse; 
                }
            }

            /// <summary>
            /// Gets <b>APPENDUID</b> optional response. Returns null if IMAP server doesn't support <b>UIDPLUS</b> extention.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_t_orc_AppendUid AppendUid
            {
                get{ 
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    if(m_pFinalResponse != null && m_pFinalResponse.OptionalResponse != null && m_pFinalResponse.OptionalResponse is IMAP_t_orc_AppendUid){
                        return ((IMAP_t_orc_AppendUid)m_pFinalResponse.OptionalResponse);
                    }
                    else{
                        return null;
                    }
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<StoreMessageAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<StoreMessageAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes APPEND command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="StoreMessageAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool StoreMessageAsync(StoreMessageAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method Enable

        /// <summary>
        /// Enables the specified IMAP capabilities in server.
        /// </summary>
        /// <param name="capabilities">IMAP capabilities.</param>
        /// <returns>Returns enabled capabilities info.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>capabilities</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public IMAP_r_u_Enable[] Enable(string[] capabilities)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(this.SelectedFolder != null){
                throw new InvalidOperationException("The 'ENABLE' command MUST only be used in the authenticated state.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(capabilities == null){
                throw new ArgumentNullException("capabilities");
            }
            if(capabilities.Length < 1){
                throw new ArgumentException("Argument 'capabilities' must contain at least 1 value.","capabilities");
            }

            List<IMAP_r_u_Enable> retVal = new List<IMAP_r_u_Enable>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_Enable){
                    retVal.Add((IMAP_r_u_Enable)e.Value);
                }
            };

            using(EnableAsyncOP op = new EnableAsyncOP(capabilities,callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<EnableAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.EnableAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method EnableAsync

        #region class EnableAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.EnableAsync"/> asynchronous operation.
        /// </summary>
        public class EnableAsyncOP : CmdAsyncOP<EnableAsyncOP>
        {
            private string[] m_pCapabilities = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="capabilities">Folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>capabilities</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public EnableAsyncOP(string[] capabilities,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(capabilities == null){
                    throw new ArgumentNullException("capabilities");
                }
                if(capabilities.Length < 1){
                    throw new ArgumentException("Argument 'capabilities' must contain at least 1 value.","capabilities");
                }

                m_pCapabilities = capabilities;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* 3.1.  The ENABLE Command
                    Arguments: capability names

                    Result:    OK: Relevant capabilities enabled
                               BAD: No arguments, or syntax error in an argument

                    The ENABLE command takes a list of capability names, and requests the
                    server to enable the named extensions.  Once enabled using ENABLE,
                    each extension remains active until the IMAP connection is closed.
                */

                StringBuilder cmd = new StringBuilder();
                cmd.Append((imap.m_CommandIndex++).ToString("d5") + " ENABLE");
                foreach(string capability in m_pCapabilities){
                    cmd.Append(" " + capability);
                }
                cmd.Append("\r\n");

                byte[] cmdLine = Encoding.UTF8.GetBytes(cmd.ToString());
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes ENABLE command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool EnableAsync(EnableAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(this.SelectedFolder != null){
                throw new InvalidOperationException("The 'ENABLE' command MUST only be used in the authenticated state.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region mehod EnableUtf8

        /// <summary>
        /// Enables UTF-8 support in IMAP server.
        /// </summary>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state(not-connected,not-authenticated or selected state).</exception>
        /// <remarks>Before calling this method, you need to check IMAP capability list to see if server supports "UTF8=ACCEPT" or "UTF8=ALL" capability.
        /// For more info see <see href="http://tools.ietf.org/html/rfc5738">rfc5738</see>.</remarks>
        public void EnableUtf8()
        {
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(this.SelectedFolder != null){
                throw new InvalidOperationException("The 'ENABLE UTF8=ACCEPT' command MUST only be used in the authenticated state.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }

            /* RFC 5161 and RFC 5738 3.
                The "ENABLE UTF8=ACCEPT" command MUST only be used in the authenticated state.
            */

            IMAP_r_u_Enable[] response = Enable(new string[]{"UTF8=ACCEPT"});

            // Per specification we may send "utf8-quoted" string when server reports "UTF8=ACCEPT" or "UTF8=ALL" without
            // sending "ENABLE UTF8=ACCEPT" command. We just enable sending or receiving utf-8 once this command is called.
            m_MailboxEncoding = IMAP_Mailbox_Encoding.ImapUtf8;
        }

        #endregion


        #region method CloseFolder

        /// <summary>
        /// Closes selected folder, all messages marked as Deleted will be expunged.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void CloseFolder()
        {       
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }

            using(CloseFolderAsyncOP op = new CloseFolderAsyncOP(null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<CloseFolderAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.CloseFolderAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method CloseFolderAsync

        #region class CloseFolderAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.CloseFolderAsync"/> asynchronous operation.
        /// </summary>
        public class CloseFolderAsyncOP : IDisposable,IAsyncOP
        {
            private object                            m_pLock          = new object();
            private AsyncOP_State                     m_State          = AsyncOP_State.WaitingForStart;
            private Exception                         m_pException     = null;
            private IMAP_r_ServerStatus               m_pFinalResponse = null;
            private IMAP_Client                       m_pImapClient    = null;
            private bool                              m_RiseCompleted  = false;
            private EventHandler<EventArgs<IMAP_r_u>> m_pCallback      = null;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public CloseFolderAsyncOP(EventHandler<EventArgs<IMAP_r_u>> callback)
            {
                m_pCallback = callback;
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
                m_pImapClient    = null;
                m_pFinalResponse = null;
                m_pCallback      = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                                
                m_pImapClient = owner;
                        
                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 3501 6.4.2. CLOSE Command.
                        Arguments:  none

                        Responses:  no specific responses for this command

                        Result:     OK - close completed, now in authenticated state
                                    BAD - command unknown or arguments invalid

                        The CLOSE command permanently removes all messages that have the
                        \Deleted flag set from the currently selected mailbox, and returns
                        to the authenticated state from the selected state.  No untagged
                        EXPUNGE responses are sent.

                        No messages are removed, and no error is given, if the mailbox is
                        selected by an EXAMINE command or is otherwise selected read-only.

                        Even if a mailbox is selected, a SELECT, EXAMINE, or LOGOUT
                        command MAY be issued without previously issuing a CLOSE command.
                        The SELECT, EXAMINE, and LOGOUT commands implicitly close the
                        currently selected mailbox without doing an expunge.  However,
                        when many messages are deleted, a CLOSE-LOGOUT or CLOSE-SELECT
                        sequence is considerably faster than an EXPUNGE-LOGOUT or
                        EXPUNGE-SELECT because no untagged EXPUNGE responses (which the
                        client would probably ignore) are sent.

                        Example:    C: A341 CLOSE
                                    S: A341 OK CLOSE completed

                    */
                    
                    byte[] cmdLine    = Encoding.UTF8.GetBytes((m_pImapClient.m_CommandIndex++).ToString("d5") + " CLOSE\r\n");
                    string cmdLineLog = Encoding.UTF8.GetString(cmdLine).TrimEnd();

                    SendCmdAndReadRespAsyncOP args = new SendCmdAndReadRespAsyncOP(cmdLine,cmdLineLog,m_pCallback);
                    args.CompletedAsync += delegate(object sender,EventArgs<SendCmdAndReadRespAsyncOP> e){
                        ProecessCmdResult(e.Value);
                    };
                    // Operation completed synchronously.
                    if(!m_pImapClient.SendCmdAndReadRespAsync(args)){
                        ProecessCmdResult(args);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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

            #region method ProecessCmdResult

            /// <summary>
            /// Processes command result.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ProecessCmdResult(SendCmdAndReadRespAsyncOP op)
            {
                try{
                    // Command send/receive failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    }
                    // Command send/receive succeeded.
                    else{
                        m_pFinalResponse = op.FinalResponse;

                        // IMAP server returned error response.
                        if(op.FinalResponse.IsError){
                            m_pException = new IMAP_ClientException(op.FinalResponse);
                        }
                        // IMAP server returned success response.
                        else{
                            m_pImapClient.m_pSelectedFolder = null;
                        }
                    }

                    SetState(AsyncOP_State.Completed);
                }
                finally{
                    op.Dispose();
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
            /// Returns IMAP server final response.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_r_ServerStatus FinalResponse
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pFinalResponse; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<CloseFolderAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<CloseFolderAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes CLOSE command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CloseFolderAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool CloseFolderAsync(CloseFolderAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method Fetch
                
        /// <summary>
        /// Fetches specified message items.
        /// </summary>
        /// <param name="uid">Specifies if argument <b>seqSet</b> contains messages UID or sequence numbers.</param>
        /// <param name="seqSet">Sequence set of messages to fetch.</param>
        /// <param name="items">Fetch items to fetch.</param>
        /// <param name="callback">Optional callback to be called for each server returned untagged response.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>seqSet</b> or <b>items</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        /// <remarks>Fetch raises <see cref="UntaggedResponse"/> for ecach fetched message.</remarks>
        public void Fetch(bool uid,IMAP_t_SeqSet seqSet,IMAP_t_Fetch_i[] items,EventHandler<EventArgs<IMAP_r_u>> callback)
        {   
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(seqSet == null){
                throw new ArgumentNullException("seqSet");
            }
            if(items == null){
                throw new ArgumentNullException("items");
            }
            if(items.Length < 1){
                throw new ArgumentException("Argument 'items' must conatain at least 1 value.","items");
            }
                        
            using(FetchAsyncOP op = new FetchAsyncOP(uid,seqSet,items,callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<FetchAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.FetchAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method FetchAsync

        #region class FetchAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.FetchAsync"/> asynchronous operation.
        /// </summary>
        public class FetchAsyncOP : CmdAsyncOP<FetchAsyncOP>
        {
            private bool             m_Uid        = false;
            private IMAP_t_SeqSet    m_pSeqSet    = null;
            private IMAP_t_Fetch_i[] m_pDataItems = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="uid">Specifies if argument <b>seqSet</b> contains messages UID or sequence numbers.</param>
            /// <param name="seqSet">Sequence set of messages to fetch.</param>
            /// <param name="items">Fetch items to fetch.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>seqSet</b> or <b>items</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public FetchAsyncOP(bool uid,IMAP_t_SeqSet seqSet,IMAP_t_Fetch_i[] items,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(seqSet == null){
                    throw new ArgumentNullException("seqSet");
                }
                if(items == null){
                    throw new ArgumentNullException("items");
                }                
                if(items.Length < 1){
                    throw new ArgumentException("Argument 'items' must conatain at least 1 value.","items");
                }

                m_Uid        = uid;
                m_pSeqSet    = seqSet;
                m_pDataItems = items;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.4.5. FETCH Command.
                    Arguments:  sequence set
                                message data item names or macro

                    Responses:  untagged responses: FETCH

                    Result:     OK - fetch completed
                                NO - fetch error: can't fetch that data
                                BAD - command unknown or arguments invalid

                    The FETCH command retrieves data associated with a message in the
                    mailbox.  The data items to be fetched can be either a single atom
                    or a parenthesized list.

                    Most data items, identified in the formal syntax under the
                    msg-att-static rule, are static and MUST NOT change for any
                    particular message.  Other data items, identified in the formal
                    syntax under the msg-att-dynamic rule, MAY change, either as a
                    result of a STORE command or due to external events.

                        For example, if a client receives an ENVELOPE for a
                        message when it already knows the envelope, it can
                        safely ignore the newly transmitted envelope.
                */

                StringBuilder command = new StringBuilder();
                command.Append((imap.m_CommandIndex++).ToString("d5"));
                if(m_Uid){
                    command.Append(" UID");
                }
                command.Append(" FETCH " + m_pSeqSet.ToString() + " (");
                for(int i=0;i<m_pDataItems.Length;i++){
                    if(i > 0){
                        command.Append(" ");
                    }
                    command.Append(m_pDataItems[i].ToString());
                }
                command.Append(")\r\n");

                byte[] cmdLine = Encoding.UTF8.GetBytes(command.ToString());
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts executing FETCH command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool FetchAsync(FetchAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method Search

        /// <summary>
        /// Searches message what matches specified search criteria.
        /// </summary>
        /// <param name="uid">If true then UID SERACH, otherwise normal SEARCH.</param>
        /// <param name="charset">Charset used in search criteria. Value null means ASCII. The UTF-8 is reccomended value non ASCII searches.</param>
        /// <param name="criteria">Search criteria.</param>
        /// <returns>Returns search expression matehced messages sequence-numbers or UIDs(This depends on argument <b>uid</b> value).</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is rised when <b>criteria</b> is null reference.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public int[] Search(bool uid,Encoding charset,IMAP_Search_Key criteria)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(criteria == null){
                throw new ArgumentNullException("criteria");
            }
            
            List<int> retVal = new List<int>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_Search){
                    retVal.AddRange(((IMAP_r_u_Search)e.Value).Values);
                }
            };

            using(SearchAsyncOP op = new SearchAsyncOP(uid,charset,criteria,callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<SearchAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.SearchAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }
                
        #endregion

        #region method SearchAsync

        #region class SearchAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.SearchAsync"/> asynchronous operation.
        /// </summary>
        public class SearchAsyncOP : CmdAsyncOP<SearchAsyncOP>
        {
            private bool            m_Uid       = false;
            private Encoding        m_pCharset  = null;
            private IMAP_Search_Key m_pCriteria = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="uid">Specifies if argument <b>seqSet</b> contains messages UID or sequence numbers.</param>
            /// <param name="charset">Charset used in search criteria. Value null means ASCII. The UTF-8 is reccomended value non ASCII searches.</param>
            /// <param name="criteria">Search criteria.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>criteria</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public SearchAsyncOP(bool uid,Encoding charset,IMAP_Search_Key criteria,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(criteria == null){
                    throw new ArgumentNullException("criteria");
                } 

                m_Uid       = uid;
                m_pCharset  = charset;
                m_pCriteria = criteria;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.4.4.  SEARCH Command.
                    Arguments:  OPTIONAL [CHARSET] specification
                                   searching criteria (one or more)

                    Responses:  REQUIRED untagged response: SEARCH

                    Result:     OK - search completed
                                NO - search error: can't search that [CHARSET] or criteria
                               BAD - command unknown or arguments invalid

                      The SEARCH command searches the mailbox for messages that match
                      the given searching criteria.  Searching criteria consist of one
                      or more search keys.  The untagged SEARCH response from the server
                      contains a listing of message sequence numbers corresponding to
                      those messages that match the searching criteria.

                      When multiple keys are specified, the result is the intersection
                      (AND function) of all the messages that match those keys.  For
                      example, the criteria DELETED FROM "SMITH" SINCE 1-Feb-1994 refers
                      to all deleted messages from Smith that were placed in the mailbox
                      since February 1, 1994.  A search key can also be a parenthesized
                      list of one or more search keys (e.g., for use with the OR and NOT
                      keys).

                      The OPTIONAL [CHARSET] specification consists of the word
                      "CHARSET" followed by a registered [CHARSET].  It indicates the
                      [CHARSET] of the strings that appear in the search criteria.
                      [MIME-IMB] content transfer encodings, and [MIME-HDRS] strings in
                      [RFC-2822]/[MIME-IMB] headers, MUST be decoded before comparing
                      text in a [CHARSET] other than US-ASCII.  US-ASCII MUST be
                      supported; other [CHARSET]s MAY be supported.
                */

                /* RFC 3501.
                    literal = "{" number "}" CRLF *CHAR8
                               ; Number represents the number of CHAR8s
                    CHAR8   = %x01-ff
                               ; any OCTET except NUL, %x00
                         
                    NOTE: Literal data is sent only when server responds "+ Continue ..."
                */
                
                ByteBuilder currentCmdLine = new ByteBuilder();
                List<ByteBuilder> cmdLines = new List<ByteBuilder>();                
                cmdLines.Add(currentCmdLine);

                currentCmdLine.Append((imap.m_CommandIndex++).ToString("d5"));
                if(m_Uid){
                    currentCmdLine.Append(" UID");
                }
                currentCmdLine.Append(" SEARCH");
                if(m_pCharset != null){
                    currentCmdLine.Append(" CHARSET " + m_pCharset.WebName.ToUpper());
                }
                currentCmdLine.Append(" (");
                //--- Build search items --------------------------------------------
                List<IMAP_Client_CmdPart> cmdParts = new List<IMAP_Client_CmdPart>();
                m_pCriteria.ToCmdParts(cmdParts);
                foreach(IMAP_Client_CmdPart cmdPart in cmdParts){
                    // Command part is string constant.
                    if(cmdPart.Type == IMAP_Client_CmdPart_Type.Constant){
                        currentCmdLine.Append(cmdPart.Value);
                    }
                    // Command part is string value.
                    else{
                        // NOTE: If charset specified, we ma not use IMAP utf-8 syntax and must use "literal" for non ASCII values.

                        // We need to use string as IMAP literal.
                        if(IMAP_Utils.MustUseLiteralString(cmdPart.Value,(m_pCharset == null && imap.m_MailboxEncoding == IMAP_Mailbox_Encoding.ImapUtf8))){
                            currentCmdLine.Append("{" + m_pCharset.GetByteCount(cmdPart.Value) + "}\r\n");                            
                            // Add new command line and set it as active.
                            currentCmdLine = new ByteBuilder();
                            cmdLines.Add(currentCmdLine);
                            // Append value.
                            currentCmdLine.Append(m_pCharset,cmdPart.Value);
                        }
                        // We enabed "UTF-8 ACCEPT", we need to us IMAP utf-8 syntax.
                        else if(m_pCharset == null && imap.m_MailboxEncoding == IMAP_Mailbox_Encoding.ImapUtf8){
                            currentCmdLine.Append(IMAP_Utils.EncodeMailbox(cmdPart.Value,imap.m_MailboxEncoding));
                        }
                        // Normal ASCII quoted string.
                        else{
                            currentCmdLine.Append(TextUtils.QuoteString(cmdPart.Value));
                        }
                    }
                }
                //--------------------------------------------------------------------
                currentCmdLine.Append(")\r\n");

                // Set command lines and their log lines.
                List<string> logLines = new List<string>();
                foreach(ByteBuilder cmdLine in cmdLines){
                    this.CmdLines.Add(new CmdLine(cmdLine.ToByte(),Encoding.UTF8.GetString(cmdLine.ToByte()).TrimEnd()));
                }
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts executing SEARCH command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool SearchAsync(SearchAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method StoreMessageFlags
                
        /// <summary>
        /// Stores specified message flags to the sepcified messages.
        /// </summary>
        /// <param name="uid">Specifies if <b>seqSet</b> contains UIDs or sequence-numbers.</param>
        /// <param name="seqSet">Messages sequence-set.</param>
        /// <param name="setType">Specifies how flags are set.</param>
        /// <param name="flags">Message flags.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>seqSet</b> or <b>flags</b> is null reference.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void StoreMessageFlags(bool uid,IMAP_t_SeqSet seqSet,IMAP_Flags_SetType setType,IMAP_t_MsgFlags flags)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(seqSet == null){
                throw new ArgumentNullException("seqSet");
            }
            if(flags == null){
                throw new ArgumentNullException("flags");
            }

            using(StoreMessageFlagsAsyncOP op = new StoreMessageFlagsAsyncOP(uid,seqSet,true,setType,flags,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<StoreMessageFlagsAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.StoreMessageFlagsAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method StoreMessageFlagsAsync

        #region class StoreMessageFlagsAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.StoreMessageFlagsAsync"/> asynchronous operation.
        /// </summary>
        public class StoreMessageFlagsAsyncOP : CmdAsyncOP<StoreMessageFlagsAsyncOP>
        {
            private bool               m_Uid          = false;
            private IMAP_t_SeqSet      m_pSeqSet      = null;
            private bool               m_Silent       = true;
            private IMAP_Flags_SetType m_FlagsSetType = IMAP_Flags_SetType.Replace;
            private IMAP_t_MsgFlags    m_pMsgFlags    = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="uid">Specifies if <b>seqSet</b> contains UIDs or message-numberss.</param>
            /// <param name="seqSet">Messages sequence set.</param>
            /// <param name="silent">If true, no FETCH (FLAGS) response returned by server.</param>
            /// <param name="setType">Specifies how flags are set.</param>
            /// <param name="msgFlags">Message flags.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>seqSet</b> or <b>msgFlags</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public StoreMessageFlagsAsyncOP(bool uid,IMAP_t_SeqSet seqSet,bool silent,IMAP_Flags_SetType setType,IMAP_t_MsgFlags msgFlags,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(seqSet == null){
                    throw new ArgumentNullException("seqSet");
                }
                if(msgFlags == null){
                    throw new ArgumentNullException("msgFlags");
                }

                m_Uid          = uid;
                m_pSeqSet      = seqSet;
                m_Silent       = silent;
                m_FlagsSetType = setType;
                m_pMsgFlags    = msgFlags;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.4.6. STORE Command.
                    Arguments:  sequence set
                                message data item name
                                value for message data item

                    Responses:  untagged responses: FETCH

                    Result:     OK - store completed
                                NO - store error: can't store that data
                                BAD - command unknown or arguments invalid

                    The STORE command alters data associated with a message in the
                    mailbox.  Normally, STORE will return the updated value of the
                    data with an untagged FETCH response.  A suffix of ".SILENT" in
                    the data item name prevents the untagged FETCH, and the server
                    SHOULD assume that the client has determined the updated value
                    itself or does not care about the updated value.

                        Note: Regardless of whether or not the ".SILENT" suffix
                        was used, the server SHOULD send an untagged FETCH
                        response if a change to a message's flags from an
                        external source is observed.  The intent is that the
                        status of the flags is determinate without a race
                        condition.

                    The currently defined data items that can be stored are:

                    FLAGS <flag list>
                        Replace the flags for the message (other than \Recent) with the
                        argument.  The new value of the flags is returned as if a FETCH
                        of those flags was done.

                    FLAGS.SILENT <flag list>
                        Equivalent to FLAGS, but without returning a new value.

                    +FLAGS <flag list>
                        Add the argument to the flags for the message.  The new value
                        of the flags is returned as if a FETCH of those flags was done.

                    +FLAGS.SILENT <flag list>
                        Equivalent to +FLAGS, but without returning a new value.

                    -FLAGS <flag list>
                        Remove the argument from the flags for the message.  The new
                        value of the flags is returned as if a FETCH of those flags was
                        done.

                    -FLAGS.SILENT <flag list>
                        Equivalent to -FLAGS, but without returning a new value.


                    Example:    C: A003 STORE 2:4 +FLAGS (\Deleted)
                                S: * 2 FETCH (FLAGS (\Deleted \Seen))
                                S: * 3 FETCH (FLAGS (\Deleted))
                                S: * 4 FETCH (FLAGS (\Deleted \Flagged \Seen))
                                S: A003 OK STORE completed
                */

                StringBuilder command = new StringBuilder();
                command.Append((imap.m_CommandIndex++).ToString("d5"));
                if(m_Uid){
                    command.Append(" UID");
                }
                command.Append(" STORE");
                command.Append(" " + m_pSeqSet.ToString());
                if(m_FlagsSetType == IMAP_Flags_SetType.Add){
                    command.Append(" +FLAGS");
                }
                else if(m_FlagsSetType == IMAP_Flags_SetType.Remove){
                    command.Append(" -FLAGS");
                }
                else if(m_FlagsSetType == IMAP_Flags_SetType.Replace){
                    command.Append(" FLAGS");
                }
                else{
                    throw new NotSupportedException("Not supported argument 'setType' value '" + m_FlagsSetType.ToString() + "'.");
                }
                if(m_Silent){
                    command.Append(".SILENT");
                }
                if(m_pMsgFlags != null){
                    command.Append(" (");
                    string[] flags = m_pMsgFlags.ToArray();
                    for(int i=0;i<flags.Length;i++){
                        if(i > 0){
                            command.Append(" ");
                        }
                        command.Append(flags[i]);
                    }                
                    command.Append(")\r\n");
                }
                else{
                    command.Append(" ()\r\n");
                }

                byte[] cmdLine = Encoding.UTF8.GetBytes(command.ToString());
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes STORE command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool StoreMessageFlagsAsync(StoreMessageFlagsAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method CopyMessages
        
        /// <summary>
        /// Copies specified messages from current selected folder to the specified target folder.
        /// </summary>
        /// <param name="uid">Specifies if <b>seqSet</b> contains UIDs or message-numberss.</param>
        /// <param name="seqSet">Messages sequence set.</param>
        /// <param name="targetFolder">Target folder name with path.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>seqSet</b> or <b>targetFolder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void CopyMessages(bool uid,IMAP_t_SeqSet seqSet,string targetFolder)
        {    
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(seqSet == null){
                throw new ArgumentNullException("seqSet");
            }
            if(targetFolder == null){
                throw new ArgumentNullException("folder");
            }
            if(targetFolder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            using(CopyMessagesAsyncOP op = new CopyMessagesAsyncOP(uid,seqSet,targetFolder,null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<CopyMessagesAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.CopyMessagesAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        /// <summary>
        /// Copies specified messages from current selected folder to the specified target folder.
        /// </summary>
        /// <param name="op">Copy messages operation.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void CopyMessages(CopyMessagesAsyncOP op)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }

            using(ManualResetEvent wait = new ManualResetEvent(false)){
                op.CompletedAsync += delegate(object s1,EventArgs<CopyMessagesAsyncOP> e1){
                    wait.Set();
                };
                if(!this.CopyMessagesAsync(op)){
                    wait.Set();
                }
                wait.WaitOne();

                if(op.Error != null){
                    throw op.Error;
                }
            }
        }

        #endregion

        #region method CopyMessagesAsync

        #region class CopyMessagesAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.CopyMessagesAsync"/> asynchronous operation.
        /// </summary>
        public class CopyMessagesAsyncOP : CmdAsyncOP<CopyMessagesAsyncOP>
        {
            private bool          m_Uid          = false;
            private IMAP_t_SeqSet m_pSeqSet      = null;
            private string        m_TargetFolder = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="uid">Specifies if <b>seqSet</b> contains UIDs or message-numberss.</param>
            /// <param name="seqSet">Messages sequence set.</param>
            /// <param name="targetFolder">Target folder name with path.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>seqSet</b> or <b>targetFolder</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public CopyMessagesAsyncOP(bool uid,IMAP_t_SeqSet seqSet,string targetFolder,EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
                if(seqSet == null){
                    throw new ArgumentNullException("seqSet");
                }
                if(targetFolder == null){
                    throw new ArgumentNullException("targetFolder");
                }
                if(string.IsNullOrEmpty(targetFolder)){
                    throw new ArgumentException("Argument 'targetFolder' value must be specified.","targetFolder");
                }

                m_Uid          = uid;
                m_pSeqSet      = seqSet;
                m_TargetFolder = targetFolder;
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.4.7. COPY Command.
                    Arguments:  sequence set
                                mailbox name

                    Responses:  no specific responses for this command

                    Result:     OK - copy completed
                                NO - copy error: can't copy those messages or to that
                                     name
                                BAD - command unknown or arguments invalid

                    The COPY command copies the specified message(s) to the end of the
                    specified destination mailbox.  The flags and internal date of the
                    message(s) SHOULD be preserved, and the Recent flag SHOULD be set,
                    in the copy.

                    If the destination mailbox does not exist, a server SHOULD return
                    an error.  It SHOULD NOT automatically create the mailbox.  Unless
                    it is certain that the destination mailbox can not be created, the
                    server MUST send the response code "[TRYCREATE]" as the prefix of
                    the text of the tagged NO response.  This gives a hint to the
                    client that it can attempt a CREATE command and retry the COPY if
                    the CREATE is successful.

                    If the COPY command is unsuccessful for any reason, server
                    implementations MUST restore the destination mailbox to its state
                    before the COPY attempt.

                    Example:    C: A003 COPY 2:4 MEETING
                                S: A003 OK COPY completed
                */
                
                if(m_Uid){
                    byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " UID COPY " + m_pSeqSet.ToString() + " " + IMAP_Utils.EncodeMailbox(m_TargetFolder,imap.m_MailboxEncoding) + "\r\n");
                    this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
                }
                else{
                    byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " COPY " + m_pSeqSet.ToString() + " " + IMAP_Utils.EncodeMailbox(m_TargetFolder,imap.m_MailboxEncoding) + "\r\n");
                    this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
                }
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets <b>COPYUID</b> optional response. Returns null if IMAP server doesn't support <b>UIDPLUS</b> extention.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_t_orc_CopyUid CopyUid
            {
                get{ 
                    if(this.State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(this.State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    if(this.FinalResponse != null && this.FinalResponse.OptionalResponse != null && this.FinalResponse.OptionalResponse is IMAP_t_orc_CopyUid){
                        return ((IMAP_t_orc_CopyUid)this.FinalResponse.OptionalResponse);
                    }
                    else{
                        return null;
                    }
                }
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes COPY command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool CopyMessagesAsync(CopyMessagesAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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
                
        #region method MoveMessages
               
        /// <summary>
        /// Moves specified messages from current selected folder to the specified target folder.
        /// </summary>
        /// <param name="uid">Specifies if <b>seqSet</b> contains UIDs or message-numberss.</param>
        /// <param name="seqSet">Messages sequence set.</param>
        /// <param name="targetFolder">Target folder name with path.</param>
        /// <param name="expunge">If ture messages are expunged from selected folder, otherwise they are marked as <b>Deleted</b>.
        /// Note: If true - then all messages marked as <b>Deleted</b> are expunged !</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void MoveMessages(bool uid,IMAP_t_SeqSet seqSet,string targetFolder,bool expunge)
        {            
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(seqSet == null){
                throw new ArgumentNullException("seqSet");
            }
            if(targetFolder == null){
                throw new ArgumentNullException("folder");
            }
            if(targetFolder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            CopyMessages(uid,seqSet,targetFolder);
            StoreMessageFlags(uid,seqSet,IMAP_Flags_SetType.Add,IMAP_t_MsgFlags.Parse(IMAP_t_MsgFlags.Deleted));
            if(expunge){
                Expunge();
            }
        }

        #endregion

        #region method Expunge

        /// <summary>
        /// Deletes all messages in selected folder which has "Deleted" flag set.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void Expunge()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }

            using(ExpungeAsyncOP op = new ExpungeAsyncOP(null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<ExpungeAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.ExpungeAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }
        }

        #endregion

        #region method ExpungeAsync

        #region class ExpungeAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.ExpungeAsync"/> asynchronous operation.
        /// </summary>
        public class ExpungeAsyncOP : CmdAsyncOP<ExpungeAsyncOP>
        {
            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public ExpungeAsyncOP(EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.4.3. EXPUNGE Command.
                    Arguments:  none

                    Responses:  untagged responses: EXPUNGE

                    Result:     OK - expunge completed
                                NO - expunge failure: can't expunge (e.g., permission
                                     denied)
                                BAD - command unknown or arguments invalid

                    The EXPUNGE command permanently removes all messages that have the
                    \Deleted flag set from the currently selected mailbox.  Before
                    returning an OK to the client, an untagged EXPUNGE response is
                    sent for each message that is removed.

                    Example:    C: A202 EXPUNGE
                                S: * 3 EXPUNGE
                                S: * 3 EXPUNGE
                                S: * 5 EXPUNGE
                                S: * 8 EXPUNGE
                                S: A202 OK EXPUNGE completed

                    Note: In this example, messages 3, 4, 7, and 11 had the
                    \Deleted flag set.  See the description of the EXPUNGE
                    response for further explanation.
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " EXPUNGE" + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes EXPUNGE command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool ExpungeAsync(ExpungeAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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

        #region method IdleAsync

        #region class IdleAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.IdleAsync"/> asynchronous operation.
        /// </summary>
        public class IdleAsyncOP : IDisposable,IAsyncOP
        {
            private object                            m_pLock          = new object();
            private AsyncOP_State                     m_State          = AsyncOP_State.WaitingForStart;
            private Exception                         m_pException     = null;
            private IMAP_r_ServerStatus               m_pFinalResponse = null;
            private IMAP_Client                       m_pImapClient    = null;
            private bool                              m_RiseCompleted  = false;
            private EventHandler<EventArgs<IMAP_r_u>> m_pCallback      = null;
            private bool                              m_DoneSent       = false;

            /// <summary>
            /// Default constructor.
            /// </summary>             
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public IdleAsyncOP(EventHandler<EventArgs<IMAP_r_u>> callback)
            {
                m_pCallback = callback;
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
                m_pImapClient    = null;
                m_pFinalResponse = null;
                m_pCallback      = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Done

            /// <summary>
            /// Starts exiting IDLE state.
            /// </summary>
            /// <exception cref="InvalidOperationException">Is raised when this not in valid state.</exception>
            public void Done()
            {
                if(this.State != AsyncOP_State.Active){
                    throw new InvalidOperationException("Mehtod 'Done' can be called only AsyncOP_State.Active state.");
                }
                if(m_DoneSent){
                    throw new InvalidOperationException("Mehtod 'Done' already called, Done is in progress.");
                }
                m_DoneSent = true;

                byte[] cmdLine = Encoding.ASCII.GetBytes("DONE\r\n");

                // Log
                m_pImapClient.LogAddWrite(cmdLine.Length,"DONE");

                // Start command sending.
                m_pImapClient.TcpStream.BeginWrite(
                    cmdLine,
                    0,
                    cmdLine.Length,
                    delegate(IAsyncResult ar){
                        try{
                            m_pImapClient.TcpStream.EndWrite(ar);
                        }
                        catch(Exception x){
                            m_pException = x;
                            m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                            SetState(AsyncOP_State.Completed);
                        }
                    },
                    null
                );
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }
                                
                m_pImapClient = owner;
                        
                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 2177.3. IDLE Command.
                        Arguments:  none

                        Responses:  continuation data will be requested; the client sends
                                    the continuation data "DONE" to end the command

                        Result:     OK - IDLE completed after client sent "DONE"
                                    NO - failure: the server will not allow the IDLE
                                         command at this time
                                    BAD - command unknown or arguments invalid

                        The IDLE command may be used with any IMAP4 server implementation
                        that returns "IDLE" as one of the supported capabilities to the
                        CAPABILITY command.  If the server does not advertise the IDLE
                        capability, the client MUST NOT use the IDLE command and must poll
                        for mailbox updates.  In particular, the client MUST continue to be
                        able to accept unsolicited untagged responses to ANY command, as
                        specified in the base IMAP specification.

                        The IDLE command is sent from the client to the server when the
                        client is ready to accept unsolicited mailbox update messages.  The
                        server requests a response to the IDLE command using the continuation
                        ("+") response.  The IDLE command remains active until the client
                        responds to the continuation, and as long as an IDLE command is
                        active, the server is now free to send untagged EXISTS, EXPUNGE, and
                        other messages at any time.

                        The IDLE command is terminated by the receipt of a "DONE"
                        continuation from the client; such response satisfies the server's
                        continuation request.  At that point, the server MAY send any
                        remaining queued untagged responses and then MUST immediately send
                        the tagged response to the IDLE command and prepare to process other
                        commands. As in the base specification, the processing of any new
                        command may cause the sending of unsolicited untagged responses,
                        subject to the ambiguity limitations.  The client MUST NOT send a
                        command while the server is waiting for the DONE, since the server
                        will not be able to distinguish a command from a continuation.             
                     
                        Example:    C: A001 SELECT INBOX
                                    S: * FLAGS (Deleted Seen)
                                    S: * 3 EXISTS
                                    S: * 0 RECENT
                                    S: * OK [UIDVALIDITY 1]
                                    S: A001 OK SELECT completed
                                    C: A002 IDLE
                                    S: + idling
                                    ...time passes; new mail arrives...
                                    S: * 4 EXISTS
                                    C: DONE
                                    S: A002 OK IDLE terminated
                    */

                    m_pImapClient.m_pIdle = this;
                    
                    byte[] cmdLine    = Encoding.UTF8.GetBytes((m_pImapClient.m_CommandIndex++).ToString("d5") + " IDLE\r\n");
                    string cmdLineLog = Encoding.UTF8.GetString(cmdLine).TrimEnd();

                    SendCmdAndReadRespAsyncOP args = new SendCmdAndReadRespAsyncOP(cmdLine,cmdLineLog,m_pCallback);
                    args.CompletedAsync += delegate(object sender,EventArgs<SendCmdAndReadRespAsyncOP> e){
                        ProecessCmdResult(e.Value);
                    };
                    // Operation completed synchronously.
                    if(!m_pImapClient.SendCmdAndReadRespAsync(args)){
                        ProecessCmdResult(args);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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

            #region method ProecessCmdResult

            /// <summary>
            /// Processes command result.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ProecessCmdResult(SendCmdAndReadRespAsyncOP op)
            {
                try{
                    // Command send/receive failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    }
                    // Command send/receive succeeded.
                    else{ 
                        // IMAP server returned error response.
                        if(op.FinalResponse.IsError){
                            m_pException = new IMAP_ClientException(op.FinalResponse);
                            SetState(AsyncOP_State.Completed);
                        }
                        // IMAP server returned "+" continue response.
                        else if(op.FinalResponse.IsContinue){
                            ReadFinalResponseAsyncOP readFinalRespOP = new ReadFinalResponseAsyncOP(m_pCallback);
                            readFinalRespOP.CompletedAsync += delegate(object sender,EventArgs<ReadFinalResponseAsyncOP> e){
                                ProcessReadFinalResponseResult(e.Value);
                            };
                            // Operation completed synchronously.
                            if(!m_pImapClient.ReadFinalResponseAsync(readFinalRespOP)){
                                ProcessReadFinalResponseResult(readFinalRespOP);
                            }
                        }
                        // IMAP server returned success response. We should not get such response, but consider it as IDLE done.
                        else{
                            m_pFinalResponse = op.FinalResponse;
                            SetState(AsyncOP_State.Completed);
                        }
                    }                    
                }
                finally{
                    op.Dispose();
                }
            }

            #endregion

            #region method ProcessReadFinalResponseResult

            /// <summary>
            /// Processes IDLE final(final response after +) response reading result.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void ProcessReadFinalResponseResult(ReadFinalResponseAsyncOP op)
            {
                try{
                    // Command send/receive failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    }
                    // Command send/receive succeeded.
                    else{ 
                        // IMAP server returned error response.
                        if(op.FinalResponse.IsError){
                            m_pException = new IMAP_ClientException(op.FinalResponse);
                            SetState(AsyncOP_State.Completed);
                        }
                        // IMAP server returned success response.
                        else{
                            m_pImapClient.m_pIdle = null;
                            m_pFinalResponse = op.FinalResponse;
                            SetState(AsyncOP_State.Completed);
                        }
                    }                    
                }
                finally{
                    op.Dispose();
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
            /// Returns IMAP server final response.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_r_ServerStatus FinalResponse
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pFinalResponse; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<IdleAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<IdleAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes IDLE command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool IdleAsync(IdleAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }
            if(m_pIdle != null){
                throw new InvalidOperationException("Already idling !");
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


        #region method Capability

        /// <summary>
        /// Gets IMAP server capabilities.
        /// </summary>
        /// <returns>Returns CAPABILITIES responses.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public IMAP_r_u_Capability[] Capability()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }

            List<IMAP_r_u_Capability> retVal = new List<IMAP_r_u_Capability>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_Capability){
                    retVal.Add((IMAP_r_u_Capability)e.Value);
                }
            };

            using(CapabilityAsyncOP op = new CapabilityAsyncOP(callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<CapabilityAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.CapabilityAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region method CapabilityAsync

        #region class CapabilityAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.CapabilityAsync"/> asynchronous operation.
        /// </summary>
        public class CapabilityAsyncOP : CmdAsyncOP<CapabilityAsyncOP>
        {
            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public CapabilityAsyncOP(EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.1.1. CAPABILITY Command.
                    Arguments:  none

                    Responses:  REQUIRED untagged response: CAPABILITY

                    Result:     OK - capability completed
                                BAD - command unknown or arguments invalid

                    The CAPABILITY command requests a listing of capabilities that the
                    server supports.  The server MUST send a single untagged
                    CAPABILITY response with "IMAP4rev1" as one of the listed
                    capabilities before the (tagged) OK response.

                    A capability name which begins with "AUTH=" indicates that the
                    server supports that particular authentication mechanism.  All
                    such names are, by definition, part of this specification.  For
                    example, the authorization capability for an experimental
                    "blurdybloop" authenticator would be "AUTH=XBLURDYBLOOP" and not
                    "XAUTH=BLURDYBLOOP" or "XAUTH=XBLURDYBLOOP".

                    Other capability names refer to extensions, revisions, or
                    amendments to this specification.  See the documentation of the
                    CAPABILITY response for additional information.  No capabilities,
                    beyond the base IMAP4rev1 set defined in this specification, are
                    enabled without explicit client action to invoke the capability.

                    Client and server implementations MUST implement the STARTTLS,
                    LOGINDISABLED, and AUTH=PLAIN (described in [IMAP-TLS])
                    capabilities.  See the Security Considerations section for
                    important information.

                    See the section entitled "Client Commands -
                    Experimental/Expansion" for information about the form of site or
                    implementation-specific capabilities.

                    Example:    C: abcd CAPABILITY
                                S: * CAPABILITY IMAP4rev1 STARTTLS AUTH=GSSAPI LOGINDISABLED
                                S: abcd OK CAPABILITY completed
                                C: efgh STARTTLS
                                S: efgh OK STARTLS completed
                                   <TLS negotiation, further commands are under [TLS] layer>
                                C: ijkl CAPABILITY
                                S: * CAPABILITY IMAP4rev1 AUTH=GSSAPI AUTH=PLAIN
                                S: ijkl OK CAPABILITY completed
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " CAPABILITY" + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes CAPABILITY command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool CapabilityAsync(CapabilityAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }          
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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
        /// Sends NOOP command to IMAP server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        public void Noop()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }

            using(NoopAsyncOP op = new NoopAsyncOP(null)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<NoopAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.NoopAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

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
        /// This class represents <see cref="IMAP_Client.NoopAsync"/> asynchronous operation.
        /// </summary>
        public class NoopAsyncOP : CmdAsyncOP<NoopAsyncOP>
        {
            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public NoopAsyncOP(EventHandler<EventArgs<IMAP_r_u>> callback) : base(callback)
            {
            }


            #region override method OnInitCmdLine

            /// <summary>
            /// Is called when we need to init command line info.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            protected override void OnInitCmdLine(IMAP_Client imap)
            {
                /* RFC 3501 6.1.2. NOOP Command.
                    Arguments:  none

                    Responses:  no specific responses for this command (but see below)

                    Result:     OK - noop completed
                               BAD - command unknown or arguments invalid

                    The NOOP command always succeeds.  It does nothing.

                    Since any command can return a status update as untagged data, the
                    NOOP command can be used as a periodic poll for new messages or
                    message status updates during a period of inactivity (this is the
                    preferred method to do this).  The NOOP command can also be used
                    to reset any inactivity autologout timer on the server.            
                */

                byte[] cmdLine = Encoding.UTF8.GetBytes((imap.m_CommandIndex++).ToString("d5") + " NOOP" + "\r\n");
                this.CmdLines.Add(new CmdLine(cmdLine,Encoding.UTF8.GetString(cmdLine).TrimEnd()));
            }

            #endregion
        }

        #endregion

        /// <summary>
        /// Executes NOOP command.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="CmdAsyncOP{T}.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public bool NoopAsync(NoopAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }          
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
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
            // Read IMAP server greeting.
            ReadResponseAsyncOP op = new ReadResponseAsyncOP();
            op.CompletedAsync += delegate(object sender,EventArgs<IMAP_Client.ReadResponseAsyncOP> e){
                ProcessGreetingResult(op,callback);
            };
            // Operation completed synchronously.
            if(!ReadResponseAsync(op)){
                ProcessGreetingResult(op,callback);                
            }
        }

        #endregion

        #region method ProcessGreetingResult

        /// <summary>
        /// Processes IMAP server greeting reading result.
        /// </summary>
        /// <param name="op">Reading operation.</param>
        /// <param name="connectCallback">Callback to be called to complete connect operation.</param>
        private void ProcessGreetingResult(ReadResponseAsyncOP op,CompleteConnectCallback connectCallback)
        {
            Exception error = null;
            
            try{
                // Operation failed.
                if(op.Error != null){
                    error = op.Error;
                }
                // Operation succeeded.
                else{
                    if(op.Response is IMAP_r_u_ServerStatus){
                        IMAP_r_u_ServerStatus statusResp = (IMAP_r_u_ServerStatus)op.Response;

                        // IMAP server rejected connection.
                        if(statusResp.IsError){
                            error = new IMAP_ClientException(statusResp.ResponseCode,statusResp.ResponseText);
                        }
                        else{
                            m_GreetingText = statusResp.ResponseText;
                        }
                    }
                    else{
                        error = new Exception("Unexpected IMAP server greeting response: " + op.Response.ToString());
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


        #region method SendCmdAndReadRespAsync

        #region class SendCmdAndReadRespAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.SendCmdAndReadRespAsync"/> asynchronous operation.
        /// </summary>
        private class SendCmdAndReadRespAsyncOP : IDisposable,IAsyncOP
        {
            private object                            m_pLock          = new object();
            private AsyncOP_State                     m_State          = AsyncOP_State.WaitingForStart;
            private Exception                         m_pException     = null;
            private IMAP_r_ServerStatus               m_pFinalResponse = null;
            private IMAP_Client                       m_pImapClient    = null;
            private bool                              m_RiseCompleted  = false;
            private Queue<CmdLine>                    m_pCmdLines      = null;
            private EventHandler<EventArgs<IMAP_r_u>> m_pCallback      = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="cmdLine">IMAP command line.</param>
            /// <param name="cmdLineLogText">IMAP command line log text.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>cmdLine</b> or <b>cmdLineLogText</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public SendCmdAndReadRespAsyncOP(byte[] cmdLine,string cmdLineLogText,EventHandler<EventArgs<IMAP_r_u>> callback)
            {
                if(cmdLine == null){
                    throw new ArgumentNullException("cmdLine");
                }
                if(cmdLine.Length < 1){
                    throw new ArgumentException("Argument 'cmdLine' value must be specified.","cmdLine");
                }
                if(cmdLineLogText == null){
                    throw new ArgumentNullException("cmdLineLogText");
                }

                m_pCallback = callback;

                m_pCmdLines = new Queue<CmdLine>();
                m_pCmdLines.Enqueue(new CmdLine(cmdLine,cmdLineLogText));
            }

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="cmdLines">IMAP command lines.</param>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>cmdLines</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public SendCmdAndReadRespAsyncOP(CmdLine[] cmdLines,EventHandler<EventArgs<IMAP_r_u>> callback)
            {
                if(cmdLines == null){
                    throw new ArgumentNullException("cmdLines");
                }

                m_pCmdLines = new Queue<CmdLine>(cmdLines);
                m_pCallback = callback;                
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
                m_pImapClient    = null;
                m_pFinalResponse = null;                
                m_pCmdLines      = null;
                m_pCallback      = null;

                this.CompletedAsync = null;
            }

            #endregion       


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                
                m_pImapClient = owner;
                        
                SetState(AsyncOP_State.Active);

                SendCmdLine();

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

            #region method SendCmdLine

            /// <summary>
            /// Sends next command line to IMAP server.
            /// </summary>
            private void SendCmdLine()
            {
                try{                    
                    // Check that we have next command line.
                    if(m_pCmdLines.Count == 0){
                        throw new Exception("Internal error: No next IMAP command line.");
                    }

                    CmdLine cmdLine = m_pCmdLines.Dequeue();

                    // Log
                    m_pImapClient.LogAddWrite(cmdLine.Data.Length,cmdLine.LogText);

                    // Start command sending.
                    m_pImapClient.TcpStream.BeginWrite(cmdLine.Data,0,cmdLine.Data.Length,this.ProcessCmdLineSendResult,null);                    
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method ProcessCmdLineSendResult

            /// <summary>
            /// Processes command line sending result.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void ProcessCmdLineSendResult(IAsyncResult ar)
            {
                try{
                    m_pImapClient.TcpStream.EndWrite(ar);

                    ReadFinalResponseAsyncOP args = new ReadFinalResponseAsyncOP(m_pCallback);
                    args.CompletedAsync += delegate(object sender,EventArgs<ReadFinalResponseAsyncOP> e){
                        try{
                            // Command failed.
                            if(args.Error != null){
                                m_pException = e.Value.Error;
                                SetState(AsyncOP_State.Completed);
                            }
                            else{
                                // We must send next command line of multi-line command line.
                                // Send only if we have any available, otherwise return reponse to user.
                                if(args.FinalResponse.IsContinue && m_pCmdLines.Count > 0){
                                    SendCmdLine();
                                }
                                else{
                                    m_pFinalResponse = (IMAP_r_ServerStatus)args.FinalResponse;
                                    SetState(AsyncOP_State.Completed);
                                }
                            }                            
                        }
                        finally{
                            args.Dispose();
                        }
                    };
                    // Read final response completed synchronously.
                    if(!m_pImapClient.ReadFinalResponseAsync(args)){
                        try{
                            // Fetch failed.
                            if(args.Error != null){
                                m_pException = args.Error;
                            }
                            else{
                                m_pFinalResponse = (IMAP_r_ServerStatus)args.FinalResponse;
                            }

                            SetState(AsyncOP_State.Completed);
                        }
                        finally{
                            args.Dispose();
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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
            /// Returns IMAP server final response.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_r_ServerStatus FinalResponse
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pFinalResponse; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<SendCmdAndReadRespAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<SendCmdAndReadRespAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Sends IMAP command to server and reads server responses.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="SendCmdAndReadRespAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any oth the arguments has invalid value.</exception>
        private bool SendCmdAndReadRespAsync(SendCmdAndReadRespAsyncOP op)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }
            if(op.State != AsyncOP_State.WaitingForStart){
                throw new ArgumentException("Invalid argument 'op' state, 'op' must be in 'AsyncOP_State.WaitingForStart' state.","op");
            }

            return op.Start(this);
        }

        #endregion

        #region method ReadResponseAsync

        #region class ReadResponseAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.ReadResponseAsync"/> asynchronous operation.
        /// </summary>
        private class ReadResponseAsyncOP : IDisposable,IAsyncOP
        {
            private object                      m_pLock         = new object();
            private AsyncOP_State               m_State         = AsyncOP_State.WaitingForStart;
            private Exception                   m_pException    = null;
            private IMAP_r                      m_pResponse     = null;
            private IMAP_Client                 m_pImapClient   = null;
            private bool                        m_RiseCompleted = false;
            private SmartStream.ReadLineAsyncOP m_pReadLineOP   = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public ReadResponseAsyncOP()
            {
                m_pReadLineOP = new SmartStream.ReadLineAsyncOP(new byte[64000],SizeExceededAction.JunkAndThrowException);
                m_pReadLineOP.Completed += new EventHandler<EventArgs<SmartStream.ReadLineAsyncOP>>(m_pReadLineOP_Completed);
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
                m_pImapClient = null;
                m_pResponse   = null;
                if(m_pReadLineOP != null){
                    m_pReadLineOP.Dispose();
                }
                m_pReadLineOP = null;

                this.CompletedAsync = null;
            }

            #endregion
                                    

            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pImapClient = owner;

                SetState(AsyncOP_State.Active);

                try{
                    // Read line completed synchronously.
                    if(owner.TcpStream.ReadLine(m_pReadLineOP,true)){
                        ReadLineCompleted(m_pReadLineOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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

            #region method Reuse

            /// <summary>
            /// Prepares this class for reuse.
            /// </summary>
            /// <exception cref="InvalidOperationException">Is raised when this is not valid state.</exception>
            public void Reuse()
            {
                if(m_State != AsyncOP_State.Completed){
                    throw new InvalidOperationException("Reuse is valid only in Completed state.");
                }

                m_State         = AsyncOP_State.WaitingForStart;
                m_pException    = null;
                m_pResponse     = null;
                m_pImapClient   = null;
                m_RiseCompleted = false;
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

            #region method m_pReadLineOP_Completed

            /// <summary>
            /// Is called when TcpStream.ReadLine has completed.
            /// </summary>
            /// <param name="sender">Sender.</param>
            /// <param name="e">Event data.</param>
            private void m_pReadLineOP_Completed(object sender,EventArgs<SmartStream.ReadLineAsyncOP> e)
            {
                try{
                    ReadLineCompleted(m_pReadLineOP);
                }
                catch(Exception x){
                    m_pException = x;
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method ReadLineCompleted

            /// <summary>
            /// Is called when read line has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void ReadLineCompleted(SmartStream.ReadLineAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    // Line reading failed, we are done.
                    if(op.Error != null){
                        m_pException = op.Error;
                    }
                    // Remote host shut down socket.                        
                    else if(op.BytesInBuffer == 0){
                        m_pException = new IOException("The remote host shut-down socket.");
                    }
                    // Line reading succeeded.
                    else{
                        string responseLine = op.LineUtf8;

                        // Log.
                        m_pImapClient.LogAddRead(op.BytesInBuffer,responseLine);

                        // Untagged response.
                        if(responseLine.StartsWith("*")){
                            string[] parts = responseLine.Split(new char[]{' '},4);
                            string   word  = responseLine.Split(' ')[1];

                            #region Untagged status responses. RFC 3501 7.1.

                            // OK,NO,BAD,PREAUTH,BYE

                            if(word.Equals("OK",StringComparison.InvariantCultureIgnoreCase)){
                                IMAP_r_u_ServerStatus statusResponse = IMAP_r_u_ServerStatus.Parse(responseLine);
                                m_pResponse = statusResponse;

                                // Process optional response-codes(7.2). ALERT,BADCHARSET,CAPABILITY,PARSE,PERMANENTFLAGS,READ-ONLY,
                                // READ-WRITE,TRYCREATE,UIDNEXT,UIDVALIDITY,UNSEEN                                
                                if(statusResponse.OptionalResponse != null){
                                    if(statusResponse.OptionalResponse is IMAP_t_orc_PermanentFlags){
                                        if(m_pImapClient.SelectedFolder != null){
                                            m_pImapClient.SelectedFolder.SetPermanentFlags(((IMAP_t_orc_PermanentFlags)statusResponse.OptionalResponse).Flags);
                                        }
                                    }
                                    else if(statusResponse.OptionalResponse is IMAP_t_orc_ReadOnly){
                                        if(m_pImapClient.SelectedFolder != null){
                                            m_pImapClient.SelectedFolder.SetReadOnly(true);
                                        }
                                    }
                                    else if(statusResponse.OptionalResponse is IMAP_t_orc_ReadWrite){
                                        if(m_pImapClient.SelectedFolder != null){
                                            m_pImapClient.SelectedFolder.SetReadOnly(true);
                                        }
                                    }
                                    else if(statusResponse.OptionalResponse is IMAP_t_orc_UidNext){
                                        if(m_pImapClient.SelectedFolder != null){
                                            m_pImapClient.SelectedFolder.SetUidNext(((IMAP_t_orc_UidNext)statusResponse.OptionalResponse).UidNext);
                                        }
                                    }
                                    else if(statusResponse.OptionalResponse is IMAP_t_orc_UidValidity){
                                        if(m_pImapClient.SelectedFolder != null){
                                            m_pImapClient.SelectedFolder.SetUidValidity(((IMAP_t_orc_UidValidity)statusResponse.OptionalResponse).Uid);
                                        }
                                    }
                                    else if(statusResponse.OptionalResponse is IMAP_t_orc_Unseen){
                                        if(m_pImapClient.SelectedFolder != null){
                                            m_pImapClient.SelectedFolder.SetFirstUnseen(((IMAP_t_orc_Unseen)statusResponse.OptionalResponse).SeqNo);
                                        }
                                    }
                                    // We don't care about other response codes.                            
                                }

                                m_pImapClient.OnUntaggedStatusResponse((IMAP_r_u)m_pResponse);
                            }
                            else if(word.Equals("NO",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_ServerStatus.Parse(responseLine);

                                m_pImapClient.OnUntaggedStatusResponse((IMAP_r_u)m_pResponse);
                            }
                            else if(word.Equals("BAD",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_ServerStatus.Parse(responseLine);

                                m_pImapClient.OnUntaggedStatusResponse((IMAP_r_u)m_pResponse);
                            }
                            else if(word.Equals("PREAUTH",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_ServerStatus.Parse(responseLine);

                                m_pImapClient.OnUntaggedStatusResponse((IMAP_r_u)m_pResponse);
                            }
                            else if(word.Equals("BYE",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_ServerStatus.Parse(responseLine);

                                m_pImapClient.OnUntaggedStatusResponse((IMAP_r_u)m_pResponse);
                            }

                            #endregion

                            #region Untagged server and mailbox status. RFC 3501 7.2.

                            // CAPABILITY,LIST,LSUB,STATUS,SEARCH,FLAGS

                            #region CAPABILITY

                            else if(word.Equals("CAPABILITY",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_Capability.Parse(responseLine); 
                               
                                // Cache IMAP server capabilities.
                                m_pImapClient.m_pCapabilities = new List<string>();
                                m_pImapClient.m_pCapabilities.AddRange(((IMAP_r_u_Capability)m_pResponse).Capabilities);
                            }

                            #endregion

                            #region LIST

                            else if(word.Equals("LIST",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_List.Parse(responseLine);
                            }

                            #endregion

                            #region LSUB

                            else if(word.Equals("LSUB",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_LSub.Parse(responseLine);
                            }

                            #endregion

                            #region STATUS

                            else if(word.Equals("STATUS",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_Status.Parse(responseLine);
                            }

                            #endregion

                            #region SEARCH

                            else if(word.Equals("SEARCH",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_Search.Parse(responseLine);
                            }

                            #endregion

                            #region FLAGS

                            else if(word.Equals("FLAGS",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_Flags.Parse(responseLine);

                                if(m_pImapClient.m_pSelectedFolder != null){
                                    m_pImapClient.m_pSelectedFolder.SetFlags(((IMAP_r_u_Flags)m_pResponse).Flags);
                                }
                            }

                            #endregion

                            #endregion

                            #region Untagged mailbox size. RFC 3501 7.3.

                            // EXISTS,RECENT

                            else if(Net_Utils.IsInteger(word) && parts[2].Equals("EXISTS",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_Exists.Parse(responseLine);

                                if(m_pImapClient.m_pSelectedFolder != null){
                                    m_pImapClient.m_pSelectedFolder.SetMessagesCount(((IMAP_r_u_Exists)m_pResponse).MessageCount);
                                }
                            }
                            else if(Net_Utils.IsInteger(word) && parts[2].Equals("RECENT",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_Recent.Parse(responseLine);

                                if(m_pImapClient.m_pSelectedFolder != null){
                                    m_pImapClient.m_pSelectedFolder.SetRecentMessagesCount(((IMAP_r_u_Recent)m_pResponse).MessageCount);
                                }
                            }
                                                
                            #endregion

                            #region Untagged message status. RFC 3501 7.4.

                            // EXPUNGE,FETCH

                            else if(Net_Utils.IsInteger(word) && parts[2].Equals("EXPUNGE",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_Expunge.Parse(responseLine);
                                m_pImapClient.OnMessageExpunged((IMAP_r_u_Expunge)m_pResponse);
                            }
                            else if(Net_Utils.IsInteger(word) && parts[2].Equals("FETCH",StringComparison.InvariantCultureIgnoreCase)){
                                // FETCH parsing may complete asynchornously, the method FetchParsingCompleted is called when parsing has completed.

                                IMAP_r_u_Fetch fetch = new IMAP_r_u_Fetch(1);                                
                                m_pResponse = fetch;
                                fetch.ParseAsync(m_pImapClient,responseLine,this.FetchParsingCompleted);

                                // Return skips SetState(AsyncOP_State.Completed), it will be called when fetch has completed.
                                return;
                            }

                            #endregion

                            #region Untagged acl realted. RFC 4314.

                            else if(word.Equals("ACL",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_Acl.Parse(responseLine);
                            }
                            else if(word.Equals("LISTRIGHTS",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_ListRights.Parse(responseLine);
                            }
                            else if(word.Equals("MYRIGHTS",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_MyRights.Parse(responseLine);
                            }

                            #endregion

                            #region Untagged quota related. RFC 2087.

                            else if(word.Equals("QUOTA",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_Quota.Parse(responseLine);
                            }
                            else if(word.Equals("QUOTAROOT",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_QuotaRoot.Parse(responseLine);
                            }

                            #endregion

                            #region Untagged namespace related. RFC 2342.

                            else if(word.Equals("NAMESPACE",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_Namespace.Parse(responseLine);
                            }

                            #endregion

                            #region Untagged enable related. RFC 5161.

                            else if(word.Equals("ENABLED",StringComparison.InvariantCultureIgnoreCase)){
                                m_pResponse = IMAP_r_u_Enable.Parse(responseLine);
                            }

                            #endregion
                            
                            // Raise event 'UntaggedResponse'.
                            m_pImapClient.OnUntaggedResponse((IMAP_r_u)m_pResponse);
                        }
                        // Command continuation response.
                        else if(responseLine.StartsWith("+")){
                            m_pResponse = IMAP_r_ServerStatus.Parse(responseLine);
                        }
                        // Completion status response.
                        else{
                            // Command response reading has completed.
                            m_pResponse = IMAP_r_ServerStatus.Parse(responseLine);
                        }
                    }                    
                }
                catch(Exception x){
                    m_pException = x;
                }

                SetState(AsyncOP_State.Completed);
            }

            #endregion


            #region method FetchParsingCompleted

            /// <summary>
            /// This method is called when FETCH parsing has completed.
            /// </summary>
            /// <param name="sender">Sender.</param>
            /// <param name="e">Event data.</param>
            private void FetchParsingCompleted(object sender,EventArgs<Exception> e)
            {             
                try{
                    // Fetch parsing failed.
                    if(e.Value != null){
                        m_pException = e.Value;
                    }

                    // Raise event 'UntaggedResponse'.
                    m_pImapClient.OnUntaggedResponse((IMAP_r_u)m_pResponse);
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

            /// <summary>
            /// Returns IMAP server response.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_r Response
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pResponse; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<ReadResponseAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<ReadResponseAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts reading IMAP server response.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="ReadResponseAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any oth the arguments has invalid value.</exception>
        private bool ReadResponseAsync(ReadResponseAsyncOP op)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }
            if(op.State != AsyncOP_State.WaitingForStart){
                throw new ArgumentException("Invalid argument 'op' state, 'op' must be in 'AsyncOP_State.WaitingForStart' state.","op");
            }

            return op.Start(this);
        }

        #endregion
 
        #region method ReadFinalResponseAsync

        #region class ReadFinalResponseAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.ReadFinalResponseAsyncOP"/> asynchronous operation.
        /// </summary>
        private class ReadFinalResponseAsyncOP : IDisposable,IAsyncOP
        {
            private object                            m_pLock          = new object();
            private AsyncOP_State                     m_State          = AsyncOP_State.WaitingForStart;
            private Exception                         m_pException     = null;
            private IMAP_r_ServerStatus               m_pFinalResponse = null;
            private IMAP_Client                       m_pImapClient    = null;
            private bool                              m_RiseCompleted  = false;
            private EventHandler<EventArgs<IMAP_r_u>> m_pCallback      = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="callback">Optional callback to be called for each received untagged response.</param>
            public ReadFinalResponseAsyncOP(EventHandler<EventArgs<IMAP_r_u>> callback)
            {
                m_pCallback = callback;
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
                m_pImapClient    = null;
                m_pFinalResponse = null;
                m_pCallback      = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                
                m_pImapClient = owner;
                
                SetState(AsyncOP_State.Active);

                try{                    
                    ReadResponseAsyncOP args = new ReadResponseAsyncOP();
                    args.CompletedAsync += delegate(object sender,EventArgs<ReadResponseAsyncOP> e){
                        try{
                            ResponseReadingCompleted(e.Value);
                            args.Reuse();

                            // Read responses while we get final response.
                            while(m_State == AsyncOP_State.Active && !m_pImapClient.ReadResponseAsync(args)){
                                ResponseReadingCompleted(args);
                                args.Reuse();
                            }
                        }
                        catch(Exception x){
                            m_pException = x;
                            m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                            SetState(AsyncOP_State.Completed);
                        }
                    };
                    // Read responses while reading completes synchronously.
                    while(m_State == AsyncOP_State.Active && !m_pImapClient.ReadResponseAsync(args)){
                        ResponseReadingCompleted(args);
                        args.Reuse();
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pImapClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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

            #region method ResponseReadingCompleted

            /// <summary>
            /// Is called when IMAP server response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ReadResponseAsyncOP">Is raiswed when <b>op</b> is null reference.</exception>
            private void ResponseReadingCompleted(ReadResponseAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    // Response reading failed.
                    if(op.Error != null){
                        m_pException = op.Error;
                        SetState(AsyncOP_State.Completed);
                    }
                    else{
                        // We are done, we got final response.
                        if(op.Response is IMAP_r_ServerStatus){
                            m_pFinalResponse = (IMAP_r_ServerStatus)op.Response;
                            SetState(AsyncOP_State.Completed);
                        }
                        else{
                            if(m_pCallback != null){
                                m_pCallback(this,new EventArgs<IMAP_r_u>((IMAP_r_u)op.Response));
                            }
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
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
            /// Returns IMAP server final response.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public IMAP_r_ServerStatus FinalResponse
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'Response' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pFinalResponse; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<ReadFinalResponseAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<ReadFinalResponseAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts reading IMAP server final(OK/BAD/NO/+) response.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="ReadFinalResponseAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any oth the arguments has invalid value.</exception>
        private bool ReadFinalResponseAsync(ReadFinalResponseAsyncOP op)
        {            
            if(op == null){
                throw new ArgumentNullException("op");
            }
            if(op.State != AsyncOP_State.WaitingForStart){
                throw new ArgumentException("Invalid argument 'op' state, 'op' must be in 'AsyncOP_State.WaitingForStart' state.","op");
            }
                        
            return op.Start(this);
        }

        #endregion
                
        #region method ReadStringLiteralAsync

        #region class ReadStringLiteralAsyncOP

        /// <summary>
        /// This class represents <see cref="IMAP_Client.ReadStringLiteralAsync"/> asynchronous operation.
        /// </summary>
        internal class ReadStringLiteralAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock         = new object();
            private AsyncOP_State m_State         = AsyncOP_State.WaitingForStart;
            private Exception     m_pException    = null;
            private Stream        m_pStream       = null;
            private int           m_LiteralSize   = 0;
            private IMAP_Client   m_pImapClient   = null;
            private bool          m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="stream">Store stream.</param>
            /// <param name="literalSize">String literal size in bytes.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
            public ReadStringLiteralAsyncOP(Stream stream,int literalSize)
            {
                if(stream == null){
                    throw new ArgumentNullException("stream");
                }

                m_pStream     = stream;
                m_LiteralSize = literalSize;
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
                m_pImapClient = null;
                m_pStream   = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner IMAP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            public bool Start(IMAP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pImapClient = owner;

                SetState(AsyncOP_State.Active);

                owner.TcpStream.BeginReadFixedCount(m_pStream,m_LiteralSize,this.ReadingCompleted,null);

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

            #region method ReadingCompleted

            /// <summary>
            /// This method is called when string-literal reading has completed.
            /// </summary>
            /// <param name="result">Asynchronous result.</param>
            private void ReadingCompleted(IAsyncResult result)
            {
                try{
                    m_pImapClient.TcpStream.EndReadFixedCount(result);

                    // Log
                    m_pImapClient.LogAddRead(m_LiteralSize,"Readed string-literal " + m_LiteralSize.ToString() + " bytes.");
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

            /// <summary>
            /// Gets literal stream.
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
                        throw new InvalidOperationException("Property 'Error' is accessible only in 'AsyncOP_State.Completed' state.");
                    }

                    return m_pStream; 
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<ReadStringLiteralAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<ReadStringLiteralAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts reading string-literal from IMAP server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="ReadStringLiteralAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any oth the arguments has invalid value.</exception>
        internal bool ReadStringLiteralAsync(ReadStringLiteralAsyncOP op)
        {
            if(op == null){
                throw new ArgumentNullException("op");
            }
            if(op.State != AsyncOP_State.WaitingForStart){
                throw new ArgumentException("Invalid argument 'op' state, 'op' must be in 'AsyncOP_State.WaitingForStart' state.","op");
            }

            return op.Start(this);
        }

        #endregion

        #region method SupportsCapability

        /// <summary>
        /// Gets if IMAP server supports the specified capability.
        /// </summary>
        /// <param name="capability">IMAP capability.</param>
        /// <returns>Return true if IMAP server supports the specified capability.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>capability</b> is null reference.</exception>
        private bool SupportsCapability(string capability)
        {
            if(capability == null){
                throw new ArgumentNullException("capability");
            }

            if(m_pCapabilities == null){
                return false;
            }
            else{
                foreach(string c in m_pCapabilities){
                    if(string.Equals(c,capability,StringComparison.InvariantCultureIgnoreCase)){
                        return true;
                    }
                }
            }

            return false;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets session authenticated user identity, returns null if not authenticated.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and IMAP client is not connected.</exception>
        public override GenericIdentity AuthenticatedUserIdentity
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
				    throw new InvalidOperationException("You must connect first.");
			    }

                return m_pAuthenticatedUser; 
            }
        }

        /// <summary>
        /// Get IMAP server greeting text.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and IMAP client is not connected.</exception>
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
        /// Get IMAP server(CAPABILITY command cached) supported capabilities.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and IMAP client is not connected.</exception>
        public string[] Capabilities
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
				    throw new InvalidOperationException("You must connect first.");
			    }

                if(m_pCapabilities == null){
                    return new string[0];
                }

                return m_pCapabilities.ToArray(); 
            }
        }

        /// <summary>
        /// Gets IMAP server folder separator.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and IMAP client is not connected.</exception>
        public char FolderSeparator
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
				    throw new InvalidOperationException("You must connect first.");
			    }

                // Empty folder name forces server to return hierarchy delimiter.
                IMAP_r_u_List[] retVal = GetFolders("");
                if(retVal.Length == 0){
                    throw new Exception("Unexpected result: IMAP server didn't return LIST response for [... LIST \"\" \"\"].");
                }
                else{
                    return retVal[0].HierarchyDelimiter;
                }
            }
        }

        /// <summary>
        /// Gets selected folder. Returns null if no folder selected.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and IMAP client is not connected.</exception>
        public IMAP_Client_SelectedFolder SelectedFolder
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
				    throw new InvalidOperationException("You must connect first.");
			    }

                return m_pSelectedFolder; 
            }
        }

        /// <summary>
        /// Gets active IDLE operation or null if no active IDLE operation.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and IMAP client is not connected.</exception>
        public IdleAsyncOP IdleOP
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
				    throw new InvalidOperationException("You must connect first.");
			    }

                return m_pIdle; 
            }
        }

        #endregion

        #region Events implementation
        
        /// <summary>
        /// This event is raised when IMAP server sends untagged status response.
        /// </summary>
        public event EventHandler<EventArgs<IMAP_r_u>> UntaggedStatusResponse = null;

        #region method OnUntaggedStatusResponse

        /// <summary>
        /// Raises <b>UntaggedStatusResponse</b> event.
        /// </summary>
        /// <param name="response">Untagged response.</param>
        private void OnUntaggedStatusResponse(IMAP_r_u response)
        {
            if(this.UntaggedStatusResponse != null){
                this.UntaggedStatusResponse(this,new EventArgs<IMAP_r_u>(response));
            }
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP server sends any untagged response.
        /// </summary>
        /// <remarks>NOTE: This event may raised from thread pool thread, so UI event handlers need to use Invoke.</remarks>
        public event EventHandler<EventArgs<IMAP_r_u>> UntaggedResponse = null;

        #region method OnUntaggedResponse

        /// <summary>
        /// Raises <b>UntaggedResponse</b> event.
        /// </summary>
        /// <param name="response">Untagged IMAP server response.</param>
        private void OnUntaggedResponse(IMAP_r_u response)
        {
            if(this.UntaggedResponse != null){
                this.UntaggedResponse(this,new EventArgs<IMAP_r_u>(response));
            }
        }

        #endregion
                
        /// <summary>
        /// This event is raised when IMAP server expunges message and sends EXPUNGE response.
        /// </summary>
        public event EventHandler<EventArgs<IMAP_r_u_Expunge>> MessageExpunged = null;

        #region method OnMessageExpunged

        /// <summary>
        /// Raises <b>MessageExpunged</b> event.
        /// </summary>
        /// <param name="response">Expunge response.</param>
        private void OnMessageExpunged(IMAP_r_u_Expunge response)
        {
            if(this.MessageExpunged != null){
                this.MessageExpunged(this,new EventArgs<IMAP_r_u_Expunge>(response));
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when FETCH response parsing allows to specify stream where to store binary data.
        /// </summary>
        /// <remarks>Thhis event is raised for FETCH BODY[]/RFC822/RFC822.HEADER/RFC822.TEXT data-items.</remarks>
        public event EventHandler<IMAP_Client_e_FetchGetStoreStream> FetchGetStoreStream = null;

        #region method OnFetchGetStoreStream

        /// <summary>
        /// Raises <b>FetchGetStoreStream</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        internal void OnFetchGetStoreStream(IMAP_Client_e_FetchGetStoreStream e)
        {
            if(this.FetchGetStoreStream != null){
                this.FetchGetStoreStream(this,e);
            }
        }

        #endregion

        #endregion


        //--- OBSOLETE ------------------------         

        #region method ReadResponse

        /// <summary>
        /// Reads IMAP server responses.
        /// </summary>
        /// <param name="folderInfo">Folder info where to store folder related data.
        /// This applies to SELECT or EXAMINE command only. This value can be null.
        /// </param>
        /// <param name="capability">List wehere to store CAPABILITY command result. This value can be null.</param>
        /// <param name="search">List wehere to store SEARCH command result. This value can be null.</param>
        /// <param name="list">List where to store LIST command result. This value can be null.</param>
        /// <param name="lsub">List where to store LSUB command result. This value can be null.</param>
        /// <param name="acl">List where to store ACL command result. This value can be null.</param>
        /// <param name="myRights">List where to store MYRIGHTS command result. This value can be null.</param>
        /// <param name="listRights">List where to store LISTRIGHTS command result. This value can be null.</param>
        /// <param name="status">List where to store STATUS command result. This value can be null.</param>
        /// <param name="quota">List where to store QUOTA command result. This value can be null.</param>
        /// <param name="quotaRoot">List where to store QUOTAROOT command result. This value can be null.</param>
        /// <param name="nspace">List where to store NAMESPACE command result. This value can be null.</param>
        /// <param name="fetchHandler">Fetch data-items handler.</param>
        /// <param name="enable">List where to store ENABLE command result. This value can be null.</param>
        /// <returns>Returns command completion status response.</returns>
        [Obsolete("deprecated")]
        private IMAP_r_ServerStatus ReadResponse(List<IMAP_r_u_Capability> capability,IMAP_Client_SelectedFolder folderInfo,List<int> search,List<IMAP_r_u_List> list,List<IMAP_r_u_LSub> lsub,List<IMAP_r_u_Acl> acl,List<IMAP_Response_MyRights> myRights,List<IMAP_r_u_ListRights> listRights,List<IMAP_r_u_Status> status,List<IMAP_r_u_Quota> quota,List<IMAP_r_u_QuotaRoot> quotaRoot,List<IMAP_r_u_Namespace> nspace,IMAP_Client_FetchHandler fetchHandler,List<IMAP_r_u_Enable> enable)
        {
            /* RFC 3501 2.2.2.
                The protocol receiver of an IMAP4rev1 client reads a response line
                from the server.  It then takes action on the response based upon the
                first token of the response, which can be a tag, a "*", or a "+".
             
                The client MUST be prepared to accept any response at all times.
            */
                        
            SmartStream.ReadLineAsyncOP args = new SmartStream.ReadLineAsyncOP(new byte[32000],SizeExceededAction.JunkAndThrowException);

            while(true){
                // Read response line.
                this.TcpStream.ReadLine(args,false);
                if(args.Error != null){
                    throw args.Error;
                }
                string responseLine = args.LineUtf8;

                // Log
                LogAddRead(args.BytesInBuffer,responseLine);

                // Untagged response.
                if(responseLine.StartsWith("*")){
                    string[] parts = responseLine.Split(new char[]{' '},4);
                    string   word  = responseLine.Split(' ')[1];

                    #region Untagged status responses. RFC 3501 7.1.

                    // OK,NO,BAD,PREAUTH,BYE

                    if(word.Equals("OK",StringComparison.InvariantCultureIgnoreCase)){
                        IMAP_r_u_ServerStatus response = IMAP_r_u_ServerStatus.Parse(responseLine);

                        // Process optional response-codes(7.2). ALERT,BADCHARSET,CAPABILITY,PARSE,PERMANENTFLAGS,READ-ONLY,
                        // READ-WRITE,TRYCREATE,UIDNEXT,UIDVALIDITY,UNSEEN

                        if(!string.IsNullOrEmpty(response.OptionalResponseCode)){
                            if(response.OptionalResponseCode.Equals("PERMANENTFLAGS",StringComparison.InvariantCultureIgnoreCase)){
                                if(folderInfo != null){
                                    StringReader r = new StringReader(response.OptionalResponseArgs);

                                    folderInfo.SetPermanentFlags(r.ReadParenthesized().Split(' '));
                                }
                            }
                            else if(response.OptionalResponseCode.Equals("READ-ONLY",StringComparison.InvariantCultureIgnoreCase)){
                                if(folderInfo != null){
                                    folderInfo.SetReadOnly(true);
                                }
                            }
                            else if(response.OptionalResponseCode.Equals("READ-WRITE",StringComparison.InvariantCultureIgnoreCase)){
                                if(folderInfo != null){
                                    folderInfo.SetReadOnly(true);
                                }
                            }
                            else if(response.OptionalResponseCode.Equals("UIDNEXT",StringComparison.InvariantCultureIgnoreCase)){
                                if(folderInfo != null){
                                    folderInfo.SetUidNext(Convert.ToInt64(response.OptionalResponseArgs));
                                }
                            }
                            else if(response.OptionalResponseCode.Equals("UIDVALIDITY",StringComparison.InvariantCultureIgnoreCase)){
                                if(folderInfo != null){
                                    folderInfo.SetUidValidity(Convert.ToInt64(response.OptionalResponseArgs));
                                }
                            }
                            else if(response.OptionalResponseCode.Equals("UNSEEN",StringComparison.InvariantCultureIgnoreCase)){
                                if(folderInfo != null){
                                    folderInfo.SetFirstUnseen(Convert.ToInt32(response.OptionalResponseArgs));
                                }
                            }
                            // We don't care about other response codes.                            
                        }

                        OnUntaggedStatusResponse(response);
                    }
                    else if(word.Equals("NO",StringComparison.InvariantCultureIgnoreCase)){
                        OnUntaggedStatusResponse(IMAP_r_u_ServerStatus.Parse(responseLine));
                    }
                    else if(word.Equals("BAD",StringComparison.InvariantCultureIgnoreCase)){
                        OnUntaggedStatusResponse(IMAP_r_u_ServerStatus.Parse(responseLine));
                    }
                    else if(word.Equals("PREAUTH",StringComparison.InvariantCultureIgnoreCase)){
                        OnUntaggedStatusResponse(IMAP_r_u_ServerStatus.Parse(responseLine));
                    }
                    else if(word.Equals("BYE",StringComparison.InvariantCultureIgnoreCase)){
                        OnUntaggedStatusResponse(IMAP_r_u_ServerStatus.Parse(responseLine));
                    }

                    #endregion

                    #region Untagged server and mailbox status. RFC 3501 7.2.

                    // CAPABILITY,LIST,LSUB,STATUS,SEARCH,FLAGS

                    #region CAPABILITY

                    else if(word.Equals("CAPABILITY",StringComparison.InvariantCultureIgnoreCase)){
                        if(capability != null){
                            capability.Add(IMAP_r_u_Capability.Parse(responseLine));
                        }
                    }

                    #endregion

                    #region LIST

                    else if(word.Equals("LIST",StringComparison.InvariantCultureIgnoreCase)){
                        if(list != null){
                            list.Add(IMAP_r_u_List.Parse(responseLine));
                        }
                    }

                    #endregion

                    #region LSUB

                    else if(word.Equals("LSUB",StringComparison.InvariantCultureIgnoreCase)){
                        if(lsub != null){
                            lsub.Add(IMAP_r_u_LSub.Parse(responseLine));
                        }
                    }

                    #endregion

                    #region STATUS

                    else if(word.Equals("STATUS",StringComparison.InvariantCultureIgnoreCase)){
                        if(status != null){
                            status.Add(IMAP_r_u_Status.Parse(responseLine));
                        }
                    }

                    #endregion

                    #region SEARCH

                    else if(word.Equals("SEARCH",StringComparison.InvariantCultureIgnoreCase)){
                        /* RFC 3501 7.2.5.  SEARCH Response
                            Contents:   zero or more numbers

                            The SEARCH response occurs as a result of a SEARCH or UID SEARCH
                            command.  The number(s) refer to those messages that match the
                            search criteria.  For SEARCH, these are message sequence numbers;
                            for UID SEARCH, these are unique identifiers.  Each number is
                            delimited by a space.

                            Example:    S: * SEARCH 2 3 6
                        */
                        
                        if(search != null){
                            if(responseLine.Split(' ').Length > 2){
                                foreach(string value in responseLine.Split(new char[]{' '},3)[2].Split(' ')){
                                    search.Add(Convert.ToInt32(value));
                                }
                            }
                        }
                    }

                    #endregion

                    #region FLAGS

                    else if(word.Equals("FLAGS",StringComparison.InvariantCultureIgnoreCase)){
                        /* RFC 3501 7.2.6. FLAGS Response.                         
                            Contents:   flag parenthesized list

                            The FLAGS response occurs as a result of a SELECT or EXAMINE
                            command.  The flag parenthesized list identifies the flags (at a
                            minimum, the system-defined flags) that are applicable for this
                            mailbox.  Flags other than the system flags can also exist,
                            depending on server implementation.

                            The update from the FLAGS response MUST be recorded by the client.

                            Example:    S: * FLAGS (\Answered \Flagged \Deleted \Seen \Draft)
                        */

                        if(folderInfo != null){
                            StringReader r = new StringReader(responseLine.Split(new char[]{' '},3)[2]);

                            folderInfo.SetFlags(r.ReadParenthesized().Split(' '));
                        }
                    }

                    #endregion

                    #endregion

                    #region Untagged mailbox size. RFC 3501 7.3.

                    // EXISTS,RECENT

                    // TODO: May this values exist other command than SELECT and EXAMINE ?
                    // Update local cached value.
                    // OnMailboxSize

                    else if(Net_Utils.IsInteger(word) && parts[2].Equals("EXISTS",StringComparison.InvariantCultureIgnoreCase)){
                        if(folderInfo != null){
                            folderInfo.SetMessagesCount(Convert.ToInt32(word));
                        }
                    }
                    else if(Net_Utils.IsInteger(word) && parts[2].Equals("RECENT",StringComparison.InvariantCultureIgnoreCase)){
                        if(folderInfo != null){
                            folderInfo.SetRecentMessagesCount(Convert.ToInt32(word));
                        }
                    }
                                        
                    #endregion

                    #region Untagged message status. RFC 3501 7.4.

                    // EXPUNGE,FETCH

                    else if(Net_Utils.IsInteger(word) && parts[2].Equals("EXPUNGE",StringComparison.InvariantCultureIgnoreCase)){
                        OnMessageExpunged(IMAP_r_u_Expunge.Parse(responseLine));
                    }
                    else if(Net_Utils.IsInteger(word) && parts[2].Equals("FETCH",StringComparison.InvariantCultureIgnoreCase)){
                        // User din't provide us FETCH handler, make dummy one which eats up all fetch responses.
                        if(fetchHandler == null){
                            fetchHandler = new IMAP_Client_FetchHandler();
                        }

                        _FetchResponseReader r = new _FetchResponseReader(this,responseLine,fetchHandler);
                        r.Start();                        
                    }

                    #endregion

                    #region Untagged acl realted. RFC 4314.

                    else if(word.Equals("ACL",StringComparison.InvariantCultureIgnoreCase)){
                        if(acl != null){
                            acl.Add(IMAP_r_u_Acl.Parse(responseLine));
                        }
                    }
                    else if(word.Equals("LISTRIGHTS",StringComparison.InvariantCultureIgnoreCase)){
                        if(listRights != null){
                            listRights.Add(IMAP_r_u_ListRights.Parse(responseLine));
                        }
                    }
                    else if(word.Equals("MYRIGHTS",StringComparison.InvariantCultureIgnoreCase)){
                        if(myRights != null){
                            myRights.Add(IMAP_Response_MyRights.Parse(responseLine));
                        }
                    }

                    #endregion

                    #region Untagged quota related. RFC 2087.

                    else if(word.Equals("QUOTA",StringComparison.InvariantCultureIgnoreCase)){
                        if(quota != null){
                            quota.Add(IMAP_r_u_Quota.Parse(responseLine));
                        }
                    }
                    else if(word.Equals("QUOTAROOT",StringComparison.InvariantCultureIgnoreCase)){
                        if(quotaRoot != null){
                            quotaRoot.Add(IMAP_r_u_QuotaRoot.Parse(responseLine));
                        }
                    }

                    #endregion

                    #region Untagged namespace related. RFC 2342.

                    else if(word.Equals("NAMESPACE",StringComparison.InvariantCultureIgnoreCase)){
                        if(nspace != null){
                            nspace.Add(IMAP_r_u_Namespace.Parse(responseLine));
                        }
                    }

                    #endregion

                    #region Untagged enable related. RFC 5161.

                    else if(word.Equals("ENABLED",StringComparison.InvariantCultureIgnoreCase)){
                        if(enable != null){
                            enable.Add(IMAP_r_u_Enable.Parse(responseLine));
                        }
                    }

                    #endregion
                }
                // Command continuation response.
                else if(responseLine.StartsWith("+")){
                    return new IMAP_r_ServerStatus("+","+","+");
                }
                // Completion status response.
                else{
                    // Command response reading has completed.
                    return IMAP_r_ServerStatus.Parse(responseLine);
                }
            }
        }

        #endregion

        #region method Search

        /// <summary>
        /// Searches message what matches specified search criteria.
        /// </summary>
        /// <param name="uid">If true then UID SERACH, otherwise normal SEARCH.</param>
        /// <param name="charset">Charset used in search criteria. Value null means ASCII. The UTF-8 is reccomended value non ASCII searches.</param>
        /// <param name="criteria">Search criteria.</param>
        /// <returns>Returns search expression matehced messages sequence-numbers or UIDs(This depends on argument <b>uid</b> value).</returns>
        /// <exception cref="ArgumentNullException">Is rised when <b>criteria</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state(not-connected, not-authenticated or not-selected state).</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        [Obsolete("Use Search(bool uid,Encoding charset,IMAP_Search_Key criteria) instead.")]
        public int[] Search(bool uid,string charset,string criteria)
        {
            if(criteria == null){
                throw new ArgumentNullException("criteria");
            }
            if(criteria == string.Empty){
                throw new ArgumentException("Argument 'criteria' value must be specified.","criteria");
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }

            StringBuilder command = new StringBuilder();
            command.Append((m_CommandIndex++).ToString("d5"));
            if(uid){
                command.Append(" UID");
            }
            command.Append(" SEARCH");
            if(!string.IsNullOrEmpty(charset)){
                command.Append(" CHARSET " + charset);
            }
            command.Append(" " + criteria + "\r\n");

            SendCommand(command.ToString());

            // Read IMAP server response.
            List<int> retVal = new List<int>();
            IMAP_r_ServerStatus response = ReadFinalResponse(delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_Search){
                    retVal.AddRange(((IMAP_r_u_Search)e.Value).Values);
                }
            });
            if(!response.ResponseCode.Equals("OK",StringComparison.InvariantCultureIgnoreCase)){
                throw new IMAP_ClientException(response.ResponseCode,response.ResponseText);
            }
           
            return retVal.ToArray();
        }

        #endregion

        #region method Fetch

        /// <summary>
        /// Fetches specified message items.
        /// </summary>
        /// <param name="uid">Specifies if argument <b>seqSet</b> contains messages UID or sequence numbers.</param>
        /// <param name="seqSet">Sequence set of messages to fetch.</param>
        /// <param name="items">Fetch items to fetch.</param>
        /// <param name="handler">Fetch responses handler.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>seqSet</b>,<b>items</b> or <b>handler</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state(not-connected, not-authenticated or not-selected state).</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        [Obsolete("Use Fetch(bool uid,IMAP_t_SeqSet seqSet,IMAP_Fetch_DataItem[] items,EventHandler<EventArgs<IMAP_r_u>> callback) intead.")]
        public void Fetch(bool uid,IMAP_SequenceSet seqSet,IMAP_Fetch_DataItem[] items,IMAP_Client_FetchHandler handler)
        {
            if(seqSet == null){
                throw new ArgumentNullException("seqSet");
            }
            if(items == null){
                throw new ArgumentNullException("items");
            }
            if(items.Length < 1){
                throw new ArgumentException("Argument 'items' must conatain at least 1 value.","items");
            }
            if(handler == null){
                throw new ArgumentNullException("handler");
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }

            /* RFC 3501 6.4.5. FETCH Command.
                Arguments:  sequence set
                            message data item names or macro

                Responses:  untagged responses: FETCH

                Result:     OK - fetch completed
                            NO - fetch error: can't fetch that data
                            BAD - command unknown or arguments invalid

                The FETCH command retrieves data associated with a message in the
                mailbox.  The data items to be fetched can be either a single atom
                or a parenthesized list.

                Most data items, identified in the formal syntax under the
                msg-att-static rule, are static and MUST NOT change for any
                particular message.  Other data items, identified in the formal
                syntax under the msg-att-dynamic rule, MAY change, either as a
                result of a STORE command or due to external events.

                    For example, if a client receives an ENVELOPE for a
                    message when it already knows the envelope, it can
                    safely ignore the newly transmitted envelope.
            */

            StringBuilder command = new StringBuilder();
            command.Append((m_CommandIndex++).ToString("d5"));
            if(uid){
                command.Append(" UID");
            }
            command.Append(" FETCH " + seqSet.ToSequenceSetString() + " (");
            for(int i=0;i<items.Length;i++){
                if(i > 0){
                    command.Append(" ");
                }
                command.Append(items[i].ToString());
            }
            command.Append(")\r\n");
     
            SendCommand(command.ToString());

            IMAP_r_ServerStatus response = ReadResponse(null,null,null,null,null,null,null,null,null,null,null,null,handler,null);
            if(!response.ResponseCode.Equals("OK",StringComparison.InvariantCultureIgnoreCase)){
                throw new IMAP_ClientException(response.ResponseCode,response.ResponseText);
            }
        }

        #endregion

        #region method StoreMessage

        /// <summary>
        /// Stores specified message to the specified folder.
        /// </summary>
        /// <param name="folder">Folder name with path.</param>
        /// <param name="flags">Message flags.</param>
        /// <param name="internalDate">Message internal data. DateTime.MinValue means server will allocate it.</param>
        /// <param name="message">Message stream.</param>
        /// <param name="count">Number of bytes send from <b>message</b> stream.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> or <b>stream</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        [Obsolete("Use method StoreMessage(string folder,IMAP_t_MsgFlags flags,DateTime internalDate,Stream message,int count) instead.")]
        public void StoreMessage(string folder,IMAP_MessageFlags flags,DateTime internalDate,Stream message,int count)
        {
            StoreMessage(folder,IMAP_Utils.MessageFlagsToStringArray(flags),internalDate,message,count);
        }        

        #endregion

        #region method StoreMessageFlags

        /// <summary>
        /// Stores specified message flags to the sepcified messages.
        /// </summary>
        /// <param name="uid">Specifies if <b>seqSet</b> contains UIDs or sequence-numbers.</param>
        /// <param name="seqSet">Messages sequence-set.</param>
        /// <param name="setType">Specifies how flags are set.</param>
        /// <param name="flags">Message flags. Value null means no flags. For example: new string[]{"\Seen","\Answered"}.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>seqSet</b> is null reference.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>        
        [Obsolete("Use method public void StoreMessageFlags(bool uid,IMAP_t_SeqSet seqSet,IMAP_Flags_SetType setType,IMAP_t_MsgFlags flags) instead.")]
        public void StoreMessageFlags(bool uid,IMAP_SequenceSet seqSet,IMAP_Flags_SetType setType,string[] flags)
        {
            if(seqSet == null){
                throw new ArgumentNullException("seqSet");
            }
            if(flags == null){
                throw new ArgumentNullException("flags");
            }

            StoreMessageFlags(uid,IMAP_t_SeqSet.Parse(seqSet.ToSequenceSetString()),setType,new IMAP_t_MsgFlags(flags));
        }

        /// <summary>
        /// Stores specified message flags to the sepcified messages.
        /// </summary>
        /// <param name="uid">Specifies if <b>seqSet</b> contains UIDs or sequence-numbers.</param>
        /// <param name="seqSet">Messages sequence-set.</param>
        /// <param name="setType">Specifies how flags are set.</param>
        /// <param name="flags">Message flags.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>seqSet</b> is null reference.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        [Obsolete("Use method public void StoreMessageFlags(bool uid,IMAP_t_SeqSet seqSet,IMAP_Flags_SetType setType,IMAP_t_MsgFlags flags) instead.")]
        public void StoreMessageFlags(bool uid,IMAP_SequenceSet seqSet,IMAP_Flags_SetType setType,IMAP_MessageFlags flags)
        {
            StoreMessageFlags(uid,seqSet,setType,IMAP_Utils.MessageFlagsToStringArray(flags));
        }

        #endregion

        #region method CopyMessages

        /// <summary>
        /// Copies specified messages from current selected folder to the specified target folder.
        /// </summary>
        /// <param name="uid">Specifies if <b>seqSet</b> contains UIDs or message-numberss.</param>
        /// <param name="seqSet">Messages sequence set.</param>
        /// <param name="targetFolder">Target folder name with path.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>seqSet</b> or <b>targetFolder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        [Obsolete("Use method 'CopyMessages(bool uid,IMAP_t_SeqSet seqSet,string targetFolder)' instead.")]
        public void CopyMessages(bool uid,IMAP_SequenceSet seqSet,string targetFolder)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(seqSet == null){
                throw new ArgumentNullException("seqSet");
            }
            if(targetFolder == null){
                throw new ArgumentNullException("folder");
            }
            if(targetFolder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }

            CopyMessages(uid,IMAP_t_SeqSet.Parse(seqSet.ToSequenceSetString()),targetFolder);
        }

        #endregion

        #region method MoveMessages

        /// <summary>
        /// Moves specified messages from current selected folder to the specified target folder.
        /// </summary>
        /// <param name="uid">Specifies if <b>seqSet</b> contains UIDs or message-numberss.</param>
        /// <param name="seqSet">Messages sequence set.</param>
        /// <param name="targetFolder">Target folder name with path.</param>
        /// <param name="expunge">If ture messages are expunged from selected folder, otherwise they are marked as <b>Deleted</b>.
        /// Note: If true - then all messages marked as <b>Deleted</b> are expunged !</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>seqSet</b> or <b>targetFolder</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state(not-connected, not-authenticated or not-selected state).</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        [Obsolete("Use method 'MoveMessages(bool uid,IMAP_t_SeqSet seqSet,string targetFolder,bool expunge)' instead.")]
        public void MoveMessages(bool uid,IMAP_SequenceSet seqSet,string targetFolder,bool expunge)
        {
            if(seqSet == null){
                throw new ArgumentNullException("seqSet");
            }
            if(targetFolder == null){
                throw new ArgumentNullException("folder");
            }
            if(targetFolder == string.Empty){
                throw new ArgumentException("Argument 'folder' value must be specified.","folder");
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }
            if(m_pSelectedFolder == null){
                throw new InvalidOperationException("Not selected state, you need to select some folder first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }

            MoveMessages(uid,IMAP_t_SeqSet.Parse(seqSet.ToSequenceSetString()),targetFolder,expunge);
        }

        #endregion

        #region method GetFolderQuota

        /// <summary>
        /// Gets the specified folder quota-root resource limit entries.
        /// </summary>
        /// <param name="quotaRootName">Quota root name.</param>
        /// <returns>Returns quota-root resource limit entries.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when IMAP client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>quotaRootName</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="IMAP_ClientException">Is raised when server refuses to complete this command and returns error.</exception>
        [Obsolete("Use method 'GetQuota' instead.")]
        public IMAP_r_u_Quota[] GetFolderQuota(string quotaRootName)
        {            
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("Not connected, you need to connect first.");
            }
            if(!this.IsAuthenticated){
                throw new InvalidOperationException("Not authenticated, you need to authenticate first.");
            }            
            if(m_pIdle != null){
                throw new InvalidOperationException("This command is not valid in IDLE state, you need stop idling before calling this command.");
            }
            if(quotaRootName == null){
                throw new ArgumentNullException("quotaRootName");
            }

            List<IMAP_r_u_Quota> retVal = new List<IMAP_r_u_Quota>();

            // Create callback. It is called for each untagged IMAP server response.
            EventHandler<EventArgs<IMAP_r_u>> callback = delegate(object sender,EventArgs<IMAP_r_u> e){
                if(e.Value is IMAP_r_u_Quota){
                    retVal.Add((IMAP_r_u_Quota)e.Value);
                }
            };

            using(GetQuotaAsyncOP op = new GetQuotaAsyncOP(quotaRootName,callback)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<GetQuotaAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.GetQuotaAsync(op)){
                        wait.Set();
                    }
                    wait.WaitOne();

                    if(op.Error != null){
                        throw op.Error;
                    }
                }
            }

            return retVal.ToArray();
        }

        #endregion

        #region class _FetchResponseReader

        /// <summary>
        /// This class implements FETCH response reader.
        /// </summary>
        [Obsolete("deprecated")]
        internal class _FetchResponseReader
        {
            private IMAP_Client              m_pImap        = null;
            private string                   m_FetchLine    = null;
            private StringReader             m_pFetchReader = null;
            private IMAP_Client_FetchHandler m_pHandler     = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="imap">IMAP client.</param>
            /// <param name="fetchLine">Initial FETCH response line.</param>
            /// <param name="handler">Fetch data-items handler.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>imap</b>,<b>fetchLine</b> or <b>handler</b> is null reference.</exception>
            public _FetchResponseReader(IMAP_Client imap,string fetchLine,IMAP_Client_FetchHandler handler)
            {
                if(imap == null){
                    throw new ArgumentNullException("imap");
                }
                if(fetchLine == null){
                    throw new ArgumentNullException("fetchLine");
                }
                if(handler == null){
                    throw new ArgumentNullException("handler");
                }

                m_pImap     = imap;
                m_FetchLine = fetchLine;
                m_pHandler  = handler;
            }

            #region method Start

            /// <summary>
            /// Starts reading FETCH response.
            /// </summary>
            public void Start()
            {
                // * seqNo FETCH 1data-item/(1*data-item)

                int seqNo = Convert.ToInt32(m_FetchLine.Split(' ')[1]);

                // Notify that current message has changed.
                m_pHandler.SetCurrentSeqNo(seqNo);
                m_pHandler.OnNextMessage();

                m_pFetchReader = new StringReader(m_FetchLine.Split(new char[]{' '},4)[3]);
                if(m_pFetchReader.StartsWith("(")){
                    m_pFetchReader.ReadSpecifiedLength(1);
                }

                // Read data-items.
                while(m_pFetchReader.Available > 0){
                    m_pFetchReader.ReadToFirstChar();
//*
                    #region BODY

                    if(m_pFetchReader.StartsWith("BODY ",false)){
                    }

                    #endregion

                    #region BODY[<section>]<<origin octet>>

                    else if(m_pFetchReader.StartsWith("BODY[",false)){
                        // Eat BODY word.
                        m_pFetchReader.ReadWord();

                        // Read body-section.
                        string section = m_pFetchReader.ReadParenthesized();

                        // Read origin if any.
                        int offset = -1;
                        if(m_pFetchReader.StartsWith("<")){
                            offset = Convert.ToInt32(m_pFetchReader.ReadParenthesized().Split(' ')[0]);
                        }


                        // Get Message store stream.
                        IMAP_Client_Fetch_Body_EArgs eArgs = new IMAP_Client_Fetch_Body_EArgs(section,offset);
                        m_pHandler.OnBody(eArgs);

                        // We don't have BODY[].
                        m_pFetchReader.ReadToFirstChar();
                        if(m_pFetchReader.StartsWith("NIL",false)){
                            // Eat NIL.
                            m_pFetchReader.ReadWord();
                        }
                        // BODY[] value is returned as string-literal.
                        else if(m_pFetchReader.StartsWith("{",false)){
                            if(eArgs.Stream == null){
                                m_pImap.ReadStringLiteral(Convert.ToInt32(m_pFetchReader.ReadParenthesized()),new JunkingStream());
                            }
                            else{
                                m_pImap.ReadStringLiteral(Convert.ToInt32(m_pFetchReader.ReadParenthesized()),eArgs.Stream);
                            }
                            
                            // Read continuing FETCH line.
                            m_pFetchReader = new StringReader(m_pImap.ReadLine());
                        }
                        // BODY[] is quoted-string.
                        else{
                            m_pFetchReader.ReadWord();
                        }

                        // Notify that message storing has completed.
                        eArgs.OnStoringCompleted();
                    }

                    #endregion
//*
                    #region BODYSTRUCTURE

                    else if(m_pFetchReader.StartsWith("BODYSTRUCTURE ",false)){
                    }

                    #endregion

                    #region ENVELOPE

                    else if(m_pFetchReader.StartsWith("ENVELOPE ",false)){
                        m_pHandler.OnEnvelope(IMAP_Envelope.Parse(this));
                    }

                    #endregion

                    #region  FLAGS

                    else if(m_pFetchReader.StartsWith("FLAGS ",false)){
                        // Eat FLAGS word.
                        m_pFetchReader.ReadWord();

                        string   flagsList = m_pFetchReader.ReadParenthesized();
                        string[] flags     = new string[0];
                        if(!string.IsNullOrEmpty(flagsList)){
                            flags = flagsList.Split(' ');
                        }

                        m_pHandler.OnFlags(flags);
                    }

                    #endregion

                    #region INTERNALDATE

                    else if(m_pFetchReader.StartsWith("INTERNALDATE ",false)){
                         // Eat INTERNALDATE word.
                        m_pFetchReader.ReadWord();

                        m_pHandler.OnInternalDate(IMAP_Utils.ParseDate(m_pFetchReader.ReadWord()));
                    }

                    #endregion

                    #region RFC822

                    else if(m_pFetchReader.StartsWith("RFC822 ",false)){
                        // Eat RFC822 word.
                        m_pFetchReader.ReadWord(false,new char[]{' '},false);
                        m_pFetchReader.ReadToFirstChar();

                        // Get Message store stream.
                        IMAP_Client_Fetch_Rfc822_EArgs eArgs = new IMAP_Client_Fetch_Rfc822_EArgs();
                        m_pHandler.OnRfc822(eArgs);

                        // We don't have RFC822.
                        if(m_pFetchReader.StartsWith("NIL",false)){
                            // Eat NIL.
                            m_pFetchReader.ReadWord();
                        }
                        // RFC822 value is returned as string-literal.
                        else if(m_pFetchReader.StartsWith("{",false)){
                            if(eArgs.Stream == null){
                                m_pImap.ReadStringLiteral(Convert.ToInt32(m_pFetchReader.ReadParenthesized()),new JunkingStream());
                            }
                            else{
                                m_pImap.ReadStringLiteral(Convert.ToInt32(m_pFetchReader.ReadParenthesized()),eArgs.Stream);
                            }
                            
                            // Read continuing FETCH line.
                            m_pFetchReader = new StringReader(m_pImap.ReadLine());
                        }
                        // RFC822 is quoted-string.
                        else{
                            m_pFetchReader.ReadWord();
                        }

                        // Notify that message storing has completed.
                        eArgs.OnStoringCompleted();
                    }

                    #endregion

                    #region RFC822.HEADER

                    else if(m_pFetchReader.StartsWith("RFC822.HEADER ",false)){
                        // Eat RFC822.HEADER word.
                        m_pFetchReader.ReadWord(false,new char[]{' '},false);
                        m_pFetchReader.ReadToFirstChar();
                        
                        string text = null;
                        // We don't have HEADER.
                        if(m_pFetchReader.StartsWith("NIL",false)){
                            // Eat NIL.
                            m_pFetchReader.ReadWord();

                            text = null;
                        }
                        // HEADER value is returned as string-literal.
                        else if(m_pFetchReader.StartsWith("{",false)){
                            text = m_pImap.ReadStringLiteral(Convert.ToInt32(m_pFetchReader.ReadParenthesized()));
                            
                            // Read continuing FETCH line.
                            m_pFetchReader = new StringReader(m_pImap.ReadLine());
                        }
                        // HEADER is quoted-string.
                        else{
                            text = m_pFetchReader.ReadWord();
                        }

                        m_pHandler.OnRfc822Header(text);
                    }

                    #endregion

                    #region RFC822.SIZE

                    else if(m_pFetchReader.StartsWith("RFC822.SIZE ",false)){
                        // Eat RFC822.SIZE word.
                        m_pFetchReader.ReadWord(false,new char[]{' '},false);

                        m_pHandler.OnSize(Convert.ToInt32(m_pFetchReader.ReadWord()));
                    }

                    #endregion

                    #region RFC822.TEXT

                    else if(m_pFetchReader.StartsWith("RFC822.TEXT ",false)){
                        // Eat RFC822.TEXT word.
                        m_pFetchReader.ReadWord(false,new char[]{' '},false);
                        m_pFetchReader.ReadToFirstChar();
                        
                        string text = null;
                        // We don't have TEXT.
                        if(m_pFetchReader.StartsWith("NIL",false)){
                            // Eat NIL.
                            m_pFetchReader.ReadWord();

                            text = null;
                        }
                        // TEXT value is returned as string-literal.
                        else if(m_pFetchReader.StartsWith("{",false)){
                            text = m_pImap.ReadStringLiteral(Convert.ToInt32(m_pFetchReader.ReadParenthesized()));
                            
                            // Read continuing FETCH line.
                            m_pFetchReader = new StringReader(m_pImap.ReadLine());
                        }
                        // TEXT is quoted-string.
                        else{
                            text = m_pFetchReader.ReadWord();
                        }

                        m_pHandler.OnRfc822Text(text);
                    }

                    #endregion

                    #region UID

                    else if(m_pFetchReader.StartsWith("UID ",false)){
                        // Eat UID word.
                        m_pFetchReader.ReadWord();

                        m_pHandler.OnUID(Convert.ToInt64(m_pFetchReader.ReadWord()));
                    }

                    #endregion

                    #region X-GM-MSGID

                    else if(m_pFetchReader.StartsWith("X-GM-MSGID ",false)){
                        // Eat X-GM-MSGID word.
                        m_pFetchReader.ReadWord();

                        m_pHandler.OnX_GM_MSGID(Convert.ToUInt64(m_pFetchReader.ReadWord()));
                    }

                    #endregion

                    #region X-GM-THRID

                    else if(m_pFetchReader.StartsWith("X-GM-THRID ",false)){
                        // Eat X-GM-THRID word.
                        m_pFetchReader.ReadWord();

                        m_pHandler.OnX_GM_THRID(Convert.ToUInt64(m_pFetchReader.ReadWord()));
                    }

                    #endregion

                    #region Fetch closing ")"

                    else if(m_pFetchReader.StartsWith(")",false)){
                        break;
                    }

                    #endregion

                    else{
                        throw new NotSupportedException("Not supported IMAP FETCH data-item '" + m_pFetchReader.ReadToEnd() + "'.");
                    }
                }
            }

            #endregion


            #region method GetReader

            /// <summary>
            /// Gets FETCH current line data reader.
            /// </summary>
            internal StringReader GetReader()
            {
                return m_pFetchReader;
            }

            #endregion

            #region method ReadString

            /// <summary>
            /// Reads string. Quoted-string-string-literal and NIL supported.
            /// </summary>
            /// <returns>Returns readed string.</returns>
            internal string ReadString()
            {                        
                m_pFetchReader.ReadToFirstChar();
                // NIL string.
                if(m_pFetchReader.StartsWith("NIL",false)){
                    m_pFetchReader.ReadWord();

                    return null;
                }
                // string-literal.
                else if(m_pFetchReader.StartsWith("{")){
                    string retVal = m_pImap.ReadStringLiteral(Convert.ToInt32(m_pFetchReader.ReadParenthesized()));

                    // Read continuing FETCH line.
                    m_pFetchReader = new StringReader(m_pImap.ReadLine());

                    return retVal;
                }
                // quoted-string or atom.
                else{
                    return MIME_Encoding_EncodedWord.DecodeS(m_pFetchReader.ReadWord());
                }
            }

            #endregion
        }

        #endregion

        #region method ReadStringLiteral

        /// <summary>
        /// Reads IMAP <b>string-literal</b> from remote endpoint.
        /// </summary>
        /// <param name="count">Number of bytes to read.</param>
        /// <returns>Returns readed string-literal.</returns>
        [Obsolete("deprecated")]
        private string ReadStringLiteral(int count)
        {
            /* RFC 3501 4.3.            
                string-literal = {bytes_count} CRLF      - Number of bytes after CRLF.
                quoted-string  = DQUOTE string DQUOTE    - Normal quoted-string.
            */

            string retVal = this.TcpStream.ReadFixedCountString(count);            
            LogAddRead(count,"Readed string-literal " + count.ToString() + " bytes.");

            return retVal;
        }

        /// <summary>
        /// Reads IMAP <b>string-literal</b> from remote endpoint.
        /// </summary>
        /// <param name="count">Number of bytes to read.</param>
        /// <param name="stream">Stream where to store readed data.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
        [Obsolete("deprecated")]
        private void ReadStringLiteral(int count,Stream stream)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            this.TcpStream.ReadFixedCount(stream,count);
            LogAddRead(count,"Readed string-literal " + count.ToString() + " bytes.");
        }

        #endregion

        #region method SendCommand

        /// <summary>
        /// Send specified command to the IMAP server.
        /// </summary>
        /// <param name="command">Command to send.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>command</b> is null reference value.</exception>
        [Obsolete("Deprecated.")]
        private void SendCommand(string command)
        {
            if(command == null){
                throw new ArgumentNullException("command");
            }

            byte[] buffer = Encoding.UTF8.GetBytes(command);                                  
            this.TcpStream.Write(buffer,0,buffer.Length);
            LogAddWrite(command.TrimEnd().Length,command.TrimEnd());
        }

        #endregion

        #region method ReadFinalResponse

        /// <summary>
        /// Reads final response from IMAP server.
        /// </summary>
        /// <param name="callback">Optional callback to be called for each server returned untagged response.</param>
        /// <returns>Returns final response.</returns>
        [Obsolete("deprecated")]
        private IMAP_r_ServerStatus ReadFinalResponse(EventHandler<EventArgs<IMAP_r_u>> callback)
        {
            ManualResetEvent wait = new ManualResetEvent(false);
            using(ReadFinalResponseAsyncOP op = new ReadFinalResponseAsyncOP(callback)){
                op.CompletedAsync += delegate(object s1,EventArgs<ReadFinalResponseAsyncOP> e1){
                    wait.Set();
                };
                if(!this.ReadFinalResponseAsync(op)){
                    wait.Set();
                }
                wait.WaitOne();
                wait.Close();

                if(op.Error != null){
                    throw op.Error;
                }
                else{
                    return op.FinalResponse;
                }
            }
        }

        #endregion
    }
}
