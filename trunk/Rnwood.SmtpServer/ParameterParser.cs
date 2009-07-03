using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class ParameterParser
    {
        public ParameterParser(string[] tokens)
        {
            ParameterText = string.Join(" ", tokens);
            Parse(tokens);
        }

        public ParameterParser(string parameterText)
        {
            ParameterText = parameterText;
            Parse(ParameterText.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries));
        }

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

        private List<Parameter> _parameters = new List<Parameter>();

        public Parameter[] Parameters
        {
            get
            {
                return _parameters.ToArray();
            }
        }

        public string ParameterText
        {
            get;
            private set;
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
