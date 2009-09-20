#region

using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using Rnwood.SmtpServer.Extensions.Auth;

#endregion

namespace Rnwood.SmtpServer
{
    public class Session
    {
        private readonly StringBuilder _log = new StringBuilder();

        internal Session()
        {
            Messages = new List<Message>();
        }

        /// <summary>
        /// Gets the date the session started.
        /// </summary>
        /// <value>The start date.</value>
        public DateTime StartDate { get; internal set; }

        /// <summary>
        /// Gets the date the session ended.
        /// </summary>
        /// <value>The end date.</value>
        public DateTime? EndDate { get; internal set; }

        /// <summary>
        /// Gets the IP address of the client that established this session.
        /// </summary>
        /// <value>The client address.</value>
        public IPAddress ClientAddress { get; internal set; }

        /// <summary>
        /// Gets or sets the name of the client as reported in its HELO/EHLO command
        /// or null.
        /// </summary>
        /// <value>The name of the client.</value>
        public string ClientName { get; internal set; }

        /// <summary>
        /// Gets or sets a value indicating whether the session is over a secure connection.
        /// </summary>
        /// <value><c>true</c> if [secure connection]; otherwise, <c>false</c>.</value>
        public bool SecureConnection { get; internal set; }

        /// <summary>
        /// Gets the session log (all communication between the client and server)
        /// if session logging is enabled.
        /// </summary>
        /// <value>The log.</value>
        public string Log
        {
            get { return _log.ToString(); }
        }

        /// <summary>
        /// Gets the list of messages recevied in this session.
        /// </summary>
        /// <value>The messages.</value>
        public List<Message> Messages { get; private set; }

        /// <summary>
        /// Gets or sets a value indicating whether this <see cref="Session"/> completed normally (by the client issuing a QUIT command).
        /// </summary>
        /// <value><c>true</c> if the session completed normally; otherwise, <c>false</c>.</value>
        public bool CompletedNormally { get; internal set; }

        /// <summary>
        /// Gets a value indicating whether this <see cref="Session"/> is authenticated.
        /// </summary>
        /// <value><c>true</c> if authenticated; otherwise, <c>false</c>.</value>
        public bool Authenticated { get; internal set; }

        public IAuthenticationRequest AuthenticationCredentials { get; internal set; }

        /// <summary>
        /// Gets the error that caused the session to terminate if it didn't complete normally.
        /// </summary>
        /// <seealso cref="CompletedNormally"/>
        /// <value>The session error.</value>
        public string SessionError { get; internal set; }

        internal void AppendToLog(string text)
        {
            _log.AppendLine(text);
        }
    }
}