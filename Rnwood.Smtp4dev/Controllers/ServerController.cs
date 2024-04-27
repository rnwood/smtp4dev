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
using DeepEqual.Syntax;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerController : Controller
    {
        public ServerController(ISmtp4devServer server, ImapServer imapServer, IOptionsMonitor<ServerOptions> serverOptions,
            IOptionsMonitor<RelayOptions> relayOptions, IOptionsMonitor<DesktopOptions> desktopOptions, CommandLineOptions cmdLineOptions, IHostingEnvironmentHelper hostingEnvironmentHelper)
        {
            this.server = server;
            this.imapServer = imapServer;
            this.serverOptions = serverOptions;
            this.relayOptions = relayOptions;
            this.desktopOptions = desktopOptions;
            this.hostingEnvironmentHelper = hostingEnvironmentHelper;
            this.cmdLineOptions = cmdLineOptions;
        }

        private readonly ISmtp4devServer server;
        private readonly ImapServer imapServer;
        private readonly IOptionsMonitor<ServerOptions> serverOptions;
        private readonly IOptionsMonitor<RelayOptions> relayOptions;
        private readonly IOptionsMonitor<DesktopOptions> desktopOptions;
        private readonly IHostingEnvironmentHelper hostingEnvironmentHelper;
        private readonly CommandLineOptions cmdLineOptions;

        /// <summary>
        /// Gets the current state and settings for the smtp4dev server.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        public ApiModel.Server GetServer()
        {
            string currentUserName = this.User?.Identity?.Name;
            string currentUserDefaultMailbox = MailboxOptions.DEFAULTNAME;

            if (!string.IsNullOrEmpty(currentUserName))
            {
               var user = serverOptions.CurrentValue.Users.FirstOrDefault(u => currentUserName.Equals(u.Username, StringComparison.CurrentCultureIgnoreCase));
                if (user != null)
                {
                    currentUserDefaultMailbox = user.DefaultMailbox ?? MailboxOptions.DEFAULTNAME;
                }
            }

            var lockedSettings = GetLockedSettings(true);

            var serverOptionsCurrentValue = serverOptions.CurrentValue;
            var relayOptionsCurrentValue = relayOptions.CurrentValue;
            return new ApiModel.Server()
            {
                IsRunning = server.IsRunning,
                LockedSettings = lockedSettings,
                Port = serverOptionsCurrentValue.Port,
                ImapPort = serverOptionsCurrentValue.ImapPort,
                HostName = serverOptionsCurrentValue.HostName,
                AllowRemoteConnections = serverOptionsCurrentValue.AllowRemoteConnections,
                NumberOfMessagesToKeep = serverOptionsCurrentValue.NumberOfMessagesToKeep,
                NumberOfSessionsToKeep = serverOptionsCurrentValue.NumberOfSessionsToKeep,
                Exception = server.Exception?.Message,
                RelaySmtpServer = relayOptionsCurrentValue.SmtpServer,
                RelayTlsMode = relayOptionsCurrentValue.TlsMode.ToString(),
                RelaySmtpPort = relayOptionsCurrentValue.SmtpPort,
                RelayLogin = relayOptionsCurrentValue.Login,
                RelayPassword = relayOptionsCurrentValue.Password,
                RelayAutomaticEmails = relayOptionsCurrentValue.AutomaticEmails.Where(s => !string.IsNullOrWhiteSpace(s)).ToArray(),
                RelaySenderAddress = relayOptionsCurrentValue.SenderAddress,
                RelayAutomaticRelayExpression = relayOptionsCurrentValue.AutomaticRelayExpression,
                SettingsAreEditable = hostingEnvironmentHelper.SettingsAreEditable,
                DisableMessageSanitisation = serverOptionsCurrentValue.DisableMessageSanitisation,
                TlsMode = serverOptionsCurrentValue.TlsMode.ToString(),
                AuthenticationRequired = serverOptionsCurrentValue.AuthenticationRequired,
                SmtpAllowAnyCredentials = serverOptionsCurrentValue.SmtpAllowAnyCredentials,
                SecureConnectionRequired = serverOptionsCurrentValue.SecureConnectionRequired,
                CredentialsValidationExpression = serverOptionsCurrentValue.CredentialsValidationExpression,
                RecipientValidationExpression = serverOptionsCurrentValue.RecipientValidationExpression,
                MessageValidationExpression = serverOptionsCurrentValue.MessageValidationExpression,
                DisableIPv6 = serverOptionsCurrentValue.DisableIPv6,
                WebAuthenticationRequired = serverOptionsCurrentValue.WebAuthenticationRequired,
                Users = serverOptionsCurrentValue.Users,
				Mailboxes = serverOptionsCurrentValue.Mailboxes,
                DesktopMinimiseToTrayIcon = desktopOptions.CurrentValue.MinimiseToTrayIcon,
                IsDesktopApp = cmdLineOptions.IsDesktopApp,
				SmtpEnabledAuthTypesWhenNotSecureConnection = serverOptionsCurrentValue.SmtpEnabledAuthTypesWhenNotSecureConnection.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
				SmtpEnabledAuthTypesWhenSecureConnection = serverOptionsCurrentValue.SmtpEnabledAuthTypesWhenSecureConnection.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                CurrentUserName = currentUserName,
                CurrentUserDefaultMailboxName = currentUserDefaultMailbox
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

                    if (!oldValue.IsDeepEqual(newValue))
                    {
                        return Unauthorized($"The property '{ConvertPropertyNameToJsonCasing(lockedSetting.Key)}' is locked because '{lockedSetting.Value}'");

                    }
                }
            }


            ServerOptions newSettings = serverOptions.CurrentValue with { };
            RelayOptions newRelaySettings = relayOptions.CurrentValue with { };
            DesktopOptions newDesktopSettings = desktopOptions.CurrentValue with { };

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
			newSettings.Mailboxes = serverUpdate.Mailboxes;
            newSettings.WebAuthenticationRequired = serverUpdate.WebAuthenticationRequired;
            newSettings.SmtpAllowAnyCredentials = serverUpdate.SmtpAllowAnyCredentials;
			newSettings.SmtpEnabledAuthTypesWhenNotSecureConnection = string.Join(",", serverUpdate.SmtpEnabledAuthTypesWhenNotSecureConnection);
			newSettings.SmtpEnabledAuthTypesWhenSecureConnection = string.Join(",", serverUpdate.SmtpEnabledAuthTypesWhenSecureConnection);

			newRelaySettings.SmtpServer = serverUpdate.RelaySmtpServer;
            newRelaySettings.SmtpPort = serverUpdate.RelaySmtpPort;
            newRelaySettings.TlsMode = Enum.Parse<SecureSocketOptions>(serverUpdate.RelayTlsMode);
            newRelaySettings.SenderAddress = serverUpdate.RelaySenderAddress;
            newRelaySettings.Login = serverUpdate.RelayLogin;
            newRelaySettings.Password = serverUpdate.RelayPassword;
            newRelaySettings.AutomaticEmails = serverUpdate.RelayAutomaticEmails.Where(s => !String.IsNullOrWhiteSpace(s)).ToArray();
            newRelaySettings.AutomaticRelayExpression = serverUpdate.RelayAutomaticRelayExpression;

            newDesktopSettings.MinimiseToTrayIcon = serverUpdate.DesktopMinimiseToTrayIcon;

            System.IO.File.WriteAllText(hostingEnvironmentHelper.GetEditableSettingsFilePath(),
                JsonSerializer.Serialize(new SettingsFile {
                    ServerOptions = newSettings,
                    RelayOptions = newRelaySettings,
                    DesktopOptions = newDesktopSettings
                },
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

        public DesktopOptions DesktopOptions { get; set; }
    }


    [JsonSourceGenerationOptions(WriteIndented = true)]
    [JsonSerializable(typeof(SettingsFile))]
    internal partial class SettingsFileSerializationContext : JsonSerializerContext
    {

    }
}