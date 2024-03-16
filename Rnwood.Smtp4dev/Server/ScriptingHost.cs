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
using Rnwood.SmtpServer;
using Serilog;

namespace Rnwood.Smtp4dev.Server;

internal class ScriptingHost
{
    private readonly ILogger log = Log.ForContext<ScriptingHost>();

    

    private IOptionsMonitor<RelayOptions> _relayOptions;

    public ScriptingHost(IOptionsMonitor<RelayOptions> relayOptions)
    {
        _relayOptions = relayOptions;
        _relayOptions.OnChange(o => ParseScripts(o));
        ParseScripts(relayOptions.CurrentValue);
    }

    private void ParseScripts(RelayOptions relayOptionsCurrentValue)
    {
        if (shouldRelaySource != relayOptionsCurrentValue.AutomaticRelayExpression)
        {
            var autoRelayExpression = relayOptionsCurrentValue.AutomaticRelayExpression ?? "";
            log.Information("Parsing AutomaticRelayExpression {autoRelayExpression}", autoRelayExpression);

            if (string.IsNullOrWhiteSpace(autoRelayExpression))
            {
                shouldRelayScript = null;
            }
            else
            {
                var parser = new JavaScriptParser();
                shouldRelayScript = parser.ParseScript(autoRelayExpression);
            }

            shouldRelaySource = autoRelayExpression;
        }
    }

    private string shouldRelaySource;
    private Script shouldRelayScript;

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
}