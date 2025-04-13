using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Log
{
    /// <summary>
    /// This class provides data for <b>Logger.WriteLog</b> event.
    /// </summary>
    public class WriteLogEventArgs : EventArgs
    {
        private LogEntry m_pLogEntry = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="logEntry">New log entry.</param>
        /// <exception cref="ArgumentNullException">Is raised when <b>logEntry</b> is null.</exception>
        public WriteLogEventArgs(LogEntry logEntry)
        {
            if(logEntry == null){
                throw new ArgumentNullException("logEntry");
            }

            m_pLogEntry = logEntry;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets new log entry.
        /// </summary>
        public LogEntry LogEntry
        {
            get{ return m_pLogEntry; }
        }

        #endregion

    }
}
