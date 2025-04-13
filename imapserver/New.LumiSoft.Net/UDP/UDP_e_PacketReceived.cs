using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Net.Sockets;

namespace LumiSoft.Net.UDP
{
    /// <summary>
    /// This class provides data for the <see cref="UDP_DataReceiver.PacketReceived"/> event.
    /// </summary>
    public class UDP_e_PacketReceived : EventArgs
    {
        private Socket     m_pSocket   = null;
        private byte[]     m_pBuffer   = null;
        private int        m_Count     = 0;
        private IPEndPoint m_pRemoteEP = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal UDP_e_PacketReceived()
        {
        }


        #region method Reuse

        /// <summary>
        /// Reuses this class.
        /// </summary>
        /// <param name="socket">Socket which received data.</param>
        /// <param name="buffer">Data buffer.</param>
        /// <param name="count">Number of bytes stored in <b>buffer</b></param>
        /// <param name="remoteEP">Remote IP end point from where data was received.</param>
        internal void Reuse(Socket socket,byte[] buffer,int count,IPEndPoint remoteEP)
        {        
            m_pSocket   = socket;
            m_pBuffer   = buffer;
            m_Count     = count;
            m_pRemoteEP = remoteEP;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets socket which received data.
        /// </summary>
        public Socket Socket
        {
            get{ return m_pSocket; }
        }

        /// <summary>
        /// Gets data buffer.
        /// </summary>
        public byte[] Buffer
        {
            get{ return m_pBuffer; }
        }

        /// <summary>
        /// Gets number of bytes stored to <b>Buffer</b>.
        /// </summary>
        public int Count
        {
            get{ return m_Count; }
        }

        /// <summary>
        /// Gets remote host from where data was received.
        /// </summary>
        public IPEndPoint RemoteEP
        {
            get{ return m_pRemoteEP; }
        }

        #endregion

    }
}
