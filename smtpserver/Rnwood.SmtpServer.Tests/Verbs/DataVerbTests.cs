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
