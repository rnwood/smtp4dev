// <copyright file="IVerbMap.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Verbs;

/// <summary>
///     Defines the <see cref="IVerbMap" />.
/// </summary>
public interface IVerbMap
{
    /// <summary>
    ///     Gets the verb processor which is registered for the specified verb.
    /// </summary>
    /// <param name="verb">The verb.</param>
    /// <returns>The verb or null.</returns>
    IVerb GetVerbProcessor(string verb);

    /// <summary>
    ///     Sets the verb processor which is registered for a verb.
    /// </summary>
    /// <param name="verb">The verb<see cref="string" />.</param>
    /// <param name="verbProcessor">The verbProcessor<see cref="IVerb" />.</param>
    void SetVerbProcessor(string verb, IVerb verbProcessor);
}
