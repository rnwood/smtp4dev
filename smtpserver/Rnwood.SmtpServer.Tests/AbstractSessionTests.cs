// <copyright file="AbstractSessionTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests
{
    using System;
    using System.Linq;
    using System.Threading.Tasks;
    using Moq;
    using Xunit;

    /// <summary>
    /// Defines the <see cref="AbstractSessionTests" />
    /// </summary>
    public abstract class AbstractSessionTests
    {
        /// <summary>
        ///
        /// </summary>
        [Fact]
        public async Task AddMessage()
        {
            IEditableSession session = this.GetSession();
            Mock<IMessage> message = new Mock<IMessage>();

            session.AddMessage(message.Object);

            System.Collections.Generic.IReadOnlyCollection<IMessage> messages = await session.GetMessages();
            Assert.Single(messages);
            Assert.Same(message.Object, messages.First());
        }

        /// <summary>
        ///
        /// </summary>
        [Fact]
        public async Task AppendToLog()
        {
            IEditableSession session = this.GetSession();
            await session.AppendLineToSessionLog("Blah1");
            await session.AppendLineToSessionLog("Blah2");

            string sessionLog = (await session.GetLog()).ReadToEnd();
            Assert.Equal(new[] { "Blah1", "Blah2", "" },
                                    sessionLog.Split(new string[] { "\r\n" }, StringSplitOptions.None));
        }

        /// <summary>
        /// The GetMessages_InitiallyEmpty
        /// </summary>
        [Fact]
        public async Task GetMessages_InitiallyEmpty()
        {
            IEditableSession session = this.GetSession();
            Assert.Empty(await session.GetMessages());
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>The <see cref="IEditableSession"/></returns>
        protected abstract IEditableSession GetSession();
    }
}
