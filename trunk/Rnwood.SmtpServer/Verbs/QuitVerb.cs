#region

using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer
{
    public class QuitVerb : Verb
    {
        public override void Process(IConnectionProcessor connectionProcessor, SmtpCommand command)
        {
            connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ClosingTransmissionChannel,
                                                               "See you later aligator"));
            connectionProcessor.CloseConnection();
            connectionProcessor.Session.SessionCompleted = true;
        }
    }
}