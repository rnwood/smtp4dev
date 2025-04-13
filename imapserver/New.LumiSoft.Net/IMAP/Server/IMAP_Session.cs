using System;
using System.Collections.Generic;
using System.Text;
using System.IO;
using System.Net.Sockets;
using System.Security.Principal;

using LumiSoft.Net.IO;
using LumiSoft.Net.TCP;
using LumiSoft.Net.AUTH;
using LumiSoft.Net.MIME;
using LumiSoft.Net.Mail;

namespace LumiSoft.Net.IMAP.Server
{
    /// <summary>
    /// This class implements IMAP server session. Defined RFC 3501.
    /// </summary>
    public class IMAP_Session : TCP_ServerSession
    {
        #region class _SelectedFolder

        /// <summary>
        /// This class holds selected folder data.
        /// </summary>
        private class _SelectedFolder
        {
            private string                 m_Folder        = null;
            private bool                   m_IsReadOnly    = false;
            private List<IMAP_MessageInfo> m_pMessagesInfo = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="folder">Folder name with optional path.</param>
            /// <param name="isReadOnly">Specifies if folder is read only.</param>
            /// <param name="messagesInfo">Messages info.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>folder</b> or <b>messagesInfo</b> is null reference.</exception>
            public _SelectedFolder(string folder,bool isReadOnly,List<IMAP_MessageInfo> messagesInfo)
            {
                if(folder == null){
                    throw new ArgumentNullException("folder");
                }
                if(messagesInfo == null){
                    throw new ArgumentNullException("messagesInfo");
                }

                m_Folder        = folder;
                m_IsReadOnly    = isReadOnly;
                m_pMessagesInfo = messagesInfo;

                Reindex();
            }
                        

            #region method Filter

            /// <summary>
            /// Gets messages which match to the specified sequence set.
            /// </summary>
            /// <param name="uid">Specifies if sequence set contains UID or sequence numbers.</param>
            /// <param name="seqSet">Sequence set.</param>
            /// <returns>Returns messages which match to the specified sequence set.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>seqSet</b> is null reference.</exception>
            internal IMAP_MessageInfo[] Filter(bool uid,IMAP_t_SeqSet seqSet)
            {
                if(seqSet == null){
                    throw new ArgumentNullException("seqSet");
                }

                List<IMAP_MessageInfo> retVal = new List<IMAP_MessageInfo>();
                for(int i=0;i<m_pMessagesInfo.Count;i++){
                    IMAP_MessageInfo msgInfo = m_pMessagesInfo[i];                    
                    if(uid){
                        if(seqSet.Contains(msgInfo.UID)){
                            retVal.Add(msgInfo);
                        }
                    }
                    else{
                        if(seqSet.Contains(i + 1)){
                            retVal.Add(msgInfo);
                        }
                    }
                }

                return retVal.ToArray();
            }

            #endregion

            #region method RemoveMessage

            /// <summary>
            /// Removes specified message from messages info.
            /// </summary>
            /// <param name="message">Message info.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>message</b> is null reference.</exception>
            internal void RemoveMessage(IMAP_MessageInfo message)
            {
                if(message == null){
                    throw new ArgumentNullException("message");
                }

                m_pMessagesInfo.Remove(message);
            }

            #endregion

            #region method GetSeqNo

            /// <summary>
            /// Gets specified message info IMAP 1-based sequence number.
            /// </summary>
            /// <param name="msgInfo">Message info.</param>
            /// <returns>Returns specified message info IMAP 1-based sequence number.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>msgInfo</b> is null reference.</exception>
            internal int GetSeqNo(IMAP_MessageInfo msgInfo)
            {
                if(msgInfo == null){
                    throw new ArgumentNullException("msgInfo");
                }

                return m_pMessagesInfo.IndexOf(msgInfo) + 1;
            }

            /// <summary>
            /// Gets specified message IMAP 1-based sequence number.
            /// </summary>
            /// <param name="uid">Message UID.</param>
            /// <returns>Returns specified message info IMAP 1-based sequence number or -1 if no such message.</returns>
            internal int GetSeqNo(long uid)
            {
                foreach(IMAP_MessageInfo msgInfo in m_pMessagesInfo){
                    if(msgInfo.UID == uid){
                        return msgInfo.SeqNo;
                    }
                }
                
                return -1;
            }

            #endregion

            #region method Reindex

            /// <summary>
            /// Reindexes messages sequence numbers.
            /// </summary>
            internal void Reindex()
            {
                for(int i=0;i<m_pMessagesInfo.Count;i++){
                    m_pMessagesInfo[i].SeqNo = i + 1;
                }
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets folder name with optional path.
            /// </summary>
            public string Folder
            {
                get{ return m_Folder; }
            }

            /// <summary>
            /// Gets if folder is read-only.
            /// </summary>
            public bool IsReadOnly
            {
                get{ return m_IsReadOnly; }
            }

            /// <summary>
            /// Gets messages info.
            /// </summary>
            internal IMAP_MessageInfo[] MessagesInfo
            {
                get{ return m_pMessagesInfo.ToArray(); }
            }

            #endregion
        }

        #endregion

        #region class _CmdReader

        /// <summary>
        /// This class implements IMAP client command reader.
        /// </summary>
        /// <remarks>Because IMAP command can contain literal strings, then command text can be multiline.</remarks>
        private class _CmdReader
        {
            private IMAP_Session m_pSession       = null;
            private string       m_InitialCmdLine = null;
            private Encoding     m_pCharset       = null;
            private string       m_CmdLine        = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="session">Owner IMAP session.</param>
            /// <param name="initialCmdLine">IMAP client initial command line.</param>
            /// <param name="charset">IMAP literal strings charset encoding.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>session</b>,<b>initialCmdLine</b> or <b>charset</b> is null reference.</exception>
            public _CmdReader(IMAP_Session session,string initialCmdLine,Encoding charset)
            {    
                if(session == null){
                    throw new ArgumentNullException("session");
                }
                if(initialCmdLine == null){
                    throw new ArgumentNullException("initialCmdLine");
                }
                if(charset == null){
                    throw new ArgumentNullException("charset");
                }

                m_pSession       = session;
                m_InitialCmdLine = initialCmdLine;
                m_pCharset       = charset;
            }


            #region method Start

            /// <summary>
            /// Start operation processing.
            /// </summary>
            public void Start()
            {
                /* RFC 3501.
                    literal = "{" number "}" CRLF *CHAR8
                              ; Number represents the number of CHAR8s
                */

                // TODO: Async
                // TODO: Limit total command size. 64k ? 

                
                // If initial command line ends with literal string, read literal string and remaining command text.
                if(EndsWithLiteralString(m_InitialCmdLine)){
                    StringBuilder cmdText     = new StringBuilder();
                    int           literalSize = GetLiteralSize(m_InitialCmdLine);

                    // Add initial command line part to command text.
                    cmdText.Append(RemoveLiteralSpecifier(m_InitialCmdLine));

                    SmartStream.ReadLineAsyncOP readLineOP = new SmartStream.ReadLineAsyncOP(new byte[32000],SizeExceededAction.JunkAndThrowException);
                    while(true){
                        #region Read literal string

                        // Send "+ Continue".
                        m_pSession.WriteLine("+ Continue.");

                        // Read literal string.
                        MemoryStream msLiteral = new MemoryStream();
                        m_pSession.TcpStream.ReadFixedCount(msLiteral,literalSize);
                        
                        // Log
                        m_pSession.LogAddRead(literalSize,m_pCharset.GetString(msLiteral.ToArray()));

                        // Add to command text as quoted string.
                        cmdText.Append(TextUtils.QuoteString(m_pCharset.GetString(msLiteral.ToArray())));

                        #endregion
                        
                        #region Read continuing command text

                        // Read continuing command text.
                        m_pSession.TcpStream.ReadLine(readLineOP,false);

                        // We have error.
                        if(readLineOP.Error != null){
                            throw readLineOP.Error;
                        }
                        else{
                            string line = readLineOP.LineUtf8;

                            // Log
                            m_pSession.LogAddRead(readLineOP.BytesInBuffer,line);

                            // Add command line part to command text.
                            if(EndsWithLiteralString(line)){
                                cmdText.Append(RemoveLiteralSpecifier(line));
                            }
                            else{
                                cmdText.Append(line);
                            }

                            // No more literal string, we are done.
                            if(!EndsWithLiteralString(line)){
                                break;
                            }
                            else{
                                literalSize = GetLiteralSize(line);
                            }
                        }

                        #endregion
                    }

                    m_CmdLine = cmdText.ToString();
                }
                // We have no literal string, so initial cmd line is final.
                else{
                    m_CmdLine = m_InitialCmdLine;
                }                
            }

            #endregion


            #region method EndsWithLiteralString

            /// <summary>
            /// Cheks if specified value ends with IMAP literal string.
            /// </summary>
            /// <param name="value">Data value.</param>
            /// <returns>Returns true if value ends with IMAP literal string, otherwise false.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
            private bool EndsWithLiteralString(string value)
            {
                if(value == null){
                    throw new ArgumentNullException("value");
                }

                if(value.EndsWith("}")){
                    int    digitCount = 0;
                    char[] chars      = value.ToCharArray();
                    for(int i=chars.Length-2;i>=0;i--){
                        // We have literal string start tag.
                        if(chars[i] == '{'){
                            break;
                        }
                        // Literal string length specifier digit.
                        else if(char.IsDigit(chars[i])){
                            digitCount++;
                        }
                        // Not IMAP literal string char, so we don't have literal string.
                        else{
                            return false;
                        }
                    }

                    // We must have at least single digit literal string length specifier.
                    if(digitCount > 0){
                        return true;
                    }
                }

                return false;
            }

            #endregion

            #region method GetLiteralSize

            /// <summary>
            /// Gets literal string bytes count.
            /// </summary>
            /// <param name="cmdLine">Command line with ending literal string.</param>
            /// <returns>Returns literal string byte count.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>cmdLine</b> is null reference.</exception>
            private int GetLiteralSize(string cmdLine)
            {
                if(cmdLine == null){
                    throw new ArgumentNullException("cmdLine");
                }

                return Convert.ToInt32(cmdLine.Substring(cmdLine.LastIndexOf('{') + 1,cmdLine.Length - cmdLine.LastIndexOf('{') - 2));
            }

            #endregion

            #region method RemoveLiteralSpecifier

            /// <summary>
            /// Removes literal string specifier({no_bytes}) from the specified string.
            /// </summary>
            /// <param name="value">Command line with ending literal string specifier.</param>
            /// <returns>Returns command line without literal string specifier.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>value</b> is null reference.</exception>
            private string RemoveLiteralSpecifier(string value)
            {
                if(value == null){
                    throw new ArgumentNullException("value");
                }

                return value.Substring(0,value.LastIndexOf('{'));
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Gets command line text.
            /// </summary>
            public string CmdLine
            {
                get{ return m_CmdLine;}
            }

            #endregion
        }

        #endregion

        #region class ResponseSender

        /// <summary>
        /// This class implements IMAP response sender.
        /// </summary>
        private class ResponseSender
        {
            #region class QueueItem

            /// <summary>
            /// This class represents queued IMAP response and it's status.
            /// </summary>
            private class QueueItem
            {
                private bool                               m_IsSent                  = false;
                private bool                               m_IsAsync                 = false;
                private IMAP_r                             m_pResponse               = null;
                private EventHandler<EventArgs<Exception>> m_pCompletedAsyncCallback = null;

                /// <summary>
                /// Default constructor.
                /// </summary>
                /// <param name="response">IMAP response.</param>
                /// <param name="completedAsyncCallback">Callback to be called when response sending completes asynchronously.</param>
                /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
                public QueueItem(IMAP_r response,EventHandler<EventArgs<Exception>> completedAsyncCallback)
                {
                    if(response == null){
                        throw new ArgumentNullException("response");
                    }

                    m_pResponse               = response;
                    m_pCompletedAsyncCallback = completedAsyncCallback;
                }


                #region Properties implementation

                /// <summary>
                /// Gets or sets if IMAP response is sent.
                /// </summary>
                public bool IsSent
                {
                    get{ return m_IsSent; }

                    set{ m_IsSent = value; }
                }

                /// <summary>
                /// Gets or sets if sending complte asynchronously.
                /// </summary>
                public bool IsAsync
                {
                    get{ return m_IsAsync; }

                    set{ m_IsAsync = value; }
                }

                /// <summary>
                /// Gets IMAP response.
                /// </summary>
                public IMAP_r Response
                {
                    get{ return m_pResponse; }
                }

                /// <summary>
                /// Gets callback to be called when response sending completes asynchronously.
                /// </summary>
                public EventHandler<EventArgs<Exception>> CompletedAsyncCallback
                {
                    get{ return m_pCompletedAsyncCallback; }
                }

                #endregion
            }

            #endregion

            private object           m_pLock      = new object();
            private IMAP_Session     m_pImap      = null;
            private bool             m_IsSending  = false;
            private Queue<QueueItem> m_pResponses = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="session">Owner IMAP session.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>session</b> is null reference.</exception>
            public ResponseSender(IMAP_Session session)
            {
                if(session == null){
                    throw new ArgumentNullException("session");
                }

                m_pImap = session;

                m_pResponses = new Queue<QueueItem>();
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
            }

            #endregion


            #region method SendResponseAsync

            /// <summary>
            /// Starts sending response.
            /// </summary>
            /// <param name="response">IMAP response.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
            public void SendResponseAsync(IMAP_r response)
            {
                if(response == null){
                    throw new ArgumentNullException("response");
                }

                SendResponseAsync(response,null);
            }

            /// <summary>
            /// Starts sending response.
            /// </summary>
            /// <param name="response">IMAP response.</param>
            /// <param name="completedAsyncCallback">Callback to be called when this method completes asynchronously.</param>
            /// <returns>Returns true is method completed asynchronously(the completedAsyncCallback is raised upon completion of the operation).
            /// Returns false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>response</b> is null reference.</exception>
            public bool SendResponseAsync(IMAP_r response,EventHandler<EventArgs<Exception>> completedAsyncCallback)
            {
                if(response == null){
                    throw new ArgumentNullException("response");
                }

                lock(m_pLock){
                    QueueItem responseItem = new QueueItem(response,completedAsyncCallback);
                    m_pResponses.Enqueue(responseItem);

                    // Start sending response, no active response sending.
                    if(!m_IsSending){
                        SendResponsesAsync();
                    }

                    // Response sent synchronously.
                    if(responseItem.IsSent){
                        return false;
                    }
                    // Response queued or sending is in progress.
                    else{
                        responseItem.IsAsync = true;

                        return true;
                    }
                }
            }

            #endregion


            #region method SendResponsesAsync

            /// <summary>
            /// Starts sending queued responses.
            /// </summary>
            private void SendResponsesAsync()
            {
                m_IsSending = true;

                QueueItem responseItem = null;

                // Create callback which is called when ToStreamAsync comletes asynchronously.
                EventHandler<EventArgs<Exception>> completedAsyncCallback = delegate(object s,EventArgs<Exception> e){
                    try{
                        lock(m_pLock){
                            responseItem.IsSent = true;

                            if(responseItem.IsAsync && responseItem.CompletedAsyncCallback != null){
                                responseItem.CompletedAsyncCallback(this,e);
                            }
                        }

                        // There are more responses available, send them.
                        if(m_pResponses.Count > 0){
                            SendResponsesAsync();
                        }
                        // We are done.
                        else{
                            lock(m_pLock){
                                m_IsSending = false;
                            }
                        }
                    }
                    catch(Exception x){
                        lock(m_pLock){
                            m_IsSending = false;
                        }
                        m_pImap.OnError(x);
                    }
                };

                // Send responses.
                while(m_pResponses.Count > 0){
                    responseItem = m_pResponses.Dequeue();

                    // Response sending completed asynchronously, completedAsyncCallback will be called when operation completes.
                    if(responseItem.Response.SendAsync(m_pImap,completedAsyncCallback)){
                        return;
                    }
                    // Response sending completed synchronously.
                    else{
                        lock(m_pLock){
                            responseItem.IsSent = true;

                            // This method(SendResponsesAsync) is called from completedAsyncCallback.
                            // Response sending has completed asynchronously, call callback.
                            if(responseItem.IsAsync && responseItem.CompletedAsyncCallback != null){
                                responseItem.CompletedAsyncCallback(this,new EventArgs<Exception>(null));
                            }
                        }
                    }
                }

                lock(m_pLock){
                    m_IsSending = false;
                }
            }

            #endregion
        }

        #endregion

        private Dictionary<string,AUTH_SASL_ServerMechanism> m_pAuthentications = null;
        private bool                                         m_SessionRejected  = false;
        private int                                          m_BadCommands      = 0;
        private List<string>                                 m_pCapabilities    = null;
        private char                                         m_FolderSeparator  = '/';
        private GenericIdentity                              m_pUser            = null;
        private _SelectedFolder                              m_pSelectedFolder  = null;
        private IMAP_Mailbox_Encoding                        m_MailboxEncoding  = IMAP_Mailbox_Encoding.ImapUtf7;
        private ResponseSender                               m_pResponseSender  = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public IMAP_Session()
        {
            m_pAuthentications = new Dictionary<string,AUTH_SASL_ServerMechanism>(StringComparer.CurrentCultureIgnoreCase);

            m_pCapabilities = new List<string>();
            m_pCapabilities.AddRange(new string[]{"IMAP4rev1","NAMESPACE","QUOTA","ACL","IDLE","ENABLE","UTF8=ACCEPT","SASL-IR"});

            m_pResponseSender = new ResponseSender(this);
        }

        #region override method Dispose

        /// <summary>
        /// Cleans up any resource being used.
        /// </summary>
        public override void Dispose()
        {
            if(this.IsDisposed){
                return;
            }
            base.Dispose();

            m_pAuthentications = null;
            m_pCapabilities = null;
            m_pUser = null;
            m_pSelectedFolder = null;
            if(m_pResponseSender != null){
                m_pResponseSender.Dispose();
            }

            // Release events
            this.Started         = null;
            this.Login           = null;
            this.Namespace       = null;
            this.List            = null;
            this.Create          = null;
            this.Delete          = null;
            this.Rename          = null;
            this.LSub            = null;
            this.Subscribe       = null;
            this.Unsubscribe     = null;
            this.Select          = null;
            this.GetMessagesInfo = null;
            this.Append          = null;
            this.GetQuotaRoot    = null;
            this.GetQuota        = null;
            this.GetAcl          = null;
            this.SetAcl          = null;
            this.DeleteAcl       = null;
            this.ListRights      = null;
            this.MyRights        = null;
            this.Fetch           = null;
            this.Search          = null;
            this.Store           = null;
            this.Copy            = null;
            this.Expunge         = null;
        }

        #endregion

        #region override method Start

        /// <summary>
        /// Starts session processing.
        /// </summary>
        protected override void Start()
        {
            base.Start();

            /* RFC 3501 7.1.1. Greeting text.
                The untagged form is also used as one of three possible greetings
                at connection startup.  It indicates that the connection is not
                yet authenticated and that a LOGIN command is needed.

                Example:    S: * OK IMAP4rev1 server ready
                            C: A001 LOGIN fred blurdybloop
                            S: * OK [ALERT] System shutdown in 10 minutes
                            S: A001 OK LOGIN Completed
            */
      
            try{
                IMAP_r_u_ServerStatus response = null;
                if(string.IsNullOrEmpty(this.Server.GreetingText)){
                    response = new IMAP_r_u_ServerStatus("OK","<" + Net_Utils.GetLocalHostName(this.LocalHostName) + "> IMAP4rev1 server ready.");
                }
                else{
                    response = new IMAP_r_u_ServerStatus("OK",this.Server.GreetingText);
                }
                
                IMAP_e_Started e = OnStarted(response);

                if(e.Response != null){
                    m_pResponseSender.SendResponseAsync(e.Response);
                }

                // Setup rejected flag, so we respond "* NO Session rejected." any command except LOGOUT.
                if(e.Response == null || e.Response.ResponseCode.Equals("NO",StringComparison.InvariantCultureIgnoreCase)){
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
                    base.OnError(x);

                    // Try to send "* BAD Internal server error."
                    try{
                        m_pResponseSender.SendResponseAsync(new IMAP_r_u_ServerStatus("BAD","Internal server error: " + x.Message));
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
                WriteLine("* BYE Idle timeout, closing connection.");
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
                // We are disposed already.
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
                                
                string[] cmd_args = Encoding.UTF8.GetString(op.Buffer,0,op.LineBytesInBuffer).Split(new char[]{' '},3);
                if(cmd_args.Length < 2){
                    m_pResponseSender.SendResponseAsync(new IMAP_r_u_ServerStatus("BAD","Error: Command '" + op.LineUtf8 + "' not recognized."));

                    return true;
                }
                string   cmdTag   = cmd_args[0];
                string   cmd      = cmd_args[1].ToUpperInvariant();
                string   args     = cmd_args.Length == 3 ? cmd_args[2] : "";
        
                // Log.
                if(this.Server.Logger != null){
                    // Hide password from log.
                    if(cmd == "LOGIN"){                        
                        this.Server.Logger.AddRead(this.ID,this.AuthenticatedUserIdentity,op.BytesInBuffer,op.LineUtf8.Substring(0,op.LineUtf8.LastIndexOf(' ')) + " <***REMOVED***>",this.LocalEndPoint,this.RemoteEndPoint);
                    }
                    else{
                        this.Server.Logger.AddRead(this.ID,this.AuthenticatedUserIdentity,op.BytesInBuffer,op.LineUtf8,this.LocalEndPoint,this.RemoteEndPoint);
                    }
                }

                if(cmd == "STARTTLS"){                    
                    STARTTLS(cmdTag,args);
                    readNextCommand = false;
                }
                else if(cmd == "LOGIN"){
                    LOGIN(cmdTag,args);
                }
                else if(cmd == "AUTHENTICATE"){
                    AUTHENTICATE(cmdTag,args);
                }
                else if(cmd == "NAMESPACE"){
                    NAMESPACE(cmdTag,args);
                }
                else if(cmd == "LIST"){
                    LIST(cmdTag,args);
                }
                else if(cmd == "CREATE"){
                    CREATE(cmdTag,args);
                }
                else if(cmd == "DELETE"){
                    DELETE(cmdTag,args);
                }
                else if(cmd == "RENAME"){
                    RENAME(cmdTag,args);
                }
                else if(cmd == "LSUB"){
                    LSUB(cmdTag,args);
                }
                else if(cmd == "SUBSCRIBE"){
                    SUBSCRIBE(cmdTag,args);
                }
                else if(cmd == "UNSUBSCRIBE"){
                    UNSUBSCRIBE(cmdTag,args);
                }
                else if(cmd == "STATUS"){
                    STATUS(cmdTag,args);
                }
                else if(cmd == "SELECT"){
                    SELECT(cmdTag,args);
                }
                else if(cmd == "EXAMINE"){
                    EXAMINE(cmdTag,args);
                }
                else if(cmd == "APPEND"){
                    APPEND(cmdTag,args);

                    return false;
                }
                else if(cmd == "GETQUOTAROOT"){
                    GETQUOTAROOT(cmdTag,args);
                }
                else if(cmd == "GETQUOTA"){
                    GETQUOTA(cmdTag,args);
                }
                else if(cmd == "GETACL"){
                    GETACL(cmdTag,args);
                }
                else if(cmd == "SETACL"){
                    SETACL(cmdTag,args);
                }
                else if(cmd == "DELETEACL"){
                    DELETEACL(cmdTag,args);
                }
                else if(cmd == "LISTRIGHTS"){
                    LISTRIGHTS(cmdTag,args);
                }
                else if(cmd == "MYRIGHTS"){
                    MYRIGHTS(cmdTag,args);
                }
                else if(cmd == "ENABLE"){
                    ENABLE(cmdTag,args);
                }
                else if(cmd == "CHECK"){
                    CHECK(cmdTag,args);
                }
                else if(cmd == "CLOSE"){
                    CLOSE(cmdTag,args);
                }
                else if(cmd == "FETCH"){
                    FETCH(false,cmdTag,args);
                }
                else if(cmd == "SEARCH"){
                    SEARCH(false,cmdTag,args);
                }
                else if(cmd == "STORE"){
                    STORE(false,cmdTag,args);
                }
                else if(cmd == "COPY"){
                    COPY(false,cmdTag,args);
                }
                else if(cmd == "UID"){
                    UID(cmdTag,args);
                }
                else if(cmd == "EXPUNGE"){
                    EXPUNGE(cmdTag,args);
                }
                else if(cmd == "IDLE"){
                    readNextCommand = IDLE(cmdTag,args);
                }
                else if(cmd == "CAPABILITY"){
                    CAPABILITY(cmdTag,args);
                }
                else if(cmd == "NOOP"){
                    NOOP(cmdTag,args);
                }
                else if(cmd == "LOGOUT"){
                    LOGOUT(cmdTag,args);
                    readNextCommand = false;
                }
                else{
                    m_BadCommands++;

                    // Maximum allowed bad commands exceeded.
                    if(this.Server.MaxBadCommands != 0 && m_BadCommands > this.Server.MaxBadCommands){
                        WriteLine("* BYE Too many bad commands, closing transmission channel.");
                        Disconnect();

                        return false;
                    }
                   
                    m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error: Command '" + cmd + "' not recognized."));
                }
             }
             catch(Exception x){
                 OnError(x);
             }

             return readNextCommand;
        }

        #endregion


        #region method STARTTLS

        private void STARTTLS(string cmdTag,string cmdText)
        {
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
                STARTTLS.  The server MAY advertise different capabilities after
                STARTTLS.

                Example:    C: a001 CAPABILITY
                            S: * CAPABILITY IMAP4rev1 STARTTLS LOGINDISABLED
                            S: a001 OK CAPABILITY completed
                            C: a002 STARTTLS
                            S: a002 OK Begin TLS negotiation now
                            <TLS negotiation, further commands are under [TLS] layer>
                            C: a003 CAPABILITY
                            S: * CAPABILITY IMAP4rev1 AUTH=PLAIN
                            S: a003 OK CAPABILITY completed
                            C: a004 LOGIN joe password
                            S: a004 OK LOGIN completed
            */

            if(m_SessionRejected){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Bad sequence of commands: Session rejected."));

                return;
            }
            if(this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","This ommand is only valid in not-authenticated state."));

                return;
            }
            if(this.IsSecureConnection){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Bad sequence of commands: Connection is already secure."));

                return;
            }
            if(this.Certificate == null){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","TLS not available: Server has no SSL certificate."));

                return;
            }

            m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","Begin TLS negotiation now."));
            
            try{
                DateTime startTime = DateTime.Now;

                // Create delegate which is called when SwitchToSecureAsync has completed.
                Action<SwitchToSecureAsyncOP> switchSecureCompleted = delegate(SwitchToSecureAsyncOP e){
                    try{
                        // Operation failed.
                        if(e.Error != null){
                            LogAddException(e.Error);
                            Disconnect();
                        }
                        // Operation suceeded.
                        else{
                            // Log
                            LogAddText("SSL negotiation completed successfully in " + (DateTime.Now - startTime).TotalSeconds.ToString("f2") + " seconds.");

                            BeginReadCmd();
                        }
                    }
                    catch(Exception x){
                        LogAddException(x);
                        Disconnect();
                    }
                };

                SwitchToSecureAsyncOP op = new SwitchToSecureAsyncOP();
                op.CompletedAsync += delegate(object sender,EventArgs<TCP_ServerSession.SwitchToSecureAsyncOP> e){
                    switchSecureCompleted(op);
                };
                // Switch to secure completed synchronously.
                if(!SwitchToSecureAsync(op)){
                    switchSecureCompleted(op);
                }
            }
            catch(Exception x){
                LogAddException(x);
                Disconnect();
            }
        }

        #endregion

        #region method LOGIN

        private void LOGIN(string cmdTag,string cmdText)
        {
            /* RFC 3501 6.2.3. LOGIN Command.
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

                Example:    C: a001 LOGIN SMITH SESAME
                            S: a001 OK LOGIN completed

                    Note: Use of the LOGIN command over an insecure network
                    (such as the Internet) is a security risk, because anyone
                    monitoring network traffic can obtain plaintext passwords.
                    The LOGIN command SHOULD NOT be used except as a last
                    resort, and it is recommended that client implementations
                    have a means to disable any automatic use of the LOGIN
                    command.

                Unless either the STARTTLS command has been negotiated or
                some other mechanism that protects the session from
                password snooping has been provided, a server
                implementation MUST implement a configuration in which it
                advertises the LOGINDISABLED capability and does NOT permit
                the LOGIN command.  Server sites SHOULD NOT use any
                configuration which permits the LOGIN command without such
                a protection mechanism against password snooping.  A client
                implementation MUST NOT send a LOGIN command if the
                LOGINDISABLED capability is advertised.
            */

            if(m_SessionRejected){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Bad sequence of commands: Session rejected."));

                return;
            }
            if(this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Re-authentication error."));

                return;
            }
            if(SupportsCap("LOGINDISABLED")){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Command 'LOGIN' is disabled, use AUTHENTICATE instead."));

                return;
            }
            if(string.IsNullOrEmpty(cmdText)){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }

            string[] user_pass = TextUtils.SplitQuotedString(cmdText,' ',true);
            if(user_pass.Length != 2){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }

            IMAP_e_Login e = OnLogin(user_pass[0],user_pass[1]);
            if(e.IsAuthenticated){
                m_pUser = new GenericIdentity(user_pass[0],"IMAP-LOGIN");

                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","LOGIN completed."));
            }
            else{
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","LOGIN failed."));
            }
        }

        #endregion

        #region method AUTHENTICATE

        private void AUTHENTICATE(string cmdTag,string cmdText)
        {
            /* RFC 3501 6.2.2. AUTHENTICATE Command.
                Arguments:  authentication mechanism name

                Responses:  continuation data can be requested

                Result:     OK - authenticate completed, now in authenticated state
                            NO - authenticate failure: unsupported authentication
                                 mechanism, credentials rejected
                            BAD - command unknown or arguments invalid,
                                  authentication exchange cancelled

                The AUTHENTICATE command indicates a [SASL] authentication
                mechanism to the server.  If the server supports the requested
                authentication mechanism, it performs an authentication protocol
                exchange to authenticate and identify the client.  It MAY also
                negotiate an OPTIONAL security layer for subsequent protocol
                interactions.  If the requested authentication mechanism is not
                supported, the server SHOULD reject the AUTHENTICATE command by
                sending a tagged NO response.

                The AUTHENTICATE command does not support the optional "initial
                response" feature of [SASL].  Section 5.1 of [SASL] specifies how
                to handle an authentication mechanism which uses an initial
                response.

                The service name specified by this protocol's profile of [SASL] is
                "imap".

                The authentication protocol exchange consists of a series of
                server challenges and client responses that are specific to the
                authentication mechanism.  A server challenge consists of a
                command continuation request response with the "+" token followed
                by a BASE64 encoded string.  The client response consists of a
                single line consisting of a BASE64 encoded string.  If the client
                wishes to cancel an authentication exchange, it issues a line
                consisting of a single "*".  If the server receives such a
                response, it MUST reject the AUTHENTICATE command by sending a
                tagged BAD response.

                If a security layer is negotiated through the [SASL]
                authentication exchange, it takes effect immediately following the
                CRLF that concludes the authentication exchange for the client,
                and the CRLF of the tagged OK response for the server.

                While client and server implementations MUST implement the
                AUTHENTICATE command itself, it is not required to implement any
                authentication mechanisms other than the PLAIN mechanism described
                in [IMAP-TLS].  Also, an authentication mechanism is not required
                to support any security layers.

                    Note: a server implementation MUST implement a
                    configuration in which it does NOT permit any plaintext
                    password mechanisms, unless either the STARTTLS command
                    has been negotiated or some other mechanism that
                    protects the session from password snooping has been
                    provided.  Server sites SHOULD NOT use any configuration
                    which permits a plaintext password mechanism without
                    such a protection mechanism against password snooping.
                    Client and server implementations SHOULD implement
                    additional [SASL] mechanisms that do not use plaintext
                    passwords, such the GSSAPI mechanism described in [SASL]
                    and/or the [DIGEST-MD5] mechanism.

                Servers and clients can support multiple authentication
                mechanisms.  The server SHOULD list its supported authentication
                mechanisms in the response to the CAPABILITY command so that the
                client knows which authentication mechanisms to use.

                A server MAY include a CAPABILITY response code in the tagged OK
                response of a successful AUTHENTICATE command in order to send
                capabilities automatically.  It is unnecessary for a client to
                send a separate CAPABILITY command if it recognizes these
                automatic capabilities.  This should only be done if a security
                layer was not negotiated by the AUTHENTICATE command, because the
                tagged OK response as part of an AUTHENTICATE command is not
                protected by encryption/integrity checking.  [SASL] requires the
                client to re-issue a CAPABILITY command in this case.
                            
                The authorization identity passed from the client to the server
                during the authentication exchange is interpreted by the server as
                the user name whose privileges the client is requesting.

                Example:    S: * OK IMAP4rev1 Server
                            C: A001 AUTHENTICATE GSSAPI
                            S: +
                            C: YIIB+wYJKoZIhvcSAQICAQBuggHqMIIB5qADAgEFoQMCAQ6iBw
                               MFACAAAACjggEmYYIBIjCCAR6gAwIBBaESGxB1Lndhc2hpbmd0
                               b24uZWR1oi0wK6ADAgEDoSQwIhsEaW1hcBsac2hpdmFtcy5jYW
                               Mud2FzaGluZ3Rvbi5lZHWjgdMwgdCgAwIBAaEDAgEDooHDBIHA
                               cS1GSa5b+fXnPZNmXB9SjL8Ollj2SKyb+3S0iXMljen/jNkpJX
                               AleKTz6BQPzj8duz8EtoOuNfKgweViyn/9B9bccy1uuAE2HI0y
                               C/PHXNNU9ZrBziJ8Lm0tTNc98kUpjXnHZhsMcz5Mx2GR6dGknb
                               I0iaGcRerMUsWOuBmKKKRmVMMdR9T3EZdpqsBd7jZCNMWotjhi
                               vd5zovQlFqQ2Wjc2+y46vKP/iXxWIuQJuDiisyXF0Y8+5GTpAL
                               pHDc1/pIGmMIGjoAMCAQGigZsEgZg2on5mSuxoDHEA1w9bcW9n
                               FdFxDKpdrQhVGVRDIzcCMCTzvUboqb5KjY1NJKJsfjRQiBYBdE
                               NKfzK+g5DlV8nrw81uOcP8NOQCLR5XkoMHC0Dr/80ziQzbNqhx
                               O6652Npft0LQwJvenwDI13YxpwOdMXzkWZN/XrEqOWp6GCgXTB
                               vCyLWLlWnbaUkZdEYbKHBPjd8t/1x5Yg==
                            S: + YGgGCSqGSIb3EgECAgIAb1kwV6ADAgEFoQMCAQ+iSzBJoAMC
                               AQGiQgRAtHTEuOP2BXb9sBYFR4SJlDZxmg39IxmRBOhXRKdDA0
                                uHTCOT9Bq3OsUTXUlk0CsFLoa8j+gvGDlgHuqzWHPSQg==
                            C:
                            S: + YDMGCSqGSIb3EgECAgIBAAD/////6jcyG4GE3KkTzBeBiVHe
                               ceP2CWY0SR0fAQAgAAQEBAQ=
                            C: YDMGCSqGSIb3EgECAgIBAAD/////3LQBHXTpFfZgrejpLlLImP
                               wkhbfa2QteAQAgAG1yYwE=
                            S: A001 OK GSSAPI authentication successful

                            Note: The line breaks within server challenges and client
                            responses are for editorial clarity and are not in real
                            authenticators.
            */

            /* RFC 4959.l7 SASL-IR 7.
                The following syntax specification uses the Augmented Backus-Naur
                Form [RFC4234] notation.  [RFC3501] defines the non-terminals
                capability, auth-type, and base64.

                capability    =/ "SASL-IR"

                authenticate  = "AUTHENTICATE" SP auth-type [SP (base64 / "=")]
                                *(CRLF base64)
                                ;;redefine AUTHENTICATE from [RFC3501]
            
            */

            if(m_SessionRejected){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Bad sequence of commands: Session rejected."));

                return;
            }
            if(this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Re-authentication error."));

                return;
            }

            #region Parse parameters

            string[] arguments = cmdText.Split(' ');
            if(arguments.Length > 2){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }
            byte[] initialClientResponse = new byte[0];
            if(arguments.Length == 2){
                if(arguments[1] == "="){
                    // Skip.
                }
                else{
                    try{
                        initialClientResponse = Convert.FromBase64String(arguments[1]);
                    }
                    catch{
                        m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Syntax error: Parameter 'initial-response' value must be BASE64 or contain a single character '='."));

                        return;
                    }
                }
            }
            string mechanism = arguments[0];

            #endregion
                        
            if(!this.Authentications.ContainsKey(mechanism)){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Not supported authentication mechanism."));

                return;
            }

            byte[] clientResponse = initialClientResponse;
            AUTH_SASL_ServerMechanism auth = this.Authentications[mechanism];            
            auth.Reset();
            while(true){
                byte[] serverResponse = auth.Continue(clientResponse);
                // Authentication completed.
                if(auth.IsCompleted){
                    if(auth.IsAuthenticated){
                        m_pUser = new GenericIdentity(auth.UserName,"SASL-" + auth.Name);
                              
                        m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","Authentication succeeded."));
                    }
                    else{
                        m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","Authentication credentials invalid."));
                    }
                    break;
                }
                // Authentication continues.
                else{
                    // Send server challange.
                    if(serverResponse.Length == 0){
                        m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus("+",""));
                    }
                    else{
                        m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus("+",Convert.ToBase64String(serverResponse)));
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
                        m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication canceled."));

                        return;
                    }
                    // We have base64 client response, decode it.
                    else{
                        try{
                            clientResponse = Convert.FromBase64String(readLineOP.LineUtf8);
                        }
                        catch{
                            m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Invalid client response '" + clientResponse + "'."));

                            return;
                        }
                    }
                }
            }
        }

        #endregion


        #region method NAMESPACE

        private void NAMESPACE(string cmdTag,string cmdText)
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

            if(!SupportsCap("NAMESPACE")){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Command not supported."));

                return;
            }
            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }

            IMAP_e_Namespace e = OnNamespace(new IMAP_r_ServerStatus(cmdTag,"OK","NAMESPACE command completed."));
            if(e.NamespaceResponse != null){
                m_pResponseSender.SendResponseAsync(e.NamespaceResponse);
            }
            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method LIST

        private void LIST(string cmdTag,string cmdText)
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

                The LIST command SHOULD return its data quickly, without undue
                delay.  For example, it SHOULD NOT go to excess trouble to
                calculate the \Marked or \Unmarked status or perform other
                processing; if each name requires 1 second of processing, then a
                list of 1200 names would take 20 minutes!

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

                Server implementations are permitted to "hide" otherwise
                accessible mailboxes from the wildcard characters, by preventing
                certain characters or names from matching a wildcard in certain
                situations.  For example, a UNIX-based server might restrict the
                interpretation of "*" so that an initial "/" character does not
                match.

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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }

            string[] parts = TextUtils.SplitQuotedString(cmdText,' ',true);
            if(parts.Length != 2){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }

            string refName = IMAP_Utils.DecodeMailbox(parts[0]);
            string folder  = IMAP_Utils.DecodeMailbox(parts[1]);

            // Store start time
			long startTime = DateTime.Now.Ticks;

            // Folder separator request.
            if(folder == string.Empty){
                m_pResponseSender.SendResponseAsync(new IMAP_r_u_List(m_FolderSeparator));
            }
            else{
                IMAP_e_List e = OnList(refName,folder);
                foreach(IMAP_r_u_List r in e.Folders){
                    m_pResponseSender.SendResponseAsync(r);
                }
            }

            m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","LIST Completed in " + ((DateTime.Now.Ticks - startTime) / (decimal)10000000).ToString("f2") + " seconds."));
        }

        #endregion

        #region method CREATE

        private void CREATE(string cmdTag,string cmdText)
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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }

            string folder = IMAP_Utils.DecodeMailbox(TextUtils.UnQuoteString(cmdText));
            
            IMAP_e_Folder e = OnCreate(cmdTag,folder,new IMAP_r_ServerStatus(cmdTag,"OK","CREATE command completed."));

            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method DELETE

        private void DELETE(string cmdTag,string cmdText)
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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }

            string folder = IMAP_Utils.DecodeMailbox(cmdText);
            
            IMAP_e_Folder e = OnDelete(cmdTag,folder,new IMAP_r_ServerStatus(cmdTag,"OK","DELETE command completed."));

            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method RENAME

        private void RENAME(string cmdTag,string cmdText)
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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            
            string[] parts = TextUtils.SplitQuotedString(cmdText,' ',true);
            if(parts.Length != 2){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }
            
            IMAP_e_Rename e = OnRename(cmdTag,IMAP_Utils.DecodeMailbox(parts[0]),IMAP_Utils.DecodeMailbox(parts[1]));
            if(e.Response == null){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Internal server error: IMAP Server application didn't return any resposne."));
            }
            else{
                m_pResponseSender.SendResponseAsync(e.Response);
            }
        }

        #endregion

        #region method LSUB

        private void LSUB(string cmdTag,string cmdText)
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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }

            string[] parts = TextUtils.SplitQuotedString(cmdText,' ',true);
            if(parts.Length != 2){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }

            string refName = IMAP_Utils.DecodeMailbox(parts[0]);
            string folder  = IMAP_Utils.DecodeMailbox(parts[1]);

            // Store start time
			long startTime = DateTime.Now.Ticks;
            
            IMAP_e_LSub e = OnLSub(refName,folder);
            foreach(IMAP_r_u_LSub r in e.Folders){
                m_pResponseSender.SendResponseAsync(r);
            }
            
            m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","LSUB Completed in " + ((DateTime.Now.Ticks - startTime) / (decimal)10000000).ToString("f2") + " seconds.")); 
        }

        #endregion

        #region method SUBSCRIBE

        private void SUBSCRIBE(string cmdTag,string cmdText)
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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }

            string folder = IMAP_Utils.DecodeMailbox(TextUtils.UnQuoteString(cmdText));
            
            IMAP_e_Folder e = OnSubscribe(cmdTag,folder,new IMAP_r_ServerStatus(cmdTag,"OK","SUBSCRIBE command completed."));
            
            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method UNSUBSCRIBE

        private void UNSUBSCRIBE(string cmdTag,string cmdText)
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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }

            string folder = IMAP_Utils.DecodeMailbox(TextUtils.UnQuoteString(cmdText));
            
            IMAP_e_Folder e = OnUnsubscribe(cmdTag,folder,new IMAP_r_ServerStatus(cmdTag,"OK","UNSUBSCRIBE command completed."));
            
            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method STATUS

        private void STATUS(string cmdTag,string cmdText)
        {
            /* RFC 3501 6.3.10.STATUS Command.
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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }

            string[] parts = TextUtils.SplitQuotedString(cmdText,' ',false,2);
            if(parts.Length != 2){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }

            // Store start time
			long startTime = DateTime.Now.Ticks;

            string folder = IMAP_Utils.DecodeMailbox(TextUtils.UnQuoteString(parts[0]));
            if(!(parts[1].StartsWith("(") && parts[1].EndsWith(")"))){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));
            }
            else{
                IMAP_e_Select eSelect = OnSelect(cmdTag,folder);
                if(eSelect.ErrorResponse != null){
                    m_pResponseSender.SendResponseAsync(eSelect.ErrorResponse);

                    return;
                }

                IMAP_e_MessagesInfo eMessagesInfo = OnGetMessagesInfo(folder);
                   
                int  msgCount    = -1;
                int  recentCount = -1;
                long uidNext     = -1;
                long folderUid   = -1;
                int  unseenCount = -1;

                string[] statusItems = parts[1].Substring(1,parts[1].Length - 2).Split(' ');
                for(int i=0;i<statusItems.Length;i++){
                    string statusItem = statusItems[i];
                    if(string.Equals(statusItem,"MESSAGES",StringComparison.InvariantCultureIgnoreCase)){
                        msgCount = eMessagesInfo.Exists;
                    }
                    else if(string.Equals(statusItem,"RECENT",StringComparison.InvariantCultureIgnoreCase)){
                        recentCount = eMessagesInfo.Recent;
                    }
                    else if(string.Equals(statusItem,"UIDNEXT",StringComparison.InvariantCultureIgnoreCase)){
                        uidNext = eMessagesInfo.UidNext;
                    }
                    else if(string.Equals(statusItem,"UIDVALIDITY",StringComparison.InvariantCultureIgnoreCase)){
                        folderUid = eSelect.FolderUID;
                    }
                    else if(string.Equals(statusItem,"UNSEEN",StringComparison.InvariantCultureIgnoreCase)){
                        unseenCount = eMessagesInfo.Unseen;
                    }
                    // Invalid status item.
                    else{
                        m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                        return;
                    }
                }
                                
                m_pResponseSender.SendResponseAsync(new IMAP_r_u_Status(folder,msgCount,recentCount,uidNext,folderUid,unseenCount));
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","STATUS completed in " + ((DateTime.Now.Ticks - startTime) / (decimal)10000000).ToString("f2") + " seconds."));
            }
        }

        #endregion

        #region method SELECT

        private void SELECT(string cmdTag,string cmdText)
        {
            /* RFC 3501 6.3.1. SELECT Command.
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

                If the client is not permitted to modify the mailbox but is
                permitted read access, the mailbox is selected as read-only, and
                the server MUST prefix the text of the tagged OK response to
                SELECT with the "[READ-ONLY]" response code.  Read-only access
                through SELECT differs from the EXAMINE command in that certain
                read-only mailboxes MAY permit the change of permanent state on a
                per-user (as opposed to global) basis.  Netnews messages marked in
                a server-based .newsrc file are an example of such per-user
                permanent state that can be modified with read-only mailboxes.

                Example:    C: A142 SELECT INBOX
                            S: * 172 EXISTS
                            S: * 1 RECENT
                            S: * OK [UNSEEN 12] Message 12 is first unseen
                            S: * OK [UIDVALIDITY 3857529045] UIDs valid
                            S: * OK [UIDNEXT 4392] Predicted next UID
                            S: * FLAGS (\Answered \Flagged \Deleted \Seen \Draft)
                            S: * OK [PERMANENTFLAGS (\Deleted \Seen \*)] Limited
                            S: A142 OK [READ-WRITE] SELECT completed
            */

            /* 5738 3.2.  UTF8 Parameter to SELECT and EXAMINE
                The "UTF8=ACCEPT" capability also indicates that the server supports
                the "UTF8" parameter to SELECT and EXAMINE.  When a mailbox is
                selected with the "UTF8" parameter, it alters the behavior of all
                IMAP commands related to message sizes, message headers, and MIME
                body headers so they refer to the message with UTF-8 headers.  If the
                mailstore is not UTF-8 header native and the SELECT or EXAMINE
                command with UTF-8 header modifier succeeds, then the server MUST
                return results as if the mailstore were UTF-8 header native with
                upconversion requirements as described in Section 8.  The server MAY
                reject the SELECT or EXAMINE command with the [NOT-UTF-8] response
                code, unless the "UTF8=ALL" or "UTF8=ONLY" capability is advertised.

                Servers MAY include mailboxes that can only be selected or examined
                if the "UTF8" parameter is provided.  However, such mailboxes MUST
                NOT be included in the output of an unextended LIST, LSUB, or
                equivalent command.  If a client attempts to SELECT or EXAMINE such
                mailboxes without the "UTF8" parameter, the server MUST reject the
                command with a [UTF-8-ONLY] response code.  As a result, such
                mailboxes will not be accessible by IMAP clients written prior to
                this specification and are discouraged unless the server advertises
                "UTF8=ONLY" or the server implements IMAP4 LIST Command Extensions
   
                    utf8-select-param = "UTF8"
                        ;; Conforms to <select-param> from RFC 4466

                    C: a SELECT newmailbox (UTF8)
                    S: ...
                    S: a OK SELECT completed
                    C: b FETCH 1 (SIZE ENVELOPE BODY)
                    S: ... < UTF-8 header native results >
                    S: b OK FETCH completed

                    C: c EXAMINE legacymailbox (UTF8)
                    S: c NO [NOT-UTF-8] Mailbox does not support UTF-8 access
            */

            // Store start time
			long startTime = DateTime.Now.Ticks;

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
                        
            // Unselect folder if any selected.
            if(m_pSelectedFolder != null){
                m_pSelectedFolder = null;
            }

            string[] args = TextUtils.SplitQuotedString(cmdText,' ');
            if(args.Length >= 2){
                // At moment we don't support UTF-8 mailboxes.
                if(string.Equals(args[1],"(UTF8)",StringComparison.InvariantCultureIgnoreCase)){
                    m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO",new IMAP_t_orc_Unknown("NOT-UTF-8"),"Mailbox does not support UTF-8 access."));
                }
                else{
                    m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));
                }

                return;
            }

            try{
                string folder = TextUtils.UnQuoteString(IMAP_Utils.DecodeMailbox(cmdText));

                IMAP_e_Select e = OnSelect(cmdTag,folder);
                if(e.ErrorResponse == null){
                    IMAP_e_MessagesInfo eMessagesInfo = OnGetMessagesInfo(folder);

                    m_pResponseSender.SendResponseAsync(new IMAP_r_u_Exists(eMessagesInfo.Exists));
                    m_pResponseSender.SendResponseAsync(new IMAP_r_u_Recent(eMessagesInfo.Recent));
                    if(eMessagesInfo.FirstUnseen > -1){
                        m_pResponseSender.SendResponseAsync(new IMAP_r_u_ServerStatus("OK",new IMAP_t_orc_Unseen(eMessagesInfo.FirstUnseen),"Message " + eMessagesInfo.FirstUnseen + " is the first unseen."));
                    }
                    m_pResponseSender.SendResponseAsync(new IMAP_r_u_ServerStatus("OK",new IMAP_t_orc_UidNext((int)eMessagesInfo.UidNext),"Predicted next message UID."));
                    m_pResponseSender.SendResponseAsync(new IMAP_r_u_ServerStatus("OK",new IMAP_t_orc_UidValidity(e.FolderUID),"Folder UID value."));
                    m_pResponseSender.SendResponseAsync(new IMAP_r_u_Flags(e.Flags.ToArray()));
                    m_pResponseSender.SendResponseAsync(new IMAP_r_u_ServerStatus("OK",new IMAP_t_orc_PermanentFlags(e.PermanentFlags.ToArray()),"Avaliable permanent flags."));
                    m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK",new IMAP_t_orc_Unknown(e.IsReadOnly ? "READ-ONLY" : "READ-WRITE"),"SELECT completed in " + ((DateTime.Now.Ticks - startTime) / (decimal)10000000).ToString("f2") + " seconds."));

                    m_pSelectedFolder = new _SelectedFolder(folder,e.IsReadOnly,eMessagesInfo.MessagesInfo);
                    m_pSelectedFolder.Reindex();
                }
                else{
                    m_pResponseSender.SendResponseAsync(e.ErrorResponse);
                }
            }
            catch(Exception x){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","NO Error: " + x.Message));
            }
        }

        #endregion

        #region method EXAMINE

        private void EXAMINE(string cmdTag,string cmdText)
        {
            /* RFC 3501 6.3.2. EXAMINE Command.
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

                Example:    C: A932 EXAMINE blurdybloop
                            S: * 17 EXISTS
                            S: * 2 RECENT
                            S: * OK [UNSEEN 8] Message 8 is first unseen
                            S: * OK [UIDVALIDITY 3857529045] UIDs valid
                            S: * OK [UIDNEXT 4392] Predicted next UID
                            S: * FLAGS (\Answered \Flagged \Deleted \Seen \Draft)
                            S: * OK [PERMANENTFLAGS ()] No permanent flags permitted
                            S: A932 OK [READ-ONLY] EXAMINE completed
            */

            /* 5738 3.2.  UTF8 Parameter to SELECT and EXAMINE
                The "UTF8=ACCEPT" capability also indicates that the server supports
                the "UTF8" parameter to SELECT and EXAMINE.  When a mailbox is
                selected with the "UTF8" parameter, it alters the behavior of all
                IMAP commands related to message sizes, message headers, and MIME
                body headers so they refer to the message with UTF-8 headers.  If the
                mailstore is not UTF-8 header native and the SELECT or EXAMINE
                command with UTF-8 header modifier succeeds, then the server MUST
                return results as if the mailstore were UTF-8 header native with
                upconversion requirements as described in Section 8.  The server MAY
                reject the SELECT or EXAMINE command with the [NOT-UTF-8] response
                code, unless the "UTF8=ALL" or "UTF8=ONLY" capability is advertised.

                Servers MAY include mailboxes that can only be selected or examined
                if the "UTF8" parameter is provided.  However, such mailboxes MUST
                NOT be included in the output of an unextended LIST, LSUB, or
                equivalent command.  If a client attempts to SELECT or EXAMINE such
                mailboxes without the "UTF8" parameter, the server MUST reject the
                command with a [UTF-8-ONLY] response code.  As a result, such
                mailboxes will not be accessible by IMAP clients written prior to
                this specification and are discouraged unless the server advertises
                "UTF8=ONLY" or the server implements IMAP4 LIST Command Extensions
   
                    utf8-select-param = "UTF8"
                        ;; Conforms to <select-param> from RFC 4466

                    C: a SELECT newmailbox (UTF8)
                    S: ...
                    S: a OK SELECT completed
                    C: b FETCH 1 (SIZE ENVELOPE BODY)
                    S: ... < UTF-8 header native results >
                    S: b OK FETCH completed

                    C: c EXAMINE legacymailbox (UTF8)
                    S: c NO [NOT-UTF-8] Mailbox does not support UTF-8 access
            */

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            
            // Store start time
			long startTime = DateTime.Now.Ticks;

            // Unselect folder if any selected.
            if(m_pSelectedFolder != null){
                m_pSelectedFolder = null;
            }

            string[] args = TextUtils.SplitQuotedString(cmdText,' ');
            if(args.Length >= 2){
                // At moment we don't support UTF-8 mailboxes.
                if(string.Equals(args[1],"(UTF8)",StringComparison.InvariantCultureIgnoreCase)){
                    m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO",new IMAP_t_orc_Unknown("NOT-UTF-8"),"Mailbox does not support UTF-8 access."));
                }
                else{
                    m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));
                }

                return;
            }

            string folder = TextUtils.UnQuoteString(IMAP_Utils.DecodeMailbox(cmdText));

            IMAP_e_Select e = OnSelect(cmdTag,folder);
            if(e.ErrorResponse == null){                 
                IMAP_e_MessagesInfo eMessagesInfo = OnGetMessagesInfo(folder);

                m_pResponseSender.SendResponseAsync(new IMAP_r_u_Exists(eMessagesInfo.Exists));
                m_pResponseSender.SendResponseAsync(new IMAP_r_u_Recent(eMessagesInfo.Recent));
                if(eMessagesInfo.FirstUnseen > -1){
                    m_pResponseSender.SendResponseAsync(new IMAP_r_u_ServerStatus("OK",new IMAP_t_orc_Unseen(eMessagesInfo.FirstUnseen),"Message " + eMessagesInfo.FirstUnseen + " is the first unseen."));
                }
                m_pResponseSender.SendResponseAsync(new IMAP_r_u_ServerStatus("OK",new IMAP_t_orc_UidNext((int)eMessagesInfo.UidNext),"Predicted next message UID."));
                m_pResponseSender.SendResponseAsync(new IMAP_r_u_ServerStatus("OK",new IMAP_t_orc_UidValidity(e.FolderUID),"Folder UID value."));
                m_pResponseSender.SendResponseAsync(new IMAP_r_u_Flags(e.Flags.ToArray()));
                m_pResponseSender.SendResponseAsync(new IMAP_r_u_ServerStatus("OK",new IMAP_t_orc_PermanentFlags(e.PermanentFlags.ToArray()),"Avaliable permanent flags."));
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK",new IMAP_t_orc_ReadOnly(),"EXAMINE completed in " + ((DateTime.Now.Ticks - startTime) / (decimal)10000000).ToString("f2") + " seconds."));

                m_pSelectedFolder = new _SelectedFolder(folder,e.IsReadOnly,eMessagesInfo.MessagesInfo);
                m_pSelectedFolder.Reindex();
            }
            else{
                m_pResponseSender.SendResponseAsync(e.ErrorResponse);
            }
        }

        #endregion

        #region method APPEND

        private void APPEND(string cmdTag,string cmdText)
        {
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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }

            // Store start time
			long startTime = DateTime.Now.Ticks;

            #region Parse arguments

            StringReader r = new StringReader(cmdText);
            r.ReadToFirstChar();
            string folder = null;
            if(r.StartsWith("\"")){
                folder = IMAP_Utils.DecodeMailbox(r.ReadWord());
            }
            else{
                folder = IMAP_Utils.DecodeMailbox(r.QuotedReadToDelimiter(' '));
            }
            r.ReadToFirstChar();
            List<string> flags = new List<string>();
            if(r.StartsWith("(")){                
                foreach(string f in r.ReadParenthesized().Split(' ')){
                    if(f.Length > 0 && !flags.Contains(f.Substring(1))){
                        flags.Add(f.Substring(1));
                    }
                }
            }
            r.ReadToFirstChar();
            DateTime date = DateTime.MinValue;
            if(!r.StartsWith("{")){
                date = IMAP_Utils.ParseDate(r.ReadWord());
            }
            int size = Convert.ToInt32(r.ReadParenthesized());
            if(r.Available > 0){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }

            #endregion

            IMAP_e_Append e = OnAppend(folder,flags.ToArray(),date,size,new IMAP_r_ServerStatus(cmdTag,"OK","APPEND command completed in %exectime seconds."));
            
            if(e.Response.IsError){
                m_pResponseSender.SendResponseAsync(e.Response);
            }
            else if(e.Stream == null){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Internal server error: No storage stream available."));
            }
            else{
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus("+","Ready for literal data."));
                
                // Create callback which is called when BeginReadFixedCount completes.
                AsyncCallback readLiteralCompletedCallback = delegate(IAsyncResult ar){
                    try{
                        this.TcpStream.EndReadFixedCount(ar);
                        // Log.
                        LogAddRead(size,"Readed " + size + " bytes.");

                        // TODO: Async
                        // Read command line terminating CRLF.
                        SmartStream.ReadLineAsyncOP readLineOP = new SmartStream.ReadLineAsyncOP(new byte[32000],SizeExceededAction.JunkAndThrowException);
                        this.TcpStream.ReadLine(readLineOP,false);
                        // Read command line terminating CRLF failed.
                        if(readLineOP.Error != null){
                            OnError(readLineOP.Error);
                        }
                        // Read command line terminating CRLF succeeded.
                        else{
                            LogAddRead(readLineOP.BytesInBuffer,readLineOP.LineUtf8);

                            // Raise Completed event.
                            e.OnCompleted();

                            m_pResponseSender.SendResponseAsync(IMAP_r_ServerStatus.Parse(e.Response.ToString().TrimEnd().Replace("%exectime",((DateTime.Now.Ticks - startTime) / (decimal)10000000).ToString("f2"))));
                            BeginReadCmd();
                        }
                    }
                    catch(Exception x){
                        OnError(x);
                    }
                };

                this.TcpStream.BeginReadFixedCount(e.Stream,size,readLiteralCompletedCallback,null);
            }
        }

        #endregion

        #region method GETQUOTAROOT

        private void GETQUOTAROOT(string cmdTag,string cmdText)
        {
            /* RFC 2087 4.3. GETQUOTAROOT
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

            if(!SupportsCap("QUOTA")){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Command not supported."));

                return;
            }
            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            if(string.IsNullOrEmpty(cmdText)){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }
            
            string folder = IMAP_Utils.DecodeMailbox(TextUtils.UnQuoteString(cmdText));

            IMAP_e_GetQuotaRoot e = OnGetGuotaRoot(folder,new IMAP_r_ServerStatus(cmdTag,"OK","GETQUOTAROOT command completed."));

            if(e.QuotaRootResponses.Count > 0){
                foreach(IMAP_r_u_QuotaRoot r in e.QuotaRootResponses){
                    m_pResponseSender.SendResponseAsync(r);
                }                
            }
            if(e.QuotaResponses.Count > 0){
                foreach(IMAP_r_u_Quota r in e.QuotaResponses){
                    m_pResponseSender.SendResponseAsync(r);
                }                
            }
            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method GETQUOTA

        private void GETQUOTA(string cmdTag,string cmdText)
        {
            /* RFC 2087 4.2. GETQUOTA
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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }            

            string quotaRoot = IMAP_Utils.DecodeMailbox(TextUtils.UnQuoteString(cmdText));

            IMAP_e_GetQuota e = OnGetQuota(quotaRoot,new IMAP_r_ServerStatus(cmdTag,"OK","QUOTA command completed."));
            if(e.QuotaResponses.Count > 0){
                foreach(IMAP_r_u_Quota r in e.QuotaResponses){
                    m_pResponseSender.SendResponseAsync(r);
                }                
            }
            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method GETACL

        private void GETACL(string cmdTag,string cmdText)
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
							S: * ACL INBOX Fred rwipslda
							S: A002 OK Getacl complete
							
							.... Multiple users
							S: * ACL INBOX Fred rwipslda test rwipslda
							
							.... No acl flags for Fred
							S: * ACL INBOX Fred "" test rwipslda         
            */

            if(!SupportsCap("ACL")){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Command not supported."));

                return;
            }
            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }            

            string folder = IMAP_Utils.DecodeMailbox(TextUtils.UnQuoteString(cmdText));

            IMAP_e_GetAcl e = OnGetAcl(folder,new IMAP_r_ServerStatus(cmdTag,"OK","GETACL command completed."));
            if(e.AclResponses.Count > 0){
                foreach(IMAP_r_u_Acl r in e.AclResponses){
                    m_pResponseSender.SendResponseAsync(r);
                }                
            }
            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method SETACL

        private void SETACL(string cmdTag,string cmdText)
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

            if(!SupportsCap("ACL")){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Command not supported."));

                return;
            }
            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }            

            string[] parts = TextUtils.SplitQuotedString(cmdText,' ',true);
            if(parts.Length != 3){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }

            string rights = parts[2];
            IMAP_Flags_SetType setType = IMAP_Flags_SetType.Replace;
            if(rights.StartsWith("+")){
                setType = IMAP_Flags_SetType.Add;
                rights = rights.Substring(1);
            }
            else if(rights.StartsWith("-")){
                setType = IMAP_Flags_SetType.Remove;
                rights = rights.Substring(1);
            }

            IMAP_e_SetAcl e = OnSetAcl(
                IMAP_Utils.DecodeMailbox(parts[0]),
                IMAP_Utils.DecodeMailbox(parts[1]),
                setType,
                rights,
                new IMAP_r_ServerStatus(cmdTag,"OK","SETACL command completed.")
            );
            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method DELETEACL

        private void DELETEACL(string cmdTag,string cmdText)
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

            if(!SupportsCap("ACL")){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Command not supported."));

                return;
            }
            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            
            string[] parts = TextUtils.SplitQuotedString(cmdText,' ',true);
            if(parts.Length != 2){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }

            IMAP_e_DeleteAcl e = OnDeleteAcl(
                IMAP_Utils.DecodeMailbox(parts[0]),
                IMAP_Utils.DecodeMailbox(parts[1]),
                new IMAP_r_ServerStatus(cmdTag,"OK","DELETEACL command completed.")
            );
            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method LISTRIGHTS

        private void LISTRIGHTS(string cmdTag,string cmdText)
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

            if(!SupportsCap("ACL")){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Command not supported."));

                return;
            }
            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }

            string[] parts = TextUtils.SplitQuotedString(cmdText,' ',true);
            if(parts.Length != 2){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }
            

            IMAP_e_ListRights e = OnListRights(
                IMAP_Utils.DecodeMailbox(parts[0]),
                IMAP_Utils.DecodeMailbox(parts[1]),
                new IMAP_r_ServerStatus(cmdTag,"OK","LISTRIGHTS command completed.")
            );

            
            if(e.ListRightsResponse != null){
                m_pResponseSender.SendResponseAsync(e.ListRightsResponse);
            }
            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method MYRIGHTS

        private void MYRIGHTS(string cmdTag,string cmdText)
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

            if(!SupportsCap("ACL")){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Command not supported."));

                return;
            }
            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            
            string folder = IMAP_Utils.DecodeMailbox(TextUtils.UnQuoteString(cmdText));

            IMAP_e_MyRights e = OnMyRights(folder,new IMAP_r_ServerStatus(cmdTag,"OK","MYRIGHTS command completed."));
            if(e.MyRightsResponse != null){
                m_pResponseSender.SendResponseAsync(e.MyRightsResponse);
            }
            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method ENABLE

        private void ENABLE(string cmdTag,string cmdText)
        {            
            /* RFC 5161 3.1. The ENABLE Command.
                Arguments: capability names

                Result: OK: Relevant capabilities enabled
                        BAD: No arguments, or syntax error in an argument

                The ENABLE command takes a list of capability names, and requests the
                server to enable the named extensions.  Once enabled using ENABLE,
                each extension remains active until the IMAP connection is closed.
                For each argument, the server does the following:

                    - If the argument is not an extension known to the server, the server
                      MUST ignore the argument.

                    - If the argument is an extension known to the server, and it is not
                      specifically permitted to be enabled using ENABLE, the server MUST
                      ignore the argument.  (Note that knowing about an extension doesn't
                      necessarily imply supporting that extension.)

                    - If the argument is an extension that is supported by the server and
                      that needs to be enabled, the server MUST enable the extension for
                      the duration of the connection.  At present, this applies only to
                      CONDSTORE ([RFC4551]).  Note that once an extension is enabled,
                      there is no way to disable it.

                If the ENABLE command is successful, the server MUST send an untagged
                ENABLED response (see Section 3.2).

                Clients SHOULD only include extensions that need to be enabled by the
                server.  At the time of publication, CONDSTORE is the only such
                extension (i.e., ENABLE CONDSTORE is an additional "CONDSTORE
                enabling command" as defined in [RFC4551]).  Future RFCs may add to
                this list.

                The ENABLE command is only valid in the authenticated state (see
                [RFC3501]), before any mailbox is selected.  Clients MUST NOT issue
                ENABLE once they SELECT/EXAMINE a mailbox; however, server
                implementations don't have to check that no mailbox is selected or
                was previously selected during the duration of a connection.

                The ENABLE command can be issued multiple times in a session.  It is
                additive; i.e., "ENABLE a b", followed by "ENABLE c" is the same as a
                single command "ENABLE a b c".  When multiple ENABLE commands are
                issued, each corresponding ENABLED response SHOULD only contain
                extensions enabled by the corresponding ENABLE command.

                There are no limitations on pipelining ENABLE.  For example, it is
                possible to send ENABLE and then immediately SELECT, or a LOGIN
                immediately followed by ENABLE.

                The server MUST NOT change the CAPABILITY list as a result of
                executing ENABLE; i.e., a CAPABILITY command issued right after an
                ENABLE command MUST list the same capabilities as a CAPABILITY
                command issued before the ENABLE command.  This is demonstrated in
                the following example:
             
                    C: t1 CAPABILITY
                    S: * CAPABILITY IMAP4rev1 ID LITERAL+ ENABLE X-GOOD-IDEA
                    S: t1 OK foo
                    C: t2 ENABLE CONDSTORE X-GOOD-IDEA
                    S: * ENABLED X-GOOD-IDEA
                    S: t2 OK foo
                    C: t3 CAPABILITY
                    S: * CAPABILITY IMAP4rev1 ID LITERAL+ ENABLE X-GOOD-IDEA
                    S: t3 OK foo again

                In the following example, the client enables CONDSTORE:

                    C: a1 ENABLE CONDSTORE
                    S: * ENABLED CONDSTORE
                    S: a1 OK Conditional Store enabled
            */

            // Capability disabled.
            if(!SupportsCap("ENABLE")){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Command 'ENABLE' not supported."));

                return;
            }
            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            if(string.IsNullOrEmpty(cmdText)){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","No arguments, or syntax error in an argument."));

                return;
            }

            foreach(string capa in cmdText.Split(' ')){
                if(string.Equals("UTF8=ACCEPT",capa,StringComparison.InvariantCultureIgnoreCase)){
                    m_MailboxEncoding = IMAP_Mailbox_Encoding.ImapUtf8;
                    m_pResponseSender.SendResponseAsync(new IMAP_r_u_Enable(new string[]{"UTF8=ACCEPT"}));
                }
                // Ignore as specification says.
                //else{
                //}
            }
            m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","ENABLE command completed."));
        }

        #endregion


        #region method CHECK

        private void CHECK(string cmdTag,string cmdText)
        {
            /* 6.4.1.  CHECK Command
                Arguments:  none

                Responses:  no specific responses for this command

                Result:     OK - check completed
                            BAD - command unknown or arguments invalid

                The CHECK command requests a checkpoint of the currently selected
                mailbox.  A checkpoint refers to any implementation-dependent
                housekeeping associated with the mailbox (e.g., resolving the
                server's in-memory state of the mailbox with the state on its
                disk) that is not normally executed as part of each command.  A
                checkpoint MAY take a non-instantaneous amount of real time to
                complete.  If a server implementation has no such housekeeping
                considerations, CHECK is equivalent to NOOP.

                There is no guarantee that an EXISTS untagged response will happen
                as a result of CHECK.  NOOP, not CHECK, SHOULD be used for new
                message polling.

                Example:    C: FXXZ CHECK
                            S: FXXZ OK CHECK Completed
            */

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            if(m_pSelectedFolder == null){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Error: This command is valid only in selected state."));

                return;
            }

            // Store start time
			long startTime = DateTime.Now.Ticks;

           
            UpdateSelectedFolderAndSendChanges();
            
            m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","CHECK Completed in " + ((DateTime.Now.Ticks - startTime) / (decimal)10000000).ToString("f2") + " seconds."));
        }

        #endregion

        #region method CLOSE

        private void CLOSE(string cmdTag,string cmdText)
        {
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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            if(m_pSelectedFolder == null){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Error: This command is valid only in selected state."));

                return;
            }

            if(m_pSelectedFolder != null && !m_pSelectedFolder.IsReadOnly){
                foreach(IMAP_MessageInfo msgInfo in m_pSelectedFolder.MessagesInfo){
                    if(msgInfo.ContainsFlag("Deleted")){
                        OnExpunge(msgInfo,new IMAP_r_ServerStatus("dummy","OK","This is CLOSE command expunge, so this response is not used."));
                    }
                }
            }
            m_pSelectedFolder = null;
            
            m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","CLOSE completed."));
        }

        #endregion
//
        #region method FETCH

        private void FETCH(bool uid,string cmdTag,string cmdText)
        {
            /* RFC 3501. 6.4.5. FETCH Command.
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

                There are three macros which specify commonly-used sets of data
                items, and can be used instead of data items.  A macro must be
                used by itself, and not in conjunction with other macros or data
                items.

                ALL
                    Macro equivalent to: (FLAGS INTERNALDATE RFC822.SIZE ENVELOPE)

                FAST
                    Macro equivalent to: (FLAGS INTERNALDATE RFC822.SIZE)

                FULL
                    Macro equivalent to: (FLAGS INTERNALDATE RFC822.SIZE ENVELOPE BODY)

                The currently defined data items that can be fetched are:

                BODY
                    Non-extensible form of BODYSTRUCTURE.

                BODY[<section>]<<partial>>
                    The text of a particular body section.  The section
                    specification is a set of zero or more part specifiers
                    delimited by periods.  A part specifier is either a part number
                    or one of the following: HEADER, HEADER.FIELDS,
                    HEADER.FIELDS.NOT, MIME, and TEXT.  An empty section
                    specification refers to the entire message, including the
                    header.

                    Every message has at least one part number.  Non-[MIME-IMB]
                    messages, and non-multipart [MIME-IMB] messages with no
                    encapsulated message, only have a part 1.

                    Multipart messages are assigned consecutive part numbers, as
                    they occur in the message.  If a particular part is of type
                    message or multipart, its parts MUST be indicated by a period
                    followed by the part number within that nested multipart part.

                    A part of type MESSAGE/RFC822 also has nested part numbers,
                    referring to parts of the MESSAGE part's body.

                    The HEADER, HEADER.FIELDS, HEADER.FIELDS.NOT, and TEXT part
                    specifiers can be the sole part specifier or can be prefixed by
                    one or more numeric part specifiers, provided that the numeric
                    part specifier refers to a part of type MESSAGE/RFC822.  The
                    MIME part specifier MUST be prefixed by one or more numeric
                    part specifiers.

                    The HEADER, HEADER.FIELDS, and HEADER.FIELDS.NOT part
                    specifiers refer to the [RFC-2822] header of the message or of
                    an encapsulated [MIME-IMT] MESSAGE/RFC822 message.
                    HEADER.FIELDS and HEADER.FIELDS.NOT are followed by a list of
                    field-name (as defined in [RFC-2822]) names, and return a
                    subset of the header.  The subset returned by HEADER.FIELDS
                    contains only those header fields with a field-name that
                    matches one of the names in the list; similarly, the subset
                    returned by HEADER.FIELDS.NOT contains only the header fields
                    with a non-matching field-name.  The field-matching is
                    case-insensitive but otherwise exact.  Subsetting does not
                    exclude the [RFC-2822] delimiting blank line between the header
                    and the body; the blank line is included in all header fetches,
                    except in the case of a message which has no body and no blank
                    line.

                    The MIME part specifier refers to the [MIME-IMB] header for
                    this part.

                    The TEXT part specifier refers to the text body of the message,
                    omitting the [RFC-2822] header.

                        Here is an example of a complex message with some of its
                        part specifiers:

                    HEADER     ([RFC-2822] header of the message)
                    TEXT       ([RFC-2822] text body of the message) MULTIPART/MIXED
                    1          TEXT/PLAIN
                    2          APPLICATION/OCTET-STREAM
                    3          MESSAGE/RFC822
                    3.HEADER   ([RFC-2822] header of the message)
                    3.TEXT     ([RFC-2822] text body of the message) MULTIPART/MIXED
                    3.1        TEXT/PLAIN
                    3.2        APPLICATION/OCTET-STREAM
                    4          MULTIPART/MIXED
                    4.1        IMAGE/GIF
                    4.1.MIME   ([MIME-IMB] header for the IMAGE/GIF)
                    4.2        MESSAGE/RFC822
                    4.2.HEADER ([RFC-2822] header of the message)
                    4.2.TEXT   ([RFC-2822] text body of the message) MULTIPART/MIXED
                    4.2.1      TEXT/PLAIN
                    4.2.2      MULTIPART/ALTERNATIVE
                    4.2.2.1    TEXT/PLAIN
                    4.2.2.2    TEXT/RICHTEXT
            
                    It is possible to fetch a substring of the designated text.
                    This is done by appending an open angle bracket ("<"), the
                    octet position of the first desired octet, a period, the
                    maximum number of octets desired, and a close angle bracket
                    (">") to the part specifier.  If the starting octet is beyond
                    the end of the text, an empty string is returned.
                    Any partial fetch that attempts to read beyond the end of the
                    text is truncated as appropriate.  A partial fetch that starts
                    at octet 0 is returned as a partial fetch, even if this
                    truncation happened.

                        Note: This means that BODY[]<0.2048> of a 1500-octet message
                        will return BODY[]<0> with a literal of size 1500, not
                        BODY[].

                        Note: A substring fetch of a HEADER.FIELDS or
                        HEADER.FIELDS.NOT part specifier is calculated after
                        subsetting the header.

                    The \Seen flag is implicitly set; if this causes the flags to
                    change, they SHOULD be included as part of the FETCH responses.

                BODY.PEEK[<section>]<<partial>>
                    An alternate form of BODY[<section>] that does not implicitly
                    set the \Seen flag.

                BODYSTRUCTURE
                    The [MIME-IMB] body structure of the message.  This is computed
                    by the server by parsing the [MIME-IMB] header fields in the
                    [RFC-2822] header and [MIME-IMB] headers.

                ENVELOPE
                    The envelope structure of the message.  This is computed by the
                    server by parsing the [RFC-2822] header into the component
                    parts, defaulting various fields as necessary.

                FLAGS
                    The flags that are set for this message.

                INTERNALDATE
                    The internal date of the message.

                RFC822
                    Functionally equivalent to BODY[], differing in the syntax of
                    the resulting untagged FETCH data (RFC822 is returned).

                RFC822.HEADER
                    Functionally equivalent to BODY.PEEK[HEADER], differing in the
                    syntax of the resulting untagged FETCH data (RFC822.HEADER is
                    returned).

                RFC822.SIZE
                    The [RFC-2822] size of the message.

                RFC822.TEXT
                    Functionally equivalent to BODY[TEXT], differing in the syntax
                    of the resulting untagged FETCH data (RFC822.TEXT is returned).
                UID
                    The unique identifier for the message.


                Example:    C: A654 FETCH 2:4 (FLAGS BODY[HEADER.FIELDS (DATE FROM)])
                            S: * 2 FETCH ....
                            S: * 3 FETCH ....
                            S: * 4 FETCH ....
                            S: A654 OK FETCH completed
            */

            // Store start time
			long startTime = DateTime.Now.Ticks;

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            if(m_pSelectedFolder == null){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Error: This command is valid only in selected state."));

                return;
            }
            
            string[] parts = cmdText.Split(new char[]{' '},2);
            if(parts.Length != 2){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }

            IMAP_t_SeqSet seqSet = null;
            try{                
                seqSet = IMAP_t_SeqSet.Parse(parts[0]);
            }
            catch{
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments: Invalid 'sequence-set' value."));

                return;
            }

            #region Parse data-items

            List<IMAP_t_Fetch_i> dataItems     = new List<IMAP_t_Fetch_i>();
            bool                 msgDataNeeded = false;

            // Remove parenthesizes.
            string dataItemsString = parts[1].Trim();
            if(dataItemsString.StartsWith("(") && dataItemsString.EndsWith(")")){
                dataItemsString = dataItemsString.Substring(1,dataItemsString.Length - 2).Trim();
            }

            // Replace macros.
            dataItemsString = dataItemsString.Replace("ALL","FLAGS INTERNALDATE RFC822.SIZE ENVELOPE");
            dataItemsString = dataItemsString.Replace("FAST","FLAGS INTERNALDATE RFC822.SIZE"); 
            dataItemsString = dataItemsString.Replace("FULL","FLAGS INTERNALDATE RFC822.SIZE ENVELOPE BODY");

            StringReader r = new StringReader(dataItemsString);

            IMAP_Fetch_DataType fetchDataType = IMAP_Fetch_DataType.FullMessage;

            // Parse data-items.
            while(r.Available > 0){
                r.ReadToFirstChar();

                #region BODYSTRUCTURE

                if(r.StartsWith("BODYSTRUCTURE",false)){
                    r.ReadWord();
                    dataItems.Add(new IMAP_t_Fetch_i_BodyStructure());
                    msgDataNeeded = true;
                    fetchDataType = IMAP_Fetch_DataType.MessageStructure;
                }

                #endregion

                #region BODY[<section>]<<partial>> and BODY.PEEK[<section>]<<partial>>

                else if(r.StartsWith("BODY[",false) || r.StartsWith("BODY.PEEK[",false)){
                    bool peek = r.StartsWith("BODY.PEEK[",false);
                    r.ReadWord();

                    #region Parse <section>

                    string section = r.ReadParenthesized();
                                                                            
                    // Full message wanted.
                    if(string.IsNullOrEmpty(section)){
                    }
                    else{
                        // Left-side part-items must be numbers, only last one may be (HEADER,HEADER.FIELDS,HEADER.FIELDS.NOT,MIME,TEXT).
                    
                        StringReader rSection = new StringReader(section);
                        string remainingSection = rSection.ReadWord();
                        while(remainingSection.Length > 0){
                            string[] section_parts = remainingSection.Split(new char[]{'.'},2);
                            // Not part number.
                            if(!Net_Utils.IsInteger(section_parts[0])){
                                // We must have one of the following values here (HEADER,HEADER.FIELDS,HEADER.FIELDS.NOT,MIME,TEXT).
                                if(remainingSection.Equals("HEADER",StringComparison.InvariantCultureIgnoreCase)){                        
                                }
                                else if(remainingSection.Equals("HEADER.FIELDS",StringComparison.InvariantCultureIgnoreCase)){
                                    rSection.ReadToFirstChar();
                                    if(!rSection.StartsWith("(")){
                                        WriteLine(cmdTag + " BAD Error in arguments.");

                                        return;
                                    }
                                    rSection.ReadParenthesized();
                                }
                                else if(remainingSection.Equals("HEADER.FIELDS.NOT",StringComparison.InvariantCultureIgnoreCase)){
                                    rSection.ReadToFirstChar();
                                    if(!rSection.StartsWith("(")){
                                        WriteLine(cmdTag + " BAD Error in arguments.");

                                        return;
                                    }
                                    rSection.ReadParenthesized();
                                }
                                else if(remainingSection.Equals("MIME",StringComparison.InvariantCultureIgnoreCase)){
                                }
                                else if(remainingSection.Equals("TEXT",StringComparison.InvariantCultureIgnoreCase)){
                                }
                                // Unknown parts specifier.
                                else{
                                    WriteLine(cmdTag + " BAD Error in arguments.");

                                    return;
                                }

                                break;
                            }

                            if(section_parts.Length == 2){
                                remainingSection = section_parts[1];
                            }
                            else{
                                remainingSection = "";
                            }
                        }
                    }

                    #endregion

                    #region Parse <partial>

                    int offset   = -1;
                    int maxCount = -1;
                    // Partial data wanted.
                    if(r.StartsWith("<")){
                        string[] origin = r.ReadParenthesized().Split('.');
                        if(origin.Length > 2){
                            WriteLine(cmdTag + " BAD Error in arguments.");

                            return;
                        }

                        if(!int.TryParse(origin[0],out offset)){
                            WriteLine(cmdTag + " BAD Error in arguments.");

                            return;
                        }
                        if(origin.Length == 2){
                            if(!int.TryParse(origin[1],out maxCount)){
                                WriteLine(cmdTag + " BAD Error in arguments.");

                                return;
                            }
                        }
                    }

                    #endregion

                    if(peek){
                        dataItems.Add(new IMAP_t_Fetch_i_BodyPeek(section,offset,maxCount));
                    }
                    else{
                        dataItems.Add(new IMAP_t_Fetch_i_Body(section,offset,maxCount));
                    }
                    msgDataNeeded = true;
                    fetchDataType = IMAP_Fetch_DataType.FullMessage;
                }

                #endregion
                
                #region BODY

                else if(r.StartsWith("BODY",false)){
                    r.ReadWord();
                    dataItems.Add(new IMAP_t_Fetch_i_BodyS());
                    msgDataNeeded = true;
                    fetchDataType = IMAP_Fetch_DataType.MessageStructure;
                }

                #endregion

                #region ENVELOPE

                else if(r.StartsWith("ENVELOPE",false)){
                    r.ReadWord();
                    dataItems.Add(new IMAP_t_Fetch_i_Envelope());
                    msgDataNeeded = true;
                    fetchDataType = IMAP_Fetch_DataType.MessageHeader;
                }

                #endregion
                
                #region FLAGS

                else if(r.StartsWith("FLAGS",false)){
                    r.ReadWord();
                    dataItems.Add(new IMAP_t_Fetch_i_Flags());
                }

                #endregion

                #region INTERNALDATE

                else if(r.StartsWith("INTERNALDATE",false)){
                    r.ReadWord();
                    dataItems.Add(new IMAP_t_Fetch_i_InternalDate());
                }

                #endregion

                #region RFC822.HEADER

                else if(r.StartsWith("RFC822.HEADER",false)){
                    r.ReadWord();
                    dataItems.Add(new IMAP_t_Fetch_i_Rfc822Header());
                    msgDataNeeded = true;
                    fetchDataType = IMAP_Fetch_DataType.MessageHeader;
                }

                #endregion

                #region RFC822.SIZE

                else if(r.StartsWith("RFC822.SIZE",false)){
                    r.ReadWord();
                    dataItems.Add(new IMAP_t_Fetch_i_Rfc822Size());
                }

                #endregion

                #region RFC822.TEXT

                else if(r.StartsWith("RFC822.TEXT",false)){
                    r.ReadWord();
                    dataItems.Add(new IMAP_t_Fetch_i_Rfc822Text());
                    msgDataNeeded = true;
                    fetchDataType = IMAP_Fetch_DataType.FullMessage;
                }

                #endregion

                #region RFC822

                else if(r.StartsWith("RFC822",false)){
                    r.ReadWord();
                    dataItems.Add(new IMAP_t_Fetch_i_Rfc822());
                    msgDataNeeded = true;
                    fetchDataType = IMAP_Fetch_DataType.FullMessage;
                }

                #endregion

                #region UID

                else if(r.StartsWith("UID",false)){
                    r.ReadWord();
                    dataItems.Add(new IMAP_t_Fetch_i_Uid());
                }

                #endregion

                #region Unknown data-item.

                else{
                    WriteLine(cmdTag + " BAD Error in arguments: Unknown FETCH data-item.");

                    return;
                }

                #endregion
            }

            #endregion

            // UID FETCH must always return UID data-item, even if user didn't request it.
            if(uid){
                bool add = true;
                foreach(IMAP_t_Fetch_i item in dataItems){                    
                    if(item is IMAP_t_Fetch_i_Uid){
                        add = false;
                        break;
                    }
                }
                if(add){
                    dataItems.Add(new IMAP_t_Fetch_i_Uid());
                }
            }

            UpdateSelectedFolderAndSendChanges();

            IMAP_e_Fetch fetchEArgs = new IMAP_e_Fetch(
                m_pSelectedFolder.Filter(uid,seqSet),
                fetchDataType,
                new IMAP_r_ServerStatus(cmdTag,"OK","FETCH command completed in %exectime seconds.")
            );
            fetchEArgs.NewMessageData += new EventHandler<IMAP_e_Fetch.e_NewMessageData>(delegate(object s,IMAP_e_Fetch.e_NewMessageData e){
                /*
                // Build response data-items.
                List<IMAP_t_Fetch_r_i> responseItems = new List<IMAP_t_Fetch_r_i>();
                foreach(IMAP_t_Fetch_i dataItem in dataItems){
                    if(dataItem is IMAP_t_Fetch_i_BodyS){
                        //responseItems.Add(new IMAP_t_Fetch_r_i_Uid(e.MessageInfo.UID));
                    }
                    else if(dataItem is IMAP_t_Fetch_i_Body){
                        //responseItems.Add(new IMAP_t_Fetch_r_i_Uid(e.MessageInfo.UID));
                    }
                    else if(dataItem is IMAP_t_Fetch_i_BodyStructure){
                        //responseItems.Add(new IMAP_t_Fetch_r_i_Uid(e.MessageInfo.UID));
                    }
                    else if(dataItem is IMAP_t_Fetch_i_Envelope){
                        //responseItems.Add(new IMAP_t_Fetch_r_i_Uid(e.MessageInfo.UID));
                    }
                    else if(dataItem is IMAP_t_Fetch_i_Flags){
                        responseItems.Add(new IMAP_t_Fetch_r_i_Flags(new IMAP_t_MsgFlags(e.MessageInfo.Flags)));
                    }
                    else if(dataItem is IMAP_t_Fetch_i_InternalDate){
                        responseItems.Add(new IMAP_t_Fetch_r_i_InternalDate(e.MessageInfo.InternalDate));
                    }
                    else if(dataItem is IMAP_t_Fetch_i_Rfc822){
                        //responseItems.Add(new IMAP_t_Fetch_r_i_Uid(e.MessageInfo.UID));
                    }
                    else if(dataItem is IMAP_t_Fetch_i_Rfc822Header){
                        //responseItems.Add(new IMAP_t_Fetch_r_i_Uid(e.MessageInfo.UID));
                    }
                    else if(dataItem is IMAP_t_Fetch_i_Rfc822Size){
                        responseItems.Add(new IMAP_t_Fetch_r_i_Rfc822Size(e.MessageInfo.Size));
                    }
                    else if(dataItem is IMAP_t_Fetch_i_Rfc822Text){
                        //responseItems.Add(new IMAP_t_Fetch_r_i_Uid(e.MessageInfo.UID));
                    }
                    else if(dataItem is IMAP_t_Fetch_i_Uid){
                        responseItems.Add(new IMAP_t_Fetch_r_i_Uid(e.MessageInfo.UID));
                    }
                }*/

                StringBuilder reponseBuffer = new StringBuilder();
                reponseBuffer.Append("* " + e.MessageInfo.SeqNo + " FETCH (");

                Mail_Message message = e.MessageData;

                // Return requested data-items for the returned message.
                for(int i=0;i<dataItems.Count;i++){
                    IMAP_t_Fetch_i dataItem = dataItems[i];
                                      
                    // Add data-items separator.
                    if(i > 0){
                        reponseBuffer.Append(" ");
                    }
                                       
                    #region BODY

                    if(dataItem is IMAP_t_Fetch_i_BodyS){
                        reponseBuffer.Append(ConstructBodyStructure(message,false));
                    }

                    #endregion

                    #region BODY[<section>]<<partial>> and BODY.PEEK[<section>]<<partial>>

                    else if(dataItem is IMAP_t_Fetch_i_Body || dataItem is IMAP_t_Fetch_i_BodyPeek){
                        string section  = "";
                        int    offset   = -1;
                        int    maxCount = -1;
                        if(dataItem is IMAP_t_Fetch_i_Body){
                            section  = ((IMAP_t_Fetch_i_Body)dataItem).Section;
                            offset   = ((IMAP_t_Fetch_i_Body)dataItem).Offset;
                            maxCount = ((IMAP_t_Fetch_i_Body)dataItem).MaxCount;
                        }
                        else{
                            section  = ((IMAP_t_Fetch_i_BodyPeek)dataItem).Section;
                            offset   = ((IMAP_t_Fetch_i_BodyPeek)dataItem).Offset;
                            maxCount = ((IMAP_t_Fetch_i_BodyPeek)dataItem).MaxCount;
                        }

                        using(MemoryStreamEx tmpFs = new MemoryStreamEx(32000)){
                            // Empty section, full message wanted.
                            if(string.IsNullOrEmpty(section)){
                                message.ToStream(tmpFs,new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8),Encoding.UTF8);
                                tmpFs.Position = 0;
                            }
                            // Message data part wanted.
                            else{
                                // Get specified MIME part.
                                MIME_Entity entity = GetMimeEntity(message,ParsePartNumberFromSection(section));
                                if(entity != null){
                                    string partSpecifier = ParsePartSpecifierFromSection(section);

                                    #region HEADER

                                    if(string.Equals(partSpecifier,"HEADER",StringComparison.InvariantCultureIgnoreCase)){                                        
                                        entity.Header.ToStream(tmpFs,new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8),Encoding.UTF8);
                                        // All header fetches must include header terminator(CRLF).
                                        if(tmpFs.Length >0 ){
                                            tmpFs.WriteByte((byte)'\r');
                                            tmpFs.WriteByte((byte)'\n');
                                        }
                                        tmpFs.Position = 0;
                                    }

                                    #endregion

                                    #region HEADER.FIELDS

                                    else if(string.Equals(partSpecifier,"HEADER.FIELDS",StringComparison.InvariantCultureIgnoreCase)){                            
                                        string   fieldsString = section.Split(new char[]{' '},2)[1];
                                        string[] fieldNames   = fieldsString.Substring(1,fieldsString.Length - 2).Split(' ');
                                        foreach(string filedName in fieldNames){
                                            MIME_h[] fields = entity.Header[filedName];
                                            if(fields != null){
                                                foreach(MIME_h field in fields){
                                                    byte[] fieldBytes = Encoding.UTF8.GetBytes(field.ToString(new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8),Encoding.UTF8));
                                                    tmpFs.Write(fieldBytes,0,fieldBytes.Length);
                                                }
                                            }
                                        }
                                        // All header fetches must include header terminator(CRLF).
                                        if(tmpFs.Length > 0){
                                            tmpFs.WriteByte((byte)'\r');
                                            tmpFs.WriteByte((byte)'\n');
                                        }
                                        tmpFs.Position = 0;
                                    }

                                    #endregion

                                    #region HEADER.FIELDS.NOT

                                    else if(string.Equals(partSpecifier,"HEADER.FIELDS.NOT",StringComparison.InvariantCultureIgnoreCase)){
                                        string   fieldsString = section.Split(new char[]{' '},2)[1];
                                        string[] fieldNames   = fieldsString.Substring(1,fieldsString.Length - 2).Split(' ');
                                        foreach(MIME_h field in entity.Header){
                                            bool contains = false;
                                            foreach(string fieldName in fieldNames){
                                                if(string.Equals(field.Name,fieldName,StringComparison.InvariantCultureIgnoreCase)){
                                                    contains = true;
                                                    break;
                                                }
                                            }

                                            if(!contains){
                                                byte[] fieldBytes = Encoding.UTF8.GetBytes(field.ToString(new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8),Encoding.UTF8));
                                                tmpFs.Write(fieldBytes,0,fieldBytes.Length);
                                            }
                                        }
                                        // All header fetches must include header terminator(CRLF).
                                        if(tmpFs.Length >0 ){
                                            tmpFs.WriteByte((byte)'\r');
                                            tmpFs.WriteByte((byte)'\n');
                                        }
                                        tmpFs.Position = 0;
                                    }

                                    #endregion

                                    #region MIME

                                    else if(string.Equals(partSpecifier,"MIME",StringComparison.InvariantCultureIgnoreCase)){
                                        entity.Header.ToStream(tmpFs,new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8),Encoding.UTF8);
                                        // All header fetches must include header terminator(CRLF).
                                        if(tmpFs.Length >0 ){
                                            tmpFs.WriteByte((byte)'\r');
                                            tmpFs.WriteByte((byte)'\n');
                                        }
                                        tmpFs.Position = 0;
                                    }

                                    #endregion

                                    #region TEXT

                                    else if(string.Equals(partSpecifier,"TEXT",StringComparison.InvariantCultureIgnoreCase)){
                                        entity.Body.ToStream(tmpFs,new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8),Encoding.UTF8,false);
                                        tmpFs.Position = 0;
                                    }

                                    #endregion

                                    #region part-number only

                                    else{
                                        entity.Body.ToStream(tmpFs,new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8),Encoding.UTF8,false);
                                        tmpFs.Position = 0;
                                    }

                                    #endregion
                                }
                            }

                            #region Send data

                            // All data wanted.
                            if(offset < 0){
                                reponseBuffer.Append("BODY[" + section + "] {" + tmpFs.Length + "}\r\n");
                                WriteLine(reponseBuffer.ToString());
                                reponseBuffer = new StringBuilder();

                                this.TcpStream.WriteStream(tmpFs);
                                LogAddWrite(tmpFs.Length,"Wrote " + tmpFs.Length + " bytes.");
                            }
                            // Partial data wanted.
                            else{                                    
                                // Offet out of range.
                                if(offset >= tmpFs.Length){
                                    reponseBuffer.Append("BODY[" + section + "]<" + offset + "> \"\"");
                                }
                                else{
                                    tmpFs.Position = offset;
                                        
                                    int count = maxCount > -1 ? (int)Math.Min(maxCount,tmpFs.Length - tmpFs.Position) : (int)(tmpFs.Length - tmpFs.Position);
                                    reponseBuffer.Append("BODY[" + section + "]<" + offset + "> {" + count + "}");
                                    WriteLine(reponseBuffer.ToString());
                                    reponseBuffer = new StringBuilder();

                                    this.TcpStream.WriteStream(tmpFs,count);
                                    LogAddWrite(tmpFs.Length,"Wrote " + count + " bytes.");
                                }
                            }

                            #endregion
                        }

                        // Set Seen flag.
                        if(!m_pSelectedFolder.IsReadOnly && dataItem is IMAP_t_Fetch_i_Body){
                            try{
                                OnStore(e.MessageInfo,IMAP_Flags_SetType.Add,new string[]{"Seen"},new IMAP_r_ServerStatus("dummy","OK","This is FETCH set Seen flag, this response not used."));
                            }
                            catch{
                            }
                        }
                    }

                    #endregion

                    #region BODYSTRUCTURE

                    else if(dataItem is IMAP_t_Fetch_i_BodyStructure){
                        reponseBuffer.Append(ConstructBodyStructure(message,true));
                    }

                    #endregion

                    #region ENVELOPE

                    else if(dataItem is IMAP_t_Fetch_i_Envelope){
                        reponseBuffer.Append(IMAP_t_Fetch_r_i_Envelope.ConstructEnvelope(message));
                    }

                    #endregion

                    #region FLAGS

                    else if(dataItem is IMAP_t_Fetch_i_Flags){
                        reponseBuffer.Append("FLAGS " + e.MessageInfo.FlagsToImapString());
                    }

                    #endregion

                    #region INTERNALDATE

                    else if(dataItem is IMAP_t_Fetch_i_InternalDate){
                        reponseBuffer.Append("INTERNALDATE \"" + IMAP_Utils.DateTimeToString(e.MessageInfo.InternalDate) + "\"");
                    }

                    #endregion

                    #region RFC822

                    else if(dataItem is IMAP_t_Fetch_i_Rfc822){
                        using(MemoryStreamEx tmpFs = new MemoryStreamEx(32000)){
                            message.ToStream(tmpFs,new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8),Encoding.UTF8);
                            tmpFs.Position = 0;

                            reponseBuffer.Append("RFC822 {" + tmpFs.Length + "}\r\n");
                            WriteLine(reponseBuffer.ToString());
                            reponseBuffer = new StringBuilder();

                            this.TcpStream.WriteStream(tmpFs);
                            LogAddWrite(tmpFs.Length,"Wrote " + tmpFs.Length + " bytes.");
                        }
                    }

                    #endregion

                    #region RFC822.HEADER

                    else if(dataItem is IMAP_t_Fetch_i_Rfc822Header){
                        MemoryStream ms = new MemoryStream();
                        message.Header.ToStream(ms,new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8),Encoding.UTF8);
                        ms.Position = 0;

                        reponseBuffer.Append("RFC822.HEADER {" + ms.Length + "}\r\n");
                        WriteLine(reponseBuffer.ToString());
                        reponseBuffer = new StringBuilder();

                        this.TcpStream.WriteStream(ms);
                        LogAddWrite(ms.Length,"Wrote " + ms.Length + " bytes.");
                    }

                    #endregion

                    #region RFC822.SIZE

                    else if(dataItem is IMAP_t_Fetch_i_Rfc822Size){
                        reponseBuffer.Append("RFC822.SIZE " + e.MessageInfo.Size);
                    }

                    #endregion

                    #region RFC822.TEXT

                    else if(dataItem is IMAP_t_Fetch_i_Rfc822Text){
                        using(MemoryStreamEx tmpFs = new MemoryStreamEx(32000)){
                            message.Body.ToStream(tmpFs,new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8),Encoding.UTF8,false);
                            tmpFs.Position = 0;

                            reponseBuffer.Append("RFC822.TEXT {" + tmpFs.Length + "}\r\n");
                            WriteLine(reponseBuffer.ToString());
                            reponseBuffer = new StringBuilder();

                            this.TcpStream.WriteStream(tmpFs);
                            LogAddWrite(tmpFs.Length,"Wrote " + tmpFs.Length + " bytes.");
                        }
                    }

                    #endregion

                    #region UID

                    else if(dataItem is IMAP_t_Fetch_i_Uid){ 
                        reponseBuffer.Append("UID " + e.MessageInfo.UID);
                    }

                    #endregion
                }

                reponseBuffer.Append(")\r\n");            
                WriteLine(reponseBuffer.ToString());
            });
                        
            // We have all needed data in message info.
            if(!msgDataNeeded){
                foreach(IMAP_MessageInfo msgInfo in m_pSelectedFolder.Filter(uid,seqSet)){
                    fetchEArgs.AddData(msgInfo);
                }
            }
            // Request messages data.
            else{
                OnFetch(fetchEArgs);
            }
                                    
            WriteLine(fetchEArgs.Response.ToString().Replace("%exectime",((DateTime.Now.Ticks - startTime) / (decimal)10000000).ToString("f2")));
        }

        #endregion

        #region method SEARCH

        private void SEARCH(bool uid,string cmdTag,string cmdText)
        {
            /* RFC 3501 6.4.4. SEARCH Command.
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

                Server implementations MAY exclude [MIME-IMB] body parts with
                terminal content media types other than TEXT and MESSAGE from
                consideration in SEARCH matching.

                The OPTIONAL [CHARSET] specification consists of the word
                "CHARSET" followed by a registered [CHARSET].  It indicates the
                [CHARSET] of the strings that appear in the search criteria.
                [MIME-IMB] content transfer encodings, and [MIME-HDRS] strings in
                [RFC-2822]/[MIME-IMB] headers, MUST be decoded before comparing
                text in a [CHARSET] other than US-ASCII.  US-ASCII MUST be
                supported; other [CHARSET]s MAY be supported.

                If the server does not support the specified [CHARSET], it MUST
                return a tagged NO response (not a BAD).  This response SHOULD
                contain the BADCHARSET response code, which MAY list the
                [CHARSET]s supported by the server.

                In all search keys that use strings, a message matches the key if
                the string is a substring of the field.  The matching is
                case-insensitive.

                The defined search keys are as follows.  Refer to the Formal
                Syntax section for the precise syntactic definitions of the
                arguments.

                <sequence set>
                    Messages with message sequence numbers corresponding to the
                    specified message sequence number set.

                ALL
                    All messages in the mailbox; the default initial key for ANDing.

                ANSWERED
                    Messages with the \Answered flag set.

                BCC <string>
                    Messages that contain the specified string in the envelope
                    structure's BCC field.

                BEFORE <date>
                    Messages whose internal date (disregarding time and timezone)
                    is earlier than the specified date.

                BODY <string>
                    Messages that contain the specified string in the body of the
                    message.

                CC <string>
                    Messages that contain the specified string in the envelope
                    structure's CC field.

                DELETED
                    Messages with the \Deleted flag set.

                DRAFT
                    Messages with the \Draft flag set.

                FLAGGED
                    Messages with the \Flagged flag set.

                FROM <string>
                    Messages that contain the specified string in the envelope
                    structure's FROM field.

                HEADER <field-name> <string>
                    Messages that have a header with the specified field-name (as
                    defined in [RFC-2822]) and that contains the specified string
                    in the text of the header (what comes after the colon).  If the
                    string to search is zero-length, this matches all messages that
                    have a header line with the specified field-name regardless of
                    the contents.

                KEYWORD <flag>
                    Messages with the specified keyword flag set.

                LARGER <n>
                    Messages with an [RFC-2822] size larger than the specified
                    number of octets.

                NEW
                    Messages that have the \Recent flag set but not the \Seen flag.
                    This is functionally equivalent to "(RECENT UNSEEN)".

                NOT <search-key>
                    Messages that do not match the specified search key.

                OLD
                    Messages that do not have the \Recent flag set.  This is
                    functionally equivalent to "NOT RECENT" (as opposed to "NOT NEW").

                ON <date>
                    Messages whose internal date (disregarding time and timezone)
                    is within the specified date.

                OR <search-key1> <search-key2>
                    Messages that match either search key.

                RECENT
                    Messages that have the \Recent flag set.

                SEEN
                    Messages that have the \Seen flag set.

                SENTBEFORE <date>
                    Messages whose [RFC-2822] Date: header (disregarding time and
                    timezone) is earlier than the specified date.

                SENTON <date>
                    Messages whose [RFC-2822] Date: header (disregarding time and
                    timezone) is within the specified date.

                SENTSINCE <date>
                    Messages whose [RFC-2822] Date: header (disregarding time and
                    timezone) is within or later than the specified date.

                SINCE <date>
                    Messages whose internal date (disregarding time and timezone)
                    is within or later than the specified date.

                SMALLER <n>
                    Messages with an [RFC-2822] size smaller than the specified
                    number of octets.

                SUBJECT <string>
                    Messages that contain the specified string in the envelope
                    structure's SUBJECT field.

                TEXT <string>
                    Messages that contain the specified string in the header or
                    body of the message.

                TO <string>
                    Messages that contain the specified string in the envelope
                    structure's TO field.

                UID <sequence set>
                    Messages with unique identifiers corresponding to the specified
                    unique identifier set.  Sequence set ranges are permitted.

                UNANSWERED
                    Messages that do not have the \Answered flag set.

                UNDELETED
                    Messages that do not have the \Deleted flag set.

                UNDRAFT
                    Messages that do not have the \Draft flag set.

                UNFLAGGED
                    Messages that do not have the \Flagged flag set.

                UNKEYWORD <flag>
                    Messages that do not have the specified keyword flag set.

                UNSEEN
                    Messages that do not have the \Seen flag set.

                Example:    C: A282 SEARCH FLAGGED SINCE 1-Feb-1994 NOT FROM "Smith"
                            S: * SEARCH 2 84 882
                            S: A282 OK SEARCH completed
                            C: A283 SEARCH TEXT "string not in mailbox"
                            S: * SEARCH
                            S: A283 OK SEARCH completed
                            C: A284 SEARCH CHARSET UTF-8 TEXT {6}
                            C: XXXXXX
                            S: * SEARCH 43
                            S: A284 OK SEARCH completed
            */

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            if(m_pSelectedFolder == null){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Error: This command is valid only in selected state."));

                return;
            }

            // Store start time
			long startTime = DateTime.Now.Ticks;

            #region Parse arguments

            _CmdReader cmdReader = new _CmdReader(this,cmdText,Encoding.UTF8);            
            cmdReader.Start();

            StringReader r = new StringReader(cmdReader.CmdLine);
            
            // See if we have optional CHARSET argument.
            if(r.StartsWith("CHARSET",false)){
                r.ReadWord();

                string charset = r.ReadWord();
                if(!(string.Equals(charset,"US-ASCII",StringComparison.InvariantCultureIgnoreCase) || string.Equals(charset,"UTF-8",StringComparison.InvariantCultureIgnoreCase))){
                    m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO",new IMAP_t_orc_BadCharset(new string[]{"US-ASCII","UTF-8"}),"Not supported charset."));

                    return;
                }
            }

            #endregion

            try{
                IMAP_Search_Key_Group criteria = IMAP_Search_Key_Group.Parse(r);

                UpdateSelectedFolderAndSendChanges();
                
                List<int> matchedValues = new List<int>();

                IMAP_e_Search searchArgs = new IMAP_e_Search(criteria,new IMAP_r_ServerStatus(cmdTag,"OK","SEARCH completed in %exectime seconds."));
                searchArgs.Matched += new EventHandler<EventArgs<long>>(delegate(object s,EventArgs<long> e){
                    if(uid){
                        matchedValues.Add((int)e.Value);
                    }
                    else{
                        // Search sequence-number for that message.
                        int seqNo = m_pSelectedFolder.GetSeqNo(e.Value);
                        if(seqNo != -1){
                            matchedValues.Add(seqNo);
                        }
                    }                    
                });
                OnSearch(searchArgs);

                m_pResponseSender.SendResponseAsync(new IMAP_r_u_Search(matchedValues.ToArray()));
                m_pResponseSender.SendResponseAsync(IMAP_r_ServerStatus.Parse(searchArgs.Response.ToString().TrimEnd().Replace("%exectime",((DateTime.Now.Ticks - startTime) / (decimal)10000000).ToString("f2"))));
            }
            catch{
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));
            }            
        }

        #endregion

        #region method STORE

        private void STORE(bool uid,string cmdTag,string cmdText)
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

            // Store start time
			long startTime = DateTime.Now.Ticks;

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            if(m_pSelectedFolder == null){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Error: This command is valid only in selected state."));

                return;
            }

            #region Parse arguments

            string[] parts = cmdText.Split(new char[]{' '},3);
            if(parts.Length != 3){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }

            IMAP_t_SeqSet seqSet = null;
            try{                
                seqSet = IMAP_t_SeqSet.Parse(parts[0]);
            }
            catch{
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }
         
            IMAP_Flags_SetType setType;
            bool               silent = false;
            if(string.Equals(parts[1],"FLAGS",StringComparison.InvariantCultureIgnoreCase)){
                setType = IMAP_Flags_SetType.Replace;
            }
            else if(string.Equals(parts[1],"FLAGS.SILENT",StringComparison.InvariantCultureIgnoreCase)){
                setType = IMAP_Flags_SetType.Replace;
                silent = true;
            }
            else if(string.Equals(parts[1],"+FLAGS",StringComparison.InvariantCultureIgnoreCase)){
                setType = IMAP_Flags_SetType.Add;
            }
            else if(string.Equals(parts[1],"+FLAGS.SILENT",StringComparison.InvariantCultureIgnoreCase)){
                setType = IMAP_Flags_SetType.Add;
                silent = true;
            }
            else if(string.Equals(parts[1],"-FLAGS",StringComparison.InvariantCultureIgnoreCase)){
                setType = IMAP_Flags_SetType.Remove;
            }
            else if(string.Equals(parts[1],"-FLAGS.SILENT",StringComparison.InvariantCultureIgnoreCase)){
                setType = IMAP_Flags_SetType.Remove;
                silent = true;
            }
            else{
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }                       

            if(!(parts[2].StartsWith("(") && parts[2].EndsWith(")"))){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }
            List<string> flags = new List<string>();
            foreach(string f in parts[2].Substring(1,parts[2].Length - 2).Split(' ')){
                if(f.Length > 0 && !flags.Contains(f.Substring(1))){
                    flags.Add(f.Substring(1));
                }
            }

            #endregion

            IMAP_r_ServerStatus response = new IMAP_r_ServerStatus(cmdTag,"OK","STORE command completed in %exectime seconds.");
            foreach(IMAP_MessageInfo msgInfo in m_pSelectedFolder.Filter(uid,seqSet)){
                IMAP_e_Store e = OnStore(msgInfo,setType,flags.ToArray(),response);
                response = e.Response;
                if(!string.Equals(e.Response.ResponseCode,"OK",StringComparison.InvariantCultureIgnoreCase)){
                    break;
                }

                // Update local message info flags value.
                msgInfo.UpdateFlags(setType,flags.ToArray());

                if(!silent){
                    // UID STORE must include UID. For more info see RFC 3501 6.4.8. UID Command.
                    if(uid){
                        m_pResponseSender.SendResponseAsync(
                            new IMAP_r_u_Fetch(
                                m_pSelectedFolder.GetSeqNo(msgInfo),
                                new IMAP_t_Fetch_r_i[]{
                                    new IMAP_t_Fetch_r_i_Flags(IMAP_t_MsgFlags.Parse(msgInfo.FlagsToImapString())),
                                    new IMAP_t_Fetch_r_i_Uid(msgInfo.UID)
                                }
                            )
                        );
                    }
                    else{
                        m_pResponseSender.SendResponseAsync(
                            new IMAP_r_u_Fetch(
                                m_pSelectedFolder.GetSeqNo(msgInfo),
                                new IMAP_t_Fetch_r_i[]{
                                    new IMAP_t_Fetch_r_i_Flags(new IMAP_t_MsgFlags(msgInfo.Flags))
                                }
                            )
                        );
                    }
                }
            }

            m_pResponseSender.SendResponseAsync(IMAP_r_ServerStatus.Parse(response.ToString().TrimEnd().Replace("%exectime",((DateTime.Now.Ticks - startTime) / (decimal)10000000).ToString("f2"))));
        }

        #endregion

        #region method COPY

        private void COPY(bool uid,string cmdTag,string cmdText)
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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            if(m_pSelectedFolder == null){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Error: This command is valid only in selected state."));

                return;
            }

            string[] parts = cmdText.Split(new char[]{' '},2);
            if(parts.Length != 2){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }
            IMAP_t_SeqSet seqSet = null;
            try{                
                seqSet = IMAP_t_SeqSet.Parse(parts[0]);
            }
            catch{
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));

                return;
            }
            string targetFolder = IMAP_Utils.DecodeMailbox(TextUtils.UnQuoteString(parts[1]));

            UpdateSelectedFolderAndSendChanges();

            IMAP_e_Copy e = OnCopy(
                targetFolder,
                m_pSelectedFolder.Filter(uid,seqSet),
                new IMAP_r_ServerStatus(cmdTag,"OK","COPY completed.")
            );
            m_pResponseSender.SendResponseAsync(e.Response);
        }

        #endregion

        #region method UID

        private void UID(string cmdTag,string cmdText)
        {
            /* RFC 3501 6.4.8. UID Command.
                Arguments:  command name
                            command arguments

                Responses:  untagged responses: FETCH, SEARCH

                Result:     OK - UID command completed
                            NO - UID command error
                            BAD - command unknown or arguments invalid

                The UID command has two forms.  In the first form, it takes as its
                arguments a COPY, FETCH, or STORE command with arguments
                appropriate for the associated command.  However, the numbers in
                the sequence set argument are unique identifiers instead of
                message sequence numbers.  Sequence set ranges are permitted, but
                there is no guarantee that unique identifiers will be contiguous.

                A non-existent unique identifier is ignored without any error
                message generated.  Thus, it is possible for a UID FETCH command
                to return an OK without any data or a UID COPY or UID STORE to
                return an OK without performing any operations.

                In the second form, the UID command takes a SEARCH command with
                SEARCH command arguments.  The interpretation of the arguments is
                the same as with SEARCH; however, the numbers returned in a SEARCH
                response for a UID SEARCH command are unique identifiers instead
                of message sequence numbers.  For example, the command UID SEARCH
                1:100 UID 443:557 returns the unique identifiers corresponding to
                the intersection of two sequence sets, the message sequence number
                range 1:100 and the UID range 443:557.

                    Note: in the above example, the UID range 443:557
                    appears.  The same comment about a non-existent unique
                    identifier being ignored without any error message also
                    applies here.  Hence, even if neither UID 443 or 557
                    exist, this range is valid and would include an existing
                    UID 495.

                Also note that a UID range of 559:* always includes the
                UID of the last message in the mailbox, even if 559 is
                higher than any assigned UID value.  This is because the
                contents of a range are independent of the order of the
                range endpoints.  Thus, any UID range with * as one of
                the endpoints indicates at least one message (the
                message with the highest numbered UID), unless the
                mailbox is empty.

                The number after the "*" in an untagged FETCH response is always a
                message sequence number, not a unique identifier, even for a UID
                command response.  However, server implementations MUST implicitly
                include the UID message data item as part of any FETCH response
                caused by a UID command, regardless of whether a UID was specified
                as a message data item to the FETCH.

                    Note: The rule about including the UID message data item as part
                    of a FETCH response primarily applies to the UID FETCH and UID
                    STORE commands, including a UID FETCH command that does not
                    include UID as a message data item.  Although it is unlikely that
                    the other UID commands will cause an untagged FETCH, this rule
                    applies to these commands as well.

                Example:    C: A999 UID FETCH 4827313:4828442 FLAGS
                            S: * 23 FETCH (FLAGS (\Seen) UID 4827313)
                            S: * 24 FETCH (FLAGS (\Seen) UID 4827943)
                            S: * 25 FETCH (FLAGS (\Seen) UID 4828442)
                            S: A999 OK UID FETCH completed
            */

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            if(m_pSelectedFolder == null){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Error: This command is valid only in selected state."));

                return;
            }

            string[] cmd_cmtText = cmdText.Split(new char[]{' '},2);
                        
            if(string.Equals(cmd_cmtText[0],"COPY",StringComparison.InvariantCultureIgnoreCase)){
                COPY(true,cmdTag,cmd_cmtText[1]);
            }
            else if(string.Equals(cmd_cmtText[0],"FETCH",StringComparison.InvariantCultureIgnoreCase)){
                FETCH(true,cmdTag,cmd_cmtText[1]);
            }
            else if(string.Equals(cmd_cmtText[0],"STORE",StringComparison.InvariantCultureIgnoreCase)){
                STORE(true,cmdTag,cmd_cmtText[1]);
            }
            else if(string.Equals(cmd_cmtText[0],"SEARCH",StringComparison.InvariantCultureIgnoreCase)){
                SEARCH(true,cmdTag,cmd_cmtText[1]);
            }
            else{
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"BAD","Error in arguments."));
            }
        }

        #endregion

        #region method EXPUNGE

        private void EXPUNGE(string cmdTag,string cmdText)
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

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return;
            }
            if(m_pSelectedFolder == null){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Error: This command is valid only in selected state."));

                return;
            }

            // Store start time
			long startTime = DateTime.Now.Ticks;
            
            IMAP_r_ServerStatus response = new IMAP_r_ServerStatus(cmdTag,"OK","EXPUNGE completed in " + ((DateTime.Now.Ticks - startTime) / (decimal)10000000).ToString("f2") + " seconds.");
            for(int i=0;i<m_pSelectedFolder.MessagesInfo.Length;i++){
                IMAP_MessageInfo msgInfo = m_pSelectedFolder.MessagesInfo[i];
                if(msgInfo.ContainsFlag("Deleted")){
                    IMAP_e_Expunge e = OnExpunge(msgInfo,response);
                    // Expunge failed.
                    if(!string.Equals(e.Response.ResponseCode,"OK",StringComparison.InvariantCultureIgnoreCase)){
                        m_pResponseSender.SendResponseAsync(e.Response);

                        return;
                    }
                    m_pSelectedFolder.RemoveMessage(msgInfo);

                    m_pResponseSender.SendResponseAsync(new IMAP_r_u_Expunge(i + 1));
                }
            }
            m_pSelectedFolder.Reindex();
            
            m_pResponseSender.SendResponseAsync(response);
        }

        #endregion

        #region method IDLE

        private bool IDLE(string cmdTag,string cmdText)
        {
            /* RFC 2177 3. IDLE Command.
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

                The server MAY consider a client inactive if it has an IDLE command
                running, and if such a server has an inactivity timeout it MAY log
                the client off implicitly at the end of its timeout period.  Because
                of that, clients using IDLE are advised to terminate the IDLE and
                re-issue it at least every 29 minutes to avoid being logged off.
                This still allows a client to receive immediate mailbox updates even
                though it need only "poll" at half hour intervals.

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
                            ...another client expunges message 2 now...
                            C: A003 FETCH 4 ALL
                            S: * 4 FETCH (...)
                            S: A003 OK FETCH completed
                            C: A004 IDLE
                            S: * 2 EXPUNGE
                            S: * 3 EXISTS
                            S: + idling
                            ...time passes; another client expunges message 3...
                            S: * 3 EXPUNGE
                            S: * 2 EXISTS
                            ...time passes; new mail arrives...
                            S: * 3 EXISTS
                            C: DONE
                            S: A004 OK IDLE terminated
                            C: A005 FETCH 3 ALL
                            S: * 3 FETCH (...)
                            S: A005 OK FETCH completed
            */

            if(!this.IsAuthenticated){
                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"NO","Authentication required."));

                return true;
            }

            m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus("+","idling"));

            TimerEx timer = new TimerEx(30000,true);
            timer.Elapsed += new System.Timers.ElapsedEventHandler(delegate(object sender,System.Timers.ElapsedEventArgs e){
                try{
                    UpdateSelectedFolderAndSendChanges();
                }
                catch{
                }
            });
            timer.Enabled = true;

            // Read client response. 
            SmartStream.ReadLineAsyncOP readLineOP = new SmartStream.ReadLineAsyncOP(new byte[32000],SizeExceededAction.JunkAndThrowException);
            readLineOP.Completed += new EventHandler<EventArgs<SmartStream.ReadLineAsyncOP>>(delegate(object sender,EventArgs<SmartStream.ReadLineAsyncOP> e){
                try{
                    if(readLineOP.Error != null){
                        LogAddText("Error: " + readLineOP.Error.Message);
                        timer.Dispose();

                        return;
                    }
                    // Remote host closed connection.
                    else if(readLineOP.BytesInBuffer == 0){
                        LogAddText("Remote host(connected client) closed IMAP connection.");
                        timer.Dispose();
                        Dispose();

                        return;
                    }

                    LogAddRead(readLineOP.BytesInBuffer,readLineOP.LineUtf8);

                    if(string.Equals(readLineOP.LineUtf8,"DONE",StringComparison.InvariantCultureIgnoreCase)){
                        timer.Dispose();

                        m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","IDLE terminated."));
                        BeginReadCmd();
                    }
                    else{
                        while(this.TcpStream.ReadLine(readLineOP,true)){
                            if(readLineOP.Error != null){
                                LogAddText("Error: " + readLineOP.Error.Message);
                                timer.Dispose();

                                return;
                            }
                            LogAddRead(readLineOP.BytesInBuffer,readLineOP.LineUtf8);

                            if(string.Equals(readLineOP.LineUtf8,"DONE",StringComparison.InvariantCultureIgnoreCase)){
                                timer.Dispose();

                                m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","IDLE terminated."));
                                BeginReadCmd();

                                break;
                            }
                        }
                    }
                }
                catch(Exception x){
                    timer.Dispose();

                    OnError(x);
                }
            });
            while(this.TcpStream.ReadLine(readLineOP,true)){
                if(readLineOP.Error != null){
                    LogAddText("Error: " + readLineOP.Error.Message);
                    timer.Dispose();

                    break;
                }

                LogAddRead(readLineOP.BytesInBuffer,readLineOP.LineUtf8);

                if(string.Equals(readLineOP.LineUtf8,"DONE",StringComparison.InvariantCultureIgnoreCase)){
                    timer.Dispose();

                    m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","IDLE terminated."));
                    BeginReadCmd();

                    break;
                }                
            }
            
            return false;            
        }

        #endregion


        #region method CAPABILITY

        private void CAPABILITY(string cmdTag,string cmdText)
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

            List<string> capabilities = new List<string>();
            if(!this.IsSecureConnection && this.Certificate != null){
                capabilities.Add("STARTTLS");
            }
            foreach(string c in m_pCapabilities){
                capabilities.Add(c);
            }
            foreach(AUTH_SASL_ServerMechanism auth in this.Authentications.Values){
                capabilities.Add("AUTH=" + auth.Name);
            }

            m_pResponseSender.SendResponseAsync(new IMAP_r_u_Capability(capabilities.ToArray()));
            m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","CAPABILITY completed."));
        }

        #endregion

        #region method NOOP

        private void NOOP(string cmdTag,string cmdText)
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

                Example:    C: a002 NOOP
                            S: a002 OK NOOP completed
                            . . .
                            C: a047 NOOP
                            S: * 22 EXPUNGE
                            S: * 23 EXISTS
                            S: * 3 RECENT
                            S: * 14 FETCH (FLAGS (\Seen \Deleted))
                            S: a047 OK NOOP completed
            */

            // Store start time
			long startTime = DateTime.Now.Ticks;

            if(m_pSelectedFolder != null){
                UpdateSelectedFolderAndSendChanges();
            }

            m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","NOOP Completed in " + ((DateTime.Now.Ticks - startTime) / (decimal)10000000).ToString("f2") + " seconds."));
        }

        #endregion

        #region method LOGOUT

        private void LOGOUT(string cmdTag,string cmdText)
        {
            /* RFC 3501 6.1.3. LOGOUT Command.
                Arguments:  none

                Responses:  REQUIRED untagged response: BYE

                Result:     OK - logout completed
                            BAD - command unknown or arguments invalid

                The LOGOUT command informs the server that the client is done with
                the connection.  The server MUST send a BYE untagged response
                before the (tagged) OK response, and then close the network
                connection.

                Example:    C: A023 LOGOUT
                            S: * BYE IMAP4rev1 Server logging out
                            S: A023 OK LOGOUT completed
                            (Server and client then close the connection)
            */

            try{
                m_pResponseSender.SendResponseAsync(new IMAP_r_u_Bye("IMAP4rev1 Server logging out."));

                // Create callback which is called when BYE comletes asynchronously.
                EventHandler<EventArgs<Exception>> byeCompletedAsyncCallback = delegate(object s,EventArgs<Exception> e){
                    try{
                        Disconnect();
                        Dispose();
                    }
                    catch{
                    }
                };
                                
                // BYE completed synchronously.
                if(!m_pResponseSender.SendResponseAsync(new IMAP_r_ServerStatus(cmdTag,"OK","LOGOUT completed."),byeCompletedAsyncCallback)){
                    Disconnect();
                    Dispose();
                }
                // BYE completed asynchronously, callback byeCompletedAsyncCallback is called when operation completes.
                //else{
            }
            catch{
                Disconnect();
                Dispose();
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

            byte[] buffer = null;
            if(line.EndsWith("\r\n")){
                buffer = Encoding.UTF8.GetBytes(line);
            }
            else{
                buffer = Encoding.UTF8.GetBytes(line + "\r\n");
            }

            this.TcpStream.Write(buffer,0,buffer.Length);
            
            // Log.
            if(this.Server.Logger != null){
                this.Server.Logger.AddWrite(this.ID,this.AuthenticatedUserIdentity,buffer.Length,line,this.LocalEndPoint,this.RemoteEndPoint);
            }
        }

        #endregion

        #region method LogAddRead

        /// <summary>
        /// Logs read operation.
        /// </summary>
        /// <param name="size">Number of bytes read.</param>
        /// <param name="text">Log text.</param>
        public void LogAddRead(long size,string text)
        {
            try{
                if(this.Server.Logger != null){
                    this.Server.Logger.AddRead(
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
        public void LogAddWrite(long size,string text)
        {
            try{
                if(this.Server.Logger != null){
                    this.Server.Logger.AddWrite(
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
        /// Logs specified text.
        /// </summary>
        /// <param name="text">text to log.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>text</b> is null reference.</exception>
        public void LogAddText(string text)
        {
            if(text == null){
                throw new ArgumentNullException("text");
            }
            
            try{
                if(this.Server.Logger != null){
                    this.Server.Logger.AddText(
                        this.ID,
                        this.AuthenticatedUserIdentity,
                        text,                        
                        this.LocalEndPoint,
                        this.RemoteEndPoint
                    );
                }
            }
            catch{
            }
        }

        #endregion

        #region method LogAddException

        /// <summary>
        /// Logs specified exception.
        /// </summary>
        /// <param name="exception">Exception to log.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>exception</b> is null reference.</exception>
        public void LogAddException(Exception exception)
        {
            if(exception == null){
                throw new ArgumentNullException("exception");
            }
            
            try{
                if(this.Server.Logger != null){
                    this.Server.Logger.AddException(
                        this.ID,
                        this.AuthenticatedUserIdentity,
                        exception.Message,                        
                        this.LocalEndPoint,
                        this.RemoteEndPoint,
                        exception
                    );
                }
            }
            catch{
            }
        }

        #endregion

        #region method UpdateSelectedFolderAndSendChanges

        /// <summary>
        /// Updates current slected folder status and sends currently selected folder changes versus current folder state.
        /// </summary>
        private void UpdateSelectedFolderAndSendChanges()
        {
            if(m_pSelectedFolder == null){
                return;
            }
                        
            IMAP_e_MessagesInfo e = OnGetMessagesInfo(m_pSelectedFolder.Folder);
            
            int currentExists = m_pSelectedFolder.MessagesInfo.Length;
            // Create ID indexed lookup table for new messages.
            Dictionary<string,string> newMessagesLookup = new Dictionary<string,string>();
            foreach(IMAP_MessageInfo msgInfo in e.MessagesInfo){
                newMessagesLookup.Add(msgInfo.ID,null);
            }
            
            StringBuilder retVal = new StringBuilder();
            // Check deleted messages, send "* n EXPUNGE" for each deleted message.
            foreach(IMAP_MessageInfo msgInfo in m_pSelectedFolder.MessagesInfo){
                // Message deleted.
                if(!newMessagesLookup.ContainsKey(msgInfo.ID)){
                    retVal.Append("* " + m_pSelectedFolder.GetSeqNo(msgInfo) + " EXPUNGE\r\n");
                    m_pSelectedFolder.RemoveMessage(msgInfo);
                }
            }

            // Send EXISTS if current count differs from existing.
            if(currentExists != e.MessagesInfo.Count){
                retVal.Append("* " + e.MessagesInfo.Count + " EXISTS\r\n");
            }

            // Send STATUS change responses.
            if(retVal.Length > 0){
                WriteLine(retVal.ToString());                
            }

            // Create new selected folder based on new messages info.
            m_pSelectedFolder = new _SelectedFolder(m_pSelectedFolder.Folder,m_pSelectedFolder.IsReadOnly,e.MessagesInfo);
        }

        #endregion

        #region method SupportsCap

        /// <summary>
        /// Gets if session supports specified capability.
        /// </summary>
        /// <param name="capability">Capability name.</param>
        /// <returns>Returns true if session supports specified capability.</returns>
        private bool SupportsCap(string capability)
        {
            foreach(string c in m_pCapabilities){
                if(string.Equals(c,capability,StringComparison.InvariantCultureIgnoreCase)){
                    return true;
                }
            }

            return false;
        }

        #endregion

        #region method ParsePartNumberFromSection

        /// <summary>
        /// Parses MIME part-number specifier from BODY[] section string.
        /// </summary>
        /// <param name="section">Section string.</param>
        /// <returns>Returns part-number.</returns>
        /// <exception cref="ArgumentNullException">Is raised wehn <b>section</b> is null reference.</exception>
        private string ParsePartNumberFromSection(string section)
        {
            if(section == null){
                throw new ArgumentNullException("section");
            }

            StringBuilder retVal = new StringBuilder();
            string[] parts = section.Split('.');
            foreach(string part in parts){
                if(Net_Utils.IsInteger(part)){
                    if(retVal.Length > 0){
                        retVal.Append(".");
                    }
                    retVal.Append(part);
                }
                else{
                    break;
                }
            }

            return retVal.ToString();
        }

        #endregion

        #region method ParsePartSpecifierFromSection

        /// <summary>
        /// Parses MIME part specifier from BODY[] section string.
        /// </summary>
        /// <param name="section">Section string.</param>
        /// <returns>Returns specifier.</returns>
        /// <exception cref="ArgumentNullException">Is raised wehn <b>section</b> is null reference.</exception>
        private string ParsePartSpecifierFromSection(string section)
        {
            if(section == null){
                throw new ArgumentNullException("section");
            }

            StringBuilder retVal = new StringBuilder();
            string[] parts = section.Split(' ')[0].Split('.');
            foreach(string part in parts){
                if(!Net_Utils.IsInteger(part)){
                    if(retVal.Length > 0){
                        retVal.Append(".");
                    }
                    retVal.Append(part);
                }
            }

            return retVal.ToString();
        }

        #endregion

        #region method GetMimeEntity

        /// <summary>
		/// Gets specified mime entity. Returns null if specified mime entity doesn't exist.
		/// </summary>
		/// <param name="message">Mail message.</param>
		/// <param name="partNumber">MIME part-number specifier. Nested mime entities are pointed by '.'. 
		/// For example: 1,1.1,2.1, ... .</param>
		/// <returns></returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>message</b> is null reference.</exception>
		public MIME_Entity GetMimeEntity(Mail_Message message,string partNumber)
		{
            if(message == null){
                throw new ArgumentNullException("message");
            }
            			
			// For single part message there is only one entity with value 1.
			// Example:
			//		header
			//		entity -> 1
			
			// For multipart message, entity counting starts from MainEntity.ChildEntities
			// Example:
			//		header
			//		multipart/mixed
			//			text/plain  -> 1
			//			application/pdf  -> 2
			//          ...

            // TODO: nested rfc 822 message

            if(partNumber == string.Empty){
                return message;
            }

			// Single part
			if(message.ContentType == null || message.ContentType.Type.ToLower() != "multipart"){
				if(Convert.ToInt32(partNumber) == 1){
					return message;
				}
				else{
					return null;
				}
			}
			// multipart
			else{                
				MIME_Entity entity = message;
				string[] parts = partNumber.Split('.');
				foreach(string part in parts){
					int mEntryNo = Convert.ToInt32(part) - 1; // Enitites are zero base, mimeEntitySpecifier is 1 based.
                    if(entity.Body is MIME_b_Multipart){
                        MIME_b_Multipart multipart = (MIME_b_Multipart)entity.Body;
                        if(mEntryNo > -1 && mEntryNo < multipart.BodyParts.Count){
						    entity = multipart.BodyParts[mEntryNo];
					    }
                        else{
                            return null;
                        }
                    }
			        else{
                        return null;
                    }
				}

				return entity;
			}			
		}

		#endregion

        #region mehtod ConstructBodyStructure

		/// <summary>
		/// Constructs FETCH BODY and BODYSTRUCTURE response.
		/// </summary>
		/// <param name="message">Mail message.</param>
		/// <param name="bodystructure">Specifies if to construct BODY or BODYSTRUCTURE.</param>
		/// <returns></returns>
		public string ConstructBodyStructure(Mail_Message message,bool bodystructure)
		{			
			if(bodystructure){
				return "BODYSTRUCTURE " + ConstructParts(message,bodystructure);
			}
			else{
				return "BODY " + ConstructParts(message,bodystructure);
			}
		}

		/// <summary>
		/// Constructs specified entity and it's childentities bodystructure string.
		/// </summary>
		/// <param name="entity">Mime entity.</param>
		/// <param name="bodystructure">Specifies if to construct BODY or BODYSTRUCTURE.</param>
		/// <returns></returns>
		private string ConstructParts(MIME_Entity entity,bool bodystructure)
		{
			/* RFC 3501 7.4.2 BODYSTRUCTURE
							  BODY A form of BODYSTRUCTURE without extension data.
			  
				A parenthesized list that describes the [MIME-IMB] body
				structure of a message.  This is computed by the server by
				parsing the [MIME-IMB] header fields, defaulting various fields
				as necessary.

				For example, a simple text message of 48 lines and 2279 octets
				can have a body structure of: ("TEXT" "PLAIN" ("CHARSET"
				"US-ASCII") NIL NIL "7BIT" 2279 48)

				Multiple parts are indicated by parenthesis nesting.  Instead
				of a body type as the first element of the parenthesized list,
				there is a sequence of one or more nested body structures.  The
				second element of the parenthesized list is the multipart
				subtype (mixed, digest, parallel, alternative, etc.).
					
				For example, a two part message consisting of a text and a
				BASE64-encoded text attachment can have a body structure of:
				(("TEXT" "PLAIN" ("CHARSET" "US-ASCII") NIL NIL "7BIT" 1152
				23)("TEXT" "PLAIN" ("CHARSET" "US-ASCII" "NAME" "cc.diff")
				"<960723163407.20117h@cac.washington.edu>" "Compiler diff"
				"BASE64" 4554 73) "MIXED")

				Extension data follows the multipart subtype.  Extension data
				is never returned with the BODY fetch, but can be returned with
				a BODYSTRUCTURE fetch.  Extension data, if present, MUST be in
				the defined order.  The extension data of a multipart body part
				are in the following order:

				body parameter parenthesized list
					A parenthesized list of attribute/value pairs [e.g., ("foo"
					"bar" "baz" "rag") where "bar" is the value of "foo", and
					"rag" is the value of "baz"] as defined in [MIME-IMB].

				body disposition
					A parenthesized list, consisting of a disposition type
					string, followed by a parenthesized list of disposition
					attribute/value pairs as defined in [DISPOSITION].

				body language
					A string or parenthesized list giving the body language
					value as defined in [LANGUAGE-TAGS].

				body location
					A string list giving the body content URI as defined in [LOCATION].

				Any following extension data are not yet defined in this
				version of the protocol.  Such extension data can consist of
				zero or more NILs, strings, numbers, or potentially nested
				parenthesized lists of such data.  Client implementations that
				do a BODYSTRUCTURE fetch MUST be prepared to accept such
				extension data.  Server implementations MUST NOT send such
				extension data until it has been defined by a revision of this
				protocol.

				The basic fields of a non-multipart body part are in the
				following order:

				body type
					A string giving the content media type name as defined in [MIME-IMB].
				
				body subtype
					 A string giving the content subtype name as defined in [MIME-IMB].

				body parameter parenthesized list
					A parenthesized list of attribute/value pairs [e.g., ("foo"
					"bar" "baz" "rag") where "bar" is the value of "foo" and
					"rag" is the value of "baz"] as defined in [MIME-IMB].

				body id
					A string giving the content id as defined in [MIME-IMB].

				body description
					A string giving the content description as defined in [MIME-IMB].

				body encoding
					A string giving the content transfer encoding as defined in	[MIME-IMB].

				body size
					A number giving the size of the body in octets.  Note that
					this size is the size in its transfer encoding and not the
					resulting size after any decoding.

				A body type of type MESSAGE and subtype RFC822 contains,
				immediately after the basic fields, the envelope structure,
				body structure, and size in text lines of the encapsulated
				message.

				A body type of type TEXT contains, immediately after the basic
				fields, the size of the body in text lines.  Note that this
				size is the size in its content transfer encoding and not the
				resulting size after any decoding.

				Extension data follows the basic fields and the type-specific
				fields listed above.  Extension data is never returned with the
				BODY fetch, but can be returned with a BODYSTRUCTURE fetch.
				Extension data, if present, MUST be in the defined order.

				The extension data of a non-multipart body part are in the
				following order:

				body MD5
					A string giving the body MD5 value as defined in [MD5].
					
				body disposition
					A parenthesized list with the same content and function as
					the body disposition for a multipart body part.

				body language
					A string or parenthesized list giving the body language
					value as defined in [LANGUAGE-TAGS].

				body location
					A string list giving the body content URI as defined in [LOCATION].

				Any following extension data are not yet defined in this
				version of the protocol, and would be as described above under
				multipart extension data.
			
			
				// We don't construct extention fields like rfc says:
					Server implementations MUST NOT send such
					extension data until it has been defined by a revision of this
					protocol.
			
										
				contentTypeMainMediaType - Example: 'TEXT'
				contentTypeSubMediaType  - Example: 'PLAIN'
				conentTypeParameters     - Example: '("CHARSET" "iso-8859-1" ...)'
				contentID                - Content-ID: header field value.
				contentDescription       - Content-Description: header field value.
				contentEncoding          - Content-Transfer-Encoding: header field value.
				contentSize              - mimeEntity ENCODED data size
				[envelope]               - NOTE: included only if contentType = "message" !!!
				[contentLines]           - number of ENCODED data lines. NOTE: included only if contentType = "text" !!!
									   			
				// Basic fields for multipart
				(nestedMimeEntries) contentTypeSubMediaType
												
				// Basic fields for non-multipart
				contentTypeMainMediaType contentTypeSubMediaType (conentTypeParameters) contentID contentDescription contentEncoding contentSize [envelope] [contentLine]

			*/

            MIME_Encoding_EncodedWord wordEncoder = new MIME_Encoding_EncodedWord(MIME_EncodedWordEncoding.B,Encoding.UTF8);
            wordEncoder.Split = false;

			StringBuilder retVal = new StringBuilder();
			// Multipart message
			if(entity.Body is MIME_b_Multipart){
				retVal.Append("(");

				// Construct child entities.
				foreach(MIME_Entity childEntity in ((MIME_b_Multipart)entity.Body).BodyParts){
					// Construct child entity. This can be multipart or non multipart.
					retVal.Append(ConstructParts(childEntity,bodystructure));
				}
			
				// Add contentTypeSubMediaType
                if(entity.ContentType != null && entity.ContentType.SubType != null){
                    retVal.Append(" \"" + entity.ContentType.SubType + "\"");
                }
                else{
                    retVal.Append(" NIL");
                }

				retVal.Append(")");
			}
			// Single part message
			else{
				retVal.Append("(");

				// NOTE: all header fields and parameters must in ENCODED form !!!

				// Add contentTypeMainMediaType
				if(entity.ContentType != null && entity.ContentType.Type != null){
					retVal.Append("\"" + entity.ContentType.Type + "\"");
				}
				else{
					retVal.Append("NIL");
				}

                // Add contentTypeSubMediaType
                if(entity.ContentType != null && entity.ContentType.SubType != null){
                    retVal.Append(" \"" + entity.ContentType.SubType + "\"");
                }
                else{
                    retVal.Append(" NIL");
                }

				// conentTypeParameters - Syntax: {("name" SP "value" *(SP "name" SP "value"))}
				if(entity.ContentType != null){
                    if(entity.ContentType.Parameters.Count > 0){
                        retVal.Append(" (");
                        bool first = true;
                        foreach(MIME_h_Parameter parameter in entity.ContentType.Parameters){
                            // For the first item, don't add SP.
                            if(first){
                                first = false;
                            }
                            else{
                                retVal.Append(" ");
                            }

                            retVal.Append("\"" + parameter.Name + "\" \"" + wordEncoder.Encode(parameter.Value) + "\"");
                        }
                        retVal.Append(")");
                    }
                    else{
                        retVal.Append(" NIL");
                    }
				}
				else{
					retVal.Append(" NIL");
				}

				// contentID
				string contentID = entity.ContentID;
				if(contentID != null){
					retVal.Append(" \"" + wordEncoder.Encode(contentID) + "\""); 
				}
				else{
					retVal.Append(" NIL");
				}

				// contentDescription
				string contentDescription = entity.ContentDescription;
				if(contentDescription != null){
					retVal.Append(" \"" + wordEncoder.Encode(contentDescription) + "\""); 
				}
				else{
					retVal.Append(" NIL");
				}

				// contentEncoding
				if(entity.ContentTransferEncoding != null){
					retVal.Append(" \"" + wordEncoder.Encode(entity.ContentTransferEncoding) + "\""); 
				}
				else{
					// If not specified, then must be 7bit.
					retVal.Append(" \"7bit\"");
				}

				// contentSize
				if(entity.Body is MIME_b_SinglepartBase){                    
					retVal.Append(" " + ((MIME_b_SinglepartBase)entity.Body).EncodedData.Length.ToString());
				}
				else{
					retVal.Append(" 0");
				}

				// envelope ---> FOR ContentType: message/rfc822 ONLY ###
				if(entity.Body is MIME_b_MessageRfc822){                    
					retVal.Append(" " + IMAP_t_Fetch_r_i_Envelope.ConstructEnvelope(((MIME_b_MessageRfc822)entity.Body).Message));

                    // TODO: BODYSTRUCTURE,LINES
				}

				// contentLines ---> FOR ContentType: text/xxx ONLY ###
				if(entity.Body is MIME_b_Text){                    
				    long lineCount = 0;
					StreamLineReader r = new StreamLineReader(new MemoryStream(((MIME_b_SinglepartBase)entity.Body).EncodedData));
					byte[] line = r.ReadLine();
					while(line != null){
						lineCount++;

						line = r.ReadLine();
					}
						
					retVal.Append(" " + lineCount.ToString());
				}

				retVal.Append(")");
			}

			return retVal.ToString();
		}

		#endregion


        #region Properties implementation

        /// <summary>
        /// Gets session owner IMAP server.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public new IMAP_Server Server
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return (IMAP_Server)base.Server;
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
        /// Gets number of bad commands happened on IMAP session.
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
        /// Gets session supported CAPABILITIES.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public List<string> Capabilities
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pCapabilities; 
            }
        }

        /// <summary>
        /// Gets selected folder name with optional path. Value null means no selected folder.
        /// </summary>
        public string SelectedFolderName
        {
            get{ 
                if(m_pSelectedFolder == null){
                    return null;
                }
                else{
                    return m_pSelectedFolder.Folder; 
                }
            }
        }


        /// <summary>
        /// Gets mailbox encoding.
        /// </summary>
        internal IMAP_Mailbox_Encoding MailboxEncoding
        {
            get{ return m_MailboxEncoding; }
        }

        #endregion

        #region Events implementation

        /// <summary>
        /// Is raised when session has started processing and needs to send "* OK ..." greeting or "* NO ..." error resposne to the connected client.
        /// </summary>
        public event EventHandler<IMAP_e_Started> Started = null;

        #region method OnStarted

        /// <summary>
        /// Raises <b>Started</b> event.
        /// </summary>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Started OnStarted(IMAP_r_u_ServerStatus response)
        {
            IMAP_e_Started eArgs = new IMAP_e_Started(response);            
            if(this.Started != null){                
                this.Started(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle LOGIN command.
        /// </summary>
        public event EventHandler<IMAP_e_Login> Login = null;

        #region method OnLogin

        /// <summary>
        /// Raises <b>Login</b> event.
        /// </summary>
        /// <param name="user">User name.</param>
        /// <param name="password">Password.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Login OnLogin(string user,string password)
        {
            IMAP_e_Login eArgs = new IMAP_e_Login(user,password);            
            if(this.Login != null){                
                this.Login(this,eArgs);
            }

            return eArgs;
        }

        #endregion


        /// <summary>
        /// Is raised when IMAP session needs to handle NAMESPACE command.
        /// </summary>
        public event EventHandler<IMAP_e_Namespace> Namespace = null;

        #region method OnNamespace

        /// <summary>
        /// Raises <b>Namespace</b> event.
        /// </summary>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Namespace OnNamespace(IMAP_r_ServerStatus response)
        {
            IMAP_e_Namespace eArgs = new IMAP_e_Namespace(response);            
            if(this.Namespace != null){                
                this.Namespace(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle LIST command.
        /// </summary>
        public event EventHandler<IMAP_e_List> List = null;

        #region method OnList

        /// <summary>
        /// Raises <b>List</b> event.
        /// </summary>
        /// <param name="refName">Folder reference name.</param>
        /// <param name="folder">Folder filter.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_List OnList(string refName,string folder)
        {
            IMAP_e_List eArgs = new IMAP_e_List(refName,folder);
            if(this.List != null){
                this.List(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle CREATE command.
        /// </summary>
        public event EventHandler<IMAP_e_Folder> Create = null;

        #region method OnCreate

        /// <summary>
        /// Raises <b>Create</b> event.
        /// </summary>
        /// <param name="cmdTag">Command tag.</param>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Folder OnCreate(string cmdTag,string folder,IMAP_r_ServerStatus response)
        {
            IMAP_e_Folder eArgs = new IMAP_e_Folder(cmdTag,folder,response);
            if(this.Create != null){
                this.Create(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle DELETE command.
        /// </summary>
        public event EventHandler<IMAP_e_Folder> Delete = null;

        #region method OnDelete

        /// <summary>
        /// Raises <b>Delete</b> event.
        /// </summary>
        /// <param name="cmdTag">Command tag.</param>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Folder OnDelete(string cmdTag,string folder,IMAP_r_ServerStatus response)
        {
            IMAP_e_Folder eArgs = new IMAP_e_Folder(cmdTag,folder,response);
            if(this.Delete != null){
                this.Delete(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle RENAME command.
        /// </summary>
        public event EventHandler<IMAP_e_Rename> Rename = null;

        #region method OnRename

        /// <summary>
        /// Raises <b>Rename</b> event.
        /// </summary>
        /// <param name="cmdTag">Command tag.</param>
        /// <param name="currentFolder">Current folder name with optional path.</param>
        /// <param name="newFolder">New folder name with optional path.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Rename OnRename(string cmdTag,string currentFolder,string newFolder)
        {
            IMAP_e_Rename eArgs = new IMAP_e_Rename(cmdTag,currentFolder,newFolder);
            if(this.Rename != null){
                this.Rename(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle LSUB command.
        /// </summary>
        public event EventHandler<IMAP_e_LSub> LSub = null;

        #region method OnLSub

        /// <summary>
        /// Raises <b>LSub</b> event.
        /// </summary>
        /// <param name="refName">Folder reference name.</param>
        /// <param name="folder">Folder filter.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_LSub OnLSub(string refName,string folder)
        {
            IMAP_e_LSub eArgs = new IMAP_e_LSub(refName,folder);
            if(this.LSub != null){
                this.LSub(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle SUBSCRIBE command.
        /// </summary>
        public event EventHandler<IMAP_e_Folder> Subscribe = null;

        #region method OnSubscribe

        /// <summary>
        /// Raises <b>Subscribe</b> event.
        /// </summary>
        /// <param name="cmdTag">Command tag.</param>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Folder OnSubscribe(string cmdTag,string folder,IMAP_r_ServerStatus response)
        {
            IMAP_e_Folder eArgs = new IMAP_e_Folder(cmdTag,folder,response);
            if(this.Subscribe != null){
                this.Subscribe(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle SUBSCRIBE command.
        /// </summary>
        public event EventHandler<IMAP_e_Folder> Unsubscribe = null;

        #region method OnUnsubscribe

        /// <summary>
        /// Raises <b>OnUnsubscribe</b> event.
        /// </summary>
        /// <param name="cmdTag">Command tag.</param>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Folder OnUnsubscribe(string cmdTag,string folder,IMAP_r_ServerStatus response)
        {
            IMAP_e_Folder eArgs = new IMAP_e_Folder(cmdTag,folder,response);
            if(this.Unsubscribe != null){
                this.Unsubscribe(this,eArgs);
            }

            return eArgs;
        }

        #endregion
                
        /// <summary>
        /// Is raised when IMAP session needs to handle SELECT command.
        /// </summary>
        public event EventHandler<IMAP_e_Select> Select = null;

        #region method OnSelect

        /// <summary>
        /// Raises <b>Select</b> event.
        /// </summary>
        /// <param name="cmdTag">Command tag.</param>
        /// <param name="folder">Folder name with optional path.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Select OnSelect(string cmdTag,string folder)
        {
            IMAP_e_Select eArgs = new IMAP_e_Select(cmdTag,folder);
            if(this.Select != null){
                this.Select(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to get folder messages info.
        /// </summary>
        public event EventHandler<IMAP_e_MessagesInfo> GetMessagesInfo = null;

        #region method OnGetMessagesInfo

        /// <summary>
        /// Raises <b>GetMessagesInfo</b> event.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_MessagesInfo OnGetMessagesInfo(string folder)
        {
            IMAP_e_MessagesInfo eArgs = new IMAP_e_MessagesInfo(folder);
            if(this.GetMessagesInfo != null){
                this.GetMessagesInfo(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle APPEND command.
        /// </summary>
        public event EventHandler<IMAP_e_Append> Append = null;

        #region method OnAppend

        /// <summary>
        /// Raises <b>StoreMessage</b> event.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="flags">Message flags.</param>
        /// <param name="date">Message IMAP internal date.</param>
        /// <param name="size">Message size in bytes.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Append OnAppend(string folder,string[] flags,DateTime date,int size,IMAP_r_ServerStatus response)
        {
            IMAP_e_Append eArgs = new IMAP_e_Append(folder,flags,date,size,response);
            if(this.Append != null){
                this.Append(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle GETQUOTAROOT command.
        /// </summary>
        public event EventHandler<IMAP_e_GetQuotaRoot> GetQuotaRoot = null;

        #region method OnGetGuotaRoot

        /// <summary>
        /// Raises <b>GetQuotaRoot</b> event.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_GetQuotaRoot OnGetGuotaRoot(string folder,IMAP_r_ServerStatus response)
        {
            IMAP_e_GetQuotaRoot eArgs = new IMAP_e_GetQuotaRoot(folder,response);
            if(this.GetQuotaRoot != null){
                this.GetQuotaRoot(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle GETQUOTA command.
        /// </summary>
        public event EventHandler<IMAP_e_GetQuota> GetQuota = null;

        #region method OnGetQuota

        /// <summary>
        /// Raises <b>GetQuota</b> event.
        /// </summary>
        /// <param name="quotaRoot">Quota root name.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_GetQuota OnGetQuota(string quotaRoot,IMAP_r_ServerStatus response)
        {
            IMAP_e_GetQuota eArgs = new IMAP_e_GetQuota(quotaRoot,response);
            if(this.GetQuota != null){
                this.GetQuota(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle GETACL command.
        /// </summary>
        public event EventHandler<IMAP_e_GetAcl> GetAcl = null;

        #region method OnGetAcl

        /// <summary>
        /// Raises <b>GetAcl</b> event.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_GetAcl OnGetAcl(string folder,IMAP_r_ServerStatus response)
        {
            IMAP_e_GetAcl eArgs = new IMAP_e_GetAcl(folder,response);
            if(this.GetAcl != null){
                this.GetAcl(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle SETACL command.
        /// </summary>
        public event EventHandler<IMAP_e_SetAcl> SetAcl = null;

        #region method OnSetAcl

        /// <summary>
        /// Raises <b>SetAcl</b> event.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="identifier">ACL identifier (normally user or group name).</param>
        /// <param name="flagsSetType">Flags set type.</param>
        /// <param name="rights">Identifier rights.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_SetAcl OnSetAcl(string folder,string identifier,IMAP_Flags_SetType flagsSetType,string rights,IMAP_r_ServerStatus response)
        {
            IMAP_e_SetAcl eArgs = new IMAP_e_SetAcl(folder,identifier,flagsSetType,rights,response);
            if(this.SetAcl != null){
                this.SetAcl(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle DELETEACL command.
        /// </summary>
        public event EventHandler<IMAP_e_DeleteAcl> DeleteAcl = null;

        #region method OnDeleteAcl

        /// <summary>
        /// Raises <b>DeleteAcl</b> event.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="identifier">ACL identifier (normally user or group name).</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_DeleteAcl OnDeleteAcl(string folder,string identifier,IMAP_r_ServerStatus response)
        {
            IMAP_e_DeleteAcl eArgs = new IMAP_e_DeleteAcl(folder,identifier,response);
            if(this.DeleteAcl != null){
                this.DeleteAcl(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle LISTRIGHTS command.
        /// </summary>
        public event EventHandler<IMAP_e_ListRights> ListRights = null;

        #region method OnListRights

        /// <summary>
        /// Raises <b>ListRights</b> event.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="identifier">ACL identifier (normally user or group name).</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_ListRights OnListRights(string folder,string identifier,IMAP_r_ServerStatus response)
        {
            IMAP_e_ListRights eArgs = new IMAP_e_ListRights(folder,identifier,response);
            if(this.ListRights != null){
                this.ListRights(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle MYRIGHTS command.
        /// </summary>
        public event EventHandler<IMAP_e_MyRights> MyRights = null;

        #region method OnMyRights

        /// <summary>
        /// Raises <b>MyRights</b> event.
        /// </summary>
        /// <param name="folder">Folder name with optional path.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_MyRights OnMyRights(string folder,IMAP_r_ServerStatus response)
        {
            IMAP_e_MyRights eArgs = new IMAP_e_MyRights(folder,response);
            if(this.MyRights != null){
                this.MyRights(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle FETCH command.
        /// </summary>
        public event EventHandler<IMAP_e_Fetch> Fetch = null;

        #region method OnFetch

        /// <summary>
        /// Raises <b>Fetch</b> event.
        /// </summary>
        /// <param name="e">Event data.</param>
        private void OnFetch(IMAP_e_Fetch e)
        {
            if(this.Fetch != null){
                this.Fetch(this,e);
            }
        }

        #endregion
                
        /// <summary>
        /// Is raised when IMAP session needs to handle SEARCH command.
        /// </summary>
        public event EventHandler<IMAP_e_Search> Search = null;

        #region method OnSearch

        /// <summary>
        /// Raises <b>Search</b> event.
        /// </summary>
        /// <param name="e">Event args.</param>
        private void OnSearch(IMAP_e_Search e)
        {
            if(this.Search != null){
                this.Search(this,e);
            }
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle STORE command.
        /// </summary>
        public event EventHandler<IMAP_e_Store> Store = null;

        #region method OnStore

        /// <summary>
        /// Raises <b>Store</b> event.
        /// </summary>
        /// <param name="msgInfo">Message info.</param>
        /// <param name="setType">Flags set type.</param>
        /// <param name="flags">Flags.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Store OnStore(IMAP_MessageInfo msgInfo,IMAP_Flags_SetType setType,string[] flags,IMAP_r_ServerStatus response)
        {
            IMAP_e_Store eArgs = new IMAP_e_Store(m_pSelectedFolder.Folder,msgInfo,setType,flags,response);
            if(this.Store != null){
                this.Store(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle COPY command.
        /// </summary>
        public event EventHandler<IMAP_e_Copy> Copy = null;

        #region method OnCopy

        /// <summary>
        /// Raises <b>Copy</b> event.
        /// </summary>
        /// <param name="targetFolder">Target folder name with optional path.</param>
        /// <param name="messagesInfo">Messages info.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Copy OnCopy(string targetFolder,IMAP_MessageInfo[] messagesInfo,IMAP_r_ServerStatus response)
        {
            IMAP_e_Copy eArgs = new IMAP_e_Copy(m_pSelectedFolder.Folder,targetFolder,messagesInfo,response);
            if(this.Copy != null){
                this.Copy(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        /// <summary>
        /// Is raised when IMAP session needs to handle EXPUNGE command.
        /// </summary>
        public event EventHandler<IMAP_e_Expunge> Expunge = null;

        #region method OnExpunge

        /// <summary>
        /// Raises <b>Expunge</b> event.
        /// </summary>
        /// <param name="msgInfo">Messgae info.</param>
        /// <param name="response">Default IMAP server response.</param>
        /// <returns>Returns event args.</returns>
        private IMAP_e_Expunge OnExpunge(IMAP_MessageInfo msgInfo,IMAP_r_ServerStatus response)
        {
            IMAP_e_Expunge eArgs = new IMAP_e_Expunge(m_pSelectedFolder.Folder,msgInfo,response);
            if(this.Expunge != null){
                this.Expunge(this,eArgs);
            }

            return eArgs;
        }

        #endregion

        #endregion
    }
}
