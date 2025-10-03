// <copyright file="BackwardCompatibilityTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Security.Authentication;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Tests to ensure backward compatibility with existing code
/// </summary>
public class BackwardCompatibilityTests
{
    [Fact]
    public void LegacyConstructor_StillWorks()
    {
        // This test ensures the old constructor is still public and accessible
        var options = new ServerOptions(
            allowRemoteConnections: false,
            enableIpV6: true,
            domainName: "test",
            portNumber: (int)StandardSmtpPort.AssignAutomatically,
            requireAuthentication: false,
            nonSecureAuthMechanismIds: new string[0],
            secureAuthMechanismNamesIds: new string[0],
            implcitTlsCertificate: null,
            startTlsCertificate: null,
            sslProtocols: SslProtocols.None,
            tlsCipherSuites: null,
            maxMessageSize: null
        );

        Assert.NotNull(options);
        Assert.Equal("test", options.DomainName);
    }

    [Fact]
    public void LegacyConstructor_CanCreateWorkingServer()
    {
        var options = new ServerOptions(
            false, false, "test", (int)StandardSmtpPort.AssignAutomatically, false,
            new string[0], new string[0], null, null, SslProtocols.None, null, null
        );

        using var server = new SmtpServer(options);
        server.Start();

        Assert.True(server.IsRunning);
        Assert.NotEmpty(server.ListeningEndpoints);

        server.Stop();
        Assert.False(server.IsRunning);
    }
}
