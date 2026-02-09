using System;
using System.Collections.Generic;
using Rnwood.Smtp4dev.Server;
using Rnwood.Smtp4dev.Server.Settings;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.Server
{
    /// <summary>
    /// Tests for MailboxRouter - the component that determines which mailbox(es)
    /// should receive a message based on recipients and header filters.
    /// </summary>
    public class MailboxRouterTests
    {
        private readonly MailboxRouter router = new MailboxRouter();

        #region Recipient Pattern Tests

        [Fact]
        public void MatchesRecipientPattern_ExactMatch()
        {
            // Exact match using glob
            Assert.True(router.MatchesRecipientPattern("user@example.com", "user@example.com"));
            Assert.False(router.MatchesRecipientPattern("other@example.com", "user@example.com"));
        }

        [Fact]
        public void MatchesRecipientPattern_WildcardDomain()
        {
            // Wildcard for entire domain
            Assert.True(router.MatchesRecipientPattern("user@sales.com", "*@sales.com"));
            Assert.True(router.MatchesRecipientPattern("admin@sales.com", "*@sales.com"));
            Assert.False(router.MatchesRecipientPattern("user@other.com", "*@sales.com"));
        }

        [Fact]
        public void MatchesRecipientPattern_WildcardUser()
        {
            // Wildcard for user part
            Assert.True(router.MatchesRecipientPattern("sales@any.com", "sales@*"));
            Assert.True(router.MatchesRecipientPattern("sales@example.com", "sales@*"));
            Assert.False(router.MatchesRecipientPattern("support@example.com", "sales@*"));
        }

        [Fact]
        public void MatchesRecipientPattern_MultiplePatterns()
        {
            // Multiple patterns separated by commas
            var patterns = "*@sales.com, *@support.com";
            Assert.True(router.MatchesRecipientPattern("user@sales.com", patterns));
            Assert.True(router.MatchesRecipientPattern("user@support.com", patterns));
            Assert.False(router.MatchesRecipientPattern("user@other.com", patterns));
        }

        [Fact]
        public void MatchesRecipientPattern_RegexPattern()
        {
            // Regex pattern for multiple domains
            var pattern = "/.*@(sales|support)\\.com$/";
            Assert.True(router.MatchesRecipientPattern("user@sales.com", pattern));
            Assert.True(router.MatchesRecipientPattern("user@support.com", pattern));
            Assert.False(router.MatchesRecipientPattern("user@other.com", pattern));
        }

        [Fact]
        public void MatchesRecipientPattern_CaseInsensitive()
        {
            // Pattern matching is case-insensitive
            Assert.True(router.MatchesRecipientPattern("User@Sales.COM", "*@sales.com"));
        }

        #endregion

        #region Header Filter Tests

        [Fact]
        public void MatchesHeaderFilter_ExactMatch()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Application", "srs" }
            };
            var filter = new HeaderFilterOptions { Header = "X-Application", Pattern = "srs" };

            Assert.True(router.MatchesHeaderFilter(headers, filter));
        }

        [Fact]
        public void MatchesHeaderFilter_ExactMatch_CaseInsensitive()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Application", "SRS" }
            };
            var filter = new HeaderFilterOptions { Header = "x-application", Pattern = "srs" };

            // Header name is case-insensitive, pattern uses glob which is case-sensitive by default
            // but our implementation uses case-insensitive regex
            Assert.True(router.MatchesHeaderFilter(headers, filter));
        }

        [Fact]
        public void MatchesHeaderFilter_NoMatch()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Application", "other" }
            };
            var filter = new HeaderFilterOptions { Header = "X-Application", Pattern = "srs" };

            Assert.False(router.MatchesHeaderFilter(headers, filter));
        }

        [Fact]
        public void MatchesHeaderFilter_HeaderNotPresent()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Other", "value" }
            };
            var filter = new HeaderFilterOptions { Header = "X-Application", Pattern = "srs" };

            Assert.False(router.MatchesHeaderFilter(headers, filter));
        }

        [Fact]
        public void MatchesHeaderFilter_ExistenceCheck()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Antivirus", "ClamAV-Scanned" }
            };
            var filter = new HeaderFilterOptions { Header = "X-Antivirus", Pattern = "*" };

            // Any value should match with * glob pattern
            Assert.True(router.MatchesHeaderFilter(headers, filter));
        }

        [Fact]
        public void MatchesHeaderFilter_EmptyPattern_ChecksExistence()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Custom", "value" }
            };
            var filter = new HeaderFilterOptions { Header = "X-Custom", Pattern = "" };

            // Empty pattern means just check if header exists
            Assert.True(router.MatchesHeaderFilter(headers, filter));
        }

        [Fact]
        public void MatchesHeaderFilter_RegexPattern()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Priority", "high" }
            };
            var filter = new HeaderFilterOptions { Header = "X-Priority", Pattern = "/^(high|urgent)$/" };

            Assert.True(router.MatchesHeaderFilter(headers, filter));
        }

        [Fact]
        public void MatchesHeaderFilter_RegexPattern_NoMatch()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Priority", "low" }
            };
            var filter = new HeaderFilterOptions { Header = "X-Priority", Pattern = "/^(high|urgent)$/" };

            Assert.False(router.MatchesHeaderFilter(headers, filter));
        }

        [Fact]
        public void MatchesHeaderFilter_NullHeaders_ReturnsFalse()
        {
            var filter = new HeaderFilterOptions { Header = "X-Application", Pattern = "srs" };
            Assert.False(router.MatchesHeaderFilter(null, filter));
        }

        [Fact]
        public void MatchesHeaderFilter_WildcardPattern()
        {
            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Mailer", "srs-module-v1" }
            };
            var filter = new HeaderFilterOptions { Header = "X-Mailer", Pattern = "srs-*" };

            Assert.True(router.MatchesHeaderFilter(headers, filter));
        }

        #endregion

        #region FindMailboxForRecipient Tests

        [Fact]
        public void FindMailboxForRecipient_MatchesFirstMailbox()
        {
            var mailboxes = new[]
            {
                new MailboxOptions { Name = "Sales", Recipients = "*@sales.com" },
                new MailboxOptions { Name = "Support", Recipients = "*@support.com" },
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            var result = router.FindMailboxForRecipient("user@sales.com", mailboxes, null, null, null);

            Assert.NotNull(result);
            Assert.Equal("Sales", result.Name);
        }

        [Fact]
        public void FindMailboxForRecipient_FallsBackToDefault()
        {
            var mailboxes = new[]
            {
                new MailboxOptions { Name = "Sales", Recipients = "*@sales.com" },
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            var result = router.FindMailboxForRecipient("user@other.com", mailboxes, null, null, null);

            Assert.NotNull(result);
            Assert.Equal("Default", result.Name);
        }

        [Fact]
        public void FindMailboxForRecipient_NoMatch_ReturnsNull()
        {
            var mailboxes = new[]
            {
                new MailboxOptions { Name = "Sales", Recipients = "*@sales.com" }
            };

            var result = router.FindMailboxForRecipient("user@other.com", mailboxes, null, null, null);

            Assert.Null(result);
        }

        [Fact]
        public void FindMailboxForRecipient_WithHeaderFilter_BothMustMatch()
        {
            var mailboxes = new[]
            {
                new MailboxOptions
                {
                    Name = "SRS",
                    Recipients = "*",
                    HeaderFilters = new[]
                    {
                        new HeaderFilterOptions { Header = "X-Application", Pattern = "srs" }
                    }
                },
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Application", "srs" }
            };

            var result = router.FindMailboxForRecipient("user@example.com", mailboxes, null, null, headers);

            Assert.NotNull(result);
            Assert.Equal("SRS", result.Name);
        }

        [Fact]
        public void FindMailboxForRecipient_HeaderFilterNoMatch_SkipsMailbox()
        {
            var mailboxes = new[]
            {
                new MailboxOptions
                {
                    Name = "SRS",
                    Recipients = "*",
                    HeaderFilters = new[]
                    {
                        new HeaderFilterOptions { Header = "X-Application", Pattern = "srs" }
                    }
                },
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Application", "other" }
            };

            var result = router.FindMailboxForRecipient("user@example.com", mailboxes, null, null, headers);

            Assert.NotNull(result);
            Assert.Equal("Default", result.Name);
        }

        [Fact]
        public void FindMailboxForRecipient_MultipleHeaderFilters_AllMustMatch()
        {
            var mailboxes = new[]
            {
                new MailboxOptions
                {
                    Name = "Critical-Sales",
                    Recipients = "*@sales.com",
                    HeaderFilters = new[]
                    {
                        new HeaderFilterOptions { Header = "X-Priority", Pattern = "high" },
                        new HeaderFilterOptions { Header = "X-Department", Pattern = "sales" }
                    }
                },
                new MailboxOptions { Name = "Sales", Recipients = "*@sales.com" },
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            var headersAllMatch = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Priority", "high" },
                { "X-Department", "sales" }
            };

            var result = router.FindMailboxForRecipient("user@sales.com", mailboxes, null, null, headersAllMatch);
            Assert.Equal("Critical-Sales", result.Name);

            // If one filter doesn't match, should skip to next mailbox
            var headersPartialMatch = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Priority", "high" },
                { "X-Department", "support" }
            };

            result = router.FindMailboxForRecipient("user@sales.com", mailboxes, null, null, headersPartialMatch);
            Assert.Equal("Sales", result.Name);
        }

        [Fact]
        public void FindMailboxForRecipient_FirstMatchWins()
        {
            var mailboxes = new[]
            {
                new MailboxOptions { Name = "First", Recipients = "*@example.com" },
                new MailboxOptions { Name = "Second", Recipients = "*@example.com" },
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            var result = router.FindMailboxForRecipient("user@example.com", mailboxes, null, null, null);

            Assert.NotNull(result);
            Assert.Equal("First", result.Name);
        }

        [Fact]
        public void FindMailboxForRecipient_NullOrEmptyRecipient_ReturnsNull()
        {
            var mailboxes = new[]
            {
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            Assert.Null(router.FindMailboxForRecipient(null, mailboxes, null, null, null));
            Assert.Null(router.FindMailboxForRecipient("", mailboxes, null, null, null));
            Assert.Null(router.FindMailboxForRecipient("   ", mailboxes, null, null, null));
        }

        #endregion

        #region Source Filter Tests

        [Fact]
        public void MatchesSourceFilter_ExactHostnameMatch()
        {
            var filter = new SourceFilterOptions { Pattern = "legacy-stack.dev.example.org" };
            
            Assert.True(router.MatchesSourceFilter("legacy-stack.dev.example.org", "192.168.1.1", filter));
            Assert.False(router.MatchesSourceFilter("modern-stack.dev.example.org", "192.168.1.1", filter));
        }

        [Fact]
        public void MatchesSourceFilter_ExactIPMatch()
        {
            var filter = new SourceFilterOptions { Pattern = "192.168.1.100" };
            
            Assert.True(router.MatchesSourceFilter("some-host.example.com", "192.168.1.100", filter));
            Assert.False(router.MatchesSourceFilter("some-host.example.com", "192.168.1.101", filter));
        }

        [Fact]
        public void MatchesSourceFilter_WildcardHostname()
        {
            var filter = new SourceFilterOptions { Pattern = "*.dev.example.org" };
            
            Assert.True(router.MatchesSourceFilter("legacy-stack.dev.example.org", "192.168.1.1", filter));
            Assert.True(router.MatchesSourceFilter("modern-stack.dev.example.org", "192.168.1.1", filter));
            Assert.False(router.MatchesSourceFilter("prod.example.org", "192.168.1.1", filter));
        }

        [Fact]
        public void MatchesSourceFilter_WildcardIP()
        {
            var filter = new SourceFilterOptions { Pattern = "192.168.1.*" };
            
            Assert.True(router.MatchesSourceFilter("host.example.com", "192.168.1.100", filter));
            Assert.True(router.MatchesSourceFilter("host.example.com", "192.168.1.200", filter));
            Assert.False(router.MatchesSourceFilter("host.example.com", "192.168.2.100", filter));
        }

        [Fact]
        public void MatchesSourceFilter_RegexPattern()
        {
            var filter = new SourceFilterOptions { Pattern = "/^(legacy|modern)-stack\\.dev\\.example\\.org$/" };
            
            Assert.True(router.MatchesSourceFilter("legacy-stack.dev.example.org", "192.168.1.1", filter));
            Assert.True(router.MatchesSourceFilter("modern-stack.dev.example.org", "192.168.1.1", filter));
            Assert.False(router.MatchesSourceFilter("other-stack.dev.example.org", "192.168.1.1", filter));
        }

        [Fact]
        public void MatchesSourceFilter_RegexIP()
        {
            var filter = new SourceFilterOptions { Pattern = "/^192\\.168\\.(1|2)\\..*$/" };
            
            Assert.True(router.MatchesSourceFilter("host.example.com", "192.168.1.100", filter));
            Assert.True(router.MatchesSourceFilter("host.example.com", "192.168.2.100", filter));
            Assert.False(router.MatchesSourceFilter("host.example.com", "192.168.3.100", filter));
        }

        [Fact]
        public void MatchesSourceFilter_PrioritizesHostnameOverIP()
        {
            var filter = new SourceFilterOptions { Pattern = "legacy-stack.dev.example.org" };
            
            // Should match hostname even if IP doesn't match
            Assert.True(router.MatchesSourceFilter("legacy-stack.dev.example.org", "192.168.1.1", filter));
        }

        [Fact]
        public void MatchesSourceFilter_FallbackToIP()
        {
            var filter = new SourceFilterOptions { Pattern = "192.168.1.100" };
            
            // Should match IP when hostname doesn't match
            Assert.True(router.MatchesSourceFilter("other-host.example.com", "192.168.1.100", filter));
        }

        [Fact]
        public void MatchesSourceFilter_NullOrEmptyPattern_ReturnsFalse()
        {
            var filter1 = new SourceFilterOptions { Pattern = null };
            var filter2 = new SourceFilterOptions { Pattern = "" };
            
            Assert.False(router.MatchesSourceFilter("host.example.com", "192.168.1.1", filter1));
            Assert.False(router.MatchesSourceFilter("host.example.com", "192.168.1.1", filter2));
        }

        [Fact]
        public void MatchesSourceFilter_CaseInsensitive()
        {
            var filter = new SourceFilterOptions { Pattern = "legacy-stack.dev.example.org" };
            
            Assert.True(router.MatchesSourceFilter("Legacy-Stack.Dev.Example.Org", "192.168.1.1", filter));
        }

        [Fact]
        public void FindMailboxForRecipient_WithSourceFilter_HostnameMatch()
        {
            var mailboxes = new[]
            {
                new MailboxOptions
                {
                    Name = "Legacy Stack",
                    Recipients = "*",
                    SourceFilters = new[]
                    {
                        new SourceFilterOptions { Pattern = "legacy-stack.dev.example.org" }
                    }
                },
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            var result = router.FindMailboxForRecipient(
                "user@example.com", 
                mailboxes, 
                "legacy-stack.dev.example.org",
                "192.168.1.1",
                null);

            Assert.NotNull(result);
            Assert.Equal("Legacy Stack", result.Name);
        }

        [Fact]
        public void FindMailboxForRecipient_WithSourceFilter_IPMatch()
        {
            var mailboxes = new[]
            {
                new MailboxOptions
                {
                    Name = "Modern Stack",
                    Recipients = "*",
                    SourceFilters = new[]
                    {
                        new SourceFilterOptions { Pattern = "192.168.1.100" }
                    }
                },
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            var result = router.FindMailboxForRecipient(
                "user@example.com", 
                mailboxes, 
                "unknown-host",
                "192.168.1.100",
                null);

            Assert.NotNull(result);
            Assert.Equal("Modern Stack", result.Name);
        }

        [Fact]
        public void FindMailboxForRecipient_SourceFilterNoMatch_SkipsMailbox()
        {
            var mailboxes = new[]
            {
                new MailboxOptions
                {
                    Name = "Legacy Stack",
                    Recipients = "*",
                    SourceFilters = new[]
                    {
                        new SourceFilterOptions { Pattern = "legacy-stack.dev.example.org" }
                    }
                },
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            var result = router.FindMailboxForRecipient(
                "user@example.com", 
                mailboxes, 
                "modern-stack.dev.example.org",
                "192.168.1.2",
                null);

            Assert.NotNull(result);
            Assert.Equal("Default", result.Name);
        }

        [Fact]
        public void FindMailboxForRecipient_MultipleSourceFilters_AllMustMatch()
        {
            var mailboxes = new[]
            {
                new MailboxOptions
                {
                    Name = "Specific Source",
                    Recipients = "*",
                    SourceFilters = new[]
                    {
                        new SourceFilterOptions { Pattern = "*.dev.example.org" },
                        new SourceFilterOptions { Pattern = "192.168.*" }
                    }
                },
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            // Both filters match
            var result = router.FindMailboxForRecipient(
                "user@example.com", 
                mailboxes, 
                "legacy-stack.dev.example.org",
                "192.168.1.1",
                null);
            Assert.Equal("Specific Source", result.Name);

            // Only one filter matches (hostname)
            result = router.FindMailboxForRecipient(
                "user@example.com", 
                mailboxes, 
                "legacy-stack.dev.example.org",
                "10.0.0.1",
                null);
            Assert.Equal("Default", result.Name);

            // Only one filter matches (IP)
            result = router.FindMailboxForRecipient(
                "user@example.com", 
                mailboxes, 
                "other.example.org",
                "192.168.1.1",
                null);
            Assert.Equal("Default", result.Name);
        }

        [Fact]
        public void FindMailboxForRecipient_SourceAndHeaderFilters_AllMustMatch()
        {
            var mailboxes = new[]
            {
                new MailboxOptions
                {
                    Name = "Filtered",
                    Recipients = "*",
                    SourceFilters = new[]
                    {
                        new SourceFilterOptions { Pattern = "legacy-stack.dev.example.org" }
                    },
                    HeaderFilters = new[]
                    {
                        new HeaderFilterOptions { Header = "X-Application", Pattern = "app1" }
                    }
                },
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Application", "app1" }
            };

            // Both source and header match
            var result = router.FindMailboxForRecipient(
                "user@example.com", 
                mailboxes, 
                "legacy-stack.dev.example.org",
                "192.168.1.1",
                headers);
            Assert.Equal("Filtered", result.Name);

            // Source matches but header doesn't
            headers["X-Application"] = "app2";
            result = router.FindMailboxForRecipient(
                "user@example.com", 
                mailboxes, 
                "legacy-stack.dev.example.org",
                "192.168.1.1",
                headers);
            Assert.Equal("Default", result.Name);

            // Header matches but source doesn't
            headers["X-Application"] = "app1";
            result = router.FindMailboxForRecipient(
                "user@example.com", 
                mailboxes, 
                "other-host.example.org",
                "192.168.1.1",
                headers);
            Assert.Equal("Default", result.Name);
        }

        [Fact]
        public void FindMailboxForRecipient_SourceHeaderAndRecipient_AllMustMatch()
        {
            var mailboxes = new[]
            {
                new MailboxOptions
                {
                    Name = "Fully Filtered",
                    Recipients = "*@sales.com",
                    SourceFilters = new[]
                    {
                        new SourceFilterOptions { Pattern = "legacy-stack.dev.example.org" }
                    },
                    HeaderFilters = new[]
                    {
                        new HeaderFilterOptions { Header = "X-Application", Pattern = "app1" }
                    }
                },
                new MailboxOptions { Name = "Default", Recipients = "*" }
            };

            var headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                { "X-Application", "app1" }
            };

            // All match
            var result = router.FindMailboxForRecipient(
                "user@sales.com", 
                mailboxes, 
                "legacy-stack.dev.example.org",
                "192.168.1.1",
                headers);
            Assert.Equal("Fully Filtered", result.Name);

            // Wrong recipient
            result = router.FindMailboxForRecipient(
                "user@support.com", 
                mailboxes, 
                "legacy-stack.dev.example.org",
                "192.168.1.1",
                headers);
            Assert.Equal("Default", result.Name);
        }

        #endregion
    }
}
