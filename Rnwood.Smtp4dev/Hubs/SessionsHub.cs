using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Rnwood.Smtp4dev.Hubs
{
    public class SessionsHub : Hub
    {
        public async Task OnSessionsChanged()
        {
            if (Clients != null) await Clients.All.SendAsync("sessionschanged");
        }
    }
}