// <copyright file="ParameterTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="ParameterTests" />
/// </summary>
public class ParameterTests
{
    /// <summary>
    ///     The Equality_Equal
    /// </summary>
    [Fact]
    public void Equality_Equal() =>
        Assert.True(new Parameter("KEYA", "VALUEA").Equals(new Parameter("KEYa", "VALUEA")));

    /// <summary>
    ///     The Equality_NotEqual
    /// </summary>
    [Fact]
    public void Equality_NotEqual() =>
        Assert.False(new Parameter("KEYb", "VALUEb").Equals(new Parameter("KEYa", "VALUEA")));

    /// <summary>
    /// </summary>
    [Fact]
    public void Name()
    {
        Parameter p = new Parameter("name", "value");

        Assert.Equal("name", p.Name);
    }

    /// <summary>
    /// </summary>
    [Fact]
    public void Value()
    {
        Parameter p = new Parameter("name", "value");

        Assert.Equal("value", p.Value);
    }
}
