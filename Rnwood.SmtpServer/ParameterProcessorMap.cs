using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;

namespace Rnwood.SmtpServer
{
    public class ParameterProcessorMap
    {
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

        private Dictionary<string, IParameterProcessor> _processors = new Dictionary<string, IParameterProcessor>();

        public void Process(string[] tokens, bool throwOnUnknownParameter)
        {
            Process(new ParameterParser(tokens), throwOnUnknownParameter);
        }

        public void Process(string parametersString, bool throwOnUnknownParameter)
        {
            Process(new ParameterParser(parametersString), throwOnUnknownParameter);
        }

        public void Process(ParameterParser parameters, bool throwOnUnknownParameter)
        {
            foreach (Parameter parameter in parameters.Parameters)
            {
                IParameterProcessor parameterProcessor = GetProcessor(parameter.Name);

                if (parameterProcessor != null)
                {
                    parameterProcessor.SetParameter(parameter.Name, parameter.Value);
                } else if (throwOnUnknownParameter)
                {
                    throw new SmtpServerException(
                        new SmtpResponse(StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                                         "Parameter {0} is not recognised", parameter.Name));
                }

            }
        }
    }
}
