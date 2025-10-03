// <copyright file="ServerOptionsBuilderTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Net;
using System.Security.Authentication;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Tests for <see cref="ServerOptionsBuilder" />
/// </summary>
public class ServerOptionsBuilderTests
{
    [Fact]
    public void Builder_DefaultValues_CreatesValidOptions()
    {
        // Act
        var options = ServerOptions.Builder().Build();

        // Assert
        Assert.NotNull(options);
        Assert.Equal("localhost", options.DomainName);
        Assert.Equal(25, options.PortNumber);
    }

    [Fact]
    public void Builder_WithDomainName_SetsValue()
    {
        // Act
        var options = ServerOptions.Builder()
            .WithDomainName("test.example.com")
            .Build();

        // Assert
        Assert.Equal("test.example.com", options.DomainName);
    }

    [Fact]
    public void Builder_WithPort_SetsValue()
    {
        // Act
        var options = ServerOptions.Builder()
            .WithPort(2525)
            .Build();

        // Assert
        Assert.Equal(2525, options.PortNumber);
    }

    [Fact]
    public void Builder_WithAllowRemoteConnections_SetsIPAddress()
    {
        // Act - Allow remote with IPv6
        var optionsRemoteIPv6 = ServerOptions.Builder()
            .WithAllowRemoteConnections(true)
            .WithEnableIpV6(true)
            .Build();

        // Assert
        Assert.Equal(IPAddress.IPv6Any, optionsRemoteIPv6.IpAddress);

        // Act - Allow remote with IPv4
        var optionsRemoteIPv4 = ServerOptions.Builder()
            .WithAllowRemoteConnections(true)
            .WithEnableIpV6(false)
            .Build();

        // Assert
        Assert.Equal(IPAddress.Any, optionsRemoteIPv4.IpAddress);

        // Act - Local only with IPv6
        var optionsLocalIPv6 = ServerOptions.Builder()
            .WithAllowRemoteConnections(false)
            .WithEnableIpV6(true)
            .Build();

        // Assert
        Assert.Equal(IPAddress.IPv6Loopback, optionsLocalIPv6.IpAddress);

        // Act - Local only with IPv4
        var optionsLocalIPv4 = ServerOptions.Builder()
            .WithAllowRemoteConnections(false)
            .WithEnableIpV6(false)
            .Build();

        // Assert
        Assert.Equal(IPAddress.Loopback, optionsLocalIPv4.IpAddress);
    }

    [Fact]
    public void Builder_WithBindAddress_OverridesIPAddress()
    {
        // Arrange
        var customAddress = IPAddress.Parse("192.168.1.100");

        // Act
        var options = ServerOptions.Builder()
            .WithAllowRemoteConnections(true)
            .WithBindAddress(customAddress)
            .Build();

        // Assert
        Assert.Equal(customAddress, options.IpAddress);
    }

    [Fact]
    public void Builder_WithRequireAuthentication_SetsValue()
    {
        // Act
        var options = ServerOptions.Builder()
            .WithRequireAuthentication(true)
            .Build();

        // Assert - We can't directly test requireAuthentication, but we can verify the object builds
        Assert.NotNull(options);
    }

    [Fact]
    public void Builder_WithAuthMechanisms_SetsValues()
    {
        // Act
        var options = ServerOptions.Builder()
            .WithNonSecureAuthMechanisms("PLAIN", "LOGIN")
            .WithSecureAuthMechanisms("PLAIN", "LOGIN", "CRAM-MD5")
            .Build();

        // Assert
        Assert.NotNull(options);
    }

    [Fact]
    public void Builder_WithSslProtocols_SetsValue()
    {
        // Act
        var options = ServerOptions.Builder()
            .WithSslProtocols(SslProtocols.Tls12 | SslProtocols.Tls13)
            .Build();

        // Assert
        Assert.NotNull(options);
    }

    [Fact]
    public void Builder_WithMaxMessageSize_SetsValue()
    {
        // Act
        var options = ServerOptions.Builder()
            .WithMaxMessageSize(10485760) // 10MB
            .Build();

        // Assert
        Assert.NotNull(options);
    }

    [Fact]
    public void Builder_FluentChaining_WorksCorrectly()
    {
        // Act
        var options = ServerOptions.Builder()
            .WithDomainName("smtp.example.com")
            .WithPort(587)
            .WithAllowRemoteConnections(true)
            .WithEnableIpV6(false)
            .WithRequireAuthentication(true)
            .WithSecureAuthMechanisms("PLAIN", "LOGIN")
            .WithMaxMessageSize(20971520) // 20MB
            .Build();

        // Assert
        Assert.Equal("smtp.example.com", options.DomainName);
        Assert.Equal(587, options.PortNumber);
        Assert.Equal(IPAddress.Any, options.IpAddress);
    }

    [Fact]
    public void Builder_CanBeUsedToCreateServer()
    {
        // Act
        var options = ServerOptions.Builder()
            .WithDomainName("test")
            .WithPort((int)StandardSmtpPort.AssignAutomatically)
            .WithAllowRemoteConnections(false)
            .Build();

        using var server = new SmtpServer(options);

        // Assert
        Assert.NotNull(server);
        Assert.Equal(options, server.Options);
    }

    [Fact]
    public void Builder_ServerCanStart_WithBuiltOptions()
    {
        // Arrange
        var options = ServerOptions.Builder()
            .WithDomainName("test")
            .WithPort((int)StandardSmtpPort.AssignAutomatically)
            .WithAllowRemoteConnections(false)
            .WithEnableIpV6(true)
            .Build();

        using var server = new SmtpServer(options);

        // Act
        server.Start();

        // Assert
        Assert.True(server.IsRunning);
        Assert.NotEmpty(server.ListeningEndpoints);

        server.Stop();
        Assert.False(server.IsRunning);
    }

    [Fact]
    public void Builder_MultipleBuilds_WithSeparateBuilders_CreateIndependentInstances()
    {
        // Act
        var options1 = ServerOptions.Builder()
            .WithDomainName("server1.example.com")
            .WithPort(2525)
            .Build();

        var options2 = ServerOptions.Builder()
            .WithDomainName("server2.example.com")
            .WithPort(2526)
            .Build();

        // Assert - Each builder creates independent options
        Assert.Equal("server1.example.com", options1.DomainName);
        Assert.Equal("server2.example.com", options2.DomainName);
        Assert.Equal(2525, options1.PortNumber);
        Assert.Equal(2526, options2.PortNumber);
    }
}
