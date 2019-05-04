﻿// <copyright file="SmtpStringBuilder.cs" company="Rnwood.SmtpServer project contributors">
// Copyright (c) Rnwood.SmtpServer project contributors. All rights reserved.
// Licensed under the BSD license. See LICENSE.md file in the project root for full license information.
// </copyright>

namespace Rnwood.SmtpServer
{
	using System;
	using System.Collections.Generic;
	using System.Text;

	/// <summary>Builds a multi line string where each line has the CRLF terminator required for SMTP.</summary>
	public class SmtpStringBuilder
	{
		private readonly StringBuilder innerStringBuilder = new StringBuilder();

		/// <summary>Appends a line to the string and terminates it with the correct CRLF required for SMTP.</summary>
		/// <param name="text">The text.</param>
		public void AppendLine(string text)
		{
			this.innerStringBuilder.Append(text);
			this.innerStringBuilder.Append("\r\n");
		}

		/// <summary>  Returns the complete string including all lines which have been appended separated with the correct CRLF.</summary>
		/// <returns>A <see cref="string"/> that represents this instance.</returns>
		public override string ToString()
		{
			return this.innerStringBuilder.ToString();
		}
	}
}
