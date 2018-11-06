// <copyright file="FileMessageBuilder.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System;
    using System.Collections.Generic;
    using System.IO;
    using System.Threading.Tasks;

    /// <summary>
    /// Implements a message builder which will build a <see cref="FileMessage"/>
    /// </summary>
    /// <seealso cref="Rnwood.SmtpServer.IMessageBuilder" />
    public class FileMessageBuilder : IMessageBuilder
    {
        private readonly FileMessage message;

        private bool disposedValue = false; // To detect redundant calls

        /// <summary>
        /// Initializes a new instance of the <see cref="FileMessageBuilder"/> class.
        /// </summary>
        /// <param name="file">The file.</param>
        /// <param name="keepOnDispose">if set to <c>true</c> [keep on dispose].</param>
        public FileMessageBuilder(FileInfo file, bool keepOnDispose)
        {
            this.message = new FileMessage(file, keepOnDispose);
        }

        /// <inheritdoc/>
        public long? DeclaredMessageSize
        {
            get
            {
                return this.message.DeclaredMessageSize;
            }

            set
            {
                this.message.DeclaredMessageSize = value;
            }
        }

        /// <inheritdoc/>
        public bool EightBitTransport
        {
            get
            {
                return this.message.EightBitTransport;
            }

            set
            {
                this.EightBitTransport = value;
            }
        }

        /// <inheritdoc/>
        public string From
        {
            get
            {
                return this.message.From;
            }

            set
            {
                this.message.From = value;
            }
        }

        /// <inheritdoc/>
        public DateTime ReceivedDate
        {
            get
            {
                return this.message.ReceivedDate;
            }

            set
            {
                this.message.ReceivedDate = value;
            }
        }

        /// <inheritdoc/>
        public bool SecureConnection
        {
            get
            {
                return this.message.SecureConnection;
            }

            set
            {
                this.message.SecureConnection = value;
            }
        }

        /// <inheritdoc/>
        public ISession Session
        {
            get
            {
                return this.message.Session;
            }

            set
            {
                this.message.Session = value;
            }
        }

        /// <inheritdoc/>
        public ICollection<string> Recipients => this.message.RecipientsList;

        /// <inheritdoc/>
        public async Task<Stream> GetData()
        {
            return await this.message.GetData().ConfigureAwait(false);
        }

        /// <inheritdoc/>
        public Task<IMessage> ToMessage()
        {
            return Task.FromResult<IMessage>(this.message);
        }

        /// <inheritdoc/>
        public Task<Stream> WriteData()
        {
            return Task.FromResult<Stream>(this.message.File.OpenWrite());
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
        protected virtual void Dispose(bool disposing)
        {
            if (!this.disposedValue)
            {
                if (disposing)
                {
                }

                this.disposedValue = true;
            }
        }
    }
}
