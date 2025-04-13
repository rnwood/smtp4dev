using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Media.Codec.Audio
{
    /// <summary>
    /// Implements PCMU(G711 ulaw) codec.
    /// </summary>
    public class PCMU : AudioCodec
    {
        #region byte[] MuLawCompressTable

        private static readonly byte[] MuLawCompressTable = new byte[]{ 
            0,0,1,1,2,2,2,2,3,3,3,3,3,3,3,3, 
            4,4,4,4,4,4,4,4,4,4,4,4,4,4,4,4, 
            5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5, 
            5,5,5,5,5,5,5,5,5,5,5,5,5,5,5,5, 
            6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6, 
            6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6, 
            6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6, 
            6,6,6,6,6,6,6,6,6,6,6,6,6,6,6,6, 
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7,7,7,7,7,7,7,7,7 
        };

        #endregion

        #region short[] MuLawDecompressTable

        private static readonly short[] MuLawDecompressTable = new short[]{ 
            -32124,-31100,-30076,-29052,-28028,-27004,-25980,-24956, 
            -23932,-22908,-21884,-20860,-19836,-18812,-17788,-16764, 
            -15996,-15484,-14972,-14460,-13948,-13436,-12924,-12412, 
            -11900,-11388,-10876,-10364, -9852, -9340, -8828, -8316, 
            -7932, -7676, -7420, -7164, -6908, -6652, -6396, -6140, 
            -5884, -5628, -5372, -5116, -4860, -4604, -4348, -4092, 
            -3900, -3772, -3644, -3516, -3388, -3260, -3132, -3004, 
            -2876, -2748, -2620, -2492, -2364, -2236, -2108, -1980, 
            -1884, -1820, -1756, -1692, -1628, -1564, -1500, -1436, 
            -1372, -1308, -1244, -1180, -1116, -1052,  -988,  -924, 
            -876,  -844,  -812,  -780,  -748,  -716,  -684,  -652, 
            -620,  -588,  -556,  -524,  -492,  -460,  -428,  -396, 
            -372,  -356,  -340,  -324,  -308,  -292,  -276,  -260, 
            -244,  -228,  -212,  -196,  -180,  -164,  -148,  -132, 
            -120,  -112,  -104,   -96,   -88,   -80,   -72,   -64, 
            -56,   -48,   -40,   -32,   -24,   -16,    -8,     0, 
            32124, 31100, 30076, 29052, 28028, 27004, 25980, 24956, 
            23932, 22908, 21884, 20860, 19836, 18812, 17788, 16764, 
            15996, 15484, 14972, 14460, 13948, 13436, 12924, 12412, 
            11900, 11388, 10876, 10364,  9852,  9340,  8828,  8316, 
            7932,  7676,  7420,  7164,  6908,  6652,  6396,  6140, 
            5884,  5628,  5372,  5116,  4860,  4604,  4348,  4092, 
            3900,  3772,  3644,  3516,  3388,  3260,  3132,  3004, 
            2876,  2748,  2620,  2492,  2364,  2236,  2108,  1980, 
            1884,  1820,  1756,  1692,  1628,  1564,  1500,  1436, 
            1372,  1308,  1244,  1180,  1116,  1052,   988,   924, 
            876,   844,   812,   780,   748,   716,   684,   652, 
            620,   588,   556,   524,   492,   460,   428,   396, 
            372,   356,   340,   324,   308,   292,   276,   260, 
            244,   228,   212,   196,   180,   164,   148,   132, 
            120,   112,   104,    96,    88,    80,    72,    64, 
            56,    48,    40,    32,    24,    16,     8,     0 
        };

        #endregion

        private AudioFormat m_pAudioFormat           = new AudioFormat(8000,16,1);
        private AudioFormat m_pCompressedAudioFormat = new AudioFormat(8000,8,1);

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PCMU()
        {
        }


        #region method Encode

        /// <summary>
        /// Encodes linear 16-bit linear PCM to 8-bit ulaw.
        /// </summary>
        /// <param name="buffer">Data to encode. Data must be in Little-Endian format.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="count">Number of bytes to encode.</param>
        /// <returns>Returns encoded block.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference value.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public override byte[] Encode(byte[] buffer,int offset,int count)
        {
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0 || offset > buffer.Length){
                throw new ArgumentException("Argument 'offset' is out of range.");
            }
            if(count < 1 || (count + offset) > buffer.Length){
                throw new ArgumentException("Argument 'count' is out of range.");
            }
            if((count % 2) != 0){
                throw new ArgumentException("Invalid buffer value, it doesn't contain 16-bit boundaries.");
            }

            int    offsetInRetVal = 0;
            byte[] retVal         = new byte[count / 2];
            while(offsetInRetVal < retVal.Length){
                // Little-Endian - lower byte,higer byte.
                short pcm = (short)(buffer[offset + 1] << 8 | buffer[offset]);
                offset += 2;
                
                retVal[offsetInRetVal++] = LinearToMuLawSample(pcm);
            }

            return retVal;
        }

        #endregion

        #region method Decode

        /// <summary>
        /// Decodes 8-bit ulaw to linear 16-bit PCM.
        /// </summary>
        /// <param name="buffer">Data to decode. Data must be in Little-Endian format.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="count">Number of bytes to decode.</param>
        /// <returns>Returns decoded data.</returns>
        /// <exception cref="ArgumentNullException">Is riased when <b>buffer</b> is null reference value.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public override byte[] Decode(byte[] buffer,int offset,int count)
        {
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0 || offset > buffer.Length){
                throw new ArgumentException("Argument 'offset' is out of range.");
            }
            if(count < 1 || (count + offset) > buffer.Length){
                throw new ArgumentException("Argument 'count' is out of range.");
            }

            int    offsetInRetVal = 0;
            byte[] retVal         = new byte[count * 2];
            for(int i=offset;i<buffer.Length;i++){
                short pcm = MuLawDecompressTable[buffer[i]];                
                retVal[offsetInRetVal++] = (byte)(pcm      & 0xFF);
                retVal[offsetInRetVal++] = (byte)(pcm >> 8 & 0xFF);
            }

            return retVal;
        }

        #endregion


        #region static method LinearToMuLawSample

        private static byte LinearToMuLawSample(short sample) 
        { 
            int cBias = 0x84; 
            int cClip = 32635;

            int sign = (sample >> 8) & 0x80; 
            if(sign != 0){ 
                sample = (short)-sample; 
            }
            if(sample > cClip){
                sample = (short)cClip;
            }
            sample = (short)(sample + cBias); 
            int exponent = (int)MuLawCompressTable[(sample>>7) & 0xFF]; 
            int mantissa = (sample >> (exponent+3)) & 0x0F; 
            int compressedByte = ~(sign | (exponent << 4) | mantissa); 

            return (byte)compressedByte;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets codec name.
        /// </summary>
        public override string Name
        {
            get{ return "PCMU"; }
        }

        /// <summary>
        /// Gets uncompressed audio format info.
        /// </summary>
        public override AudioFormat AudioFormat
        {
            get{ return m_pAudioFormat; }
        }

        /// <summary>
        /// Gets compressed audio format info.
        /// </summary>
        public override AudioFormat CompressedAudioFormat
        {
            get{ return m_pCompressedAudioFormat; }
        }

        #endregion

    }
}
