using System;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.Server;
using Microsoft.Extensions.Options;
using System.Text.Json;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Rnwood.Smtp4dev.Service;
using System.Text.Json.Serialization;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerController : Controller
    {
        public ServerController(ISmtp4devServer server, ImapServer imapServer, IOptionsMonitor<ServerOptions> serverOptions,
            IOptionsMonitor<RelayOptions> relayOptions, IHostingEnvironmentHelper hostingEnvironmentHelper)
        {
            this.server = server;
            this.imapServer = imapServer;
            this.serverOptions = serverOptions;
            this.relayOptions = relayOptions;
            this.hostingEnvironmentHelper = hostingEnvironmentHelper;
        }


        private ISmtp4devServer server;
        private ImapServer imapServer;
        private IOptionsMonitor<ServerOptions> serverOptions;
        private IOptionsMonitor<RelayOptions> relayOptions;
        private readonly IHostingEnvironmentHelper hostingEnvironmentHelper;

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
                    TlsMode = relayOptions.CurrentValue.TlsMode.ToString(),
                    SmtpPort = relayOptions.CurrentValue.SmtpPort,
                    Login = relayOptions.CurrentValue.Login,
                    Password = relayOptions.CurrentValue.Password,
                    AutomaticEmails = relayOptions.CurrentValue.AutomaticEmails,
                    SenderAddress = relayOptions.CurrentValue.SenderAddress
                },
                SettingsAreEditable = hostingEnvironmentHelper.SettingsAreEditable,
                DisableMessageSanitisation = serverOptions.CurrentValue.DisableMessageSanitisation
            
            };
        }

        [HttpPost]
        public ActionResult UpdateServer(ApiModel.Server serverUpdate)
        {
            if (!hostingEnvironmentHelper.SettingsAreEditable)
            {
                return Forbid();
            }
            ServerOptions newSettings = serverOptions.CurrentValue;
            RelayOptions newRelaySettings = relayOptions.CurrentValue;

            newSettings.Port = serverUpdate.PortNumber;
            newSettings.HostName = serverUpdate.HostName;
            newSettings.AllowRemoteConnections = serverUpdate.AllowRemoteConnections;
            newSettings.NumberOfMessagesToKeep = serverUpdate.NumberOfMessagesToKeep;
            newSettings.NumberOfSessionsToKeep = serverUpdate.NumberOfSessionsToKeep;
            newSettings.ImapPort = serverUpdate.ImapPortNumber;
            newSettings.DisableMessageSanitisation = serverUpdate.DisableMessageSanitisation;

            newRelaySettings.SmtpServer = serverUpdate.RelayOptions.SmtpServer;
            newRelaySettings.SmtpPort = serverUpdate.RelayOptions.SmtpPort;
            newRelaySettings.TlsMode = Enum.Parse<SecureSocketOptions>(serverUpdate.RelayOptions.TlsMode);
            newRelaySettings.SenderAddress = serverUpdate.RelayOptions.SenderAddress;
            newRelaySettings.Login = serverUpdate.RelayOptions.Login;
            newRelaySettings.Password = serverUpdate.RelayOptions.Password;
            newRelaySettings.AutomaticEmails = serverUpdate.RelayOptions.AutomaticEmails;

            System.IO.File.WriteAllText(hostingEnvironmentHelper.GetEditableSettingsFilePath(),
                JsonSerializer.Serialize(new SettingsFile{ ServerOptions = newSettings, RelayOptions = newRelaySettings },
                    SettingsFileSerializationContext.Default.SettingsFile)
            );

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


            return Ok();
        }
    }

    internal class SettingsFile
    {
        public ServerOptions ServerOptions { get; set; }
        public RelayOptions RelayOptions { get; set; }
    }


    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(SettingsFile))]
    internal partial class SettingsFileSerializationContext : JsonSerializerContext {

    }
}