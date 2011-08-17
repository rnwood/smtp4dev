#region

using System;
using System.Collections.Generic;

#endregion

namespace Rnwood.SmtpServer.Verbs
{
    public class VerbMap : IVerbMap
    {
        private readonly Dictionary<string, IVerb> _processorVerbs = new Dictionary<string, IVerb>(StringComparer.InvariantCultureIgnoreCase);

        public void SetVerbProcessor(string verb, IVerb verbProcessor)
        {
            _processorVerbs[verb] = verbProcessor;
        }

        public IVerb GetVerbProcessor(string verb)
        {
            IVerb result = null;
            _processorVerbs.TryGetValue(verb, out result);
            return result;
        }
    }
}