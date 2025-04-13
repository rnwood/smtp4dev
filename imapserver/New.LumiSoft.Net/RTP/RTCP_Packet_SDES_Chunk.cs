using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This class implements RTCP SDES packet one "chunk". 
    /// </summary>
    public class RTCP_Packet_SDES_Chunk
    {
        private uint   m_Source   = 0;
        private string m_CName    = null;
        private string m_Name     = null;
        private string m_Email    = null;
        private string m_Phone    = null;
        private string m_Location = null;
        private string m_Tool     = null;
        private string m_Note     = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="source">SSRC or CSRC identifier.</param>
        /// <param name="cname">Canonical End-Point Identifier.</param>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public RTCP_Packet_SDES_Chunk(uint source,string cname)
        {
            if(source == 0){
                throw new ArgumentException("Argument 'source' value must be > 0.");
            }
            if(string.IsNullOrEmpty(cname)){
                throw new ArgumentException("Argument 'cname' value may not be null or empty.");
            }

            m_Source = source;
            m_CName  = cname;
        }

        /// <summary>
        /// Parser constructor.
        /// </summary>
        internal RTCP_Packet_SDES_Chunk()
        {
        }


        #region method Parse

        /// <summary>
        /// Parses SDES chunk from the specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer which contains SDES chunk.</param>
        /// <param name="offset">Offset in buffer.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public void Parse(byte[] buffer,ref int offset)
        {
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0){
                throw new ArgumentException("Argument 'offset' value must be >= 0.");
            }

            /* RFC 3550 6.5.
                The list of items in each chunk
                MUST be terminated by one or more null octets, the first of which is
                interpreted as an item type of zero to denote the end of the list.
                No length octet follows the null item type octet, but additional null
                octets MUST be included if needed to pad until the next 32-bit
                boundary.  Note that this padding is separate from that indicated by
                the P bit in the RTCP header.  A chunk with zero items (four null
                octets) is valid but useless.
            */

            int startOffset = offset;
                        
            // Read SSRC/CSRC
            m_Source = (uint)(buffer[offset++] << 24 | buffer[offset++] << 16 | buffer[offset++] << 8 | buffer[offset++]);

            // Read SDES items while reach end of buffer or we get chunk terminator(\0).
            while(offset < buffer.Length && buffer[offset] != 0){
                int type   = buffer[offset++];
                int length = buffer[offset++];

                // CNAME
                if(type == 1){
                    m_CName = Encoding.UTF8.GetString(buffer,offset,length);
                }
                // NAME
                else if(type == 2){
                    m_Name = Encoding.UTF8.GetString(buffer,offset,length);
                }
                // EMAIL
                else if(type == 3){
                    m_Email = Encoding.UTF8.GetString(buffer,offset,length);
                }
                // PHONE
                else if(type == 4){
                    m_Phone = Encoding.UTF8.GetString(buffer,offset,length);
                }
                // LOC
                else if(type == 5){
                    m_Location = Encoding.UTF8.GetString(buffer,offset,length);
                }
                // TOOL
                else if(type == 6){
                    m_Tool = Encoding.UTF8.GetString(buffer,offset,length);
                }
                // NOTE
                else if(type == 7){
                    m_Note = Encoding.UTF8.GetString(buffer,offset,length);
                }
                // PRIV
                else if(type == 8){
                    // TODO:
                }
                offset += length;
            }

            // Terminator 0.
            offset++;
           
            // Pad to 32-bit boundary, if it isn't. See not above.
            offset += (offset - startOffset) % 4;
        }

        #endregion

        #region method ToByte

        /// <summary>
        /// Stores SDES junk to the specified buffer.
        /// </summary>
        /// <param name="buffer">Buffer where to store data.</param>
        /// <param name="offset">Offset in buffer.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public void ToByte(byte[] buffer,ref int offset)
        {
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0){
                throw new ArgumentException("Argument 'offset' value must be >= 0.");
            }

            int startOffset = offset;

            // SSRC/SDES
            buffer[offset++] = (byte)((m_Source >> 24) & 0xFF);
            buffer[offset++] = (byte)((m_Source >> 16) & 0xFF);
            buffer[offset++] = (byte)((m_Source >>  8) & 0xFF);
            buffer[offset++] = (byte)((m_Source)       & 0xFF);

            //--- SDES items -----------------------------------
            if(!string.IsNullOrEmpty(m_CName)){
                byte[] b = Encoding.UTF8.GetBytes(m_CName);
                buffer[offset++] = 1;
                buffer[offset++] = (byte)b.Length;
                Array.Copy(b,0,buffer,offset,b.Length);
                offset += b.Length;
            }
            if(!string.IsNullOrEmpty(m_Name)){
                byte[] b = Encoding.UTF8.GetBytes(m_Name);
                buffer[offset++] = 2;
                buffer[offset++] = (byte)b.Length;
                Array.Copy(b,0,buffer,offset,b.Length);
                offset += b.Length;
            }
            if(!string.IsNullOrEmpty(m_Email)){
                byte[] b = Encoding.UTF8.GetBytes(m_Email);
                buffer[offset++] = 3;
                buffer[offset++] = (byte)b.Length;
                Array.Copy(b,0,buffer,offset,b.Length);
                offset += b.Length;
            }
            if(!string.IsNullOrEmpty(m_Phone)){
                byte[] b = Encoding.UTF8.GetBytes(m_Phone);
                buffer[offset++] = 4;
                buffer[offset++] = (byte)b.Length;
                Array.Copy(b,0,buffer,offset,b.Length);
                offset += b.Length;
            }
            if(!string.IsNullOrEmpty(m_Location)){
                byte[] b = Encoding.UTF8.GetBytes(m_Location);
                buffer[offset++] = 5;
                buffer[offset++] = (byte)b.Length;
                Array.Copy(b,0,buffer,offset,b.Length);
                offset += b.Length;
            }
            if(!string.IsNullOrEmpty(m_Tool)){
                byte[] b = Encoding.UTF8.GetBytes(m_Tool);
                buffer[offset++] = 6;
                buffer[offset++] = (byte)b.Length;
                Array.Copy(b,0,buffer,offset,b.Length);
                offset += b.Length;
            }
            if(!string.IsNullOrEmpty(m_Note)){
                byte[] b = Encoding.UTF8.GetBytes(m_Note);
                buffer[offset++] = 7;
                buffer[offset++] = (byte)b.Length;
                Array.Copy(b,0,buffer,offset,b.Length);
                offset += b.Length;
            }
            // Terminate chunk
            buffer[offset++] = 0;

            // Pad to 4(32-bit) bytes boundary.
            while((offset - startOffset) % 4 > 0){
                buffer[offset++] = 0;
            }            
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets SSRC or CSRC identifier.
        /// </summary>
        public uint Source
        {
            get{ return m_Source; }
        }

        /// <summary>
        /// Gets Canonical End-Point Identifier.
        /// </summary>
        public string CName
        {
            get{ return m_CName; }
        }

        /// <summary>
        /// Gets or sets the real name, eg. "John Doe". Value null means not specified.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public string Name
        {
            get{ return m_Name; }

            set{
                if(Encoding.UTF8.GetByteCount(value) > 255){
                    throw new ArgumentException("Property 'Name' value must be <= 255 bytes.");
                }

                m_Name = value;
            }
        }

        /// <summary>
        /// Gets or sets email address. For example "John.Doe@example.com". Value null means not specified.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public string Email
        {
            get{ return m_Email; }

            set{
                if(Encoding.UTF8.GetByteCount(value) > 255){
                    throw new ArgumentException("Property 'Email' value must be <= 255 bytes.");
                }

                m_Email = value;
            }
        }

        /// <summary>
        /// Gets or sets phone number. For example "+1 908 555 1212". Value null means not specified.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public string Phone
        {
            get{ return m_Phone; }

            set{
                if(Encoding.UTF8.GetByteCount(value) > 255){
                    throw new ArgumentException("Property 'Phone' value must be <= 255 bytes.");
                }

                m_Phone = value;
            }
        }

        /// <summary>
        /// Gets or sets location string. It may be geographic address or for example chat room name.
        /// Value null means not specified.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public string Location
        {
            get{ return m_Location; }

            set{
                if(Encoding.UTF8.GetByteCount(value) > 255){
                    throw new ArgumentException("Property 'Location' value must be <= 255 bytes.");
                }

                m_Location = value;
            }
        }

        /// <summary>
        /// Gets or sets streaming application name/version.
        /// Value null means not specified.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public string Tool
        {
            get{ return m_Tool; }

            set{
                if(Encoding.UTF8.GetByteCount(value) > 255){
                    throw new ArgumentException("Property 'Tool' value must be <= 255 bytes.");
                }

                m_Tool = value;
            }
        }

        /// <summary>
        /// Gets or sets note text. The NOTE item is intended for transient messages describing the current state
        /// of the source, e.g., "on the phone, can't talk". Value null means not specified.
        /// </summary>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public string Note
        {
            get{ return m_Note; }

            set{
                if(Encoding.UTF8.GetByteCount(value) > 255){
                    throw new ArgumentException("Property 'Note' value must be <= 255 bytes.");
                }

                m_Note = value;
            }
        }

        /// <summary>
        /// Gets number of bytes needed for this SDES chunk.
        /// </summary>
        public int Size
        {
            get{
                int size = 4;
                if(!string.IsNullOrEmpty(m_CName)){
                    size += 2;
                    size += Encoding.UTF8.GetByteCount(m_CName);
                }
                if(!string.IsNullOrEmpty(m_Name)){
                    size += 2;
                    size += Encoding.UTF8.GetByteCount(m_Name);
                }
                if(!string.IsNullOrEmpty(m_Email)){
                    size += 2;
                    size += Encoding.UTF8.GetByteCount(m_Email);
                }
                if(!string.IsNullOrEmpty(m_Phone)){
                    size += 2;
                    size += Encoding.UTF8.GetByteCount(m_Phone);
                }
                if(!string.IsNullOrEmpty(m_Location)){
                    size += 2;
                    size += Encoding.UTF8.GetByteCount(m_Location);
                }
                if(!string.IsNullOrEmpty(m_Tool)){
                    size += 2;
                    size += Encoding.UTF8.GetByteCount(m_Tool);
                }
                if(!string.IsNullOrEmpty(m_Note)){
                    size += 2;
                    size += Encoding.UTF8.GetByteCount(m_Note);
                }

                // Add terminate byte and padding bytes.
                size++;
                while((size % 4) > 0){
                    size++;
                }

                return size; 
            }
        }

        #endregion

    }
}
