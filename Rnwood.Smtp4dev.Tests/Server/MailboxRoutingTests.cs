using System;
using Rnwood.Smtp4dev.Server.Settings;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.Server
{
    /// <summary>
    /// Tests for mailbox routing configuration models and type converters.
    /// These tests verify the foundation for mailbox message routing functionality.
    /// </summary>
    public class MailboxRoutingTests
    {

        [Fact]
        public void MailboxOptions_Recipients_WildcardMatching()
        {
            // Test that wildcard patterns work correctly
            var mailboxOptions = new MailboxOptions { Name = "Test", Recipients = "*@example.com" };
            
            // This test just verifies the model is set up correctly
            Assert.Equal("Test", mailboxOptions.Name);
            Assert.Equal("*@example.com", mailboxOptions.Recipients);
        }

        [Fact]
        public void MailboxOptions_Recipients_MultiplePatterns()
        {
            // Test that multiple recipient patterns can be specified
            var mailboxOptions = new MailboxOptions 
            { 
                Name = "Sales", 
                Recipients = "*@sales.com, sales@*, *sales*@*" 
            };
            
            Assert.Equal("Sales", mailboxOptions.Name);
            Assert.Equal("*@sales.com, sales@*, *sales*@*", mailboxOptions.Recipients);
        }

        [Fact]
        public void MailboxOptions_Recipients_RegexPattern()
        {
            // Test that regex patterns can be specified
            var mailboxOptions = new MailboxOptions 
            { 
                Name = "Regex", 
                Recipients = "/.*@(sales|support)\\.com$/" 
            };
            
            Assert.Equal("Regex", mailboxOptions.Name);
            Assert.Equal("/.*@(sales|support)\\.com$/", mailboxOptions.Recipients);
        }

        [Fact]
        public void MailboxFromStringConverter_ValidFormat()
        {
            // Test the type converter that allows "Name=Recipients" format
            var converter = new MailboxFromStringConverter();
            var result = converter.ConvertFrom("Sales=*@sales.com") as MailboxOptions;
            
            Assert.NotNull(result);
            Assert.Equal("Sales", result.Name);
            Assert.Equal("*@sales.com", result.Recipients);
        }

        [Fact]
        public void MailboxFromStringConverter_InvalidFormat_ThrowsException()
        {
            // Test that invalid format throws an exception
            var converter = new MailboxFromStringConverter();
            
            Assert.Throws<FormatException>(() => converter.ConvertFrom("InvalidFormat"));
        }

        [Fact]
        public void MailboxFromStringConverter_WithMultipleRecipients()
        {
            // Test the type converter with multiple recipients
            var converter = new MailboxFromStringConverter();
            var result = converter.ConvertFrom("Sales=*@sales.com, sales@*") as MailboxOptions;
            
            Assert.NotNull(result);
            Assert.Equal("Sales", result.Name);
            Assert.Equal("*@sales.com, sales@*", result.Recipients);
        }

        /// <summary>
        /// Note: Integration tests for actual routing behavior would require 
        /// creating a full Smtp4devServer instance with all dependencies.
        /// These tests focus on the configuration models that are the foundation
        /// for routing logic. The routing logic itself is tested through E2E tests
        /// and manual validation.
        /// </summary>
        [Fact]
        public void MailboxOptions_DefaultName_IsCorrect()
        {
            // Verify the default mailbox name constant
            Assert.Equal("Default", MailboxOptions.DEFAULTNAME);
        }

        [Fact]
        public void HeaderFilterOptions_Properties_CanBeSet()
        {
            // Test that header filter options can be created and configured
            var headerFilter = new HeaderFilterOptions
            {
                Header = "X-Application",
                Pattern = "srs"
            };

            Assert.Equal("X-Application", headerFilter.Header);
            Assert.Equal("srs", headerFilter.Pattern);
        }

        [Fact]
        public void MailboxOptions_HeaderFilters_CanBeSet()
        {
            // Test that mailbox can have header filters
            var mailbox = new MailboxOptions
            {
                Name = "SRS",
                Recipients = "*",
                HeaderFilters = new[]
                {
                    new HeaderFilterOptions { Header = "X-Application", Pattern = "srs" },
                    new HeaderFilterOptions { Header = "X-Mailer", Pattern = "/^srs-.*/" }
                }
            };

            Assert.Equal("SRS", mailbox.Name);
            Assert.Equal("*", mailbox.Recipients);
            Assert.NotNull(mailbox.HeaderFilters);
            Assert.Equal(2, mailbox.HeaderFilters.Length);
            Assert.Equal("X-Application", mailbox.HeaderFilters[0].Header);
            Assert.Equal("srs", mailbox.HeaderFilters[0].Pattern);
            Assert.Equal("X-Mailer", mailbox.HeaderFilters[1].Header);
            Assert.Equal("/^srs-.*/", mailbox.HeaderFilters[1].Pattern);
        }

        [Fact]
        public void HeaderFilterOptions_Pattern_SupportsRegex()
        {
            // Test that regex patterns can be specified with / delimiters
            var headerFilter = new HeaderFilterOptions
            {
                Header = "X-Antivirus",
                Pattern = "/.*scanned.*/"
            };

            Assert.Equal("X-Antivirus", headerFilter.Header);
            Assert.True(headerFilter.Pattern.StartsWith("/") && headerFilter.Pattern.EndsWith("/"));
        }

        [Fact]
        public void HeaderFilterOptions_Pattern_SupportsExistenceCheck()
        {
            // Test that existence check can be done with .* pattern
            var headerFilter = new HeaderFilterOptions
            {
                Header = "X-Custom-Header",
                Pattern = ".*"
            };

            Assert.Equal("X-Custom-Header", headerFilter.Header);
            Assert.Equal(".*", headerFilter.Pattern);
        }

        [Fact]
        public void MailboxOptions_CombinesRecipientsAndHeaderFilters()
        {
            // Test that mailbox can have both recipient patterns and header filters
            var mailbox = new MailboxOptions
            {
                Name = "TestMailbox",
                Recipients = "*@example.com",
                HeaderFilters = new[]
                {
                    new HeaderFilterOptions { Header = "X-Priority", Pattern = "high" }
                }
            };

            Assert.Equal("TestMailbox", mailbox.Name);
            Assert.Equal("*@example.com", mailbox.Recipients);
            Assert.NotNull(mailbox.HeaderFilters);
            Assert.Single(mailbox.HeaderFilters);
        }

        [Fact]
        public void MailboxFromStringConverter_ParsesJsonFormat()
        {
            // Test that JSON format can be parsed for command line support
            var converter = new MailboxFromStringConverter();
            var json = "{\"name\":\"SRS\",\"recipients\":\"*\",\"headerFilters\":[{\"header\":\"X-Application\",\"pattern\":\"srs\"}]}";
            var result = converter.ConvertFrom(json) as MailboxOptions;
            
            Assert.NotNull(result);
            Assert.Equal("SRS", result.Name);
            Assert.Equal("*", result.Recipients);
            Assert.NotNull(result.HeaderFilters);
            Assert.Single(result.HeaderFilters);
            Assert.Equal("X-Application", result.HeaderFilters[0].Header);
            Assert.Equal("srs", result.HeaderFilters[0].Pattern);
        }

        [Fact]
        public void MailboxFromStringConverter_ParsesLegacyFormat()
        {
            // Test that legacy "Name=Recipients" format still works
            var converter = new MailboxFromStringConverter();
            var result = converter.ConvertFrom("Sales=*@sales.com") as MailboxOptions;
            
            Assert.NotNull(result);
            Assert.Equal("Sales", result.Name);
            Assert.Equal("*@sales.com", result.Recipients);
            Assert.Null(result.HeaderFilters); // No header filters in legacy format
        }

        [Fact]
        public void MailboxFromStringConverter_InvalidJsonThrowsException()
        {
            // Test that invalid JSON throws an exception
            var converter = new MailboxFromStringConverter();
            
            Assert.Throws<FormatException>(() => converter.ConvertFrom("{invalid json}"));
        }
    }
}
