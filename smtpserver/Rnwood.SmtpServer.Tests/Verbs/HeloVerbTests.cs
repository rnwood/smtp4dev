// <copyright file="HeloVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs;

/// <summary>
///     Defines the <see cref="HeloVerbTests" />
/// </summary>
public class HeloVerbTests
{
    /// <summary>
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task SayHelo()
    {
        TestMocks mocks = new TestMocks();

        HeloVerb verb = new HeloVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO foo.blah"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        mocks.Session.VerifySet(s => s.ClientName = "foo.blah");
    }

    /// <summary>
    ///     The SayHelo_NoName
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task SayHelo_NoName()
    {
        TestMocks mocks = new TestMocks();

        HeloVerb verb = new HeloVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        mocks.Session.VerifySet(s => s.ClientName = "");
    }

    /// <summary>
    ///     The SayHeloTwice_ReturnsError
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task SayHeloTwice_ReturnsError()
    {
        TestMocks mocks = new TestMocks();
        mocks.Session.SetupGet(s => s.ClientName).Returns("already.said.helo");

        HeloVerb verb = new HeloVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO foo.blah"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.BadSequenceOfCommands);
    }

    /// <summary>
    ///     RFC 5321 Section 4.1.1.1 - Test domain name format validation
    /// </summary>
    [Fact]
    public async Task Process_DomainNameFormats_Accepted()
    {
        TestMocks mocks = new TestMocks();

        HeloVerb verb = new HeloVerb();
        
        // Simple domain
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO example.com"));
        mocks.Session.VerifySet(s => s.ClientName = "example.com");
        
        // Subdomain
        mocks.Session.SetupGet(s => s.ClientName).Returns(""); // Reset for next test
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO mail.example.com"));
        mocks.Session.VerifySet(s => s.ClientName = "mail.example.com");
    }

    /// <summary>
    ///     RFC 5321 Section 4.1.1.1 - Test IP address literal format
    /// </summary>
    [Fact]
    public async Task Process_IPAddressLiteral_Accepted()
    {
        TestMocks mocks = new TestMocks();

        HeloVerb verb = new HeloVerb();
        
        // IPv4 address literal
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO [192.0.2.1]"));
        mocks.Session.VerifySet(s => s.ClientName = "[192.0.2.1]");
        
        // IPv6 address literal
        mocks.Session.SetupGet(s => s.ClientName).Returns(""); // Reset for next test
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO [IPv6:2001:db8::1]"));
        mocks.Session.VerifySet(s => s.ClientName = "[IPv6:2001:db8::1]");
    }

    /// <summary>
    ///     RFC 5321 Section 2.3.5 - Test domain name length limits (253 chars max)
    /// </summary>
    [Fact]
    public async Task Process_LongDomainName_Accepted()
    {
        TestMocks mocks = new TestMocks();

        // Create a domain name at the 253 character limit
        var longDomain = new string('a', 240) + ".example.com"; // 253 chars total
        
        HeloVerb verb = new HeloVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO " + longDomain));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        mocks.Session.VerifySet(s => s.ClientName = longDomain);
    }

    /// <summary>
    ///     RFC 5321 Section 4.1.4 - Test HELO with special characters
    /// </summary>
    [Fact]
    public async Task Process_SpecialCharacters_Accepted()
    {
        TestMocks mocks = new TestMocks();

        HeloVerb verb = new HeloVerb();
        
        // Domain with hyphens
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO mail-server.example-domain.com"));
        mocks.Session.VerifySet(s => s.ClientName = "mail-server.example-domain.com");
        
        // Domain with numbers
        mocks.Session.SetupGet(s => s.ClientName).Returns(""); // Reset for next test
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO mx1.example2.com"));
        mocks.Session.VerifySet(s => s.ClientName = "mx1.example2.com");
    }

    /// <summary>
    ///     RFC 5321 - Test case sensitivity preservation
    /// </summary>
    [Fact]
    public async Task Process_CaseSensitivity_Preserved()
    {
        TestMocks mocks = new TestMocks();

        HeloVerb verb = new HeloVerb();
        await verb.Process(mocks.Connection.Object, new SmtpCommand("HELO Example.COM"));

        mocks.VerifyWriteResponse(StandardSmtpResponseCode.OK);
        mocks.Session.VerifySet(s => s.ClientName = "Example.COM");
    }
}
