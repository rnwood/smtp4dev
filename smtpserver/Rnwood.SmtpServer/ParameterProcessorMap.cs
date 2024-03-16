// <copyright file="ParameterProcessorMap.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Rnwood.SmtpServer;

/// <summary>
///     Manages a set of processors which know how to manage the processing of parameter values
///     and handles dispatching of parameter values to them when a new command is received.
/// </summary>
public class ParameterProcessorMap
{
    private readonly Dictionary<string, IParameterProcessor> processors = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    ///     Gets the processor which is registered for the parameter with the given <paramref name="key" />
    ///     or null if none is found.
    /// </summary>
    /// <param name="key">The key<see cref="string" />.</param>
    /// <returns>The <see cref="IParameterProcessor" /> or null.</returns>
    public IParameterProcessor GetProcessor(string key)
    {
        processors.TryGetValue(key, out IParameterProcessor result);
        return result;
    }

    /// <summary>
    ///     Processes a set of parameters using the registered processors.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <param name="parameters">The parameters<see cref="ParameterParser" />.</param>
    /// <param name="throwOnUnknownParameter">The throwOnUnknownParameter<see cref="bool" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    public Task Process(IConnection connection, ParameterParser parameters, bool throwOnUnknownParameter)
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
                    new SmtpResponse(
                        StandardSmtpResponseCode.SyntaxErrorInCommandArguments,
                        "Parameter {0} is not recognised",
                        parameter.Name));
            }
        }

        return Task.CompletedTask;
    }

    /// <summary>
    ///     Processes a set of parameters using the registered processors.
    /// </summary>
    /// <param name="connection">The connection<see cref="IConnection" />.</param>
    /// <param name="arguments">The arguments<see cref="string" />.</param>
    /// <param name="throwOnUnknownParameter">The throwOnUnknownParameter<see cref="bool" />.</param>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    public async Task Process(IConnection connection, string[] arguments, bool throwOnUnknownParameter) =>
        await Process(connection, new ParameterParser(arguments), throwOnUnknownParameter).ConfigureAwait(false);

    /// <summary>
    ///     Sets the processor instance which will process the parameter with the given <paramref name="key" />.
    /// </summary>
    /// <param name="key">The key<see cref="string" />.</param>
    /// <param name="processor">The processor<see cref="IParameterProcessor" />.</param>
    public void SetProcessor(string key, IParameterProcessor processor) => processors[key] = processor;
}
