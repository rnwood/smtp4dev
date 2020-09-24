using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Hubs;
using Microsoft.EntityFrameworkCore;
using System.IO;
using MimeKit;
using HtmlAgilityPack;
using Rnwood.Smtp4dev.Server;
using Microsoft.Extensions.Options;
using System.Text.Json;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerController : Controller
    {
        public ServerController(Smtp4devServer server, ImapServer imapServer, IOptionsMonitor<ServerOptions> serverOptions, IOptionsMonitor<RelayOptions> relayOptions)
        {
            this.server = server;
            this.imapServer = imapServer;
            this.serverOptions = serverOptions;
            this.relayOptions = relayOptions;
        }


        private Smtp4devServer server;
        private ImapServer imapServer;
        private IOptionsMonitor<ServerOptions> serverOptions;
        private IOptionsMonitor<RelayOptions> relayOptions;

        [HttpGet]
        public ApiModel.Server GetServer()
        {
            return new ApiModel.Server()
            {
                IsRunning = server.IsRunning,
                PortNumber = serverOptions.CurrentValue.Port,
                ImapPortNumber = serverOptions.CurrentValue.ImapPort,
                HostName = serverOptions.CurrentValue.HostName,
                AllowRemoteConnections = serverOptions.CurrentValue.AllowRemoteConnections,
                NumberOfMessagesToKeep = serverOptions.CurrentValue.NumberOfMessagesToKeep,
                NumberOfSessionsToKeep = serverOptions.CurrentValue.NumberOfSessionsToKeep,
                Exception = server.Exception?.Message,
                RelayOptions = new ApiModel.ServerRelayOptions
                {
                    SmtpServer = relayOptions.CurrentValue.SmtpServer,
                    SmtpPort = relayOptions.CurrentValue.SmtpPort,
                    Login = relayOptions.CurrentValue.Login,
                    Password = relayOptions.CurrentValue.Password,
                    AutomaticEmails = relayOptions.CurrentValue.AutomaticEmails,
                    SenderAddress = relayOptions.CurrentValue.SenderAddress
                }
            };
        }

        [HttpPost]
        public void UpdateServer(ApiModel.Server serverUpdate)
        {
            ServerOptions newSettings = serverOptions.CurrentValue;
            RelayOptions newRelaySettings = relayOptions.CurrentValue;

            newSettings.Port = serverUpdate.PortNumber;
            newSettings.HostName = serverUpdate.HostName;
            newSettings.AllowRemoteConnections = serverUpdate.AllowRemoteConnections;
            newSettings.NumberOfMessagesToKeep = serverUpdate.NumberOfMessagesToKeep;
            newSettings.NumberOfSessionsToKeep = serverUpdate.NumberOfSessionsToKeep;
            newSettings.ImapPort = serverUpdate.ImapPortNumber;

            newRelaySettings.SmtpServer = serverUpdate.RelayOptions.SmtpServer;
            newRelaySettings.SmtpPort = serverUpdate.RelayOptions.SmtpPort;
            newRelaySettings.SenderAddress = serverUpdate.RelayOptions.SenderAddress;
            newRelaySettings.Login = serverUpdate.RelayOptions.Login;
            newRelaySettings.Password = serverUpdate.RelayOptions.Password;
            newRelaySettings.AutomaticEmails = serverUpdate.RelayOptions.AutomaticEmails;

            if (!serverUpdate.IsRunning && this.server.IsRunning)
            {
                this.server.Stop();
            }
            else if (serverUpdate.IsRunning && !this.server.IsRunning)
            {
                this.server.TryStart();
            }

            if (!serverUpdate.IsRunning && this.imapServer.IsRunning)
            {
                this.imapServer.Stop();
            }
            else if (serverUpdate.IsRunning && !this.imapServer.IsRunning)
            {
                this.imapServer.TryStart();
            }

            string dataDir = Path.Join(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "smtp4dev");
            string settingsFile = Path.Join(dataDir, "appsettings.json");
            System.IO.File.WriteAllText(settingsFile, JsonSerializer.Serialize(new { ServerOptions = newSettings, RelayOptions = newRelaySettings }, new JsonSerializerOptions { WriteIndented = true }));
        }

    }
}
