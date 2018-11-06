// <copyright file="AbstractSession.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Net;
    using Rnwood.SmtpServer.Extensions.Auth;

    /// <summary>
    /// Provides a base implementation for <see cref="IEditableSession"/>
    /// </summary>
    /// <seealso cref="Rnwood.SmtpServer.IEditableSession" />
    public abstract class AbstractSession : IEditableSession
    {
        private List<IMessage> messages;

        /// <summary>
        /// Initializes a new instance of the <see cref="AbstractSession"/> class.
        /// </summary>
        /// <param name="clientAddress">The client address.</param>
        /// <param name="startDate">The start date.</param>
        internal AbstractSession(IPAddress clientAddress, DateTime startDate)
        {
            this.messages = new List<IMessage>();
            this.ClientAddress = clientAddress;
            this.StartDate = startDate;
        }

        /// <inheritdoc/>
        public bool Authenticated { get; set; }

        /// <inheritdoc/>
        public IAuthenticationCredentials AuthenticationCredentials { get; set; }

        /// <inheritdoc/>
        public IPAddress ClientAddress { get; set; }

        /// <inheritdoc/>
        public string ClientName { get; set; }

        /// <inheritdoc/>
        public bool CompletedNormally { get; set; }

        /// <inheritdoc/>
        public DateTime? EndDate { get; set; }

        /// <inheritdoc/>
        public bool SecureConnection { get; set; }

        /// <inheritdoc/>
        public Exception SessionError { get; set; }

        /// <inheritdoc/>
        public SessionErrorType SessionErrorType { get; set; }

        /// <inheritdoc/>
        public DateTime StartDate { get; set; }

        /// <inheritdoc/>
        public void AddMessage(IMessage message)
        {
            this.messages.Add(message);
        }

        /// <inheritdoc/>
        public abstract void AppendToLog(string text);

        /// <inheritdoc/>
        public abstract TextReader GetLog();

        /// <inheritdoc/>
        public IReadOnlyCollection<IMessage> GetMessages()
        {
            return this.messages.AsReadOnly();
        }

        /// <summary>
        /// Performs application-defined tasks associated with freeing, releasing, or resetting unmanaged resources.
        /// </summary>
        public void Dispose()
        {
            this.Dispose(true);
            GC.SuppressFinalize(this);
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected abstract void Dispose(bool disposing);
    }
}
