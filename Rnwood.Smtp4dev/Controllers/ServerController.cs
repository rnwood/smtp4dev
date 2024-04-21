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
using System.Collections.Generic;
using System.Runtime.ConstrainedExecution;
using Namotion.Reflection;
using Microsoft.EntityFrameworkCore.Metadata.Internal;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerController : Controller
    {
        public ServerController(ISmtp4devServer server, ImapServer imapServer, IOptionsMonitor<ServerOptions> serverOptions,
            IOptionsMonitor<RelayOptions> relayOptions, CommandLineOptions cmdLineOptions, IHostingEnvironmentHelper hostingEnvironmentHelper)
        {
            this.server = server;
            this.imapServer = imapServer;
            this.serverOptions = serverOptions;
            this.relayOptions = relayOptions;
            this.hostingEnvironmentHelper = hostingEnvironmentHelper;
            this.cmdLineOptions = cmdLineOptions;
        }

        private ISmtp4devServer server;
        private ImapServer imapServer;
        private IOptionsMonitor<ServerOptions> serverOptions;
        private IOptionsMonitor<RelayOptions> relayOptions;
        private readonly IHostingEnvironmentHelper hostingEnvironmentHelper;
        private readonly CommandLineOptions cmdLineOptions;

        /// <summary>
        /// Gets the current state and settings for the smtp4dev server.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ApiModel.Server GetServer()
        {
            var lockedSettings = GetLockedSettings(true);

            return new ApiModel.Server()
            {
                IsRunning = server.IsRunning,
                LockedSettings = lockedSettings,
                Port = serverOptions.CurrentValue.Port,
                ImapPort = serverOptions.CurrentValue.ImapPort,
                HostName = serverOptions.CurrentValue.HostName,
                AllowRemoteConnections = serverOptions.CurrentValue.AllowRemoteConnections,
                NumberOfMessagesToKeep = serverOptions.CurrentValue.NumberOfMessagesToKeep,
                NumberOfSessionsToKeep = serverOptions.CurrentValue.NumberOfSessionsToKeep,
                Exception = server.Exception?.Message,
                RelaySmtpServer = relayOptions.CurrentValue.SmtpServer,
                RelayTlsMode = relayOptions.CurrentValue.TlsMode.ToString(),
                RelaySmtpPort = relayOptions.CurrentValue.SmtpPort,
                RelayLogin = relayOptions.CurrentValue.Login,
                RelayPassword = relayOptions.CurrentValue.Password,
                RelayAutomaticEmails = relayOptions.CurrentValue.AutomaticEmails.Where(s => !String.IsNullOrWhiteSpace(s)).ToArray(),
                RelaySenderAddress = relayOptions.CurrentValue.SenderAddress,
                RelayAutomaticRelayExpression = relayOptions.CurrentValue.AutomaticRelayExpression,
                SettingsAreEditable = hostingEnvironmentHelper.SettingsAreEditable,
                DisableMessageSanitisation = serverOptions.CurrentValue.DisableMessageSanitisation,
                TlsMode = serverOptions.CurrentValue.TlsMode.ToString(),
                AuthenticationRequired = serverOptions.CurrentValue.AuthenticationRequired,
                SecureConnectionRequired = serverOptions.CurrentValue.SecureConnectionRequired,
                CredentialsValidationExpression = serverOptions.CurrentValue.CredentialsValidationExpression,
                RecipientValidationExpression = serverOptions.CurrentValue.RecipientValidationExpression,
                MessageValidationExpression = serverOptions.CurrentValue.MessageValidationExpression,
                DisableIPv6 = serverOptions.CurrentValue.DisableIPv6,
                WebAuthenticationRequired = serverOptions.CurrentValue.WebAuthenticationRequired,
                Users = serverOptions.CurrentValue.Users
            };
        }

        private IDictionary<string, string> GetLockedSettings(bool toJsonCasing)
        {
            Dictionary<string, string> lockedSettings = new Dictionary<string, string>();

            if (!hostingEnvironmentHelper.SettingsAreEditable)
            {
                var properties = typeof(ApiModel.Server).GetProperties(System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance);
                foreach (var property in properties)
                {
                    lockedSettings[property.Name] = "All settings are locked.";
                }

            }
            else
            {
                if (this.hostingEnvironmentHelper.IsRunningInContainer())
                {
                    lockedSettings.Add(nameof(ApiModel.Server.Port), "Running in a container. Change this port mapping in the container host.");
                    lockedSettings.Add(nameof(ApiModel.Server.ImapPort), "Running in a container. Change this port mapping in the container host.");
                    lockedSettings.Add(nameof(ApiModel.Server.AllowRemoteConnections), "Running in a container. Change this port mapping in the container host.");
                }

                if (cmdLineOptions.ServerOptions != null)
                {
                    foreach (var p in cmdLineOptions.ServerOptions.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                    {
                        if (p.GetValue(cmdLineOptions.ServerOptions) != null)
                        {
                            lockedSettings[p.Name] = "Specified using command line option";
                        }
                    }
                }

                if (cmdLineOptions.RelayOptions != null)
                {
                    foreach (var p in cmdLineOptions.RelayOptions.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                    {
                        if (p.GetValue(cmdLineOptions.RelayOptions) != null)
                        {
                            lockedSettings["Relay" + p.Name] = "Specified using command line option";
                        }
                    }
                }
            }

            if (toJsonCasing)
            {
                foreach (var lockedSetting in lockedSettings.ToArray())
                {
                    lockedSettings.Remove(lockedSetting.Key);
                    lockedSettings[ConvertPropertyNameToJsonCasing(lockedSetting.Key)] = lockedSetting.Value;
                }
            }

            return lockedSettings;
        }

        private static string ConvertPropertyNameToJsonCasing(string propertyName)
        {
            return string.Join('.', propertyName.Split('.').Select(p => p[..1].ToLower() + p[1..]));
        }

        /// <summary>
        /// Updates the state of and settings for the smtp4dev server.
        /// </summary>
        /// Settings can not be updated if disabled in smtp4dev settings or if the settings file is not writable.
        /// <param name="serverUpdate"></param>
        [HttpPost]
        [SwaggerResponse(System.Net.HttpStatusCode.OK, typeof(void), Description = "If settings were successfully updated and state changes initiated (not not applied synchronously and can fail).")]
        [SwaggerResponse(System.Net.HttpStatusCode.Forbidden, typeof(void), Description = "If settings are not editable")]
        public ActionResult UpdateServer(ApiModel.Server serverUpdate)
        {

            if (!hostingEnvironmentHelper.SettingsAreEditable)
            {
                return Unauthorized("Settings are locked");
            }


            var lockedSettings = GetLockedSettings(false);
            if (lockedSettings.Any())
            {
                var currentSettings = GetServer();
                foreach (var lockedSetting in lockedSettings)
                {
                    var oldValue = currentSettings.TryGetPropertyValue<object>(lockedSetting.Key);
                    var newValue = serverUpdate.TryGetPropertyValue<object>(lockedSetting.Key);

                    if (!object.Equals(oldValue, newValue))
                    {
                        return Unauthorized($"The property '{ConvertPropertyNameToJsonCasing(lockedSetting.Key)}' is locked because '{lockedSetting.Value}'");
                    }
                }
            }


            ServerOptions newSettings = serverOptions.CurrentValue;
            RelayOptions newRelaySettings = relayOptions.CurrentValue;

            newSettings.Port = serverUpdate.Port;
            newSettings.HostName = serverUpdate.HostName;
            newSettings.AllowRemoteConnections = serverUpdate.AllowRemoteConnections;
            newSettings.NumberOfMessagesToKeep = serverUpdate.NumberOfMessagesToKeep;
            newSettings.NumberOfSessionsToKeep = serverUpdate.NumberOfSessionsToKeep;
            newSettings.ImapPort = serverUpdate.ImapPort;
            newSettings.DisableMessageSanitisation = serverUpdate.DisableMessageSanitisation;
            newSettings.TlsMode = Enum.Parse<TlsMode>(serverUpdate.TlsMode);
            newSettings.AuthenticationRequired = serverUpdate.AuthenticationRequired;
            newSettings.SecureConnectionRequired = serverUpdate.SecureConnectionRequired;
            newSettings.CredentialsValidationExpression = serverUpdate.CredentialsValidationExpression;
            newSettings.RecipientValidationExpression = serverUpdate.RecipientValidationExpression;
            newSettings.MessageValidationExpression = serverUpdate.MessageValidationExpression;
            newSettings.DisableIPv6 = serverUpdate.DisableIPv6;
            newSettings.Users = serverUpdate.Users;
            newSettings.WebAuthenticationRequired = serverUpdate.WebAuthenticationRequired;



            newRelaySettings.SmtpServer = serverUpdate.RelaySmtpServer;
            newRelaySettings.SmtpPort = serverUpdate.RelaySmtpPort;
            newRelaySettings.TlsMode = Enum.Parse<SecureSocketOptions>(serverUpdate.RelayTlsMode);
            newRelaySettings.SenderAddress = serverUpdate.RelaySenderAddress;
            newRelaySettings.Login = serverUpdate.RelayLogin;
            newRelaySettings.Password = serverUpdate.RelayPassword;
            newRelaySettings.AutomaticEmails = serverUpdate.RelayAutomaticEmails.Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();
            newRelaySettings.AutomaticRelayExpression = serverUpdate.RelayAutomaticRelayExpression;

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