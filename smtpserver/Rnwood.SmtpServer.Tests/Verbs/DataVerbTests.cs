// <copyright file="DataVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.IO;
using System.Text;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs;

/// <summary>
///     Defines the <see cref="DataVerbTests" />
/// </summary>
public class DataVerbTests
{
    /// <summary>
    ///     The Data_8BitData_PassedThrough
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Data_8BitData_PassedThrough()
    {
        string data = ((char)(0x41 + 128)).ToString();
        await TestGoodDataAsync(new[] { data, "." }, data);
    }

    /// <summary>
    ///     The Data_AboveSizeLimit_Rejected
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Data_AboveSizeLimit_Rejected()
    {
        TestMocks mocks = new TestMocks();

        MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
        mocks.ServerOptions.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).ReturnsAsync(10);

        string[] messageData = { new('x', 11), "." };
        int messageLine = 0;
        mocks.Connection.Setup(c => c.ReadLineBytes()).Returns(() =>
            Task.FromResult(Encoding.ASCII.GetBytes(messageData[messageLine++])));

        DataVerb verb = new DataVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.StartMailInputEndWithDot);
        mocks.VerifyWriteResponse(StandardSmtpResponseCode.ExceededStorageAllocation);

        mocks.Connection.Verify(c=>c.AbortMessage());
    }

    /// <summary>
    ///     The Data_DoubleDots_Unescaped
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Data_DoubleDots_Unescaped() =>
        //Check escaping of end of message character ".." is decoded to "."
        //but the .. after B should be left alone
        await TestGoodDataAsync(new[] { "A", "..", "B..", "." }, "A\r\n.\r\nB..");

    /// <summary>
    ///     The Data_EmptyMessage_Accepted
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Data_EmptyMessage_Accepted() => await TestGoodDataAsync(new[] { "." }, "");

    /// <summary>
    ///     The Data_ExactlySizeLimit_Accepted
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Data_ExactlySizeLimit_Accepted()
    {
        TestMocks mocks = new TestMocks();

        MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
        mocks.ServerOptions.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).ReturnsAsync(10);

        string[] messageData = { new('x', 10), "." };
        int messageLine = 0;
        mocks.Connection.Setup(c => c.ReadLineBytes())
            .Returns(() => Task.FromResult(Encoding.UTF8.GetBytes(messageData[messageLine++])));

        DataVerb verb = new DataVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.StartMailInputEndWithDot);
        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);


        mocks.Connection.Verify(c=>c.CommitMessage());
    }

      [Fact]
    public async Task Data_OptionsThrowsErrorOnCompletedMessage_Rejected()
    {
        TestMocks mocks = new TestMocks();

        MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
        mocks.ServerOptions.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).ReturnsAsync(10);
        mocks.ServerOptions.Setup(b => b.OnMessageCompleted(It.IsAny<IConnection>())).ThrowsAsync(new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.TransactionFailed, "No thanks!")));

        string[] messageData = { new('x', 10), "." };
        int messageLine = 0;
        mocks.Connection.Setup(c => c.ReadLineBytes())
            .Returns(() => Task.FromResult(Encoding.UTF8.GetBytes(messageData[messageLine++])));

        DataVerb verb = new DataVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.StartMailInputEndWithDot);
        mocks.VerifyWriteResponse(StandardSmtpResponseCode.TransactionFailed);

        mocks.Connection.Verify(c=>c.AbortMessage());
    }

    /// <summary>
    ///     The Data_NoCurrentMessage_ReturnsError
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Data_NoCurrentMessage_ReturnsError()
    {
        TestMocks mocks = new TestMocks();

        DataVerb verb = new DataVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.BadSequenceOfCommands);
    }

    /// <summary>
    ///     The Data_WithinSizeLimit_Accepted
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Data_WithinSizeLimit_Accepted()
    {
        TestMocks mocks = new TestMocks();

        MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
        mocks.ServerOptions.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).ReturnsAsync(10);

        string[] messageData = { new('x', 9), "." };
        int messageLine = 0;
        mocks.Connection.Setup(c => c.ReadLineBytes())
            .Returns(() => Task.FromResult(Encoding.UTF8.GetBytes(messageData[messageLine++])));

        DataVerb verb = new DataVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.StartMailInputEndWithDot);
        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);

        mocks.Connection.Verify(c=>c.CommitMessage());
    }

    /// <summary>
    /// If the SMTP server reports a maximum message size of 0, the server should not block the message.
    /// </summary>
    [Fact]
    public async Task Data_SizeLimitZero_Accepted()
    {
        TestMocks mocks = new TestMocks();

        MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
        mocks.ServerOptions.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>())).ReturnsAsync(0);

        string[] messageData = { new('x', 10), "." };
        int messageLine = 0;
        mocks.Connection.Setup(c => c.ReadLineBytes())
            .Returns(() => Task.FromResult(Encoding.UTF8.GetBytes(messageData[messageLine++])));

        DataVerb verb = new DataVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.StartMailInputEndWithDot);
        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);


        mocks.Connection.Verify(c => c.CommitMessage());
    }

    /// <summary>
    ///     RFC 5321 Section 4.1.1.9 - Test DATA without recipients (implementation behavior)
    /// </summary>
    [Fact]
    public async Task Data_NoRecipients_Processed()
    {
        TestMocks mocks = new TestMocks();
        
        MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
        // No recipients added to message builder
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
        mocks.ServerOptions.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>()))
            .ReturnsAsync((long?)null);

        // Mock the data reading as DATA verb will try to read the message
        string[] messageData = { "Test message body", "." };
        int messageLine = 0;
        mocks.Connection.Setup(c => c.ReadLineBytes())
            .Returns(() => Task.FromResult(Encoding.UTF8.GetBytes(messageData[messageLine++])));

        DataVerb verb = new DataVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

        // The implementation processes DATA even without recipients
        // This is common in SMTP servers - recipient validation happens elsewhere
        mocks.VerifyWriteResponse(StandardSmtpResponseCode.StartMailInputEndWithDot);
        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
    }

    /// <summary>
    ///     RFC 5321 Section 4.5.3.1.6 - Test line length limits (1000 chars including CRLF)
    /// </summary>
    [Fact]
    public async Task Data_LineLengthLimit_Accepted()
    {
        // Test line at exactly 998 characters (plus CRLF = 1000 total)
        string longLine = new string('A', 998);
        await TestGoodDataAsync(new[] { longLine, "." }, longLine);
    }

    /// <summary>
    ///     RFC 2822 Section 2.1.1 - Test header/body separation with blank line
    /// </summary>
    [Fact]
    public async Task Data_HeaderBodySeparation_Preserved()
    {
        string[] messageData = {
            "From: sender@example.com",
            "To: recipient@example.com", 
            "Subject: Test Message",
            "", // Blank line separating headers from body
            "This is the message body.",
            "Second line of body.",
            "."
        };
        
        string expectedData = "From: sender@example.com\r\n" +
                             "To: recipient@example.com\r\n" +
                             "Subject: Test Message\r\n" +
                             "\r\n" +
                             "This is the message body.\r\n" +
                             "Second line of body.";
        
        await TestGoodDataAsync(messageData, expectedData);
    }

    /// <summary>
    ///     RFC 5321 Section 4.5.3.1.4 - Test various message sizes
    /// </summary>
    [Fact]
    public async Task Data_VariousMessageSizes_HandledCorrectly()
    {
        // Very small message
        await TestGoodDataAsync(new[] { "Hi", "." }, "Hi");
        
        // Medium message
        string mediumContent = new string('M', 500);
        await TestGoodDataAsync(new[] { mediumContent, "." }, mediumContent);
    }

    /// <summary>
    ///     RFC 5321 Section 4.5.3.1.6 - Test dot transparency with complex scenarios
    /// </summary>
    [Fact]
    public async Task Data_DotTransparencyComplexScenarios_HandledCorrectly()
    {
        // Line starting with single dot (should be unescaped)
        await TestGoodDataAsync(new[] { ".Single dot line", "." }, "Single dot line");
        
        // Line starting with multiple dots
        await TestGoodDataAsync(new[] { "...Multiple dots", "." }, "..Multiple dots");
        
        // Line with dots in middle (should be unchanged)
        await TestGoodDataAsync(new[] { "Middle.dot.line", "." }, "Middle.dot.line");
        
        // Line ending with dot (should be unchanged)
        await TestGoodDataAsync(new[] { "Line ending with dot.", "." }, "Line ending with dot.");
    }

    /// <summary>
    ///     RFC 5321 - Test message with mixed content types
    /// </summary>
    [Fact]
    public async Task Data_MixedContent_PreservedCorrectly()
    {
        string[] messageData = {
            "Content-Type: multipart/mixed; boundary=boundary123",
            "",
            "--boundary123",
            "Content-Type: text/plain",
            "",
            "Plain text part",
            "--boundary123",
            "Content-Type: text/html",
            "",
            "<html><body>HTML part</body></html>",
            "--boundary123--",
            "."
        };
        
        string expectedData = "Content-Type: multipart/mixed; boundary=boundary123\r\n" +
                             "\r\n" +
                             "--boundary123\r\n" +
                             "Content-Type: text/plain\r\n" +
                             "\r\n" +
                             "Plain text part\r\n" +
                             "--boundary123\r\n" +
                             "Content-Type: text/html\r\n" +
                             "\r\n" +
                             "<html><body>HTML part</body></html>\r\n" +
                             "--boundary123--";
        
        await TestGoodDataAsync(messageData, expectedData);
    }

    /// <summary>
    /// </summary>
    /// <param name="messageData">The messageData<see cref="string" /></param>
    /// <param name="expectedData">The expectedData<see cref="string" /></param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    private async Task TestGoodDataAsync(string[] messageData, string expectedData)
    {
        TestMocks mocks = new TestMocks();

        MemoryMessageBuilder messageBuilder = new MemoryMessageBuilder();
        mocks.Connection.SetupGet(c => c.CurrentMessage).Returns(messageBuilder);
        mocks.ServerOptions.Setup(b => b.GetMaximumMessageSize(It.IsAny<IConnection>()))
            .ReturnsAsync((long?)null);

        int messageLine = 0;
        mocks.Connection.Setup(c => c.ReadLineBytes())
            .Returns(() => Task.FromResult(Encoding.UTF8.GetBytes(messageData[messageLine++])));

        DataVerb verb = new DataVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("DATA"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.StartMailInputEndWithDot);
        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);


        mocks.Connection.Verify(c=>c.CommitMessage());

        using StreamReader dataReader =
            new StreamReader(await messageBuilder.GetData(), Encoding.UTF8);
        Assert.Equal(expectedData, dataReader.ReadToEnd());
    }
}
