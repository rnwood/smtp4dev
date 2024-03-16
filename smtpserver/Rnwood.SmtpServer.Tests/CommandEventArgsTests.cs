// <copyright file="CommandEventArgsTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="CommandEventArgsTests" />
/// </summary>
public class CommandEventArgsTests
{
    /// <summary>
    /// </summary>
    [Fact]
    public void Command()
    {
        SmtpCommand command = new SmtpCommand("BLAH");
        CommandEventArgs args = new CommandEventArgs(command);

        Assert.Same(command, args.Command);
    }
}
