using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// A data packet consisting of the fixed RTP header, a possibly empty list of contributing 
    /// sources (see below), and the payload data. Some underlying protocols may require an 
    /// encapsulation of the RTP packet to be defined. Typically one packet of the underlying 
    /// protocol contains a single RTP packet, but several RTP packets MAY be contained if 
    /// permitted by the encapsulation method (see Section 11).
    /// </summary>
    public class RTP_Packet
    {
        private int    m_Version        = 2;
        private bool   m_IsMarker       = false;
        private int    m_PayloadType    = 0;
        private ushort m_SequenceNumber = 0;
        private uint   m_Timestamp      = 0;
        private uint   m_SSRC           = 0;
        private uint[] m_CSRC           = null;
        private byte[] m_Data           = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        public RTP_Packet()
        {
        }


        #region static method Parse

        /// <summary>
        /// Parses RTP packet.
        /// </summary>
        /// <param name="buffer">Buffer containing RTP packet.</param>
        /// <param name="size">Number of bytes used in buffer.</param>
        /// <returns>Returns parsed RTP packet.</returns>
        public static RTP_Packet Parse(byte[] buffer,int size)
        {
            RTP_Packet packet = new RTP_Packet();
            packet.ParseInternal(buffer,size);

            return packet;
        }

        #endregion

        #region method Validate

        /// <summary>
        /// Validates RTP packet.
        /// </summary>
        public void Validate()
        {
            // TODO: Validate RTP apcket
        }

        #endregion

        #region method ToByte

        /// <summary>
        /// Stores this packet to the specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer where to store packet.</param>
        /// <param name="offset">Offset in buffer.</param>
        public void ToByte(byte[] buffer,ref int offset)
        {
            /* RFC 3550.5.1 RTP Fixed Header Fields.
             
                The RTP header has the following format:

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |V=2|P|X|  CC   |M|     PT      |       sequence number         |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |                           timestamp                           |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |           synchronization source (SSRC) identifier            |
               +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
               |            contributing source (CSRC) identifiers             |
               |                             ....                              |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             
             
               5.3. Available if X bit filled.
                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |      defined by profile       |           length              |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |                        header extension                       |
               |                             ....                              |

            */

            int cc = 0;
            if(m_CSRC != null){
                cc = m_CSRC.Length;
            }

            // V P X CC
            buffer[offset++] = (byte)(m_Version << 6 | 0 << 5 | cc & 0xF);
            // M PT
            buffer[offset++] = (byte)(Convert.ToInt32(m_IsMarker) << 7 | m_PayloadType & 0x7F);
            // sequence number
            buffer[offset++] = (byte)(m_SequenceNumber >> 8);
            buffer[offset++] = (byte)(m_SequenceNumber & 0xFF);
            // timestamp
            buffer[offset++] = (byte)((m_Timestamp >> 24) & 0xFF);
            buffer[offset++] = (byte)((m_Timestamp >> 16) & 0xFF);
            buffer[offset++] = (byte)((m_Timestamp >>  8) & 0xFF);
            buffer[offset++] = (byte)(m_Timestamp & 0xFF);
            // SSRC
            buffer[offset++] = (byte)((m_SSRC >> 24) & 0xFF);
            buffer[offset++] = (byte)((m_SSRC >> 16) & 0xFF);
            buffer[offset++] = (byte)((m_SSRC >>  8) & 0xFF);
            buffer[offset++] = (byte)(m_SSRC & 0xFF);
            // CSRCs
            if(m_CSRC != null){
                foreach(int csrc in m_CSRC){
                    buffer[offset++] = (byte)((csrc >> 24) & 0xFF);
                    buffer[offset++] = (byte)((csrc >> 16) & 0xFF);
                    buffer[offset++] = (byte)((csrc >>  8) & 0xFF);
                    buffer[offset++] = (byte)(csrc & 0xFF);
                }
            }
            // X
            Array.Copy(m_Data,0,buffer,offset,m_Data.Length);
            offset += m_Data.Length;
        }

        #endregion

        #region override method ToString

        /// <summary>
        /// Returns this packet info as string.
        /// </summary>
        /// <returns>Returns packet info.</returns>
        public override string ToString()
        {
            StringBuilder retVal = new StringBuilder();
            retVal.Append("----- RTP Packet\r\n");
            retVal.Append("Version: " + m_Version.ToString() + "\r\n");
            retVal.Append("IsMaker: " + m_IsMarker.ToString() + "\r\n");
            retVal.Append("PayloadType: " + m_PayloadType.ToString() + "\r\n");
            retVal.Append("SeqNo: " + m_SequenceNumber.ToString() + "\r\n");
            retVal.Append("Timestamp: " + m_Timestamp.ToString() + "\r\n");
            retVal.Append("SSRC: " + m_SSRC.ToString() + "\r\n");
            retVal.Append("Data: " + m_Data.Length + " bytes.\r\n");

            return retVal.ToString();
        }

        #endregion


        #region method ParseInternal

        /// <summary>
        /// Parses RTP packet from the specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer containing RTP packet.</param>
        /// <param name="size">Number of bytes used in buffer.</param>
        private void ParseInternal(byte[] buffer,int size)
        {
            /* RFC 3550.5.1 RTP Fixed Header Fields.
             
                The RTP header has the following format:

                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |V=2|P|X|  CC   |M|     PT      |       sequence number         |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |                           timestamp                           |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |           synchronization source (SSRC) identifier            |
               +=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+=+
               |            contributing source (CSRC) identifiers             |
               |                             ....                              |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
             
             
               5.3. Available if X bit filled.
                0                   1                   2                   3
                0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1 2 3 4 5 6 7 8 9 0 1
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |      defined by profile       |           length              |
               +-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+-+
               |                        header extension                       |
               |                             ....                              |

            */

            int offset = 0;

            // V
            m_Version = buffer[offset] >> 6;
            // P
            bool isPadded  = Convert.ToBoolean((buffer[offset] >> 5) & 0x1);
            // X
            bool hasExtention = Convert.ToBoolean((buffer[offset] >> 4) & 0x1);
            // CC
            int csrcCount = buffer[offset++] & 0xF;
            // M
            m_IsMarker = Convert.ToBoolean(buffer[offset] >> 7);
            // PT
            m_PayloadType = buffer[offset++] & 0x7F;
            // sequence number
            m_SequenceNumber = (ushort)(buffer[offset++] << 8 | buffer[offset++]);
            // timestamp
            m_Timestamp = (uint)(buffer[offset++] << 24 | buffer[offset++] << 16 | buffer[offset++] << 8 | buffer[offset++]);
            // SSRC
            m_SSRC = (uint)(buffer[offset++] << 24 | buffer[offset++] << 16 | buffer[offset++] << 8 | buffer[offset++]);
            // CSRC
            m_CSRC = new uint[csrcCount];
            for(int i=0;i<csrcCount;i++){
                m_CSRC[i] = (uint)(buffer[offset++] << 24 | buffer[offset++] << 16 | buffer[offset++] << 8 | buffer[offset++]);
            }
            // X
            if(hasExtention){
                // Skip extention
                offset++;
                offset += buffer[offset];
            }

            // TODO: Padding

            // Data
            m_Data = new byte[size - offset];
            Array.Copy(buffer,offset,m_Data,0,m_Data.Length);
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets RTP version.
        /// </summary>
        public int Version
        {
            get{ return m_Version; }
        }

        /// <summary>
        /// Gets if packet is padded to some bytes boundary.
        /// </summary>
        public bool IsPadded
        {
            get{ return false; }
        }

        /// <summary>
        /// Gets marker bit. The usage of this bit depends on payload type.
        /// </summary>
        public bool IsMarker
        {
            get{ return m_IsMarker; }

            set{ m_IsMarker = value; }
        }

        /// <summary>
        /// Gets payload type.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public int PayloadType
        {
            get{ return m_PayloadType; }

            set{
                if(value < 0 || value > 128){
                    throw new ArgumentException("Payload value must be >= 0 and <= 128.");
                }

                m_PayloadType = value; 
            }
        }

        /// <summary>
        /// Gets or sets RTP packet sequence number.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public ushort SeqNo
        {
            get{ return m_SequenceNumber; }

            set{ m_SequenceNumber = value; }
        }
        
        /// <summary>
        /// Gets sets packet timestamp. 
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public uint Timestamp
        {
            get{ return m_Timestamp; }

            set{
                if(value < 1){
                    throw new ArgumentException("Timestamp value must be >= 1.");
                }

                m_Timestamp = value;
            }
        }

        /// <summary>
        /// Gets or sets synchronization source ID.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public uint SSRC
        {
            get{ return m_SSRC; }

            set{
                if(value < 1){
                    throw new ArgumentException("SSRC value must be >= 1.");
                }

                m_SSRC = value; 
            }
        }

        /// <summary>
        /// Gets or sets the contributing sources for the payload contained in this packet.
        /// Value null means none.
        /// </summary>
        public uint[] CSRC
        {
            get{ return m_CSRC; }

            set{ m_CSRC = value; }
        }

        /// <summary>
        /// Gets SSRC + CSRCs as joined array.
        /// </summary>
        public uint[] Sources
        {
            get{
                uint[] retVal = new uint[1];
                if(m_CSRC != null){
                    retVal = new uint[1 + m_CSRC.Length];
                }
                retVal[0] = m_SSRC;
                Array.Copy(m_CSRC,retVal,m_CSRC.Length);

                return retVal; 
            }
        }

        /// <summary>
        /// Gets or sets RTP data. Data must be encoded with PayloadType encoding.
        /// </summary>
        /// <exception cref="ArgumentNullException">Is raised when null value is passed.</exception>
        public byte[] Data
        {
            get{ return m_Data; }

            set{
                if(value == null){
                    throw new ArgumentNullException("Data");
                }

                m_Data = value; 
            }
        }

        #endregion

    }
}
