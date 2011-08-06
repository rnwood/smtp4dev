using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using Rnwood.SmtpServer.Extensions.Auth;

namespace Rnwood.SmtpServer
{
    public interface IEditableSession : ISession
    {
        DateTime StartDate { get; set; }
        
        DateTime? EndDate { get; set; }

        IPAddress ClientAddress { get; set; }

        string ClientName { get; set; }

        bool SecureConnection { get; set; }

        void AddMessage(IMessage message);
        
        bool CompletedNormally { get; set; }

        bool Authenticated { get; set; }

        IAuthenticationRequest AuthenticationCredentials { get; set; }

        string SessionError { get; set; }

        void AppendToLog(string text);
    }

    public interface ISession
    {
        /// <summary>
        /// Gets the date the session started.
        /// </summary>
        /// <value>The start date.</value>
        DateTime StartDate { get; }

        /// <summary>
        /// Gets the date the session ended.
        /// </summary>
        /// <value>The end date.</value>
        DateTime? EndDate { get;}

        /// <summary>
        /// Gets the IP address of the client that established this session.
        /// </summary>
        /// <value>The client address.</value>
        IPAddress ClientAddress { get;}

        /// <summary>
        /// Gets or sets the name of the client as reported in its HELO/EHLO command
        /// or null.
        /// </summary>
        /// <value>The name of the client.</value>
        string ClientName { get;}

        /// <summary>
        /// Gets or sets a value indicating whether the session is over a secure connection.
        /// </summary>
        /// <value><c>true</c> if [secure connection]; otherwise, <c>false</c>.</value>
        bool SecureConnection { get;}

        /// <summary>
        /// Gets the session log (all communication between the client and server)
        /// if session logging is enabled.
        /// </summary>
        /// <value>The log.</value>
        TextReader GetLog();

        /// <summary>
        /// Gets an array of messages recevied in this session.
        /// </summary>
        /// <value>The messages.</value>
        IMessage[] GetMessages();

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="AbstractSession"/> completed normally (by the client issuing a QUIT command).
        /// </summary>
        /// <value><c>true</c> if the session completed normally; otherwise, <c>false</c>.</value>
        bool CompletedNormally { get; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="AbstractSession"/> is authenticated.
        /// </summary>
        /// <value><c>true</c> if authenticated; otherwise, <c>false</c>.</value>
        bool Authenticated { get; }

        IAuthenticationRequest AuthenticationCredentials { get; }

        /// <summary>
        /// Gets the error that caused the session to terminate if it didn't complete normally.
        /// </summary>
        /// <seealso cref="CompletedNormally"/>
        /// <value>The session error.</value>
        string SessionError { get; }
    }
}