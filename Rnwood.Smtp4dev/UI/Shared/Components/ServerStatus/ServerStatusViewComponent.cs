using Microsoft.AspNet.Mvc;
using Rnwood.Smtp4dev.Model;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.Smtp4dev.UI.Shared.Components.ServerStatus
{
    [ViewComponent(Name = "ServerStatus")]
    public class ServerStatusViewComponent : ViewComponent
    {
        public ServerStatusViewComponent(ISmtp4devServer server)
        {
            _server = server;
        }

        private ISmtp4devServer _server;

        public IViewComponentResult Invoke()
        {
            ServerStatusViewModel vm = new ServerStatusViewModel();

            vm.ServerError = _server.ServerError?.Message;
            vm.ServerRunning = _server.IsRunning;

            return View(vm);
        }
    }
}