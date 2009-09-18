#region

using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer
{
    public class RcptToVerb : Verb
    {
        public override void Process(IConnectionProcessor connectionProcessor, SmtpCommand command)
        {
            if (connectionProcessor.CurrentMessage == null)
            {
                connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "No current message"));
                return;
            }

            if (command.ArgumentsText.Length < 3 || !command.ArgumentsText.StartsWith("<") ||
                !command.ArgumentsText.EndsWith(">"))
            {
                connectionProcessor.WriteResponse(
                    new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                                     "Must specify to address <address>"));
                return;
            }

            connectionProcessor.CurrentMessage.ToList.Add(command.ArgumentsText.TrimStart('<').TrimEnd('>'));
            connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Recipient accepted"));
        }
    }
}