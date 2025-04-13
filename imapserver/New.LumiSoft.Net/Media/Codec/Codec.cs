using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Media.Codec
{
    /// <summary>
    /// This class is base class for media codecs.
    /// </summary>
    public abstract class Codec
    {
        #region method Encode

        /// <summary>
        /// Encodes specified data block.
        /// </summary>
        /// <param name="buffer">Data to encode.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="count">Number of bytes to encode.</param>
        /// <returns>Returns encoded block.</returns>
        public abstract byte[] Encode(byte[] buffer,int offset,int count);

        #endregion

        #region method Decode

        /// <summary>
        /// Decodes specified data block.
        /// </summary>
        /// <param name="buffer">Data to encode.</param>
        /// <param name="offset">Offset in the buffer.</param>
        /// <param name="count">Number of bytes to decode.</param>
        /// <returns>Returns encoded data.</returns>
        public abstract byte[] Decode(byte[] buffer,int offset,int count);

        #endregion


        #region Properties implementation

        /// <summary>
        /// Gets codec name.
        /// </summary>
        public abstract string Name
        {
            get;
        }

        #endregion

    }
}
