using System;
using System.IO;
using System.Text.Json;
using Microsoft.Extensions.Options;
using NSubstitute;
using Rnwood.Smtp4dev.Controllers;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Server.Settings;
using Rnwood.Smtp4dev.Service;
using Xunit;
using Microsoft.AspNetCore.Mvc;
using FluentAssertions;
using CommandLiners;

namespace Rnwood.Smtp4dev.Tests.Controllers
{
    public class ServerControllerTests
    {
        [Fact]
        public void UpdateServer_HostNameNotChanged_ShouldPreserveCurrentHostName()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var defaultSettingsPath = Path.Combine(tempDir, "default_appsettings.json");
            var editableSettingsPath = Path.Combine(tempDir, "editable_appsettings.json");

            // Create default settings with machine name as hostname
            var defaultSettings = new SettingsFile
            {
                ServerOptions = new ServerOptionsSource
                {
                    HostName = "MACHINE-NAME",
                    Port = 25
                }
            };

            // Create editable settings with localhost as hostname (user changed it)
            var editableSettings = new SettingsFile
            {
                ServerOptions = new ServerOptionsSource
                {
                    HostName = "localhost"
                }
            };

            File.WriteAllText(defaultSettingsPath, JsonSerializer.Serialize(defaultSettings, SettingsFileSerializationContext.Default.SettingsFile));
            File.WriteAllText(editableSettingsPath, JsonSerializer.Serialize(editableSettings, SettingsFileSerializationContext.Default.SettingsFile));

            // Mock dependencies
            var server = Substitute.For<ISmtp4devServer>();
            ImapServer imapServer = null; // Not needed for this test
            var serverOptions = Substitute.For<IOptionsMonitor<ServerOptions>>();
            var relayOptions = Substitute.For<IOptionsMonitor<RelayOptions>>();
            var desktopOptions = Substitute.For<IOptionsMonitor<DesktopOptions>>();
            var commandLineOptions = new MapOptions<CommandLineOptions>();
            var cmdLineOptions = new CommandLineOptions();
            var hostingEnvironmentHelper = Substitute.For<IHostingEnvironmentHelper>();

            // Mock the current server options to reflect the current state (hostname = localhost)
            var currentServerOptions = new ServerOptions { HostName = "localhost", Port = 25 };
            serverOptions.CurrentValue.Returns(currentServerOptions);

            var currentRelayOptions = new RelayOptions();
            relayOptions.CurrentValue.Returns(currentRelayOptions);

            hostingEnvironmentHelper.SettingsAreEditable.Returns(true);
            hostingEnvironmentHelper.GetDefaultSettingsFilePath().Returns(defaultSettingsPath);
            hostingEnvironmentHelper.GetEditableSettingsFilePath().Returns(editableSettingsPath);

            var controller = new ServerController(server, imapServer, serverOptions, relayOptions, desktopOptions, commandLineOptions, cmdLineOptions, hostingEnvironmentHelper);

            // Create server update that simulates saving from another tab (hostname should remain localhost)
            var serverUpdate = new ApiModel.Server
            {
                HostName = "localhost", // Current value from frontend
                Port = 587, // Different port value being saved
                IsRunning = true,
                TlsMode = "None",
                SmtpEnabledAuthTypesWhenNotSecureConnection = new string[0],
                SmtpEnabledAuthTypesWhenSecureConnection = new string[0],
                RelayTlsMode = "None",
                RelayAutomaticEmails = new string[0],
                Users = new UserOptions[0],
                Mailboxes = new MailboxOptions[0]
            };

            try
            {
                // Act
                var result = controller.UpdateServer(serverUpdate);

                // Assert
                result.Should().BeOfType<OkResult>();

                // Read the updated settings file
                var updatedContent = File.ReadAllText(editableSettingsPath);
                var updatedSettings = JsonSerializer.Deserialize(updatedContent, SettingsFileSerializationContext.Default.SettingsFile);

                // HostName should be preserved as "localhost", not reset to null (which would cause fallback to machine name)
                updatedSettings.ServerOptions.HostName.Should().Be("localhost");
                updatedSettings.ServerOptions.Port.Should().Be(587);
            }
            finally
            {
                // Cleanup
                if (File.Exists(defaultSettingsPath))
                    File.Delete(defaultSettingsPath);
                if (File.Exists(editableSettingsPath))
                    File.Delete(editableSettingsPath);
            }
        }

        [Fact]
        public void UpdateServer_HostNameChangedFromDefault_ShouldSaveNewHostName()
        {
            // Arrange
            var tempDir = Path.GetTempPath();
            var defaultSettingsPath = Path.Combine(tempDir, "default_appsettings.json");
            var editableSettingsPath = Path.Combine(tempDir, "editable_appsettings.json");

            // Create default settings with machine name as hostname
            var defaultSettings = new SettingsFile
            {
                ServerOptions = new ServerOptionsSource
                {
                    HostName = "MACHINE-NAME",
                    Port = 25
                }
            };

            // No existing editable settings (fresh install)
            File.WriteAllText(defaultSettingsPath, JsonSerializer.Serialize(defaultSettings, SettingsFileSerializationContext.Default.SettingsFile));

            // Mock dependencies
            var server = Substitute.For<ISmtp4devServer>();
            ImapServer imapServer = null; // Not needed for this test
            var serverOptions = Substitute.For<IOptionsMonitor<ServerOptions>>();
            var relayOptions = Substitute.For<IOptionsMonitor<RelayOptions>>();
            var desktopOptions = Substitute.For<IOptionsMonitor<DesktopOptions>>();
            var commandLineOptions = new MapOptions<CommandLineOptions>();
            var cmdLineOptions = new CommandLineOptions();
            var hostingEnvironmentHelper = Substitute.For<IHostingEnvironmentHelper>();

            // Mock the current server options to reflect default state (hostname = MACHINE-NAME)
            var currentServerOptions = new ServerOptions { HostName = "MACHINE-NAME", Port = 25 };
            serverOptions.CurrentValue.Returns(currentServerOptions);

            var currentRelayOptions = new RelayOptions();
            relayOptions.CurrentValue.Returns(currentRelayOptions);

            hostingEnvironmentHelper.SettingsAreEditable.Returns(true);
            hostingEnvironmentHelper.GetDefaultSettingsFilePath().Returns(defaultSettingsPath);
            hostingEnvironmentHelper.GetEditableSettingsFilePath().Returns(editableSettingsPath);

            var controller = new ServerController(server, imapServer, serverOptions, relayOptions, desktopOptions, commandLineOptions, cmdLineOptions, hostingEnvironmentHelper);

            // Create server update with changed hostname
            var serverUpdate = new ApiModel.Server
            {
                HostName = "localhost", // User changes from MACHINE-NAME to localhost
                Port = 25, // Keep default port
                IsRunning = true,
                TlsMode = "None",
                SmtpEnabledAuthTypesWhenNotSecureConnection = new string[0],
                SmtpEnabledAuthTypesWhenSecureConnection = new string[0],
                RelayTlsMode = "None",
                RelayAutomaticEmails = new string[0],
                Users = new UserOptions[0],
                Mailboxes = new MailboxOptions[0]
            };

            try
            {
                // Act
                var result = controller.UpdateServer(serverUpdate);

                // Assert
                result.Should().BeOfType<OkResult>();

                // Read the updated settings file
                var updatedContent = File.ReadAllText(editableSettingsPath);
                var updatedSettings = JsonSerializer.Deserialize(updatedContent, SettingsFileSerializationContext.Default.SettingsFile);

                // HostName should be saved as the new value
                updatedSettings.ServerOptions.HostName.Should().Be("localhost");
                // Port should be null (not changed from default)
                updatedSettings.ServerOptions.Port.Should().BeNull();
            }
            finally
            {
                // Cleanup
                if (File.Exists(defaultSettingsPath))
                    File.Delete(defaultSettingsPath);
                if (File.Exists(editableSettingsPath))
                    File.Delete(editableSettingsPath);
            }
        }
    }
}