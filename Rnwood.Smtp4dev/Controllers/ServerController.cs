using System;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Rnwood.Smtp4dev.Server;
using Microsoft.Extensions.Options;
using System.Text.Json;
using MailKit.Security;
using Microsoft.Extensions.Hosting;
using Rnwood.Smtp4dev.Service;
using System.Text.Json.Serialization;
using NSwag.Annotations;
using System.ComponentModel;
using Rnwood.Smtp4dev.Server.Settings;

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

        /// <summary>
        /// Gets the current state and settings for the smtp4dev server.
        /// </summary>
        /// <returns></returns>
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
                    AutomaticEmails = relayOptions.CurrentValue.AutomaticEmails.Where(s => !String.IsNullOrWhiteSpace(s)).ToArray(),
                    SenderAddress = relayOptions.CurrentValue.SenderAddress,
                    AutomaticRelayExpression = relayOptions.CurrentValue.AutomaticRelayExpression
                },
                SettingsAreEditable = hostingEnvironmentHelper.SettingsAreEditable,
                DisableMessageSanitisation = serverOptions.CurrentValue.DisableMessageSanitisation,
                TlsMode = serverOptions.CurrentValue.TlsMode.ToString(),
                AuthenticationRequired = serverOptions.CurrentValue.AuthenticationRequired,
                SecureConnectionRequired = serverOptions.CurrentValue.SecureConnectionRequired,
                CredentialsValidationExpression = serverOptions.CurrentValue.CredentialsValidationExpression,
                RecipientValidationExpression = serverOptions.CurrentValue.RecipientValidationExpression,
                MessageValidationExpression = serverOptions.CurrentValue.MessageValidationExpression,
                DisableIPv6 = serverOptions.CurrentValue.DisableIPv6,
                Users = serverOptions.CurrentValue.Users
            };
        }

        /// <summary>
        /// Updates the state of and settings for the smtp4dev server.
        /// </summary>
        /// Settings can not be updated if disabled in smtp4dev settings or if the settings file is not writable.
        /// <param name="serverUpdate"></param>
        [HttpPost]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(void), Description="If settings were successfully updated and state changes initiated (not not applied synchronously and can fail).")]
        [SwaggerResponse( System.Net.HttpStatusCode.Forbidden, typeof(void), Description = "If settings are not editable")]
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
            newSettings.TlsMode = Enum.Parse<TlsMode>(serverUpdate.TlsMode);
            newSettings.AuthenticationRequired = serverUpdate.AuthenticationRequired;
            newSettings.SecureConnectionRequired = serverUpdate.SecureConnectionRequired;
            newSettings.CredentialsValidationExpression = serverUpdate.CredentialsValidationExpression;
            newSettings.RecipientValidationExpression = serverUpdate.RecipientValidationExpression;
            newSettings.MessageValidationExpression = serverUpdate.MessageValidationExpression;
            newSettings.DisableIPv6 = serverUpdate.DisableIPv6;
            newSettings.Users = serverUpdate.Users;

            newRelaySettings.SmtpServer = serverUpdate.RelayOptions.SmtpServer;
            newRelaySettings.SmtpPort = serverUpdate.RelayOptions.SmtpPort;
            newRelaySettings.TlsMode = Enum.Parse<SecureSocketOptions>(serverUpdate.RelayOptions.TlsMode);
            newRelaySettings.SenderAddress = serverUpdate.RelayOptions.SenderAddress;
            newRelaySettings.Login = serverUpdate.RelayOptions.Login;
            newRelaySettings.Password = serverUpdate.RelayOptions.Password;
            newRelaySettings.AutomaticEmails = serverUpdate.RelayOptions.AutomaticEmails.Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();
            newRelaySettings.AutomaticRelayExpression = serverUpdate.RelayOptions.AutomaticRelayExpression;

            System.IO.File.WriteAllText(hostingEnvironmentHelper.GetEditableSettingsFilePath(),
                JsonSerializer.Serialize(new SettingsFile { ServerOptions = newSettings, RelayOptions = newRelaySettings },
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
    internal partial class SettingsFileSerializationContext : JsonSerializerContext
    {

    }
}