// <copyright file="VerbMap.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;

namespace Rnwood.SmtpServer.Verbs;

/// <summary>
///     Defines the <see cref="VerbMap" />.
/// </summary>
public class VerbMap : IVerbMap
{
    private readonly Dictionary<string, IVerb> processorVerbs = new(StringComparer.OrdinalIgnoreCase);

    /// <inheritdoc />
    public virtual IVerb GetVerbProcessor(string verb)
    {
        processorVerbs.TryGetValue(verb, out IVerb result);
        return result;
    }

    /// <inheritdoc />
    public virtual void SetVerbProcessor(string verb, IVerb verbProcessor) => processorVerbs[verb] = verbProcessor;
}
