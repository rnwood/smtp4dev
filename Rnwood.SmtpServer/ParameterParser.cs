#region

using System;
using System.Collections.Generic;

#endregion

namespace Rnwood.SmtpServer
{
    public class ParameterParser
    {
        private readonly List<Parameter> _parameters = new List<Parameter>();

        public ParameterParser(params string[] arguments)
        {
            Parse(arguments);
        }

        public Parameter[] Parameters
        {
            get { return _parameters.ToArray(); }
        }

        private void Parse(string[] tokens)
        {
            foreach (string token in tokens)
            {
                string[] tokenParts = token.Split(new[] { '=' }, 2, StringSplitOptions.RemoveEmptyEntries);
                string key = tokenParts[0];
                string value = tokenParts.Length > 1 ? tokenParts[1] : null;
                _parameters.Add(new Parameter(key, value));
            }
        }
    }
}