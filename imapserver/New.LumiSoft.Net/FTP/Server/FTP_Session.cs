using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Security.Principal;
using System.Data;
using System.Globalization;

using LumiSoft.Net.IO;
using LumiSoft.Net.TCP;
using LumiSoft.Net.AUTH;

namespace LumiSoft.Net.FTP.Server
{
	/// <summary>
	/// FTP Session.
	/// </summary>
	public class FTP_Session : TCP_ServerSession
    {
        #region class DataConnection

        /// <summary>
        /// This class represents FTP session data connection.
        /// </summary>
        private class DataConnection
        {
            private bool        m_IsDisposed = false;
            private FTP_Session m_pSession   = null;
            private Stream      m_pStream    = null;
            private bool        m_Read_Write = false;
            private Socket      m_pSocket    = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="session">Owner FTP session.</param>
            /// <param name="stream">Data connection data stream.</param>
            /// <param name="read_write">Specifies if data read from remote endpoint or written to it.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>session</b> or <b>stream</b> is null reference.</exception>
            public DataConnection(FTP_Session session,Stream stream,bool read_write)
            {
                if(session == null){
                    throw new ArgumentNullException("session");
                }
                if(stream == null){
                    throw new ArgumentNullException("stream");
                }

                m_pSession   = session;
                m_pStream    = stream;
                m_Read_Write = read_write;
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

                // Reset session PASV cached data.
                if(m_pSession.m_pPassiveSocket != null){
                    m_pSession.m_pPassiveSocket.Close();
                    m_pSession.m_pPassiveSocket = null;
                }                
                m_pSession.m_PassiveMode = false;

                m_pSession = null;
                if(m_pStream != null){
                    m_pStream.Dispose();
                    m_pStream = null;
                }
                if(m_pSocket != null){
                    m_pSocket.Close();
                    m_pSocket = null;
                }
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts data connection processing.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this is disposed and this method is accessed.</exception>
            public void Start()
            {
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                // Passive mode, start waiting client connection.
                if(m_pSession.PassiveMode){
                    WriteLine("150 Waiting data connection on port '" + ((IPEndPoint)m_pSession.m_pPassiveSocket.LocalEndPoint).Port + "'.");
                                                            
                    // Start connection wait timeout timer.
                    TimerEx timer = new TimerEx(10000,false);
                    timer.Elapsed += delegate(object sender,System.Timers.ElapsedEventArgs e){
                        WriteLine("550 Data connection wait timeout.");
                        Dispose();
                    };
                    timer.Enabled = true;

                    m_pSession.m_pPassiveSocket.BeginAccept(
                        delegate(IAsyncResult ar){
                            try{
                                timer.Dispose();

                                m_pSocket = m_pSession.m_pPassiveSocket.EndAccept(ar);

                                // Log
                                m_pSession.LogAddText("Data connection opened.");

                                StartDataTransfer();
                            }
                            catch{
                                WriteLine("425 Opening data connection failed.");
                                Dispose();
                            }
                        },
                        null
                    );
                }
                // Active mode, connect to client data port.
                else{
                    WriteLine("150 Opening data connection to '" + m_pSession.m_pDataConEndPoint.ToString() + "'.");
                    
					m_pSocket = new Socket(m_pSession.LocalEndPoint.AddressFamily,SocketType.Stream,ProtocolType.Tcp);
                    m_pSocket.BeginConnect(
                        m_pSession.m_pDataConEndPoint,
                        delegate(IAsyncResult ar){
                            try{
                                m_pSocket.EndConnect(ar);

                                // Log
                                m_pSession.LogAddText("Data connection opened.");

                                StartDataTransfer();
                            }
                            catch{
                                WriteLine("425 Opening data connection to '" + m_pSession.m_pDataConEndPoint.ToString() + "' failed.");
                                Dispose();
                            }
                        },
                        null
                    );
                }
            }

            #endregion

            #region method Abort

            /// <summary>
            /// Aborts transfer.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this is disposed and this method is accessed.</exception>
            public void Abort()
            {
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                WriteLine("426 Data connection closed; transfer aborted.");
                Dispose();
            }

            #endregion


            #region method WriteLine

            /// <summary>
            /// Writes line to control connection.
            /// </summary>
            /// <param name="line">Line.</param>
            private void WriteLine(string line)
            {
                if(line == null){
                    throw new ArgumentNullException("line");
                }
                if(m_IsDisposed){
                    return;
                }

                m_pSession.WriteLine(line);
            }

            #endregion

            #region method StartDataTransfer

            private void StartDataTransfer()
            {
                // TODO: Async

                try{
                    if(m_Read_Write){
                        Net_Utils.StreamCopy(new NetworkStream(m_pSocket,false),m_pStream,64000);
                    }
                    else{
                        Net_Utils.StreamCopy(m_pStream,new NetworkStream(m_pSocket,false),64000);
                    }
                    m_pSocket.Shutdown(SocketShutdown.Both);

                    m_pSession.WriteLine("226 Transfer Complete.");
                }
                catch{
                }
                Dispose();
            }

            #endregion
        }

        #endregion

        private Dictionary<string,AUTH_SASL_ServerMechanism> m_pAuthentications = null;
        private bool                                         m_SessionRejected  = false;
        private int                                          m_BadCommands      = 0;
        private string                                       m_UserName         = null;
        private GenericIdentity                              m_pUser            = null;
		private string                                       m_CurrentDir       = "/";
		private string                                       m_RenameFrom       = "";
        private DataConnection                               m_pDataConnection  = null;
		private bool                                         m_PassiveMode      = false;
        private Socket                                       m_pPassiveSocket   = null;
		private IPEndPoint                                   m_pDataConEndPoint = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public FTP_Session()
        {
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public override void Dispose()
        {
            if(IsDisposed){
                return;
            }
            base.Dispose();

            if(m_pDataConnection != null){
                m_pDataConnection.Dispose();
                m_pDataConnection = null;
            }
            if(m_pPassiveSocket != null){
                m_pPassiveSocket.Close();
                m_pPassiveSocket = null;
            }
        }

        #endregion


        #region override method Start

        /// <summary>
        /// Starts session processing.
        /// </summary>
        protected override void Start()
        {
            base.Start();
            
            try{
                string reply = null;
                if(string.IsNullOrEmpty(this.Server.GreetingText)){
                    reply = "220 [" + Net_Utils.GetLocalHostName(this.LocalHostName) + "] FTP Service Ready.";
                }
                else{
                    reply = "220 " + this.Server.GreetingText;
                }

                FTP_e_Started e = OnStarted(reply);

                if(!string.IsNullOrEmpty(e.Response)){
                    WriteLine(reply.ToString());
                }

                // Setup rejected flag, so we respond "-ERR Session rejected." any command except QUIT.
                if(string.IsNullOrEmpty(e.Response) || e.Response.ToUpper().StartsWith("500")){
                    m_SessionRejected = true;
                }
                               
                BeginReadCmd();
            }
            catch(Exception x){
                OnError(x);
            }
        }

        #endregion

        #region override method OnError

        /// <summary>
        /// Is called when session has processing error.
        /// </summary>
        /// <param name="x">Exception happened.</param>
        protected override void OnError(Exception x)
        {
            if(this.IsDisposed){
                return;
            }
            if(x == null){
                return;
            }

            /* Error handling:
                IO and Socket exceptions are permanent, so we must end session.
            */
            
            try{
                LogAddText("Exception: " + x.Message);

                // Permanent error.
                if(x is IOException || x is SocketException){
                    Dispose();
                }
                // xxx error, may be temporary.
                else{
                    // Raise FTP_Server.Error event.
                    base.OnError(x);

                    // Try to send "500 Internal server error."
                    try{
                        WriteLine("500 Internal server error.");
                    }
                    catch{
                        // Error is permanent.
                        Dispose();
                    }
                }
            }
            catch{
            }
        }

        #endregion

        #region override method OnTimeout

        /// <summary>
        /// This method is called when specified session times out.
        /// </summary>
        /// <remarks>
        /// This method allows inhereted classes to report error message to connected client.
        /// Session will be disconnected after this method completes.
        /// </remarks>
        protected override void OnTimeout()
        {
            try{
                WriteLine("500 Idle timeout, closing connection.");
            }
            catch{
                // Skip errors.
            }
        }

        #endregion


        #region method BeginReadCmd

        /// <summary>
        /// Starts reading incoming command from the connected client.
        /// </summary>
        private void BeginReadCmd()
        {
            if(this.IsDisposed){
                return;
            }

            try{
                SmartStream.ReadLineAsyncOP readLineOP = new SmartStream.ReadLineAsyncOP(new byte[32000],SizeExceededAction.JunkAndThrowException);
                // This event is raised only when read next coomand completes asynchronously.
                readLineOP.Completed += new EventHandler<EventArgs<SmartStream.ReadLineAsyncOP>>(delegate(object sender,EventArgs<SmartStream.ReadLineAsyncOP> e){                
                    if(ProcessCmd(readLineOP)){
                        BeginReadCmd();
                    }
                });
                // Process incoming commands while, command reading completes synchronously.
                while(this.TcpStream.ReadLine(readLineOP,true)){
                    if(!ProcessCmd(readLineOP)){
                        break;
                    }
                }
            }
            catch(Exception x){
                OnError(x);
            }
        }

        #endregion

        #region method ProcessCmd

        /// <summary>
        /// Completes command reading operation.
        /// </summary>
        /// <param name="op">Operation.</param>
        /// <returns>Returns true if server should start reading next command.</returns>
        private bool ProcessCmd(SmartStream.ReadLineAsyncOP op)
        {
            bool readNextCommand = true;
                        
            try{
                // We are already disposed.
                if(this.IsDisposed){
                    return false;
                }
                // Check errors.
                if(op.Error != null){
                    OnError(op.Error);
                }
                // Remote host shut-down(Socket.ShutDown) socket.
                if(op.BytesInBuffer == 0){
                    LogAddText("The remote host '" + this.RemoteEndPoint.ToString() + "' shut down socket.");
                    Dispose();
                
                    return false;
                }
                                
                string[] cmd_args = Encoding.UTF8.GetString(op.Buffer,0,op.LineBytesInBuffer).Split(new char[]{' '},2);
                string   cmd      = cmd_args[0].ToUpperInvariant();
                string   args     = cmd_args.Length == 2 ? cmd_args[1] : "";

                // Log.
                if(this.Server.Logger != null){
                    // Hide password from log.
                    if(cmd == "PASS"){
                        this.Server.Logger.AddRead(this.ID,this.AuthenticatedUserIdentity,op.BytesInBuffer,"PASS <***REMOVED***>",this.LocalEndPoint,this.RemoteEndPoint);
                    }
                    else{
                        this.Server.Logger.AddRead(this.ID,this.AuthenticatedUserIdentity,op.BytesInBuffer,op.LineUtf8,this.LocalEndPoint,this.RemoteEndPoint);
                    }
                }

                if(cmd == "USER"){
                    USER(args);
                }
                else if(cmd == "PASS"){
                    PASS(args);
                }
                else if(cmd == "CWD" || cmd == "XCWD"){
                    CWD(args);
                }
                else if(cmd == "CDUP" || cmd == "XCUP"){
                    CDUP(args);
                }
                else if(cmd == "PWD" || cmd == "XPWD"){
                    PWD(args);
                }
                else if(cmd == "ABOR"){
                    ABOR(args);
                }
                else if(cmd == "RETR"){
                    RETR(args);
                }
                else if(cmd == "STOR"){
                    STOR(args);
                }
                else if(cmd == "DELE"){
                    DELE(args);
                }
                else if(cmd == "APPE"){
                    APPE(args);
                }
                else if(cmd == "SIZE"){
                    SIZE(args);
                }
                else if(cmd == "RNFR"){
                    RNFR(args);
                }
                else if(cmd == "DELE"){
                    DELE(args);
                }
                else if(cmd == "RNTO"){
                    RNTO(args);
                }
                else if(cmd == "RMD" || cmd == "XRMD"){
                    RMD(args);
                }
                else if(cmd == "MKD" || cmd == "XMKD"){
                    MKD(args);
                }
                else if(cmd == "LIST"){
                    LIST(args);
                }
                else if(cmd == "NLST"){
                    NLST(args);
                }
                else if(cmd == "TYPE"){
                    TYPE(args);
                }
                else if(cmd == "PORT"){
                    PORT(args);
                }
                else if(cmd == "PASV"){
                    PASV(args);
                }
                else if(cmd == "SYST"){
                    SYST(args);
                }
                else if(cmd == "NOOP"){
                    NOOP(args);
                }
                else if(cmd == "QUIT"){
                    QUIT(args);
                    readNextCommand = false;
                }
                else if(cmd == "FEAT"){
                    FEAT(args);
                }
                else if(cmd == "OPTS"){
                    OPTS(args);
                }
                else{
                     m_BadCommands++;

                     // Maximum allowed bad commands exceeded.
                     if(this.Server.MaxBadCommands != 0 && m_BadCommands > this.Server.MaxBadCommands){
                         WriteLine("500 Too many bad commands, closing transmission channel.");
                         Disconnect();

                         return false;
                     }
                            
                     WriteLine("500 Error: command '" + cmd + "' not recognized.");
                 }
             }
             catch(Exception x){
                 OnError(x);
             }

             return readNextCommand;
        }

        #endregion


		#region method USER

		private void USER(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
			if(this.IsAuthenticated){
				WriteLine("500 You are already authenticated");

				return;
			}
			if(!string.IsNullOrEmpty(m_UserName)){
				WriteLine("500 username is already specified, please specify password");

				return;
			}

			string[] param = argsText.Split(new char[]{' '});

			// There must be only one parameter - userName
			if(argsText.Length > 0 && param.Length == 1){
				string userName = param[0];
							
				WriteLine("331 Password required or user:'" + userName + "'");
				m_UserName = userName;
			}
			else{
				WriteLine("500 Syntax error. Syntax:{USER username}");
			}
		}

		#endregion

		#region method PASS

		private void PASS(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
			if(this.IsAuthenticated){
				WriteLine("500 You are already authenticated");

				return;
			}
			if(m_UserName.Length == 0){
				WriteLine("503 please specify username first");

				return;
			}

			string[] param = argsText.Split(new char[]{' '});

			// There may be only one parameter - password
			if(param.Length == 1){
				string password = param[0];
									
				// Authenticate user
				if(OnAuthenticate(m_UserName,password).IsAuthenticated){
					WriteLine("230 Password ok");

                    m_pUser = new GenericIdentity(m_UserName,"FTP-USER/PASS");
				}
				else{						
					WriteLine("530 UserName or Password is incorrect");					
					m_UserName = ""; // Reset userName !!!
				}
			}
			else{
				WriteLine("500 Syntax error. Syntax:{PASS userName}");
			}
		}

		#endregion


		#region method CWD

		private void CWD(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			} 

			/*
				This command allows the user to work with a different
				directory or dataset for file storage or retrieval without
				altering his login or accounting information.  Transfer
				parameters are similarly unchanged.  The argument is a
				pathname specifying a directory or other system dependent
				file group designator.
			*/

            FTP_e_Cwd eArgs = new FTP_e_Cwd(argsText);
            OnCwd(eArgs);

            // API didn't provide response.
            if(eArgs.Response == null){
                WriteLine("500 Internal server error: FTP server didn't provide response for CWD command.");
            }
            else{
                foreach(FTP_t_ReplyLine reply in eArgs.Response){
                    WriteLine(reply.ToString());
                }
            }
		}

		#endregion

		#region method CDUP

		private void CDUP(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}           
            if(!string.IsNullOrEmpty(argsText)){
                WriteLine("501 Error in arguments.");
            }

			/*
				This command is a special case of CWD, and is included to
				simplify the implementation of programs for transferring
				directory trees between operating systems having different
				syntaxes for naming the parent directory.  The reply codes
				shall be identical to the reply codes of CWD.
			*/

            FTP_e_Cdup eArgs = new FTP_e_Cdup();
            OnCdup(eArgs);

            // API didn't provide response.
            if(eArgs.Response == null){
                WriteLine("500 Internal server error: FTP server didn't provide response for CDUP command.");
            }
            else{
                foreach(FTP_t_ReplyLine reply in eArgs.Response){
                    WriteLine(reply.ToString());
                }
            }
		}

		#endregion

		#region method PWD

		private void PWD(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}

			/*
				This command causes the name of the current working
				directory to be returned in the reply.
			*/			

			WriteLine("257 \"" + m_CurrentDir + "\" is current directory.");
		}

		#endregion


        #region method ABOR

		private void ABOR(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }			
			if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}
            if(!string.IsNullOrEmpty(argsText)){
                WriteLine("501 Error in arguments. !");

				return;
            }

            /* RFC 959 4.1.3. ABORT (ABOR)
                This command tells the server to abort the previous FTP
                service command and any associated transfer of data.  The
                abort command may require "special action", as discussed in
                the Section on FTP Commands, to force recognition by the
                server.  No action is to be taken if the previous command
                has been completed (including data transfer).  The control
                connection is not to be closed by the server, but the data
                connection must be closed.

                There are two cases for the server upon receipt of this
                command: (1) the FTP service command was already completed,
                or (2) the FTP service command is still in progress.

                   In the first case, the server closes the data connection
                   (if it is open) and responds with a 226 reply, indicating
                   that the abort command was successfully processed.

                   In the second case, the server aborts the FTP service in
                   progress and closes the data connection, returning a 426
                   reply to indicate that the service request terminated
                   abnormally.  The server then sends a 226 reply,
                   indicating that the abort command was successfully
                   processed.
            */

            if(m_pDataConnection != null){
                m_pDataConnection.Abort();
            }

            WriteLine("226 ABOR command successful.");
		}

		#endregion

		#region method RETR

		private void RETR(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }			
			if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}
            if(string.IsNullOrEmpty(argsText)){
                WriteLine("501 Invalid file name. !");

				return;
            }

            /*
				This command causes the server-DTP to transfer a copy of the
				file, specified in the pathname, to the server- or user-DTP
				at the other end of the data connection.  The status and
				contents of the file at the server site shall be unaffected.
			*/
                        
            FTP_e_GetFile eArgs = new FTP_e_GetFile(argsText);
            OnGetFile(eArgs);

            // Error getting directory listing.
            if(eArgs.Error != null){
                foreach(FTP_t_ReplyLine reply in eArgs.Error){
                    WriteLine(reply.ToString());
                }
            }
            // Get file succeeded.
            else{
                if(eArgs.FileStream == null){
                    WriteLine("500 Internal server error: File stream not provided by server.");

                    return;
                }
                
                m_pDataConnection = new DataConnection(this,eArgs.FileStream,false);
                m_pDataConnection.Start();
            }
		}

		#endregion

		#region method STOR

		private void STOR(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}
            if(string.IsNullOrEmpty(argsText)){
                WriteLine("501 Invalid file name.");
            }

			/*
				This command causes the server-DTP to transfer a copy of the
				file, specified in the pathname, to the server- or user-DTP
				at the other end of the data connection.  The status and
				contents of the file at the server site shall be unaffected.
			*/

            FTP_e_Stor eArgs = new FTP_e_Stor(argsText);
            OnStor(eArgs);

            // Opearation failed.
            if(eArgs.Error != null){
                foreach(FTP_t_ReplyLine reply in eArgs.Error){
                    WriteLine(reply.ToString());
                }
            }
            // Opearation succeeded.
            else{
                if(eArgs.FileStream == null){
                    WriteLine("500 Internal server error: File stream not provided by server.");

                    return;
                }

                m_pDataConnection = new DataConnection(this,eArgs.FileStream,true);
                m_pDataConnection.Start();
            }
		}

		#endregion

		#region method DELE

		private void DELE(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}
            if(string.IsNullOrEmpty(argsText)){
                WriteLine("501 Invalid file name.");
            }

			/*
				This command causes the file specified in the pathname to be
				deleted at the server site.  If an extra level of protection
				is desired (such as the query, "Do you really wish to
				delete?"), it should be provided by the user-FTP process.
			*/

            FTP_e_Dele eArgs = new FTP_e_Dele(argsText);
            OnDele(eArgs);

            // API didn't provide response.
            if(eArgs.Response == null){
                WriteLine("500 Internal server error: FTP server didn't provide response for DELE command.");
            }
            else{
                foreach(FTP_t_ReplyLine reply in eArgs.Response){
                    WriteLine(reply.ToString());
                }
            }
		}

		#endregion

		#region method APPE

		private void APPE(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}
            if(string.IsNullOrEmpty(argsText)){
                WriteLine("501 Invalid file name.");
            }

			/*
				This command causes the server-DTP to accept the data
				transferred via the data connection and to store the data in
				a file at the server site.  If the file specified in the
				pathname exists at the server site, then the data shall be
				appended to that file; otherwise the file specified in the
				pathname shall be created at the server site.
			*/
			
			FTP_e_Appe eArgs = new FTP_e_Appe(argsText);
            OnAppe(eArgs);

            // Opearation failed.
            if(eArgs.Error != null){
                foreach(FTP_t_ReplyLine reply in eArgs.Error){
                    WriteLine(reply.ToString());
                }
            }
            // Opearation succeeded.
            else{
                if(eArgs.FileStream == null){
                    WriteLine("500 Internal server error: File stream not provided by server.");

                    return;
                }

                m_pDataConnection = new DataConnection(this,eArgs.FileStream,true);
                m_pDataConnection.Start();
            }
		}

		#endregion

        #region method SIZE

        private void SIZE(string argsText)
        {
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}
            if(string.IsNullOrEmpty(argsText)){
                WriteLine("501 Invalid file name.");
            }

            /* RFC 3659 4.1.
                   The syntax of the SIZE command is:

                      size          = "Size" SP pathname CRLF

                   The server-PI will respond to the SIZE command with a 213 reply
                   giving the transfer size of the file whose pathname was supplied, or
                   an error response if the file does not exist, the size is
                   unavailable, or some other error has occurred.  The value returned is
                   in a format suitable for use with the RESTART (REST) command for mode
                   STREAM, provided the transfer mode and type are not altered.

                      size-response = "213" SP 1*DIGIT CRLF /
                                      error-response

                   Note that when the 213 response is issued, that is, when there is no
                   error, the format MUST be exactly as specified.  Multi-line responses
                   are not permitted.
            */

            FTP_e_GetFileSize eArgs = new FTP_e_GetFileSize(argsText);
            OnGetFileSize(eArgs);

            // Error completing operation.
            if(eArgs.Error != null){
                foreach(FTP_t_ReplyLine reply in eArgs.Error){
                    WriteLine(reply.ToString());
                }
            }
            // Operation succeeded.
            else{
                WriteLine("213 " + eArgs.FileSize);   
            }            
        }

        #endregion


		#region method RNFR

		private void RNFR(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}
            if(string.IsNullOrEmpty(argsText)){
                WriteLine("501 Invalid path value.");
            }

			/*
				This command specifies the old pathname of the file which is
				to be renamed.  This command must be immediately followed by
				a "rename to" command specifying the new file pathname.
			*/

            m_RenameFrom = argsText;
		}

		#endregion

		#region method RNTO

		private void RNTO(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}
            if(string.IsNullOrEmpty(argsText)){
                WriteLine("501 Invalid path value.");
            }
			if(m_RenameFrom.Length == 0){
				WriteLine("503 Bad sequence of commands.");

				return;
			}

			/*
				This command specifies the new pathname of the file
				specified in the immediately preceding "rename from"
				command.  Together the two commands cause a file to be
				renamed.
			*/

            FTP_e_Rnto eArgs = new FTP_e_Rnto(m_RenameFrom,argsText);
            OnRnto(eArgs);

            // API didn't provide response.
            if(eArgs.Response == null){
                WriteLine("500 Internal server error: FTP server didn't provide response for RNTO command.");
            }
            else{
                foreach(FTP_t_ReplyLine reply in eArgs.Response){
                    WriteLine(reply.ToString());
                }
            }
		}

		#endregion


		#region method RMD

		private void RMD(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}
            if(string.IsNullOrEmpty(argsText)){
                WriteLine("501 Invalid directory name.");
            }

			/*
				This command causes the directory specified in the pathname
				to be removed as a directory (if the pathname is absolute)
				or as a subdirectory of the current working directory (if
				the pathname is relative).
			*/
			
            FTP_e_Rmd eArgs = new FTP_e_Rmd(argsText);
            OnRmd(eArgs);

            // API didn't provide response.
            if(eArgs.Response == null){
                WriteLine("500 Internal server error: FTP server didn't provide response for RMD command.");
            }
            else{
                foreach(FTP_t_ReplyLine reply in eArgs.Response){
                    WriteLine(reply.ToString());
                }
            }
		}

		#endregion

		#region method MKD

		private void MKD(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }			
			if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}
            if(string.IsNullOrEmpty(argsText)){
                WriteLine("501 Invalid directory name.");
            }

            /*
				This command causes the directory specified in the pathname
				to be created as a directory (if the pathname is absolute)
				or as a subdirectory of the current working directory (if
				the pathname is relative).
			*/

			FTP_e_Mkd eArgs = new FTP_e_Mkd(argsText);
            OnMkd(eArgs);

            // API didn't provide response.
            if(eArgs.Response == null){
                WriteLine("500 Internal server error: FTP server didn't provide response for MKD command.");
            }
            else{
                foreach(FTP_t_ReplyLine reply in eArgs.Response){
                    WriteLine(reply.ToString());
                }
            }
		}

		#endregion

		#region method LIST
		
		private void LIST(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}

			/*
				This command causes a list to be sent from the server to the
				passive DTP.  If the pathname specifies a directory or other
				group of files, the server should transfer a list of files
				in the specified directory.  If the pathname specifies a
				file then the server should send current information on the
				file.  A null argument implies the user's current working or
				default directory.  The data transfer is over the data
				connection in type ASCII or type EBCDIC.  (The user must
				ensure that the TYPE is appropriately ASCII or EBCDIC).
				Since the information on a file may vary widely from system
				to system, this information may be hard to use automatically
				in a program, but may be quite useful to a human user.
			*/

            FTP_e_GetDirListing eArgs = new FTP_e_GetDirListing(argsText);
            OnGetDirListing(eArgs);

            // Error getting directory listing.
            if(eArgs.Error != null){
                foreach(FTP_t_ReplyLine reply in eArgs.Error){
                    WriteLine(reply.ToString());
                }
            }
            // Listing succeeded.
            else{
                // Build directory listing.
                MemoryStreamEx retVal = new MemoryStreamEx(8000);
                foreach(FTP_ListItem item in eArgs.Items){
                    if(item.IsDir){
                        byte[] data = Encoding.UTF8.GetBytes(item.Modified.ToString("MM-dd-yy HH:mm") + " <DIR> " + item.Name + "\r\n");
					    retVal.Write(data,0,data.Length);
					}
					else{
                        byte[] data = Encoding.UTF8.GetBytes(item.Modified.ToString("MM-dd-yy HH:mm") + " " + item.Size.ToString() + " " + item.Name + "\r\n");
					    retVal.Write(data,0,data.Length);
					}
                }
                retVal.Position = 0;                

                m_pDataConnection = new DataConnection(this,retVal,false);
                m_pDataConnection.Start();
            }
		}

		#endregion

		#region method NLST

		private void NLST(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }			
			if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}

            /*
				This command causes a directory listing to be sent from
				server to user site.  The pathname should specify a
				directory or other system-specific file group descriptor; a
				null argument implies the current directory.  The server
				will return a stream of names of files and no other
				information.  The data will be transferred in ASCII or
				EBCDIC type over the data connection as valid pathname
				strings separated by <CRLF> or <NL>.  (Again the user must
				ensure that the TYPE is correct.)  This command is intended
				to return information that can be used by a program to
				further process the files automatically.  For example, in
				the implementation of a "multiple get" function.
			*/

			FTP_e_GetDirListing eArgs = new FTP_e_GetDirListing(argsText);
            OnGetDirListing(eArgs);

            // Error getting directory listing.
            if(eArgs.Error != null){
                foreach(FTP_t_ReplyLine reply in eArgs.Error){
                    WriteLine(reply.ToString());
                }
            }
            // Listing succeeded.
            else{
                // Build directory listing.
                MemoryStreamEx retVal = new MemoryStreamEx(8000);
                foreach(FTP_ListItem item in eArgs.Items){
                    byte[] data = Encoding.UTF8.GetBytes(item.Name + "\r\n");
                    retVal.Write(data,0,data.Length);
                }
                retVal.Position = 0;                

                m_pDataConnection = new DataConnection(this,retVal,false);
                m_pDataConnection.Start();
            }
		}

		#endregion


		#region method TYPE

		private void TYPE(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
			/*
				The argument specifies the representation type as described
				in the Section on Data Representation and Storage.  Several
				types take a second parameter.  The first parameter is
				denoted by a single Telnet character, as is the second
				Format parameter for ASCII and EBCDIC; the second parameter
				for local byte is a decimal integer to indicate Bytesize.
				The parameters are separated by a <SP> (Space, ASCII code
				32).

				The following codes are assigned for type:

							\    /
				A - ASCII |    | N - Non-print
							|-><-| T - Telnet format effectors
				E - EBCDIC|    | C - Carriage Control (ASA)
							/    \
				I - Image
	               
				L <byte size> - Local byte Byte size
				
				The default representation type is ASCII Non-print.  If the
				Format parameter is changed, and later just the first
				argument is changed, Format then returns to the Non-print
				default.
			*/
			if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");
				return;
			}

			if(argsText.Trim().ToUpper() == "A" || argsText.Trim().ToUpper() == "I"){
				WriteLine("200 Type is set to " + argsText + ".");
			}
			else{
				WriteLine("500 Invalid type " + argsText + ".");
			}
		}

		#endregion

		#region method PORT

		private void PORT(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
			/*
				 The argument is a HOST-PORT specification for the data port
				to be used in data connection.  There are defaults for both
				the user and server data ports, and under normal
				circumstances this command and its reply are not needed.  If
				this command is used, the argument is the concatenation of a
				32-bit internet host address and a 16-bit TCP port address.
				This address information is broken into 8-bit fields and the
				value of each field is transmitted as a decimal number (in
				character string representation).  The fields are separated
				by commas.  A port command would be:

				PORT h1,h2,h3,h4,p1,p2

				where h1 is the high order 8 bits of the internet host
				address.
			*/
			if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}

			string[] parts = argsText.Split(',');
			if(parts.Length != 6){
				WriteLine("550 Invalid arguments.");

				return;
			}

			string ip   = parts[0] + "." + parts[1] + "." + parts[2] + "." + parts[3];
			int    port = (Convert.ToInt32(parts[4]) << 8) | Convert.ToInt32(parts[5]);

			m_pDataConEndPoint = new IPEndPoint(IPAddress.Parse(ip),port);

			WriteLine("200 PORT Command successful.");
		}

		#endregion

		#region method PASV

		private void PASV(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }			
			if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}

            /*
				This command requests the server-DTP to "listen" on a data
				port (which is not its default data port) and to wait for a
				connection rather than initiate one upon receipt of a
				transfer command.  The response to this command includes the
				host and port address this server is listening on.
			*/

            int port = this.Server.PassiveStartPort;

            // We have already passive socket.
            if(m_pPassiveSocket != null){
                // DO nothing ... Use existing socket.
            }
            // Create new passive socket.
            else{
                m_pPassiveSocket = new Socket(AddressFamily.InterNetwork,SocketType.Stream,ProtocolType.Tcp);

                // Find free port.
                for(int i=port;i<IPEndPoint.MaxPort;i++){
                    try{
                        m_pPassiveSocket.Bind(new IPEndPoint(IPAddress.Any,port));

					    // If we reach here then port is free
					    break;
				    }
				    catch{
				    }
                }

                m_pPassiveSocket.Listen(1);
            }

			// Notify client on what IP and port server is listening client to connect.
			// PORT h1,h2,h3,h4,p1,p2
            if(this.Server.PassivePublicIP != null){
                WriteLine("227 Entering Passive Mode (" + this.Server.PassivePublicIP.ToString().Replace(".",",") + "," + (port >> 8) + "," + (port & 255)  + ").");
            }
            else{
                WriteLine("227 Entering Passive Mode (" + this.LocalEndPoint.Address.ToString().Replace(".",",") + "," + (port >> 8) + "," + (port & 255)  + ").");
            }
			m_PassiveMode = true;
		}

		#endregion

		#region method SYST

		private void SYST(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
			/*
				This command is used to find out the type of operating
				system at the server.  The reply shall have as its first
				word one of the system names listed in the current version
				of the Assigned Numbers document [4].
			*/
			if(!this.IsAuthenticated){
				WriteLine("530 Please authenticate firtst !");

				return;
			}

			WriteLine("215 Windows_NT");
		}

		#endregion
                        

		#region method NOOP

		private void NOOP(string argsText)
		{
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }
			/*
				This command does not affect any parameters or previously
				entered commands. It specifies no action other than that the
				server send an OK reply.
			*/
			WriteLine("200 OK");
		}

		#endregion

		#region method QUIT

		private void QUIT(string argsText)
		{
			/*
				This command terminates a USER and if file transfer is not
				in progress, the server closes the control connection.  If
				file transfer is in progress, the connection will remain
				open for result response and the server will then close it.
				If the user-process is transferring files for several USERs
				but does not wish to close and then reopen connections for
				each, then the REIN command should be used instead of QUIT.

				An unexpected close on the control connection will cause the
				server to take the effective action of an abort (ABOR) and a
				logout (QUIT).
			*/

            try{
			    WriteLine("221 FTP server signing off");
            }
            catch{
            }            
            Disconnect();
            Dispose();
		}

		#endregion


        #region method FEAT

        private void FEAT(string argsText)
        {
            /* RFC 2389 3.1. Feature (FEAT) 
                Command Syntax
                        feat            = "Feat" CRLF

                   The FEAT command consists solely of the word "FEAT".  It has no
                   parameters or arguments.

                3.2. FEAT Command Responses

                   Where a server-FTP process does not support the FEAT command, it will
                   respond to the FEAT command with a 500 or 502 reply.  This is simply
                   the normal "unrecognized command" reply that any unknown command
                   would elicit.  Errors in the command syntax, such as giving
                   parameters, will result in a 501 reply.

                   Server-FTP processes that recognize the FEAT command, but implement
                   no extended features, and therefore have nothing to report, SHOULD
                   respond with the "no-features" 211 reply.  However, as this case is
                   practically indistinguishable from a server-FTP that does not
                   recognize the FEAT command, a 500 or 502 reply MAY also be used.  The
                   "no-features" reply MUST NOT use the multi-line response format,
                   exactly one response line is required and permitted.

                   Replies to the FEAT command MUST comply with the following syntax.
                   Text on the first line of the reply is free form, and not
                   interpreted, and has no practical use, as this text is not expected
                   to be revealed to end users.  The syntax of other reply lines is
                   precisely defined, and if present, MUST be exactly as specified.

                        feat-response   = error-response / no-features / feature-listing
                        no-features     = "211" SP *TCHAR CRLF
                        feature-listing = "211-" *TCHAR CRLF
                                          1*( SP feature CRLF )
                                          "211 End" CRLF
                        feature         = feature-label [ SP feature-parms ]
                        feature-label   = 1*VCHAR
                        feature-parms   = 1*TCHAR

                   Note that each feature line in the feature-listing begins with a
                   single space.  That space is not optional, nor does it indicate
                   general white space.  This space guarantees that the feature line can
                   never be misinterpreted as the end of the feature-listing, but is
                   required even where there is no possibility of ambiguity.

                   Each extension supported must be listed on a separate line to
                   facilitate the possible inclusion of parameters supported by each
                   extension command.  The feature-label to be used in the response to
                   the FEAT command will be specified as each new feature is added to
                   the FTP command set.  Often it will be the name of a new command
                   added, however this is not required.  In fact it is not required that
                   a new feature actually add a new command.  Any parameters included
                   are to be specified with the definition of the command concerned.
                   That specification shall also specify how any parameters present are
                   to be interpreted.

                   The feature-label and feature-parms are nominally case sensitive,
                   however the definitions of specific labels and parameters specify the
                   precise interpretation, and it is to be expected that those
                   definitions will usually specify the label and parameters in a case
                   independent manner.  Where this is done, implementations are
                   recommended to use upper case letters when transmitting the feature
                   response.

                   The FEAT command itself is not included in the list of features
                   supported, support for the FEAT command is indicated by return of a
                   reply other than a 500 or 502 reply.

                   A typical example reply to the FEAT command might be a multiline
                   reply of the form:

                        C> feat
                        S> 211-Extensions supported:
                        S>  MLST size*;create;modify*;perm;media-type
                        S>  SIZE
                        S>  COMPRESSION
                        S>  MDTM
                        S> 211 END
            */

            StringBuilder retVal = new StringBuilder();
            retVal.Append("211-Extensions supported:\r\n");
            retVal.Append(" SIZE\r\n");
            retVal.Append("211 End of extentions.\r\n");

            WriteLine(retVal.ToString());
        }

        #endregion

        #region method OPTS

        private void OPTS(string argsText)
        {
            if(m_SessionRejected){
                WriteLine("500 Bad sequence of commands: Session rejected.");

                return;
            }

            /* RFC 2389 4. The OPTS Command.
                   The OPTS (options) command allows a user-PI to specify the desired
                   behavior of a server-FTP process when another FTP command (the target
                   command) is later issued.  The exact behavior, and syntax, will vary
                   with the target command indicated, and will be specified with the
                   definition of that command.  Where no OPTS behavior is defined for a
                   particular command there are no options available for that command.

                   Request Syntax:
                        opts             = opts-cmd SP command-name
                                               [ SP command-options ] CRLF
                        opts-cmd         = "opts"
                        command-name     = <any FTP command which allows option setting>
                        command-options  = <format specified by individual FTP command>

                   Response Syntax:
                        opts-response    = opts-good / opts-bad
                        opts-good        = "200" SP response-message CRLF
                        opts-bad         = "451" SP response-message CRLF /
                                           "501" SP response-message CRLF
                        response-message = *TCHAR

                   An "opts-good" response (200 reply) MUST be sent when the command-
                   name specified in the OPTS command is recognized, and the command-
                   options, if any, are recognized, and appropriate.  An "opts-bad"
                   response is sent in other cases.  A 501 reply is appropriate for any
                   permanent error.  That is, for any case where simply repeating the
                   command at some later time, without other changes of state, will also
                   be an error.  A 451 reply should be sent where some temporary
                   condition at the server, not related to the state of communications
                   between user and server, prevents the command being accepted when
                   issued, but where if repeated at some later time, a changed
                   environment for the server-FTP process may permit the command to
                   succeed.  If the OPTS command itself is not recognized, a 500 or 502
                   reply will, of course, result.
            */

            if(string.Equals(argsText,"UTF8 ON",StringComparison.InvariantCultureIgnoreCase)){
                WriteLine("200 Ok.");
            }
            else{
                WriteLine("501 OPTS parameter not supported.");
            }
        }

        #endregion


        #region method WriteLine

        /// <summary>
        /// Sends and logs specified line to connected host.
        /// </summary>
        /// <param name="line">Line to send.</param>
        private void WriteLine(string line)
        {
            if(line == null){
                throw new ArgumentNullException("line");
            }

            int countWritten = this.TcpStream.WriteLine(line);

            // Log.
            if(this.Server.Logger != null){
                this.Server.Logger.AddWrite(this.ID,this.AuthenticatedUserIdentity,countWritten,line.TrimEnd(),this.LocalEndPoint,this.RemoteEndPoint);
            }
        }

        #endregion

        #region method LogAddText

        /// <summary>
        /// Logs specified text.
        /// </summary>
        /// <param name="text">text to log.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>text</b> is null reference.</exception>
        public void LogAddText(string text)
        {
            if(text == null){
                throw new ArgumentNullException("text");
            }

            // Log
            if(this.Server.Logger != null){
                this.Server.Logger.AddText(this.ID,text);
            }
        }

        #endregion


		#region Properties Implementation
        
        /// <summary>
        /// Gets session owner FTP server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public new FTP_Server Server
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return (FTP_Server)base.Server;
            }
        }

        /// <summary>
        /// Gets supported SASL authentication methods collection.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public Dictionary<string,AUTH_SASL_ServerMechanism> Authentications
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pAuthentications; 
            }
        }

        /// <summary>
        /// Gets number of bad commands happened on POP3 session.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public int BadCommands
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_BadCommands; 
            }
        }

        /// <summary>
        /// Gets authenticated user identity or null if user has not authenticated.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public override GenericIdentity AuthenticatedUserIdentity
        {
	        get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

		        return m_pUser;
	        }
        }

        /// <summary>
        /// Gets or sets current working directory.
        /// </summary>
        public string CurrentDir
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_CurrentDir; 
            }

            set{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                m_CurrentDir = value;
            }
        }

		/// <summary>
		/// Gets if sessions is in passive mode.
		/// </summary>
		public bool PassiveMode
		{
			get{ return m_PassiveMode; }
		}

		#endregion

        #region Events implementation

        /// <summary>
        /// Is raised when session has started processing and needs to send 220 greeting or 500 error resposne to the connected client.
        /// </summary>
        public event EventHandler<FTP_e_Started> Started = null;

        #region method OnStarted

        /// <summary>
        /// Raises <b>Started</b> event.
        /// </summary>
        /// <param name="reply">Default FTP server reply.</param>
        /// <returns>Returns event args.</returns>
        private FTP_e_Started OnStarted(string reply)
        {
            FTP_e_Started eArgs = new FTP_e_Started(reply);

            if(this.Started != null){                
                this.Started(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to authenticate session using USER/PASS FTP authentication.
        /// </summary>
        public event EventHandler<FTP_e_Authenticate> Authenticate = null;

        #region method OnAuthenticate

        /// <summary>
        /// Raises <b>Authenticate</b> event.
        /// </summary>
        /// <param name="user">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>Returns event args.</returns>
        private FTP_e_Authenticate OnAuthenticate(string user,string password)
        {
            FTP_e_Authenticate eArgs = new FTP_e_Authenticate(user,password);

            if(this.Authenticate != null){
                this.Authenticate(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to get specified file.
        /// </summary>
        public event EventHandler<FTP_e_GetFile> GetFile = null;

        #region method OnGetFile

        /// <summary>
        /// Raises <b>GetFile</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void OnGetFile(FTP_e_GetFile e)
        {
            if(this.GetFile != null){
                this.GetFile(this,e);
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to complete STOR(store file) command.
        /// </summary>
        public event EventHandler<FTP_e_Stor> Stor = null;

        #region method OnStor

        /// <summary>
        /// Raises <b>Stor</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void OnStor(FTP_e_Stor e)
        {
            if(this.Stor != null){
                this.Stor(this,e);
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to get specified file size.
        /// </summary>
        public event EventHandler<FTP_e_GetFileSize> GetFileSize = null;

        #region method OnGetFileSize

        /// <summary>
        /// Raises <b>GetFileSize</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void OnGetFileSize(FTP_e_GetFileSize e)
        {
            if(this.GetFileSize != null){
                this.GetFileSize(this,e);
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to complete DELE(delete file) command.
        /// </summary>
        public event EventHandler<FTP_e_Dele> Dele = null;

        #region method OnDele

        /// <summary>
        /// Raises <b>Dele</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void OnDele(FTP_e_Dele e)
        {
            if(this.Dele != null){
                this.Dele(this,e);
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to complete APPE(append to file) command.
        /// </summary>
        public event EventHandler<FTP_e_Appe> Appe = null;

        #region method OnAppe

        /// <summary>
        /// Raises <b>Appe</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void OnAppe(FTP_e_Appe e)
        {
            if(this.Appe != null){
                this.Appe(this,e);
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to complete CWD(change working directory) command.
        /// </summary>
        public event EventHandler<FTP_e_Cwd> Cwd = null;

        #region method OnCwd

        /// <summary>
        /// Raises <b>Cwd</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void OnCwd(FTP_e_Cwd e)
        {
            if(this.Cwd != null){
                this.Cwd(this,e);
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to complete CDUP(change directory up) command.
        /// </summary>
        public event EventHandler<FTP_e_Cdup> Cdup = null;

        #region method OnCdup

        /// <summary>
        /// Raises <b>Cdup</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void OnCdup(FTP_e_Cdup e)
        {
            if(this.Cdup != null){
                this.Cdup(this,e);
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to complete RMD(remove directory) command.
        /// </summary>
        public event EventHandler<FTP_e_Rmd> Rmd = null;

        #region method OnRmd

        /// <summary>
        /// Raises <b>Rmd</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void OnRmd(FTP_e_Rmd e)
        {
            if(this.Rmd != null){
                this.Rmd(this,e);
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to complete MKD(make directory) command.
        /// </summary>
        public event EventHandler<FTP_e_Mkd> Mkd = null;

        #region method OnMkd

        /// <summary>
        /// Raises <b>Mkd</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void OnMkd(FTP_e_Mkd e)
        {
            if(this.Mkd != null){
                this.Mkd(this,e);
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to get directory listing.
        /// </summary>
        public event EventHandler<FTP_e_GetDirListing> GetDirListing = null;

        #region method OnGetDirListing

        /// <summary>
        /// Raises <b>GetDirListing</b> event.
        /// </summary>
        /// <param name="e">Event arguments.</param>
        private void OnGetDirListing(FTP_e_GetDirListing e)
        {
            if(this.GetDirListing != null){
                this.GetDirListing(this,e);
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to complete RNTO(rename file/directory to) command.
        /// </summary>
        public event EventHandler<FTP_e_Rnto> Rnto = null;

        #region method OnRnto

        /// <summary>
        /// Raises <b>Rnto</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void OnRnto(FTP_e_Rnto e)
        {
            if(this.Rnto != null){
                this.Rnto(this,e);
            }
        }

        #endregion

        #endregion

    }
}
