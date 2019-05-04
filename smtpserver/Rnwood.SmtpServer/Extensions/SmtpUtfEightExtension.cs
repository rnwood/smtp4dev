﻿// <copyright file="SmtpUtfEightExtension.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer.Extensions
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using System.Threading.Tasks;
	using Rnwood.SmtpServer.Verbs;

	/// <summary>Implements the SMTPUTF8 extension.</summary>
	public class SmtpUtfEightExtension : IExtension
	{
		/// <summary>Creates the extension processor for a connection.</summary>
		/// <param name="connection">The connection<see cref="Rnwood.SmtpServer.IConnection"/>.</param>
		/// <returns>The <see cref="Rnwood.SmtpServer.Extensions.IExtensionProcessor"/>.</returns>
		public IExtensionProcessor CreateExtensionProcessor(IConnection connection)
		{
			return new SmtpUtfEightExtensionProcessor(connection);
		}

		private class SmtpUtfEightExtensionProcessor : ExtensionProcessor
		{
			public SmtpUtfEightExtensionProcessor(IConnection connection)
				: base(connection)
			{
				MailVerb mailVerbProcessor = connection.MailVerb;
				MailFromVerb mailFromProcessor = mailVerbProcessor.FromSubVerb;
				mailFromProcessor.ParameterProcessorMap.SetProcessor("SMTPUTF8", new SmtpUtfEightParameterProcessor());
			}

			public override Task<string[]> GetEHLOKeywords()
			{
				return Task.FromResult(new[] { "SMTPUTF8" });
			}
		}

		private class SmtpUtfEightParameterProcessor : IParameterProcessor
		{
			public Task SetParameter(IConnection connection, string key, string value)
			{
				connection.CurrentMessage.EightBitTransport = true;
				return Task.CompletedTask;
			}
		}
	}
}
