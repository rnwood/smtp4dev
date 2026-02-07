using System;
using System.Threading.Tasks;
using Rnwood.Smtp4dev.Server.Auth;
using Serilog;
using Xunit;

namespace Rnwood.Smtp4dev.Tests.Server.Auth
{
    public class OAuth2TokenValidatorTests
    {
        private readonly ILogger log = new LoggerConfiguration().CreateLogger();

        [Fact]
        public async Task ValidateTokenAsync_WithNullToken_ReturnsInvalid()
        {
            // Arrange
            var validator = new OAuth2TokenValidator(log);

            // Act
            var (isValid, subject, error) = await validator.ValidateTokenAsync(
                null, 
                "https://example.com", 
                "test-audience", 
                "test-issuer");

            // Assert
            Assert.False(isValid);
            Assert.Null(subject);
            Assert.Equal("Token is null or empty", error);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithEmptyToken_ReturnsInvalid()
        {
            // Arrange
            var validator = new OAuth2TokenValidator(log);

            // Act
            var (isValid, subject, error) = await validator.ValidateTokenAsync(
                "", 
                "https://example.com", 
                "test-audience", 
                "test-issuer");

            // Assert
            Assert.False(isValid);
            Assert.Null(subject);
            Assert.Equal("Token is null or empty", error);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithNullAuthority_ReturnsInvalid()
        {
            // Arrange
            var validator = new OAuth2TokenValidator(log);

            // Act
            var (isValid, subject, error) = await validator.ValidateTokenAsync(
                "test-token", 
                null, 
                "test-audience", 
                "test-issuer");

            // Assert
            Assert.False(isValid);
            Assert.Null(subject);
            Assert.Equal("OAuth2Authority is not configured", error);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithEmptyAuthority_ReturnsInvalid()
        {
            // Arrange
            var validator = new OAuth2TokenValidator(log);

            // Act
            var (isValid, subject, error) = await validator.ValidateTokenAsync(
                "test-token", 
                "", 
                "test-audience", 
                "test-issuer");

            // Assert
            Assert.False(isValid);
            Assert.Null(subject);
            Assert.Equal("OAuth2Authority is not configured", error);
        }

        [Fact]
        public async Task ValidateTokenAsync_WithInvalidToken_ReturnsInvalid()
        {
            // Arrange
            var validator = new OAuth2TokenValidator(log);

            // Act
            // Using a malformed token will cause validation to fail
            var (isValid, subject, error) = await validator.ValidateTokenAsync(
                "not-a-jwt-token", 
                "https://login.microsoftonline.com/common/v2.0", 
                "test-audience", 
                null);

            // Assert
            Assert.False(isValid);
            Assert.Null(subject);
            Assert.NotNull(error);
            Assert.Contains("Token validation error", error);
        }
    }
}
