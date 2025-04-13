using System;

namespace LumiSoft.Net.IMAP
{
	/// <summary>
	/// IMAP flags store type.
	/// </summary>
	public enum IMAP_Flags_SetType
	{		
		/// <summary>
		/// Flags are added to existing ones.
		/// </summary>
		Add = 1,

		/// <summary>
		/// Flags are removed from existing ones.
		/// </summary>
		Remove = 3,

		/// <summary>
		/// Flags are replaced.
		/// </summary>
		Replace = 4,
	}
}
