// <copyright file="ParameterParserTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Linq;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="ParameterParserTests" />
/// </summary>
public class ParameterParserTests
{
    /// <summary>
    /// </summary>
    [Fact]
    public void MultipleParameters()
    {
        ParameterParser parameterParser = new ParameterParser("KEYA=VALUEA", "KEYB=VALUEB", "KEYC");

        Assert.Equal(3, parameterParser.Parameters.Count);
        Assert.Equal(new Parameter("KEYA", "VALUEA"), parameterParser.Parameters.First());
        Assert.Equal(new Parameter("KEYB", "VALUEB"), parameterParser.Parameters.ElementAt(1));
        Assert.Equal(new Parameter("KEYC", null), parameterParser.Parameters.ElementAt(2));
    }

    /// <summary>
    /// </summary>
    [Fact]
    public void NoParameters()
    {
        ParameterParser parameterParser = new ParameterParser();

        Assert.Empty(parameterParser.Parameters);
    }

    /// <summary>
    /// </summary>
    [Fact]
    public void SingleParameter()
    {
        ParameterParser parameterParser = new ParameterParser("KEYA=VALUEA");

        Assert.Single(parameterParser.Parameters);
        Assert.Equal(new Parameter("KEYA", "VALUEA"), parameterParser.Parameters.First());
    }
}
