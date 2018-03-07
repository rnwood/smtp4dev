using Microsoft.AspNetCore.SignalR;
using Rnwood.Smtp4dev.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.Hubs
{
    public class SessionsHub : Hub
    {
        public async Task OnSessionsChanged()
        {
            if (Clients != null)
            {
                await Clients.All.SendAsync("sessionschanged");
            }
        }
    }
}
