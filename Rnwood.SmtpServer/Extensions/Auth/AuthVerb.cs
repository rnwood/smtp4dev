using Rnwood.SmtpServer.Verbs;
using System.Linq;

namespace Rnwood.SmtpServer.Extensions.Auth
{
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

                string initialData = null;
                if (command.Arguments.Length > 1)
                {
                    initialData = string.Join(" ", command.Arguments.Skip(1).ToArray());
                }

                AuthMechanismProcessorStatus status =
                    authMechanismProcessor.ProcessResponse(initialData);
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
                    connection.WriteResponse(new SmtpResponse(StandardSmtpResponseCode.AuthenticationOK,
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