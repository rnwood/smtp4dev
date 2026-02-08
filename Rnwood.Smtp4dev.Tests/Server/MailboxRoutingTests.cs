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
    }
}
