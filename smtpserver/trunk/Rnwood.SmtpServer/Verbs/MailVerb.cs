#region



#endregion

namespace Rnwood.SmtpServer.Verbs
{
    public class MailVerb : VerbWithSubCommands
    {
        public MailVerb()
        {
            SubVerbMap.SetVerbProcessor("FROM", new MailFromVerb());
        }
            
        public MailFromVerb FromSubVerb
        {
            get { return (MailFromVerb) SubVerbMap.GetVerbProcessor("FROM"); }
        }
    }
}