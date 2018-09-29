using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;

namespace Rnwood.Smtp4dev.Hubs
{
    public class MessagesHub : Hub
    {
        public async Task OnMessagesChanged()
        {
            if (Clients != null) await Clients.All.SendAsync("messageschanged");
        }
    }
}