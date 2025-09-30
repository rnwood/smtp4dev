﻿using System;
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
using System.Text.Json.Serialization.Metadata;
using Microsoft.Extensions.Configuration.EnvironmentVariables;
using Microsoft.Extensions.Configuration;
using CommandLiners;
using System.Text.Json.Nodes;
using System.Dynamic;

namespace Rnwood.Smtp4dev.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class ServerController : Controller
    {
        public ServerController(ISmtp4devServer server, ImapServer imapServer, Rnwood.Smtp4dev.Server.Pop3.Pop3Server pop3Server, IOptionsMonitor<ServerOptions> serverOptions,
            IOptionsMonitor<RelayOptions> relayOptions, IOptionsMonitor<DesktopOptions> desktopOptions, MapOptions<CommandLineOptions> commandLineOptions, CommandLineOptions cmdLineOptions, IHostingEnvironmentHelper hostingEnvironmentHelper)
        {
            this.server = server;
            this.imapServer = imapServer;
            this.pop3Server = pop3Server;
            this.serverOptions = serverOptions;
            this.relayOptions = relayOptions;
            this.desktopOptions = desktopOptions;
            this.hostingEnvironmentHelper = hostingEnvironmentHelper;
            this.cmdLineOptions = cmdLineOptions;
            this.commandLineOptions = commandLineOptions;
        }

        private readonly ISmtp4devServer server;
        private readonly ImapServer imapServer;
        private readonly Rnwood.Smtp4dev.Server.Pop3.Pop3Server pop3Server;
        private readonly IOptionsMonitor<ServerOptions> serverOptions;
        private readonly IOptionsMonitor<RelayOptions> relayOptions;
        private readonly IOptionsMonitor<DesktopOptions> desktopOptions;
        private readonly IHostingEnvironmentHelper hostingEnvironmentHelper;
        private readonly CommandLineOptions cmdLineOptions;
        private readonly MapOptions<CommandLineOptions> commandLineOptions;

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
                Pop3Port = serverOptionsCurrentValue.Pop3Port,
                Pop3TlsMode = serverOptionsCurrentValue.Pop3TlsMode.ToString(),
                Pop3SecureConnectionRequired = serverOptionsCurrentValue.Pop3SecureConnectionRequired,
                HostName = serverOptionsCurrentValue.HostName,
                AllowRemoteConnections = serverOptionsCurrentValue.AllowRemoteConnections,
                BindAddress = serverOptionsCurrentValue.BindAddress,
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
                CommandValidationExpression = serverOptionsCurrentValue.CommandValidationExpression,
                DisableIPv6 = serverOptionsCurrentValue.DisableIPv6,
                WebAuthenticationRequired = serverOptionsCurrentValue.WebAuthenticationRequired,
                DeliverMessagesToUsersDefaultMailbox = serverOptionsCurrentValue.DeliverMessagesToUsersDefaultMailbox,
                Users = serverOptionsCurrentValue.Users,
                Mailboxes = serverOptionsCurrentValue.Mailboxes,
                DesktopMinimiseToTrayIcon = desktopOptions.CurrentValue.MinimiseToTrayIcon,
                IsDesktopApp = cmdLineOptions.IsDesktopApp,
                SmtpEnabledAuthTypesWhenNotSecureConnection = serverOptionsCurrentValue.SmtpEnabledAuthTypesWhenNotSecureConnection.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                SmtpEnabledAuthTypesWhenSecureConnection = serverOptionsCurrentValue.SmtpEnabledAuthTypesWhenSecureConnection.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries),
                CurrentUserName = currentUserName,
                CurrentUserDefaultMailboxName = currentUserDefaultMailbox,
                HtmlValidateConfig = serverOptionsCurrentValue.HtmlValidateConfig != null ? serverOptionsCurrentValue.HtmlValidateConfig : null,
                DisableHtmlValidation = serverOptionsCurrentValue.DisableHtmlValidation,
                DisableHtmlCompatibilityCheck = serverOptionsCurrentValue.DisableHtmlCompatibilityCheck
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
                var fixedConfigSources = new ConfigurationBuilder().AddEnvironmentVariables()
                            .AddCommandLineOptions(commandLineOptions).Build();

                var fixedConfig = new SettingsFile
                {
                    ServerOptions = fixedConfigSources.GetSection("ServerOptions").Get<ServerOptionsSource>(),
                    DesktopOptions = fixedConfigSources.GetSection("DesktopOptions").Get<DesktopOptionsSource>(),
                    RelayOptions = fixedConfigSources.GetSection("RelayOptions").Get<RelayOptionsSource>()
                };

                if (fixedConfig.ServerOptions != null)
                {
                    foreach (var p in fixedConfig.ServerOptions.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                    {
                        if (p.GetValue(fixedConfig.ServerOptions) != null)
                        {
                            lockedSettings[p.Name] = "Specified using command line option or environment variable";
                        }
                    }
                }

                if (fixedConfig.RelayOptions != null)
                {
                    foreach (var p in fixedConfig.RelayOptions.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                    {
                        if (p.GetValue(fixedConfig.RelayOptions) != null)
                        {
                            lockedSettings["Relay" + p.Name] = "Specified using command line option or environment variable";
                        }
                    }
                }

                if (fixedConfig.DesktopOptions != null)
                {
                    foreach (var p in fixedConfig.DesktopOptions.GetType().GetProperties(System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public))
                    {
                        if (p.GetValue(fixedConfig.DesktopOptions) != null)
                        {
                            lockedSettings["Desktop" + p.Name] = "Specified using command line option or environment variable";
                        }
                    }
                }


                if (this.hostingEnvironmentHelper.IsRunningInContainer())
                {
                    lockedSettings[nameof(ApiModel.Server.Port)]= "Running in a container. Change this port mapping in the container host.";
                    lockedSettings[nameof(ApiModel.Server.ImapPort)]= "Running in a container. Change this port mapping in the container host.";
                    lockedSettings[nameof(ApiModel.Server.AllowRemoteConnections)]= "Running in a container. Change this port mapping in the container host.";
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

            var currentSettings = GetServer();

            var lockedSettings = GetLockedSettings(false);
            if (lockedSettings.Any())
            {
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

            SettingsFile defaultSettingsFile = JsonSerializer.Deserialize(System.IO.File.ReadAllText(hostingEnvironmentHelper.GetDefaultSettingsFilePath()), SettingsFileSerializationContext.Default.SettingsFile);


            string editableSettingsFilePath = hostingEnvironmentHelper.GetEditableSettingsFilePath();

            SettingsFile newSettingsFile = new SettingsFile();
            if (editableSettingsFilePath != null && System.IO.File.Exists(editableSettingsFilePath))
            {
                newSettingsFile = JsonSerializer.Deserialize(System.IO.File.ReadAllText(hostingEnvironmentHelper.GetEditableSettingsFilePath()), SettingsFileSerializationContext.Default.SettingsFile);
            }

            ServerOptionsSource newSettings = newSettingsFile.ServerOptions = newSettingsFile.ServerOptions ?? new ServerOptionsSource();
            RelayOptionsSource newRelaySettings = newSettingsFile.RelayOptions ?? newSettingsFile.RelayOptions ?? new RelayOptionsSource();
            DesktopOptionsSource newDesktopSettings = newSettingsFile.DesktopOptions ?? newSettingsFile.DesktopOptions ?? new DesktopOptionsSource();

            //Horrible without reflection! Maybe source generators can do this?
            newSettings.Port = serverUpdate.Port != defaultSettingsFile.ServerOptions.Port ? serverUpdate.Port : null;
            newSettings.HostName = serverUpdate.HostName != defaultSettingsFile.ServerOptions.HostName ? serverUpdate.HostName : null;
            newSettings.AllowRemoteConnections = serverUpdate.AllowRemoteConnections != defaultSettingsFile.ServerOptions.AllowRemoteConnections ? serverUpdate.AllowRemoteConnections : null;
            newSettings.BindAddress = serverUpdate.BindAddress != defaultSettingsFile.ServerOptions.BindAddress ? serverUpdate.BindAddress : null;
            newSettings.NumberOfMessagesToKeep = serverUpdate.NumberOfMessagesToKeep != defaultSettingsFile.ServerOptions.NumberOfMessagesToKeep ? serverUpdate.NumberOfMessagesToKeep : null;
            newSettings.NumberOfSessionsToKeep = serverUpdate.NumberOfSessionsToKeep != defaultSettingsFile.ServerOptions.NumberOfSessionsToKeep ? serverUpdate.NumberOfSessionsToKeep : null;
            newSettings.ImapPort = serverUpdate.ImapPort != defaultSettingsFile.ServerOptions.ImapPort ? serverUpdate.ImapPort : null;
            newSettings.Pop3Port = serverUpdate.Pop3Port != defaultSettingsFile.ServerOptions.Pop3Port ? serverUpdate.Pop3Port : null;
            newSettings.Pop3TlsMode = Enum.Parse<TlsMode>(serverUpdate.Pop3TlsMode) != defaultSettingsFile.ServerOptions.Pop3TlsMode ? Enum.Parse<TlsMode>(serverUpdate.Pop3TlsMode) : null;
            newSettings.Pop3SecureConnectionRequired = serverUpdate.Pop3SecureConnectionRequired != defaultSettingsFile.ServerOptions.Pop3SecureConnectionRequired ? serverUpdate.Pop3SecureConnectionRequired : null;
            newSettings.DisableMessageSanitisation = serverUpdate.DisableMessageSanitisation != defaultSettingsFile.ServerOptions.DisableMessageSanitisation ? serverUpdate.DisableMessageSanitisation : null;
            newSettings.TlsMode =  Enum.Parse<TlsMode>(serverUpdate.TlsMode) != defaultSettingsFile.ServerOptions.TlsMode ? Enum.Parse<TlsMode>(serverUpdate.TlsMode) : null;
            newSettings.AuthenticationRequired = serverUpdate.AuthenticationRequired != defaultSettingsFile.ServerOptions.AuthenticationRequired ? serverUpdate.AuthenticationRequired : null;
            newSettings.SecureConnectionRequired = serverUpdate.SecureConnectionRequired != defaultSettingsFile.ServerOptions.SecureConnectionRequired ? serverUpdate.SecureConnectionRequired : null;
            newSettings.CredentialsValidationExpression = serverUpdate.CredentialsValidationExpression != defaultSettingsFile.ServerOptions.CredentialsValidationExpression ? serverUpdate.CredentialsValidationExpression : null;
            newSettings.RecipientValidationExpression = serverUpdate.RecipientValidationExpression != defaultSettingsFile.ServerOptions.RecipientValidationExpression ? serverUpdate.RecipientValidationExpression : null;
            newSettings.CommandValidationExpression = serverUpdate.CommandValidationExpression != defaultSettingsFile.ServerOptions.CommandValidationExpression ? serverUpdate.CommandValidationExpression : null;
            newSettings.MessageValidationExpression = serverUpdate.MessageValidationExpression != defaultSettingsFile.ServerOptions.MessageValidationExpression ? serverUpdate.MessageValidationExpression : null;
            newSettings.DisableIPv6 = serverUpdate.DisableIPv6 != defaultSettingsFile.ServerOptions.DisableIPv6 ? serverUpdate.DisableIPv6 : null;
            newSettings.Users = serverUpdate.Users != defaultSettingsFile.ServerOptions.Users ? serverUpdate.Users : null;
            newSettings.Mailboxes = serverUpdate.Mailboxes != defaultSettingsFile.ServerOptions.Mailboxes ? serverUpdate.Mailboxes : null;
            newSettings.WebAuthenticationRequired = serverUpdate.WebAuthenticationRequired != defaultSettingsFile.ServerOptions.WebAuthenticationRequired ? serverUpdate.WebAuthenticationRequired : null;
            newSettings.DeliverMessagesToUsersDefaultMailbox = serverUpdate.DeliverMessagesToUsersDefaultMailbox != defaultSettingsFile.ServerOptions.DeliverMessagesToUsersDefaultMailbox ? serverUpdate.DeliverMessagesToUsersDefaultMailbox : null;
            newSettings.SmtpAllowAnyCredentials = serverUpdate.SmtpAllowAnyCredentials != defaultSettingsFile.ServerOptions.SmtpAllowAnyCredentials ? serverUpdate.SmtpAllowAnyCredentials : null;
            newSettings.SmtpEnabledAuthTypesWhenNotSecureConnection = string.Join(",", serverUpdate.SmtpEnabledAuthTypesWhenNotSecureConnection) != defaultSettingsFile.ServerOptions.SmtpEnabledAuthTypesWhenNotSecureConnection ? string.Join(",", serverUpdate.SmtpEnabledAuthTypesWhenNotSecureConnection) : null;
            newSettings.SmtpEnabledAuthTypesWhenSecureConnection = string.Join(",", serverUpdate.SmtpEnabledAuthTypesWhenSecureConnection) != defaultSettingsFile.ServerOptions.SmtpEnabledAuthTypesWhenSecureConnection ? string.Join(",", serverUpdate.SmtpEnabledAuthTypesWhenSecureConnection) : null;
            newSettings.HtmlValidateConfig = serverUpdate.HtmlValidateConfig != defaultSettingsFile.ServerOptions.HtmlValidateConfig ?  serverUpdate.HtmlValidateConfig : null;
            newSettings.DisableHtmlValidation = serverUpdate.DisableHtmlValidation != defaultSettingsFile.ServerOptions.DisableHtmlValidation ? serverUpdate.DisableHtmlValidation : null;
            newSettings.DisableHtmlCompatibilityCheck = serverUpdate.DisableHtmlCompatibilityCheck != defaultSettingsFile.ServerOptions.DisableHtmlCompatibilityCheck ? serverUpdate.DisableHtmlCompatibilityCheck : null;

            newRelaySettings.SmtpServer = serverUpdate.RelaySmtpServer != defaultSettingsFile.RelayOptions.SmtpServer ? serverUpdate.RelaySmtpServer : null;
            newRelaySettings.SmtpPort = serverUpdate.RelaySmtpPort != defaultSettingsFile.RelayOptions.SmtpPort ? serverUpdate.RelaySmtpPort : null;
            newRelaySettings.TlsMode = Enum.Parse<SecureSocketOptions>(serverUpdate.RelayTlsMode) != defaultSettingsFile.RelayOptions.TlsMode ? Enum.Parse<SecureSocketOptions>(serverUpdate.RelayTlsMode) : null;
            newRelaySettings.SenderAddress = serverUpdate.RelaySenderAddress != defaultSettingsFile.RelayOptions.SenderAddress ? serverUpdate.RelaySenderAddress : null;
            newRelaySettings.Login = serverUpdate.RelayLogin != defaultSettingsFile.RelayOptions.Login ? serverUpdate.RelayLogin : null;
            newRelaySettings.Password = serverUpdate.RelayPassword != defaultSettingsFile.RelayOptions.Password ? serverUpdate.RelayPassword : null;
            newRelaySettings.AutomaticEmails = serverUpdate.RelayAutomaticEmails.Where(s => !String.IsNullOrWhiteSpace(s)).ToArray() != defaultSettingsFile.RelayOptions.AutomaticEmails.Where(s => !String.IsNullOrWhiteSpace(s)).ToArray() ? serverUpdate.RelayAutomaticEmails.Where(s => !String.IsNullOrWhiteSpace(s)).ToArray() : null;
            newRelaySettings.AutomaticRelayExpression = serverUpdate.RelayAutomaticRelayExpression != defaultSettingsFile.RelayOptions.AutomaticRelayExpression ? serverUpdate.RelayAutomaticRelayExpression : null;

            newDesktopSettings.MinimiseToTrayIcon = serverUpdate.DesktopMinimiseToTrayIcon;

            System.IO.File.WriteAllText(editableSettingsFilePath,
                JsonSerializer.Serialize(new SettingsFile
                {
                    ServerOptions = newSettings,
                    RelayOptions = newRelaySettings,
                    DesktopOptions = newDesktopSettings
                },
                    SettingsFileSerializationContext.Default.SettingsFile
            ));

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


            if (!serverUpdate.IsRunning && this.pop3Server.IsRunning)
            {
                this.pop3Server.Stop();
            }
            else if (serverUpdate.IsRunning && !this.pop3Server.IsRunning)
            {
                this.pop3Server.TryStart();
            }

            return Ok();
        }
    }

    internal class SettingsFile
    {
        public ServerOptionsSource ServerOptions { get; set; }
        public RelayOptionsSource RelayOptions { get; set; }

        public DesktopOptionsSource DesktopOptions { get; set; }
    }

  
    [JsonSourceGenerationOptions(WriteIndented = true, DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull, ReadCommentHandling =JsonCommentHandling.Skip, UseStringEnumConverter = true)]
    [JsonSerializable(typeof(SettingsFile))]
    internal partial class SettingsFileSerializationContext : JsonSerializerContext
    {
    }
}