using Rnwood.SmtpServer;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Model
{
    public class MessageStore : IMessageStore
    {
        public MessageStore(DirectoryInfo directory)
        {
            _directory = directory;

            if (!_directory.Exists)
            {
                _directory.Create();
            }
        }

        private DirectoryInfo _directory;

        private ConcurrentQueue<ISmtp4devMessage> _messages = new ConcurrentQueue<ISmtp4devMessage>();

        public IEnumerable<ISmtp4devMessage> Messages
        {
            get
            {
                return _messages.ToList();
            }
        }

        public event EventHandler<Smtp4devMessageEventArgs> MessageAdded;

        public void AddMessage(ISmtp4devMessage message)
        {
            _messages.Enqueue(message);
            MessageAdded?.Invoke(this, new Smtp4devMessageEventArgs(message));
        }

        public ISmtp4devMessage CreateMessage(IConnection connection)
        {
            Guid id = Guid.NewGuid();

            string fileName = Path.Combine(_directory.FullName, id.ToString() + ".msg");

            return new Smtp4devMessage(connection.Session, id, new FileInfo(fileName));
        }
    }
}