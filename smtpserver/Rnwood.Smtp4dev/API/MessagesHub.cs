using Microsoft.AspNet.SignalR;
using Rnwood.Smtp4dev.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.API
{
    public class MessagesHub : Hub
    {
        public MessagesHub(IMessageStore messageStore)
        {
            messageStore.MessageAdded += (s, ea) => { MessageAdded(ea.Message); };
        }

        public void MessageAdded(ISmtp4devMessage message)
        {
            Clients.All.messageAdded(message.Id);
        }
    }
}