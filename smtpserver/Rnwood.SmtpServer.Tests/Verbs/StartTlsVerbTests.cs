// <copyright file="StartTlsVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Security.Cryptography.X509Certificates;
using System.Threading.Tasks;
using Moq;
using Rnwood.SmtpServer.Extensions;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs;

/// <summary>
///     Defines the <see cref="StartTlsVerbTests" />
/// </summary>
public class StartTlsVerbTests
{
    /// <summary>
    ///     The NoCertificateAvailable_ReturnsErrorResponse
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task NoCertificateAvailable_ReturnsErrorResponse()
    {
        TestMocks mocks = new TestMocks();
        mocks.ServerOptions.Setup(b => b.GetSSLCertificate(It.IsAny<IConnection>()))
            .ReturnsAsync((X509Certificate)null);

        StartTlsVerb verb = new StartTlsVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("STARTTLS"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.CommandNotImplemented);
    }
}
