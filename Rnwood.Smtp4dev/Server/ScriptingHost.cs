using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Esprima;
using Esprima.Ast;
using Jint;
using Jint.Native;
using Jint.Runtime;
using Microsoft.Extensions.Options;
using Rnwood.Smtp4dev.DbModel;
using Rnwood.SmtpServer;
using Rnwood.SmtpServer.Extensions.Auth;
using Serilog;

namespace Rnwood.Smtp4dev.Server;

internal class ScriptingHost
{
    private readonly ILogger log = Log.ForContext<ScriptingHost>();

    

    private IOptionsMonitor<RelayOptions> relayOptions;
    private IOptionsMonitor<ServerOptions> serverOptions;

    public ScriptingHost(IOptionsMonitor<RelayOptions> relayOptions, IOptionsMonitor<ServerOptions> serverOptions)
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

    private void ParseScripts(RelayOptions relayOptionsCurrentValue, ServerOptions serverOptionsCurrentValue)
    {
        ParseScript("AutomaticRelayExpression", relayOptionsCurrentValue.AutomaticRelayExpression, ref shouldRelayScript, ref shouldRelaySource);
        ParseScript("CredentialsValidationExpression", serverOptionsCurrentValue.CredentialsValidationExpression, ref credValidationScript,
            ref credValidationSource);
        ParseScript("RecipientValidationExpression", serverOptionsCurrentValue.RecipientValidationExpression, ref recipValidationScript,
            ref recipValidationSource);
    }

    private string shouldRelaySource;
    private Script shouldRelayScript;
    private string credValidationSource;
    private Script credValidationScript;
    private string recipValidationSource;
    private Script recipValidationScript;
    
    
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

            List<string> recpients = new List<string>();
            if (result.IsString())
            {
                if (result.AsString() != String.Empty)
                {
                    recpients.Add(result.AsString());
                }
            }
            else if (result.IsArray())
            {
                recpients.AddRange(result.AsArray().Select(v => v.AsString()));
            }
            else if (result.AsBoolean())
            {
                recpients.Add(recipient);
            }

            log.Information("AutomaticRelayExpression: (message: {message.Id}, recipient: {recipient}, session: {session.Id}) => {result} => {recipients}", message.Id, recipient,
                session.Id, result, recpients);

            return recpients;

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

    public AuthenticationResult ValidateCredentials(Session session, IAuthenticationCredentials credentials)
    {
        if (credValidationScript == null)
        {
            return AuthenticationResult.Success;
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
    
    public bool ValidateRecipient(Session session, string recipient)
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
}