using System;
using System.Collections.Generic;
using System.Text;

namespace LumiSoft.Net.Log
{
    /// <summary>
    /// Specifies log entry type.
    /// </summary>
    public enum LogEntryType
    {
        /// <summary>
        /// Read entry.
        /// </summary>
        Read,

        /// <summary>
        /// Write entry.
        /// </summary>
        Write,

        /// <summary>
        /// Text entry.
        /// </summary>
        Text,

        /// <summary>
        /// Exception entry.
        /// </summary>
        Exception,
    }
}
