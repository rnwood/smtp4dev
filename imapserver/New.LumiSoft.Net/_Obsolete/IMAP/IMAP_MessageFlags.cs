using System;

namespace LumiSoft.Net.IMAP
{
	/// <summary>
	/// IMAP message flags.
	/// </summary>
    [Obsolete("Use IMAP_t_MsgFlags instead.")]
	public enum IMAP_MessageFlags
	{
        /// <summary>
        /// No flags defined.
        /// </summary>
        None = 0,

		/// <summary>
		/// Message has been read.
		/// </summary>
		Seen = 2,

		/// <summary>
		/// Message has been answered.
		/// </summary>
		Answered = 4,

		/// <summary>
		/// Message is "flagged" for urgent/special attention.
		/// </summary>
		Flagged = 8,

		/// <summary>
		/// Message is "deleted" for removal by later EXPUNGE.
		/// </summary>
		Deleted = 16,

		/// <summary>
		/// Message has not completed composition.
		/// </summary>
		Draft = 32,

		/// <summary>
		/// Message is "recently" arrived in this mailbox.
		/// </summary>
		Recent = 64,
	}
}
