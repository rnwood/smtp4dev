#region

using System;
using System.Collections.Generic;

#endregion

namespace Rnwood.SmtpServer
{
    public class ParameterParser
    {
        private readonly List<Parameter> _parameters = new List<Parameter>();

        public ParameterParser(string[] tokens)
        {
            ParameterText = string.Join(" ", tokens);
            Parse(tokens);
        }

        public ParameterParser(string parameterText)
        {
            ParameterText = parameterText;
            Parse(ParameterText.Split(new[] {' '}, StringSplitOptions.RemoveEmptyEntries));
        }

        public Parameter[] Parameters
        {
            get { return _parameters.ToArray(); }
        }

        public string ParameterText { get; private set; }

        private void Parse(string[] tokens)
        {
            foreach (string token in tokens)
            {
                string[] tokenParts = token.Split(new[] {'='}, 2, StringSplitOptions.RemoveEmptyEntries);
                string key = tokenParts[0];
                string value = tokenParts.Length > 1 ? tokenParts[1] : null;
                _parameters.Add(new Parameter(key, value));
            }
        }
    }

    public class Parameter
    {
        internal Parameter(string name, string value)
        {
            Name = name;
            Value = value;
        }

        public string Name { get; private set; }
        public string Value { get; private set; }
    }
}