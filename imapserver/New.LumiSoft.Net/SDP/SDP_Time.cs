using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.SDP
{
    /// <summary>
    /// A SDP_Time represents an <B>t=</B> SDP message field. Defined in RFC 4566 5.9. Timing.
    /// </summary>
    public class SDP_Time
    {
        private long m_StartTime = 0;
        private long m_StopTime  = 0;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="startTime">Start time when session must start. Network Time Protocol (NTP) time values in 
        /// seconds since 1900, 0 value means not specified.</param>
        /// <param name="stopTime">Stop time when session must end.Network Time Protocol (NTP) time values in 
        /// seconds since 1900, 0 value means not specified.</param>
        public SDP_Time(long startTime,long stopTime)
        {
            if(startTime < 0){
                throw new ArgumentException("Argument 'startTime' value must be >= 0.");
            }
            if(stopTime < 0){
                throw new ArgumentException("Argument 'stopTime' value must be >= 0.");
            }

            m_StartTime = startTime;
            m_StopTime  = stopTime;
        }


        #region static method Parse

        /// <summary>
        /// Parses media from "t" SDP message field.
        /// </summary>
        /// <param name="tValue">"t" SDP message field.</param>
        /// <returns></returns>
        public static SDP_Time Parse(string tValue)
        {
            // t=<start-time> <stop-time>
            
            long startTime = 0;
            long endTime   = 0;
 
            // Remove t=
            StringReader r = new StringReader(tValue);
            r.QuotedReadToDelimiter('=');

            //--- <start-time> ------------------------------------------------------------
            string word = r.ReadWord();
            if(word == null){
                throw new Exception("SDP message \"t\" field <start-time> value is missing !");
            }
            startTime = Convert.ToInt64(word);

            //--- <stop-time> -------------------------------------------------------------
            word = r.ReadWord();
            if(word == null){
                throw new Exception("SDP message \"t\" field <stop-time> value is missing !");
            }
            endTime = Convert.ToInt64(word);

            return new SDP_Time(startTime,endTime);
        }

        #endregion

        #region method ToValue

        /// <summary>
        /// Converts this to valid "t" string.
        /// </summary>
        /// <returns></returns>
        public string ToValue()
        {
            // t=<start-time> <stop-time>

            return "t=" + StartTime + " " + StopTime + "\r\n";
        }

        #endregion


        #region Properties Implementation

        /// <summary>
        /// Gets or sets start time when session must start. Network Time Protocol (NTP) time values in 
        /// seconds since 1900. 0 value means not specified, if StopTime is also 0, then means infinite session.
        /// </summary>
        public long StartTime
        {
            get{ return m_StartTime; }

            set{
                if(value < 0){
                    throw new ArgumentException("Property StartTime value must be >= 0 !");
                }

                m_StopTime = value;
            }
        }

        /// <summary>
        /// Gets or sets stop time when session must end. Network Time Protocol (NTP) time values in 
        /// seconds since 1900. 0 value means not specified, if StopTime is also 0, then means infinite session.
        /// </summary>
        public long StopTime
        {
            get{ return m_StopTime; }

            set{
                if(value < 0){
                    throw new ArgumentException("Property StopTime value must be >= 0 !");
                }

                m_StopTime = value;
            }
        }

        #endregion

    }
}
