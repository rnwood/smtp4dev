// <copyright file="NoopVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Rnwood.SmtpServer.Verbs;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs;

/// <summary>
///     Defines the <see cref="NoopVerbTests" />
/// </summary>
public class NoopVerbTests
{
    /// <summary>
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Noop()
    {
        TestMocks mocks = new TestMocks();

        NoopVerb verb = new NoopVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("NOOP"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
    }
}
