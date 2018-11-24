using System;
using System.Collections.Generic;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class SmtpStringBuilder
    {
        private readonly StringBuilder innerStringBuilder = new StringBuilder();

        public void AppendLine(string text)
        {
            this.innerStringBuilder.Append(text);
            this.innerStringBuilder.Append("\r\n");
        }

        public override string ToString()
        {
            return this.innerStringBuilder.ToString();
        }

    }
}
