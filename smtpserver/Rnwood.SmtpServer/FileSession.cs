// <copyright file="FileSession.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System;
    using System.IO;
    using System.Net;

    /// <summary>
    /// Implements an <see cref="ISession"/> where the session log is saved to a file.
    /// </summary>
    /// <seealso cref="Rnwood.SmtpServer.AbstractSession" />
    public class FileSession : AbstractSession
    {
        private readonly FileInfo file;

        private readonly bool keepOnDispose;

        /// <summary>
        /// Initializes a new instance of the <see cref="FileSession"/> class.
        /// </summary>
        /// <param name="clientAddress">The clientAddress<see cref="IPAddress"/></param>
        /// <param name="startDate">The startDate<see cref="DateTime"/></param>
        /// <param name="file">The file<see cref="FileInfo"/></param>
        /// <param name="keepOnDispose">The keepOnDispose<see cref="bool"/></param>
        public FileSession(IPAddress clientAddress, DateTime startDate, FileInfo file, bool keepOnDispose)
            : base(clientAddress, startDate)
        {
            this.file = file;
            this.keepOnDispose = keepOnDispose;
        }

        /// <inheritdoc/>
        public override void AppendToLog(string text)
        {
            using (StreamWriter writer = this.file.AppendText())
            {
                writer.WriteLine(text);
            }
        }

        /// <inheritdoc/>
        public override TextReader GetLog()
        {
            return this.file.OpenText();
        }

        /// <summary>
        /// Releases unmanaged and - optionally - managed resources.
        /// </summary>
        /// <param name="disposing"><c>true</c> to release both managed and unmanaged resources; <c>false</c> to release only unmanaged resources.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                if (!this.keepOnDispose && this.file.Exists)
                {
                    this.file.Delete();
                }
            }
        }
    }
}
