#region

using System;
using System.Collections.Generic;

#endregion

namespace Rnwood.SmtpServer.Verbs
{
    public class VerbMap
    {
        private readonly Dictionary<string, IVerb> _processorVerbs = new Dictionary<string, IVerb>();

        public void SetVerbProcessor(string verb, IVerb verbProcessor)
        {
            _processorVerbs[verb] = verbProcessor;
        }

        public IVerb GetVerbProcessor(string verb)
        {
            foreach (KeyValuePair<string, IVerb> processorEntry in _processorVerbs)
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