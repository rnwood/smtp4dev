// <copyright file="CurrentDateTimeProvider.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
    using System;

    /// <summary>
    /// Implements <see cref="ICurrentDateTimeProvider"/> using the real local date time.
    /// </summary>
    /// <seealso cref="Rnwood.SmtpServer.ICurrentDateTimeProvider" />
    internal class CurrentDateTimeProvider : ICurrentDateTimeProvider
    {
        /// <inheritdoc/>
        public DateTime GetCurrentDateTime()
        {
            return DateTime.Now;
        }
    }
}
