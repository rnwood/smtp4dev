using Rnwood.Smtp4dev.DbModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Rnwood.Smtp4dev.Server.Imap
{
    /// <summary>
    /// Helpers for IMAP folder names, which use '/' as the hierarchy delimiter.
    /// </summary>
    public static class ImapFolderNames
    {
        public const char Delimiter = '/';

        /// <summary>
        /// Removes any trailing hierarchy delimiter and maps INBOX (which is case-insensitive per RFC 3501) to its canonical form.
        /// </summary>
        public static string Normalize(string folderName)
        {
            var name = (folderName ?? "").TrimEnd(Delimiter);
            return name.Equals(MailboxFolder.INBOX, StringComparison.OrdinalIgnoreCase) ? MailboxFolder.INBOX : name;
        }

        /// <summary>
        /// Returns true for the folders which always exist in every mailbox and cannot be deleted or renamed.
        /// </summary>
        public static bool IsDefaultFolder(string folderName)
        {
            return folderName == MailboxFolder.INBOX || folderName == MailboxFolder.SENT;
        }

        /// <summary>
        /// Returns the superior hierarchical names of a folder. E.g. "a/b/c" returns "a" and "a/b".
        /// </summary>
        public static IEnumerable<string> GetAncestors(string folderName)
        {
            for (int i = folderName.IndexOf(Delimiter); i > 0; i = folderName.IndexOf(Delimiter, i + 1))
            {
                yield return folderName.Substring(0, i);
            }
        }

        public static bool IsDescendantOf(string folderName, string ancestorFolderName)
        {
            return folderName.StartsWith(ancestorFolderName + Delimiter, StringComparison.Ordinal);
        }

        /// <summary>
        /// Checks if a folder name matches a LIST/LSUB reference name and mailbox pattern.
        /// '*' matches zero or more characters and '%' matches zero or more characters except the hierarchy delimiter.
        /// </summary>
        public static bool IsMatch(string folderName, string referenceName, string pattern)
        {
            var fullPattern = (referenceName ?? "") + (pattern ?? "");
            var regex = "^" + string.Concat(fullPattern.Select(c => c switch
            {
                '*' => ".*",
                '%' => "[^" + Regex.Escape(Delimiter.ToString()) + "]*",
                _ => Regex.Escape(c.ToString())
            })) + "$";

            // The pattern comes from the client, so avoid backtracking and limit the execution time
            var options = RegexOptions.NonBacktracking | (folderName == MailboxFolder.INBOX ? RegexOptions.IgnoreCase : RegexOptions.None);
            return Regex.IsMatch(folderName, regex, options, TimeSpan.FromSeconds(1));
        }
    }
}
