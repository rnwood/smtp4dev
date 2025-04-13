using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This is base class for RTCP packets.
    /// </summary>
    public abstract class RTCP_Packet
    {
        private int m_PaddBytesCount = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public RTCP_Packet()
        {
        }


        #region static method Parse

        /// <summary>
        /// Parses 1 RTCP packet from the specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer which contains RTCP packet.</param>
        /// <param name="offset">Offset in buffer.</param>
        /// <returns>Returns parsed RTCP packet.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public static RTCP_Packet Parse(byte[] buffer,ref int offset)
        {
            return Parse(buffer, ref offset,false);
        }

        /// <summary>
        /// Parses 1 RTCP packet from the specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer which contains RTCP packet.</param>
        /// <param name="offset">Offset in buffer.</param>
        /// <param name="noException">If true and parsing failed, no exception is raised.</param>
        /// <returns>Returns parsed RTCP packet or null if parsing is failed and <b>noException=true</b>.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public static RTCP_Packet Parse(byte[] buffer,ref int offset,bool noException)
        {
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0){
                throw new ArgumentException("Argument 'offset' value must be >= 0.");
            }

            /* RFC 3550 RTCP header.
                    0                   1                   2                   3
                    0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            header |V=2|P|    XX   |   type        |             length            |
                   +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
            */

            int type = buffer[offset + 1];

            // SR
            if(type == RTCP_PacketType.SR){
                RTCP_Packet_SR packet = new RTCP_Packet_SR();
                packet.ParseInternal(buffer,ref offset);

                return packet;
            }
            // RR
            else if(type == RTCP_PacketType.RR){
                RTCP_Packet_RR packet = new RTCP_Packet_RR();
                packet.ParseInternal(buffer,ref offset);

                return packet;
            }
            // SDES
            else if(type == RTCP_PacketType.SDES){
                RTCP_Packet_SDES packet = new RTCP_Packet_SDES();
                packet.ParseInternal(buffer,ref offset);

                return packet;
            }
            // BYE
            else if(type == RTCP_PacketType.BYE){
                RTCP_Packet_BYE packet = new RTCP_Packet_BYE();
                packet.ParseInternal(buffer,ref offset);

                return packet;
            }
            // APP
            else if(type == RTCP_PacketType.APP){
                RTCP_Packet_APP packet = new RTCP_Packet_APP();
                packet.ParseInternal(buffer,ref offset);

                return packet;
            }
            else{
                // We need to move offset.
                offset += 2;
                int length = buffer[offset++] << 8 | buffer[offset++];
                offset += length;

                if(noException){
                    return null;
                }
                else{
                    throw new ArgumentException("Unknown RTCP packet type '" + type + "'.");
                }
            }
        }

        #endregion

        #region method ToByte

        /// <summary>
        /// Stores this packet to the specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer where to store packet.</param>
        /// <param name="offset">Offset in buffer.</param>
        public abstract void ToByte(byte[] buffer,ref int offset);

        #endregion


        #region method ParseInternal

        /// <summary>
        /// Parses RTCP packet from the specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer which contains packet.</param>
        /// <param name="offset">Offset in buffer.</param>
        protected abstract void ParseInternal(byte[] buffer,ref int offset);

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets RTCP version.
        /// </summary>
        public abstract int Version
        {
            get;
        }

        /// <summary>
        /// Gets if packet is padded to some bytes boundary.
        /// </summary>
        public bool IsPadded
        {
            get{ 
                if(m_PaddBytesCount > 0){
                    return true;
                }
                else{
                    return false;
                }
            }
        }

        /// <summary>
        /// Gets RTCP packet type.
        /// </summary>
        public abstract int Type
        {
            get;
        }

        /// <summary>
        /// Gets or sets number empty bytes to add at the end of packet.
        /// </summary>
        public int PaddBytesCount
        {
            get{ return m_PaddBytesCount; }

            set{
                if(value < 0){
                    throw new ArgumentException("Property 'PaddBytesCount' value must be >= 0.");
                }

                m_PaddBytesCount = value;
            }            
        }

        /// <summary>
        /// Gets number of bytes needed for this packet.
        /// </summary>
        public abstract int Size
        {
            get;
        }

        #endregion

    }
}
