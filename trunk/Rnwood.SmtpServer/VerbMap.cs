using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class VerbMap
    {
        private Dictionary<string, Verb> _processorVerbs = new Dictionary<string, Verb>();

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
