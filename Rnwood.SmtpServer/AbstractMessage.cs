#region

using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace Rnwood.SmtpServer
{
    public abstract class AbstractMessage : IEditableMessage, IMessage
    {
        /// <summary>
        /// Creates a message associated with the specified session.
        /// </summary>
        /// <param name="session">The session on which the message is being received.</param>
        /// <returns></returns>
        public AbstractMessage(ISession session)
        {
            Session = session;
            ReceivedDate = DateTime.Now;
        }

        public bool SecureConnection { get; set; }

        public bool EightBitTransport
        {
            get;
            set;
        }

        public DateTime ReceivedDate { get; set; }

        public ISession Session { get; private set; }

        public string From { get; set; }

        private List<string> _toList = new List<string>();

        public void AddTo(string to)
        {
            _toList.Add(to);
        }

        public string[] To
        {
            get { return _toList.ToArray(); }
        }

        public long? DeclaredMessageSize
        {
            get;
            set;
        }

        public Stream GetData()
        {
            return GetData(DataAccessMode.ForReading);
        }

        public abstract Stream GetData(DataAccessMode dataAccessMode);

        public abstract void Dispose();
    }
}