using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class AuthExtension : Extension
    {
        public override ExtensionProcessor CreateExtensionProcessor(ConnectionProcessor processor)
        {
            return new AuthExtensionProcessor(processor);
        }
    }

    public class AuthExtensionProcessor : ExtensionProcessor
    {
        public AuthExtensionProcessor(ConnectionProcessor processor)
        {
            MechanismMap = new AuthMechanismMap();
            MechanismMap.Add(new CramMd5Mechanism());
            MechanismMap.Add(new PlainMechanism());
            MechanismMap.Add(new LoginMechanism());
            MechanismMap.Add(new AnonymousMechanism());
            processor.VerbMap.SetVerbProcessor("AUTH", new AuthVerb(this));
        }

        public AuthMechanismMap MechanismMap
        {
            get;
            private set;
        }

        public override string[] GetEHLOKeywords()
        {
            IEnumerable<AuthMechanism> mechanisms = MechanismMap.GetAll();

            if (mechanisms.Any())
            {
                return new string[] { "AUTH=" + string.Join(" ", mechanisms.Select(m => m.Identifier).ToArray()),
                "AUTH " + string.Join(" ", mechanisms.Select(m => m.Identifier).ToArray())};
            }
            else
            {
                return new string[0];
            }
        }
    }

    public class AuthVerb : Verb
    {
        public AuthVerb(AuthExtensionProcessor authExtensionProcessor)
        {
            AuthExtensionProcessor = authExtensionProcessor;
        }

        public AuthExtensionProcessor AuthExtensionProcessor
        {
            get;
            private set;
        }


        public override void Process(ConnectionProcessor connectionProcessor, SmtpRequest request)
        {
            if (request.Arguments.Length > 0)
            {
                if (connectionProcessor.Session.Authenticated)
                {
                    throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands, "Already authenticated"));
                }

                string mechanismId = request.Arguments[0];
                AuthMechanism mechanism = AuthExtensionProcessor.MechanismMap.Get(mechanismId);

                if (mechanism == null)
                {
                    throw new SmtpServerException(
                        new SmtpResponse(StandardSmtpResponseCode.CommandParameterNotImplemented, "Specified AUTH mechanism not supported"));
                }

                AuthMechanismProcessor authMechanismProcessor = mechanism.CreateAuthMechanismProcessor(connectionProcessor);

                AuthMechanismProcessorStatus status = authMechanismProcessor.ProcessResponse(string.Join(" ", request.Arguments.Skip(1).ToArray()));
                while (status == AuthMechanismProcessorStatus.Continue)
                {
                    string response = connectionProcessor.ReadLine();
                    status = authMechanismProcessor.ProcessResponse(response);
                }

                if (status == AuthMechanismProcessorStatus.Success)
                {
                    connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenitcationOK, "Authenticated OK"));
                    connectionProcessor.Session.Authenticated = true;
                }
                else
                {
                    connectionProcessor.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure, "Authentication failure"));
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
