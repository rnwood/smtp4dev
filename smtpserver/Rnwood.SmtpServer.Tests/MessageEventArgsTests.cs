// <copyright file="MessageEventArgsTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="MessageEventArgsTests" />
/// </summary>
public class MessageEventArgsTests
{
    /// <summary>
    /// </summary>
    [Fact]
    public void Message()
    {
        IMessage message = new MemoryMessage();
        MessageEventArgs messageEventArgs = new MessageEventArgs(message);

        Assert.Same(message, messageEventArgs.Message);
    }
}
