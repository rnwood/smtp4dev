// <copyright file="ICurrentDateTimeProvider.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the <see cref="ICurrentDateTimeProvider" />.
/// </summary>
public interface ICurrentDateTimeProvider
{
    /// <summary>
    ///     Returns the current date and time.
    /// </summary>
    /// <returns>The <see cref="DateTime" />.</returns>
    DateTime GetCurrentDateTime();
}
