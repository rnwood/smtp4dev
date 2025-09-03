// <copyright file="RcptToVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs;

/// <summary>
///     Defines the <see cref="RcptToVerbTests" />
/// </summary>
public class RcptToVerbTests
{
    /// <summary>
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task EmailAddressOnly() =>
        await TestGoodAddressAsync("<rob@rnwood.co.uk>", "rob@rnwood.co.uk");

    /// <summary>
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task EmailAddressWithDisplayName() =>
        //Should this format be accepted????
        await TestGoodAddressAsync("<Robert Wood<rob@rnwood.co.uk>>", "Robert Wood<rob@rnwood.co.uk>")
            ;

    /// <summary>
    ///     The EmptyAddress_ReturnsError
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task EmptyAddress_ReturnsError() => await TestBadAddressAsync("<>");

    /// <summary>
    ///     The MismatchedBraket_ReturnsError
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task MismatchedBraket_ReturnsError()
    {
        await TestBadAddressAsync("<rob@rnwood.co.uk");
        await TestBadAddressAsync("<Robert Wood<rob@rnwood.co.uk>");
    }

    /// <summary>
    ///     The UnbraketedAddress_ReturnsError
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task UnbraketedAddress_ReturnsError() =>
        await TestBadAddressAsync("rob@rnwood.co.uk");


    [Fact]
    public async Task NonAsciiAddress_SmtpUtf8_Accepted() =>
        await TestGoodAddressAsync("<ظػؿقط <rob@rnwood.co.uk>>", "ظػؿقط <rob@rnwood.co.uk>", true)
            ;

    /// <summary>
    ///     RFC 5321 Section 4.1.1.2 - Test RCPT TO without prior MAIL FROM is rejected
    /// </summary>
    [Fact]
    public async Task Process_NoMailFrom_Rejected()
    {
        TestMocks mocks = new TestMocks();
        // No current message setup - simulates missing MAIL FROM
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns((IMessageBuilder)null);

        RcptToVerb verb = new RcptToVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("TO <test@example.com>"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.BadSequenceOfCommands);
    }

    /// <summary>
    ///     RFC 5321 Section 4.5.3.1.2 - Test forward path length limits
    /// </summary>
    [Fact]
    public async Task Process_ForwardPathLengthLimit_Accepted()
    {
        // Test at 256 character limit - should be accepted
        var longAddress = "<" + new string('a', 240) + "@example.com>";
        await TestGoodAddressAsync(longAddress, new string('a', 240) + "@example.com");
    }

    /// <summary>
    ///     RFC 5321 Section 3.3 - Test postmaster address handling (case insensitive)
    /// </summary>
    [Fact]
    public async Task Process_PostmasterAddress_Accepted()
    {
        await TestGoodAddressAsync("<postmaster>", "postmaster");
        await TestGoodAddressAsync("<POSTMASTER>", "POSTMASTER");
        await TestGoodAddressAsync("<Postmaster@domain.com>", "Postmaster@domain.com");
    }

    /// <summary>
    ///     RFC 2822 Section 3.4 - Test various valid recipient address formats
    /// </summary>
    [Fact]
    public async Task Process_ValidAddressFormats_Accepted()
    {
        // Simple address
        await TestGoodAddressAsync("<user@domain.com>", "user@domain.com");
        
        // Address with subdomain
        await TestGoodAddressAsync("<user@mail.domain.com>", "user@mail.domain.com");
        
        // Address with plus sign (common for filtering)
        await TestGoodAddressAsync("<user+tag@domain.com>", "user+tag@domain.com");
        
        // Address with dot in local part
        await TestGoodAddressAsync("<first.last@domain.com>", "first.last@domain.com");
    }

    /// <summary>
    ///     RFC 5321 Section 4.1.1.3 - Test multiple recipients are accepted
    /// </summary>
    [Fact]
    public async Task Process_MultipleRecipients_AllAccepted()
    {
        TestMocks mocks = new TestMocks();
        MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);

        RcptToVerb verb = new RcptToVerb();
        
        // Add first recipient
        await verb.Process(mocks.Connection.Object, new SmtpCommand("TO <first@example.com>"));
        
        // Add second recipient
        await verb.Process(mocks.Connection.Object, new SmtpCommand("TO <second@example.com>"));
        
        // Add third recipient
        await verb.Process(mocks.Connection.Object, new SmtpCommand("TO <third@example.com>"));

        Assert.Equal(3, messageBuilder.Recipients.Count);
        Assert.Contains("first@example.com", messageBuilder.Recipients);
        Assert.Contains("second@example.com", messageBuilder.Recipients);
        Assert.Contains("third@example.com", messageBuilder.Recipients);
    }

    /// <summary>
    ///     RFC 5321 - Test edge case with bracket validation logic
    /// </summary>
    [Fact]
    public async Task Process_BracketValidation_WorksAsImplemented()
    {
        // These should be rejected due to unequal bracket counts
        await TestBadAddressAsync("<test@example.com");
        await TestBadAddressAsync("test@example.com>");
        await TestBadAddressAsync("<<<test@example.com>");
        
        // These have equal bracket counts but are still parsed (implementation behavior)
        // The current implementation only checks that < count == > count
        // Then removes first character and everything from position (length-2) to end
        // For "<<test@example.com>>": removes first '<' and last '>', leaving "<test@example.com>"
        await TestGoodAddressAsync("<<test@example.com>>", "<test@example.com>");
    }


    /// <summary>
    /// </summary>
    /// <param name="address">The address<see cref="string" /></param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    private async Task TestBadAddressAsync(string address, bool asException = false)
    {
        TestMocks mocks = new TestMocks();
        MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);

        RcptToVerb verb = new RcptToVerb();

        if (!asException)
        {
            await verb.Process(mocks.Connection.Object, new SmtpCommand("TO " + address));
            mocks.VerifyWriteResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments);
        }
        else
        {
            SmtpServerException e = await Assert
                .ThrowsAsync<SmtpServerException>(() =>
                    verb.Process(mocks.Connection.Object, new SmtpCommand("TO " + address)));
            Assert.Equal((int)StandardSmtpResponseCode.SyntaxErrorInCommandArguments, e.SmtpResponse.Code);
        }

        Assert.Empty(messageBuilder.Recipients);
    }

    /// <summary>
    /// </summary>
    /// <param name="address">The address<see cref="string" /></param>
    /// <param name="expectedAddress">The expectedAddress<see cref="string" /></param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    private async Task TestGoodAddressAsync(string address, string expectedAddress, bool eightBit = false)
    {
        TestMocks mocks = new TestMocks();
        MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
        messageBuilder.EightBitTransport = eightBit;
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);

        RcptToVerb verb = new RcptToVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("TO " + address));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        Assert.Equal(expectedAddress, messageBuilder.Recipients.First());
    }
}
