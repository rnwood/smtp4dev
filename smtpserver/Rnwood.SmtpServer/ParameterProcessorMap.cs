#region

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

#endregion

namespace Rnwood.SmtpServer
{
    public class ParameterProcessorMap
    {
        private readonly Dictionary<string, IParameterProcessor> _processors =
            new Dictionary<string, IParameterProcessor>(StringComparer.OrdinalIgnoreCase);

        public void SetProcessor(string key, IParameterProcessor processor)
        {
            _processors[key] = processor;
        }

        public IParameterProcessor GetProcessor(string key)
        {
            IParameterProcessor result = null;
            _processors.TryGetValue(key, out result);
            return result;
        }

        public async Task ProcessAsync(IConnection connection, string[] arguments, bool throwOnUnknownParameter)
        {
            await ProcessAsync(connection, new ParameterParser(arguments), throwOnUnknownParameter);
        }

        public Task ProcessAsync(IConnection connection, ParameterParser parameters, bool throwOnUnknownParameter)
        {
            foreach (Parameter parameter in parameters.Parameters)
            {
                IParameterProcessor parameterProcessor = GetProcessor(parameter.Name);

                if (parameterProcessor != null)
                {
                    parameterProcessor.SetParameter(connection, parameter.Name, parameter.Value);
                }
                else if (throwOnUnknownParameter)
                {
                    throw new SmtpServerException(
                        new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                                         "Parameter {0} is not recognised", parameter.Name));
                }
            }

            return Task.CompletedTask;
        }
    }
}