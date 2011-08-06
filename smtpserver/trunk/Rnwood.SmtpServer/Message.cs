#region

using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace Rnwood.SmtpServer
{
    public abstract class AbstractMessage : IEditableMessage
    {
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
            return GetData(false);
        }

        public abstract Stream GetData(bool forWriting);
        public abstract void Dispose();
    }
}