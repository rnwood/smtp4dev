// <copyright file="SmtpCommandTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="SmtpCommandTests" />
/// </summary>
public class SmtpCommandTests
{
    /// <summary>
    ///     The Parsing_ArgsSeparatedByColon
    /// </summary>
    [Fact]
    public void Parsing_ArgsSeparatedByColon()
    {
        SmtpCommand command = new SmtpCommand("DATA:ARGS");
        Assert.True(command.IsValid);
        Assert.Equal("DATA", command.Verb);
        Assert.Equal("ARGS", command.ArgumentsText);
    }

    /// <summary>
    ///     The Parsing_ArgsSeparatedBySpace
    /// </summary>
    [Fact]
    public void Parsing_ArgsSeparatedBySpace()
    {
        SmtpCommand command = new SmtpCommand("DATA ARGS");
        Assert.True(command.IsValid);
        Assert.Equal("DATA", command.Verb);
        Assert.Equal("ARGS", command.ArgumentsText);
    }

    /// <summary>
    ///     The Parsing_SingleToken
    /// </summary>
    [Fact]
    public void Parsing_SingleToken()
    {
        SmtpCommand command = new SmtpCommand("DATA");
        Assert.True(command.IsValid);
        Assert.Equal("DATA", command.Verb);
        Assert.Equal("", command.ArgumentsText);
    }
}
