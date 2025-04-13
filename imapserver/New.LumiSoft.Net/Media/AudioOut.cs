using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Runtime.InteropServices;

namespace LumiSoft.Net.Media
{
    /// <summary>
    /// This class implements PCM audio player.
    /// </summary>
    public class AudioOut : IDisposable
    {
        #region class WaveOut

        /// <summary>
        /// This class provides windows native waveOut implementation.
        /// </summary>
        private class WaveOut
        {
            /// <summary>
            /// The waveOutProc function is the callback function used with the waveform-audio output device.
            /// </summary>
            /// <param name="hdrvr">Handle to the waveform-audio device associated with the callback.</param>
            /// <param name="uMsg">Waveform-audio output message.</param>
            /// <param name="dwUser">User-instance data specified with waveOutOpen.</param>
            /// <param name="dwParam1">Message parameter.</param>
            /// <param name="dwParam2">Message parameter.</param>
            private delegate void waveOutProc(IntPtr hdrvr,int uMsg,int dwUser,int dwParam1,int dwParam2);

            #region class MMSYSERR

            /// <summary>
            /// This class holds MMSYSERR errors.
            /// </summary>
            private class MMSYSERR
            {
                /// <summary>
                /// Success.
                /// </summary>
                public const int NOERROR = 0;
                /// <summary>
                /// Unspecified error.
                /// </summary>
                public const int ERROR = 1;
                /// <summary>
                /// Device ID out of range.
                /// </summary>
                public const int BADDEVICEID = 2;
                /// <summary>
                /// Driver failed enable.
                /// </summary>
                public const int NOTENABLED = 3;
                /// <summary>
                /// Device already allocated.
                /// </summary>
                public const int ALLOCATED = 4;
                /// <summary>
                /// Device handle is invalid.
                /// </summary>
                public const int INVALHANDLE = 5;
                /// <summary>
                /// No device driver present.
                /// </summary>
                public const int NODRIVER = 6;
                /// <summary>
                /// Memory allocation error.
                /// </summary>
                public const int NOMEM = 7;
                /// <summary>
                /// Function isn't supported.
                /// </summary>
                public const int NOTSUPPORTED = 8;
                /// <summary>
                /// Error value out of range.
                /// </summary>
                public const int BADERRNUM = 9;
                /// <summary>
                /// Invalid flag passed.
                /// </summary>
                public const int INVALFLAG = 1;
                /// <summary>
                /// Invalid parameter passed.
                /// </summary>
                public const int INVALPARAM = 11;
                /// <summary>
                /// Handle being used simultaneously on another thread (eg callback).
                /// </summary>
                public const int HANDLEBUSY = 12;
                /// <summary>
                /// Specified alias not found.
                /// </summary>
                public const int INVALIDALIAS = 13;
                /// <summary>
                /// Bad registry database.
                /// </summary>
                public const int BADDB = 14;
                /// <summary>
                /// Registry key not found.
                /// </summary>
                public const int KEYNOTFOUND = 15;
                /// <summary>
                /// Registry read error.
                /// </summary>
                public const int READERROR = 16;
                /// <summary>
                /// Registry write error.
                /// </summary>
                public const int WRITEERROR = 17;
                /// <summary>
                /// Eegistry delete error.
                /// </summary>
                public const int DELETEERROR = 18;
                /// <summary>
                /// Registry value not found. 
                /// </summary>
                public const int VALNOTFOUND = 19;
                /// <summary>
                /// Driver does not call DriverCallback.
                /// </summary>
                public const int NODRIVERCB = 20;
                /// <summary>
                /// Last error in range.
                /// </summary>
                public const int LASTERROR = 20;
            }

            #endregion

            #region class WavConstants

            /// <summary>
            /// This class provides most used wav constants.
            /// </summary>
            private class WavConstants
            {
                public const int MM_WOM_OPEN = 0x3BB;
		        public const int MM_WOM_CLOSE = 0x3BC;
		        public const int MM_WOM_DONE = 0x3BD;

                public const int MM_WIM_OPEN = 0x3BE;   
                public const int MM_WIM_CLOSE = 0x3BF;
                public const int MM_WIM_DATA = 0x3C0;


		        public const int CALLBACK_FUNCTION = 0x00030000;

                public const int WAVERR_STILLPLAYING = 0x21;

                public const int WHDR_DONE = 0x00000001;
                public const int WHDR_PREPARED = 0x00000002;
                public const int WHDR_BEGINLOOP = 0x00000004;
                public const int WHDR_ENDLOOP = 0x00000008;
                public const int WHDR_INQUEUE = 0x00000010;
            }

            #endregion

            #region class WavMethods

            /// <summary>
            /// This class provides windows wav methods.
            /// </summary>
            private class WavMethods
            {
                /// <summary>
                /// Closes the specified waveform output device.
                /// </summary>
                /// <param name="hWaveOut">Handle to the waveform-audio output device. If the function succeeds, the handle is no longer valid after this call.</param>
                /// <returns>Returns value of MMSYSERR.</returns>
                [DllImport("winmm.dll")]
		        public static extern int waveOutClose(IntPtr hWaveOut);
                
                /// <summary>
                /// Queries a specified waveform device to determine its capabilities.
                /// </summary>
                /// <param name="hwo">Identifier of the waveform-audio output device. It can be either a device identifier or a Handle to an open waveform-audio output device.</param>
                /// <param name="pwoc">Pointer to a WAVEOUTCAPS structure to be filled with information about the capabilities of the device.</param>
                /// <param name="cbwoc">Size, in bytes, of the WAVEOUTCAPS structure.</param>
                /// <returns>Returns value of MMSYSERR.</returns>
                [DllImport("winmm.dll")]
                public static extern uint waveOutGetDevCaps(uint hwo,ref WAVEOUTCAPS pwoc,int cbwoc);

                /// <summary>
                /// Retrieves the number of waveform output devices present in the system.
                /// </summary>
                /// <returns>The number of devices indicates success. Zero indicates that no devices are present or that an error occurred.</returns>
                [DllImport("winmm.dll")]
		        public static extern int waveOutGetNumDevs();
        
                /// <summary>
                /// Retrieves the current playback position of the specified waveform output device.
                /// </summary>
                /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
                /// <param name="lpInfo">Pointer to an MMTIME structure.</param>
                /// <param name="uSize">Size, in bytes, of the MMTIME structure.</param>
                /// <returns>Returns value of MMSYSERR.</returns>
                [DllImport("winmm.dll")]
		        public static extern int waveOutGetPosition(IntPtr hWaveOut,out int lpInfo,int uSize);

                /// <summary>
                /// Queries the current volume setting of a waveform output device.
                /// </summary>
                /// <param name="hWaveOut">Handle to an open waveform-audio output device. This parameter can also be a device identifier.</param>
                /// <param name="dwVolume">Pointer to a variable to be filled with the current volume setting. 
                /// The low-order word of this location contains the left-channel volume setting, and the high-order 
                /// word contains the right-channel setting. A value of 0xFFFF represents full volume, and a 
                /// value of 0x0000 is silence.</param>
                /// <returns>Returns value of MMSYSERR.</returns>
                [DllImport("winmm.dll")]
		        public static extern int waveOutGetVolume(IntPtr hWaveOut,out int dwVolume);

                /// <summary>
                /// The waveOutOpen function opens the given waveform-audio output device for playback.
                /// </summary>
                /// <param name="hWaveOut">Pointer to a buffer that receives a handle identifying the open waveform-audio output device. Use the handle to identify the device when calling other waveform-audio output functions. This parameter might be NULL if the WAVE_FORMAT_QUERY flag is specified for fdwOpen.</param>
                /// <param name="uDeviceID">Identifier of the waveform-audio output device to open. It can be either a device identifier or a handle of an open waveform-audio input device.</param>
                /// <param name="lpFormat">Pointer to a WAVEFORMATEX structure that identifies the format of the waveform-audio data to be sent to the device. You can free this structure immediately after passing it to waveOutOpen.</param>
                /// <param name="dwCallback">Pointer to a fixed callback function, an event handle, a handle to a window, or the identifier of a thread to be called during waveform-audio playback to process messages related to the progress of the playback. If no callback function is required, this value can be zero.</param>
                /// <param name="dwInstance">User-instance data passed to the callback mechanism.</param>
                /// <param name="dwFlags">Flags for opening the device.</param>
                /// <returns>Returns value of MMSYSERR.</returns>
		        [DllImport("winmm.dll")]
		        public static extern int waveOutOpen(out IntPtr hWaveOut,int uDeviceID,WAVEFORMATEX lpFormat,waveOutProc dwCallback,int dwInstance,int dwFlags);
        
                /// <summary>
                /// Pauses playback on a specified waveform output device.
                /// </summary>
                /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
                /// <returns>Returns value of MMSYSERR.</returns>
                [DllImport("winmm.dll")]
		        public static extern int waveOutPause(IntPtr hWaveOut);

                /// <summary>
                /// Prepares a waveform data block for playback.
                /// </summary>
                /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
                /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure that identifies the data block to be prepared. The buffer's base address must be aligned with the respect to the sample size.</param>
                /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
                /// <returns>Returns value of MMSYSERR.</returns>
		        [DllImport("winmm.dll")]
		        public static extern int waveOutPrepareHeader(IntPtr hWaveOut,IntPtr lpWaveOutHdr,int uSize);

                /// <summary>
                /// Stops playback on a specified waveform output device and resets the current position to 0.
                /// </summary>
                /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
                /// <returns>Returns value of MMSYSERR.</returns>
                [DllImport("winmm.dll")]
		        public static extern int waveOutReset(IntPtr hWaveOut);

                /// <summary>
                /// Restarts a paused waveform output device.
                /// </summary>
                /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
                /// <returns>Returns value of MMSYSERR.</returns>
                [DllImport("winmm.dll")]
		        public static extern int waveOutRestart(IntPtr hWaveOut);

                /// <summary>
                /// Sets the volume of a waveform output device.
                /// </summary>
                /// <param name="hWaveOut">Handle to an open waveform-audio output device. This parameter can also be a device identifier.</param>
                /// <param name="dwVolume">Specifies a new volume setting. The low-order word contains the left-channel 
                /// volume setting, and the high-order word contains the right-channel setting. A value of 0xFFFF 
                /// represents full volume, and a value of 0x0000 is silence.</param>
                /// <returns>Returns value of MMSYSERR.</returns>
                [DllImport("winmm.dll")]
		        public static extern int waveOutSetVolume(IntPtr hWaveOut,int dwVolume);

                /// <summary>
                /// Cleans up the preparation performed by waveOutPrepareHeader.
                /// </summary>
                /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
                /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure identifying the data block to be cleaned up.</param>
                /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
                /// <returns>Returns value of MMSYSERR.</returns>
		        [DllImport("winmm.dll")]
		        public static extern int waveOutUnprepareHeader(IntPtr hWaveOut,IntPtr lpWaveOutHdr,int uSize);

                /// <summary>
                /// Sends a data block to the specified waveform output device.
                /// </summary>
                /// <param name="hWaveOut">Handle to the waveform-audio output device.</param>
                /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure containing information about the data block.</param>
                /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
                /// <returns>Returns value of MMSYSERR.</returns>
		        [DllImport("winmm.dll")]
		        public static extern int waveOutWrite(IntPtr hWaveOut,IntPtr lpWaveOutHdr,int uSize);

            }

            #endregion

            #region struct WAVEOUTCAPS

            /// <summary>
            /// This class represents WAVEOUTCAPS structure.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            private struct WAVEOUTCAPS
            {
                /// <summary>
                /// Manufacturer identifier for the device driver for the device.
                /// </summary>
                public ushort wMid;
                /// <summary>
                /// Product identifier for the device.
                /// </summary>
                public ushort wPid;
                /// <summary>
                /// Version number of the device driver for the device.
                /// </summary>
                public uint vDriverVersion;
                /// <summary>
                /// Product name in a null-terminated string.
                /// </summary>
                [MarshalAs(UnmanagedType.ByValTStr,SizeConst = 32)]
                public string szPname;
                /// <summary>
                /// Standard formats that are supported.
                /// </summary>
                public uint dwFormats;
                /// <summary>
                /// Number specifying whether the device supports mono (1) or stereo (2) output.
                /// </summary>
                public ushort wChannels;
                /// <summary>
                /// Packing.
                /// </summary>
                public ushort wReserved1;
                /// <summary>
                /// Optional functionality supported by the device.
                /// </summary>
                public uint dwSupport;
            }

            #endregion

            #region class WAVEFORMATEX

            /// <summary>
            /// This class represents WAVEFORMATEX structure.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            private class WAVEFORMATEX
            {
                /// <summary>
                /// Waveform-audio format type. Format tags are registered with Microsoft Corporation for many 
                /// compression algorithms. A complete list of format tags can be found in the Mmreg.h header file. 
                /// For one- or two-channel PCM data, this value should be WAVE_FORMAT_PCM. When this structure is 
                /// included in a WAVEFORMATEXTENSIBLE structure, this value must be WAVE_FORMAT_EXTENSIBLE.</summary>
                public ushort wFormatTag;
                /// <summary>
                /// Number of channels in the waveform-audio data. Monaural data uses one channel and stereo data 
                /// uses two channels.
                /// </summary>
                public ushort nChannels;
                /// <summary>
                /// Sample rate, in samples per second (hertz). If wFormatTag is WAVE_FORMAT_PCM, then common 
                /// values for nSamplesPerSec are 8.0 kHz, 11.025 kHz, 22.05 kHz, and 44.1 kHz.
                /// </summary>
                public uint nSamplesPerSec;
                /// <summary>
                /// Required average data-transfer rate, in bytes per second, for the format tag. If wFormatTag 
                /// is WAVE_FORMAT_PCM, nAvgBytesPerSec should be equal to the product of nSamplesPerSec and nBlockAlign.
                /// </summary>
                public uint nAvgBytesPerSec;
                /// <summary>
                /// Block alignment, in bytes. The block alignment is the minimum atomic unit of data for the wFormatTag 
                /// format type. If wFormatTag is WAVE_FORMAT_PCM or WAVE_FORMAT_EXTENSIBLE, nBlockAlign must be equal 
                /// to the product of nChannels and wBitsPerSample divided by 8 (bits per byte).
                /// </summary>
                public ushort nBlockAlign;
                /// <summary>
                /// Bits per sample for the wFormatTag format type. If wFormatTag is WAVE_FORMAT_PCM, then 
                /// wBitsPerSample should be equal to 8 or 16.
                /// </summary>
                public ushort wBitsPerSample;
                /// <summary>
                /// Size, in bytes, of extra format information appended to the end of the WAVEFORMATEX structure.
                /// </summary>
                public ushort cbSize;
            }

            #endregion

            #region struct WAVEHDR

            /// <summary>
            /// This class represents WAVEHDR structure.
            /// </summary>
            [StructLayout(LayoutKind.Sequential)]
            private struct WAVEHDR
            {
                /// <summary>
                /// Long pointer to the address of the waveform buffer.
                /// </summary>
                public IntPtr lpData;
                /// <summary>
                /// Specifies the length, in bytes, of the buffer.
                /// </summary>
                public uint dwBufferLength;
                /// <summary>
                /// When the header is used in input, this member specifies how much data is in the buffer. 
                /// When the header is used in output, this member specifies the number of bytes played from the buffer.
                /// </summary>
                public uint dwBytesRecorded;
                /// <summary>
                /// Specifies user data.
                /// </summary>
                public IntPtr dwUser;
                /// <summary>
                /// Specifies information about the buffer.
                /// </summary>
                public uint dwFlags;
                /// <summary>
                /// Specifies the number of times to play the loop.
                /// </summary>
                public uint dwLoops;
                /// <summary>
                /// Reserved. This member is used within the audio driver to maintain a first-in, first-out linked list of headers awaiting playback.
                /// </summary>
                public IntPtr lpNext;
                /// <summary>
                /// Reserved.
                /// </summary>
                public uint reserved;
            }

            #endregion

            #region class PlayItem

            /// <summary>
            /// This class holds queued wav play item.
            /// </summary>
            private class PlayItem
            {
                private GCHandle m_HeaderHandle;
                private GCHandle m_DataHandle;
                private int      m_DataSize = 0;

                /// <summary>
                /// Default constructor.
                /// </summary>
                /// <param name="headerHandle">Header handle.</param>
                /// <param name="dataHandle">Wav header data handle.</param>
                /// <param name="dataSize">Data size in bytes.</param>
                public PlayItem(ref GCHandle headerHandle,ref GCHandle dataHandle,int dataSize)
                {
                    m_HeaderHandle = headerHandle;
                    m_DataHandle   = dataHandle;
                    m_DataSize     = dataSize;
                }

                #region method Dispose

                /// <summary>
                /// Cleans up any resources being used.
                /// </summary>
                public void Dispose()
                {
                    m_HeaderHandle.Free();
                    m_DataHandle.Free();
                }

                #endregion


                #region Properties Implementation

                /// <summary>
                /// Gets header handle.
                /// </summary>
                public GCHandle HeaderHandle
                {
                    get{ return m_HeaderHandle; }
                }

                /// <summary>
                /// Gets header.
                /// </summary>
                public WAVEHDR Header
                {
                    get{ return (WAVEHDR)m_HeaderHandle.Target; }
                }

                /// <summary>
                /// Gets wav header data pointer handle.
                /// </summary>
                public GCHandle DataHandle
                {
                    get{ return m_DataHandle; }
                }

                /// <summary>
                /// Gets wav header data size in bytes.
                /// </summary>
                public int DataSize
                {
                    get{ return m_DataSize; }
                }

                #endregion

            }

            #endregion

            private AudioOutDevice  m_pOutDevice    = null;
            private int             m_SamplesPerSec = 8000;
            private int             m_BitsPerSample = 16;
            private int             m_Channels      = 1;
            private int             m_MinBuffer     = 1200;
            private IntPtr          m_pWavDevHandle = IntPtr.Zero;
            private int             m_BlockSize     = 0;
            private int             m_BytesBuffered = 0;
            private bool            m_IsPaused      = false;
            private List<PlayItem>  m_pPlayItems    = null;
            private waveOutProc     m_pWaveOutProc  = null;
            private bool            m_IsDisposed    = false;
        
            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="outputDevice">Output device.</param>
            /// <param name="samplesPerSec">Sample rate, in samples per second (hertz). For PCM common values are 
            /// 8.0 kHz, 11.025 kHz, 22.05 kHz, and 44.1 kHz.</param>
            /// <param name="bitsPerSample">Bits per sample. For PCM 8 or 16 are the only valid values.</param>
            /// <param name="channels">Number of channels.</param>
            /// <exception cref="ArgumentNullException">Is raised when <b>outputDevice</b> is null.</exception>
            /// <exception cref="ArgumentException">Is raised when any of the aruments has invalid value.</exception>
            public WaveOut(AudioOutDevice outputDevice,int samplesPerSec,int bitsPerSample,int channels)
            {
                if(outputDevice == null){
                    throw new ArgumentNullException("outputDevice");
                }
                if(samplesPerSec < 8000){
                    throw new ArgumentException("Argument 'samplesPerSec' value must be >= 8000.");
                }
                if(bitsPerSample < 8){
                    throw new ArgumentException("Argument 'bitsPerSample' value must be >= 8.");
                }
                if(channels < 1){
                    throw new ArgumentException("Argument 'channels' value must be >= 1.");
                }

                m_pOutDevice    = outputDevice;
                m_SamplesPerSec = samplesPerSec;
                m_BitsPerSample = bitsPerSample;
                m_Channels      = channels;
                m_BlockSize     = m_Channels * (m_BitsPerSample / 8);
                m_pPlayItems    = new List<PlayItem>();
            
                // Try to open wav device.            
                WAVEFORMATEX format = new WAVEFORMATEX();
                format.wFormatTag      = 0x0001; // PCM - 0x0001
                format.nChannels       = (ushort)m_Channels;
                format.nSamplesPerSec  = (uint)samplesPerSec;                        
                format.nAvgBytesPerSec = (uint)(m_SamplesPerSec * m_Channels * (m_BitsPerSample / 8));
                format.nBlockAlign     = (ushort)m_BlockSize;
                format.wBitsPerSample  = (ushort)m_BitsPerSample;
                format.cbSize          = 0; 
                // We must delegate reference, otherwise GC will collect it.
                m_pWaveOutProc = new waveOutProc(this.OnWaveOutProc);
                int result = WavMethods.waveOutOpen(out m_pWavDevHandle,m_pOutDevice.Index,format,m_pWaveOutProc,0,WavConstants.CALLBACK_FUNCTION);
                if(result != MMSYSERR.NOERROR){
                    throw new Exception("Failed to open wav device, error: " + result.ToString() + ".");
                }
            }

            /// <summary>
            /// Default destructor.
            /// </summary>
            ~WaveOut()
            {
                Dispose();
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

                try{
                    // If playing, we need to reset wav device first.
                    WavMethods.waveOutReset(m_pWavDevHandle);
                    WavMethods.waveOutClose(m_pWavDevHandle);

                    // If there are unprepared wav headers, we need to unprepare these.
                    foreach(PlayItem item in m_pPlayItems){
                        WavMethods.waveOutUnprepareHeader(m_pWavDevHandle,item.HeaderHandle.AddrOfPinnedObject(),Marshal.SizeOf(item.Header));
                        item.Dispose();
                    }
                
                    // Close output device.
                    WavMethods.waveOutClose(m_pWavDevHandle);

                    m_pOutDevice    = null;
                    m_pWavDevHandle = IntPtr.Zero;
                    m_pPlayItems    = null;
                    m_pWaveOutProc  = null;
                }
                catch{
                }
            }

            #endregion


            #region method OnWaveOutProc

            /// <summary>
            /// This method is called when wav device generates some event.
            /// </summary>
            /// <param name="hdrvr">Handle to the waveform-audio device associated with the callback.</param>
            /// <param name="uMsg">Waveform-audio output message.</param>
            /// <param name="dwUser">User-instance data specified with waveOutOpen.</param>
            /// <param name="dwParam1">Message parameter.</param>
            /// <param name="dwParam2">Message parameter.</param>
            private void OnWaveOutProc(IntPtr hdrvr,int uMsg,int dwUser,int dwParam1,int dwParam2)
            {   
                // NOTE: MSDN warns, we may not call any wav related methods here.

                try{
                    if(uMsg == WavConstants.MM_WOM_DONE){ 
                        ThreadPool.QueueUserWorkItem(new WaitCallback(this.OnCleanUpFirstBlock));
                    }
                }
                catch{
                }
            }

            #endregion

            #region method OnCleanUpFirstBlock

            /// <summary>
            /// Cleans up the first data block in play queue.
            /// </summary>
            /// <param name="state">User data.</param>
            private void OnCleanUpFirstBlock(object state)
            {
                if(m_IsDisposed){
                    return;
                }

                try{            
                    lock(m_pPlayItems){
                        PlayItem item = m_pPlayItems[0];
                        WavMethods.waveOutUnprepareHeader(m_pWavDevHandle,item.HeaderHandle.AddrOfPinnedObject(),Marshal.SizeOf(item.Header));                    
                        m_pPlayItems.Remove(item);
                        m_BytesBuffered -= item.DataSize;
                        item.Dispose();
                    }
                }
                catch{
                }
            }

            #endregion


            #region method Play

            /// <summary>
            /// Plays specified audio data bytes. If player is currently playing, data will be queued for playing.
            /// </summary>
            /// <param name="audioData">Audio data. Data boundary must n * BlockSize.</param>
            /// <param name="offset">Offset in the buffer.</param>
            /// <param name="count">Number of bytes to play form the specified offset.</param>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this method is accessed.</exception>
            /// <exception cref="ArgumentNullException">Is raised when <b>audioData</b> is null.</exception>
            /// <exception cref="ArgumentException">Is raised when <b>audioData</b> is with invalid length.</exception>
            public void Play(byte[] audioData,int offset,int count)
            {
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WaveOut");
                }
                if(audioData == null){
                    throw new ArgumentNullException("audioData");
                }
                if((count % m_BlockSize) != 0){
                    throw new ArgumentException("Audio data is not n * BlockSize.");
                }

                //--- Queue specified audio block for play. --------------------------------------------------------
                byte[]   data       = new byte[count];
                Array.Copy(audioData,offset,data,0,count);
                GCHandle dataHandle = GCHandle.Alloc(data,GCHandleType.Pinned);

                WAVEHDR wavHeader = new WAVEHDR();
                wavHeader.lpData          = dataHandle.AddrOfPinnedObject();
                wavHeader.dwBufferLength  = (uint)data.Length;
                wavHeader.dwBytesRecorded = 0;
                wavHeader.dwUser          = IntPtr.Zero;
                wavHeader.dwFlags         = 0;
                wavHeader.dwLoops         = 0;
                wavHeader.lpNext          = IntPtr.Zero;
                wavHeader.reserved        = 0;
                GCHandle headerHandle = GCHandle.Alloc(wavHeader,GCHandleType.Pinned);
                int result = 0;        
                result = WavMethods.waveOutPrepareHeader(m_pWavDevHandle,headerHandle.AddrOfPinnedObject(),Marshal.SizeOf(wavHeader));
                if(result == MMSYSERR.NOERROR){
                    PlayItem item = new PlayItem(ref headerHandle,ref dataHandle,data.Length);
                    m_pPlayItems.Add(item);

                    m_BytesBuffered += data.Length;

                    // We ran out of minimum buffer, we must pause playing while min buffer filled.
                    if(m_BytesBuffered < 1000){
                        if(!m_IsPaused){
                            WavMethods.waveOutPause(m_pWavDevHandle);
                            m_IsPaused = true;
                        }
                    }
                    // Buffering completed,we may resume playing.
                    else if(m_IsPaused && m_BytesBuffered > m_MinBuffer){
                        WavMethods.waveOutRestart(m_pWavDevHandle);
                        m_IsPaused = false;
                    }
                                        
                    result = WavMethods.waveOutWrite(m_pWavDevHandle,headerHandle.AddrOfPinnedObject(),Marshal.SizeOf(wavHeader));
                }
                else{
                    dataHandle.Free();
                    headerHandle.Free();
                }
                //--------------------------------------------------------------------------------------------------
            }

            #endregion


            #region Properties Implementation

            /// <summary>
            /// Gets all available output audio devices.
            /// </summary>
            public static AudioOutDevice[] Devices
            {
                get{
                    List<AudioOutDevice> retVal = new List<AudioOutDevice>();
                    // Get all available output devices and their info.
                    int devicesCount = WavMethods.waveOutGetNumDevs();
                    for(int i=0;i<devicesCount;i++){
                        WAVEOUTCAPS pwoc = new WAVEOUTCAPS();
                        if(WavMethods.waveOutGetDevCaps((uint)i,ref pwoc,Marshal.SizeOf(pwoc)) == MMSYSERR.NOERROR){
                            retVal.Add(new AudioOutDevice(i,pwoc.szPname,pwoc.wChannels));
                        }
                    }

                    return retVal.ToArray(); 
                }
            }


            /// <summary>
            /// Gets if this object is disposed.
            /// </summary>
            public bool IsDisposed
            {
                get{ return m_IsDisposed; }
            }

            /// <summary>
            /// Gets if wav player is currently playing something.
            /// </summary>
            /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
            public bool IsPlaying
            {
                get{
                    if(m_IsDisposed){
                        throw new ObjectDisposedException("WaveOut");
                    }
                
                    if(m_pPlayItems.Count > 0){
                        return true;
                    }
                    else{
                        return false;
                    }
                }
            }

            /// <summary>
            /// Gets or sets volume level. Value 0 is mute and value 100 is maximum.
            /// </summary>
            /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
            public int Volume
            {
                get{ 
                    int volume = 0;
                    WavMethods.waveOutGetVolume(m_pWavDevHandle,out volume);

                    ushort left  = (ushort)(volume & 0x0000ffff);
                    ushort right = (ushort)(volume >> 16);

                    return (int)(left / (0xFFFF / 100.0));
                }

                set{
                    if(value < 0 || value > 100){
                        throw new ArgumentException("Property 'Volume' value must be >=0 and <= 100.");
                    }

                    int level = (int)(value * (0xFFFF / 100.0));

                    WavMethods.waveOutSetVolume(m_pWavDevHandle,(level << 16 | level & 0xFFFF));
                }
            }

            /// <summary>
            /// Gets number of bytes buffered for playing.
            /// </summary>
            public int BytesBuffered
            {
                get{ return m_BytesBuffered; }
            }

            #endregion
        }

        #endregion

        // TODO: Linux WaveOut similar PCM audio player.

        private bool           m_IsDisposed    = false;
        private AudioOutDevice m_pDevice       = null;
        private AudioFormat    m_pAudioFormat  = null;
        private WaveOut        m_pWaveOut      = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="device">Audio output device.</param>
        /// <param name="format">Audio output format.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>device</b> or <b>format</b> is null reference.</exception>
        public AudioOut(AudioOutDevice device,AudioFormat format)
        {
            if(device == null){
                throw new ArgumentNullException("device");
            }
            if(format == null){
                throw new ArgumentNullException("format");
            }

            m_pDevice      = device;
            m_pAudioFormat = format;

            m_pWaveOut = new WaveOut(device,format.SamplesPerSecond,format.BitsPerSample,format.Channels);
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="device">Audio output device.</param>
        /// <param name="samplesPerSec">Sample rate, in samples per second (hertz).</param>
        /// <param name="bitsPerSample">Bits per sample. For PCM 8 or 16 are the only valid values.</param>
        /// <param name="channels">Number of channels.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>device</b> is null reference.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the arguments has invalid value.</exception>
        public AudioOut(AudioOutDevice device,int samplesPerSec,int bitsPerSample,int channels)
        {
            if(device == null){
                throw new ArgumentNullException("device");
            }
            if(samplesPerSec < 1){
                throw new ArgumentException("Argument 'samplesPerSec' value must be >= 1.","samplesPerSec");
            }
            if(bitsPerSample < 8){
                throw new ArgumentException("Argument 'bitsPerSample' value must be >= 8.","bitsPerSample");
            }
            if(channels < 1){
                throw new ArgumentException("Argument 'channels' value must be >= 1.","channels");
            }

            m_pDevice      = device;
            m_pAudioFormat = new AudioFormat(samplesPerSec,bitsPerSample,channels);

            m_pWaveOut = new WaveOut(device,samplesPerSec,bitsPerSample,channels);
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

            m_pWaveOut.Dispose();
            m_pWaveOut = null;
        }

        #endregion


        #region method Write

        /// <summary>
        /// Writes specified audio data bytes to the active audio device. If player is currently playing, data will be queued for playing.
        /// </summary>
        /// <param name="buffer">Data buffer.</param>
        /// <param name="offset">Offset int the <b>buffer</b>.</param>
        /// <param name="count">Number of bytes available in the <b>buffer</b>. Data boundary must n * BlockSize.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>buffer</b> is null reference.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Is raised when any of the argument value is out of allowed range.</exception>
        public void Write(byte[] buffer,int offset,int count)
        {
            if(buffer == null){
                throw new ArgumentNullException("buffer");
            }
            if(offset < 0 || offset > buffer.Length){
                throw new ArgumentOutOfRangeException("offset");
            }
            if(count < 0 || count > (buffer.Length + offset)){
                throw new ArgumentOutOfRangeException("count");
            }
            if((count % this.BlockSize) != 0){
                throw new ArgumentOutOfRangeException("count","Argument 'count' is not n * BlockSize.");
            }

            m_pWaveOut.Play(buffer,offset,count);
        }

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets all available audio output devices.
        /// </summary>
        public static AudioOutDevice[] Devices
        {
            get{ return WaveOut.Devices; }
        }


        /// <summary>
        /// Gets audio output device where audio is outputed.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public AudioOutDevice OutputDevice
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pDevice; 
            }
        }

        /// <summary>
        /// Gets audio format.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public AudioFormat AudioFormat
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
 
                return m_pAudioFormat; 
            }
        }

        /// <summary>
        /// Gets number of samples per second.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int SamplesPerSec
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
 
                return m_pAudioFormat.SamplesPerSecond; 
            }
        }

        /// <summary>
        /// Gets number of bits per sample.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int BitsPerSample
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
            
                return m_pAudioFormat.BitsPerSample; 
            }
        }

        /// <summary>
        /// Gets number of channels.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int Channels
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
             
                return m_pAudioFormat.Channels; 
            }
        }

        /// <summary>
        /// Gets one sample block size in bytes (nChannels * (bitsPerSample / 8)).
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int BlockSize
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pAudioFormat.Channels * (m_pAudioFormat.BitsPerSample / 8); 
            }
        }

        /// <summary>
        /// Gets or sets volume level. Value 0 is mute and value 100 is maximum.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        /// <exception cref="ArgumentException">Is raised when invalid value is passed.</exception>
        public int Volume
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
 
                return m_pWaveOut.Volume; 
            }

            set{
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }
                if(value < 0 || value > 100){
                    throw new ArgumentException("Property 'Volume' value must be >=0 and <= 100.");
                }

                m_pWaveOut.Volume = value;
            }
        }

        /// <summary>
        /// Gets number of bytes buffered for playing.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this class is Disposed and this property is accessed.</exception>
        public int BytesBuffered
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException(this.GetType().Name);
                }

                return m_pWaveOut.BytesBuffered; 
            }
        }

        #endregion

    }
}
