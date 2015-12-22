using Rnwood.SmtpServer;
using System;
using System.Collections.Generic;

namespace Rnwood.Smtp4dev.Model
{
    public interface IMessageStore
    {
        IEnumerable<ISmtp4devMessage> Messages { get; }

        event EventHandler<Smtp4devMessageEventArgs> MessageAdded;

        void AddMessage(ISmtp4devMessage message);
    }
}