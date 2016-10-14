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
        public MessageStore()
        {
        }

        private ConcurrentDictionary<Guid, ISmtp4devMessage> _messages = new ConcurrentDictionary<Guid, ISmtp4devMessage>();

        public IEnumerable<ISmtp4devMessage> Messages
        {
            get
            {
                //Return copy
                return _messages.Values.ToArray();
            }
        }

        public event EventHandler<Smtp4devMessageEventArgs> MessageAdded;

        public event EventHandler<Smtp4devMessageEventArgs> MessageDeleted;

        public void DeleteMessage(ISmtp4devMessage message)
        {
            ISmtp4devMessage deletedMessage;
            if (_messages.TryRemove(message.Id, out deletedMessage))
            {
                MessageDeleted?.Invoke(this, new Smtp4devMessageEventArgs(deletedMessage));
            }
        }

        public void AddMessage(ISmtp4devMessage message)
        {
            if (_messages.TryAdd(message.Id, message))
            {
                MessageAdded?.Invoke(this, new Smtp4devMessageEventArgs(message));
            }
        }

        public IEnumerable<ISmtp4devMessage> SearchMessages(string searchTerm)
        {
            return Messages
                .Where(m =>
                    (m.Subject != null && m.Subject.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) > -1)
                    || m.To.Any(to => to.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) > -1)
                    || m.From.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) > -1
                )
                .OrderByDescending(m => m.ReceivedDate);
        }

        public void DeleteAllMessages()
        {
            foreach (ISmtp4devMessage message in Messages)
            {
                DeleteMessage(message);
            }
        }
    }
}