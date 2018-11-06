// <copyright file="RsetVerbTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests.Verbs
{
    using System.Threading.Tasks;
    using Rnwood.SmtpServer.Verbs;
    using Xunit;

    /// <summary>
    /// Defines the <see cref="RsetVerbTests" />
    /// </summary>
    public class RsetVerbTests
    {
        /// <summary>
        ///
        /// </summary>
        /// <returns>A <see cref="Task{T}"/> representing the async operation</returns>
        [Fact]
        public async Task ProcessAsync()
        {
            TestMocks mocks = new TestMocks();

            RsetVerb verb = new RsetVerb();
            await verb.Process(mocks.Connection.Object, new SmtpCommand("RSET")).ConfigureAwait(false);

            mocks.VerifyWriteResponseAsync(StandardSmtpResponseCode.OK);
            mocks.Connection.Verify(c => c.AbortMessage());
        }
    }
}
