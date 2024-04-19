// <copyright file="VerbMapTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using Moq;
using Rnwood.SmtpServer.Verbs;
using Xunit;

namespace Rnwood.SmtpServer.Tests.Verbs;

/// <summary>
///     Defines the <see cref="VerbMapTests" />
/// </summary>
public class VerbMapTests
{
    /// <summary>
    ///     The GetVerbProcessor_NoRegisteredVerb_ReturnsNull
    /// </summary>
    [Fact]
    public void GetVerbProcessor_NoRegisteredVerb_ReturnsNull()
    {
        VerbMap verbMap = new VerbMap();

        Assert.Null(verbMap.GetVerbProcessor("VERB"));
    }

    /// <summary>
    ///     The GetVerbProcessor_RegisteredVerb_ReturnsVerb
    /// </summary>
    [Fact]
    public void GetVerbProcessor_RegisteredVerb_ReturnsVerb()
    {
        VerbMap verbMap = new VerbMap();
        Mock<IVerb> verbMock = new Mock<IVerb>();

        verbMap.SetVerbProcessor("verb", verbMock.Object);

        Assert.Same(verbMock.Object, verbMap.GetVerbProcessor("verb"));
    }

    /// <summary>
    ///     The GetVerbProcessor_RegisteredVerbWithDifferentCase_ReturnsVerb
    /// </summary>
    [Fact]
    public void GetVerbProcessor_RegisteredVerbWithDifferentCase_ReturnsVerb()
    {
        VerbMap verbMap = new VerbMap();
        Mock<IVerb> verbMock = new Mock<IVerb>();

        verbMap.SetVerbProcessor("vErB", verbMock.Object);

        Assert.Same(verbMock.Object, verbMap.GetVerbProcessor("VERB"));
    }

    /// <summary>
    ///     The SetVerbProcessor_RegisteredVerbAgain_UpdatesRegistration
    /// </summary>
    [Fact]
    public void SetVerbProcessor_RegisteredVerbAgain_UpdatesRegistration()
    {
        VerbMap verbMap = new VerbMap();
        Mock<IVerb> verbMock1 = new Mock<IVerb>();
        Mock<IVerb> verbMock2 = new Mock<IVerb>();
        verbMap.SetVerbProcessor("verb", verbMock1.Object);

        verbMap.SetVerbProcessor("veRb", verbMock2.Object);

        Assert.Same(verbMock2.Object, verbMap.GetVerbProcessor("verb"));
    }

    /// <summary>
    ///     The SetVerbProcessor_RegisteredVerbAgainDifferentCaseWithNull_ClearsRegistration
    /// </summary>
    [Fact]
    public void SetVerbProcessor_RegisteredVerbAgainDifferentCaseWithNull_ClearsRegistration()
    {
        VerbMap verbMap = new VerbMap();
        Mock<IVerb> verbMock = new Mock<IVerb>();
        verbMap.SetVerbProcessor("verb", verbMock.Object);

        verbMap.SetVerbProcessor("vErb", null);

        Assert.Null(verbMap.GetVerbProcessor("verb"));
    }

    /// <summary>
    ///     The SetVerbProcessor_RegisteredVerbAgainWithNull_ClearsRegistration
    /// </summary>
    [Fact]
    public void SetVerbProcessor_RegisteredVerbAgainWithNull_ClearsRegistration()
    {
        VerbMap verbMap = new VerbMap();
        Mock<IVerb> verbMock = new Mock<IVerb>();
        verbMap.SetVerbProcessor("verb", verbMock.Object);

        verbMap.SetVerbProcessor("verb", null);

        Assert.Null(verbMap.GetVerbProcessor("verb"));
    }
}
