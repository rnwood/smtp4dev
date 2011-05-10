#region



#endregion

namespace Rnwood.SmtpServer.Verbs
{
    public class RcptVerb : VerbWithSubCommands
    {
        public RcptVerb()
        {
            SubVerbMap.SetVerbProcessor("TO", new RcptToVerb());
        }
    }
}