// <copyright file="Connection.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Security;
using System.Reflection;
using System.Runtime.Versioning;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;
using Rnwood.SmtpServer.Extensions;
using Rnwood.SmtpServer.Verbs;

namespace Rnwood.SmtpServer;

/// <summary>
///     Represents a single SMTP server from a client to the server.
/// </summary>
public class Connection : IConnection
{
    private readonly string id;

    /// <summary>
    ///     Initializes a new instance of the <see cref="Connection" /> class.
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="session">The session.</param>
    /// <param name="connectionChannel">The connection channel.</param>
    /// <param name="verbMap">The verb map.</param>
    /// <param name="extensionProcessors">The extension processors.</param>
    internal Connection(ISmtpServer server, IEditableSession session, IConnectionChannel connectionChannel,
        IVerbMap verbMap, Func<IConnection, IExtensionProcessor[]> extensionProcessors)
    {
        id = $"[RemoteIP={connectionChannel.ClientIPAddress}]";

        ConnectionChannel = connectionChannel;
        ConnectionChannel.ClosedEventHandler += OnConnectionChannelClosed;

        VerbMap = verbMap;
        Session = session;
        Server = server;
        ExtensionProcessors = extensionProcessors(this).ToArray();
    }

    private IConnectionChannel ConnectionChannel { get; }

    /// <inheritdoc />
    public event AsyncEventHandler<ConnectionEventArgs> ConnectionClosedEventHandler;

    /// <inheritdoc />
    public IMessageBuilder CurrentMessage { get; private set; }

    /// <inheritdoc />
    public MailVerb MailVerb => (MailVerb)VerbMap.GetVerbProcessor("MAIL");

    /// <inheritdoc />
    public ISmtpServer Server { get; }

    /// <inheritdoc />
    public IEditableSession Session { get; }

    /// <inheritdoc />
    public IVerbMap VerbMap { get; }

    /// <summary>
    ///     Gets a list of extensions which are available for this connection.
    /// </summary>
    public IReadOnlyCollection<IExtensionProcessor> ExtensionProcessors { get; }

    /// <inheritdoc />
    public Task AbortMessage()
    {
        CurrentMessage = null;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public async Task ApplyStreamFilter(Func<Stream, Task<Stream>> filter) =>
        await ConnectionChannel.ApplyStreamFilter(filter).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task CloseConnection() => await ConnectionChannel.Close().ConfigureAwait(false);

    /// <inheritdoc />
    public async Task CommitMessage()
    {
        IMessage message = await CurrentMessage.ToMessage().ConfigureAwait(false);
        await Session.AddMessage(message).ConfigureAwait(false);
        CurrentMessage = null;

        await Server.Options.OnMessageReceived(this, message).ConfigureAwait(false);
    }

    /// <inheritdoc />
    public async Task<IMessageBuilder> NewMessage()
    {
        CurrentMessage = await Server.Options.OnCreateNewMessage(this).ConfigureAwait(false);
        CurrentMessage.Session = Session;
        return CurrentMessage;
    }

    /// <inheritdoc />
    public async Task<string> ReadLine()
    {
        string text = await ConnectionChannel.ReadLine().ConfigureAwait(false);
        await Session.AppendLineToSessionLog(text).ConfigureAwait(false);
        return text;
    }

    /// <inheritdoc />
    public async Task WriteResponse(SmtpResponse response) =>
        await WriteLineAndFlush(response.ToString().TrimEnd()).ConfigureAwait(false);

    /// <inheritdoc />
    public async Task<byte[]> ReadLineBytes()
    {
        byte[] data = await ConnectionChannel.ReadLineBytes();

        await Session.AppendLineToSessionLog(Encoding.GetEncoding("ISO-8859-1").GetString(data)).ConfigureAwait(false);

        return data;
    }

    /// <summary>
    ///     Returns a <see cref="string" /> that represents this instance.
    /// </summary>
    /// <returns>
    ///     A <see cref="string" /> that represents this instance.
    /// </returns>
    public override string ToString() => id;

    /// <summary>
    ///     Creates the a connection for the specified server and channel..
    /// </summary>
    /// <param name="server">The server.</param>
    /// <param name="connectionChannel">The connection channel.</param>
    /// <param name="verbMap">The verb map.</param>
    /// <returns>An <see cref="Task{T}" /> representing the async operation.</returns>
    internal static async Task<Connection> Create(ISmtpServer server, IConnectionChannel connectionChannel,
        IVerbMap verbMap)
    {
        IEditableSession session = await server.Options.OnCreateNewSession(connectionChannel).ConfigureAwait(false);
        IEnumerable<IExtension> extensions =
            await server.Options.GetExtensions(connectionChannel).ConfigureAwait(false);

        IExtensionProcessor[] CreateConnectionExtensions(IConnection c)
        {
            return extensions.Select(e => e.CreateExtensionProcessor(c)).ToArray();
        }

        Connection result = new Connection(server, session, connectionChannel, verbMap, CreateConnectionExtensions);
        return result;
    }

    /// <summary>
    ///     Start the Tls stream.
    /// </summary>
    /// <param name="s">stream.</param>
    /// <returns>A <see cref="Task{TResult}" /> representing the result of the asynchronous operation.</returns>
    internal async Task<Stream> StartImplicitTls(Stream s)
    {
        SslStream sslStream = new SslStream(s);

        SslProtocols sslProtos;

        string ver = Assembly.GetEntryAssembly()?.GetCustomAttribute<TargetFrameworkAttribute>()?.FrameworkName;
        if (ver == null || !ver.StartsWith(".NETCoreApp,"))
        {
            sslProtos = SslProtocols.Tls12 | SslProtocols.Tls | SslProtocols.Tls11 | SslProtocols.Ssl3 |
                        SslProtocols.Ssl2;
        }
        else
        {
            sslProtos = SslProtocols.None;
        }

        X509Certificate cert =
            await Server.Options.GetSSLCertificate(this).ConfigureAwait(false);

        await sslStream.AuthenticateAsServerAsync(cert, false, sslProtos, false).ConfigureAwait(false);
        return sslStream;
    }

    /// <summary>
    ///     Starts processing of this connection.
    /// </summary>
    /// <returns>A <see cref="Task{T}" /> representing the async operation.</returns>
    internal async Task ProcessAsync()
    {
        try
        {
            await Server.Options.OnSessionStarted(this, Session).ConfigureAwait(false);

            if (await Server.Options.IsSSLEnabled(this).ConfigureAwait(false))
            {
                await ConnectionChannel.ApplyStreamFilter(StartImplicitTls).ConfigureAwait(false);

                Session.SecureConnection = true;
            }

            await WriteResponse(new SmtpResponse(
                StandardSmtpResponseCode.ServiceReady,
                Server.Options.DomainName + " smtp4dev ready")).ConfigureAwait(false);

            while (ConnectionChannel.IsConnected)
            {
                await ReadAndProcessNextCommand().ConfigureAwait(false);
            }
        }
        catch (IOException ioException)
        {
            Session.SessionError = ioException;
            Session.SessionErrorType = SessionErrorType.NetworkError;
        }
#pragma warning disable CA1031 // Do not catch general exception types
        catch (Exception exception)
        {
            Session.SessionError = exception;
            Session.SessionErrorType = SessionErrorType.UnexpectedException;
        }
#pragma warning restore CA1031 // Do not catch general exception types

        await CloseConnection().ConfigureAwait(false);

        Session.EndDate = DateTime.Now;
        await Server.Options.OnSessionCompleted(this, Session).ConfigureAwait(false);
    }

    private async Task ReadAndProcessNextCommand()
    {
        bool badCommand = false;
        SmtpCommand command = new SmtpCommand(await ReadLine().ConfigureAwait(false));
        await Server.Options.OnCommandReceived(this, command).ConfigureAwait(false);

        if (command.IsValid)
        {
            badCommand = !await TryProcessCommand(command).ConfigureAwait(false);
        }
        else if (!command.IsEmpty)
        {
            badCommand = true;
        }

        if (badCommand)
        {
            await Session.IncrementBadCommandCounter().ConfigureAwait(false);

            if (Server.Options.MaximumNumberOfSequentialBadCommands > 0 &&
                Session.NumberOfBadCommandsInARow >= Server.Options.MaximumNumberOfSequentialBadCommands)
            {
                await WriteResponse(new SmtpResponse(StandardSmtpResponseCode.ClosingTransmissionChannel,
                    "Too many bad commands. Bye!")).ConfigureAwait(false);
                await CloseConnection().ConfigureAwait(false);
            }
            else
            {
                await WriteResponse(new SmtpResponse(
                    StandardSmtpResponseCode.SyntaxErrorCommandUnrecognised,
                    "Command unrecognised")).ConfigureAwait(false);
            }
        }
        else
        {
            await Session.ResetBadCommandCounter().ConfigureAwait(false);
        }
    }

    private async Task<bool> TryProcessCommand(SmtpCommand command)
    {
        IVerb verbProcessor = VerbMap.GetVerbProcessor(command.Verb);

        if (verbProcessor != null)
        {
            try
            {
                await verbProcessor.Process(this, command).ConfigureAwait(false);
            }
            catch (SmtpServerException exception)
            {
                await WriteResponse(exception.SmtpResponse).ConfigureAwait(false);
            }

            return true;
        }

        return false;
    }

    /// <summary>
    ///     Writes a line of text to the client.
    /// </summary>
    /// <param name="text">
    ///     The text<see cref="string" /> optionally containing placeholders into which <paramref name="args" />
    ///     are subtituted using <see cref="string.Format(string, object[])" />.
    /// </param>
    /// <param name="args">The arguments which are formatted into <paramref name="text" />.</param>
    /// <returns>
    ///     The <see cref="Task" />.
    /// </returns>
    protected async Task WriteLineAndFlush(string text, params object[] args)
    {
        string formattedText = string.Format(CultureInfo.InvariantCulture, text, args);
        await Session.AppendLineToSessionLog(formattedText).ConfigureAwait(false);
        await ConnectionChannel.WriteLine(formattedText).ConfigureAwait(false);
        await ConnectionChannel.Flush().ConfigureAwait(false);
    }

    private async Task OnConnectionChannelClosed(object sender, EventArgs eventArgs)
    {
        ConnectionEventArgs connEventArgs = new ConnectionEventArgs(this);

        foreach (Delegate handler
                 in ConnectionClosedEventHandler?.GetInvocationList() ?? Enumerable.Empty<Delegate>())
        {
            await ((Task)handler.DynamicInvoke(this, connEventArgs)).ConfigureAwait(false);
        }
    }
}
