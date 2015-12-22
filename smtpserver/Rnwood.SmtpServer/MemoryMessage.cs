using System;
using System.Collections.Generic;
using System.IO;

namespace Rnwood.SmtpServer
{
    public class MemoryMessage : IMessage
    {
        public MemoryMessage()
        {
        }

        internal byte[] Data { get; set; }

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
            get { return _to.ToArray(); }
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
        }

        public Stream GetData()
        {
            return new MemoryStream(Data, false);
        }

        public class Builder : IMessageBuilder
        {
            public Builder() : this(new MemoryMessage())
            {
            }

            protected Builder(MemoryMessage message)
            {
                _message = message;
            }

            private MemoryMessage _message;

            public Stream WriteData()
            {
                CloseNotifyingMemoryStream stream = new CloseNotifyingMemoryStream();
                stream.Closing += (s, ea) =>
                {
                    _message.Data = stream.ToArray();
                };

                return stream;
            }

            internal class CloseNotifyingMemoryStream : MemoryStream
            {
                public event EventHandler Closing;

                public override void Close()
                {
                    if (Closing != null)
                    {
                        Closing(this, EventArgs.Empty);
                    }

                    base.Close();
                }
            }

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

            public ICollection<string> To
            {
                get
                {
                    return _message._to;
                }
            }

            public IMessage ToMessage()
            {
                return _message;
            }

            public Stream GetData()
            {
                return _message.GetData();
            }
        }
    }
}