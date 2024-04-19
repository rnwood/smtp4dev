// <copyright file="HeloVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs;

/// <summary>
///     Defines the <see cref="HeloVerbTests" />
/// </summary>
public class HeloVerbTests
{
    /// <summary>
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task SayHelo()
    {
        TestMocks mocks = new TestMocks();

        HeloVerb verb = new HeloVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO foo.blah"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        mocks.Session.VerifySet(s => s.ClientName = "foo.blah");
    }

    /// <summary>
    ///     The SayHelo_NoName
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task SayHelo_NoName()
    {
        TestMocks mocks = new TestMocks();

        HeloVerb verb = new HeloVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        mocks.Session.VerifySet(s => s.ClientName = "");
    }

    /// <summary>
    ///     The SayHeloTwice_ReturnsError
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task SayHeloTwice_ReturnsError()
    {
        TestMocks mocks = new TestMocks();
        mocks.Session.SetupGet(s => s.ClientName).Returns("already.said.helo");

        HeloVerb verb = new HeloVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO foo.blah"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.BadSequenceOfCommands);
    }
}
