// <copyright file="FileMessageBuilderTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System.IO;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="FileMessageBuilderTests" />
/// </summary>
public class FileMessageBuilderTests : MessageBuilderTests
{
    /// <summary>
    /// </summary>
    /// <returns>The <see cref="IMessageBuilder" /></returns>
    protected override IMessageBuilder GetInstance()
    {
        FileInfo tempFile = new FileInfo(Path.GetTempFileName());

        TestMocks mocks = new TestMocks();
        return new FileMessageBuilder(tempFile, false);
    }
}
