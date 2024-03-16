// <copyright file="SmtpResponseTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="SmtpResponseTests" />
/// </summary>
public class SmtpResponseTests
{
    /// <summary>
    /// </summary>
    [Fact]
    public void Code()
    {
        SmtpResponse r = new SmtpResponse(1, "Blah");
        Assert.Equal(1, r.Code);
    }

    /// <summary>
    ///     The Equality_Equal
    /// </summary>
    [Fact]
    public void Equality_Equal() =>
        Assert.True(
            new SmtpResponse(StandardSmtpResponseCode.OK, "OK").Equals(new SmtpResponse(StandardSmtpResponseCode.OK,
                "OK")));

    /// <summary>
    ///     The Equality_NotEqual
    /// </summary>
    [Fact]
    public void Equality_NotEqual() =>
        Assert.False(
            new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorCommandUnrecognised, "Eror").Equals(
                new SmtpResponse(StandardSmtpResponseCode.OK, "OK")));

    /// <summary>
    ///     The IsError_Error
    /// </summary>
    [Fact]
    public void IsError_Error()
    {
        SmtpResponse r = new SmtpResponse(500, "An error happened");
        Assert.True(r.IsError);
    }

    /// <summary>
    ///     The IsError_NotError
    /// </summary>
    [Fact]
    public void IsError_NotError()
    {
        SmtpResponse r = new SmtpResponse(200, "No error happened");
        Assert.False(r.IsError);
    }

    /// <summary>
    ///     The IsSuccess_Error
    /// </summary>
    [Fact]
    public void IsSuccess_Error()
    {
        SmtpResponse r = new SmtpResponse(500, "An error happened");
        Assert.False(r.IsSuccess);
    }

    /// <summary>
    ///     The IsSuccess_NotError
    /// </summary>
    [Fact]
    public void IsSuccess_NotError()
    {
        SmtpResponse r = new SmtpResponse(200, "No error happened");
        Assert.True(r.IsSuccess);
    }

    /// <summary>
    /// </summary>
    [Fact]
    public void Message()
    {
        SmtpResponse r = new SmtpResponse(1, "Blah");
        Assert.Equal("Blah", r.Message);
    }

    /// <summary>
    ///     The ToString_MultiLineMessage
    /// </summary>
    [Fact]
    public void ToString_MultiLineMessage()
    {
        SmtpResponse r = new SmtpResponse(200, "Multi line message line 1\r\n" +
                                               "Multi line message line 2\r\n" +
                                               "Multi line message line 3");
        Assert.Equal("200-Multi line message line 1\r\n" +
                     "200-Multi line message line 2\r\n" +
                     "200 Multi line message line 3\r\n", r.ToString());
    }

    /// <summary>
    ///     The ToString_SingleLineMessage
    /// </summary>
    [Fact]
    public void ToString_SingleLineMessage()
    {
        SmtpResponse r = new SmtpResponse(200, "Single line message");
        Assert.Equal("200 Single line message\r\n", r.ToString());
    }
}
