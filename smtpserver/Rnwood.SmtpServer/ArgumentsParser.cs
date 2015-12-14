using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class ArgumentsParser
    {
        public ArgumentsParser(string text)
        {
            this.Text = text;
            this.Arguments = ParseArguments(text);
        }

        public string[] Arguments { get; private set; }
        public string Text { get; private set; }

        private string[] ParseArguments(string argumentsText)
        {
            int ltCount = 0;
            List<string> arguments = new List<string>();
            StringBuilder currentArgument = new StringBuilder();
            foreach (char character in argumentsText)
            {
                switch (character)
                {
                    case '<':
                        ltCount++;
                        goto default;
                    case '>':
                        ltCount--;
                        goto default;
                    case ' ':
                        if (ltCount == 0)
                        {
                            arguments.Add(currentArgument.ToString());
                            currentArgument = new StringBuilder();
                        }
                        else
                        {
                            goto default;
                        }
                        break;

                    default:
                        currentArgument.Append(character);
                        break;
                }
            }

            if (currentArgument.Length != 0)
            {
                arguments.Add(currentArgument.ToString());
            }
            return arguments.ToArray();
        }
    }
}