using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Security;
using System.Security.Principal;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Threading;

using LumiSoft.Net.IO;
using LumiSoft.Net.TCP;
using LumiSoft.Net.AUTH;
using LumiSoft.Net.DNS;
using LumiSoft.Net.DNS.Client;
using LumiSoft.Net.Mail;
using LumiSoft.Net.MIME;
using LumiSoft.Net.Mime;

namespace LumiSoft.Net.SMTP.Client
{
    /// <summary>
    /// This class implements SMTP client. Defined in RFC 5321.
    /// </summary>
	/// <example>
    /// Simple way:
    /// <code>
	/// /*
	///  To make this code to work, you need to import following namespaces:
	///  using LumiSoft.Net.SMTP.Client; 
	/// */
    /// 
    /// // You can send any valid SMTP message here, from disk,memory, ... or
    /// // you can use LumiSoft.Net.Mail classes to compose valid SMTP mail message.
    /// 
    /// // SMTP_Client.QuickSendSmartHost(...
    /// or
    /// // SMTP_Client.QuickSend(...
    /// </code>
    /// 
    /// Advanced way:
	/// <code> 
	/// /*
	///  To make this code to work, you need to import following namespaces:
	///  using LumiSoft.Net.SMTP.Client; 
	/// */
	/// 
	/// using(SMTP_Client smtp = new SMTP_Client()){      
    ///     // You can use Dns_Client.GetEmailHosts(... to get target recipient SMTP hosts for Connect method.
	///		smtp.Connect("hostName",WellKnownPorts.SMTP); 
    ///		smtp.EhloHelo("mail.domain.com");
    ///     // Authenticate if target server requires.
    ///     // smtp.Auth(smtp.AuthGetStrongestMethod("user","password"));
    ///     smtp.MailFrom("sender@domain.com");
    ///     // Repeat this for all recipients.
    ///     smtp.RcptTo("to@domain.com");
    /// 
    ///     // Send message to server.
    ///     // You can send any valid SMTP message here, from disk,memory, ... or
    ///     // you can use LumiSoft.Net.Mail classes to compose valid SMTP mail message.
    ///     // smtp.SendMessage(.... .
    ///     
    ///     smtp.Disconnect();
	///	}
	/// </code>
	/// </example>
    public class SMTP_Client : TCP_Client
    {
        private string          m_LocalHostName      = null;
        private string          m_RemoteHostName     = null;
        private string          m_GreetingText       = "";
        private bool            m_IsEsmtpSupported   = false;
        private List<string>    m_pEsmtpFeatures     = null;
        private string          m_MailFrom           = null;
        private List<string>    m_pRecipients        = null;
        private GenericIdentity m_pAuthdUserIdentity = null;
        
        /// <summary>
        /// Default constructor.
        /// </summary>
        public SMTP_Client()
        {
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
		/// Closes connection to SMTP server.
		/// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
		public override void Disconnect()
		{
            Disconnect(true);
        }

        /// <summary>
		/// Closes connection to SMTP server.
		/// </summary>
        /// <param name="sendQuit">If true QUIT command is sent to SMTP server.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
		public void Disconnect(bool sendQuit)
		{
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("SMTP client is not connected.");
            }

			try{
                if(sendQuit){
                    // Send QUIT command to server.                
                    WriteLine("QUIT");

                    // Read QUIT response.
                    ReadLine();
                }
			}
			catch{
			}

            m_LocalHostName      = null;
            m_RemoteHostName     = null;
            m_GreetingText       = "";
            m_IsEsmtpSupported   = false;
            m_pEsmtpFeatures     = null;
            m_MailFrom           = null;
            m_pRecipients        = null;
            m_pAuthdUserIdentity = null;

            try{
                base.Disconnect(); 
            }
            catch{
            }
		}

		#endregion

        #region method EhloHelo

        /// <summary>
        /// Sends EHLO/HELO command to SMTP server.
        /// </summary>
        /// <param name="hostName">Local host DNS name.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>hostName</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        /// <remarks>NOTE: EHLO command will reset all SMTP session state data.</remarks>
        public void EhloHelo(string hostName)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(hostName == null){
                throw new ArgumentNullException("hostName");
            }
            if(hostName == string.Empty){
                throw new ArgumentException("Argument 'hostName' value must be specified.","hostName");
            }

            ManualResetEvent wait = new ManualResetEvent(false);
            using(EhloHeloAsyncOP op = new EhloHeloAsyncOP(hostName)){
                op.CompletedAsync += delegate(object s1,EventArgs<EhloHeloAsyncOP> e1){
                    wait.Set();
                };
                if(!this.EhloHeloAsync(op)){
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

        #region method EhloHeloAsync

        #region class EhloHeloAsyncOP

        /// <summary>
        /// This class represents <see cref="SMTP_Client.EhloHeloAsync"/> asynchronous operation.
        /// </summary>
        public class EhloHeloAsyncOP : IDisposable,IAsyncOP
        {
            private object             m_pLock         = new object();
            private AsyncOP_State      m_State         = AsyncOP_State.WaitingForStart;
            private Exception          m_pException    = null;
            private string             m_HostName      = null;
            private SMTP_Client        m_pSmtpClient   = null;
            private SMTP_t_ReplyLine[] m_pReplyLines   = null;
            private bool               m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="hostName">Local host DNS name.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>hostName</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public EhloHeloAsyncOP(string hostName)
            {
                if(hostName == null){
                    throw new ArgumentNullException("hostName");
                }
                if(hostName == string.Empty){
                    throw new ArgumentException("Argument 'hostName' value must be specified.","hostName");
                }

                m_HostName = hostName;
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
                m_HostName    = null;
                m_pSmtpClient = null;
                m_pReplyLines = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner SMTP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(SMTP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pSmtpClient = owner;

                SetState(AsyncOP_State.Active);

                try{
                    // NOTE: At frist we try EHLO command, if it fails we fallback to HELO.

                    /* RFC 5321 4.1.1.1.
                        ehlo        = "EHLO" SP ( Domain / address-literal ) CRLF  
                     
                        ehlo-ok-rsp = ( "250" SP Domain [ SP ehlo-greet ] CRLF )
                                    / ( "250-" Domain [ SP ehlo-greet ] CRLF
                                     *( "250-" ehlo-line CRLF )
                                        "250" SP ehlo-line CRLF )
                    */

                    byte[] buffer = Encoding.UTF8.GetBytes("EHLO " + m_HostName + "\r\n");

                    // Log
                    m_pSmtpClient.LogAddWrite(buffer.Length,"EHLO " + m_HostName);

                    // Start command sending.
                    m_pSmtpClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.EhloCommandSendingCompleted,null);                    
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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

            #region method EhloCommandSendingCompleted

            /// <summary>
            /// Is called when EHLO command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void EhloCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pSmtpClient.TcpStream.EndWrite(ar);

                    // Read SMTP server response.
                    ReadResponseAsyncOP readResponseOP = new ReadResponseAsyncOP();
                    readResponseOP.CompletedAsync += delegate(object s,EventArgs<ReadResponseAsyncOP> e){
                        EhloReadResponseCompleted(readResponseOP);
                    };
                    if(!m_pSmtpClient.ReadResponseAsync(readResponseOP)){
                        EhloReadResponseCompleted(readResponseOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method EhloReadResponseCompleted

            /// <summary>
            /// Is called when SMTP server EHLO command response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void EhloReadResponseCompleted(ReadResponseAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                        SetState(AsyncOP_State.Completed);
                    }
                    else{
                        m_pReplyLines = op.ReplyLines;

                        // EHLO succeeded.
                        if(m_pReplyLines[0].ReplyCode == 250){
                            /* RFC 5321 4.1.1.1.
                                ehlo        = "EHLO" SP ( Domain / address-literal ) CRLF  
                     
                                ehlo-ok-rsp = ( "250" SP Domain [ SP ehlo-greet ] CRLF )
                                            / ( "250-" Domain [ SP ehlo-greet ] CRLF
                                             *( "250-" ehlo-line CRLF )
                                                "250" SP ehlo-line CRLF )
                            */

                            m_pSmtpClient.m_RemoteHostName = m_pReplyLines[0].Text.Split(new char[]{' '},2)[0];
                            m_pSmtpClient.m_IsEsmtpSupported = true;
                            List<string> esmtpFeatures = new List<string>();
                            foreach(SMTP_t_ReplyLine line in m_pReplyLines){
                                esmtpFeatures.Add(line.Text);
                            }
                            m_pSmtpClient.m_pEsmtpFeatures = esmtpFeatures;

                            SetState(AsyncOP_State.Completed);
                        }
                        // EHLO failed, try HELO(EHLO may be disabled or not supported)
                        else{
                            /* RFC 5321 4.1.1.1.
                                helo        = "HELO" SP Domain CRLF
                                helo-ok-rsp = "250" SP Domain [ SP helo-greet ] CRLF
                            */

                            // Log.
                            m_pSmtpClient.LogAddText("EHLO failed, will try HELO.");
                            
                            byte[] buffer = Encoding.UTF8.GetBytes("HELO " + m_HostName + "\r\n");

                            // Log
                            m_pSmtpClient.LogAddWrite(buffer.Length,"HELO " + m_HostName);

                            // Start command sending.
                            m_pSmtpClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.HeloCommandSendingCompleted,null);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    SetState(AsyncOP_State.Completed);
                }

                op.Dispose();
            }

            #endregion

            #region method HeloCommandSendingCompleted

            /// <summary>
            /// Is called when HELO command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void HeloCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pSmtpClient.TcpStream.EndWrite(ar);

                    // Read HELO command response.
                    ReadResponseAsyncOP readResponseOP = new ReadResponseAsyncOP();
                    readResponseOP.CompletedAsync += delegate(object s,EventArgs<ReadResponseAsyncOP> e){
                        HeloReadResponseCompleted(readResponseOP);
                    };
                    if(!m_pSmtpClient.ReadResponseAsync(readResponseOP)){
                        HeloReadResponseCompleted(readResponseOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method HeloReadResponseCompleted

            /// <summary>
            /// Is called when SMTP server HELO command response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void HeloReadResponseCompleted(ReadResponseAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    }
                    else{
                        m_pReplyLines = op.ReplyLines;

                        // HELO succeeded.
                        if(m_pReplyLines[0].ReplyCode == 250){
                            /* RFC 5321 4.1.1.1.
                                helo        = "HELO" SP Domain CRLF
                                helo-ok-rsp = "250" SP Domain [ SP helo-greet ] CRLF
                            */

                            m_pSmtpClient.m_RemoteHostName = m_pReplyLines[0].Text.Split(new char[]{' '},2)[0];
                            m_pSmtpClient.m_IsEsmtpSupported = true;
                            List<string> esmtpFeatures = new List<string>();
                            foreach(SMTP_t_ReplyLine line in m_pReplyLines){
                                esmtpFeatures.Add(line.Text);
                            }
                            m_pSmtpClient.m_pEsmtpFeatures = esmtpFeatures;
                        }
                        // HELO failed.
                        else{
                            m_pException = new SMTP_ClientException(op.ReplyLines);
                            m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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

            /// <summary>
            /// Gets SMTP server reply-lines.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public SMTP_t_ReplyLine[] ReplyLines
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'ReplyLines' is accessible only in 'AsyncOP_State.Completed' state.");
                    }
                    if(m_pException != null){
                        throw m_pException;
                    }

                    return m_pReplyLines;
                }
            }

            #endregion

            #region Events implementation

            /// <summary>
            /// Is called when asynchronous operation has completed.
            /// </summary>
            public event EventHandler<EventArgs<EhloHeloAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<EhloHeloAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending EHLO/HELO command to SMTP server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="EhloHeloAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <remarks>NOTE: EHLO command will reset all SMTP session state data.</remarks>
        public bool EhloHeloAsync(EhloHeloAsyncOP op)
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
                
        #region method StartTLS

        /// <summary>
        /// Sends STARTTLS command to SMTP server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected or is already secure connection.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        /// <remarks>After successful STARTTLS all SMTP session data(EHLO,MAIL FROM, ....) will be reset.
        /// If unknwon(not SMTP error) error happens during STARTTLS negotiation, SMTP client should disconnect.</remarks>
        public void StartTLS()
        {
            StartTLS(null);
        }

        /// <summary>
        /// Sends STARTTLS command to SMTP server.
        /// </summary>
        /// <param name="certCallback">SSL server certificate validation callback. Value null means any certificate is accepted.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected or is already secure connection.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        /// <remarks>After successful STARTTLS all SMTP session data(EHLO,MAIL FROM, ....) will be reset.
        /// If unknwon(not SMTP error) error happens during STARTTLS negotiation, SMTP client should disconnect.</remarks>
        public void StartTLS(RemoteCertificateValidationCallback certCallback)
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

            ManualResetEvent wait = new ManualResetEvent(false);
            using(StartTlsAsyncOP op = new StartTlsAsyncOP(certCallback)){
                op.CompletedAsync += delegate(object s1,EventArgs<StartTlsAsyncOP> e1){
                    wait.Set();
                };
                if(!this.StartTlsAsync(op)){
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

        #region method StartTlsAsync

        #region class StartTlsAsyncOP

        /// <summary>
        /// This class represents <see cref="SMTP_Client.StartTlsAsync"/> asynchronous operation.
        /// </summary>
        public class StartTlsAsyncOP : IDisposable,IAsyncOP
        {
            private object                              m_pLock         = new object();
            private AsyncOP_State                       m_State         = AsyncOP_State.WaitingForStart;
            private Exception                           m_pException    = null;
            private RemoteCertificateValidationCallback m_pCertCallback = null;
            private SMTP_Client                         m_pSmtpClient   = null;
            private bool                                m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="certCallback">SSL server certificate validation callback. Value null means any certificate is accepted.</param>
            public StartTlsAsyncOP(RemoteCertificateValidationCallback certCallback)
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
                m_pSmtpClient   = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner SMTP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(SMTP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pSmtpClient = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 3207 4.
                        The format for the STARTTLS command is:

                        STARTTLS

                        with no parameters.

                        After the client gives the STARTTLS command, the server responds with
                        one of the following reply codes:

                        220 Ready to start TLS
                        501 Syntax error (no parameters allowed)
                        454 TLS not available due to temporary reason
                    */

                    byte[] buffer = Encoding.UTF8.GetBytes("STARTTLS\r\n");

                    // Log
                    m_pSmtpClient.LogAddWrite(buffer.Length,"STARTTLS");

                    // Start command sending.
                    m_pSmtpClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.StartTlsCommandSendingCompleted,null);                    
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
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

            #region method StartTlsCommandSendingCompleted

            /// <summary>
            /// Is called when STARTTLS command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void StartTlsCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pSmtpClient.TcpStream.EndWrite(ar);

                    // Read SMTP server response.
                    ReadResponseAsyncOP readResponseOP = new ReadResponseAsyncOP();
                    readResponseOP.CompletedAsync += delegate(object s,EventArgs<ReadResponseAsyncOP> e){
                        StartTlsReadResponseCompleted(readResponseOP);
                    };
                    if(!m_pSmtpClient.ReadResponseAsync(readResponseOP)){
                        StartTlsReadResponseCompleted(readResponseOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method StartTlsReadResponseCompleted

            /// <summary>
            /// Is called when STARTTLS command response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void StartTlsReadResponseCompleted(ReadResponseAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                    }
                    else{
                        // STARTTLS accepted.
                        if(op.ReplyLines[0].ReplyCode == 220){
                            /* RFC 3207 4.
                                The format for the STARTTLS command is:

                                STARTTLS

                                with no parameters.

                                After the client gives the STARTTLS command, the server responds with
                                one of the following reply codes:

                                220 Ready to start TLS
                                501 Syntax error (no parameters allowed)
                                454 TLS not available due to temporary reason
                            */

                            // Log
                            m_pSmtpClient.LogAddText("Starting TLS handshake.");

                            SwitchToSecureAsyncOP switchSecureOP = new SwitchToSecureAsyncOP(m_pCertCallback);
                            switchSecureOP.CompletedAsync += delegate(object s,EventArgs<SwitchToSecureAsyncOP> e){
                                SwitchToSecureCompleted(switchSecureOP);
                            };
                            if(!m_pSmtpClient.SwitchToSecureAsync(switchSecureOP)){
                                SwitchToSecureCompleted(switchSecureOP);
                            }                       
                        }
                        // STARTTLS failed.
                        else{
                            m_pException = new SMTP_ClientException(op.ReplyLines);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;                    
                }

                op.Dispose();

                if(m_pException != null){
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    SetState(AsyncOP_State.Completed);
                }
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
                        m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    }
                    else{
                        // Log
                        m_pSmtpClient.LogAddText("TLS handshake completed sucessfully.");
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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
        /// Starts sending STARTTLS command to SMTP server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="StartTlsAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected or connection is already secure.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <remarks>After successful STARTTLS all SMTP session data(EHLO,MAIL FROM, ....) will be reset.
        /// If unknwon(not SMTP error) error happens during STARTTLS negotiation, SMTP client should disconnect.</remarks>
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
            if(op == null){
                throw new ArgumentNullException("op");
            }
            if(op.State != AsyncOP_State.WaitingForStart){
                throw new ArgumentException("Invalid argument 'op' state, 'op' must be in 'AsyncOP_State.WaitingForStart' state.","op");
            }

            return op.Start(this);
        }

        #endregion

        #region method AuthGetStrongestMethod

        /// <summary>
        /// Gets strongest authentication method which we can support from SMTP server.
        /// Preference order DIGEST-MD5 -> CRAM-MD5 -> LOGIN -> PLAIN.
        /// </summary>
        /// <param name="userName">User name.</param>
        /// <param name="password">User password.</param>
        /// <returns>Returns authentication method.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected .</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>userName</b> or <b>password</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="NotSupportedException">Is raised when SMTP server won't support authentication or we 
        /// don't support any of the server authentication mechanisms.</exception>
        public AUTH_SASL_Client AuthGetStrongestMethod(string userName,string password)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}

            List<string> authMethods = new List<string>(this.SaslAuthMethods);
            if(authMethods.Count == 0){
                throw new NotSupportedException("SMTP server does not support authentication.");
            }
            else if(authMethods.Contains("DIGEST-MD5")){
                return new AUTH_SASL_Client_DigestMd5("SMTP",this.RemoteEndPoint.Address.ToString(),userName,password);
            }
            else if(authMethods.Contains("CRAM-MD5")){
                return new AUTH_SASL_Client_CramMd5(userName,password);
            }
            else if(authMethods.Contains("LOGIN")){
                return new AUTH_SASL_Client_Login(userName,password);
            }
            else if(authMethods.Contains("PLAIN")){
                return new AUTH_SASL_Client_Plain(userName,password);
            }
            else{
                throw new NotSupportedException("We don't support any of the SMTP server authentication methods.");
            }
        }

        #endregion

        #region method Auth

        /// <summary>
        /// Sends AUTH command to SMTP server.
        /// </summary>
        /// <param name="sasl">SASL authentication. You can use method <see cref="AuthGetStrongestMethod"/> to get strongest supported authentication.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected or is already authenticated.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
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

            ManualResetEvent wait = new ManualResetEvent(false);
            using(AuthAsyncOP op = new AuthAsyncOP(sasl)){
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

        #endregion

        #region method AuthAsync

        #region class AuthAsyncOP

        /// <summary>
        /// This class represents <see cref="SMTP_Client.AuthAsync"/> asynchronous operation.
        /// </summary>
        public class AuthAsyncOP : IDisposable,IAsyncOP
        {
            private object           m_pLock         = new object();
            private AsyncOP_State    m_State         = AsyncOP_State.WaitingForStart;
            private Exception        m_pException    = null;
            private SMTP_Client      m_pSmtpClient   = null;
            private AUTH_SASL_Client m_pSASL         = null;
            private bool             m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="sasl">SASL authentication. You can use method <see cref="AuthGetStrongestMethod"/> to get strongest supported authentication.</param>
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
                m_pSmtpClient = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner SMTP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(SMTP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pSmtpClient = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 4954 4. The AUTH Command.

                        AUTH mechanism [initial-response]

                        Arguments:
                            mechanism: A string identifying a [SASL] authentication mechanism.

                            initial-response: An optional initial client response.  If
                            present, this response MUST be encoded as described in Section
                            4 of [BASE64] or contain a single character "=".
                    */

                    if(m_pSASL.SupportsInitialResponse){
                        byte[] buffer = Encoding.UTF8.GetBytes("AUTH " + m_pSASL.Name + " " + Convert.ToBase64String(m_pSASL.Continue(null)) + "\r\n");

                        // Log
                        m_pSmtpClient.LogAddWrite(buffer.Length,Encoding.UTF8.GetString(buffer).TrimEnd());

                        // Start command sending.
                        m_pSmtpClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.AuthCommandSendingCompleted,null);
                    }
                    else{
                        byte[] buffer = Encoding.UTF8.GetBytes("AUTH " + m_pSASL.Name + "\r\n");

                        // Log
                        m_pSmtpClient.LogAddWrite(buffer.Length,"AUTH " + m_pSASL.Name);

                        // Start command sending.
                        m_pSmtpClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.AuthCommandSendingCompleted,null);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
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
                    m_pSmtpClient.TcpStream.EndWrite(ar);

                    // Read SMTP server response.
                    ReadResponseAsyncOP readResponseOP = new ReadResponseAsyncOP();
                    readResponseOP.CompletedAsync += delegate(object s,EventArgs<ReadResponseAsyncOP> e){
                        AuthReadResponseCompleted(readResponseOP);
                    };
                    if(!m_pSmtpClient.ReadResponseAsync(readResponseOP)){
                        AuthReadResponseCompleted(readResponseOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method AuthReadResponseCompleted

            /// <summary>
            /// Is called when SMTP server response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void AuthReadResponseCompleted(ReadResponseAsyncOP op)
            {
                try{
                    // Continue authenticating.
                    if(op.ReplyLines[0].ReplyCode == 334){
                        // 334 base64Data, we need to decode it.
                        byte[] serverResponse = Convert.FromBase64String(op.ReplyLines[0].Text);

                        byte[] clientResponse = m_pSASL.Continue(serverResponse);

                        // We need just send SASL returned auth-response as base64.
                        byte[] buffer = Encoding.UTF8.GetBytes(Convert.ToBase64String(clientResponse) + "\r\n");
                        
                        // Log
                        m_pSmtpClient.LogAddWrite(buffer.Length,Convert.ToBase64String(clientResponse));

                        // Start auth-data sending.
                        m_pSmtpClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.AuthCommandSendingCompleted,null);
                    }
                    // Authentication suceeded.
                    else if(op.ReplyLines[0].ReplyCode == 235){
                        m_pSmtpClient.m_pAuthdUserIdentity = new GenericIdentity(m_pSASL.UserName,m_pSASL.Name);

                        SetState(AsyncOP_State.Completed);
                    }
                    // Authentication rejected.
                    else{
                        m_pException = new SMTP_ClientException(op.ReplyLines);
                        SetState(AsyncOP_State.Completed);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
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
        /// Starts sending AUTH command to SMTP server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="AuthAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected or connection is already authenticated.</exception>
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

        #region method MailFrom

        /// <summary>
        /// Sends MAIL command to SMTP server.
        /// </summary>
        /// <param name="from">Sender email address. Value null means no sender info.</param>
        /// <param name="messageSize">Message size in bytes. Value -1 means that message size unknown.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        /// <remarks>Before using <b>ret</b> or <b>envid</b> arguments, check that remote server supports(SMTP_Client.EsmtpFeatures) SMTP DSN extention.</remarks>
        public void MailFrom(string from,long messageSize)
        {
            MailFrom(from,messageSize,SMTP_DSN_Ret.NotSpecified,null);
        }

        /// <summary>
        /// Sends MAIL command to SMTP server.
        /// </summary>
        /// <param name="from">Sender email address. Value null means no sender info.</param>
        /// <param name="messageSize">Message size in bytes. Value -1 means that message size unknown.</param>
        /// <param name="ret">Delivery satus notification(DSN) RET value. For more info see RFC 3461.</param>
        /// <param name="envid">Delivery satus notification(DSN) ENVID value. Value null means not specified. For more info see RFC 3461.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        /// <remarks>Before using <b>ret</b> or <b>envid</b> arguments, check that remote server supports(SMTP_Client.EsmtpFeatures) SMTP DSN extention.</remarks>
        public void MailFrom(string from,long messageSize,SMTP_DSN_Ret ret,string envid)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            
            ManualResetEvent wait = new ManualResetEvent(false);
            using(MailFromAsyncOP op = new MailFromAsyncOP(from,messageSize,ret,envid)){
                op.CompletedAsync += delegate(object s1,EventArgs<MailFromAsyncOP> e1){
                    wait.Set();
                };
                if(!this.MailFromAsync(op)){
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

        #region method MailFromAsync

        #region class MailFromAsyncOP

        /// <summary>
        /// This class represents <see cref="SMTP_Client.MailFromAsync"/> asynchronous operation.
        /// </summary>
        public class MailFromAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock         = new object();
            private AsyncOP_State m_State         = AsyncOP_State.WaitingForStart;
            private Exception     m_pException    = null;
            private string        m_MailFrom      = null;
            private long          m_MessageSize   = -1;
            private SMTP_DSN_Ret  m_DsnRet        = SMTP_DSN_Ret.NotSpecified;
            private string        m_EnvID         = null;
            private SMTP_Client   m_pSmtpClient   = null;
            private bool          m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="from">Sender email address. Value null means no sender info.</param>
            /// <param name="messageSize">Message size in bytes. Value -1 means that message size unknown.</param>
            public MailFromAsyncOP(string from,long messageSize) : this(from,messageSize,SMTP_DSN_Ret.NotSpecified,null)
            {
            }

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="from">Sender email address. Value null means no sender info.</param>
            /// <param name="messageSize">Message size in bytes. Value -1 means that message size unknown.</param>
            /// <param name="ret">Delivery satus notification(DSN) RET value. For more info see RFC 3461.</param>
            /// <param name="envid">Delivery satus notification(DSN) ENVID value. Value null means not specified. For more info see RFC 3461.</param>
            /// <remarks>Before using <b>ret</b> or <b>envid</b> arguments, check that remote server supports(SMTP_Client.EsmtpFeatures) SMTP DSN extention.</remarks>
            public MailFromAsyncOP(string from,long messageSize,SMTP_DSN_Ret ret,string envid)
            {
                m_MailFrom    = from;
                m_MessageSize = messageSize;
                m_DsnRet      = ret;
                m_EnvID       = envid;
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
                m_MailFrom    = null;
                m_EnvID       = null;
                m_pSmtpClient = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner SMTP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(SMTP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pSmtpClient = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 5321 4.1.1.2. MAIL
			            mail         = "MAIL FROM:" Reverse-path [SP Mail-parameters] CRLF
                        Reverse-path = Path / "<>"
                        Path         = "<" [ A-d-l ":" ] Mailbox ">"

			  
			           RFC 1870 adds optional SIZE keyword support.
			                SIZE keyword may only be used if it's reported in EHLO command response.
			 	        Examples:
			 		        MAIL FROM:<ivx@lumisoft.ee> SIZE=1000
             
                       RFC 3461 adds RET and ENVID paramters.
			        */

                    bool isSizeSupported = false;
                    foreach(string feature in m_pSmtpClient.EsmtpFeatures){
                        if(feature.ToLower().StartsWith("size ")){
                            isSizeSupported = true;

                            break;
                        }
                    }

                    // Build command.
                    StringBuilder cmd = new StringBuilder();
                    cmd.Append("MAIL FROM:<" + m_MailFrom + ">");
                    if(isSizeSupported && m_MessageSize > 0){
                        cmd.Append(" SIZE=" + m_MessageSize.ToString());
                    }
                    if(m_DsnRet == SMTP_DSN_Ret.FullMessage){
                        cmd.Append(" RET=FULL");
                    }
                    else if(m_DsnRet == SMTP_DSN_Ret.Headers){
                        cmd.Append(" RET=HDRS");
                    }
                    if(!string.IsNullOrEmpty(m_EnvID)){
                        cmd.Append(" ENVID=" + m_EnvID);
                    }

                    byte[] buffer = Encoding.UTF8.GetBytes(cmd.ToString() + "\r\n");

                    // Log
                    m_pSmtpClient.LogAddWrite(buffer.Length,cmd.ToString());

                    // Start command sending.
                    m_pSmtpClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.MailCommandSendingCompleted,null);                    
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
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

            #region method MailCommandSendingCompleted

            /// <summary>
            /// Is called when MAIL command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void MailCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pSmtpClient.TcpStream.EndWrite(ar);

                    // Read SMTP server response.
                    ReadResponseAsyncOP readResponseOP = new ReadResponseAsyncOP();
                    readResponseOP.CompletedAsync += delegate(object s,EventArgs<ReadResponseAsyncOP> e){
                        MailReadResponseCompleted(readResponseOP);
                    };
                    if(!m_pSmtpClient.ReadResponseAsync(readResponseOP)){
                        MailReadResponseCompleted(readResponseOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method MailReadResponseCompleted

            /// <summary>
            /// Is called when SMTP server MAIL command response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void MailReadResponseCompleted(ReadResponseAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    }
                    else{
                        // MAIL succeeded.
                        if(op.ReplyLines[0].ReplyCode == 250){
                            m_pSmtpClient.m_MailFrom = m_MailFrom;
                        }
                        // MAIL failed.
                        else{
                            m_pException = new SMTP_ClientException(op.ReplyLines);
                            m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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
            public event EventHandler<EventArgs<MailFromAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<MailFromAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending MAIL command to SMTP server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="MailFromAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <remarks>Before using <b>ret</b> or <b>envid</b> arguments, check that remote server supports(SMTP_Client.EsmtpFeatures) SMTP DSN extention.</remarks>
        public bool MailFromAsync(MailFromAsyncOP op)
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

        #region method RcptTo

        /// <summary>
        /// Sends RCPT TO: command to SMTP server.
        /// </summary>
        /// <param name="to">Recipient email address.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        /// <remarks>Before using <b>notify</b> or <b>orcpt</b> arguments, check that remote server supports(SMTP_Client.EsmtpFeatures) SMTP DSN extention.</remarks>
        public void RcptTo(string to)
        {
            RcptTo(to,SMTP_DSN_Notify.NotSpecified,null);
        }

        /// <summary>
        /// Sends RCPT TO: command to SMTP server.
        /// </summary>
        /// <param name="to">Recipient email address.</param>
        /// <param name="notify">Delivery satus notification(DSN) NOTIFY value. For more info see RFC 3461.</param>
        /// <param name="orcpt">Delivery satus notification(DSN) ORCPT value. Value null means not specified. For more info see RFC 3461.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        /// <remarks>Before using <b>notify</b> or <b>orcpt</b> arguments, check that remote server supports(SMTP_Client.EsmtpFeatures) SMTP DSN extention.</remarks>
        public void RcptTo(string to,SMTP_DSN_Notify notify,string orcpt)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            
            ManualResetEvent wait = new ManualResetEvent(false);
            using(RcptToAsyncOP op = new RcptToAsyncOP(to,notify,orcpt)){
                op.CompletedAsync += delegate(object s1,EventArgs<RcptToAsyncOP> e1){
                    wait.Set();
                };
                if(!this.RcptToAsync(op)){
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

        #region method RcptToAsync

        #region class RcptToAsyncOP

        /// <summary>
        /// This class represents <see cref="SMTP_Client.RcptToAsync"/> asynchronous operation.
        /// </summary>
        public class RcptToAsyncOP : IDisposable,IAsyncOP
        {
            private object          m_pLock         = new object();
            private AsyncOP_State   m_State         = AsyncOP_State.WaitingForStart;
            private Exception       m_pException    = null;
            private string          m_To            = null;
            private SMTP_DSN_Notify m_DsnNotify     = SMTP_DSN_Notify.NotSpecified;
            private string          m_ORcpt         = null;
            private SMTP_Client     m_pSmtpClient   = null;
            private bool            m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="to">Recipient email address.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>to</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public RcptToAsyncOP(string to) : this(to,SMTP_DSN_Notify.NotSpecified,null)
            {
            }

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="to">Recipient email address.</param>
            /// <param name="notify">Delivery satus notification(DSN) NOTIFY value. For more info see RFC 3461.</param>
            /// <param name="orcpt">Delivery satus notification(DSN) ORCPT value. Value null means not specified. For more info see RFC 3461.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>to</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public RcptToAsyncOP(string to,SMTP_DSN_Notify notify,string orcpt)
            {
                if(to == null){
                    throw new ArgumentNullException("to");
                }
                if(to == string.Empty){
                    throw new ArgumentException("Argument 'to' value must be specified.","to");
                }

                m_To        = to;
                m_DsnNotify = notify;
                m_ORcpt     = orcpt;
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
                m_To          = null;
                m_ORcpt       = null;
                m_pSmtpClient = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner SMTP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(SMTP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pSmtpClient = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 5321 4.1.1.3. RCPT.
                        rcpt = "RCPT TO:" ( "<Postmaster@" Domain ">" / "<Postmaster>" / Forward-path ) [SP Rcpt-parameters] CRLF

			            Examples:
			 		        RCPT TO:<ivar@lumisoft.ee>
             
                        RFC 3461 adds NOTIFY and ORCPT parameters.
			        */

                    // Build command.
                    StringBuilder cmd = new StringBuilder();
                    cmd.Append("RCPT TO:<" + m_To + ">");            
                    if(m_DsnNotify == SMTP_DSN_Notify.NotSpecified){
                    }
                    else if(m_DsnNotify == SMTP_DSN_Notify.Never){
                        cmd.Append(" NOTIFY=NEVER");
                    }
                    else{
                        bool first = true;                
                        if((m_DsnNotify & SMTP_DSN_Notify.Delay) != 0){
                            cmd.Append(" NOTIFY=DELAY");
                            first = false;
                        }
                        if((m_DsnNotify & SMTP_DSN_Notify.Failure) != 0){
                            if(first){
                                cmd.Append(" NOTIFY=FAILURE");   
                            }
                            else{
                                cmd.Append(",FAILURE");
                            }
                            first = false;
                        }
                        if((m_DsnNotify & SMTP_DSN_Notify.Success) != 0){
                            if(first){
                                cmd.Append(" NOTIFY=SUCCESS");   
                            }
                            else{
                                cmd.Append(",SUCCESS");
                            }
                            first = false;
                        }
                    }
                    if(!string.IsNullOrEmpty(m_ORcpt)){
                        cmd.Append(" ORCPT=" + m_ORcpt);
                    }

                    byte[] buffer = Encoding.UTF8.GetBytes(cmd.ToString() + "\r\n");

                    // Log
                    m_pSmtpClient.LogAddWrite(buffer.Length,cmd.ToString());

                    // Start command sending.
                    m_pSmtpClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.RcptCommandSendingCompleted,null);                    
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
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

            #region method MailCommandSendingCompleted

            /// <summary>
            /// Is called when RCPT command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void RcptCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pSmtpClient.TcpStream.EndWrite(ar);

                    // Read SMTP server response.
                    ReadResponseAsyncOP readResponseOP = new ReadResponseAsyncOP();
                    readResponseOP.CompletedAsync += delegate(object s,EventArgs<ReadResponseAsyncOP> e){
                        RcptReadResponseCompleted(readResponseOP);
                    };
                    if(!m_pSmtpClient.ReadResponseAsync(readResponseOP)){
                        RcptReadResponseCompleted(readResponseOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method RcptReadResponseCompleted

            /// <summary>
            /// Is called when SMTP server RCPT command response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void RcptReadResponseCompleted(ReadResponseAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    }
                    else{
                        // RCPT succeeded.
                        if(op.ReplyLines[0].ReplyCode == 250){
                            if(!m_pSmtpClient.m_pRecipients.Contains(m_To)){
                                m_pSmtpClient.m_pRecipients.Add(m_To);
                            }
                        }
                        // RCPT failed.
                        else{
                            m_pException = new SMTP_ClientException(op.ReplyLines);
                            m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
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
            public event EventHandler<EventArgs<RcptToAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<RcptToAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending RCPT command to SMTP server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="RcptToAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        /// <remarks>Before using <b>notify</b> or <b>orcpt</b> arguments, check that remote server supports(SMTP_Client.EsmtpFeatures) SMTP DSN extention.</remarks>
        public bool RcptToAsync(RcptToAsyncOP op)
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
 
        #region method SendMessage

        /// <summary>
        /// Sends raw message to SMTP server.
        /// </summary>
        /// <param name="stream">Message stream. Sending starts from stream current position.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        /// <remarks>The stream must contain data in MIME format, other formats normally are rejected by SMTP server.</remarks>
        public void SendMessage(Stream stream)
        {
            this.SendMessage(stream,false);
        }

        /// <summary>
        /// Sends raw message to SMTP server.
        /// </summary>
        /// <param name="stream">Message stream. Sending starts from stream current position.</param>
        /// <param name="useBdatIfPossibe">Specifies if BDAT command is used to send message, if remote server supports it.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        /// <remarks>The stream must contain data in MIME format, other formats normally are rejected by SMTP server.</remarks>
        public void SendMessage(Stream stream,bool useBdatIfPossibe)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }

            ManualResetEvent wait = new ManualResetEvent(false);
            using(SendMessageAsyncOP op = new SendMessageAsyncOP(stream,useBdatIfPossibe)){
                op.CompletedAsync += delegate(object s1,EventArgs<SendMessageAsyncOP> e1){
                    wait.Set();
                };
                if(!this.SendMessageAsync(op)){
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

        #region method SendMessageAsync

        #region class SendMessageAsyncOP

        /// <summary>
        /// This class represents <see cref="SMTP_Client.SendMessageAsync"/> asynchronous operation.
        /// </summary>
        public class SendMessageAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock             = new object();
            private AsyncOP_State m_State             = AsyncOP_State.WaitingForStart;
            private Exception     m_pException        = null;
            private Stream        m_pStream           = null;
            private bool          m_UseBdat           = false;
            private SMTP_Client   m_pSmtpClient       = null;
            private byte[]        m_pBdatBuffer       = null;
            private int           m_BdatBytesInBuffer = 0;
            private byte[]        m_BdatSendBuffer    = null;
            private bool          m_RiseCompleted     = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="stream">Message stream. Message sending starts from <b>stream</b> current position and all stream data will be sent.</param>
            /// <param name="useBdatIfPossibe">Specifies if BDAT command is used to send message, if remote server supports it.</param>
            public SendMessageAsyncOP(Stream stream,bool useBdatIfPossibe)
            {
                if(stream == null){
                    throw new ArgumentNullException("stream");
                }

                m_pStream = stream;
                m_UseBdat = useBdatIfPossibe;
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
                m_pStream        = null;
                m_pSmtpClient    = null;
                m_pBdatBuffer    = null;
                m_BdatSendBuffer = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner SMTP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(SMTP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pSmtpClient = owner;

                SetState(AsyncOP_State.Active);

                try{
                    // See if BDAT supported.
                    bool bdatSupported = false;
                    foreach(string feature in m_pSmtpClient.EsmtpFeatures){
                        if(feature.ToUpper() == SMTP_ServiceExtensions.CHUNKING){
                            bdatSupported = true;

                            break;
                        }
                    }
            
                    // BDAT.
                    if(bdatSupported && m_UseBdat){
                        /* RFC 3030 2.
                            bdat-cmd   ::= "BDAT" SP chunk-size [ SP end-marker ] CR LF
                            chunk-size ::= 1*DIGIT
                            end-marker ::= "LAST"
                        */

                        m_pBdatBuffer    = new byte[64000];
                        m_BdatSendBuffer = new byte[64100]; // 100 bytes for "BDAT xxxxxx...CRLF"

                        // Start reading message data-block.
                        m_pStream.BeginRead(m_pBdatBuffer,0,m_pBdatBuffer.Length,this.BdatChunkReadingCompleted,null);
                    }
                    // DATA.
                    else{
                        /* RFC 5321 4.1.1.4.
                            The mail data are terminated by a line containing only a period, that
                            is, the character sequence "<CRLF>.<CRLF>", where the first <CRLF> is
                            actually the terminator of the previous line.
                          
                            Examples:
			 		            C: DATA<CRLF>
			 		            S: 354 Start sending message, end with <crlf>.<crlf>.<CRLF>
			 		            C: send_message
			 		            C: .<CRLF>
                                S: 250 Ok<CRLF>
                        */

                        byte[] buffer = Encoding.UTF8.GetBytes("DATA\r\n");

                        // Log
                        m_pSmtpClient.LogAddWrite(buffer.Length,"DATA");

                        // Start command sending.
                        m_pSmtpClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.DataCommandSendingCompleted,null);
                    }                    
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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

            #region method BdatChunkReadingCompleted

            /// <summary>
            /// Is called when message data block for BDAT reading has completed.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void BdatChunkReadingCompleted(IAsyncResult ar)
            {
                try{
                    m_BdatBytesInBuffer = m_pStream.EndRead(ar);

                    /* RFC 3030 2.
                        bdat-cmd   ::= "BDAT" SP chunk-size [ SP end-marker ] CR LF
                        chunk-size ::= 1*DIGIT
                        end-marker ::= "LAST"
                    */

                    // Send data chunk.
                    if(m_BdatBytesInBuffer > 0){
                        byte[] buffer = Encoding.UTF8.GetBytes("BDAT " + m_BdatBytesInBuffer + "\r\n");

                        // Log
                        m_pSmtpClient.LogAddWrite(buffer.Length,"BDAT " + m_BdatBytesInBuffer);
                        m_pSmtpClient.LogAddWrite(m_BdatBytesInBuffer,"<BDAT data-chunk of " + m_BdatBytesInBuffer + " bytes>");

                        // Copy data to send buffer.(BDAT xxxCRLF<xxx-bytes>).
                        Array.Copy(buffer,m_BdatSendBuffer,buffer.Length);
                        Array.Copy(m_pBdatBuffer,0,m_BdatSendBuffer,buffer.Length,m_BdatBytesInBuffer);

                        // Start command sending.
                        m_pSmtpClient.TcpStream.BeginWrite(m_BdatSendBuffer,0,buffer.Length + m_BdatBytesInBuffer,this.BdatCommandSendingCompleted,null);
                    }
                    // EOS, we readed all message data.
                    else{
                        byte[] buffer = Encoding.UTF8.GetBytes("BDAT 0 LAST\r\n");

                        // Log
                        m_pSmtpClient.LogAddWrite(buffer.Length,"BDAT 0 LAST");

                        // Start command sending.
                        m_pSmtpClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.BdatCommandSendingCompleted,null);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method BdatCommandSendingCompleted

            /// <summary>
            /// Is called when BDAT command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void BdatCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pSmtpClient.TcpStream.EndWrite(ar);

                    // Read BDAT command response.
                    ReadResponseAsyncOP readResponseOP = new ReadResponseAsyncOP();
                    readResponseOP.CompletedAsync += delegate(object s,EventArgs<ReadResponseAsyncOP> e){
                        BdatReadResponseCompleted(readResponseOP);
                    };
                    if(!m_pSmtpClient.ReadResponseAsync(readResponseOP)){
                        BdatReadResponseCompleted(readResponseOP);
                    }          
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);   
                }
            }

            #endregion

            #region method BdatReadResponseCompleted

            /// <summary>
            /// Is called when SMTP server BDAT command response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void BdatReadResponseCompleted(ReadResponseAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                        SetState(AsyncOP_State.Completed);
                    }
                    else{
                        // BDAT succeeded.
                        if(op.ReplyLines[0].ReplyCode == 250){ 
                            // We have sent whole message, we are done.
                            if(m_BdatBytesInBuffer == 0){
                                SetState(AsyncOP_State.Completed);

                                return;
                            }
                            // Send next BDAT data-chunk.
                            else{
                                // Start reading next message data-block.
                                m_pStream.BeginRead(m_pBdatBuffer,0,m_pBdatBuffer.Length,this.BdatChunkReadingCompleted,null);
                            }
                        }
                        // BDAT failed.
                        else{
                            m_pException = new SMTP_ClientException(op.ReplyLines);

                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    SetState(AsyncOP_State.Completed);
                }

                op.Dispose();
            }

            #endregion

            #region method DataCommandSendingCompleted

            /// <summary>
            /// Is called when DATA command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void DataCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pSmtpClient.TcpStream.EndWrite(ar);

                    // Read DATA command response.
                    ReadResponseAsyncOP readResponseOP = new ReadResponseAsyncOP();
                    readResponseOP.CompletedAsync += delegate(object s,EventArgs<ReadResponseAsyncOP> e){
                        DataReadResponseCompleted(readResponseOP);
                    };
                    if(!m_pSmtpClient.ReadResponseAsync(readResponseOP)){
                        DataReadResponseCompleted(readResponseOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);    
                }
            }

            #endregion

            #region method DataReadResponseCompleted

            /// <summary>
            /// Is called when SMTP server DATA command initial response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void DataReadResponseCompleted(ReadResponseAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                        SetState(AsyncOP_State.Completed);
                    }
                    else{
                        // DATA command succeeded.
                        if(op.ReplyLines[0].ReplyCode == 354){ 
                            // Start sending message.
                            SmartStream.WritePeriodTerminatedAsyncOP sendMsgOP = new SmartStream.WritePeriodTerminatedAsyncOP(m_pStream);
                            sendMsgOP.CompletedAsync += delegate(object s,EventArgs<SmartStream.WritePeriodTerminatedAsyncOP> e){
                                DataMsgSendingCompleted(sendMsgOP);
                            };
                            if(!m_pSmtpClient.TcpStream.WritePeriodTerminatedAsync(sendMsgOP)){
                                DataMsgSendingCompleted(sendMsgOP);
                            }                            
                        }
                        // DATA command failed.
                        else{
                            m_pException = new SMTP_ClientException(op.ReplyLines);
                            SetState(AsyncOP_State.Completed);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    SetState(AsyncOP_State.Completed);
                }

                op.Dispose();
            }

            #endregion

            #region method DataMsgSendingCompleted

            /// <summary>
            /// Is called when DATA command message sending has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void DataMsgSendingCompleted(SmartStream.WritePeriodTerminatedAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                        SetState(AsyncOP_State.Completed);
                    }
                    else{
                        // Log
                        m_pSmtpClient.LogAddWrite(op.BytesWritten,"Sent message " + op.BytesWritten + " bytes.");
                                                                       
                        // Read DATA command final response.
                        ReadResponseAsyncOP readResponseOP = new ReadResponseAsyncOP();
                        readResponseOP.CompletedAsync += delegate(object s,EventArgs<ReadResponseAsyncOP> e){
                            DataReadFinalResponseCompleted(readResponseOP);
                        };
                        if(!m_pSmtpClient.ReadResponseAsync(readResponseOP)){
                            DataReadFinalResponseCompleted(readResponseOP);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    SetState(AsyncOP_State.Completed);
                }

                op.Dispose();
            }

            #endregion

            #region method DataReadFinalResponseCompleted

            /// <summary>
            /// Is called when SMTP server DATA command final response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void DataReadFinalResponseCompleted(ReadResponseAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                        SetState(AsyncOP_State.Completed);
                    }
                    else{
                        // DATA command failed, only 2xx response is success.
                        if(op.ReplyLines[0].ReplyCode < 200 || op.ReplyLines[0].ReplyCode > 299){
                            m_pException = new SMTP_ClientException(op.ReplyLines);
                        }

                        SetState(AsyncOP_State.Completed);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
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
            public event EventHandler<EventArgs<SendMessageAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<SendMessageAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending message to SMTP server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="SendMessageAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool SendMessageAsync(SendMessageAsyncOP op)
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

        #region method Rset

        /// <summary>
        /// Send RSET command to server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        public void Rset()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }

            ManualResetEvent wait = new ManualResetEvent(false);
            using(RsetAsyncOP op = new RsetAsyncOP()){
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

        #endregion

        #region method RsetAsync

        #region class RsetAsyncOP

        /// <summary>
        /// This class represents <see cref="SMTP_Client.RsetAsync"/> asynchronous operation.
        /// </summary>
        public class RsetAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock         = new object();
            private AsyncOP_State m_State         = AsyncOP_State.WaitingForStart;
            private Exception     m_pException    = null;
            private SMTP_Client   m_pSmtpClient   = null;
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
                m_pSmtpClient = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner SMTP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(SMTP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pSmtpClient = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 5321 4.1.1.5.
                        rset = "REST" CRLF
                    */

                    byte[] buffer = Encoding.UTF8.GetBytes("RSET\r\n");

                    // Log
                    m_pSmtpClient.LogAddWrite(buffer.Length,"RSET");

                    // Start command sending.
                    m_pSmtpClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.RsetCommandSendingCompleted,null);
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
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
                    m_pSmtpClient.TcpStream.EndWrite(ar);

                    // Read SMTP server response.
                    ReadResponseAsyncOP readResponseOP = new ReadResponseAsyncOP();
                    readResponseOP.CompletedAsync += delegate(object s,EventArgs<ReadResponseAsyncOP> e){
                        RsetReadResponseCompleted(readResponseOP);
                    };
                    if(!m_pSmtpClient.ReadResponseAsync(readResponseOP)){
                        RsetReadResponseCompleted(readResponseOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method RsetReadResponseCompleted

            /// <summary>
            /// Is called when SMTP server RSET command response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void RsetReadResponseCompleted(ReadResponseAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    }
                    else{
                        // RSET succeeded.
                        if(op.ReplyLines[0].ReplyCode == 250){
                            /* RFC 5321 4.1.1.9.
                                rset      = "RSET" CRLF
                                rset-resp = "250 OK" CRLF
                            */

                            // Do nothing.
                        }
                        // RSET failed.
                        else{
                            m_pException = new SMTP_ClientException(op.ReplyLines);
                            m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
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
        /// Starts sending RSET command to SMTP server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="RsetAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool RsetAsync(RsetAsyncOP op)
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
                
        #region method Noop

        /// <summary>
        /// Send NOOP command to server. This method can be used for keeping connection alive(not timing out).
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        public void Noop()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }

            ManualResetEvent wait = new ManualResetEvent(false);
            using(NoopAsyncOP op = new NoopAsyncOP()){
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

        #endregion
        
        #region method NoopAsync

        #region class NoopAsyncOP

        /// <summary>
        /// This class represents <see cref="SMTP_Client.NoopAsync"/> asynchronous operation.
        /// </summary>
        public class NoopAsyncOP : IDisposable,IAsyncOP
        {
            private object        m_pLock         = new object();
            private AsyncOP_State m_State         = AsyncOP_State.WaitingForStart;
            private Exception     m_pException    = null;
            private SMTP_Client   m_pSmtpClient   = null;
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
                m_pSmtpClient = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner SMTP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(SMTP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pSmtpClient = owner;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 5321 4.1.1.9.
                        noop = "NOOP" [ SP String ] CRLF
                    */

                    byte[] buffer = Encoding.UTF8.GetBytes("NOOP\r\n");

                    // Log
                    m_pSmtpClient.LogAddWrite(buffer.Length,"NOOP");

                    // Start command sending.
                    m_pSmtpClient.TcpStream.BeginWrite(buffer,0,buffer.Length,this.NoopCommandSendingCompleted,null);                    
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
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
                    m_pSmtpClient.TcpStream.EndWrite(ar);

                    // Read SMTP server response.
                    ReadResponseAsyncOP readResponseOP = new ReadResponseAsyncOP();
                    readResponseOP.CompletedAsync += delegate(object s,EventArgs<ReadResponseAsyncOP> e){
                        NoopReadResponseCompleted(readResponseOP);
                    };
                    if(!m_pSmtpClient.ReadResponseAsync(readResponseOP)){
                        NoopReadResponseCompleted(readResponseOP);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method NoopReadResponseCompleted

            /// <summary>
            /// Is called when NOOP command response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private void NoopReadResponseCompleted(ReadResponseAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    if(op.Error != null){
                        m_pException = op.Error;
                        m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                    }
                    else{
                        // NOOP succeeded.
                        if(op.ReplyLines[0].ReplyCode == 250){
                            /* RFC 5321 4.1.1.9.
                                noop      = "NOOP" [ SP String ] CRLF
                                noop-resp = "250 OK" CRLF
                            */

                            // Do nothing.
                        }
                        // NOOP failed.
                        else{
                            m_pException = new SMTP_ClientException(op.ReplyLines);
                            m_pSmtpClient.LogAddException("Exception: " + m_pException.Message,m_pException);
                        }
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pSmtpClient.LogAddException("Exception: " + x.Message,x);
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
        /// Starts sending NOOP command to SMTP server. This method can be used for keeping connection alive(not timing out).
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="NoopAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool NoopAsync(NoopAsyncOP op)
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
            // Read SMTP server greeting response.
            ReadResponseAsyncOP readGreetingOP = new ReadResponseAsyncOP();
            readGreetingOP.CompletedAsync += delegate(object s,EventArgs<ReadResponseAsyncOP> e){
                ReadServerGreetingCompleted(readGreetingOP,callback);
            };
            if(!ReadResponseAsync(readGreetingOP)){
                ReadServerGreetingCompleted(readGreetingOP,callback);
            }
        }

        #endregion

        #region method ReadServerGreetingCompleted

        /// <summary>
        /// Is called when SMTP server greeting reading has completed.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <param name="connectCallback">Callback to be called to complete connect operation.</param>
        private void ReadServerGreetingCompleted(ReadResponseAsyncOP op,CompleteConnectCallback connectCallback)
        {
            Exception error = null;

            try{
                // Greeting reading failed, we are done.
                if(op.Error != null){
                    error = op.Error;
                }
                // Greeting reading succeded.
                else{
                    /* RFC 5321 4.2.
                        Greeting = ( "220 " (Domain / address-literal) [ SP textstring ] CRLF ) /
                                   ( "220-" (Domain / address-literal) [ SP textstring ] CRLF
                                  *( "220-" [ textstring ] CRLF )
                                     "220" [ SP textstring ] CRLF )

                    */

                    // SMTP server accepted connection, get greeting text.
                    if(op.ReplyLines[0].ReplyCode == 220){
                        StringBuilder greetingText = new StringBuilder();
                        foreach(SMTP_t_ReplyLine line in op.ReplyLines){
                            greetingText.AppendLine(line.Text);
                        }

                        m_GreetingText = greetingText.ToString();
                        m_pEsmtpFeatures = new List<string>();
                        m_pRecipients = new List<string>();
                    }
                    // SMTP server rejected connection.
                    else{
                        error = new SMTP_ClientException(op.ReplyLines);
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


        #region method ReadResponseAsync

        #region class ReadResponseAsyncOP

        /// <summary>
        /// This class represents <see cref="SMTP_Client.ReadResponseAsync"/> asynchronous operation.
        /// </summary>
        private class ReadResponseAsyncOP : IDisposable,IAsyncOP
        {
            private AsyncOP_State          m_State       = AsyncOP_State.WaitingForStart;
            private Exception              m_pException  = null;
            private SMTP_Client            m_pSmtpClient = null;
            private List<SMTP_t_ReplyLine> m_pReplyLines = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public ReadResponseAsyncOP()
            {
                m_pReplyLines = new List<SMTP_t_ReplyLine>();
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
                m_pSmtpClient = null;
                m_pReplyLines = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner SMTP client.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(SMTP_Client owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pSmtpClient = owner;

                try{
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){   
                        try{
                            // Response reading completed.
                            if(!ReadLineCompleted(op)){
                                SetState(AsyncOP_State.Completed);                        
                                OnCompletedAsync();
                            }
                            // Continue response reading.
                            else{
                                while(owner.TcpStream.ReadLine(op,true)){
                                    // Response reading completed.
                                    if(!ReadLineCompleted(op)){
                                        SetState(AsyncOP_State.Completed);                        
                                        OnCompletedAsync();

                                        break;
                                    }
                                }
                            }
                        }
                        catch(Exception x){
                            m_pException = x;
                            SetState(AsyncOP_State.Completed);                        
                            OnCompletedAsync();
                        }
                    };
                    while(owner.TcpStream.ReadLine(op,true)){
                        // Response reading completed.
                        if(!ReadLineCompleted(op)){
                            SetState(AsyncOP_State.Completed);

                            return false;
                        }                        
                    }

                    return true;
                }
                catch(Exception x){
                    m_pException = x;
                    SetState(AsyncOP_State.Completed);

                    return false;
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
                m_State = state;
            }

            #endregion

            #region method ReadLineCompleted

            /// <summary>
            /// Is called when read line has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            /// <returns>Returns true if multiline response has more response lines.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
            private bool ReadLineCompleted(SmartStream.ReadLineAsyncOP op)
            {
                if(op == null){
                    throw new ArgumentNullException("op");
                }

                try{
                    // Line reading failed, we are done.
                    if(op.Error != null){
                        m_pException = op.Error;
                    }
                    // Line reading succeeded.
                    else{
                        // Log.
                        m_pSmtpClient.LogAddRead(op.BytesInBuffer,op.LineUtf8);

                        SMTP_t_ReplyLine replyLine = SMTP_t_ReplyLine.Parse(op.LineUtf8);
                        m_pReplyLines.Add(replyLine);

                        return !replyLine.IsLastLine;
                    }                    
                }
                catch(Exception x){
                    m_pException = x;
                }

                return false;
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
            /// Gets SMTP server reply-lines.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this property is accessed.</exception>
            /// <exception cref="InvalidOperationException">Is raised when this property is accessed other than <b>AsyncOP_State.Completed</b> state.</exception>
            public SMTP_t_ReplyLine[] ReplyLines
            {
                get{
                    if(m_State == AsyncOP_State.Disposed){
                        throw new ObjectDisposedException(this.GetType().Name);
                    }
                    if(m_State != AsyncOP_State.Completed){
                        throw new InvalidOperationException("Property 'ReplyLines' is accessible only in 'AsyncOP_State.Completed' state.");
                    }
                    if(m_pException != null){
                        throw m_pException;
                    }

                    return m_pReplyLines.ToArray();
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
        /// Reads SMTP server single or multiline response.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="ReadResponseAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        private bool ReadResponseAsync(ReadResponseAsyncOP op)
        {
            if(this.IsDisposed){
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

                
        #region static method QuickSend

        /// <summary>
        /// Sends specified mime message.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>message</b> is null.</exception>
        [Obsolete("Use QuickSend(Mail_Message) instead")]
        public static void QuickSend(LumiSoft.Net.Mime.Mime message)
        {
            if(message == null){
                throw new ArgumentNullException("message");
            }

            string from = "";
            if(message.MainEntity.From != null && message.MainEntity.From.Count > 0){
                from = ((MailboxAddress)message.MainEntity.From[0]).EmailAddress;
            }

            List<string> recipients = new List<string>();
            if(message.MainEntity.To != null){
				MailboxAddress[] addresses = message.MainEntity.To.Mailboxes;				
				foreach(MailboxAddress address in addresses){
					recipients.Add(address.EmailAddress);
				}
			}
			if(message.MainEntity.Cc != null){
				MailboxAddress[] addresses = message.MainEntity.Cc.Mailboxes;				
				foreach(MailboxAddress address in addresses){
					recipients.Add(address.EmailAddress);
				}
			}
			if(message.MainEntity.Bcc != null){
				MailboxAddress[] addresses = message.MainEntity.Bcc.Mailboxes;				
				foreach(MailboxAddress address in addresses){
					recipients.Add(address.EmailAddress);
				}

                // We must hide BCC
                message.MainEntity.Bcc.Clear();
			}

            foreach(string recipient in recipients){
                QuickSend(null,from,recipient,new MemoryStream(message.ToByteData()));
            }
        }

        /// <summary>
        /// Sends specified mime message.
        /// </summary>
        /// <param name="message">Message to send.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>message</b> is null.</exception>
        public static void QuickSend(Mail_Message message)
        {
            if(message == null){
                throw new ArgumentNullException("message");
            }

            string from = "";
            if(message.From != null && message.From.Count > 0){
                from = ((Mail_t_Mailbox)message.From[0]).Address;
            }

            List<string> recipients = new List<string>();
            if(message.To != null){
				Mail_t_Mailbox[] addresses = message.To.Mailboxes;	
				foreach(Mail_t_Mailbox address in addresses){
					recipients.Add(address.Address);
				}
			}
			if(message.Cc != null){
				Mail_t_Mailbox[] addresses = message.Cc.Mailboxes;				
				foreach(Mail_t_Mailbox address in addresses){
					recipients.Add(address.Address);
				}
			}
			if(message.Bcc != null){
				Mail_t_Mailbox[] addresses = message.Bcc.Mailboxes;				
				foreach(Mail_t_Mailbox address in addresses){
					recipients.Add(address.Address);
				}

                // We must hide BCC
                message.Bcc.Clear();
			}

            foreach(string recipient in recipients){
                MemoryStream ms = new MemoryStream();
                message.ToStream(ms,new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.Q,Encoding.UTF8),Encoding.UTF8);
                ms.Position = 0;
                QuickSend(null,from,recipient,ms);
            }
        }

        /// <summary>
        /// Sends message directly to email domain. Domain email sever resolve order: MX recordds -> A reords if no MX.
        /// </summary>
        /// <param name="from">Sender email what is reported to SMTP server.</param>
        /// <param name="to">Recipient email.</param>
        /// <param name="message">Raw message to send.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>from</b>,<b>to</b> or <b>message</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        public static void QuickSend(string from,string to,Stream message)
        {
            QuickSend(null,from,to,message);
        }

        /// <summary>
        /// Sends message directly to email domain. Domain email sever resolve order: MX recordds -> A reords if no MX.
        /// </summary>
        /// <param name="localHost">Host name which is reported to SMTP server.</param>
        /// <param name="from">Sender email what is reported to SMTP server.</param>
        /// <param name="to">Recipient email.</param>
        /// <param name="message">Raw message to send.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>from</b>,<b>to</b> or <b>message</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        public static void QuickSend(string localHost,string from,string to,Stream message)
        {
            if(from == null){
                throw new ArgumentNullException("from");
            }
            if(from != "" && !SMTP_Utils.IsValidAddress(from)){
                throw new ArgumentException("Argument 'from' has invalid value.");
            }
            if(to == null){
                throw new ArgumentNullException("to");
            }
            if(to == ""){
                throw new ArgumentException("Argument 'to' value must be specified.");
            }
            if(!SMTP_Utils.IsValidAddress(to)){
                throw new ArgumentException("Argument 'to' has invalid value.");
            }            
            if(message == null){
                throw new ArgumentNullException("message");
            }

            QuickSendSmartHost(localHost,Dns_Client.Static.GetEmailHosts(to)[0].HostName,25,false,from,new string[]{to},message);
        }

        #endregion

        #region static method QuickSendSmartHost

        /// <summary>
        /// Sends message by using specified smart host.
        /// </summary>
        /// <param name="host">Host name or IP address.</param>
        /// <param name="port">Host port.</param>
        /// <param name="ssl">Specifies if connected via SSL.</param>
        /// <param name="message">Mail message to send.</param>
        /// <exception cref="ArgumentNullException">Is raised when argument <b>host</b> or <b>message</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the method arguments has invalid value.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        public static void QuickSendSmartHost(string host,int port,bool ssl,Mail_Message message)
        {
            if(message == null){
                throw new ArgumentNullException("message");
            }

            string from = "";
            if(message.From != null && message.From.Count > 0){
                from = ((Mail_t_Mailbox)message.From[0]).Address;
            }

            List<string> recipients = new List<string>();
            if(message.To != null){
				Mail_t_Mailbox[] addresses = message.To.Mailboxes;	
				foreach(Mail_t_Mailbox address in addresses){
					recipients.Add(address.Address);
				}
			}
			if(message.Cc != null){
				Mail_t_Mailbox[] addresses = message.Cc.Mailboxes;				
				foreach(Mail_t_Mailbox address in addresses){
					recipients.Add(address.Address);
				}
			}
			if(message.Bcc != null){
				Mail_t_Mailbox[] addresses = message.Bcc.Mailboxes;				
				foreach(Mail_t_Mailbox address in addresses){
					recipients.Add(address.Address);
				}

                // We must hide BCC
                message.Bcc.Clear();
			}

            foreach(string recipient in recipients){
                MemoryStream ms = new MemoryStream();
                message.ToStream(ms,new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.Q,Encoding.UTF8),Encoding.UTF8);
                ms.Position = 0;
                QuickSendSmartHost(null,host,port,ssl,null,null,from,new string[]{recipient},ms);
            }            
        }

        /// <summary>
        /// Sends message by using specified smart host.
        /// </summary>
        /// <param name="host">Host name or IP address.</param>
        /// <param name="port">Host port.</param>
        /// <param name="from">Sender email what is reported to SMTP server.</param>
        /// <param name="to">Recipients email addresses.</param>
        /// <param name="message">Raw message to send.</param>
        /// <exception cref="ArgumentNullException">Is raised when argument <b>host</b>,<b>from</b>,<b>to</b> or <b>message</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the method arguments has invalid value.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        public static void QuickSendSmartHost(string host,int port,string from,string[] to,Stream message)
        {
            QuickSendSmartHost(null,host,port,false,null,null,from,to,message);
        }

        /// <summary>
        /// Sends message by using specified smart host.
        /// </summary>
        /// <param name="host">Host name or IP address.</param>
        /// <param name="port">Host port.</param>
        /// <param name="ssl">Specifies if connected via SSL.</param>
        /// <param name="from">Sender email what is reported to SMTP server.</param>
        /// <param name="to">Recipients email addresses.</param>
        /// <param name="message">Raw message to send.</param>
        /// <exception cref="ArgumentNullException">Is raised when argument <b>host</b>,<b>from</b>,<b>to</b> or <b>stream</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the method arguments has invalid value.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        public static void QuickSendSmartHost(string host,int port,bool ssl,string from,string[] to,Stream message)
        {
            QuickSendSmartHost(null,host,port,ssl,null,null,from,to,message);
        }

        /// <summary>
        /// Sends message by using specified smart host.
        /// </summary>
        /// <param name="localHost">Host name which is reported to SMTP server.</param>
        /// <param name="host">Host name or IP address.</param>
        /// <param name="port">Host port.</param>
        /// <param name="ssl">Specifies if connected via SSL.</param>
        /// <param name="from">Sender email what is reported to SMTP server.</param>
        /// <param name="to">Recipients email addresses.</param>
        /// <param name="message">Raw message to send.</param>
        /// <exception cref="ArgumentNullException">Is raised when argument <b>host</b>,<b>from</b>,<b>to</b> or <b>stream</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the method arguments has invalid value.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        public static void QuickSendSmartHost(string localHost,string host,int port,bool ssl,string from,string[] to,Stream message)
        {
            QuickSendSmartHost(localHost,host,port,ssl,null,null,from,to,message);
        }

        /// <summary>
        /// Sends message by using specified smart host.
        /// </summary>
        /// <param name="localHost">Host name which is reported to SMTP server.</param>
        /// <param name="host">Host name or IP address.</param>
        /// <param name="port">Host port.</param>
        /// <param name="ssl">Specifies if connected via SSL.</param>
        /// <param name="userName">SMTP server user name. This value may be null, then authentication not used.</param>
        /// <param name="password">SMTP server password.</param>
        /// <param name="from">Sender email what is reported to SMTP server.</param>
        /// <param name="to">Recipients email addresses.</param>
        /// <param name="message">Raw message to send.</param>
        /// <exception cref="ArgumentNullException">Is raised when argument <b>host</b>,<b>from</b>,<b>to</b> or <b>stream</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the method arguments has invalid value.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        public static void QuickSendSmartHost(string localHost,string host,int port,bool ssl,string userName,string password,string from,string[] to,Stream message)
        {
            if(host == null){
                throw new ArgumentNullException("host");
            }
            if(host == ""){
                throw new ArgumentException("Argument 'host' value may not be empty.");
            }
            if(port < 1){
                throw new ArgumentException("Argument 'port' value must be >= 1.");
            }
            if(from == null){
                throw new ArgumentNullException("from");
            }
            if(from != "" && !SMTP_Utils.IsValidAddress(from)){
                throw new ArgumentException("Argument 'from' has invalid value.");
            }
            if(to == null){
                throw new ArgumentNullException("to");
            }
            if(to.Length == 0){
                throw new ArgumentException("Argument 'to' must contain at least 1 recipient.");
            }
            foreach(string t in to){
                if(!SMTP_Utils.IsValidAddress(t)){
                    throw new ArgumentException("Argument 'to' has invalid value '" + t + "'.");
                }
            }
            if(message == null){
                throw new ArgumentNullException("message");
            }

            using(SMTP_Client smtp = new SMTP_Client()){
                smtp.Connect(host,port,ssl);                
                smtp.EhloHelo(localHost != null ? localHost : Dns.GetHostName());
                if(!string.IsNullOrEmpty(userName)){
                    smtp.Auth(smtp.AuthGetStrongestMethod(userName,password));
                }
                smtp.MailFrom(from,-1);
                foreach(string t in to){
                    smtp.RcptTo(t);
                }
                smtp.SendMessage(message);
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets host name which is reported to SMTP server. If value null, then local computer name is used.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and SMTP client is connected.</exception>
        public string LocalHostName
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_LocalHostName; 
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(this.IsConnected){
                    throw new InvalidOperationException("Property LocalHostName is available only when SMTP client is not connected.");
                }

                m_LocalHostName = value;
            }
        }

        /// <summary>
        /// Gets SMTP server host name which it reported to us.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and SMTP client is not connected.</exception>
        public string RemoteHostName
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
                    throw new InvalidOperationException("You must connect first.");
                }

                return m_RemoteHostName; 
            }
        }

        /// <summary>
        /// Gets greeting text which was sent by SMTP server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and SMTP client is not connected.</exception>
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
        /// Gets if connected SMTP server suports ESMTP.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and SMTP client is not connected.</exception>
        public bool IsEsmtpSupported
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
                    throw new InvalidOperationException("You must connect first.");
                }

                return m_IsEsmtpSupported; 
            }
        }

        /// <summary>
        /// Gets what ESMTP features are supported by connected SMTP server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and SMTP client is not connected.</exception>
        public string[] EsmtpFeatures
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
                    throw new InvalidOperationException("You must connect first.");
                }

                return m_pEsmtpFeatures.ToArray(); 
            }
        }

        /// <summary>
        /// Gets SMTP server supported SASL authentication method.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and SMTP client is not connected.</exception>
        public string[] SaslAuthMethods
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!this.IsConnected){
                    throw new InvalidOperationException("You must connect first.");
                }

                // Search AUTH entry.
                foreach(string feature in this.EsmtpFeatures){
                    if(feature.ToUpper().StartsWith(SMTP_ServiceExtensions.AUTH)){
                        // Remove AUTH<SP> and split authentication methods.
                        return feature.Substring(4).Trim().Split(' ');
                    }
                }

                return new string[0];
            }
        }

        /// <summary>
        /// Gets maximum message size in bytes what SMTP server accepts. Value null means not known.
        /// </summary>
        public long MaxAllowedMessageSize
        {
            get{ 
                try{
                    foreach(string feature in this.EsmtpFeatures){
                        if(feature.ToUpper().StartsWith(SMTP_ServiceExtensions.SIZE)){
                            return Convert.ToInt64(feature.Split(' ')[1]);
                        }
                    }
                }
                catch{
                    // Never should reach here, skip errors here.
                }

                return 0; 
            }
        }


        /// <summary>
        /// Gets session authenticated user identity, returns null if not authenticated.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when this property is accessed and SMTP client is not connected.</exception>
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

                
        //------- OBSOLETE  

        #region method Authenticate

        /// <summary>
        /// Authenticates user. Authenticate method chooses strongest possible authentication method supported by server, 
        /// preference order DIGEST-MD5 -> CRAM-MD5 -> LOGIN.
        /// </summary>
        /// <param name="userName">User login name.</param>
        /// <param name="password">Password.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected or is already authenticated.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>userName</b> is null.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        [Obsolete("Use method 'Auth' instead.")]
        public void Authenticate(string userName,string password)
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
            if(string.IsNullOrEmpty(userName)){
                throw new ArgumentNullException("userName");
            }
            if(password == null){
                password = "";
            }
                        
            // Choose authentication method, we consider LOGIN as default.
            string authMethod = "LOGIN";
            List<string> authMethods = new List<string>(this.SaslAuthMethods);
            if(authMethods.Contains("DIGEST-MD5")){
                authMethod = "DIGEST-MD5";
            }
            else if(authMethods.Contains("CRAM-MD5")){
                authMethod = "CRAM-MD5";
            }

            #region AUTH LOGIN

            if(authMethod == "LOGIN"){
                /* LOGIN
			          Example:
			            C: AUTH LOGIN<CRLF>
			            S: 334 VXNlcm5hbWU6<CRLF>   VXNlcm5hbWU6 = base64("USERNAME")
			            C: base64(username)<CRLF>
			            S: 334 UGFzc3dvcmQ6<CRLF>   UGFzc3dvcmQ6 = base64("PASSWORD")
			            C: base64(password)<CRLF>
			            S: 235 Ok<CRLF>
			    */

                WriteLine("AUTH LOGIN");

                // Read server response.
                string line = ReadLine();
                // Response line must start with 334 or otherwise it's error response.
				if(!line.StartsWith("334")){
					throw new SMTP_ClientException(line);
				}

                // Send user name to server.
                WriteLine(Convert.ToBase64String(Encoding.ASCII.GetBytes(userName)));

                // Read server response.
                line = ReadLine();
                // Response line must start with 334 or otherwise it's error response.
				if(!line.StartsWith("334")){
					throw new SMTP_ClientException(line);
				}

                // Send password to server.
                WriteLine(Convert.ToBase64String(Encoding.ASCII.GetBytes(password)));

                // Read server response.
                line = ReadLine();
                // Response line must start with 334 or otherwise it's error response.
				if(!line.StartsWith("235")){
					throw new SMTP_ClientException(line);
				}

                m_pAuthdUserIdentity = new GenericIdentity(userName,"LOGIN");
            }

            #endregion

            #region AUTH CRAM-MD5

            else if(authMethod == "CRAM-MD5"){
                /* CRAM-M5
                    Description:
                        HMACMD5 key is "password".
                 
			        Example:
					    C: AUTH CRAM-MD5<CRLF>
					    S: 334 base64(md5_calculation_hash)<CRLF>
					    C: base64(username password_hash)<CRLF>
					    S: 235 Ok<CRLF>
			    */
                
                WriteLine("AUTH CRAM-MD5");

                // Read server response.
                string line = ReadLine();
                // Response line must start with 334 or otherwise it's error response.
				if(!line.StartsWith("334")){
					throw new SMTP_ClientException(line);
				}
                 								
				HMACMD5 kMd5         = new HMACMD5(Encoding.ASCII.GetBytes(password));
				string  passwordHash = Net_Utils.ToHex(kMd5.ComputeHash(Convert.FromBase64String(line.Split(' ')[1]))).ToLower();
				
                // Send authentication info to server.
				WriteLine(Convert.ToBase64String(Encoding.ASCII.GetBytes(userName + " " + passwordHash)));

                // Read server response.
				line = ReadLine();
				// Response line must start with 235 or otherwise it's error response
				if(!line.StartsWith("235")){
					throw new SMTP_ClientException(line);
				}
         
                m_pAuthdUserIdentity = new GenericIdentity(userName,"CRAM-MD5");
            }

            #endregion

            #region AUTH DIGEST-MD5

            else if(authMethod == "DIGEST-MD5"){
                /*
                    Example:
					    C: AUTH DIGEST-MD5<CRLF>
					    S: 334 base64(digestChallange)<CRLF>
					    C: base64(digestResponse)<CRLF>
                        S: 334 base64(serverDigestRpAuth)<CRLF>
                        C: <CRLF>
					    S: 235 Ok<CRLF>
                */

                WriteLine("AUTH DIGEST-MD5");

                // Read server response.
                string line = ReadLine();
                // Response line must start with 334 or otherwise it's error response.
				if(!line.StartsWith("334")){
					throw new SMTP_ClientException(line);
				}

                // Parse server challenge.
                AUTH_SASL_DigestMD5_Challenge challenge = AUTH_SASL_DigestMD5_Challenge.Parse(Encoding.Default.GetString(Convert.FromBase64String(line.Split(' ')[1])));

                // Construct our response to server challenge.
                AUTH_SASL_DigestMD5_Response response = new AUTH_SASL_DigestMD5_Response(
                    challenge,
                    challenge.Realm[0],
                    userName,
                    password,Guid.NewGuid().ToString().Replace("-",""),
                    1,
                    challenge.QopOptions[0],
                    "smtp/" + this.RemoteEndPoint.Address.ToString()
                );

                // Send authentication info to server.
				WriteLine(Convert.ToBase64String(Encoding.Default.GetBytes(response.ToResponse())));

                // Read server response.
				line = ReadLine();
				// Response line must start with 334 or otherwise it's error response.
				if(!line.StartsWith("334")){
					throw new SMTP_ClientException(line);
				}

                // Check rspauth value.
                if(!string.Equals(Encoding.Default.GetString(Convert.FromBase64String(line.Split(' ')[1])),response.ToRspauthResponse(userName,password),StringComparison.InvariantCultureIgnoreCase)){
                    throw new Exception("SMTP server 'rspauth' value mismatch.");
                }

                // Send empty line.
                WriteLine("");

                // Read server response.
				line = ReadLine();
				// Response line must start with 235 or otherwise it's error response.
				if(!line.StartsWith("235")){
					throw new SMTP_ClientException(line);
				}

                m_pAuthdUserIdentity = new GenericIdentity(userName,"DIGEST-MD5");
            }

            #endregion
        }

        #endregion

        #region method BeginAuthenticate

        /// <summary>
        /// Internal helper method for asynchronous Authenticate method.
        /// </summary>
        [Obsolete("Use method 'AuthAsync' instead.")]
        private delegate void AuthenticateDelegate(string userName,string password);

        /// <summary>
        /// Starts authentication.
        /// </summary>
		/// <param name="userName">User login name.</param>
		/// <param name="password">Password.</param>
        /// <param name="callback">Callback to call when the asynchronous operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous operation.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected or is already authenticated.</exception>
        [Obsolete("Use method 'AuthAsync' instead.")]
        public IAsyncResult BeginAuthenticate(string userName,string password,AsyncCallback callback,object state)
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
            asyncState.SetAsyncResult(asyncMethod.BeginInvoke(userName,password,new AsyncCallback(asyncState.CompletedCallback),null));

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
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        [Obsolete("Use method 'AuthAsync' instead.")]
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
                throw new ArgumentException("Argument 'asyncResult' was not returned by a call to the BeginAuthenticate method.");
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
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        [Obsolete("Use method 'NoopAsync' instead.")]
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
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        [Obsolete("Use method 'NoopAsync' instead.")]
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
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected or is already secure connection.</exception>
        [Obsolete("Use method StartTlsAsync instead.")]
        public IAsyncResult BeginStartTLS(AsyncCallback callback,object state)
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
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        [Obsolete("Use method StartTlsAsync instead.")]
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

        #region method BeginRcptTo

        /// <summary>
        /// Internal helper method for asynchronous RcptTo method.
        /// </summary>
        private delegate void RcptToDelegate(string to,SMTP_DSN_Notify notify,string orcpt);

        /// <summary>
        /// Starts sending RCPT TO: command to SMTP server.
        /// </summary>
        /// <param name="to">Recipient email address.</param>
        /// <param name="callback">Callback to call when the asynchronous operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous disconnect.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        [Obsolete("Use method RcptToAsync instead.")]
        public IAsyncResult BeginRcptTo(string to,AsyncCallback callback,object state)
        {
            return BeginRcptTo(to,SMTP_DSN_Notify.NotSpecified,null,callback,state);
        }

        /// <summary>
        /// Starts sending RCPT TO: command to SMTP server.
        /// </summary>
        /// <param name="to">Recipient email address.</param>
        /// <param name="notify">Delivery satus notification(DSN) NOTIFY value. For more info see RFC 3461.</param>
        /// <param name="orcpt">Delivery satus notification(DSN) ORCPT value. Value null means not specified. For more info see RFC 3461.</param>
        /// <param name="callback">Callback to call when the asynchronous operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous disconnect.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <remarks>Before using <b>notify</b> or <b>orcpt</b> arguments, check that remote server supports SMTP DSN extention.</remarks>
        [Obsolete("Use method RcptToAsync instead.")]
        public IAsyncResult BeginRcptTo(string to,SMTP_DSN_Notify notify,string orcpt,AsyncCallback callback,object state)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}

            RcptToDelegate asyncMethod = new RcptToDelegate(this.RcptTo);
            AsyncResultState asyncState = new AsyncResultState(this,asyncMethod,callback,state);
            asyncState.SetAsyncResult(asyncMethod.BeginInvoke(to,notify,orcpt,new AsyncCallback(asyncState.CompletedCallback),null));

            return asyncState;
        }

        #endregion

        #region method EndRcptTo

        /// <summary>
        /// Ends a pending asynchronous RcptTo request.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that stores state information and any user defined data for this asynchronous operation.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid <b>asyncResult</b> passed to this method.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        [Obsolete("Use method RcptToAsync instead.")]
        public void EndRcptTo(IAsyncResult asyncResult)
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
            if(castedAsyncResult.AsyncDelegate is RcptToDelegate){
                ((RcptToDelegate)castedAsyncResult.AsyncDelegate).EndInvoke(castedAsyncResult.AsyncResult);
            }
            else{
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginReset method.");
            }
        }

        #endregion

        #region method BeginMailFrom

        /// <summary>
        /// Internal helper method for asynchronous MailFrom method.
        /// </summary>
        private delegate void MailFromDelegate(string from,long messageSize,SMTP_DSN_Ret ret,string envid);

        /// <summary>
        /// Starts sending MAIL FROM: command to SMTP server.
        /// </summary>
        /// <param name="from">Sender email address reported to SMTP server.</param>
        /// <param name="messageSize">Sendable message size in bytes, -1 if message size unknown.</param>
        /// <param name="callback">Callback to call when the asynchronous operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous disconnect.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        [Obsolete("Use method MailFromAsync instead.")]
        public IAsyncResult BeginMailFrom(string from,long messageSize,AsyncCallback callback,object state)
        {
            return BeginMailFrom(from,messageSize,SMTP_DSN_Ret.NotSpecified,null,callback,state);
        }

        /// <summary>
        /// Starts sending MAIL FROM: command to SMTP server.
        /// </summary>
        /// <param name="from">Sender email address reported to SMTP server.</param>
        /// <param name="messageSize">Sendable message size in bytes, -1 if message size unknown.</param>
        /// <param name="ret">Delivery satus notification(DSN) ret value. For more info see RFC 3461.</param>
        /// <param name="envid">Envelope ID. Value null means not specified. For more info see RFC 3461.</param>
        /// <param name="callback">Callback to call when the asynchronous operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous disconnect.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <remarks>Before using <b>ret</b> or <b>envid</b> arguments, check that remote server supports SMTP DSN extention.</remarks>
        [Obsolete("Use method MailFromAsync instead.")]
        public IAsyncResult BeginMailFrom(string from,long messageSize,SMTP_DSN_Ret ret,string envid,AsyncCallback callback,object state)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
			}

            MailFromDelegate asyncMethod = new MailFromDelegate(this.MailFrom);
            AsyncResultState asyncState = new AsyncResultState(this,asyncMethod,callback,state);
            asyncState.SetAsyncResult(asyncMethod.BeginInvoke(from,(int)messageSize,ret,envid,new AsyncCallback(asyncState.CompletedCallback),null));

            return asyncState;
        }

        #endregion

        #region method EndMailFrom

        /// <summary>
        /// Ends a pending asynchronous MailFrom request.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that stores state information and any user defined data for this asynchronous operation.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid <b>asyncResult</b> passed to this method.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        [Obsolete("Use method MailFromAsync instead.")]
        public void EndMailFrom(IAsyncResult asyncResult)
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
            if(castedAsyncResult.AsyncDelegate is MailFromDelegate){
                ((MailFromDelegate)castedAsyncResult.AsyncDelegate).EndInvoke(castedAsyncResult.AsyncResult);
            }
            else{
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginReset method.");
            }
        }

        #endregion

        #region method BeginReset

        /// <summary>
        /// Internal helper method for asynchronous Reset method.
        /// </summary>
        private delegate void ResetDelegate();

        /// <summary>
        /// Starts resetting SMTP session, all state data will be deleted.
        /// </summary>
        /// <param name="callback">Callback to call when the asynchronous operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous disconnect.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        [Obsolete("Use 'RsetAsync' method instead.")]
        public IAsyncResult BeginReset(AsyncCallback callback,object state)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
				throw new InvalidOperationException("You must connect first.");
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
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        [Obsolete("Use 'RsetAsync' method instead.")]
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
        /// Resets SMTP session, all state data will be deleted.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        [Obsolete("Use Rset method instead.")]
        public void Reset()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }

            /* RFC 2821 4.1.1.5 RESET (RSET).
                This command specifies that the current mail transaction will be
                aborted.  Any stored sender, recipients, and mail data MUST be
                discarded, and all buffers and state tables cleared.  The receiver
                MUST send a "250 OK" reply to a RSET command with no arguments.  A
                reset command may be issued by the client at any time.
            */

            WriteLine("RSET");

			string line = ReadLine();
			if(!line.StartsWith("250")){
				throw new SMTP_ClientException(line);
			}

            m_MailFrom = null;
            m_pRecipients.Clear();
        }

        #endregion

        #region method BeginSendMessage

        /// <summary>
        /// Internal helper method for asynchronous SendMessage method.
        /// </summary>
        private delegate void SendMessageDelegate(Stream message);

        /// <summary>
        /// Starts sending specified raw message to SMTP server.
        /// </summary>
        /// <param name="message">Message stream. Message will be readed from current stream position and to the end of stream.</param>
        /// <param name="callback">Callback to call when the asynchronous operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous method.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when SMTP client is not connected.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>message</b> is null.</exception>
        [Obsolete("Use method 'SendMessageAsync' instead.")]
        public IAsyncResult BeginSendMessage(Stream message,AsyncCallback callback,object state)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(!this.IsConnected){
                throw new InvalidOperationException("You must connect first.");
            }
            if(message == null){
                throw new ArgumentNullException("message");
            }

            SendMessageDelegate asyncMethod = new SendMessageDelegate(this.SendMessage);
            AsyncResultState asyncState = new AsyncResultState(this,asyncMethod,callback,state);
            asyncState.SetAsyncResult(asyncMethod.BeginInvoke(message,new AsyncCallback(asyncState.CompletedCallback),null));

            return asyncState;
        }

        #endregion

        #region method EndSendMessage

        /// <summary>
        /// Ends a pending asynchronous SendMessage request.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that stores state information and any user defined data for this asynchronous operation.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid <b>asyncResult</b> passed to this method.</exception>
        /// <exception cref="SMTP_ClientException">Is raised when SMTP server returns error.</exception>
        [Obsolete("Use method 'SendMessageAsync' instead.")]
        public void EndSendMessage(IAsyncResult asyncResult)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }

            AsyncResultState castedAsyncResult = asyncResult as AsyncResultState;
            if(castedAsyncResult == null || castedAsyncResult.AsyncObject != this){
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginSendMessage method.");
            }
            if(castedAsyncResult.IsEndCalled){
                throw new InvalidOperationException("BeginSendMessage was previously called for the asynchronous connection.");
            }
             
            castedAsyncResult.IsEndCalled = true;
            if(castedAsyncResult.AsyncDelegate is SendMessageDelegate){
                ((SendMessageDelegate)castedAsyncResult.AsyncDelegate).EndInvoke(castedAsyncResult.AsyncResult);
            }
            else{
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginSendMessage method.");
            }
        }

        #endregion


        #region static method BeginGetDomainHosts

        /// <summary>
        /// Internal helper method for asynchronous SendMessage method.
        /// </summary>
        private delegate string[] GetDomainHostsDelegate(string domain);

        /// <summary>
        /// Starts getting specified email domain SMTP hosts.
        /// </summary>
        /// <param name="domain">Email domain or email address. For example domain.com or user@domain.com.</param>
        /// <param name="callback">Callback to call when the asynchronous operation is complete.</param>
        /// <param name="state">User data.</param>
        /// <returns>An IAsyncResult that references the asynchronous method.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>domain</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>        
        [Obsolete("Use method Dns_Client.GetEmailHostsAsync instead.")]
        public static IAsyncResult BeginGetDomainHosts(string domain,AsyncCallback callback,object state)
        {
            if(domain == null){
                throw new ArgumentNullException("domain");
            }
            if(string.IsNullOrEmpty(domain)){
                throw new ArgumentException("Invalid argument 'domain' value, you need to specify domain value.");
            }
            
            GetDomainHostsDelegate asyncMethod = new GetDomainHostsDelegate(GetDomainHosts);
            AsyncResultState asyncState = new AsyncResultState(null,asyncMethod,callback,state);
            asyncState.SetAsyncResult(asyncMethod.BeginInvoke(domain,new AsyncCallback(asyncState.CompletedCallback),null));

            return asyncState;
        }

        #endregion

        #region static method EndGetDomainHosts

        /// <summary>
        /// Ends a pending asynchronous BeginGetDomainHosts request.
        /// </summary>
        /// <param name="asyncResult">An IAsyncResult that stores state information and any user defined data for this asynchronous operation.</param>
        /// <returns>Returns specified email domain SMTP hosts.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>asyncResult</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid <b>asyncResult</b> passed to this method.</exception>      
        [Obsolete("Use method Dns_Client.GetEmailHostsAsync instead.")]
        public static string[] EndGetDomainHosts(IAsyncResult asyncResult)
        {
            if(asyncResult == null){
                throw new ArgumentNullException("asyncResult");
            }

            AsyncResultState castedAsyncResult = asyncResult as AsyncResultState;
            if(castedAsyncResult == null){
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginGetDomainHosts method.");
            }
            if(castedAsyncResult.IsEndCalled){
                throw new InvalidOperationException("BeginGetDomainHosts was previously called for the asynchronous connection.");
            }
             
            castedAsyncResult.IsEndCalled = true;
            if(castedAsyncResult.AsyncDelegate is GetDomainHostsDelegate){
                return ((GetDomainHostsDelegate)castedAsyncResult.AsyncDelegate).EndInvoke(castedAsyncResult.AsyncResult);
            }
            else{
                throw new ArgumentException("Argument asyncResult was not returned by a call to the BeginGetDomainHosts method.");
            }
        }

        #endregion

        #region static method GetDomainHosts

        /// <summary>
        /// Gets specified email domain SMTP hosts. Values are in descending priority order.
        /// </summary>
        /// <param name="domain">Domain name. This value can be email address too, then domain parsed automatically.</param>
        /// <returns>Returns specified email domain SMTP hosts.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>domain</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        /// <exception cref="DNS_ClientException">Is raised when DNS query failure.</exception>
        [Obsolete("Use method Dns_Client.GetEmailHosts instead.")]
        public static string[] GetDomainHosts(string domain)
        {
            if(domain == null){
                throw new ArgumentNullException("domain");
            }
            if(string.IsNullOrEmpty(domain)){
                throw new ArgumentException("Invalid argument 'domain' value, you need to specify domain value.");
            }

            // We have email address, parse domain.
            if(domain.IndexOf("@") > -1){
                domain = domain.Substring(domain.IndexOf('@') + 1);
            }

            List<string> retVal = new List<string>();

            // Get MX records.
            using(Dns_Client dns = new Dns_Client()){
                DnsServerResponse response = dns.Query(domain,DNS_QType.MX);
                if(response.ResponseCode == DNS_RCode.NO_ERROR){
                    foreach(DNS_rr_MX mx in response.GetMXRecords()){
                        // Block invalid MX records.
                        if(!string.IsNullOrEmpty(mx.Host)){
                            retVal.Add(mx.Host);
                        }
                    }
                }
                else{
                    throw new DNS_ClientException(response.ResponseCode);
                }
            }

            /* RFC 2821 5.
			    If no MX records are found, but an A RR is found, the A RR is treated as if it 
                was associated with an implicit MX RR, with a preference of 0, pointing to that host.
			*/
            if(retVal.Count == 0){
                retVal.Add(domain);
            }

            return retVal.ToArray();
        }

        #endregion

        private bool m_BdatEnabled = true;

        /// <summary>
        /// Gets or sets if BDAT command can be used.
        /// </summary>
        [Obsolete("Use method SendMessage argument 'useBdatIfPossibe' instead.")]
        public bool BdatEnabled
        {
            get{ return m_BdatEnabled; }

            set{ m_BdatEnabled = value; }
        }

    }
}
