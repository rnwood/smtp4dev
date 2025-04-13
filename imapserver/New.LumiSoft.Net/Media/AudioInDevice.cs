using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Media
{
    /// <summary>
    /// This class represents audio input device(microphone,line-in, ....).
    /// </summary>
    public class AudioInDevice
    {
        private int    m_Index    = 0;
        private string m_Name     = "";
        private int    m_Channels = 1;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="index">Device index in devices.</param>
        /// <param name="name">Device name.</param>
        /// <param name="channels">Number of audio channels.</param>
        internal AudioInDevice(int index,string name,int channels)
        {
            m_Index    = index;
            m_Name     = name;
            m_Channels = channels;
        }


        #region Properties implementation

        /// <summary>
        /// Gets device name.
        /// </summary>
        public string Name
        {
            get{ return m_Name; }
        }

        /// <summary>
        /// Gets number of input channels(mono,stereo,...) supported.
        /// </summary>
        public int Channels
        {
            get{ return m_Channels; }
        }


        /// <summary>
        /// Gets device index in devices.
        /// </summary>
        internal int Index
        {
            get{ return m_Index; }
        }

        #endregion

    }
}
