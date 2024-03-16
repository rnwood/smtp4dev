// <copyright file="StandardSmtpResponseCode.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer;

/// <summary>
///     Defines the StandardSmtpResponseCode.
/// </summary>
public enum StandardSmtpResponseCode
{
    /// <summary>
    ///     Defines the SyntaxErrorCommandUnrecognised
    /// </summary>
    SyntaxErrorCommandUnrecognised = 500,

    /// <summary>
    ///     Defines the SyntaxErrorInCommandArguments
    /// </summary>
    SyntaxErrorInCommandArguments = 501,

    /// <summary>
    ///     Defines the CommandNotImplemented
    /// </summary>
    CommandNotImplemented = 502,

    /// <summary>
    ///     Defines the BadSequenceOfCommands
    /// </summary>
    BadSequenceOfCommands = 503,

    /// <summary>
    ///     Defines the CommandParameterNotImplemented
    /// </summary>
    CommandParameterNotImplemented = 504,

    /// <summary>
    ///     Defines the ExceededStorageAllocation
    /// </summary>
    ExceededStorageAllocation = 552,

    /// <summary>
    ///     Defines the AuthenticationFailure
    /// </summary>
    AuthenticationFailure = 535,

    /// <summary>
    ///     Defines the AuthenticationRequired
    /// </summary>
    AuthenticationRequired = 530,

    /// <summary>
    ///     Defines the RecipientRejected
    /// </summary>
    RecipientRejected = 550,

    /// <summary>
    ///     Defines the TransactionFailed
    /// </summary>
    TransactionFailed = 554,

    /// <summary>
    ///     Defines the SystemStatusOrHelpReply
    /// </summary>
    SystemStatusOrHelpReply = 211,

    /// <summary>
    ///     Defines the HelpMessage
    /// </summary>
    HelpMessage = 214,

    /// <summary>
    ///     Defines the ServiceReady
    /// </summary>
    ServiceReady = 220,

    /// <summary>
    ///     Defines the ClosingTransmissionChannel
    /// </summary>
    ClosingTransmissionChannel = 221,

    /// <summary>
    ///     Defines the OK
    /// </summary>
    OK = 250,

    /// <summary>
    ///     Defines the UserNotLocalWillForwardTo
    /// </summary>
    UserNotLocalWillForwardTo = 251,

    /// <summary>
    ///     Defines the StartMailInputEndWithDot
    /// </summary>
    StartMailInputEndWithDot = 354,

    /// <summary>
    ///     Defines the AuthenticationContinue
    /// </summary>
    AuthenticationContinue = 334,

    /// <summary>
    ///     Defines the AuthenticationOK
    /// </summary>
    AuthenticationOK = 235
}
