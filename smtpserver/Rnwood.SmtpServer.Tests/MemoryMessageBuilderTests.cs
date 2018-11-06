// <copyright file="MemoryMessageBuilderTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests
{
    /// <summary>
    /// Defines the <see cref="MemoryMessageBuilderTests" />
    /// </summary>
    public class MemoryMessageBuilderTests : MessageBuilderTests
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns>The <see cref="IMessageBuilder"/></returns>
        protected override IMessageBuilder GetInstance()
        {
            TestMocks mocks = new TestMocks();
            return new MemoryMessageBuilder();
        }
    }
}
