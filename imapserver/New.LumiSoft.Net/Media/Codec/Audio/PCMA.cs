using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Media.Codec.Audio
{
    /// <summary>
    /// Implements PCMA(G711 alaw) codec.
    /// </summary>
    public class PCMA : AudioCodec
    {
        #region byte[] ALawCompressTable

        private static readonly byte[] ALawCompressTable = new byte[]{ 
            1,1,2,2,3,3,3,3, 
            4,4,4,4,4,4,4,4, 
            5,5,5,5,5,5,5,5, 
            5,5,5,5,5,5,5,5, 
            6,6,6,6,6,6,6,6, 
            6,6,6,6,6,6,6,6, 
            6,6,6,6,6,6,6,6, 
            6,6,6,6,6,6,6,6, 
            7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7, 
            7,7,7,7,7,7,7,7 
        };

        #endregion

        #region short[] ALawDecompressTable

        private static readonly short[] ALawDecompressTable = new short[]{ 
            -5504, -5248, -6016, -5760, -4480, -4224, -4992, -4736, 
            -7552, -7296, -8064, -7808, -6528, -6272, -7040, -6784, 
            -2752, -2624, -3008, -2880, -2240, -2112, -2496, -2368, 
            -3776, -3648, -4032, -3904, -3264, -3136, -3520, -3392, 
            -22016,-20992,-24064,-23040,-17920,-16896,-19968,-18944, 
            -30208,-29184,-32256,-31232,-26112,-25088,-28160,-27136, 
            -11008,-10496,-12032,-11520,-8960, -8448, -9984, -9472, 
            -15104,-14592,-16128,-15616,-13056,-12544,-14080,-13568, 
            -344,  -328,  -376,  -360,  -280,  -264,  -312,  -296, 
            -472,  -456,  -504,  -488,  -408,  -392,  -440,  -424, 
            -88,   -72,   -120,  -104,  -24,   -8,    -56,   -40, 
            -216,  -200,  -248,  -232,  -152,  -136,  -184,  -168, 
            -1376, -1312, -1504, -1440, -1120, -1056, -1248, -1184, 
            -1888, -1824, -2016, -1952, -1632, -1568, -1760, -1696, 
            -688,  -656,  -752,  -720,  -560,  -528,  -624,  -592, 
            -944,  -912,  -1008, -976,  -816,  -784,  -880,  -848, 
            5504,  5248,  6016,  5760,  4480,  4224,  4992,  4736, 
            7552,  7296,  8064,  7808,  6528,  6272,  7040,  6784, 
            2752,  2624,  3008,  2880,  2240,  2112,  2496,  2368, 
            3776,  3648,  4032,  3904,  3264,  3136,  3520,  3392, 
            22016, 20992, 24064, 23040, 17920, 16896, 19968, 18944, 
            30208, 29184, 32256, 31232, 26112, 25088, 28160, 27136, 
            11008, 10496, 12032, 11520, 8960,  8448,  9984,  9472, 
            15104, 14592, 16128, 15616, 13056, 12544, 14080, 13568, 
            344,   328,   376,   360,   280,   264,   312,   296, 
            472,   456,   504,   488,   408,   392,   440,   424, 
            88,    72,   120,   104,    24,     8,    56,    40, 
            216,   200,   248,   232,   152,   136,   184,   168, 
            1376,  1312,  1504,  1440,  1120,  1056,  1248,  1184, 
            1888,  1824,  2016,  1952,  1632,  1568,  1760,  1696, 
            688,   656,   752,   720,   560,   528,   624,   592, 
            944,   912,  1008,   976,   816,   784,   880,   848 
        };

        #endregion

        private AudioFormat m_pAudioFormat           = new AudioFormat(8000,16,1);
        private AudioFormat m_pCompressedAudioFormat = new AudioFormat(8000,8,1);

        /// <summary>
        /// Default constructor.
        /// </summary>
        public PCMA()
        {
        }


        #region method Encode

        /// <summary>
        /// Encodes linear 16-bit linear PCM to 8-bit alaw.
        /// </summary>
        /// <param name="buffer">Data to encode. Data must be in Little-Endian format.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="count">Number of bytes to encode.</param>
        /// <returns>Returns encoded block.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
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
                throw new ArgumentException("Invalid 'count' value, it doesn't contain 16-bit boundaries.");
            }

            int    offsetInRetVal = 0;
            byte[] retVal         = new byte[count / 2];
            while(offsetInRetVal < retVal.Length){
                // Little-Endian - lower byte,higer byte.
                short pcm = (short)(buffer[offset + 1] << 8 | buffer[offset]);
                offset += 2;
                
                retVal[offsetInRetVal++] = LinearToALawSample(pcm);
            }

            return retVal;
        }

        #endregion

        #region method Decode

        /// <summary>
        /// Decodes 8-bit alaw to linear 16-bit PCM.
        /// </summary>
        /// <param name="buffer">Data to decode. Data must be in Little-Endian format.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="count">Number of bytes to decode.</param>
        /// <returns>Returns decoded data.</returns>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public override byte[] Decode(byte[] buffer,int offset,int count)
        {
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0 || offset > buffer.Length){
                throw new ArgumentException("Argument 'offse't is out of range.");
            }
            if(count < 1 || (count + offset) > buffer.Length){
                throw new ArgumentException("Argument 'count' is out of range.");
            }

            int    offsetInRetVal = 0;
            byte[] retVal         = new byte[count * 2];
            for(int i=offset;i<buffer.Length;i++){
                short pcm = ALawDecompressTable[buffer[i]];                
                retVal[offsetInRetVal++] = (byte)(pcm      & 0xFF);
                retVal[offsetInRetVal++] = (byte)(pcm >> 8 & 0xFF);
            }

            return retVal;
        }

        #endregion


        #region static method LinearToALawSample

        private static byte LinearToALawSample(short sample) 
        { 
            int  sign           = 0;
            int  exponent       = 0; 
            int  mantissa       = 0; 
            byte compressedByte = 0;

            sign = ((~sample) >> 8) & 0x80; 
            if(sign == 0){ 
                sample = (short)-sample; 
            }
            if(sample > 32635){
                sample = 32635;
            }
            if(sample >= 256){ 
                exponent = (int)ALawCompressTable[(sample >> 8) & 0x7F]; 
                mantissa = (sample >> (exponent + 3) ) & 0x0F; 
                compressedByte = (byte)((exponent << 4) | mantissa); 
            } 
            else{ 
                compressedByte = (byte)(sample >> 4); 
            } 

            compressedByte ^= (byte)(sign ^ 0x55); 

            return compressedByte;
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets codec name.
        /// </summary>
        public override string Name
        {
            get{ return "PCMA"; }
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
