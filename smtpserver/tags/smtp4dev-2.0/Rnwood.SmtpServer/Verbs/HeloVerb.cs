#region

using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer
{
    public class HeloVerb : IVerb
    {
        public void Process(IConnection connection, SmtpCommand command)
        {
            if (!string.IsNullOrEmpty(connection.Session.ClientName))
            {
                connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "You already said HELO"));
                return;
            }

            connection.Session.ClientName = command.Arguments[0];
            connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.OK, "Nice to meet you"));
        }
    }
}