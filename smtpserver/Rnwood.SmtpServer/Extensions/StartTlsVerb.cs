// <copyright file="StartTlsVerb.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Net.Security;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer.Extensions;

/// <summary>
///     Defines the <see cref="StartTlsVerb" />.
/// </summary>
public class StartTlsVerb : IVerb
{
    /// <inheritdoc />
    public async Task Process(IConnection connection, SmtpCommand command)
    {
        X509Certificate certificate =
            await connection.Server.Options.GetSSLCertificate(connection).ConfigureAwait(false);

        if (certificate == null)
        {
            await connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.CommandNotImplemented,
                "TLS configuration error - no certificate")).ConfigureAwait(false);
            return;
        }

        await connection.WriteResponse(new SmtpResponse(
            StandardSmtpResponseCode.ServiceReady,
            "Ready to start TLS")).ConfigureAwait(false);

        SslProtocols sslProtos;

        string ver = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        if (ver == null || !ver.StartsWith(".NETCoreApp,"))
        {
            sslProtos = SslProtocols.Tls12 | SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Ssl3 |
                        SslProtocols.Ssl2;
        }
        else
        {
            sslProtos = SslProtocols.None;
        }

        await connection.ApplyStreamFilter(async stream =>
        {
            SslStream sslStream = new SslStream(stream);
            await sslStream.AuthenticateAsServerAsync(
                certificate,
                false,
                sslProtos,
                false).ConfigureAwait(false);
            return sslStream;
        }).ConfigureAwait(false);

        connection.Session.SecureConnection = true;
    }
}
