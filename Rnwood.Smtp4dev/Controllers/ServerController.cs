using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Microsoft.EntityFrameworkCore;
using Newtonsoft.Json;
using System.IO;
using MimeKit;
using HtmlAgilityPack;
using Rnwood.Smtp4dev.Server;
using Microsoft.AspNet.OData;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerController : Controller
    {
        public ServerController(Smtp4devServer server)
        {
            this.server = server;
        }


        private Smtp4devServer server;

        [HttpGet]
        public ApiModel.Server GetServer()
        {
            return new ApiModel.Server()
            {
                IsRunning = server.IsRunning,
                PortNumber = server.PortNumber,
                Exception = server.Exception?.Message
            };
        }

        [HttpPost]
        public void UpdateServer(ApiModel.Server serverUpdate)
        {
            if (!serverUpdate.IsRunning && this.server.IsRunning)
            {
                this.server.Stop();
            }else if (serverUpdate.IsRunning && !this.server.IsRunning)
            {
                this.server.TryStart();
            }
        }

    }
}
