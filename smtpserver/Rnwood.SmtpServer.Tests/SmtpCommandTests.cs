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

    // RFC 5321 Section 4.1.1.1: Commands are case-insensitive
    [Fact]
    public void RFC5321_Commands_CaseInsensitive()
    {
        var upperCase = new SmtpCommand("MAIL FROM:<test@example.com>");
        var lowerCase = new SmtpCommand("mail from:<test@example.com>");
        var mixedCase = new SmtpCommand("Mail From:<test@example.com>");

        Assert.True(upperCase.IsValid);
        Assert.True(lowerCase.IsValid);
        Assert.True(mixedCase.IsValid);

        // Verb should be preserved as-is (implementation choice)
        Assert.Equal("MAIL", upperCase.Verb);
        Assert.Equal("mail", lowerCase.Verb);
        Assert.Equal("Mail", mixedCase.Verb);
    }

    // RFC 5321 Section 4.1.1.4: Command line length must not exceed 512 octets
    [Fact]
    public void RFC5321_CommandLine_MaxLength512Octets()
    {
        // Create a command exactly 512 characters (including CRLF would make 514)
        var longCommand = "MAIL FROM:<" + new string('a', 486) + "@example.com>";
        Assert.Equal(510, longCommand.Length); // 510 + CRLF = 512

        var command = new SmtpCommand(longCommand);
        Assert.True(command.IsValid);
        Assert.Equal("MAIL", command.Verb);
        Assert.Contains("@example.com>", command.ArgumentsText);
    }

    // RFC 5321: Commands exceeding 512 octets should still be parsed (graceful handling)
    [Fact]
    public void RFC5321_CommandLine_ExceedingMaxLength_StillParsed()
    {
        var tooLongCommand = "MAIL FROM:<" + new string('a', 1000) + "@example.com>";
        
        var command = new SmtpCommand(tooLongCommand);
        Assert.True(command.IsValid); // Still parses, but would be rejected at protocol level
        Assert.Equal("MAIL", command.Verb);
    }

    // RFC 5321 Section 4.1.1: Valid SMTP verbs
    [Fact]
    public void RFC5321_StandardVerbs_ParseCorrectly()
    {
        var verbs = new[]
        {
            "HELO", "EHLO", "MAIL", "RCPT", "DATA", "RSET", "VRFY", "EXPN", "HELP", "NOOP", "QUIT"
        };

        foreach (var verb in verbs)
        {
            var command = new SmtpCommand(verb);
            Assert.True(command.IsValid, $"Verb {verb} should be valid");
            Assert.Equal(verb, command.Verb);
            Assert.Equal("", command.ArgumentsText);
        }
    }

    // RFC 5321: Command format with angle brackets and paths
    [Fact]
    public void RFC5321_MailCommands_AngleBracketPaths()
    {
        var mailFrom = new SmtpCommand("MAIL FROM:<user@example.com>");
        Assert.True(mailFrom.IsValid);
        Assert.Equal("MAIL", mailFrom.Verb);
        Assert.Equal("FROM:<user@example.com>", mailFrom.ArgumentsText);

        var rcptTo = new SmtpCommand("RCPT TO:<recipient@example.com>");
        Assert.True(rcptTo.IsValid);
        Assert.Equal("RCPT", rcptTo.Verb);
        Assert.Equal("TO:<recipient@example.com>", rcptTo.ArgumentsText);
    }

    // RFC 5321: Empty command should be handled
    [Fact]
    public void RFC5321_EmptyCommand_HandledGracefully()
    {
        var emptyCommand = new SmtpCommand("");
        Assert.False(emptyCommand.IsValid);
        Assert.True(emptyCommand.IsEmpty);
        Assert.Null(emptyCommand.Verb);
    }

    // RFC 5321: Null command should be handled
    [Fact]
    public void RFC5321_NullCommand_HandledGracefully()
    {
        var nullCommand = new SmtpCommand(null);
        Assert.False(nullCommand.IsValid);
        Assert.True(nullCommand.IsEmpty);
        Assert.Null(nullCommand.Verb);
    }

    // RFC 5321: Commands with only whitespace
    [Fact]
    public void RFC5321_WhitespaceOnlyCommand_Invalid()
    {
        var whitespaceCommand = new SmtpCommand("   ");
        Assert.False(whitespaceCommand.IsValid);
        Assert.True(whitespaceCommand.IsEmpty); // Whitespace is considered empty
    }

    // RFC 5321: Commands can use colon separator
    [Fact]
    public void RFC5321_ColonSeparator_ValidFormat()
    {
        var command = new SmtpCommand("MAIL:FROM:<test@example.com>");
        Assert.True(command.IsValid);
        Assert.Equal("MAIL", command.Verb);
        Assert.Equal("FROM:<test@example.com>", command.ArgumentsText);
    }

    // RFC 5321: Commands with special characters in arguments
    [Fact]
    public void RFC5321_SpecialCharacters_InArguments()
    {
        var command = new SmtpCommand("MAIL FROM:<test+tag@example.com>");
        Assert.True(command.IsValid);
        Assert.Equal("MAIL", command.Verb);
        Assert.Equal("FROM:<test+tag@example.com>", command.ArgumentsText);
    }

    // RFC 5321: EHLO with domain name
    [Fact]
    public void RFC5321_EhloCommand_WithDomain()
    {
        var command = new SmtpCommand("EHLO client.example.com");
        Assert.True(command.IsValid);
        Assert.Equal("EHLO", command.Verb);
        Assert.Equal("client.example.com", command.ArgumentsText);
    }
}
