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
        public MessagesHub(ISmtp4devServer server)
        {
            server.MessagesChanged += (s, ea) => { Refresh(); };
        }

        public void Refresh()
        {
            Clients.All.refresh();
        }
    }
}