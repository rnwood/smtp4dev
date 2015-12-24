using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;

namespace Rnwood.Smtp4dev.Model
{
    public interface IMessageStore
    {
        IEnumerable<ISmtp4devMessage> Messages { get; }

        event EventHandler<Smtp4devMessageEventArgs> MessageAdded;

        event EventHandler<Smtp4devMessageEventArgs> MessageDeleted;

        void AddMessage(ISmtp4devMessage message);

        void DeleteMessage(ISmtp4devMessage message);

        IEnumerable<ISmtp4devMessage> SearchMessages(string searchTerm);

        void DeleteAllMessages();
    }
}