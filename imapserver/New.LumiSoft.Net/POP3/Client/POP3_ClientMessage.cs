using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;

using LumiSoft.Net.IO;

namespace LumiSoft.Net.POP3.Client
{
    /// <summary>
    /// This class represents POP3 client message.
    /// </summary>
    public class POP3_ClientMessage
    {
        private POP3_Client m_Pop3Client          = null;
        private int         m_SequenceNumber      = 1;
        private string      m_UID                 = "";
        private int         m_Size                = 0;
        private bool        m_IsMarkedForDeletion = false;
        private bool        m_IsDisposed          = false;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="pop3">Owner POP3 client.</param>
        /// <param name="seqNumber">Message 1 based sequence number.</param>
        /// <param name="size">Message size in bytes.</param>
        internal POP3_ClientMessage(POP3_Client pop3,int seqNumber,int size)
        {
            m_Pop3Client     = pop3;
            m_SequenceNumber = seqNumber;
            m_Size           = size;
        }


        #region method MarkForDeletion

        /// <summary>
        /// Marks message as deleted.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 serveer returns error.</exception>
        public void MarkForDeletion()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(this.IsMarkedForDeletion){
                return;
            }

            using(MarkForDeletionAsyncOP op = new MarkForDeletionAsyncOP()){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<MarkForDeletionAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.MarkForDeletionAsync(op)){
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

        #region method MarkForDeletionAsync

        #region class MarkForDeletionAsyncOP

        /// <summary>
        /// This class represents <see cref="POP3_ClientMessage.MarkForDeletionAsync"/> asynchronous operation.
        /// </summary>
        public class MarkForDeletionAsyncOP : IDisposable,IAsyncOP
        {
            private object             m_pLock         = new object();
            private AsyncOP_State      m_State         = AsyncOP_State.WaitingForStart;
            private Exception          m_pException    = null;
            private POP3_ClientMessage m_pOwner        = null;
            private POP3_Client        m_pPop3Client   = null;
            private bool               m_RiseCompleted = false;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public MarkForDeletionAsyncOP()
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
                m_pOwner      = null;
                m_pPop3Client = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner POP3 client message.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(POP3_ClientMessage owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pOwner      = owner;
                m_pPop3Client = owner.m_Pop3Client;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 1939 5. DELE
                         Arguments:
                             a message-number (required) which may NOT refer to a
                             message marked as deleted

                         Restrictions:
                             may only be given in the TRANSACTION state

                         Discussion:
                             The POP3 server marks the message as deleted.  Any future
                             reference to the message-number associated with the message
                             in a POP3 command generates an error.  The POP3 server does
                             not actually delete the message until the POP3 session
                             enters the UPDATE state.

                         Possible Responses:
                             +OK message deleted
                             -ERR no such message

                         Examples:
                             C: DELE 1
                             S: +OK message 1 deleted
                                ...
                             C: DELE 2
                             S: -ERR message 2 already deleted
			        */

                    byte[] buffer = Encoding.UTF8.GetBytes("DELE " + owner.SequenceNumber.ToString() + "\r\n");

                    // Log
                    m_pPop3Client.LogAddWrite(buffer.Length,"DELE " + owner.SequenceNumber.ToString());

                    // Start command sending.
                    m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.DeleCommandSendingCompleted,null);
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

            #region method DeleCommandSendingCompleted

            /// <summary>
            /// Is called when DELE command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void DeleCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pPop3Client.TcpStream.EndWrite(ar);
                    
                    // Read POP3 server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        DeleReadResponseCompleted(op);
                    };
                    if(m_pPop3Client.TcpStream.ReadLine(op,true)){
                        DeleReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method DeleReadResponseCompleted
            
            /// <summary>
            /// Is called when POP3 server DELE response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void DeleReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
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
                            m_pOwner.m_IsMarkedForDeletion = true;
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
            public event EventHandler<EventArgs<MarkForDeletionAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<MarkForDeletionAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending DELE command to POP3 server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="MarkForDeletionAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool MarkForDeletionAsync(MarkForDeletionAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }          
            if(this.IsMarkedForDeletion){
                throw new InvalidOperationException("Message is already marked for deletion.");
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

        #region mehtod HeaderToString

        /// <summary>
        /// Gets message header as string.
        /// </summary>
        /// <returns>Returns message header as string.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when message is marked for deletion and this method is accessed.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 serveer returns error.</exception>
        public string HeaderToString()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(this.IsMarkedForDeletion){
                throw new InvalidOperationException("Can't access message, it's marked for deletion.");
            }

            return Encoding.Default.GetString(HeaderToByte());
        }

        #endregion

        #region method HeaderToByte

        /// <summary>
        /// Gets message header as byte[] data.
        /// </summary>
        /// <returns>Returns message header as byte[] data.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when message is marked for deletion and this method is accessed.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 serveer returns error.</exception>
        public byte[] HeaderToByte()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(this.IsMarkedForDeletion){
                throw new InvalidOperationException("Can't access message, it's marked for deletion.");
            }

            MemoryStream retVal = new MemoryStream();
            MessageTopLinesToStream(retVal,0);

            return retVal.ToArray();
        }

        #endregion

        #region method HeaderToStream

        /// <summary>
        /// Stores message header to the specified stream.
        /// </summary>
        /// <param name="stream">Stream where to store data.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when argument <b>stream</b> value is null.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 serveer returns error.</exception>
        public void HeaderToStream(Stream stream)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(stream == null){
                throw new ArgumentNullException("Argument 'stream' value can't be null.");
            }
            if(this.IsMarkedForDeletion){
                throw new InvalidOperationException("Can't access message, it's marked for deletion.");
            }

            MessageTopLinesToStream(stream,0);
        }

        #endregion

        #region method MessageToByte

        /// <summary>
        /// Gets message as byte[] data.
        /// </summary>
        /// <returns>Returns message as byte[] data.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when message is marked for deletion and this method is accessed.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 serveer returns error.</exception>
        public byte[] MessageToByte()
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(this.IsMarkedForDeletion){
                throw new InvalidOperationException("Can't access message, it's marked for deletion.");
            }

            MemoryStream retVal = new MemoryStream();
            MessageToStream(retVal);

            return retVal.ToArray();
        }

        #endregion

        #region method MessageToStream

        /// <summary>
        /// Stores message to specified stream.
        /// </summary>
        /// <param name="stream">Stream where to store message.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when argument <b>stream</b> value is null.</exception>
        /// <exception cref="InvalidOperationException">Is raised when message is marked for deletion and this method is accessed.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 serveer returns error.</exception>
        public void MessageToStream(Stream stream)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(stream == null){
                throw new ArgumentNullException("Argument 'stream' value can't be null.");
            }
            if(this.IsMarkedForDeletion){
                throw new InvalidOperationException("Can't access message, it's marked for deletion.");
            }

            using(MessageToStreamAsyncOP op = new MessageToStreamAsyncOP(stream)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<MessageToStreamAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.MessageToStreamAsync(op)){
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

        #region method MessageToStreamAsync

        #region class MessageToStreamAsyncOP

        /// <summary>
        /// This class represents <see cref="POP3_ClientMessage.MessageToStreamAsync"/> asynchronous operation.
        /// </summary>
        public class MessageToStreamAsyncOP : IDisposable,IAsyncOP
        {
            private object             m_pLock         = new object();
            private AsyncOP_State      m_State         = AsyncOP_State.WaitingForStart;
            private Exception          m_pException    = null;
            private POP3_ClientMessage m_pOwner        = null;
            private POP3_Client        m_pPop3Client   = null;
            private bool               m_RiseCompleted = false;
            private Stream             m_pStream       = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="stream">Stream where to store message.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
            public MessageToStreamAsyncOP(Stream stream)
            {
                if(stream == null){
                    throw new ArgumentNullException("stream");
                }

                m_pStream = stream;
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
                m_pOwner      = null;
                m_pPop3Client = null;
                m_pStream     = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner POP3 client message.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(POP3_ClientMessage owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pOwner      = owner;
                m_pPop3Client = owner.m_Pop3Client;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 1939 5. RETR
                         Arguments:
                             a message-number (required) which may NOT refer to a
                             message marked as deleted

                         Restrictions:
                             may only be given in the TRANSACTION state

                         Discussion:
                             If the POP3 server issues a positive response, then the
                             response given is multi-line.  After the initial +OK, the
                             POP3 server sends the message corresponding to the given
                             message-number, being careful to byte-stuff the termination
                             character (as with all multi-line responses).

                         Possible Responses:
                             +OK message follows
                             -ERR no such message

                         Examples:
                             C: RETR 1
                             S: +OK 120 octets
                             S: <the POP3 server sends the entire message here>
                             S: .
			        */

                    byte[] buffer = Encoding.UTF8.GetBytes("RETR " + owner.SequenceNumber.ToString() + "\r\n");

                    // Log
                    m_pPop3Client.LogAddWrite(buffer.Length,"RETR " + owner.SequenceNumber.ToString());

                    // Start command sending.
                    m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.RetrCommandSendingCompleted,null);
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

            #region method RetrCommandSendingCompleted

            /// <summary>
            /// Is called when RETR command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void RetrCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pPop3Client.TcpStream.EndWrite(ar);
                    
                    // Read POP3 server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        RetrReadResponseCompleted(op);
                    };
                    if(m_pPop3Client.TcpStream.ReadLine(op,true)){
                        RetrReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method RetrReadResponseCompleted
            
            /// <summary>
            /// Is called when POP3 server RETR response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void RetrReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
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
                            SmartStream.ReadPeriodTerminatedAsyncOP readMsgOP = new SmartStream.ReadPeriodTerminatedAsyncOP(m_pStream,long.MaxValue,SizeExceededAction.ThrowException);
                            readMsgOP.Completed += delegate(object sender,EventArgs<SmartStream.ReadPeriodTerminatedAsyncOP> e){
                                MessageReadingCompleted(readMsgOP);
                            };
                            if(m_pPop3Client.TcpStream.ReadPeriodTerminated(readMsgOP,true)){
                                MessageReadingCompleted(readMsgOP);
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

            #region method MessageReadingCompleted

            /// <summary>
            /// Is called when message reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void MessageReadingCompleted(SmartStream.ReadPeriodTerminatedAsyncOP op)
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
                        m_pPop3Client.LogAddRead(op.BytesStored,"Readed period-terminated message " + op.BytesStored.ToString() + " bytes.");

                        SetState(AsyncOP_State.Completed);
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
            public event EventHandler<EventArgs<MessageToStreamAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<MessageToStreamAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending RETR command to POP3 server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="MarkForDeletionAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool MessageToStreamAsync(MessageToStreamAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }          
            if(this.IsMarkedForDeletion){
                throw new InvalidOperationException("Message is already marked for deletion.");
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

        #region method MessageTopLinesToByte

        /// <summary>
        /// Gets message header + specified number lines of message body.
        /// </summary>
        /// <param name="lineCount">Number of lines to get from message body.</param>
        /// <returns>Returns message header + specified number lines of message body.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when <b>numberOfLines</b> is negative value.</exception>
        /// <exception cref="InvalidOperationException">Is raised when message is marked for deletion and this method is accessed.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 serveer returns error.</exception>
        public byte[] MessageTopLinesToByte(int lineCount)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(lineCount < 0){
                throw new ArgumentException("Argument 'lineCount' value must be >= 0.");
            }
            if(this.IsMarkedForDeletion){
                throw new InvalidOperationException("Can't access message, it's marked for deletion.");
            }

            MemoryStream retVal = new MemoryStream();
            MessageTopLinesToStream(retVal,lineCount);

            return retVal.ToArray();
        }

        #endregion

        #region method MessageTopLinesToStream

        /// <summary>
        /// Stores message header + specified number lines of message body to the specified stream.
        /// </summary>
        /// <param name="stream">Stream where to store data.</param>
        /// <param name="lineCount">Number of lines to get from message body.</param>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
        /// <exception cref="ArgumentNullException">Is raised when argument <b>stream</b> value is null.</exception>
        /// <exception cref="InvalidOperationException">Is raised when message is marked for deletion and this method is accessed.</exception>
        /// <exception cref="POP3_ClientException">Is raised when POP3 serveer returns error.</exception>
        public void MessageTopLinesToStream(Stream stream,int lineCount)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(stream == null){
                throw new ArgumentNullException("Argument 'stream' value can't be null.");
            }
            if(this.IsMarkedForDeletion){
                throw new InvalidOperationException("Can't access message, it's marked for deletion.");
            }

            using(MessageTopLinesToStreamAsyncOP op = new MessageTopLinesToStreamAsyncOP(stream,lineCount)){
                using(ManualResetEvent wait = new ManualResetEvent(false)){
                    op.CompletedAsync += delegate(object s1,EventArgs<MessageTopLinesToStreamAsyncOP> e1){
                        wait.Set();
                    };
                    if(!this.MessageTopLinesToStreamAsync(op)){
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

        #region method MessageTopLinesToStreamAsync

        #region class MessageTopLinesToStreamAsyncOP

        /// <summary>
        /// This class represents <see cref="POP3_ClientMessage.MessageTopLinesToStreamAsync"/> asynchronous operation.
        /// </summary>
        public class MessageTopLinesToStreamAsyncOP : IDisposable,IAsyncOP
        {
            private object             m_pLock         = new object();
            private AsyncOP_State      m_State         = AsyncOP_State.WaitingForStart;
            private Exception          m_pException    = null;
            private POP3_ClientMessage m_pOwner        = null;
            private POP3_Client        m_pPop3Client   = null;
            private bool               m_RiseCompleted = false;
            private Stream             m_pStream       = null;
            private int                m_LineCount     = 0;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="stream">Stream where to store message.</param>
            /// <param name="lineCount">Number of lines to get from body(after message header) of the message.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
            public MessageTopLinesToStreamAsyncOP(Stream stream,int lineCount)
            {
                if(stream == null){
                    throw new ArgumentNullException("stream");
                }
                if(lineCount < 0){
                    throw new ArgumentException("Argument 'lineCount' value must be >= 0.","lineCount");
                }

                m_pStream   = stream;
                m_LineCount = lineCount;
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
                m_pOwner      = null;
                m_pPop3Client = null;
                m_pStream     = null;

                this.CompletedAsync = null;
            }

            #endregion


            #region method Start

            /// <summary>
            /// Starts operation processing.
            /// </summary>
            /// <param name="owner">Owner POP3 client message.</param>
            /// <returns>Returns true if asynchronous operation in progress or false if operation completed synchronously.</returns>
            /// <exception cref="ArgumentNullException">Is raised when <b>owner</b> is null reference.</exception>
            internal bool Start(POP3_ClientMessage owner)
            {
                if(owner == null){
                    throw new ArgumentNullException("owner");
                }

                m_pOwner      = owner;
                m_pPop3Client = owner.m_Pop3Client;

                SetState(AsyncOP_State.Active);

                try{
                    /* RFC 1939 7. TOP
                         Arguments:
                             a message-number (required) which may NOT refer to to a
                             message marked as deleted, and a non-negative number
                             of lines (required)

                         Restrictions:
                             may only be given in the TRANSACTION state

                         Discussion:
                             If the POP3 server issues a positive response, then the
                             response given is multi-line.  After the initial +OK, the
                             POP3 server sends the headers of the message, the blank
                             line separating the headers from the body, and then the
                             number of lines of the indicated message's body, being
                             careful to byte-stuff the termination character (as with
                             all multi-line responses).

                             Note that if the number of lines requested by the POP3
                             client is greater than than the number of lines in the
                             body, then the POP3 server sends the entire message.

                         Possible Responses:
                             +OK top of message follows
                             -ERR no such message

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

                    byte[] buffer = Encoding.UTF8.GetBytes("TOP " + owner.SequenceNumber.ToString()+ " " + m_LineCount.ToString() + "\r\n");

                    // Log
                    m_pPop3Client.LogAddWrite(buffer.Length,"TOP " + owner.SequenceNumber.ToString()+ " " + m_LineCount.ToString());

                    // Start command sending.
                    m_pPop3Client.TcpStream.BeginWrite(buffer,0,buffer.Length,this.TopCommandSendingCompleted,null);
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

            #region method TopCommandSendingCompleted

            /// <summary>
            /// Is called when TOP command sending has finished.
            /// </summary>
            /// <param name="ar">Asynchronous result.</param>
            private void TopCommandSendingCompleted(IAsyncResult ar)
            {
                try{
                    m_pPop3Client.TcpStream.EndWrite(ar);
                    
                    // Read POP3 server response.
                    SmartStream.ReadLineAsyncOP op = new SmartStream.ReadLineAsyncOP(new byte[8000],SizeExceededAction.JunkAndThrowException);
                    op.Completed += delegate(object s,EventArgs<SmartStream.ReadLineAsyncOP> e){
                        TopReadResponseCompleted(op);
                    };
                    if(m_pPop3Client.TcpStream.ReadLine(op,true)){
                        TopReadResponseCompleted(op);
                    }
                }
                catch(Exception x){
                    m_pException = x;
                    m_pPop3Client.LogAddException("Exception: " + x.Message,x);
                    SetState(AsyncOP_State.Completed);
                }
            }

            #endregion

            #region method TopReadResponseCompleted
            
            /// <summary>
            /// Is called when POP3 server TOP response reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void TopReadResponseCompleted(SmartStream.ReadLineAsyncOP op)
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
                            SmartStream.ReadPeriodTerminatedAsyncOP readMsgOP = new SmartStream.ReadPeriodTerminatedAsyncOP(m_pStream,long.MaxValue,SizeExceededAction.ThrowException);
                            readMsgOP.Completed += delegate(object sender,EventArgs<SmartStream.ReadPeriodTerminatedAsyncOP> e){
                                MessageReadingCompleted(readMsgOP);
                            };
                            if(m_pPop3Client.TcpStream.ReadPeriodTerminated(readMsgOP,true)){
                                MessageReadingCompleted(readMsgOP);
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

            #region method MessageReadingCompleted

            /// <summary>
            /// Is called when message reading has completed.
            /// </summary>
            /// <param name="op">Asynchronous operation.</param>
            private void MessageReadingCompleted(SmartStream.ReadPeriodTerminatedAsyncOP op)
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
                        m_pPop3Client.LogAddRead(op.BytesStored,"Readed period-terminated message " + op.BytesStored.ToString() + " bytes.");

                        SetState(AsyncOP_State.Completed);
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
            public event EventHandler<EventArgs<MessageTopLinesToStreamAsyncOP>> CompletedAsync = null;

            #region method OnCompletedAsync

            /// <summary>
            /// Raises <b>CompletedAsync</b> event.
            /// </summary>
            private void OnCompletedAsync()
            {
                if(this.CompletedAsync != null){
                    this.CompletedAsync(this,new EventArgs<MessageTopLinesToStreamAsyncOP>(this));
                }
            }

            #endregion

            #endregion
        }

        #endregion

        /// <summary>
        /// Starts sending TOP command to POP3 server.
        /// </summary>
        /// <param name="op">Asynchronous operation.</param>
        /// <returns>Returns true if aynchronous operation is pending (The <see cref="MessageTopLinesToStreamAsyncOP.CompletedAsync"/> event is raised upon completion of the operation).
        /// Returns false if operation completed synchronously.</returns>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and and this method is accessed.</exception>
        /// <exception cref="InvalidOperationException">Is raised when POP3 client is not in valid state. For example 'not connected'.</exception>
        /// <exception cref="ArgumentNullException">Is raised when <b>op</b> is null reference.</exception>
        public bool MessageTopLinesToStreamAsync(MessageTopLinesToStreamAsyncOP op)
        {
            if(this.IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }          
            if(this.IsMarkedForDeletion){
                throw new InvalidOperationException("Message is already marked for deletion.");
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


        #region method Dispose

        /// <summary>
        /// Disposes message.
        /// </summary>
        internal void Dispose()
        {
            if(m_IsDisposed){
                return;
            }

            m_IsDisposed = true;
            m_Pop3Client = null;
        }

        #endregion

        #region method SetUID

        /// <summary>
        /// Sets message UID value.
        /// </summary>
        /// <param name="uid">UID value.</param>
        internal void SetUID(string uid)
        {
            m_UID = uid;
        }

        #endregion

        #region method SetMarkedForDeletion

        /// <summary>
        /// Sets IsMarkedForDeletion flag value.
        /// </summary>
        /// <param name="isMarkedForDeletion">New IsMarkedForDeletion value.</param>
        internal void SetMarkedForDeletion(bool isMarkedForDeletion)
        {
            m_IsMarkedForDeletion = isMarkedForDeletion;
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets if POP3 message is Disposed.
        /// </summary>
        public bool IsDisposed
        {
            get{ return m_IsDisposed; }
        }

        /// <summary>
        /// Gets message 1 based sequence number.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public int SequenceNumber
        {
            get{               
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_SequenceNumber; 
            }
        }

        /// <summary>
        /// Gets message UID. NOTE: Before accessing this property, check that server supports UIDL command.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        /// <exception cref="NotSupportedException">Is raised when POP3 server doesnt support UIDL command.</exception>
        public string UID
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(!m_Pop3Client.IsUidlSupported){
                    throw new NotSupportedException("POP3 server doesn't support UIDL command.");
                }

                return m_UID; 
            }
        }

        /// <summary>
        /// Gets message size in bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public int Size
        {
            get{ 
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_Size;
            }
        }

        /// <summary>
        /// Gets if message is marked for deletion.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public bool IsMarkedForDeletion
        {
            get{
                if(this.IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                
                return m_IsMarkedForDeletion; 
            }
        }

        #endregion

    }
}
