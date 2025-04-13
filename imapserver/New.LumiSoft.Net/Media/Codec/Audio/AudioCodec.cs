using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Media.Codec.Audio
{
    /// <summary>
    /// This class is base calss for audio codecs.
    /// </summary>
    public abstract class AudioCodec : Codec
    {
        #region Properties implementation

        /// <summary>
        /// Gets uncompressed audio format info.
        /// </summary>
        public abstract AudioFormat AudioFormat
        {
            get;
        }

        /// <summary>
        /// Gets compressed audio format info.
        /// </summary>
        public abstract AudioFormat CompressedAudioFormat
        {
            get;
        }
        
        #endregion
    }
}
