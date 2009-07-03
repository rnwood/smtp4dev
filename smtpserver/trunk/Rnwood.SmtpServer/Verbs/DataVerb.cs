using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class DataVerb : Verb
    {
        public override void Process(ConnectionProcessor connectionProcessor, SmtpRequest request)
        {
            if (connectionProcessor.CurrentMessage == null)
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands, "Bad sequence of commands"));
                return;
            }

            connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.StartMailInputEndWithDot, "End message with period"));
            StringBuilder message = new StringBuilder();

            do
            {
                string line = connectionProcessor.ReadLine();

                if (line != ".")
                {
                    line = ProcessLine(line + "\r\n");
                    message.Append(line);
                }
                else
                {
                    break;
                }

            } while (true);

            if (connectionProcessor.Server.MaxMessageSize.HasValue && message.Length > connectionProcessor.Server.MaxMessageSize.Value)
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ExceededStorageAllocation, "Message exceeds fixed size limit"));
            }
            else
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Mail accepted"));
                connectionProcessor.CurrentMessage.Data = message.ToString();
                connectionProcessor.CommitMessage();
            }
        }

        protected virtual string ProcessLine(string line)
        {
            //Remove escaping of end of message character
            if (line.StartsWith("."))
            {
                line.Remove(0, 1);
            }
            return line;
        }

    }
}
