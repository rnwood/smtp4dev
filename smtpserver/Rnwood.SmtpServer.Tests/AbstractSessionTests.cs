// <copyright file="AbstractSessionTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Tests
{
    using System;
    using System.Linq;
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
        public void AddMessage()
        {
            IEditableSession session = this.GetSession();
            Mock<IMessage> message = new Mock<IMessage>();

            session.AddMessage(message.Object);

            Assert.Single(session.GetMessages());
            Assert.Same(message.Object, session.GetMessages().First());
        }

        /// <summary>
        ///
        /// </summary>
        [Fact]
        public void AppendToLog()
        {
            IEditableSession session = this.GetSession();
            session.AppendToLog("Blah1");
            session.AppendToLog("Blah2");

            Assert.Equal(new[] { "Blah1", "Blah2", "" },
                                    session.GetLog().ReadToEnd().Split(new string[] { "\r\n" }, StringSplitOptions.None));
        }

        /// <summary>
        /// The GetMessages_InitiallyEmpty
        /// </summary>
        [Fact]
        public void GetMessages_InitiallyEmpty()
        {
            IEditableSession session = this.GetSession();
            Assert.Empty(session.GetMessages());
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns>The <see cref="IEditableSession"/></returns>
        protected abstract IEditableSession GetSession();
    }
}
