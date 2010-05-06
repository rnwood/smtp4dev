#region

using System.Collections.Generic;
using System.Linq;
using Rnwood.SmtpServer.Verbs;

#endregion

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class AuthExtension : IExtension
    {
        public IExtensionProcessor CreateExtensionProcessor(IConnection connection)
        {
            return new AuthExtensionProcessor(connection);
        }
    }

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

    public class AuthVerb : IVerb
    {
        public AuthVerb(AuthExtensionProcessor authExtensionProcessor)
        {
            AuthExtensionProcessor = authExtensionProcessor;
        }

        public AuthExtensionProcessor AuthExtensionProcessor { get; private set; }


        public void Process(IConnection connection, SmtpCommand command)
        {
            if (command.Arguments.Length > 0)
            {
                if (connection.Session.Authenticated)
                {
                    throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "Already authenticated"));
                }

                string mechanismId = command.Arguments[0];
                IAuthMechanism mechanism = AuthExtensionProcessor.MechanismMap.Get(mechanismId);

                if (mechanism == null)
                {
                    throw new SmtpServerException(
                        new SmtpResponse(StandardSmtpResponseCode.CommandParameterNotImplemented,
                                         "Specified AUTH mechanism not supported"));
                }

                if (!AuthExtensionProcessor.IsMechanismEnabled(mechanism))
                {
                    throw new SmtpServerException(
                        new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure,
                                         "Specified AUTH mechanism not allowed right now (might require secure connection etc)"));
                }

                IAuthMechanismProcessor authMechanismProcessor =
                    mechanism.CreateAuthMechanismProcessor(connection);

                AuthMechanismProcessorStatus status =
                    authMechanismProcessor.ProcessResponse(string.Join(" ", command.Arguments.Skip(1).ToArray()));
                while (status == AuthMechanismProcessorStatus.Continue)
                {
                    string response = connection.ReadLine();

                    if (response == "*")
                    {
                        connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments, "Authentication aborted"));
                        return;
                    }

                    status = authMechanismProcessor.ProcessResponse(response);
                }

                if (status == AuthMechanismProcessorStatus.Success)
                {
                    connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenitcationOK,
                                                                       "Authenticated OK"));
                    connection.Session.Authenticated = true;
                    connection.Session.AuthenticationCredentials = authMechanismProcessor.Credentials;
                }
                else
                {
                    connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure,
                                                                       "Authentication failure"));
                }
            }
            else
            {
                throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                                                               "Must specify AUTH mechanism as a parameter"));
            }
        }
    }
}