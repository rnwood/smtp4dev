using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using DotNet.Globbing;
using Rnwood.Smtp4dev.Server.Settings;

namespace Rnwood.Smtp4dev.Server
{
    /// <summary>
    /// Handles routing logic for determining which mailbox(es) should receive a message
    /// based on recipients and header filters.
    /// </summary>
    public class MailboxRouter
    {
        /// <summary>
        /// Finds the target mailbox for a recipient based on mailbox configurations and message headers.
        /// </summary>
        /// <param name="recipient">The recipient email address</param>
        /// <param name="mailboxes">Available mailbox configurations (in priority order)</param>
        /// <param name="messageHeaders">Parsed message headers (case-insensitive dictionary)</param>
        /// <returns>The matching mailbox or null if no match found</returns>
        public MailboxOptions FindMailboxForRecipient(
            string recipient, 
            IEnumerable<MailboxOptions> mailboxes,
            Dictionary<string, string> messageHeaders)
        {
            if (string.IsNullOrWhiteSpace(recipient))
            {
                return null;
            }

            foreach (var mailbox in mailboxes)
            {
                // Check header filters first (if any)
                if (mailbox.HeaderFilters != null && mailbox.HeaderFilters.Length > 0)
                {
                    bool allHeaderFiltersMatch = true;
                    
                    foreach (var headerFilter in mailbox.HeaderFilters)
                    {
                        if (!MatchesHeaderFilter(messageHeaders, headerFilter))
                        {
                            allHeaderFiltersMatch = false;
                            break;
                        }
                    }
                    
                    if (!allHeaderFiltersMatch)
                    {
                        continue; // Skip this mailbox if header filters don't match
                    }
                }

                // Then check recipient patterns
                if (MatchesRecipientPattern(recipient, mailbox.Recipients))
                {
                    return mailbox;
                }
            }

            return null;
        }

        /// <summary>
        /// Checks if a recipient matches the recipient pattern(s) of a mailbox.
        /// </summary>
        /// <param name="recipient">The recipient email address</param>
        /// <param name="recipientPatterns">Comma-separated patterns (glob or regex)</param>
        /// <returns>True if the recipient matches any pattern</returns>
        public bool MatchesRecipientPattern(string recipient, string recipientPatterns)
        {
            if (string.IsNullOrWhiteSpace(recipientPatterns))
            {
                return false;
            }

            foreach (var recipRule in recipientPatterns.Split(",", StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
            {
                bool isRegex = recipRule.StartsWith("/") && recipRule.EndsWith("/");

                bool isMatch = isRegex ?
                    MatchesRegexPattern(recipient, recipRule.Substring(1, recipRule.Length - 2)) :
                    MatchesGlobPattern(recipient, recipRule);

                if (isMatch)
                {
                    return true;
                }
            }

            return false;
        }

        /// <summary>
        /// Checks if a message header matches a header filter.
        /// </summary>
        /// <param name="messageHeaders">Message headers (case-insensitive dictionary)</param>
        /// <param name="headerFilter">The header filter configuration</param>
        /// <returns>True if the header matches the filter</returns>
        public bool MatchesHeaderFilter(Dictionary<string, string> messageHeaders, HeaderFilterOptions headerFilter)
        {
            if (messageHeaders == null || string.IsNullOrWhiteSpace(headerFilter?.Header))
            {
                return false;
            }

            // Check if header exists
            if (!messageHeaders.TryGetValue(headerFilter.Header, out string headerValue))
            {
                return false; // Header not present in message
            }

            // If no pattern specified, just check existence
            if (string.IsNullOrWhiteSpace(headerFilter.Pattern))
            {
                return true;
            }

            // Check if pattern is a regex (surrounded by /)
            bool isRegex = headerFilter.Pattern.StartsWith("/") && headerFilter.Pattern.EndsWith("/");

            if (isRegex)
            {
                string pattern = headerFilter.Pattern.Substring(1, headerFilter.Pattern.Length - 2);
                return MatchesRegexPattern(headerValue, pattern);
            }
            else
            {
                // Exact match (case-insensitive) or wildcard match using glob
                return MatchesGlobPattern(headerValue, headerFilter.Pattern);
            }
        }

        /// <summary>
        /// Matches a value against a regex pattern with timeout protection.
        /// </summary>
        private bool MatchesRegexPattern(string value, string pattern)
        {
            try
            {
                // Use timeout to prevent ReDoS (Regular Expression Denial of Service) attacks
                return Regex.IsMatch(value, pattern, RegexOptions.IgnoreCase, TimeSpan.FromSeconds(1));
            }
            catch (RegexMatchTimeoutException)
            {
                // Regex timed out - treat as non-match
                return false;
            }
        }

        /// <summary>
        /// Matches a value against a glob pattern.
        /// </summary>
        private bool MatchesGlobPattern(string value, string pattern)
        {
            // Use case-insensitive matching to be consistent with regex matching
            var options = new GlobOptions { Evaluation = { CaseInsensitive = true } };
            return Glob.Parse(pattern, options).IsMatch(value);
        }
    }
}
