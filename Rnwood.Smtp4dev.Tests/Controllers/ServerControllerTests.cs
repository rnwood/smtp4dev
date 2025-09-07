using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using NSubstitute;
using Rnwood.Smtp4dev.Controllers;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Server.Settings;
using Rnwood.Smtp4dev.Service;
using Xunit;
using FluentAssertions;
using CommandLiners;

namespace Rnwood.Smtp4dev.Tests.Controllers
{
    public class ServerControllerTests
    {
        // TODO: Add tests for hostname preservation logic when mocking infrastructure is improved
        // The fix in ServerController.cs line 256 removes the problematic condition that caused
        // hostname to be reset when saving from other tabs.
        //
        // The fix changed this line:
        // FROM: newSettings.HostName = serverUpdate.HostName != defaultSettingsFile.ServerOptions.HostName && serverUpdate.HostName != currentSettings.HostName ? serverUpdate.HostName : null;
        // TO:   newSettings.HostName = serverUpdate.HostName != defaultSettingsFile.ServerOptions.HostName ? serverUpdate.HostName : null;
        //
        // This ensures that hostname is preserved when it differs from the default, regardless of whether
        // it matches the current settings (which it should when the frontend sends the unchanged value).
    }
}