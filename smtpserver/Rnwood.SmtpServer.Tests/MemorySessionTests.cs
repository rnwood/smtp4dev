// <copyright file="MemorySessionTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Net;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="MemorySessionTests" />
/// </summary>
public class MemorySessionTests : AbstractSessionTests
{
    /// <summary>
    /// </summary>
    /// <returns>The <see cref="IEditableSession" /></returns>
    protected override IEditableSession GetSession() => new MemorySession(IPAddress.Loopback, DateTime.Now);
}
