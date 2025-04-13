using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;
using System.Threading;

using LumiSoft.Net;

namespace LumiSoft.Net.UDP
{
    /// <summary>
    /// This class implements high performance UDP data receiver.
    /// </summary>
    /// <remarks>NOTE: High performance server applications should create multiple instances of this class per one socket.</remarks>
    public class UDP_DataReceiver : IDisposable
    {
        private bool                 m_IsDisposed  = false;
        private bool                 m_IsRunning   = false;
        private Socket               m_pSocket     = null;
        private byte[]               m_pBuffer     = null;
        private int                  m_BufferSize  = 1400;
        private SocketAsyncEventArgs m_pSocketArgs = null;
        private UDP_e_PacketReceived m_pEventArgs  = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="socket">UDP socket.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>socket</b> is null reference.</exception>
        public UDP_DataReceiver(Socket socket)
        {
            if(socket == null){
                throw new ArgumentNullException("socket");
            }

            m_pSocket = socket;
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

            m_pSocket = null;
            m_pBuffer = null;
            if(m_pSocketArgs != null){
                m_pSocketArgs.Dispose();
                m_pSocketArgs = null;
            }
            m_pEventArgs = null;

            this.PacketReceived = null;
            this.Error = null;            
        }

        #endregion


        #region method Start

        /// <summary>
        /// Starts receiving data.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this calss is disposed and this method is accessed.</exception>
        public void Start()
        {
            if(m_IsDisposed){
                throw new ObjectDisposedException(this.GetType().Name);
            }
            if(m_IsRunning){
                return;
            }
            m_IsRunning = true;
            
            bool isIoCompletionSupported = Net_Utils.IsSocketAsyncSupported();

            m_pEventArgs = new UDP_e_PacketReceived();
            m_pBuffer = new byte[m_BufferSize];
            
            if(isIoCompletionSupported){
                m_pSocketArgs = new SocketAsyncEventArgs();
                m_pSocketArgs.SetBuffer(m_pBuffer,0,m_BufferSize);
                m_pSocketArgs.RemoteEndPoint = new IPEndPoint(m_pSocket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any,0);
                m_pSocketArgs.Completed += delegate(object s1,SocketAsyncEventArgs e1){
                    if(m_IsDisposed){
                        return;
                    }

                    try{
                        if(m_pSocketArgs.SocketError == SocketError.Success){
                            OnPacketReceived(m_pBuffer,m_pSocketArgs.BytesTransferred,(IPEndPoint)m_pSocketArgs.RemoteEndPoint);                            
                        }
                        else{
                            OnError(new Exception("Socket error '" + m_pSocketArgs.SocketError + "'."));
                        }

                        IOCompletionReceive();
                    }
                    catch(Exception x){
                        OnError(x);
                    }
                };
            }

            // Move processing to thread pool.
            ThreadPool.QueueUserWorkItem(delegate(object state){
                if(m_IsDisposed){
                    return;
                }

                try{ 
                    if(isIoCompletionSupported){                        
                        IOCompletionReceive();
                    }
                    else{
                        EndPoint rtpRemoteEP = new IPEndPoint(m_pSocket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any,0);
                        m_pSocket.BeginReceiveFrom(
                            m_pBuffer,
                            0,
                            m_BufferSize,
                            SocketFlags.None,
                            ref rtpRemoteEP,
                            new AsyncCallback(this.AsyncSocketReceive),
                            null
                        );
                    }
                }
                catch(Exception x){
                    OnError(x);
                }
            });
        }

        #endregion


        #region method IOCompletionReceive

        /// <summary>
        /// Receives synchornously(if packet(s) available now) or starts waiting UDP packet asynchronously if no packets at moment.
        /// </summary>
        private void IOCompletionReceive()
        {
            try{ 
                // Use active worker thread as long as ReceiveFromAsync completes synchronously.
                // (With this approach we don't have thread context switches while ReceiveFromAsync completes synchronously)
                while(!m_IsDisposed && !m_pSocket.ReceiveFromAsync(m_pSocketArgs)){
                    if(m_pSocketArgs.SocketError == SocketError.Success){
                        try{
                            OnPacketReceived(m_pBuffer,m_pSocketArgs.BytesTransferred,(IPEndPoint)m_pSocketArgs.RemoteEndPoint);
                        }
                        catch(Exception x){
                            OnError(x);
                        }
                    }
                    else{
                        OnError(new Exception("Socket error '" + m_pSocketArgs.SocketError + "'."));
                    }

                    // Reset remote end point.
                    m_pSocketArgs.RemoteEndPoint = new IPEndPoint(m_pSocket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any,0);
                }
            }
            catch(Exception x){
                OnError(x);
            }
        }

        #endregion

        #region method AsyncSocketReceive

        /// <summary>
        /// Is called BeginReceiveFrom has completed.
        /// </summary>
        /// <param name="ar">The result of the asynchronous operation.</param>
        private void AsyncSocketReceive(IAsyncResult ar)
        {
            if(m_IsDisposed){
                return;
            }

            try{
                EndPoint remoteEP = new IPEndPoint(IPAddress.Any,0);
                int count = m_pSocket.EndReceiveFrom(ar,ref remoteEP);

                OnPacketReceived(m_pBuffer,count,(IPEndPoint)remoteEP);
            }
            catch(Exception x){
                OnError(x);
            }

            try{
                // Start receiving new packet.
                EndPoint rtpRemoteEP = new IPEndPoint(m_pSocket.AddressFamily == AddressFamily.InterNetwork ? IPAddress.Any : IPAddress.IPv6Any,0);
                m_pSocket.BeginReceiveFrom(
                    m_pBuffer,
                    0,
                    m_BufferSize,
                    SocketFlags.None,
                    ref rtpRemoteEP,
                    new AsyncCallback(this.AsyncSocketReceive),
                    null
                );
            }
            catch(Exception x){
                 OnError(x);
            }
        }

        #endregion


        #region Events implementation

        /// <summary>
        /// Is raised when when new UDP packet is available.
        /// </summary>
        public event EventHandler<UDP_e_PacketReceived> PacketReceived = null;

        #region method OnPacketReceived

        /// <summary>
        /// Raises <b>PacketReceived</b> event.
        /// </summary>
        /// <param name="buffer">Data buffer.</param>
        /// <param name="count">Number of bytes stored in <b>buffer</b></param>
        /// <param name="remoteEP">Remote IP end point from where data was received.</param>
        private void OnPacketReceived(byte[] buffer,int count,IPEndPoint remoteEP)
        {
            if(this.PacketReceived != null){
                m_pEventArgs.Reuse(m_pSocket,buffer,count,remoteEP);

                this.PacketReceived(this,m_pEventArgs);
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
            if(m_IsDisposed){
                return;
            }

            if(this.Error != null){
                this.Error(this,new ExceptionEventArgs(x));
            }
        }

        #endregion

        #endregion

    }
}
