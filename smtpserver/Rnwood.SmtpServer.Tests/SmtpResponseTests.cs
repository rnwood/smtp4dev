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

    /// <summary>
    ///     The ToString_EmptyMessage
    /// </summary>
    [Fact]
    public void ToString_EmptyMessage()
    {
        SmtpResponse r = new SmtpResponse(200, string.Empty);
        Assert.Equal("200\r\n", r.ToString());
    }

    // RFC 5321 Section 4.2.1: Response format must be 3-digit code followed by optional text
    [Fact]
    public void RFC5321_ResponseFormat_ThreeDigitCode()
    {
        var response = new SmtpResponse(250, "OK");
        var result = response.ToString();
        
        Assert.StartsWith("250 ", result);
        Assert.EndsWith("\r\n", result);
        Assert.Equal("250 OK\r\n", result);
    }

    // RFC 5321 Section 4.2.1: Multi-line responses use hyphen for continuation
    [Fact]
    public void RFC5321_MultiLineResponse_HyphenForContinuation()
    {
        var response = new SmtpResponse(250, "smtp4dev ready\r\nSIZE 10240000\r\n8BITMIME\r\nOK");
        var result = response.ToString();
        
        var expected = "250-smtp4dev ready\r\n" +
                      "250-SIZE 10240000\r\n" +
                      "250-8BITMIME\r\n" +
                      "250 OK\r\n";
        Assert.Equal(expected, result);
    }

    // RFC 5321: Response codes must be in valid ranges
    [Fact]
    public void RFC5321_ResponseCodes_ValidRanges()
    {
        // 2xx - Success
        var success = new SmtpResponse(250, "OK");
        Assert.True(success.IsSuccess);
        Assert.False(success.IsError);

        // 3xx - Intermediate
        var intermediate = new SmtpResponse(354, "Start mail input");
        Assert.False(intermediate.IsSuccess);
        Assert.False(intermediate.IsError);

        // 4xx - Transient failure
        var transient = new SmtpResponse(450, "Mailbox unavailable");
        Assert.False(transient.IsSuccess);
        Assert.False(transient.IsError);

        // 5xx - Permanent failure
        var permanent = new SmtpResponse(550, "No such user");
        Assert.False(permanent.IsSuccess);
        Assert.True(permanent.IsError);
    }

    // RFC 5321 Section 4.2.1: Response text should not exceed recommended lengths
    [Fact]
    public void RFC5321_ResponseText_ReasonableLength()
    {
        var longMessage = new string('A', 512); // Reasonable upper bound
        var response = new SmtpResponse(250, longMessage);
        var result = response.ToString();
        
        Assert.Contains(longMessage, result);
        Assert.EndsWith("\r\n", result);
    }

    // RFC 5321: Standard response codes should match expectations
    [Fact]
    public void RFC5321_StandardResponseCodes_MatchRFC()
    {
        Assert.Equal(220, (int)StandardSmtpResponseCode.ServiceReady);
        Assert.Equal(221, (int)StandardSmtpResponseCode.ClosingTransmissionChannel);
        Assert.Equal(250, (int)StandardSmtpResponseCode.OK);
        Assert.Equal(354, (int)StandardSmtpResponseCode.StartMailInputEndWithDot);
        Assert.Equal(500, (int)StandardSmtpResponseCode.SyntaxErrorCommandUnrecognised);
        Assert.Equal(550, (int)StandardSmtpResponseCode.RecipientRejected);
    }

    // RFC 5321: Response must use CRLF line endings
    [Fact]
    public void RFC5321_Response_UsesCRLFLineEndings()
    {
        var response = new SmtpResponse(250, "Test\r\nMultiple\r\nLines");
        var result = response.ToString();
        
        // Should not contain bare LF or CR
        Assert.DoesNotContain("\n", result.Replace("\r\n", ""));
        Assert.DoesNotContain("\r", result.Replace("\r\n", ""));
        
        // Should end with CRLF
        Assert.EndsWith("\r\n", result);
    }
}
