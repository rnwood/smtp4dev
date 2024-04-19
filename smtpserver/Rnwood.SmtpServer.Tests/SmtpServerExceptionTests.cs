// <copyright file="SmtpServerExceptionTests.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using Xunit;

namespace Rnwood.SmtpServer.Tests;

/// <summary>
///     Defines the <see cref="SmtpServerExceptionTests" />
/// </summary>
public class SmtpServerExceptionTests
{
    /// <summary>
    /// </summary>
    [Fact]
    public void InnerException()
    {
        Exception innerException = new Exception();

        SmtpServerException e = new SmtpServerException(
            new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation, "Blah"), innerException);

        Assert.Same(innerException, e.InnerException);
    }

    /// <summary>
    /// </summary>
    [Fact]
    public void SmtpResponse()
    {
        SmtpResponse smtpResponse = new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation, "Blah");
        SmtpServerException e = new SmtpServerException(smtpResponse);

        Assert.Same(smtpResponse, e.SmtpResponse);
    }
}
