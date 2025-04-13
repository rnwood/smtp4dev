using System;
using System.Collections.Generic;
using System.Text;
using System.Net;
using System.Security.Principal;

namespace LumiSoft.Net.Log
{
    /// <summary>
    /// General logging module.
    /// </summary>
    public class Logger : IDisposable
    {    
        /// <summary>
        /// Default constructor.
        /// </summary>
        public Logger()
        {
        }

        #region method Dispose

        /// <summary>
        /// Cleans up any resources being used.
        /// </summary>
        public void Dispose()
        {
        }

        #endregion


        #region method AddRead

        /// <summary>
        /// Adds read log entry.
        /// </summary>
        /// <param name="size">Readed data size in bytes.</param>
        /// <param name="text">Log text.</param>
        public void AddRead(long size,string text)
        {
            OnWriteLog(new LogEntry(LogEntryType.Read,"",size,text));
        }

        /// <summary>
        /// Adds read log entry.
        /// </summary>
        /// <param name="id">Log entry ID.</param>
        /// <param name="size">Readed data size in bytes.</param>
        /// <param name="text">Log text.</param>
        /// <param name="userIdentity">Authenticated user identity.</param>
        /// <param name="localEP">Local IP endpoint.</param>
        /// <param name="remoteEP">Remote IP endpoint.</param>
        public void AddRead(string id,GenericIdentity userIdentity,long size,string text,IPEndPoint localEP,IPEndPoint remoteEP)
        {
            OnWriteLog(new LogEntry(LogEntryType.Read,id,userIdentity,size,text,localEP,remoteEP,(byte[])null));
        }

        /// <summary>
        /// Adds read log entry.
        /// </summary>
        /// <param name="id">Log entry ID.</param>
        /// <param name="size">Readed data size in bytes.</param>
        /// <param name="text">Log text.</param>
        /// <param name="userIdentity">Authenticated user identity.</param>
        /// <param name="localEP">Local IP endpoint.</param>
        /// <param name="remoteEP">Remote IP endpoint.</param>
        /// <param name="data">Log data.</param>
        public void AddRead(string id,GenericIdentity userIdentity,long size,string text,IPEndPoint localEP,IPEndPoint remoteEP,byte[] data)
        {
            OnWriteLog(new LogEntry(LogEntryType.Read,id,userIdentity,size,text,localEP,remoteEP,data));
        }

        #endregion

        #region method AddWrite

        /// <summary>
        /// Add write log entry.
        /// </summary>
        /// <param name="size">Written data size in bytes.</param>
        /// <param name="text">Log text.</param>
        public void AddWrite(long size,string text)
        {
            OnWriteLog(new LogEntry(LogEntryType.Write,"",size,text));
        }

        /// <summary>
        /// Add write log entry.
        /// </summary>
        /// <param name="id">Log entry ID.</param>
        /// <param name="size">Written data size in bytes.</param>
        /// <param name="text">Log text.</param>
        /// <param name="userIdentity">Authenticated user identity.</param>
        /// <param name="localEP">Local IP endpoint.</param>
        /// <param name="remoteEP">Remote IP endpoint.</param>
        public void AddWrite(string id,GenericIdentity userIdentity,long size,string text,IPEndPoint localEP,IPEndPoint remoteEP)
        {
            OnWriteLog(new LogEntry(LogEntryType.Write,id,userIdentity,size,text,localEP,remoteEP,(byte[])null));
        }

        /// <summary>
        /// Add write log entry.
        /// </summary>
        /// <param name="id">Log entry ID.</param>
        /// <param name="size">Written data size in bytes.</param>
        /// <param name="text">Log text.</param>
        /// <param name="userIdentity">Authenticated user identity.</param>
        /// <param name="localEP">Local IP endpoint.</param>
        /// <param name="remoteEP">Remote IP endpoint.</param>
        /// <param name="data">Log data.</param>
        public void AddWrite(string id,GenericIdentity userIdentity,long size,string text,IPEndPoint localEP,IPEndPoint remoteEP,byte[] data)
        {
            OnWriteLog(new LogEntry(LogEntryType.Write,id,userIdentity,size,text,localEP,remoteEP,data));
        }

        #endregion

        #region method AddText

        /// <summary>
        /// Adds text entry.
        /// </summary>
        /// <param name="text">Log text.</param>
        public void AddText(string text)
        {
            OnWriteLog(new LogEntry(LogEntryType.Text,"",0,text));
        }

        /// <summary>
        /// Adds text entry.
        /// </summary>
        /// <param name="id">Log entry ID.</param>
        /// <param name="text">Log text.</param>
        public void AddText(string id,string text)
        {
            OnWriteLog(new LogEntry(LogEntryType.Text,id,0,text));
        }

        /// <summary>
        /// Adds text entry.
        /// </summary>
        /// <param name="id">Log entry ID.</param>
        /// <param name="text">Log text.</param>
        /// <param name="userIdentity">Authenticated user identity.</param>
        /// <param name="localEP">Local IP endpoint.</param>
        /// <param name="remoteEP">Remote IP endpoint.</param>
        public void AddText(string id,GenericIdentity userIdentity,string text,IPEndPoint localEP,IPEndPoint remoteEP)
        {
            OnWriteLog(new LogEntry(LogEntryType.Text,id,userIdentity,0,text,localEP,remoteEP,(byte[])null));
        }

        #endregion

        #region method AddException

        /// <summary>
        /// Adds exception entry.
        /// </summary>
        /// <param name="id">Log entry ID.</param>
        /// <param name="text">Log text.</param>
        /// <param name="userIdentity">Authenticated user identity.</param>
        /// <param name="localEP">Local IP endpoint.</param>
        /// <param name="remoteEP">Remote IP endpoint.</param>
        /// <param name="exception">Exception happened.</param>
        public void AddException(string id,GenericIdentity userIdentity,string text,IPEndPoint localEP,IPEndPoint remoteEP,Exception exception)
        {
            OnWriteLog(new LogEntry(LogEntryType.Exception,id,userIdentity,0,text,localEP,remoteEP,exception));
        }

        #endregion


        #region Properties Implementation

        #endregion
        
        #region Events Implementation

        /// <summary>
        /// Is raised when new log entry is available.
        /// </summary>
        public event EventHandler<WriteLogEventArgs> WriteLog = null;

        #region method OnWriteLog

        /// <summary>
        /// Raises WriteLog event.
        /// </summary>
        /// <param name="entry">Log entry.</param>
        private void OnWriteLog(LogEntry entry)
        {
            if(this.WriteLog != null){
                this.WriteLog(this,new WriteLogEventArgs(entry));
            }
        }

        #endregion

        #endregion

    }
}
