// <copyright file="MemorySessionTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests
{
    using System;
    using System.Net;

    /// <summary>
    /// Defines the <see cref="MemorySessionTests" />
    /// </summary>
    public class MemorySessionTests : AbstractSessionTests
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns>The <see cref="IEditableSession"/></returns>
        protected override IEditableSession GetSession()
        {
            return new MemorySession(IPAddress.Loopback, DateTime.Now);
        }
    }
}
