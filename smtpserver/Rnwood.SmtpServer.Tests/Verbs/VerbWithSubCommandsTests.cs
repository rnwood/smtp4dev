// <copyright file="VerbWithSubCommandsTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Moq;
using Rnwood.SmtpServer.Verbs;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs;

/// <summary>
///     Defines the <see cref="VerbWithSubCommandsTests" />
/// </summary>
public class VerbWithSubCommandsTests
{
    /// <summary>
    ///     The ProcessAsync_RegisteredSubCommand_Processed
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task ProcessAsync_RegisteredSubCommand_Processed()
    {
        TestMocks mocks = new TestMocks();

        Mock<VerbWithSubCommands> verbWithSubCommands = new Mock<VerbWithSubCommands> { CallBase = true };
        Mock<IVerb> verb = new Mock<IVerb>();
        verbWithSubCommands.Object.SubVerbMap.SetVerbProcessor("SUBCOMMAND1", verb.Object);

        await verbWithSubCommands.Object.Process(mocks.Connection.Object, new SmtpCommand("VERB SUBCOMMAND1"))
            ;

        verb.Verify(v => v.Process(mocks.Connection.Object, new SmtpCommand("SUBCOMMAND1")));
    }

    /// <summary>
    ///     The ProcessAsync_UnregisteredSubCommand_ErrorResponse
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task ProcessAsync_UnregisteredSubCommand_ErrorResponse()
    {
        TestMocks mocks = new TestMocks();

        Mock<VerbWithSubCommands> verbWithSubCommands = new Mock<VerbWithSubCommands> { CallBase = true };

        await verbWithSubCommands.Object.Process(mocks.Connection.Object, new SmtpCommand("VERB SUBCOMMAND1"))
            ;

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.CommandParameterNotImplemented);
    }
}
