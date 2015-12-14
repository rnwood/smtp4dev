#region

using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

#endregion

namespace Rnwood.SmtpServer
{
    public class SmtpCommand : IEquatable<SmtpCommand>
    {
        public static Regex COMMANDREGEX = new Regex("(?'verb'[^ :]+)[ :]*(?'arguments'.*)");

        public SmtpCommand(string text)
        {
            Text = text;

            IsValid = false;
            IsEmpty = true;

            if (!string.IsNullOrEmpty(text))
            {
                Match match = COMMANDREGEX.Match(text);

                if (match.Success)
                {
                    Verb = match.Groups["verb"].Value;
                    ArgumentsText = match.Groups["arguments"].Value ?? "";
                    IsValid = true;
                }
            }
        }

        /// <summary>
        /// Gets the text.
        /// </summary>
        /// <value>
        /// The text.
        /// </value>
        public string Text { get; private set; }

        public string ArgumentsText { get; private set; }

        public string Verb { get; private set; }

        public bool IsValid { get; private set; }
        public bool IsEmpty { get; private set; }

        public bool Equals(SmtpCommand other)
        {
            if (ReferenceEquals(null, other)) return false;
            if (ReferenceEquals(this, other)) return true;
            return Equals(other.Text, Text);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj)) return false;
            if (ReferenceEquals(this, obj)) return true;
            if (obj.GetType() != typeof(SmtpCommand)) return false;
            return Equals((SmtpCommand)obj);
        }

        public override int GetHashCode()
        {
            return (Text != null ? Text.GetHashCode() : 0);
        }
    }
}