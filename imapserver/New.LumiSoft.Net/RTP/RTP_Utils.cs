using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.RTP
{
    /// <summary>
    /// This class provides utility methods for RTP.
    /// </summary>
    public class RTP_Utils
    {
        #region static method GenerateSSRC

        /// <summary>
        /// Generates random SSRC value.
        /// </summary>
        /// <returns>Returns random SSRC value.</returns>
        public static uint GenerateSSRC()
        {
            return (uint)new Random().Next(100000,int.MaxValue);
        }

        #endregion

        #region static method GenerateCNAME

        /// <summary>
        /// Generates random CNAME value.
        /// </summary>
        /// <returns></returns>
        public static string GenerateCNAME()
        {
            // user@host.randomTag

            return Environment.UserName + "@" + System.Net.Dns.GetHostName() + "." + Guid.NewGuid().ToString().Substring(0,8);
        }

        #endregion

        #region static method DateTimeToNTP32

        /// <summary>
        /// Converts specified DateTime value to short NTP time. Note: NTP time is in UTC.
        /// </summary>
        /// <param name="value">DateTime value to convert. This value must be in local time.</param>
        /// <returns>Returns NTP value.</returns>
        public static uint DateTimeToNTP32(DateTime value)
        {
            /*
                In some fields where a more compact representation is
                appropriate, only the middle 32 bits are used; that is, the low 16
                bits of the integer part and the high 16 bits of the fractional part.
                The high 16 bits of the integer part must be determined
                independently.
            */

            return (uint)((DateTimeToNTP64(value) >> 16) & 0xFFFFFFFF);
        }

        #endregion

        #region static method DateTimeToNTP64

        /// <summary>
        /// Converts specified DateTime value to long NTP time. Note: NTP time is in UTC.
        /// </summary>
        /// <param name="value">DateTime value to convert. This value must be in local time.</param>
        /// <returns>Returns NTP value.</returns>
        public static ulong DateTimeToNTP64(DateTime value)
        {
            /*
                Wallclock time (absolute date and time) is represented using the
                timestamp format of the Network Time Protocol (NTP), which is in
                seconds relative to 0h UTC on 1 January 1900 [4].  The full
                resolution NTP timestamp is a 64-bit unsigned fixed-point number with
                the integer part in the first 32 bits and the fractional part in the
                last 32 bits. In some fields where a more compact representation is
                appropriate, only the middle 32 bits are used; that is, the low 16
                bits of the integer part and the high 16 bits of the fractional part.
                The high 16 bits of the integer part must be determined
                independently.
            */

            TimeSpan ts = ((TimeSpan)(value.ToUniversalTime() - new DateTime(1900,1,1,0,0,0)));
                       
            return ((ulong)(ts.TotalMilliseconds % 1000) << 32) | (uint)(ts.Milliseconds << 22);
        }

        #endregion
    }
}
