// <copyright file="MailFromVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs;

/// <summary>
///     Defines the <see cref="MailFromVerbTests" />
/// </summary>
public class MailFromVerbTests
{
    /// <summary>
    ///     The Process_Address_Bracketed
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_Address_Bracketed() =>
        await Process_AddressAsync("<rob@rnwood.co.uk>", "rob@rnwood.co.uk", StandardSmtpResponseCode.OK)
            ;

    /// <summary>
    ///     The Process_Address_BracketedWithName
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_Address_BracketedWithName() =>
        await Process_AddressAsync("<Robert Wood <rob@rnwood.co.uk>>", "Robert Wood <rob@rnwood.co.uk>",
            StandardSmtpResponseCode.OK);

    /// <summary>
    ///     The Process_Address_NonAsciiChars_Rejected
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_NonAsciiChars_SmtpUtf8_Accepted() =>
        await Process_AddressAsync("<ظػؿقط <rob@rnwood.co.uk>>", "ظػؿقط <rob@rnwood.co.uk>",
            StandardSmtpResponseCode.OK, eightBitMessage: true);

    /// <summary>
    ///     The Process_Address_Plain
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_Address_Plain() =>
        await Process_AddressAsync("rob@rnwood.co.uk", "rob@rnwood.co.uk", StandardSmtpResponseCode.OK)
            ;

    /// <summary>
    ///     The Process_AlreadyGivenFrom_ErrorResponse
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_AlreadyGivenFrom_ErrorResponse()
    {
        TestMocks mocks = new TestMocks();
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(new Mock<IMessageBuilder>().Object);

        MailFromVerb mailFromVerb = new MailFromVerb();
        await mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM <foo@bar.com>"))
            ;

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.BadSequenceOfCommands);
    }

    /// <summary>
    ///     The Process_MissingAddress_ErrorResponse
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_MissingAddress_ErrorResponse()
    {
        TestMocks mocks = new TestMocks();

        MailFromVerb mailFromVerb = new MailFromVerb();
        await mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments);
    }

    /// <summary>
    ///     RFC 5321 Section 3.3 - Test null sender (bounce messages)
    /// </summary>
    [Fact]
    public async Task Process_NullSender_Accepted() =>
        await Process_AddressAsync("<>", "", StandardSmtpResponseCode.OK);

    /// <summary>
    ///     RFC 5321 Section 4.5.3.1.1 - Test forward path length limits (256 chars max)
    /// </summary>
    [Fact]
    public async Task Process_ForwardPathTooLong_Accepted()
    {
        // Test at exactly 256 character limit - should be accepted
        var longAddress = "<" + new string('a', 240) + "@example.com>";
        await Process_AddressAsync(longAddress, new string('a', 240) + "@example.com", StandardSmtpResponseCode.OK);
    }

    /// <summary>
    ///     RFC 5321 Section 4.1.1.3 - Test unrecognized parameter handling (should be rejected)
    /// </summary>
    [Fact]
    public async Task Process_WithUnrecognizedParameter_Rejected()
    {
        TestMocks mocks = new TestMocks();
        Mock<IMessageBuilder> message = new Mock<IMessageBuilder>();
        IMessageBuilder currentMessage = null;
        mocks.Connection.Setup(c => c.NewMessage()).ReturnsAsync(() =>
        {
            currentMessage = message.Object;
            return currentMessage;
        });
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(() => currentMessage);

        MailFromVerb mailFromVerb = new MailFromVerb();
        
        // Should throw exception for unrecognized SIZE parameter
        var exception = await Assert.ThrowsAsync<SmtpServerException>(() =>
            mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM <test@example.com> SIZE=1000")));
        
        Assert.Contains("SIZE", exception.Message);
    }

    /// <summary>
    ///     RFC 2822 Section 3.4 - Test various valid email address formats
    /// </summary>
    [Fact]
    public async Task Process_ValidEmailFormats_Accepted()
    {
        // Simple address
        await Process_AddressAsync("<user@domain.com>", "user@domain.com", StandardSmtpResponseCode.OK);
        
        // Address with subdomain
        await Process_AddressAsync("<user@mail.domain.com>", "user@mail.domain.com", StandardSmtpResponseCode.OK);
        
        // Address with plus sign (common for filtering)
        await Process_AddressAsync("<user+tag@domain.com>", "user+tag@domain.com", StandardSmtpResponseCode.OK);
        
        // Address with dot in local part
        await Process_AddressAsync("<first.last@domain.com>", "first.last@domain.com", StandardSmtpResponseCode.OK);
    }

    /// <summary>
    ///     RFC 5321 Section 4.1.1.1 - Test multiple MAIL FROM commands are rejected
    /// </summary>
    [Fact]
    public async Task Process_MultipleMailFrom_SecondRejected()
    {
        TestMocks mocks = new TestMocks();
        Mock<IMessageBuilder> message = new Mock<IMessageBuilder>();
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(message.Object);

        MailFromVerb mailFromVerb = new MailFromVerb();
        
        // Second MAIL FROM should be rejected
        await mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM <second@example.com>"));
        
        mocks.VerifyWriteResponse(StandardSmtpResponseCode.BadSequenceOfCommands);
    }

    /// <summary>
    ///     The Process_AddressAsync
    /// </summary>
    /// <param name="address">The address<see cref="string" /></param>
    /// <param name="expectedParsedAddress">The expectedParsedAddress<see cref="string" /></param>
    /// <param name="expectedResponse">The expectedResponse<see cref="StandardSmtpResponseCode" /></param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    private async Task Process_AddressAsync(string address, string expectedParsedAddress,
        StandardSmtpResponseCode expectedResponse, bool asException = false, bool eightBitMessage = false)
    {
        TestMocks mocks = new TestMocks();
        Mock<IMessageBuilder> message = new Mock<IMessageBuilder>();
        message.SetupGet(m => m.EightBitTransport).Returns(eightBitMessage);
        IMessageBuilder currentMessage = null;
        mocks.Connection.Setup(c => c.NewMessage()).ReturnsAsync(() =>
        {
            currentMessage = message.Object;
            return currentMessage;
        });
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(() => currentMessage);

        MailFromVerb mailFromVerb = new MailFromVerb();

        if (!asException)
        {
            await mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM " + address))
                ;
            mocks.VerifyWriteResponse(expectedResponse);
        }
        else
        {
            SmtpServerException e = await Assert.ThrowsAsync<SmtpServerException>(() =>
                mailFromVerb.Process(mocks.Connection.Object, new SmtpCommand("FROM " + address)));
            Assert.Equal((int)expectedResponse, e.SmtpResponse.Code);
        }

        if (expectedParsedAddress != null)
        {
            message.VerifySet(m => m.From = expectedParsedAddress);
        }
    }
}
