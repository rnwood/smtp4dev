using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This class provides data for RTP packet related events/methods.
    /// </summary>
    public class RTP_PacketEventArgs : EventArgs
    {
        private RTP_Packet m_pPacket = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="packet">RTP packet.</param>
        public RTP_PacketEventArgs(RTP_Packet packet)
        {
            if(packet == null){
                throw new ArgumentNullException("packet");
            }

            m_pPacket = packet;
        }


        #region Properties implementation

        /// <summary>
        /// Gets RTP packet.
        /// </summary>
        public RTP_Packet Packet
        {
            get{ return m_pPacket; }
        }

        #endregion

    }
}
