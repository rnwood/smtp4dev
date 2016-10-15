using Rnwood.SmtpServer.Verbs;
using System.Linq;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer.Extensions.Auth
{
    public class AuthVerb : IVerb
    {
        public AuthVerb(AuthExtensionProcessor authExtensionProcessor)
        {
            AuthExtensionProcessor = authExtensionProcessor;
        }

        public AuthExtensionProcessor AuthExtensionProcessor { get; private set; }

        public async Task ProcessAsync(IConnection connection, SmtpCommand command)
        {
            ArgumentsParser argumentsParser = new ArgumentsParser(command.ArgumentsText);

            if (argumentsParser.Arguments.Length > 0)
            {
                if (connection.Session.Authenticated)
                {
                    throw new SmtpServerException(new SmtpResponse(StandardSmtpResponseCode.BadSequenceOfCommands,
                                                                   "Already authenticated"));
                }

                string mechanismId = argumentsParser.Arguments[0];
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
                if (argumentsParser.Arguments.Length > 1)
                {
                    initialData = string.Join(" ", argumentsParser.Arguments.Skip(1).ToArray());
                }

                AuthMechanismProcessorStatus status =
                    await authMechanismProcessor.ProcessResponseAsync(initialData);
                while (status == AuthMechanismProcessorStatus.Continue)
                {
                    string response = await connection.ReadLineAsync();

                    if (response == "*")
                    {
                        await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments, "Authentication aborted"));
                        return;
                    }

                    status = await authMechanismProcessor.ProcessResponseAsync(response);
                }

                if (status == AuthMechanismProcessorStatus.Success)
                {
                    await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.AuthenticationOK,
                                                              "Authenticated OK"));
                    connection.Session.Authenticated = true;
                    connection.Session.AuthenticationCredentials = authMechanismProcessor.Credentials;
                }
                else
                {
                    await connection.WriteResponseAsync(new SmtpResponse(StandardSmtpResponseCode.AuthenticationFailure,
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