using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Esprima;
using Esprima.Ast;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Microsoft.EntityFrameworkCore.Storage.ValueConversion;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.Smtp4dev.Server.Settings;
using Rnwood.SmtpServer;
using Rnwood.SmtpServer.Extensions.Auth;
using Serilog;

namespace Rnwood.Smtp4dev.Server;

public class ScriptingHost
{
    private readonly ILogger log = Log.ForContext<ScriptingHost>();



    private IOptionsMonitor<RelayOptions> relayOptions;
    private IOptionsMonitor<Settings.ServerOptions> serverOptions;

    public ScriptingHost(IOptionsMonitor<RelayOptions> relayOptions, IOptionsMonitor<Settings.ServerOptions> serverOptions)
    {
        this.relayOptions = relayOptions;
        this.serverOptions = serverOptions;
        this.relayOptions.OnChange(_ => ParseScripts(relayOptions.CurrentValue, serverOptions.CurrentValue));
        this.serverOptions.OnChange(_ => ParseScripts(relayOptions.CurrentValue, serverOptions.CurrentValue));
        ParseScripts(relayOptions.CurrentValue, serverOptions.CurrentValue);
    }

    private void ParseScript(string type, string expression, ref Script script, ref string source)
    {
        if (source != expression)
        {
            expression = expression ?? "";
            log.Information("Parsing {type} {expression}", type, expression);

            if (string.IsNullOrWhiteSpace(expression))
            {
                script = null;
            }
            else
            {
                var parser = new JavaScriptParser();
                script = parser.ParseScript(expression);
            }

            source = expression;
        }
    }

    private void ParseScripts(RelayOptions relayOptionsCurrentValue, Settings.ServerOptions serverOptionsCurrentValue)
    {
        ParseScript("AutomaticRelayExpression", relayOptionsCurrentValue.AutomaticRelayExpression, ref shouldRelayScript, ref shouldRelaySource);
        ParseScript("CredentialsValidationExpression", serverOptionsCurrentValue.CredentialsValidationExpression, ref credValidationScript,
            ref credValidationSource);
        ParseScript("RecipientValidationExpression", serverOptionsCurrentValue.RecipientValidationExpression, ref recipValidationScript,
            ref recipValidationSource);
        ParseScript("MessageValidationExpression", serverOptionsCurrentValue.MessageValidationExpression, ref messageValidationScript,
            ref messageValidationSource);
    }

    private string shouldRelaySource;
    private Script shouldRelayScript;
    private string credValidationSource;
    private Script credValidationScript;
    private string recipValidationSource;
    private Script recipValidationScript;

    private string messageValidationSource;
    private Script messageValidationScript;

    public bool HasValidateMessageExpression { get => this.messageValidationScript != null; }

    public IReadOnlyCollection<string> GetAutoRelayRecipients(ApiModel.Message message, string recipient, ApiModel.Session session)
    {
        if (shouldRelayScript == null)
        {
            return Array.Empty<string>();
        }

        Engine jsEngine = new Engine();

        jsEngine.SetValue("recipient", recipient);
        jsEngine.SetValue("message", message);
        jsEngine.SetValue("session", session);

        try
        {
            JsValue result = jsEngine.Evaluate(shouldRelayScript);

            List<string> relayRecipients = new List<string>();
            if (result.IsNull())
            {

            }
            else if (result.IsString())
            {
                if (result.AsString() != String.Empty)
                {
                    relayRecipients.Add(result.AsString());
                }
            }
            else if (result.IsArray())
            {
                relayRecipients.AddRange(result.AsArray().Select(v => v.AsString()));
            }
            else if (result.AsBoolean())
            {
                relayRecipients.Add(recipient);
            }

            log.Information("AutomaticRelayExpression: (message: {messageId}, recipient: {recipient}, session: {sessionId}) => {result} => {relayRecipients}", message.Id, recipient,
                session.Id, result, relayRecipients);

            return relayRecipients;

        }
        catch (JavaScriptException ex)
        {
            log.Error("Error executing AutomaticRelayExpression : {error}", ex.Error);
            return Array.Empty<string>();
        }
        catch (Exception ex)
        {
            log.Error("Error executing AutomaticRelayExpression : {error}", ex.ToString());
            return Array.Empty<string>();
        }

    }

    public AuthenticationResult? ValidateCredentials(ApiModel.Session session, IAuthenticationCredentials credentials)
    {
        if (credValidationScript == null)
        {
            return null;
        }

        Engine jsEngine = new Engine();

        jsEngine.SetValue("credentials", credentials);
        jsEngine.SetValue("session", session);

        try
        {
            JsValue result = jsEngine.Evaluate(credValidationScript);

            bool success = result.AsBoolean();

            log.Information("CredentialValidationExpression: (credentials: {credentials}, session: {session.Id}) => {result} => {success}", credentials,
                session.Id, result, success);

            return success ? AuthenticationResult.Success : AuthenticationResult.Failure;

        }
        catch (JavaScriptException ex)
        {
            log.Error("Error executing CredentialValidationExpression : {error}", ex.Error);
            return AuthenticationResult.TemporaryFailure;
        }
        catch (Exception ex)
        {
            log.Error("Error executing CredentialValidationExpression : {error}", ex.ToString());
            return AuthenticationResult.TemporaryFailure;
        }
    }

    public bool ValidateRecipient(ApiModel.Session session, string recipient)
    {
        if (recipValidationScript == null)
        {
            return true;
        }

        Engine jsEngine = new Engine();

        jsEngine.SetValue("recipient", recipient);
        jsEngine.SetValue("session", session);

        try
        {
            JsValue result = jsEngine.Evaluate(recipValidationScript);

            bool success = result.AsBoolean();

            log.Information("RecipientValidationExpression: (recipient: {recipient}, session: {session.Id}) => {result} => {success}", recipient,
                session.Id, result, success);

            return success;

        }
        catch (JavaScriptException ex)
        {
            log.Error("Error executing RecipientValidationExpression : {error}", ex.Error);
            return false;
        }
        catch (Exception ex)
        {
            log.Error("Error executing RecipientValidationExpression : {error}", ex.ToString());
            return false;
        }
    }

    internal SmtpResponse ValidateMessage(ApiModel.Message message, ApiModel.Session session)
    {
        if (messageValidationScript == null)
        {
            return null;
        }

        Engine jsEngine = new Engine();

        jsEngine.SetValue("message", message);
        jsEngine.SetValue("session", session);
        jsEngine.SetValue("error", (Action<int?, string>)((code, message) => throw new SmtpServerException(new SmtpResponse(code ?? (int)StandardSmtpResponseCode.TransactionFailed, message ?? ""))));

        try
        {
            JsValue result = jsEngine.Evaluate(messageValidationScript);

            SmtpResponse response;

            if (result.IsNull() || result.IsUndefined())
            {
                response = null;
            }
            else if (result.IsNumber())
            {
                response = new SmtpResponse((int)result.AsNumber(), "Message rejected by MessageValidationExpression");
            } else if (result.IsString()) {
                response = new SmtpResponse(StandardSmtpResponseCode.TransactionFailed, result.AsString());
            }
            else
            {
                response = result.AsBoolean() ? null : new SmtpResponse(StandardSmtpResponseCode.TransactionFailed, "Message rejected by MessageValidationExpression");
            }

            log.Information("MessageValidationExpression: (message: {message}, session: {session.Id}) => {result} => {success}", message,
                session.Id, result, response?.Code.ToString() ?? "Success");

            return response;

        }
        catch (SmtpServerException ex)
        {
            return ex.SmtpResponse;
        }
        catch (JavaScriptException ex)
        {
            log.Error("Error executing MessageValidationExpression : {error}", ex.Error);
            return new SmtpResponse(StandardSmtpResponseCode.TransactionFailed, "MessageValidationExpression failed");
        }
        catch (Exception ex)
        {
            log.Error("Error executing MessageValidationExpression : {error}", ex.ToString());
            return new SmtpResponse(StandardSmtpResponseCode.TransactionFailed, "MessageValidationExpression failed");
        }
    }
}