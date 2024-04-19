// <copyright file="QuitVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs;

/// <summary>
///     Defines the <see cref="QuitVerbTests" />
/// </summary>
public class QuitVerbTests
{
    /// <summary>
    ///     The Quit_RespondsWithClosingChannel
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Quit_RespondsWithClosingChannel()
    {
        TestMocks mocks = new TestMocks();

        QuitVerb quitVerb = new QuitVerb();
        await quitVerb.Process(mocks.Connection.Object, new SmtpCommand("QUIT"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.ClosingTransmissionChannel);
    }
}
