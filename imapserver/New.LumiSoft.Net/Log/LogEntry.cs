using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Security.Principal;

namespace LumiSoft.Net.Log
{
    /// <summary>
    /// Implements log entry.
    /// </summary>
    public class LogEntry
    {
        private LogEntryType    m_Type          = LogEntryType.Text;
        private string          m_ID            = "";
        private DateTime        m_Time;
        private GenericIdentity m_pUserIdentity = null;
        private long            m_Size          = 0;
        private string          m_Text          = "";
        private Exception       m_pException    = null;
        private IPEndPoint      m_pLocalEP      = null;
        private IPEndPoint      m_pRemoteEP     = null;
        private byte[]          m_pData         = null;

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="type">Log entry type.</param>
        /// <param name="id">Log entry ID.</param>
        /// <param name="size">Specified how much data was readed or written.</param>
        /// <param name="text">Description text.</param>
        public LogEntry(LogEntryType type,string id,long size,string text)
        {
            m_Type = type;
            m_ID   = id;
            m_Size = size;
            m_Text = text;

            m_Time = DateTime.Now;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="type">Log entry type.</param>
        /// <param name="id">Log entry ID.</param>
        /// <param name="userIdentity">Log entry owner user or null if none.</param>
        /// <param name="size">Log entry read/write size in bytes.</param>
        /// <param name="text">Log text.</param>
        /// <param name="localEP">Local IP end point.</param>
        /// <param name="remoteEP">Remote IP end point.</param>
        /// <param name="data">Log data.</param>
        public LogEntry(LogEntryType type,string id,GenericIdentity userIdentity,long size,string text,IPEndPoint localEP,IPEndPoint remoteEP,byte[] data)
        {   
            m_Type          = type;
            m_ID            = id;
            m_pUserIdentity = userIdentity;
            m_Size          = size;
            m_Text          = text;
            m_pLocalEP      = localEP;
            m_pRemoteEP     = remoteEP;
            m_pData         = data;
                        
            m_Time = DateTime.Now;
        }

        /// <summary>
        /// Default constructor.
        /// </summary>
        /// <param name="type">Log entry type.</param>
        /// <param name="id">Log entry ID.</param>
        /// <param name="userIdentity">Log entry owner user or null if none.</param>
        /// <param name="size">Log entry read/write size in bytes.</param>
        /// <param name="text">Log text.</param>
        /// <param name="localEP">Local IP end point.</param>
        /// <param name="remoteEP">Remote IP end point.</param>
        /// <param name="exception">Exception happened. Can be null.</param>
        public LogEntry(LogEntryType type,string id,GenericIdentity userIdentity,long size,string text,IPEndPoint localEP,IPEndPoint remoteEP,Exception exception)
        {   
            m_Type          = type;
            m_ID            = id;
            m_pUserIdentity = userIdentity;
            m_Size          = size;
            m_Text          = text;
            m_pLocalEP      = localEP;
            m_pRemoteEP     = remoteEP;
            m_pException    = exception;
                        
            m_Time = DateTime.Now;
        }


        #region Properties Implementation

        /// <summary>
        /// Gets log entry type.
        /// </summary>
        public LogEntryType EntryType
        {
            get{ return m_Type; }
        }

        /// <summary>
        /// Gets log entry ID.
        /// </summary>
        public string ID
        {
            get{ return m_ID; }
        }

        /// <summary>
        /// Gets time when log entry was created.
        /// </summary>
        public DateTime Time
        {
            get{ return m_Time; }
        }

        /// <summary>
        /// Gets log entry related user identity.
        /// </summary>
        public GenericIdentity UserIdentity
        {
            get{ return m_pUserIdentity; }
        }

        /// <summary>
        /// Gets how much data was readed or written, depends on <b>LogEntryType</b>.
        /// </summary>
        public long Size
        {
            get{ return m_Size; }
        }

        /// <summary>
        /// Gets describing text.
        /// </summary>
        public string Text
        {
            get{ return m_Text; }
        }

        /// <summary>
        /// Gets exception happened. This property is available only if LogEntryType.Exception.
        /// </summary>
        public Exception Exception
        {
            get{ return m_pException; }
        }

        /// <summary>
        /// Gets local IP end point. Value null means no local end point.
        /// </summary>
        public IPEndPoint LocalEndPoint
        {
            get{ return m_pLocalEP; }
        }

        /// <summary>
        /// Gets remote IP end point. Value null means no remote end point.
        /// </summary>
        public IPEndPoint RemoteEndPoint
        {
            get{ return m_pRemoteEP; }
        }

        /// <summary>
        /// Gest log data. Value null means no log data.
        /// </summary>
        public byte[] Data
        {
            get{ return m_pData; }
        }

        #endregion

    }
}
