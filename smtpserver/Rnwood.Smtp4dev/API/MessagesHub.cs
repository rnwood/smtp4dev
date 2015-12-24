using Microsoft.AspNet.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.PlatformAbstractions;
using Rnwood.Smtp4dev.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.API
{
    public class MessagesHub : Hub
    {
        public MessagesHub()
        {
        }
    }
}