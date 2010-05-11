#region

using System;
using System.Collections.Generic;
using System.IO;

#endregion

namespace Rnwood.SmtpServer
{
    public class Message : IMessage
    {
        public Message(ISession session)
        {
            Session = session;
            ToList = new List<string>();
            ReceivedDate = DateTime.Now;
        }

        public bool SecureConnection { get; set; }

        public DateTime ReceivedDate { get; set; }

        public ISession Session { get; private set; }

        public string From { get; set; }

        internal List<string> ToList { get; set; }

        public string[] To
        {
            get { return ToList.ToArray(); }
        }

        private byte[] _data;

        public Stream GetData()
        {
            return GetData(false);
        }

        public Stream GetData(bool forWriting)
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


        class CloseNotifyingMemoryStream : MemoryStream
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