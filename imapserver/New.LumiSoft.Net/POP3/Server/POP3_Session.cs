using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Security.Principal;

using LumiSoft.Net.IO;
using LumiSoft.Net.TCP;
using LumiSoft.Net.AUTH;

namespace LumiSoft.Net.POP3.Server
{
    /// <summary>
    /// This class implements POP3 server session. Defined RFC 1939.
    /// </summary>
    public class POP3_Session : TCP_ServerSession
    {
        private Dictionary<string,AUTH_SASL_ServerMechanism>  m_pAuthentications = null;
        private bool                                          m_SessionRejected  = false;
        private int                                           m_BadCommands      = 0;
        private string                                        m_UserName         = null;
        private GenericIdentity                               m_pUser            = null;
        private KeyValueCollection<string,POP3_ServerMessage> m_pMessages        = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public POP3_Session()
        {
            m_pAuthentications = new Dictionary<string,AUTH_SASL_ServerMechanism>(StringComparer.CurrentCultureIgnoreCase);
            m_pMessages = new KeyValueCollection<string,POP3_ServerMessage>();
        }


        #region override method Start

        /// <summary>
        /// Starts session processing.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            /* RFC 1939 4.
                Once the TCP connection has been opened by a POP3 client, the POP3
                server issues a one line greeting.  This can be any positive
                response.  An example might be:

                    S:  +OK POP3 server ready

            */
            
            try{
                string reply = null;
                if(string.IsNullOrEmpty(this.Server.GreetingText)){
                    reply = "+OK [" + Net_Utils.GetLocalHostName(this.LocalHostName) + "] POP3 Service Ready.";
                }
                else{
                    reply = "+OK " + this.Server.GreetingText;
                }

                POP3_e_Started e = OnStarted(reply);

                if(!string.IsNullOrEmpty(e.Response)){
                    WriteLine(reply.ToString());
                }

                // Setup rejected flag, so we respond "-ERR Session rejected." any command except QUIT.
                if(string.IsNullOrEmpty(e.Response) || e.Response.ToUpper().StartsWith("-ERR")){
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
                    // Raise POP3_Server.Error event.
                    base.OnError(x);

                    // Try to send "-ERR Internal server error."
                    try{
                        WriteLine("-ERR Internal server error.");
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
                // TODO: ? We should close active message stream.

                WriteLine("-ERR Idle timeout, closing connection.");
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

                if(cmd == "STLS"){
                    STLS(args);
                }
                else if(cmd == "USER"){
                    USER(args);
                }
                else if(cmd == "PASS"){
                    PASS(args);
                }
                else if(cmd == "PASS"){
                    PASS(args);
                }
                else if(cmd == "AUTH"){
                    AUTH(args);
                }
                else if(cmd == "STAT"){
                    STAT(args);
                }
                else if(cmd == "LIST"){
                    LIST(args);
                }
                else if(cmd == "UIDL"){
                    UIDL(args);
                }
                else if(cmd == "TOP"){
                    TOP(args);
                }
                else if(cmd == "RETR"){
                    RETR(args);
                }
                else if(cmd == "DELE"){
                    DELE(args);
                }
                else if(cmd == "NOOP"){
                    NOOP(args);
                }
                else if(cmd == "RSET"){
                    RSET(args);
                }
                else if(cmd == "DELE"){
                    DELE(args);
                }
                else if(cmd == "CAPA"){
                    CAPA(args);
                }
                else if(cmd == "QUIT"){
                    QUIT(args);
                }
                else{
                     m_BadCommands++;

                     // Maximum allowed bad commands exceeded.
                     if(this.Server.MaxBadCommands != 0 && m_BadCommands > this.Server.MaxBadCommands){
                         WriteLine("-ERR Too many bad commands, closing transmission channel.");
                         Disconnect();
                         return false;
                     }
                            
                     WriteLine("-ERR Error: command '" + cmd + "' not recognized.");
                 }
             }
             catch(Exception x){
                 OnError(x);
             }

             return readNextCommand;
        }

        #endregion


        #region method STLS

        private void STLS(string cmdText)
        {
            /* RFC 2595 4. POP3 STARTTLS extension.
                 Arguments: none

                 Restrictions:
                     Only permitted in AUTHORIZATION state.

                 Discussion:
                     A TLS negotiation begins immediately after the CRLF at the
                     end of the +OK response from the server.  A -ERR response
                     MAY result if a security layer is already active.  Once a
                     client issues a STLS command, it MUST NOT issue further
                     commands until a server response is seen and the TLS
                     negotiation is complete.

                     The STLS command is only permitted in AUTHORIZATION state
                     and the server remains in AUTHORIZATION state, even if
                     client credentials are supplied during the TLS negotiation.
                     The AUTH command [POP-AUTH] with the EXTERNAL mechanism
                     [SASL] MAY be used to authenticate once TLS client
                     credentials are successfully exchanged, but servers
                     supporting the STLS command are not required to support the
                     EXTERNAL mechanism.

                     Once TLS has been started, the client MUST discard cached
                     information about server capabilities and SHOULD re-issue
                     the CAPA command.  This is necessary to protect against
                     man-in-the-middle attacks which alter the capabilities list
                     prior to STLS.  The server MAY advertise different
                     capabilities after STLS.

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

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }
            if(this.IsAuthenticated){
                this.TcpStream.WriteLine("-ERR This ommand is only valid in AUTHORIZATION state (RFC 2595 4).");

                return;
            }
            if(this.IsSecureConnection){
                WriteLine("-ERR Bad sequence of commands: Connection is already secure.");

                return;
            }
            if(this.Certificate == null){
                WriteLine("-ERR TLS not available: Server has no SSL certificate.");

                return;
            }

            WriteLine("+OK Ready to start TLS.");

            try{
                SwitchToSecure();

                // Log
                LogAddText("TLS negotiation completed successfully.");
            }
            catch(Exception x){
                // Log
                LogAddText("TLS negotiation failed: " + x.Message + ".");

                Disconnect();
            }
        }

        #endregion

        #region method USER

        private void USER(string cmdText)
        {
            /* RFC 1939 7. USER
			    Arguments:
				    a string identifying a mailbox (required), which is of
				    significance ONLY to the server
				
			    NOTE:
				    If the POP3 server responds with a positive
				    status indicator ("+OK"), then the client may issue
				    either the PASS command to complete the authentication,
				    or the QUIT command to terminate the POP3 session.			 
			*/

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }
            if(this.IsAuthenticated){
                this.TcpStream.WriteLine("-ERR Re-authentication error.");

                return;
            }
            if(m_UserName != null){
                this.TcpStream.WriteLine("-ERR User name already specified.");

                return;
            }

            m_UserName = cmdText;

            this.TcpStream.WriteLine("+OK User name OK.");
        }

        #endregion

        #region method PASS

        private void PASS(string cmdText)
        {
            /* RFC 1939 7. PASS
			Arguments:
				a server/mailbox-specific password (required)
				
			Restrictions:
				may only be given in the AUTHORIZATION state immediately
				after a successful USER command
				
			NOTE:
				When the client issues the PASS command, the POP3 server
				uses the argument pair from the USER and PASS commands to
				determine if the client should be given access to the
				appropriate maildrop.
				
			Possible Responses:
				+OK maildrop locked and ready
				-ERR invalid password
				-ERR unable to lock maildrop
						
			*/

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }
            if(this.IsAuthenticated){
                this.TcpStream.WriteLine("-ERR Re-authentication error.");

                return;
            }
            if(m_UserName == null){
                this.TcpStream.WriteLine("-ERR Specify user name first.");

                return;
            }
            if(string.IsNullOrEmpty(cmdText)){
                this.TcpStream.WriteLine("-ERR Error in arguments.");

                return;
            }
                        
            POP3_e_Authenticate e = OnAuthenticate(m_UserName,cmdText);
            if(e.IsAuthenticated){
                m_pUser = new GenericIdentity(m_UserName,"POP3-USER/PASS");

                // Get mailbox messages.
                POP3_e_GetMessagesInfo eMessages = OnGetMessagesInfo();
                int seqNo = 1;
                foreach(POP3_ServerMessage message in eMessages.Messages){
                    message.SequenceNumber = seqNo++;
                    m_pMessages.Add(message.UID,message);
                }

                this.TcpStream.WriteLine("+OK Authenticated successfully.");                
            }
            else{
                this.TcpStream.WriteLine("-ERR Authentication failed.");
            }
        }

        #endregion

        #region method AUTH

        private void AUTH(string cmdText)
        {
            /* RFC 1734
				
				AUTH mechanism

					Arguments:
						a string identifying an IMAP4 authentication mechanism,
						such as defined by [IMAP4-AUTH].  Any use of the string
						"imap" used in a server authentication identity in the
						definition of an authentication mechanism is replaced with
						the string "pop".
						
					Possible Responses:
						+OK maildrop locked and ready
						-ERR authentication exchange failed

					Restrictions:
						may only be given in the AUTHORIZATION state

					Discussion:
						The AUTH command indicates an authentication mechanism to
						the server.  If the server supports the requested
						authentication mechanism, it performs an authentication
						protocol exchange to authenticate and identify the user.
						Optionally, it also negotiates a protection mechanism for
						subsequent protocol interactions.  If the requested
						authentication mechanism is not supported, the server						
						should reject the AUTH command by sending a negative
						response.

						The authentication protocol exchange consists of a series
						of server challenges and client answers that are specific
						to the authentication mechanism.  A server challenge,
						otherwise known as a ready response, is a line consisting
						of a "+" character followed by a single space and a BASE64
						encoded string.  The client answer consists of a line
						containing a BASE64 encoded string.  If the client wishes
						to cancel an authentication exchange, it should issue a
						line with a single "*".  If the server receives such an
						answer, it must reject the AUTH command by sending a
						negative response.

						A protection mechanism provides integrity and privacy
						protection to the protocol session.  If a protection
						mechanism is negotiated, it is applied to all subsequent
						data sent over the connection.  The protection mechanism
						takes effect immediately following the CRLF that concludes
						the authentication exchange for the client, and the CRLF of
						the positive response for the server.  Once the protection
						mechanism is in effect, the stream of command and response
						octets is processed into buffers of ciphertext.  Each
						buffer is transferred over the connection as a stream of
						octets prepended with a four octet field in network byte
						order that represents the length of the following data.
						The maximum ciphertext buffer length is defined by the
						protection mechanism.

						The server is not required to support any particular
						authentication mechanism, nor are authentication mechanisms
						required to support any protection mechanisms.  If an AUTH
						command fails with a negative response, the session remains
						in the AUTHORIZATION state and client may try another
						authentication mechanism by issuing another AUTH command,
						or may attempt to authenticate by using the USER/PASS or
						APOP commands.  In other words, the client may request
						authentication types in decreasing order of preference,
						with the USER/PASS or APOP command as a last resort.

						Should the client successfully complete the authentication
						exchange, the POP3 server issues a positive response and
						the POP3 session enters the TRANSACTION state.
						
				Examples:
							S: +OK POP3 server ready
							C: AUTH KERBEROS_V4
							S: + AmFYig==
							C: BAcAQU5EUkVXLkNNVS5FRFUAOCAsho84kLN3/IJmrMG+25a4DT
								+nZImJjnTNHJUtxAA+o0KPKfHEcAFs9a3CL5Oebe/ydHJUwYFd
								WwuQ1MWiy6IesKvjL5rL9WjXUb9MwT9bpObYLGOKi1Qh
							S: + or//EoAADZI=
							C: DiAF5A4gA+oOIALuBkAAmw==
							S: +OK Kerberos V4 authentication successful
								...
							C: AUTH FOOBAR
							S: -ERR Unrecognized authentication type
			 
			*/

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }
            if(this.IsAuthenticated){
                this.TcpStream.WriteLine("-ERR Re-authentication error.");

                return;
            }

            string mechanism = cmdText;

            /* MS specific or someone knows where in RFC let me know about this.
                Empty AUTH commands causes authentication mechanisms listing. 
             
                C: AUTH
                S: PLAIN
                S: .
                
                http://msdn.microsoft.com/en-us/library/cc239199.aspx
            */
            if(string.IsNullOrEmpty(mechanism)){
                StringBuilder resp = new StringBuilder();
                resp.Append("+OK\r\n");
                foreach(AUTH_SASL_ServerMechanism m in m_pAuthentications.Values){
                    resp.Append(m.Name + "\r\n");
                }
                resp.Append(".\r\n");

                WriteLine(resp.ToString());

                return;
            }

            if(!this.Authentications.ContainsKey(mechanism)){
                WriteLine("-ERR Not supported authentication mechanism.");
                return;
            }

            byte[] clientResponse = new byte[0];
            AUTH_SASL_ServerMechanism auth = this.Authentications[mechanism];
            auth.Reset();
            while(true){
                byte[] serverResponse = auth.Continue(clientResponse);
                // Authentication completed.
                if(auth.IsCompleted){
                    if(auth.IsAuthenticated){
                        m_pUser = new GenericIdentity(auth.UserName,"SASL-" + auth.Name);

                        // Get mailbox messages.
                        POP3_e_GetMessagesInfo eMessages = OnGetMessagesInfo();
                        int seqNo = 1;
                        foreach(POP3_ServerMessage message in eMessages.Messages){
                            message.SequenceNumber = seqNo++;
                            m_pMessages.Add(message.UID,message);
                        }

                        WriteLine("+OK Authentication succeeded.");
                    }
                    else{
                        WriteLine("-ERR Authentication credentials invalid.");
                    }
                    break;
                }
                // Authentication continues.
                else{
                    // Send server challange.
                    if(serverResponse.Length == 0){
                        WriteLine("+ ");
                    }
                    else{
                        WriteLine("+ " + Convert.ToBase64String(serverResponse));
                    }

                    // Read client response. 
                    SmartStream.ReadLineAsyncOP readLineOP = new SmartStream.ReadLineAsyncOP(new byte[32000],SizeExceededAction.JunkAndThrowException);
                    this.TcpStream.ReadLine(readLineOP,false);
                    if(readLineOP.Error != null){
                        throw readLineOP.Error;
                    }
                    // Log
                    if(this.Server.Logger != null){
                        this.Server.Logger.AddRead(this.ID,this.AuthenticatedUserIdentity,readLineOP.BytesInBuffer,"base64 auth-data",this.LocalEndPoint,this.RemoteEndPoint);
                    }

                    // Client canceled authentication.
                    if(readLineOP.LineUtf8 == "*"){
                        WriteLine("-ERR Authentication canceled.");
                        return;
                    }
                    // We have base64 client response, decode it.
                    else{
                        try{
                            clientResponse = Convert.FromBase64String(readLineOP.LineUtf8);
                        }
                        catch{
                            WriteLine("-ERR Invalid client response '" + clientResponse + "'.");
                            return;
                        }
                    }
                }
            }
        }

        #endregion


        #region method STAT

        private void STAT(string cmdText)
        {
            /* RFC 1939 5. STAT
			NOTE:
				The positive response consists of "+OK" followed by a single
				space, the number of messages in the maildrop, a single
				space, and the size of the maildrop in octets.
				
				Note that messages marked as deleted are not counted in
				either total.
			 
			Example:
				C: STAT
				S: +OK 2 320
			*/

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
                WriteLine("-ERR Authentication required.");

                return;
            }

            // Calculate count and total size in bytes, exclude marked for deletion messages.
            int count = 0;
            int size  = 0;
            foreach(POP3_ServerMessage msg in m_pMessages){
                if(!msg.IsMarkedForDeletion){
                    count++;
                    size += msg.Size;
                }
            }

            WriteLine("+OK " + count + " " + size);
        }

        #endregion

        #region method LIST

        private void LIST(string cmdText)
        {
            /* RFC 1939 5. LIST
			Arguments:
				a message-number (optional), which, if present, may NOT
				refer to a message marked as deleted
			 
			NOTE:
				If an argument was given and the POP3 server issues a
				positive response with a line containing information for
				that message.

				If no argument was given and the POP3 server issues a
				positive response, then the response given is multi-line.
				
				Note that messages marked as deleted are not listed.
			
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

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
                WriteLine("-ERR Authentication required.");

                return;
            }

            string[] args = cmdText.Split(' ');

            // List whole mailbox.
            if(string.IsNullOrEmpty(cmdText)){
                // Calculate count and total size in bytes, exclude marked for deletion messages.
                int count = 0;
                int size  = 0;
                foreach(POP3_ServerMessage msg in m_pMessages){
                    if(!msg.IsMarkedForDeletion){
                        count++;
                        size += msg.Size;
                    }
                }

                StringBuilder response = new StringBuilder();
                response.Append("+OK " + count + " messages (" + size + " bytes).\r\n");
                foreach(POP3_ServerMessage msg in m_pMessages){
                    response.Append(msg.SequenceNumber + " " + msg.Size + "\r\n");
                }
                response.Append(".");

                 WriteLine(response.ToString());
            }
            // Single message info listing.
            else{
                if(args.Length > 1 || !Net_Utils.IsInteger(args[0])){
                    WriteLine("-ERR Error in arguments.");

                    return;
                }

                POP3_ServerMessage msg = null;
                if(m_pMessages.TryGetValueAt(Convert.ToInt32(args[0]) - 1,out msg)){
                    // Block messages marked for deletion.
                    if(msg.IsMarkedForDeletion){
                        WriteLine("-ERR Invalid operation: Message marked for deletion.");

                        return;
                    }

                    WriteLine("+OK " + msg.SequenceNumber + " " + msg.Size);
                }
                else{
                    WriteLine("-ERR no such message or message marked for deletion.");
                }
            }
        }

        #endregion

        #region method UIDL

        private void UIDL(string cmdText)
        {
            /* RFC 1939 UIDL [msg]
			Arguments:
			    a message-number (optional), which, if present, may NOT
				refer to a message marked as deleted
				
			NOTE:
				If an argument was given and the POP3 server issues a positive
				response with a line containing information for that message.

				If no argument was given and the POP3 server issues a positive
				response, then the response given is multi-line.  After the
				initial +OK, for each message in the maildrop, the POP3 server
				responds with a line containing information for that message.	
				
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
				S: -ERR no such message
			*/

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
                WriteLine("-ERR Authentication required.");

                return;
            }

            string[] args = cmdText.Split(' ');

            // List whole mailbox.
            if(string.IsNullOrEmpty(cmdText)){
                // Calculate count and total size in bytes, exclude marked for deletion messages.
                int count = 0;
                int size  = 0;
                foreach(POP3_ServerMessage msg in m_pMessages){
                    if(!msg.IsMarkedForDeletion){
                        count++;
                        size += msg.Size;
                    }
                }

                StringBuilder response = new StringBuilder();
                response.Append("+OK " + count + " messages (" + size + " bytes).\r\n");
                foreach(POP3_ServerMessage msg in m_pMessages){
                    response.Append(msg.SequenceNumber + " " + msg.UID + "\r\n");
                }
                response.Append(".");

                 WriteLine(response.ToString());
            }
            // Single message info listing.
            else{
                if(args.Length > 1){
                    WriteLine("-ERR Error in arguments.");

                    return;
                }

                POP3_ServerMessage msg = null;
                if(m_pMessages.TryGetValueAt(Convert.ToInt32(args[0]) - 1,out msg)){
                    // Block messages marked for deletion.
                    if(msg.IsMarkedForDeletion){
                        WriteLine("-ERR Invalid operation: Message marked for deletion.");

                        return;
                    }

                    WriteLine("+OK " + msg.SequenceNumber + " " + msg.UID);
                }
                else{
                    WriteLine("-ERR no such message or message marked for deletion.");
                }
            }
        }

        #endregion

        #region method TOP

        private void TOP(string cmdText)
        {
            /* RFC 1939 7. TOP
			    Arguments:
				    a message-number (required) which may NOT refer to to a
				    message marked as deleted, and a non-negative number
				    of lines (required)
		
			    NOTE:
				    If the POP3 server issues a positive response, then the
				    response given is multi-line.  After the initial +OK, the
				    POP3 server sends the headers of the message, the blank
				    line separating the headers from the body, and then the
				    number of lines of the indicated message's body, being
				    careful to byte-stuff the termination character (as with
				    all multi-line responses).
			
			    Examples:
				    C: TOP 1 10
				    S: +OK
				    S: <the POP3 server sends the headers of the
					    message, a blank line, and the first 10 lines
					    of the body of the message>
				    S: .
                    ...
				    C: TOP 100 3
				    S: -ERR no such message
			 
			*/

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
                WriteLine("-ERR Authentication required.");

                return;
            }

            string[] args = cmdText.Split(' ');

            if(args.Length != 2 || !Net_Utils.IsInteger(args[0]) || !Net_Utils.IsInteger(args[1])){
                WriteLine("-ERR Error in arguments.");

                return;
            }

            POP3_ServerMessage msg = null;
            if(m_pMessages.TryGetValueAt(Convert.ToInt32(args[0]) - 1,out msg)){
                // Block messages marked for deletion.
                if(msg.IsMarkedForDeletion){
                    WriteLine("-ERR Invalid operation: Message marked for deletion.");

                    return;
                }

                POP3_e_GetTopOfMessage e = OnGetTopOfMessage(msg,Convert.ToInt32(args[1]));

                // User didn't provide us message stream, assume that message deleted(for example by IMAP during this POP3 session).
                if(e.Data == null){
                    WriteLine("-ERR no such message.");
                }
                else{
                    WriteLine("+OK Start sending top of message.");

                    long countWritten = this.TcpStream.WritePeriodTerminated(new MemoryStream(e.Data));

                    // Log.
                    if(this.Server.Logger != null){
                        this.Server.Logger.AddWrite(this.ID,this.AuthenticatedUserIdentity,countWritten,"Wrote top of message(" + countWritten + " bytes).",this.LocalEndPoint,this.RemoteEndPoint);
                    }
                }
            }
            else{
                WriteLine("-ERR no such message.");
            }
        }

        #endregion

        #region method RETR

        private void RETR(string cmdText)
        {
            /* RFC 1939 5. RETR
			    Arguments:
				    a message-number (required) which may NOT refer to a
				    message marked as deleted
			 
			    NOTE:
				    If the POP3 server issues a positive response, then the
				    response given is multi-line.  After the initial +OK, the
				    POP3 server sends the message corresponding to the given
				    message-number, being careful to byte-stuff the termination
				    character (as with all multi-line responses).
				
			    Example:
				    C: RETR 1
				    S: +OK 120 octets
				    S: <the POP3 server sends the entire message here>
				    S: .
			
			*/

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
                WriteLine("-ERR Authentication required.");

                return;
            }

            string[] args = cmdText.Split(' ');

            if(args.Length != 1 || !Net_Utils.IsInteger(args[0])){
                WriteLine("-ERR Error in arguments.");

                return;
            }

            POP3_ServerMessage msg = null;
            if(m_pMessages.TryGetValueAt(Convert.ToInt32(args[0]) - 1,out msg)){
                // Block messages marked for deletion.
                if(msg.IsMarkedForDeletion){
                    WriteLine("-ERR Invalid operation: Message marked for deletion.");

                    return;
                }

                POP3_e_GetMessageStream e = OnGetMessageStream(msg);

                // User didn't provide us message stream, assume that message deleted(for example by IMAP during this POP3 session).
                if(e.MessageStream == null){
                    WriteLine("-ERR no such message.");
                }
                else{
                    try{
                        WriteLine("+OK Start sending message.");

                        long countWritten = this.TcpStream.WritePeriodTerminated(e.MessageStream);

                        // Log.
                        if(this.Server.Logger != null){
                            this.Server.Logger.AddWrite(this.ID,this.AuthenticatedUserIdentity,countWritten,"Wrote message(" + countWritten + " bytes).",this.LocalEndPoint,this.RemoteEndPoint);
                        }
                    }
                    finally{
                        // Close message stream if CloseStream = true.
                        if(e.CloseMessageStream){
                            e.MessageStream.Dispose();
                        }
                    }                    
                }
            }
            else{
                WriteLine("-ERR no such message.");
            }
        }

        #endregion

        #region method DELE

        private void DELE(string cmdText)
        {
            /* RFC 1939 5. DELE
			    Arguments:
				    a message-number (required) which may NOT refer to a
				    message marked as deleted
			 
			    NOTE:
				    The POP3 server marks the message as deleted.  Any future
				    reference to the message-number associated with the message
				    in a POP3 command generates an error.  The POP3 server does
				    not actually delete the message until the POP3 session
				    enters the UPDATE state.
			*/

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
                WriteLine("-ERR Authentication required.");

                return;
            }

            string[] args = cmdText.Split(' ');

            if(args.Length != 1 || !Net_Utils.IsInteger(args[0])){
                WriteLine("-ERR Error in arguments.");

                return;
            }

            POP3_ServerMessage msg = null;
            if(m_pMessages.TryGetValueAt(Convert.ToInt32(args[0]) - 1,out msg)){  
                if(!msg.IsMarkedForDeletion){
                    msg.SetIsMarkedForDeletion(true);

                    WriteLine("+OK Message marked for deletion.");
                }
                else{
                    WriteLine("-ERR Message already marked for deletion.");
                }
            }
            else{
                WriteLine("-ERR no such message.");
            }
        }

        #endregion

        #region method NOOP

        private void NOOP(string cmdText)
        {
            /* RFC 1939 5. NOOP
			    NOTE:
				    The POP3 server does nothing, it merely replies with a
				    positive response.
			*/

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
                WriteLine("-ERR Authentication required.");

                return;
            }

            WriteLine("+OK");
        }

        #endregion

        #region method RSET

        private void RSET(string cmdText)
        {
            /* RFC 1939 5. RSET
			Discussion:
				If any messages have been marked as deleted by the POP3
				server, they are unmarked.  The POP3 server then replies
				with a positive response.
			*/

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }
            if(!this.IsAuthenticated){
                WriteLine("-ERR Authentication required.");

                return;
            }

            // Unmark messages marked for deletion.
            foreach(POP3_ServerMessage msg in m_pMessages){
                msg.SetIsMarkedForDeletion(false);
            }

            WriteLine("+OK");

            OnReset();
        }

        #endregion


        #region method CAPA

        private void CAPA(string cmdText)
        {
            /* RFC 2449 5.  The CAPA Command
			
				The POP3 CAPA command returns a list of capabilities supported by the
				POP3 server.  It is available in both the AUTHORIZATION and
				TRANSACTION states.

				A capability description MUST document in which states the capability
				is announced, and in which states the commands are valid.

				Capabilities available in the AUTHORIZATION state MUST be announced
				in both states.

				If a capability is announced in both states, but the argument might
				differ after authentication, this possibility MUST be stated in the
				capability description.

				(These requirements allow a client to issue only one CAPA command if
				it does not use any TRANSACTION-only capabilities, or any
				capabilities whose values may differ after authentication.)

				If the authentication step negotiates an integrity protection layer,
				the client SHOULD reissue the CAPA command after authenticating, to
				check for active down-negotiation attacks.

				Each capability may enable additional protocol commands, additional
				parameters and responses for existing commands, or describe an aspect
				of server behavior.  These details are specified in the description
				of the capability.
				
				Section 3 describes the CAPA response using [ABNF].  When a
				capability response describes an optional command, the <capa-tag>
				SHOULD be identical to the command keyword.  CAPA response tags are
				case-insensitive.

				CAPA

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

            if(m_SessionRejected){
                WriteLine("-ERR Bad sequence of commands: Session rejected.");

                return;
            }

            StringBuilder capaResponse = new StringBuilder();
			capaResponse.Append("+OK Capability list follows\r\n");
			capaResponse.Append("PIPELINING\r\n");
			capaResponse.Append("UIDL\r\n");
			capaResponse.Append("TOP\r\n");

            StringBuilder sasl = new StringBuilder();
            foreach(AUTH_SASL_ServerMechanism authMechanism in this.Authentications.Values){
                if(!authMechanism.RequireSSL || (authMechanism.RequireSSL && this.IsSecureConnection)){
                    sasl.Append(authMechanism.Name + " ");
                }
            }
            if(sasl.Length > 0){
                capaResponse.Append("SASL " + sasl.ToString().Trim() + "\r\n");
            }

            if(!this.IsSecureConnection && this.Certificate != null){
                capaResponse.Append("STLS\r\n");
            }

			capaResponse.Append(".");

            WriteLine(capaResponse.ToString());
        }

        #endregion

        #region method QUIT

        private void QUIT(string cmdText)
        {
            /* RFC 1939 6. QUIT
			   NOTE:
                When the client issues the QUIT command from the TRANSACTION state,
				the POP3 session enters the UPDATE state.  (Note that if the client
				issues the QUIT command from the AUTHORIZATION state, the POP3
				session terminates but does NOT enter the UPDATE state.)

				If a session terminates for some reason other than a client-issued
				QUIT command, the POP3 session does NOT enter the UPDATE state and
				MUST not remove any messages from the maildrop.
             
				The POP3 server removes all messages marked as deleted
				from the maildrop and replies as to the status of this
				operation.  If there is an error, such as a resource
				shortage, encountered while removing messages, the
				maildrop may result in having some or none of the messages
				marked as deleted be removed.  In no case may the server
				remove any messages not marked as deleted.

				Whether the removal was successful or not, the server
				then releases any exclusive-access lock on the maildrop
				and closes the TCP connection.
			*/

            try{                
                if(this.IsAuthenticated){
                    // Delete messages marked for deletion.
                    foreach(POP3_ServerMessage msg in m_pMessages){
                        if(msg.IsMarkedForDeletion){
                            OnDeleteMessage(msg);
                        }
                    }
                }

                WriteLine("+OK <" + Net_Utils.GetLocalHostName(this.LocalHostName) + "> Service closing transmission channel.");                
            }
            catch{
            }
            Disconnect();
            Dispose();
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
                this.Server.Logger.AddWrite(this.ID,this.AuthenticatedUserIdentity,countWritten,line,this.LocalEndPoint,this.RemoteEndPoint);
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


        #region Properties implementation

        /// <summary>
        /// Gets session owner POP3 server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public new POP3_Server Server
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return (POP3_Server)base.Server;
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

        #endregion

        #region Events implementation

        /// <summary>
        /// Is raised when session has started processing and needs to send +OK greeting or -ERR error resposne to the connected client.
        /// </summary>
        public event EventHandler<POP3_e_Started> Started = null;

        #region method OnStarted

        /// <summary>
        /// Raises <b>Started</b> event.
        /// </summary>
        /// <param name="reply">Default POP3 server reply.</param>
        /// <returns>Returns event args.</returns>
        private POP3_e_Started OnStarted(string reply)
        {
            POP3_e_Started eArgs = new POP3_e_Started(reply);

            if(this.Started != null){                
                this.Started(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to authenticate session using USER/PASS POP3 authentication.
        /// </summary>
        public event EventHandler<POP3_e_Authenticate> Authenticate = null;

        #region method OnAuthenticate

        /// <summary>
        /// Raises <b>Authenticate</b> event.
        /// </summary>
        /// <param name="user">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>Returns event args.</returns>
        private POP3_e_Authenticate OnAuthenticate(string user,string password)
        {
            POP3_e_Authenticate eArgs = new POP3_e_Authenticate(user,password);

            if(this.Authenticate != null){
                this.Authenticate(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to get mailbox messsages info.
        /// </summary>
        public event EventHandler<POP3_e_GetMessagesInfo> GetMessagesInfo = null;

        #region method OnGetMessagesInfo

        /// <summary>
        /// Raises <b>GetMessagesInfo</b> event.
        /// </summary>
        /// <returns>Returns event args.</returns>
        private POP3_e_GetMessagesInfo OnGetMessagesInfo()
        {
            POP3_e_GetMessagesInfo eArgs = new POP3_e_GetMessagesInfo();

            if(this.GetMessagesInfo != null){
                this.GetMessagesInfo(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to get top of the specified message data.
        /// </summary>
        public event EventHandler<POP3_e_GetTopOfMessage> GetTopOfMessage = null;

        #region method OnGetTopOfMessage

        /// <summary>
        /// Raises <b>GetTopOfMessage</b> event.
        /// </summary>
        /// <param name="message">Message which top data to get.</param>
        /// <param name="lines">Number of message-body lines to get.</param>
        /// <returns>Returns event args.</returns>
        private POP3_e_GetTopOfMessage OnGetTopOfMessage(POP3_ServerMessage message,int lines)
        {
            POP3_e_GetTopOfMessage eArgs = new POP3_e_GetTopOfMessage(message,lines);

            if(this.GetTopOfMessage != null){
                this.GetTopOfMessage(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to get specified message stream.
        /// </summary>
        public event EventHandler<POP3_e_GetMessageStream> GetMessageStream = null;

        #region method OnGetMessageStream

        /// <summary>
        /// Raises <b>GetMessageStream</b> event.
        /// </summary>
        /// <param name="message">Message stream to get.</param>
        /// <returns>Returns event arguments.</returns>
        private POP3_e_GetMessageStream OnGetMessageStream(POP3_ServerMessage message)
        {
            POP3_e_GetMessageStream eArgs = new POP3_e_GetMessageStream(message);

            if(this.GetMessageStream != null){
                this.GetMessageStream(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// This event is raised when session needs to delete specified message.
        /// </summary>
        public event EventHandler<POP3_e_DeleteMessage> DeleteMessage = null;

        #region method OnDeleteMessage

        /// <summary>
        /// Raises <b>DeleteMessage</b> event.
        /// </summary>
        /// <param name="message">Message to delete.</param>
        private void OnDeleteMessage(POP3_ServerMessage message)
        {
            if(this.DeleteMessage != null){
                this.DeleteMessage(this,new POP3_e_DeleteMessage(message));
            }
        }

        #endregion

        /// <summary>
        /// This event is raised when session is reset by remote user.
        /// </summary>
        public event EventHandler Reset = null;

        #region method OnReset

        /// <summary>
        /// Raises <b>Reset</b> event.
        /// </summary>
        private void OnReset()
        {
            if(this.Reset != null){
                this.Reset(this,new EventArgs());
            }
        }

        #endregion

        #endregion
    }
}
