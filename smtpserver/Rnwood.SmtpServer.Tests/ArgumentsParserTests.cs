// <copyright file="ArgumentsParserTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Linq;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="ArgumentsParserTests" />
/// </summary>
public class ArgumentsParserTests
{
    /// <summary>
    ///     The Parsing_FirstArgumentAferVerbWithColon_Split
    /// </summary>
    [Fact]
    public void Parsing_FirstArgumentAferVerbWithColon_Split()
    {
        ArgumentsParser args = new ArgumentsParser("ARG1=VALUE:BLAH");
        Assert.Single(args.Arguments);
        Assert.Equal("ARG1=VALUE:BLAH", args.Arguments.First());
    }

    /// <summary>
    ///     The Parsing_MailFrom_EmailOnly
    /// </summary>
    [Fact]
    public void Parsing_MailFrom_EmailOnly()
    {
        ArgumentsParser args = new ArgumentsParser("<rob@rnwood.co.uk> ARG1 ARG2");
        Assert.Equal("<rob@rnwood.co.uk>", args.Arguments.First());
        Assert.Equal("ARG1", args.Arguments.ElementAt(1));
        Assert.Equal("ARG2", args.Arguments.ElementAt(2));
    }

    /// <summary>
    ///     The Parsing_MailFrom_WithDisplayName
    /// </summary>
    [Fact]
    public void Parsing_MailFrom_WithDisplayName()
    {
        ArgumentsParser args = new ArgumentsParser("<Robert Wood<rob@rnwood.co.uk>> ARG1 ARG2");
        Assert.Equal("<Robert Wood<rob@rnwood.co.uk>>", args.Arguments.First());
        Assert.Equal("ARG1", args.Arguments.ElementAt(1));
        Assert.Equal("ARG2", args.Arguments.ElementAt(2));
    }
}
