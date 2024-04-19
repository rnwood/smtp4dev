// <copyright file="MessageBuilderTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="MessageBuilderTests" />
/// </summary>
public abstract class MessageBuilderTests
{
    /// <summary>
    /// </summary>
    [Fact]
    public void AddTo()
    {
        IMessageBuilder builder = GetInstance();

        builder.Recipients.Add("foo@bar.com");
        builder.Recipients.Add("bar@foo.com");

        Assert.Equal(2, builder.Recipients.Count);
        Assert.Equal("foo@bar.com", builder.Recipients.ElementAt(0));
        Assert.Equal("bar@foo.com", builder.Recipients.ElementAt(1));
    }

    /// <summary>
    ///     The WriteData_Accepted
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation</returns>
    [Fact]
    public async Task WriteData_Accepted()
    {
        IMessageBuilder builder = GetInstance();

        byte[] writtenBytes = new byte[64 * 1024];
        new Random().NextBytes(writtenBytes);

        using (Stream stream = await builder.WriteData())
        {
            stream.Write(writtenBytes, 0, writtenBytes.Length);
        }

        byte[] readBytes;
        using (Stream stream = await builder.GetData())
        {
            readBytes = new byte[stream.Length];
            stream.Read(readBytes, 0, readBytes.Length);
        }

        Assert.Equal(writtenBytes, readBytes);
    }

    /// <summary>
    /// </summary>
    /// <returns>The <see cref="IMessageBuilder" /></returns>
    protected abstract IMessageBuilder GetInstance();
}
