#region

using System.Linq;
using System.Text.RegularExpressions;
using System.Text;
using System.Collections.Generic;

#endregion

namespace Rnwood.SmtpServer
{
    public class SmtpCommand
    {
        public static Regex COMMANDREGEX = new Regex("(?'verb'[^ :]+)[ :]*(?'arguments'.*)");

        public SmtpCommand(string text)
        {
            Text = text;

            if (!string.IsNullOrEmpty(text))
            {
                Match match = COMMANDREGEX.Match(text);

                if (match.Success)
                {
                    Verb = match.Groups["verb"].Value;
                    ArgumentsText = match.Groups["arguments"].Value ?? "";
                    Arguments = ParseArguments(ArgumentsText);
                    IsValid = true;
                    return;
                }
            }

            IsValid = false;
            IsEmpty = true;
        }

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
                    case ':':
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
        public string Text { get; private set; }

        public string ArgumentsText { get; private set; }

        public string[] Arguments { get; private set; }

        public string Verb { get; private set; }

        public bool IsValid { get; private set; }
        public bool IsEmpty { get; private set; }
    }
}