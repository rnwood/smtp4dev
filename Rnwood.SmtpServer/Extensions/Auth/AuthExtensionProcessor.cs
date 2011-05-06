using System.Collections.Generic;
using System.Linq;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class AuthExtensionProcessor : IExtensionProcessor
    {
        private readonly IConnection _processor;

        public AuthExtensionProcessor(IConnection connection)
        {
            _processor = connection;
            MechanismMap = new AuthMechanismMap();
            MechanismMap.Add(new CramMd5Mechanism());
            MechanismMap.Add(new PlainMechanism());
            MechanismMap.Add(new LoginMechanism());
            MechanismMap.Add(new AnonymousMechanism());
            connection.VerbMap.SetVerbProcessor("AUTH", new AuthVerb(this));
        }

        public AuthMechanismMap MechanismMap { get; private set; }

        public string[] EHLOKeywords
        {
            get
            {
                IEnumerable<IAuthMechanism> mechanisms = MechanismMap.GetAll();

                if (mechanisms.Any())
                {
                    return new[]
                               {
                                   "AUTH=" +
                                   string.Join(" ",
                                               mechanisms.Where(IsMechanismEnabled).Select(m => m.Identifier).ToArray())
                                   ,
                                   "AUTH " + string.Join(" ", mechanisms.Select(m => m.Identifier).ToArray())
                               };
                }
                else
                {
                    return new string[0];
                }
            }
        }

        public bool IsMechanismEnabled(IAuthMechanism mechanism)
        {
            return _processor.Server.Behaviour.IsAuthMechanismEnabled(_processor, mechanism);
        }
    }
}