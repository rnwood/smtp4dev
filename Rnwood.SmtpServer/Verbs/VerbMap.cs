#region

using System;
using System.Collections.Generic;

#endregion

namespace Rnwood.SmtpServer.Verbs
{
    public class VerbMap
    {
        private readonly Dictionary<string, Verb> _processorVerbs = new Dictionary<string, Verb>();

        public void SetVerbProcessor(string verb, Verb verbProcessor)
        {
            _processorVerbs[verb] = verbProcessor;
        }

        public Verb GetVerbProcessor(string verb)
        {
            foreach (KeyValuePair<string, Verb> processorEntry in _processorVerbs)
            {
                if (string.Equals(processorEntry.Key, verb, StringComparison.InvariantCultureIgnoreCase))
                {
                    return processorEntry.Value;
                }
            }

            return null;
        }
    }
}