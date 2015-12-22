using System;
using System.Collections.Generic;
using System.IO;

namespace Rnwood.SmtpServer
{
    public class FileMessage : IMessage
    {
        public FileMessage(FileInfo file, bool keepOnDispose)
        {
            _file = file;
            _keepOnDispose = keepOnDispose;
        }

        private readonly FileInfo _file;
        private readonly bool _keepOnDispose;

        public DateTime ReceivedDate
        {
            get; private set;
        }

        public ISession Session
        {
            get; private set;
        }

        public string From
        {
            get; private set;
        }

        private List<string> _to = new List<string>();

        public string[] To
        {
            get
            {
                return _to.ToArray();
            }
        }

        public bool SecureConnection
        {
            get; private set;
        }

        public bool EightBitTransport
        {
            get; private set;
        }

        public long? DeclaredMessageSize
        {
            get; private set;
        }

        public virtual void Dispose()
        {
            if (!_keepOnDispose)
            {
                if (_file.Exists)
                {
                    _file.Delete();
                }
            }
        }

        public Stream GetData()
        {
            return new FileStream(_file.FullName, FileMode.Open, FileAccess.Read, FileShare.Delete | FileShare.Read);
        }

        public class Builder : IMessageBuilder
        {
            public Builder(FileInfo file, bool keepOnDispose)
            {
                _message = new FileMessage(file, keepOnDispose);
            }

            private FileMessage _message;

            public ISession Session
            {
                get
                {
                    return _message.Session;
                }

                set
                {
                    _message.Session = value;
                }
            }

            public DateTime ReceivedDate
            {
                get
                {
                    return _message.ReceivedDate;
                }

                set
                {
                    _message.ReceivedDate = value;
                }
            }

            public string From
            {
                get
                {
                    return _message.From;
                }

                set
                {
                    _message.From = value;
                }
            }

            public ICollection<string> To
            {
                get
                {
                    return _message._to;
                }
            }

            public bool SecureConnection
            {
                get
                {
                    return _message.SecureConnection;
                }

                set
                {
                    _message.SecureConnection = value;
                }
            }

            public bool EightBitTransport
            {
                get
                {
                    return _message.EightBitTransport;
                }

                set
                {
                    EightBitTransport = value;
                }
            }

            public long? DeclaredMessageSize
            {
                get
                {
                    return _message.DeclaredMessageSize;
                }

                set
                {
                    _message.DeclaredMessageSize = value;
                }
            }

            public Stream GetData()
            {
                return _message.GetData();
            }

            public IMessage ToMessage()
            {
                return _message;
            }

            public Stream WriteData()
            {
                return _message._file.OpenWrite();
            }
        }
    }
}