using NDatabase;
using NDatabase.Api;
using NDatabase.Exceptions;
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
        public MessageStore(FileInfo file)
        {
            _database = OdbFactory.Open(file.FullName);
        }

        private IOdb _database;
        private object _syncRoot = new object();

        public IEnumerable<ISmtp4devMessage> Messages
        {
            get
            {
                lock (_syncRoot)
                {
                    return _database.AsQueryable<ISmtp4devMessage>().OrderByDescending(m => m.ReceivedDate);
                }
            }
        }

        public event EventHandler<Smtp4devMessageEventArgs> MessageAdded;

        public event EventHandler<Smtp4devMessageEventArgs> MessageDeleted;

        public void DeleteMessage(ISmtp4devMessage message)
        {
            lock (_syncRoot)
            {
                _database.Delete(message);
                _database.Commit();
            }

            MessageDeleted?.Invoke(this, new Smtp4devMessageEventArgs(message));
        }

        public void AddMessage(ISmtp4devMessage message)
        {
            lock (_syncRoot)
            {
                _database.Store(message);
                _database.Commit();
            }

            MessageAdded?.Invoke(this, new Smtp4devMessageEventArgs(message));
        }

        public IEnumerable<ISmtp4devMessage> SearchMessages(string searchTerm)
        {
            lock (_syncRoot)
            {
                return _database.AsQueryable<ISmtp4devMessage>()
                    .Where(m =>
                        (m.Subject != null && m.Subject.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) > -1)
                        || m.To.Any(to => to.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) > -1)
                        || m.From.IndexOf(searchTerm, StringComparison.OrdinalIgnoreCase) > -1
                    )
                    .OrderByDescending(m => m.ReceivedDate);
            }
        }

        public void DeleteAllMessages()
        {
            lock (_syncRoot)
            {
                foreach (ISmtp4devMessage message in Messages)
                {
                    DeleteMessage(message);
                }
            }
        }
    }
}