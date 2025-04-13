using System;
using System.IO;
using System.Collections.Generic;
using System.Text;
using System.Threading;

namespace LumiSoft.Net.Media
{
    /// <summary>
    /// This class represent <b>wav</b> file player.
    /// </summary>
    public class WavePlayer
    {
        #region class RIFF_Chunk

        /// <summary>
        /// This class represents wave RIFF chunk.
        /// </summary>
        private class RIFF_Chunk
        {
            private uint   m_ChunkSize = 0;
            private string m_Format    = "";

            /// <summary>
            /// Default constructor.
            /// </summary>
            public RIFF_Chunk()
            {
            }


            #region method Parse

            /// <summary>
            /// Parses RIFF chunk from the specified reader.
            /// </summary>
            /// <param name="reader">Wave reader.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>reader</b> is null reference.</exception>
            public void Parse(BinaryReader reader)
            {
                if(reader == null){
                   throw new ArgumentNullException("reader");
                }

                m_ChunkSize = reader.ReadUInt32();
                m_Format    = new string(reader.ReadChars(4)).Trim();
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Returns "RIFF".
            /// </summary>
            public string ChunkID
            {
                get{ return "RIFF"; }
            }

            /// <summary>
            /// Gets chunk size.
            /// </summary>
            public uint ChunkSize
            {
                get{ return m_ChunkSize; }
            }

            /// <summary>
            /// Gets format.
            /// </summary>
            public string Format
            {
                get{ return m_Format; }
            }

            #endregion
        }

        #endregion

        #region class fmt_Chunk

        /// <summary>
        /// This class represents wave fmt chunk.
        /// </summary>
        private class fmt_Chunk
        {
            private uint m_ChunkSize        = 0;
            private int  m_AudioFormat      = 0;
            private int  m_NumberOfChannels = 0;
            private int  m_SampleRate       = 0;
            private int  m_AvgBytesPerSec   = 0;
            private int  m_BlockAlign       = 0;
            private int  m_BitsPerSample    = 0;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public fmt_Chunk()
            {
            }


            #region method Parse

            /// <summary>
            /// Parses fmt chunk from the specified reader.
            /// </summary>
            /// <param name="reader">Wave reader.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>reader</b> is null reference.</exception>
            public void Parse(BinaryReader reader)
            {
                if(reader == null){
                   throw new ArgumentNullException("reader");
                }

                m_ChunkSize        = reader.ReadUInt32();
                m_AudioFormat      = reader.ReadInt16();
                m_NumberOfChannels = reader.ReadInt16();
                m_SampleRate       = reader.ReadInt32();
                m_AvgBytesPerSec   = reader.ReadInt32();
                m_BlockAlign       = reader.ReadInt16();
                m_BitsPerSample    = reader.ReadInt16();

                // Eat all bytes above 16 size.
                for(int i=0;i<(m_ChunkSize - 16);i++){
                    reader.ReadByte();
                }
            }

            #endregion

            #region method ToString

            /// <summary>
            /// Returns this as string.
            /// </summary>
            /// <returns>Returns this as string.</returns>
            public override string ToString()
            {
                StringBuilder retVal = new StringBuilder();
                retVal.AppendLine("ChunkSize: " + m_ChunkSize);
                retVal.AppendLine("AudioFormat: " + m_AudioFormat);
                retVal.AppendLine("Channels: " + m_NumberOfChannels);
                retVal.AppendLine("SampleRate: " + m_SampleRate);
                retVal.AppendLine("AvgBytesPerSec: " + m_AvgBytesPerSec);
                retVal.AppendLine("BlockAlign: " + m_BlockAlign);
                retVal.AppendLine("BitsPerSample: " + m_BitsPerSample);

                return retVal.ToString();
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Returns "fmt".
            /// </summary>
            public string ChunkID
            {
                get{ return "fmt"; }
            }

            /// <summary>
            /// Gets chunk size.
            /// </summary>
            public uint ChunkSize
            {
                get{ return m_ChunkSize; }
            }

            /// <summary>
            /// Gets auido format. Value 1 is PCM.
            /// </summary>
            public int AudioFormat
            {
                get{ return m_AudioFormat; }
            }

            /// <summary>
            /// Gets number of channels.
            /// </summary>
            public int NumberOfChannels
            {
                get{ return m_NumberOfChannels; }
            }

            /// <summary>
            /// Gets sample rate(Hz).
            /// </summary>
            public int SampleRate
            {
                get{ return m_SampleRate; }
            }

            /// <summary>
            /// The average number of bytes per secondec at which the waveform data should be transferred.
            /// </summary>
            public int AvgBytesPerSec
            {
                get{ return m_AvgBytesPerSec; }
            }

            /// <summary>
            /// The block alignment (in bytes) of the waveform data.
            /// </summary>
            public int BlockAlign
            {
                get{ return m_BlockAlign; }
            }

            /// <summary>
            /// Gets bits per sample.
            /// </summary>
            public int BitsPerSample
            {
                get{ return m_BitsPerSample; }
            }

            #endregion
        }

        #endregion

        #region class data_Chunk

        /// <summary>
        /// This class represents wave data chunk.
        /// </summary>
        private class data_Chunk
        {
            private uint m_ChunkSize = 0;

            /// <summary>
            /// Default constructor.
            /// </summary>
            public data_Chunk()
            {
            }


            #region method Parse

            /// <summary>
            /// Parses data chunk from the specified reader.
            /// </summary>
            /// <param name="reader">Wave reader.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>reader</b> is null reference.</exception>
            public void Parse(BinaryReader reader)
            {
                if(reader == null){
                   throw new ArgumentNullException("reader");
                }

                m_ChunkSize = reader.ReadUInt32();
            }

            #endregion


            #region Properties implementation

            /// <summary>
            /// Returns "data".
            /// </summary>
            public string ChunkID
            {
                get{ return "data"; }
            }

            /// <summary>
            /// Gets chunk size.
            /// </summary>
            public uint ChunkSize
            {
                get{ return m_ChunkSize; }
            }

            #endregion
        }

        #endregion

        #region class WavReader

        /// <summary>
        /// This class implements wav file reader.
        /// </summary>
        private class WavReader
        {            
            private BinaryReader m_pBinaryReader = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="reader">Wav file reader.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>reader</b> is null reference.</exception>
            public WavReader(BinaryReader reader)
            {
                if(reader == null){
                    throw new ArgumentNullException("reader");
                }

                m_pBinaryReader = reader;
            }


            #region method Read_ChunkID

            /// <summary>
            /// Reads 4 char chunk ID.
            /// </summary>
            /// <returns>Returns 4 char chunk ID or null if end of stream reached.</returns>
            public string Read_ChunkID()
            {
                char[] chars = m_pBinaryReader.ReadChars(4);

                if(chars.Length == 0){
                    return null;
                }
                else{
                    return new string(chars).Trim();
                }
            }

            #endregion

            #region method Read_RIFF

            /// <summary>
            /// Reads RIFF chunk. 
            /// </summary>
            /// <returns>Returns RIFF chunk.</returns>
            public RIFF_Chunk Read_RIFF()
            {
                RIFF_Chunk retVal = new RIFF_Chunk();
                retVal.Parse(m_pBinaryReader);

                return retVal;
            }

            #endregion

            #region method Read_fmt

            /// <summary>
            /// Reads fmt chunk. 
            /// </summary>
            /// <returns>Returns fmt chunk.</returns>
            public fmt_Chunk Read_fmt()
            {
                fmt_Chunk retVal = new fmt_Chunk();
                retVal.Parse(m_pBinaryReader);

                return retVal;
            }

            #endregion

            #region method Read_data

            /// <summary>
            /// Reads data chunk. 
            /// </summary>
            /// <returns>Returns data chunk.</returns>
            public data_Chunk Read_data()
            {
                data_Chunk retVal = new data_Chunk();
                retVal.Parse(m_pBinaryReader);

                return retVal;
            }

            #endregion

            #region method SkipChunk

            /// <summary>
            /// Skips active chunk.
            /// </summary>
            public void SkipChunk()
            {
                uint chunkSize = m_pBinaryReader.ReadUInt32();

                m_pBinaryReader.BaseStream.Position += chunkSize;
            }

            #endregion
        }

        #endregion


        private bool           m_IsPlaying     = false;
        private bool           m_Stop          = false;
        private AudioOutDevice m_pOutputDevice = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="device">Audio output device.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>device</b> is null reference.</exception>
        public WavePlayer(AudioOutDevice device)
        {
            if(device == null){
                throw new ArgumentNullException("device");
            }

            m_pOutputDevice = device;
        }


        #region method Play

        /// <summary>
        /// Starts playing specified wave file for the specified number of times.
        /// </summary>
        /// <param name="file">Wave file.</param>
        /// <param name="count">Number of times to play.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>file</b> is null reference.</exception>
        public void Play(string file,int count)
        {
            if(file == null){
                throw new ArgumentNullException("file");
            }

            Play(File.OpenRead(file),count);
        }

        /// <summary>
        /// Starts playing specified wave file for the specified number of times.
        /// </summary>
        /// <param name="stream">Wave stream.</param>
        /// <param name="count">Number of times to play.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>stream</b> is null reference.</exception>
        public void Play(Stream stream,int count)
        {
            if(stream == null){
                throw new ArgumentNullException("stream");
            }

            if(m_IsPlaying){
                Stop();
            }

            m_IsPlaying = true;
            m_Stop      = false;

            ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object state){                        
                using(BinaryReader waveFile = new BinaryReader(stream)){
                    WavReader wavReader = new WavReader(waveFile);

                    if(!string.Equals(wavReader.Read_ChunkID(),"riff",StringComparison.InvariantCultureIgnoreCase)){
                        throw new ArgumentNullException("Invalid wave file, RIFF header missing.");
                    }
                    RIFF_Chunk riff = wavReader.Read_RIFF();
                                        
                    wavReader.Read_ChunkID();                    
                    fmt_Chunk fmt = wavReader.Read_fmt();
                                                                      
                    using(AudioOut player = new AudioOut(m_pOutputDevice,fmt.SampleRate,fmt.BitsPerSample,fmt.NumberOfChannels)){
                        long audioStartOffset = waveFile.BaseStream.Position;

                        // Loop audio playing for specified times.
                        for(int i=0;i<count;i++){
                            waveFile.BaseStream.Position = audioStartOffset;

                            // Read wave chunks.
                            while(true){
                                string chunkID = wavReader.Read_ChunkID();

                                // EOS reached.
                                if(chunkID == null || (waveFile.BaseStream.Length - waveFile.BaseStream.Position) < 4){
                                    break;
                                }
                                // Wave data chunk.
                                else if(string.Equals(chunkID,"data",StringComparison.InvariantCultureIgnoreCase)){
                                    data_Chunk data = wavReader.Read_data();

                                    int    totalReaded = 0;
                                    byte[] buffer      = new byte[8000];
                                    while(totalReaded < data.ChunkSize){
                                        if(m_Stop){
                                            m_IsPlaying = false;

                                            return;
                                        }

                                        // Read audio block.
                                        int countReaded = waveFile.Read(buffer,0,(int)Math.Min(buffer.Length,data.ChunkSize - totalReaded));

                                        // Queue audio for play.
                                        player.Write(buffer,0,countReaded);

                                        // Don't buffer more than 2x read buffer, just wait some data played out first.
                                        while(m_IsPlaying && player.BytesBuffered >= (buffer.Length * 2)){
                                            Thread.Sleep(10);
                                        }

                                        totalReaded += countReaded;
                                    }
                                }
                                // unknown chunk.
                                else{
                                    wavReader.SkipChunk();
                                }
                            }                            
                        }

                        // Wait while audio playing is completed.
                        while(m_IsPlaying && player.BytesBuffered > 0){
                            Thread.Sleep(10);
                        }
                    }
                }

                m_IsPlaying = false;
            }));
        }

        #endregion

        #region method Stop

        /// <summary>
        /// Stop currently played audio.
        /// </summary>
        public void Stop()
        {
            m_Stop = true;

            while(m_IsPlaying){
                Thread.Sleep(5);
            }
        }

        #endregion
    }
}
