using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This class represents BYE: Goodbye RTCP Packet.
    /// </summary>
    public class RTCP_Packet_BYE : RTCP_Packet
    {
        private int    m_Version       = 2;
        private uint[] m_Sources       = null;
        private string m_LeavingReason = "";

        /// <summary>
        /// Default constructor.
        /// </summary>
        internal RTCP_Packet_BYE()
        {
        }

                
        #region method ParseInternal

        /// <summary>
        /// Parses BYE packet from raw byte[] bye packet.
        /// </summary>
        /// <param name="buffer">Buffer what contains BYE packet.</param>
        /// <param name="offset">Offset in buffer.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        protected override void ParseInternal(byte[] buffer,ref int offset)
        {
            /* RFC 3550.6.6 BYE: Goodbye RTCP Packet.
                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                   |V=2|P|    SC   |   PT=BYE=203  |             length            |
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                   |                           SSRC/CSRC                           |
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                   :                              ...                              :
                   +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
             (opt) |     length    |               reason for leaving            ...
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            */

            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0){
                throw new ArgumentException("Argument 'offset' value must be >= 0.");
            }

                 m_Version   = buffer[offset] >> 6;
            bool isPadded    = Convert.ToBoolean((buffer[offset] >> 5) & 0x1);
            int  sourceCount = buffer[offset++] & 0x1F;
            int  type        = buffer[offset++];
            int  length      = buffer[offset++] << 8 | buffer[offset++];
            if(isPadded){
                this.PaddBytesCount = buffer[offset + length];
            }

            m_Sources = new uint[sourceCount];
            for(int i=0;i<sourceCount;i++){
                m_Sources[i] = (uint)(buffer[offset++] << 24 | buffer[offset++] << 16 | buffer[offset++] << 8 | buffer[offset++]);
            }

            // See if we have optional reason text.
            if(length > m_Sources.Length * 4){
                int reasonLength = buffer[offset++];
                m_LeavingReason = Encoding.UTF8.GetString(buffer,offset,reasonLength);
                offset += reasonLength;
            }
        }

        #endregion

        #region mehtod ToByte

        /// <summary>
        /// Stores BYE packet to the specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer where to store BYE packet.</param>
        /// <param name="offset">Offset in buffer.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public override void ToByte(byte[] buffer,ref int offset)
        {
            /* RFC 3550.6.6 BYE: Goodbye RTCP Packet.
                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                   |V=2|P|    SC   |   PT=BYE=203  |             length            |
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                   |                           SSRC/CSRC                           |
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
                   :                              ...                              :
                   +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
             (opt) |     length    |               reason for leaving            ...
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            */

            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0){
                throw new ArgumentException("Argument 'offset' value must be >= 0.");
            }

            // Calculate packet body size in bytes.
            int length = 0;
            length += m_Sources.Length * 4;
            if(!string.IsNullOrEmpty(m_LeavingReason)){
                length++;
                length += Encoding.UTF8.GetByteCount(m_LeavingReason);
            }

            // V=2 P SC
            buffer[offset++] = (byte)(2 << 6 | 0 << 5 | m_Sources.Length & 0x1F);
            // PT=BYE=203
            buffer[offset++] = 203;
            // length
            buffer[offset++] = (byte)((length >> 8) & 0xFF);
            buffer[offset++] = (byte)(length        & 0xFF);
            // SSRC/CSRC's
            foreach(int source in m_Sources){
                buffer[offset++] = (byte)((source & 0xFF000000) >> 24);
                buffer[offset++] = (byte)((source & 0x00FF0000) >> 16);
                buffer[offset++] = (byte)((source & 0x0000FF00) >> 8);
                buffer[offset++] = (byte)((source & 0x000000FF));
            }
            // reason for leaving
            if(!string.IsNullOrEmpty(m_LeavingReason)){
                byte[] reasonBytes = Encoding.UTF8.GetBytes(m_LeavingReason);
                buffer[offset++] = (byte)reasonBytes.Length;
                Array.Copy(reasonBytes,0,buffer,offset,reasonBytes.Length);
                offset += reasonBytes.Length;
            }
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets RTCP version.
        /// </summary>
        public override int Version
        {
            get{ return m_Version; }
        }

        /// <summary>
        /// Gets RTCP packet type.
        /// </summary>
        public override int Type
        {
            get{ return RTCP_PacketType.BYE; }
        }

        /// <summary>
        /// Gets or sets SSRC/CSRC identifiers included in this BYE packet. 
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public uint[] Sources
        {
            get{ return m_Sources; }

            set{
                if(value.Length > 31){
                    throw new ArgumentException("Property 'Sources' can accomodate only 31 entries.");
                }

                m_Sources = value; 
            }
        }

        /// <summary>
        /// Gets leaving reason.
        /// </summary>
        public string LeavingReason
        {
            get{ return m_LeavingReason; }

            set{ m_LeavingReason =value; }
        }

        /// <summary>
        /// Gets number of bytes needed for this packet.
        /// </summary>
        public override int Size
        {
            get{
                int size = 4;
                if(m_Sources != null){
                    size += 4 * m_Sources.Length;
                }
                if(!string.IsNullOrEmpty(m_LeavingReason)){
                    size++;
                    size += Encoding.UTF8.GetByteCount(m_LeavingReason);
                }

                return size; 
            }
        }

        #endregion

    }
}
