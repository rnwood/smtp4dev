// -----------------------------------------------------------------------
//
//   Copyright (C) 2003-2006 Angel Marin
// 
//   This file is part of SharpMimeTools
//
//   SharpMimeTools is free software; you can redistribute it and/or
//   modify it under the terms of the GNU Lesser General Public
//   License as published by the Free Software Foundation; either
//   version 2.1 of the License, or (at your option) any later version.
//
//   SharpMimeTools is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE. See the GNU
//   Lesser General Public License for more details.
//
//   You should have received a copy of the GNU Lesser General Public
//   License along with SharpMimeTools; if not, write to the Free Software
//   Foundation, Inc., 51 Franklin Street, Fifth Floor,
//   Boston, MA  02110-1301  USA
//
// -----------------------------------------------------------------------

using System;

namespace anmar.SharpMimeTools
{
	/// <summary>
	/// Options used while decoding the message's content.
	/// </summary>
	[System.Flags]
	public enum SharpDecodeOptions {
		/// <summary>
		/// None of the above.
		/// </summary>
		None = 0,
		/// <summary>
		/// Allow attachments.
		/// </summary>
		AllowAttachments = 1,
		/// <summary>
		/// Allow html body parts. If there is no alternative part to the html one, the body part will be ignored
		/// </summary>
		AllowHtml = 2,
		/// <summary>
		/// Add a named anchor to the html body before each body part that has a <b>Content-ID</b> header. The anchor will be named as <b>Message-ID</b>_<b>Content-ID</b>
		/// </summary>
		NamedAnchors = 4,
		/// <summary>
		/// Decode ms-tnef content
		/// </summary>
		DecodeTnef = 8,
		/// <summary>
		/// Decode uuencoded content
		/// </summary>
		UuDecode = 16,
		/// <summary>
		/// If folder where attachemts are saved does not exist, create it.
		/// </summary>
		CreateFolder = 32,
		/// <summary>
		/// Do not decode <i>message/rfc822</i> parts recursively and present them as attachments.
		/// </summary>
		NotRecursiveRfc822 = 64,
		/// <summary>
		/// Default options (<b>AllowAttachments</b> and <b>AllowHtml</b>)
		/// </summary>
		Default = AllowAttachments | AllowHtml
	}
}
