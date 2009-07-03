using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class AuthMechanismMap
    {

        private Dictionary<string, AuthMechanism> _map = new Dictionary<string, AuthMechanism>();

        public void Add(AuthMechanism mechanism)
        {
            _map[mechanism.Identifier] = mechanism;
        }

        public AuthMechanism Get(string identifier)
        {
            AuthMechanism result;
            _map.TryGetValue(identifier, out result);

            return result;
        }

        public IEnumerable<AuthMechanism> GetAll()
        {
            return _map.Values;
        }
    }
}
