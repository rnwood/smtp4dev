using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class AuthMechanismMap
    {

        private Dictionary<string, IAuthMechanism> _map = new Dictionary<string, IAuthMechanism>();

        public void Add(IAuthMechanism mechanism)
        {
            _map[mechanism.Identifier] = mechanism;
        }

        public IAuthMechanism Get(string identifier)
        {
            IAuthMechanism result;
            _map.TryGetValue(identifier, out result);

            return result;
        }

        public IEnumerable<IAuthMechanism> GetAll()
        {
            return _map.Values;
        }
    }
}
