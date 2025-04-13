using System;
using System.Collections.Generic;
using System.Text;
using System.Runtime.InteropServices;
using System.Threading;

namespace LumiSoft.Net.Media
{
    /// <summary>
    /// This class implements streaming microphone wav data receiver.
    /// </summary>
    internal class _WaveIn
    {
        /// <summary>
        /// The waveInProc function is the callback function used with the waveform-audio input device.
        /// </summary>
        /// <param name="hdrvr">Handle to the waveform-audio device associated with the callback.</param>
        /// <param name="uMsg">Waveform-audio input message.</param>
        /// <param name="dwUser">User-instance data specified with waveOutOpen.</param>
        /// <param name="dwParam1">Message parameter.</param>
        /// <param name="dwParam2">Message parameter.</param>
        private delegate void waveInProc(IntPtr hdrvr,int uMsg,IntPtr dwUser,IntPtr dwParam1,IntPtr dwParam2);

        #region Wave in methods

        /// <summary>
        /// The waveInAddBuffer function sends an input buffer to the given waveform-audio input device. When the buffer is filled, the application is notified.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure that identifies the buffer.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
	    [DllImport("winmm.dll")]
	    private static extern int waveInAddBuffer(IntPtr hWaveOut,IntPtr lpWaveOutHdr,int uSize);

        /// <summary>
        /// Closes the specified waveform input device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device. If the function succeeds, the handle is no longer valid after this call.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
	    private static extern int waveInClose(IntPtr hWaveOut);

        /// <summary>
        /// Queries a specified waveform device to determine its capabilities.
        /// </summary>
        /// <param name="hwo">Identifier of the waveform-audio input device. It can be either a device identifier or a Handle to an open waveform-audio output device.</param>
        /// <param name="pwoc">Pointer to a WAVEOUTCAPS structure to be filled with information about the capabilities of the device.</param>
        /// <param name="cbwoc">Size, in bytes, of the WAVEOUTCAPS structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
        private static extern uint waveInGetDevCaps(uint hwo,ref WAVEOUTCAPS pwoc,int cbwoc);

        /// <summary>
        /// Get the waveInGetNumDevs function returns the number of waveform-audio input devices present in the system.
        /// </summary>
        /// <returns>Returns the waveInGetNumDevs function returns the number of waveform-audio input devices present in the system.
        /// </returns>
        [DllImport("winmm.dll")]
        private static extern int waveInGetNumDevs();

        /// <summary>
        /// The waveInOpen function opens the given waveform-audio input device for recording.
        /// </summary>
        /// <param name="hWaveOut">Pointer to a buffer that receives a handle identifying the open waveform-audio input device.</param>
        /// <param name="uDeviceID">Identifier of the waveform-audio input device to open. It can be either a device identifier or a handle of an open waveform-audio input device. You can use the following flag instead of a device identifier.</param>
        /// <param name="lpFormat">Pointer to a WAVEFORMATEX structure that identifies the desired format for recording waveform-audio data. You can free this structure immediately after waveInOpen returns.</param>
        /// <param name="dwCallback">Pointer to a fixed callback function, an event handle, a handle to a window, 
        /// or the identifier of a thread to be called during waveform-audio recording to process messages related 
        /// to the progress of recording. If no callback function is required, this value can be zero. 
        /// For more information on the callback function, see waveInProc.</param>
        /// <param name="dwInstance">User-instance data passed to the callback mechanism.</param>
        /// <param name="dwFlags">Flags for opening the device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
		[DllImport("winmm.dll")]
		private static extern int waveInOpen(out IntPtr hWaveOut,int uDeviceID,WAVEFORMATEX lpFormat,waveInProc dwCallback,int dwInstance,int dwFlags);

        /// <summary>
        /// Prepares a waveform data block for recording.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure that identifies the data block to be prepared. 
        /// The buffer's base address must be aligned with the respect to the sample size.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
		[DllImport("winmm.dll")]
		private static extern int waveInPrepareHeader(IntPtr hWaveOut,IntPtr lpWaveOutHdr,int uSize);

        /// <summary>
        /// Stops input on a specified waveform output device and resets the current position to 0.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		private static extern int waveInReset(IntPtr hWaveOut);

        /// <summary>
        /// Starts input on the given waveform-audio input device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		private static extern int waveInStart(IntPtr hWaveOut);

        /// <summary>
        /// Stops input on the given waveform-audio input device.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
        [DllImport("winmm.dll")]
		private static extern int waveInStop(IntPtr hWaveOut);

        /// <summary>
        /// Cleans up the preparation performed by waveInPrepareHeader.
        /// </summary>
        /// <param name="hWaveOut">Handle to the waveform-audio input device.</param>
        /// <param name="lpWaveOutHdr">Pointer to a WAVEHDR structure identifying the data block to be cleaned up.</param>
        /// <param name="uSize">Size, in bytes, of the WAVEHDR structure.</param>
        /// <returns>Returns value of MMSYSERR.</returns>
		[DllImport("winmm.dll")]
		private static extern int waveInUnprepareHeader(IntPtr hWaveOut,IntPtr lpWaveOutHdr,int uSize);

        #endregion
            
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

            public const int WAVE_FORMAT_DIRECT = 0x00008;

            public const int WAVERR_STILLPLAYING = 0x21;

            public const int WHDR_DONE = 0x00000001;
            public const int WHDR_PREPARED = 0x00000002;
            public const int WHDR_BEGINLOOP = 0x00000004;
            public const int WHDR_ENDLOOP = 0x00000008;
            public const int WHDR_INQUEUE = 0x00000010;
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

        #region class WAVEHDR

        /// <summary>
        /// This class represents WAVEHDR structure.
        /// </summary>
        [StructLayout(LayoutKind.Sequential)]
        private class WAVEHDR
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

        #region class WavFormat

        /// <summary>
        /// This class holds most known wav compression formats.
        /// </summary>
        internal class WavFormat
        {
            public const int PCM = 0x0001;
            /*
            public const int ADPCM = 0x0002;
            public const int ALAW = 0x0006;
            public const int MULAW = 0x0007;
            public const int G723_ADPCM = 0x0014; 
            public const int GSM610 = 0x0031;
            public const int G721_ADPCM = 0x0040;
            public const int G726_ADPCM = 0x0064; 
            public const int G722_ADPCM =  0x006;         
            public const int G729A = 0x0083;*/
        }

        #endregion

        #region class BufferItem

        /// <summary>
        /// This class holds queued recording buffer.
        /// </summary>
        private class BufferItem
        {
            private IntPtr            m_WavDevHandle = IntPtr.Zero;
            private WAVEHDR           m_Header;
            private byte[]            m_pBuffer = null;
            private GCHandle          m_ThisHandle;
            private GCHandle          m_HeaderHandle;
            private GCHandle          m_DataHandle;
            private int               m_DataSize = 0;  
            private EventArgs<byte[]> m_pEventArgs = null;

            /// <summary>
            /// Default constructor.
            /// </summary>
            /// <param name="wavDevHandle">Wave in device handle.</param>
            /// <param name="dataSize">Data buffer size in bytes.</param>
            public BufferItem(IntPtr wavDevHandle,int dataSize)
            {
                m_WavDevHandle = wavDevHandle;

                m_ThisHandle = GCHandle.Alloc(this);

                m_pBuffer = new byte[dataSize];
                m_DataHandle = GCHandle.Alloc(m_pBuffer,GCHandleType.Pinned);

                m_Header = new WAVEHDR();
                m_Header.lpData          = m_DataHandle.AddrOfPinnedObject();
                m_Header.dwBufferLength  = (uint)dataSize;
                m_Header.dwBytesRecorded = 0;
                m_Header.dwUser          = (IntPtr)m_ThisHandle;
                m_Header.dwFlags         = 0;
                m_Header.dwLoops         = 0;
                m_Header.lpNext          = IntPtr.Zero;
                m_Header.reserved        = 0;

                m_HeaderHandle = GCHandle.Alloc(m_Header,GCHandleType.Pinned);

                m_pEventArgs = new EventArgs<byte[]>(m_pBuffer);
            }

            #region method Dispose

            /// <summary>
            /// Cleans up any resources being used.
            /// </summary>
            public void Dispose()
            {
                waveInUnprepareHeader(m_WavDevHandle,m_HeaderHandle.AddrOfPinnedObject(),Marshal.SizeOf(m_Header));

                m_ThisHandle.Free();
                m_HeaderHandle.Free();
                m_DataHandle.Free();

                m_pEventArgs = null;
            }

            #endregion


            #region method Queue

            /// <summary>
            /// Queues buffer for recording.
            /// </summary>
            /// <param name="unprepare">If true unprepares old header before adding to recording queue.</param>
            internal void Queue(bool unprepare)
            {   
                m_Header.dwFlags = 0;
          
                int result = 0;        
                if(unprepare){
                    result = waveInUnprepareHeader(m_WavDevHandle,m_HeaderHandle.AddrOfPinnedObject(),Marshal.SizeOf(m_Header));
                    if(result != MMSYSERR.NOERROR){
                        throw new Exception("Error unpreparing wave in buffer, error: " + result + ".");
                    }
                }
                result = waveInPrepareHeader(m_WavDevHandle,m_HeaderHandle.AddrOfPinnedObject(),Marshal.SizeOf(m_Header));
                if(result != MMSYSERR.NOERROR){
                    throw new Exception("Error preparing wave in buffer, error: " + result + ".");
                }
                else{  
                    result = waveInAddBuffer(m_WavDevHandle,m_HeaderHandle.AddrOfPinnedObject(),Marshal.SizeOf(m_Header));
                    if(result != MMSYSERR.NOERROR){
                        throw new Exception("Error adding wave in buffer, error: " + result + ".");
                    }
                }
            }

            #endregion


            #region Properties Implementation
            
            /// <summary>
            /// Gets wave header.
            /// </summary>
            public WAVEHDR Header
            {
                get{ return m_Header; }
            }
                 
            /// <summary>
            /// Gets header handle.
            /// </summary>
            public GCHandle HeaderHandle
            {
                get{ return m_HeaderHandle; }
            }

            /// <summary>
            /// Gets wav header data.
            /// </summary>
            public byte[] Data
            {
                get{ return m_pBuffer; }
            }

            /// <summary>
            /// Gets wav header data size in bytes.
            /// </summary>
            public int DataSize
            {
                get{ return m_DataSize; }
            }

            /// <summary>
            /// Gets event args.
            /// </summary>
            public EventArgs<byte[]> EventArgs
            {
                get{ return m_pEventArgs; }
            }

            #endregion
        }

        #endregion

        private bool                         m_IsDisposed    = false;
        private AudioInDevice                m_pInDevice     = null;
        private int                          m_SamplesPerSec = 8000;
        private int                          m_BitsPerSample = 8;
        private int                          m_Channels      = 1;
        private int                          m_BufferSize    = 400;
        private IntPtr                       m_pWavDevHandle = IntPtr.Zero;
        private int                          m_BlockSize     = 0;
        private Dictionary<long,BufferItem>  m_pBuffers      = null;
        private waveInProc                   m_pWaveInProc   = null;
        private bool                         m_IsRecording   = false;
        private object                       m_pLock         = new object();
            
        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="device">Input device.</param>
        /// <param name="samplesPerSec">Sample rate, in samples per second (hertz). For PCM common values are 
        /// 8.0 kHz, 11.025 kHz, 22.05 kHz, and 44.1 kHz.</param>
        /// <param name="bitsPerSample">Bits per sample. For PCM 8 or 16 are the only valid values.</param>
        /// <param name="channels">Number of channels.</param>
        /// <param name="bufferSize">Specifies recording buffer size.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>outputDevice</b> is null.</exception>
        /// <exception cref="ArgumentException">Is raised when any of the aruments has invalid value.</exception>
        public _WaveIn(AudioInDevice device,int samplesPerSec,int bitsPerSample,int channels,int bufferSize)
        {
            if(device == null){
                throw new ArgumentNullException("device");
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

            m_pInDevice     = device;
            m_SamplesPerSec = samplesPerSec;
            m_BitsPerSample = bitsPerSample;
            m_Channels      = channels;
            m_BufferSize    = bufferSize;
            m_BlockSize     = m_Channels * (m_BitsPerSample / 8);
            m_pBuffers      = new Dictionary<long,BufferItem>();

            // Try to open wav device.            
            WAVEFORMATEX format = new WAVEFORMATEX();
            format.wFormatTag      = WavFormat.PCM;
            format.nChannels       = (ushort)m_Channels;
            format.nSamplesPerSec  = (uint)samplesPerSec;                        
            format.nAvgBytesPerSec = (uint)(m_SamplesPerSec * (m_Channels * (m_BitsPerSample / 8)));
            format.nBlockAlign     = (ushort)m_BlockSize;
            format.wBitsPerSample  = (ushort)m_BitsPerSample;
            format.cbSize          = 0; 
            // We must delegate reference, otherwise GC will collect it.
            m_pWaveInProc = new waveInProc(this.OnWaveInProc);
            
            int result = waveInOpen(out m_pWavDevHandle,m_pInDevice.Index,format,m_pWaveInProc,0,WavConstants.CALLBACK_FUNCTION);
            if(result != MMSYSERR.NOERROR){
                throw new Exception("Failed to open wav device, error: " + result.ToString() + ".");
            }

            CreateBuffers();
        }
        
        /// <summary>
        /// Default destructor.
        /// </summary>
        ~_WaveIn()
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
                // If recording, we need to reset wav device first.
                waveInReset(m_pWavDevHandle);
                
                // If there are unprepared wav headers, we need to unprepare these.
                foreach(BufferItem item in m_pBuffers.Values){
                    item.Dispose();
                }
                
                // Close input device.
                waveInClose(m_pWavDevHandle);

                m_pInDevice     = null;
                m_pWavDevHandle = IntPtr.Zero;

                this.AudioFrameReceived = null;
            }
            catch{                
            }
        }

        #endregion


        #region method Start

        /// <summary>
        /// Starts recording.
        /// </summary>
        public void Start()
        {
            if(m_IsRecording){
                return;
            }
            m_IsRecording = true;
                
            int result = waveInStart(m_pWavDevHandle);
            if(result != MMSYSERR.NOERROR){
                throw new Exception("Failed to start wav device, error: " + result + ".");
            }
        }

        #endregion

        #region method Stop

        /// <summary>
        /// Stops recording.
        /// </summary>
        public void Stop()
        {
            if(!m_IsRecording){
                return;
            }
            m_IsRecording = false;
            
            int result = waveInStop(m_pWavDevHandle);
            if(result != MMSYSERR.NOERROR){
                throw new Exception("Failed to stop wav device, error: " + result + ".");
            }
        }

        #endregion

                        
        #region method OnWaveInProc

        /// <summary>
        /// This method is called when wav device generates some event.
        /// </summary>
        /// <param name="hdrvr">Handle to the waveform-audio device associated with the callback.</param>
        /// <param name="uMsg">Waveform-audio input message.</param>
        /// <param name="dwUser">User-instance data specified with waveOutOpen.</param>
        /// <param name="dwParam1">Message parameter.</param>
        /// <param name="dwParam2">Message parameter.</param>
        private void OnWaveInProc(IntPtr hdrvr,int uMsg,IntPtr dwUser,IntPtr dwParam1,IntPtr dwParam2)
        {   
            // NOTE: MSDN warns, we may not call any wave related methods here.
            // This may cause deadlock. But MSDN examples do it, probably we can do it too.

            if(m_IsDisposed){
                return;
            }

            try{
                if(uMsg == WavConstants.MM_WIM_DATA){                            
                    // Free buffer and queue it for reuse.
                    //ThreadPool.QueueUserWorkItem(new WaitCallback(delegate(object state){
                    try{
                        if(m_IsDisposed){
                            return;
                        }

                        BufferItem bufferItem = m_pBuffers[dwParam1.ToInt64()];
                          
                        OnAudioFrameReceived(bufferItem.EventArgs);
                                       
                        bufferItem.Queue(true);
                    }
                    catch(Exception x){
                        Console.WriteLine(x.ToString());
                    }
                    //}));
                }
            }
            catch{
                // We don't care about errors here.
            }            
        }

        #endregion

        #region method CreateBuffers

        /// <summary>
        /// Fills recording buffers.
        /// </summary>
        private void CreateBuffers()
        {               
            while(m_pBuffers.Count < 10){
                BufferItem bufferItem = new BufferItem(m_pWavDevHandle,m_BufferSize);                    
                m_pBuffers.Add(bufferItem.HeaderHandle.AddrOfPinnedObject().ToInt64(),bufferItem);
                bufferItem.Queue(false);
            } 
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets all available input audio devices.
        /// </summary>
        public static AudioInDevice[] Devices
        {
            get{
                List<AudioInDevice> retVal = new List<AudioInDevice>();
                // Get all available output devices and their info.                
                int devicesCount = waveInGetNumDevs();
                for(int i=0;i<devicesCount;i++){
                    WAVEOUTCAPS pwoc = new WAVEOUTCAPS();
                    if(waveInGetDevCaps((uint)i,ref pwoc,Marshal.SizeOf(pwoc)) == MMSYSERR.NOERROR){
                        retVal.Add(new AudioInDevice(i,pwoc.szPname,pwoc.wChannels));
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
        /// Gets current input device.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public AudioInDevice InputDevice
        {
            get{
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WavRecorder");
                }

                return m_pInDevice; 
            }
        }

        /// <summary>
        /// Gets number of samples per second.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public int SamplesPerSec
        {
            get{                 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WavRecorder");
                }

                return m_SamplesPerSec; 
            }
        }

        /// <summary>
        /// Gets number of buts per sample.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public int BitsPerSample
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WavRecorder");
                }
                
                return m_BitsPerSample; 
            }
        }

        /// <summary>
        /// Gets number of channels.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public int Channels
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WavRecorder");
                }
                
                return m_Channels; 
            }
        }

        /// <summary>
        /// Gets recording buffer size.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public int BufferSize
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WavRecorder");
                }
                
                return m_BufferSize; 
            }
        }

        /// <summary>
        /// Gets one sample block size in bytes.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Is raised when this object is disposed and this property is accessed.</exception>
        public int BlockSize
        {
            get{ 
                if(m_IsDisposed){
                    throw new ObjectDisposedException("WavRecorder");
                }

                return m_BlockSize; 
            }
        }

        #endregion        

        #region Events implementation

        /// <summary>
        /// Is raised when wave-in device has received new audio frame.
        /// </summary>
        public event EventHandler<EventArgs<byte[]>> AudioFrameReceived = null;

        /// <summary>
        /// Raises <b>AudioFrameReceived</b> event.
        /// </summary>
        /// <param name="eArgs">Event arguments.</param>
        private void OnAudioFrameReceived(EventArgs<byte[]> eArgs)
        {
            if(this.AudioFrameReceived != null){
                this.AudioFrameReceived(this,eArgs);
            }
        }

        #endregion
    }
}
