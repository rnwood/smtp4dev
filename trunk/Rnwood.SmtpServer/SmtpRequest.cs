using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace Rnwood.SmtpServer
{
    public class SmtpRequest
    {
        public static Regex SPLITREGEX = new Regex("[ :]");

        public SmtpRequest(string text)
        {
            Text = text;

            if (!string.IsNullOrEmpty(text))
            {
                string[] commandParts = SPLITREGEX.Split(text).Where(part => !string.IsNullOrEmpty(part)).ToArray();
                Verb = commandParts[0];
                Arguments = commandParts.Skip(1).ToArray();
                ArgumentsText = string.Join(" ", Arguments);
                IsValid = true;
            } else
            {
                IsValid = false;
                IsEmpty = true;
            }
        }

        public string Text { get; private set; }

        public string ArgumentsText { get; private set; }

        public string[] Arguments { get; private set; }

        public string Verb
        {
            get;
            private set;
        }

        public bool IsValid { get; private set; }
        public bool IsEmpty { get; private set; }
    }
}
