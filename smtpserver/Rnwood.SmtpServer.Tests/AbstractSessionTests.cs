// <copyright file="AbstractSessionTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Moq;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="AbstractSessionTests" />
/// </summary>
public abstract class AbstractSessionTests
{
    /// <summary>
    /// </summary>
    [Fact]
    public async Task AddMessage()
    {
        IEditableSession session = GetSession();
        Mock<IMessage> message = new Mock<IMessage>();

        await session.AddMessage(message.Object);

        IReadOnlyCollection<IMessage> messages = await session.GetMessages();
        Assert.Single(messages);
        Assert.Same(message.Object, messages.First());
    }

    /// <summary>
    /// </summary>
    [Fact]
    public async Task AppendToLog()
    {
        IEditableSession session = GetSession();
        await session.AppendLineToSessionLog("Blah1");
        await session.AppendLineToSessionLog("Blah2");

        string sessionLog = (await session.GetLog()).ReadToEnd();
        Assert.Equal(new[] { "Blah1", "Blah2", "" },
            sessionLog.Split(new[] { "\r\n" }, StringSplitOptions.None));
    }

    /// <summary>
    ///     The GetMessages_InitiallyEmpty
    /// </summary>
    [Fact]
    public async Task GetMessages_InitiallyEmpty()
    {
        IEditableSession session = GetSession();
        Assert.Empty(await session.GetMessages());
    }

    /// <summary>
    /// </summary>
    /// <returns>The <see cref="IEditableSession" /></returns>
    protected abstract IEditableSession GetSession();
}
