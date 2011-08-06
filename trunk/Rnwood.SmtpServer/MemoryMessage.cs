using System;
using System.IO;

namespace Rnwood.SmtpServer
{
    public class MemoryMessage : AbstractMessage
    {
        public MemoryMessage(ISession session)
            : base(session)
        {
        }

        private byte[] _data;

        public override Stream GetData(bool forWriting)
        {
            if (forWriting)
            {
                CloseNotifyingMemoryStream stream = new CloseNotifyingMemoryStream();
                stream.Closing += (s, ea) =>
                                      {
                                          _data = new byte[stream.Length];
                                          stream.Position = 0;
                                          stream.Read(_data, 0, _data.Length);
                                      };

                return stream;
            }
            else
            {
                return new MemoryStream(_data, false);
            }
        }

        public override void Dispose()
        {
        }


        public class CloseNotifyingMemoryStream : MemoryStream
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

    }
}