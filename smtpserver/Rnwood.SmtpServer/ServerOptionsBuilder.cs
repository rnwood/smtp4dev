// <copyright file="ServerOptionsBuilder.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Net;
using System.Net.Security;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;

namespace Rnwood.SmtpServer;

/// <summary>
///     Fluent builder for creating <see cref="ServerOptions" /> instances.
/// </summary>
public class ServerOptionsBuilder
{
    private bool allowRemoteConnections = false;
    private bool enableIpV6 = true;
    private string domainName = "localhost";
    private int portNumber = 25;
    private bool requireAuthentication = false;
    private string[] nonSecureAuthMechanismIds = Array.Empty<string>();
    private string[] secureAuthMechanismIds = Array.Empty<string>();
    private X509Certificate implicitTlsCertificate = null;
    private X509Certificate startTlsCertificate = null;
    private SslProtocols sslProtocols = SslProtocols.None;
    private TlsCipherSuite[] tlsCipherSuites = null;
    private long? maxMessageSize = null;
    private IPAddress bindAddress = null;

    /// <summary>
    ///     Sets whether remote connections to the server are allowed.
    /// </summary>
    /// <param name="allow">If set to <c>true</c>, remote connections are allowed.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithAllowRemoteConnections(bool allow = true)
    {
        this.allowRemoteConnections = allow;
        return this;
    }

    /// <summary>
    ///     Sets whether IPv6 dual stack should be enabled.
    /// </summary>
    /// <param name="enable">If set to <c>true</c>, IPv6 is enabled.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithEnableIpV6(bool enable = true)
    {
        this.enableIpV6 = enable;
        return this;
    }

    /// <summary>
    ///     Sets the domain name the server will send in greeting.
    /// </summary>
    /// <param name="domainName">The domain name.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithDomainName(string domainName)
    {
        this.domainName = domainName ?? throw new ArgumentNullException(nameof(domainName));
        return this;
    }

    /// <summary>
    ///     Sets the TCP port number on which to listen for connections.
    /// </summary>
    /// <param name="port">The port number.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithPort(int port)
    {
        this.portNumber = port;
        return this;
    }

    /// <summary>
    ///     Sets whether authentication is required.
    /// </summary>
    /// <param name="require">If set to <c>true</c>, authentication is required.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithRequireAuthentication(bool require = true)
    {
        this.requireAuthentication = require;
        return this;
    }

    /// <summary>
    ///     Sets the identifiers of AUTH mechanisms that will be allowed for insecure connections.
    /// </summary>
    /// <param name="mechanismIds">The mechanism identifiers.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithNonSecureAuthMechanisms(params string[] mechanismIds)
    {
        this.nonSecureAuthMechanismIds = mechanismIds ?? Array.Empty<string>();
        return this;
    }

    /// <summary>
    ///     Sets the identifiers of AUTH mechanisms that will be allowed for secure connections.
    /// </summary>
    /// <param name="mechanismIds">The mechanism identifiers.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithSecureAuthMechanisms(params string[] mechanismIds)
    {
        this.secureAuthMechanismIds = mechanismIds ?? Array.Empty<string>();
        return this;
    }

    /// <summary>
    ///     Sets the TLS certificate to use for implicit TLS.
    /// </summary>
    /// <param name="certificate">The certificate.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithImplicitTlsCertificate(X509Certificate certificate)
    {
        this.implicitTlsCertificate = certificate;
        return this;
    }

    /// <summary>
    ///     Sets the TLS certificate to use for STARTTLS.
    /// </summary>
    /// <param name="certificate">The certificate.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithStartTlsCertificate(X509Certificate certificate)
    {
        this.startTlsCertificate = certificate;
        return this;
    }

    /// <summary>
    ///     Sets the SSL protocol versions to allow.
    /// </summary>
    /// <param name="protocols">The SSL protocols.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithSslProtocols(SslProtocols protocols)
    {
        this.sslProtocols = protocols;
        return this;
    }

    /// <summary>
    ///     Sets the TLS cipher suites to allow.
    /// </summary>
    /// <param name="cipherSuites">The cipher suites.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithTlsCipherSuites(params TlsCipherSuite[] cipherSuites)
    {
        this.tlsCipherSuites = cipherSuites;
        return this;
    }

    /// <summary>
    ///     Sets the maximum message size in bytes accepted by the server.
    /// </summary>
    /// <param name="maxSize">The maximum message size.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithMaxMessageSize(long? maxSize)
    {
        this.maxMessageSize = maxSize;
        return this;
    }

    /// <summary>
    ///     Sets the specific IP address to bind to.
    /// </summary>
    /// <param name="address">The IP address, or null to use default behavior.</param>
    /// <returns>The builder instance for method chaining.</returns>
    public ServerOptionsBuilder WithBindAddress(IPAddress address)
    {
        this.bindAddress = address;
        return this;
    }

    /// <summary>
    ///     Builds the <see cref="ServerOptions" /> instance with the configured settings.
    /// </summary>
    /// <returns>A new <see cref="ServerOptions" /> instance.</returns>
    public ServerOptions Build()
    {
        return new ServerOptions(
            allowRemoteConnections,
            enableIpV6,
            domainName,
            portNumber,
            requireAuthentication,
            nonSecureAuthMechanismIds,
            secureAuthMechanismIds,
            implicitTlsCertificate,
            startTlsCertificate,
            sslProtocols,
            tlsCipherSuites,
            maxMessageSize,
            bindAddress
        );
    }
}
