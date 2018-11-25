// <copyright file="VerbMap.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Verbs
{
    using System;
    using System.Collections.Generic;

    /// <summary>
    /// Defines the <see cref="VerbMap" />
    /// </summary>
    public class VerbMap : IVerbMap
    {
        private readonly Dictionary<string, IVerb> processorVerbs = new Dictionary<string, IVerb>(StringComparer.OrdinalIgnoreCase);

        /// <inheritdoc/>
        public IVerb GetVerbProcessor(string verb)
        {
            this.processorVerbs.TryGetValue(verb, out IVerb result);
            return result;
        }

        /// <inheritdoc/>
        public void SetVerbProcessor(string verb, IVerb verbProcessor)
        {
            this.processorVerbs[verb] = verbProcessor;
        }
    }
}
