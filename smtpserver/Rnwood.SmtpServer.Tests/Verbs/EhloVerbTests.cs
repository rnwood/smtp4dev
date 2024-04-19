// <copyright file="EhloVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Moq;
using Rnwood.SmtpServer.Extensions;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs;

/// <summary>
///     Defines the <see cref="EhloVerbTests" />
/// </summary>
public class EhloVerbTests
{
    /// <summary>
    ///     The Process_NoArguments_Accepted
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_NoArguments_Accepted()
    {
        TestMocks mocks = new TestMocks();
        EhloVerb ehloVerb = new EhloVerb();
        await ehloVerb.Process(mocks.Connection.Object, new SmtpCommand("EHLO"));
        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);

        mocks.Session.VerifySet(s => s.ClientName = "");
    }

    /// <summary>
    ///     The Process_RecordsClientName
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_RecordsClientName()
    {
        TestMocks mocks = new TestMocks();
        EhloVerb ehloVerb = new EhloVerb();
        await ehloVerb.Process(mocks.Connection.Object, new SmtpCommand("EHLO foobar"));

        mocks.Session.VerifySet(s => s.ClientName = "foobar");
    }

    /// <summary>
    ///     The Process_RespondsWith250
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_RespondsWith250()
    {
        TestMocks mocks = new TestMocks();
        Mock<IExtensionProcessor> mockExtensionProcessor1 = new Mock<IExtensionProcessor>();
        mockExtensionProcessor1.Setup(ep => ep.GetEHLOKeywords()).ReturnsAsync(new[] { "EXTN1" });
        Mock<IExtensionProcessor> mockExtensionProcessor2 = new Mock<IExtensionProcessor>();
        mockExtensionProcessor2.Setup(ep => ep.GetEHLOKeywords()).ReturnsAsync(new[] { "EXTN2A", "EXTN2B" });

        mocks.Connection.SetupGet(c => c.ExtensionProcessors).Returns(new[]
        {
            mockExtensionProcessor1.Object, mockExtensionProcessor2.Object
        });

        EhloVerb ehloVerb = new EhloVerb();
        await ehloVerb.Process(mocks.Connection.Object, new SmtpCommand("EHLO foobar"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
    }

    /// <summary>
    ///     The Process_RespondsWithExtensionKeywords
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_RespondsWithExtensionKeywords()
    {
        TestMocks mocks = new TestMocks();
        Mock<IExtensionProcessor> mockExtensionProcessor1 = new Mock<IExtensionProcessor>();
        mockExtensionProcessor1.Setup(ep => ep.GetEHLOKeywords()).ReturnsAsync(new[] { "EXTN1" });
        Mock<IExtensionProcessor> mockExtensionProcessor2 = new Mock<IExtensionProcessor>();
        mockExtensionProcessor2.Setup(ep => ep.GetEHLOKeywords()).ReturnsAsync(new[] { "EXTN2A", "EXTN2B" });

        mocks.Connection.SetupGet(c => c.ExtensionProcessors).Returns(new[]
        {
            mockExtensionProcessor1.Object, mockExtensionProcessor2.Object
        });

        EhloVerb ehloVerb = new EhloVerb();
        await ehloVerb.Process(mocks.Connection.Object, new SmtpCommand("EHLO foobar"));

        mocks.Connection.Verify(c => c.WriteResponse(It.Is<SmtpResponse>(r =>
            r.Message.Contains("EXTN1") &&
            r.Message.Contains("EXTN2A") &&
            r.Message.Contains("EXTN2B")
        )));
    }

    /// <summary>
    ///     The Process_SaidHeloAlready_Allowed
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task Process_SaidHeloAlready_Allowed()
    {
        TestMocks mocks = new TestMocks();


        EhloVerb verb = new EhloVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("EHLO foo.blah"));
        await verb.Process(mocks.Connection.Object, new SmtpCommand("EHLO foo.blah"));
        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK, Times.Exactly(2));
    }
}
